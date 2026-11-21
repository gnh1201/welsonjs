// KeyVault.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace WelsonJS.Launcher.ApiEndpoints
{
    /// <summary>
    /// GET /credentials/keyvault/&lt;vault&gt;/&lt;secret&gt;: the value of an Azure Key Vault
    /// secret, read with the account signed in from Settings > Sign in to Azure...
    /// (AzureKeyVault). WelsonJS scripts use it for "keyvault:" entries of data/apikey.json.
    ///
    /// It hands out secrets, so on top of the launcher's check for local callers
    /// (ResourceServer.IsTrustedLocalRequest) it accepts only:
    /// <list type="bullet">
    /// <item>requests without browser headers (no Origin, no Sec-Fetch-Site), so neither a
    /// web page nor the launcher's own editor page can read secrets;</item>
    /// <item>requests that send "X-WelsonJS-Credentials-Token" with the value of
    /// %APPDATA%\WelsonJS\credentials.token, which the launcher writes at startup and only
    /// the current user can read, so other users and service accounts of the machine are
    /// refused.</item>
    /// </list>
    /// The launcher's API key (ResourceServerApiKey) does not open this endpoint.
    ///
    /// Answers: 200 {"value": "..."}; 400 invalid name; 401 sign-in required ("code":
    /// "sign_in_required"); 403 refused caller or no access to the vault; 404 no such
    /// secret; 502 Key Vault or the sign-in service failed; 503 AzureClientId not set.
    /// Responses are not cached (Cache-Control: no-store). Values are never logged.
    /// </summary>
    public class KeyVault : IApiEndpoint
    {
        private const string Prefix = "credentials/keyvault/";
        public const string TokenHeader = "X-WelsonJS-Credentials-Token";

        private readonly ResourceServer Server;
        private readonly ILogger _logger;
        private readonly string _accessToken;

        public KeyVault(ResourceServer server, HttpClient httpClient, ILogger logger)
        {
            Server = server;
            _logger = logger;
            _accessToken = CreateAccessTokenFile();
        }

        /// <summary>The file that holds the per-start access token of this endpoint.</summary>
        public static string AccessTokenPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WelsonJS", "credentials.token");

        public bool CanHandle(HttpListenerContext context, string path)
        {
            return path != null && path.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase);
        }

        public async Task HandleAsync(HttpListenerContext context, string path)
        {
            HttpListenerRequest request = context.Request;
            string target = path.Substring(Prefix.Length);

            if (!IsScriptRequest(request))
            {
                _logger?.Warn($"Refused a Key Vault request with browser headers: /{path} (Origin: {request.Headers["Origin"] ?? "(none)"}, Sec-Fetch-Site: {request.Headers["Sec-Fetch-Site"] ?? "(none)"})");
                await Reply(context, 403, "forbidden", "This API is only available to local scripts.");
                return;
            }
            if (_accessToken == null || !FixedTimeEquals(request.Headers[TokenHeader] ?? "", _accessToken))
            {
                _logger?.Warn($"Refused a Key Vault request without a valid {TokenHeader}: /{path}");
                await Reply(context, 403, "forbidden", $"Send {TokenHeader} with the contents of %APPDATA%\\WelsonJS\\credentials.token.");
                return;
            }
            if (!string.Equals(request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
            {
                await Reply(context, 405, "method_not_allowed", "Use GET.");
                return;
            }

            string[] parts = target.Split('/');
            if (parts.Length != 2 || !AzureKeyVault.IsValidVaultName(parts[0]) || !AzureKeyVault.IsValidSecretName(parts[1]))
            {
                await Reply(context, 400, "invalid_name", "Use /credentials/keyvault/<vault>/<secret> with a valid vault and secret name.");
                return;
            }
            string vault = parts[0], secret = parts[1];

            if (!AzureKeyVault.IsConfigured)
            {
                await Reply(context, 503, "not_configured", "AzureClientId is not set in the launcher settings.");
                return;
            }

            try
            {
                string value = await AzureKeyVault.Instance.GetSecretAsync(vault, secret);
                _logger?.Info($"Key Vault secret read: {vault}/{secret}");
                await ReplyJson(context, 200, new Dictionary<string, object> { ["value"] = value });
            }
            catch (AzureKeyVault.SignInRequiredException ex)
            {
                _logger?.Warn($"Key Vault secret {vault}/{secret}: {ex.Message}");
                await Reply(context, 401, "sign_in_required", ex.Message + " (Settings > Sign in to Azure...)");
            }
            catch (AzureKeyVault.KeyVaultException ex)
            {
                int status = ex.Status == HttpStatusCode.Forbidden ? 403
                    : ex.Status == HttpStatusCode.NotFound ? 404
                    : 502;
                string code = status == 403 ? "access_denied" : status == 404 ? "secret_not_found" : "key_vault_error";
                _logger?.Warn($"Key Vault secret {vault}/{secret}: {ex.Message}");
                await Reply(context, status, code, ex.Message);
            }
            catch (Exception ex)
            {
                _logger?.Error($"Key Vault secret {vault}/{secret} failed: {ex.Message}");
                await Reply(context, 502, "key_vault_error", ex.Message);
            }
        }

        // A script (MSXML, WinHTTP, curl...) sends no browser fetch metadata. A browser
        // always sends Sec-Fetch-Site (also "same-origin" for the launcher's own pages) and
        // Origin on cross-origin and non-GET requests.
        private static bool IsScriptRequest(HttpListenerRequest request)
        {
            return string.IsNullOrEmpty(request.Headers["Origin"]) &&
                string.IsNullOrEmpty(request.Headers["Sec-Fetch-Site"]) &&
                string.IsNullOrEmpty(request.Headers["Sec-Fetch-Mode"]);
        }

        private Task Reply(HttpListenerContext context, int status, string code, string message)
        {
            return ReplyJson(context, status, new Dictionary<string, object>
            {
                ["error"] = new Dictionary<string, object> { ["code"] = code, ["message"] = message }
            });
        }

        private Task ReplyJson(HttpListenerContext context, int status, object body)
        {
            context.Response.Headers["Cache-Control"] = "no-store";
            return Server.ServeResource(context, JsonCodec.Serialize(body), "application/json", status);
        }

        // Writes a new random token to %APPDATA%\WelsonJS\credentials.token, readable only by
        // the current user (inherited permissions removed). Returns null when it cannot be
        // written, which keeps the endpoint closed.
        private string CreateAccessTokenFile()
        {
            try
            {
                byte[] buffer = new byte[32];
                using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(buffer);
                string token = Convert.ToBase64String(buffer).TrimEnd('=').Replace('+', '-').Replace('/', '_');

                string path = AccessTokenPath;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, token, new UTF8Encoding(false));

                var security = new FileSecurity();
                security.SetAccessRuleProtection(true, false);
                security.AddAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().User,
                    FileSystemRights.FullControl, AccessControlType.Allow));
                File.SetAccessControl(path, security);
                return token;
            }
            catch (Exception ex)
            {
                _logger?.Error($"Could not write {AccessTokenPath}; the Key Vault endpoint stays closed: {ex.Message}");
                return null;
            }
        }

        private static bool FixedTimeEquals(string a, string b)
        {
            byte[] x = Encoding.UTF8.GetBytes(a), y = Encoding.UTF8.GetBytes(b);
            int diff = x.Length ^ y.Length;
            for (int i = 0; i < Math.Max(x.Length, y.Length); i++)
                diff |= (i < x.Length ? x[i] : 0) ^ (i < y.Length ? y[i] : 0);
            return diff == 0;
        }
    }
}
