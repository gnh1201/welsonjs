// AzureKeyVault.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace WelsonJS.Launcher
{
    /// <summary>
    /// Reads secrets from Azure Key Vault on behalf of WelsonJS scripts (see
    /// ApiEndpoints.KeyVault), with the user's own Microsoft Entra ID account.
    ///
    /// Sign-in (SignInAsync, from Settings > Sign in to Azure...) uses the authorization
    /// code flow with PKCE in the default browser and a loopback redirect
    /// (http://localhost:&lt;port&gt;/), against the "organizations" authority unless
    /// AzureTenant names one, so the user only picks an account. The refresh token is kept
    /// in %APPDATA%\WelsonJS\azure-signin.dat, encrypted with DPAPI for the current user;
    /// only the launcher reads it. Access tokens and secret values stay in memory.
    ///
    /// The tenant of a vault is not configured: an unauthenticated request to the vault
    /// answers 401 with "WWW-Authenticate: Bearer authorization=https://login.microsoftonline.com/&lt;tenant&gt;",
    /// and the refresh token is redeemed at that tenant for a Key Vault token.
    ///
    /// No NuGet packages: the OAuth requests are plain HTTP, JSON goes through JsonCodec,
    /// and DPAPI is part of .NET Framework (System.Security).
    /// </summary>
    public sealed class AzureKeyVault
    {
        private const string LoginHost = "https://login.microsoftonline.com";
        private const string VaultScope = "https://vault.azure.net/.default";
        private const string ApiVersion = "7.4";
        private static readonly TimeSpan SecretLifetime = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan SignInTimeout = TimeSpan.FromMinutes(5);
        private static readonly Regex VaultNamePattern = new Regex("^[A-Za-z][A-Za-z0-9-]{1,22}[A-Za-z0-9]$");
        private static readonly Regex SecretNamePattern = new Regex("^[A-Za-z0-9-]{1,127}$");
        private static readonly Regex AuthorityPattern = new Regex(
            "authorization(?:_uri)?=\"?https://login\\.(?:microsoftonline\\.com|windows\\.net)/([A-Za-z0-9.-]+)",
            RegexOptions.IgnoreCase);

        private static readonly Lazy<AzureKeyVault> _instance = new Lazy<AzureKeyVault>(() => new AzureKeyVault(new TraceLogger(typeof(AzureKeyVault))));
        public static AzureKeyVault Instance => _instance.Value;

        private readonly ILogger _logger;
        private readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        private readonly SemaphoreSlim _tokenLock = new SemaphoreSlim(1, 1);
        private readonly object _sync = new object();
        private readonly Dictionary<string, CachedValue> _accessTokens = new Dictionary<string, CachedValue>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, CachedValue> _secrets = new Dictionary<string, CachedValue>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _vaultTenants = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private SignInState _state;

        private sealed class CachedValue
        {
            public string Value;
            public DateTime ExpiresUtc;
        }

        private sealed class SignInState
        {
            public string RefreshToken;
            public string Account;
            public string HomeTenant;
        }

        /// <summary>Thrown when no account is signed in, or the sign-in has expired or was revoked.</summary>
        public sealed class SignInRequiredException : Exception
        {
            public SignInRequiredException(string message) : base(message) { }
        }

        /// <summary>Thrown when Key Vault refuses the request, with its HTTP status.</summary>
        public sealed class KeyVaultException : Exception
        {
            public HttpStatusCode Status { get; }
            public KeyVaultException(HttpStatusCode status, string message) : base(message) { Status = status; }
        }

        private AzureKeyVault(ILogger logger)
        {
            _logger = logger;
            _state = LoadState();
        }

        private static string StatePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WelsonJS", "azure-signin.dat");

        private static string ClientId => (Program.GetAppConfig("AzureClientId") ?? "").Trim();

        private static string SignInTenant
        {
            get
            {
                string tenant = (Program.GetAppConfig("AzureTenant") ?? "").Trim();
                return string.IsNullOrEmpty(tenant) ? "organizations" : tenant;
            }
        }

        /// <summary>True when an Entra ID app registration (AzureClientId) is configured.</summary>
        public static bool IsConfigured => !string.IsNullOrEmpty(ClientId);

        /// <summary>The signed-in account (user principal name), or null.</summary>
        public string Account
        {
            get { lock (_sync) return _state?.Account; }
        }

        // -------------------- sign-in --------------------

        /// <summary>
        /// Signs in with the default browser (authorization code with PKCE and a loopback
        /// redirect) and keeps the refresh token. Returns the signed-in account.
        /// </summary>
        public async Task<string> SignInAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!IsConfigured)
                throw new InvalidOperationException("AzureClientId is not set in the launcher settings.");

            string verifier = RandomUrlSafe(32);
            string challenge = Base64Url(Sha256(Encoding.ASCII.GetBytes(verifier)));
            string expectedState = RandomUrlSafe(16);
            int port = LocalPorts.FindAvailable();
            string redirectUri = $"http://localhost:{port}/";

            using (var listener = new HttpListener())
            {
                listener.Prefixes.Add(redirectUri);
                listener.Start();

                string authorizeUrl = $"{LoginHost}/{Uri.EscapeDataString(SignInTenant)}/oauth2/v2.0/authorize" +
                    $"?client_id={Uri.EscapeDataString(ClientId)}" +
                    "&response_type=code" +
                    $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                    "&response_mode=query" +
                    $"&scope={Uri.EscapeDataString(VaultScope + " offline_access openid profile")}" +
                    $"&code_challenge={challenge}&code_challenge_method=S256" +
                    $"&state={expectedState}" +
                    "&prompt=select_account";
                Process.Start(new ProcessStartInfo { FileName = authorizeUrl, UseShellExecute = true });

                Task<HttpListenerContext> contextTask = listener.GetContextAsync();
                Task done = await Task.WhenAny(contextTask, Task.Delay(SignInTimeout, cancellationToken));
                if (done != contextTask)
                    throw new TimeoutException("The browser sign-in did not finish in time.");

                HttpListenerContext context = contextTask.Result;
                var query = context.Request.QueryString;
                string error = query["error"];
                string code = query["code"];
                bool stateOk = string.Equals(query["state"], expectedState, StringComparison.Ordinal);
                await WriteBrowserPage(context, error == null && code != null && stateOk
                    ? "Signed in to Azure. You can close this window and return to WelsonJS."
                    : "The Azure sign-in did not complete. You can close this window.");

                if (!stateOk)
                    throw new InvalidOperationException("The sign-in response did not match the request (state).");
                if (error != null)
                    throw new InvalidOperationException($"Sign-in failed: {error}: {query["error_description"]}");
                if (string.IsNullOrEmpty(code))
                    throw new InvalidOperationException("Sign-in returned no authorization code.");

                var form = new Dictionary<string, string>
                {
                    ["client_id"] = ClientId,
                    ["grant_type"] = "authorization_code",
                    ["code"] = code,
                    ["redirect_uri"] = redirectUri,
                    ["code_verifier"] = verifier,
                    ["scope"] = VaultScope + " offline_access openid profile"
                };
                Dictionary<string, object> token = await PostTokenAsync(SignInTenant, form);

                string refreshToken = token.TryGetValue("refresh_token", out object rt) ? rt as string : null;
                if (string.IsNullOrEmpty(refreshToken))
                    throw new InvalidOperationException("Sign-in returned no refresh token (offline_access).");

                ReadIdToken(token.TryGetValue("id_token", out object idt) ? idt as string : null, out string account, out string homeTenant);
                var state = new SignInState { RefreshToken = refreshToken, Account = account, HomeTenant = homeTenant };
                lock (_sync)
                {
                    _state = state;
                    _accessTokens.Clear();
                    _secrets.Clear();
                    if (!string.IsNullOrEmpty(homeTenant))
                        StoreAccessToken(homeTenant, token);
                }
                SaveState(state);
                _logger?.Info($"Signed in to Azure as {account ?? "(unknown account)"}.");
                return account;
            }
        }

        /// <summary>Forgets the signed-in account, its tokens and the cached secrets.</summary>
        public void SignOut()
        {
            lock (_sync)
            {
                _state = null;
                _accessTokens.Clear();
                _secrets.Clear();
            }
            try
            {
                if (File.Exists(StatePath)) File.Delete(StatePath);
            }
            catch (Exception ex)
            {
                _logger?.Warn($"Could not delete the Azure sign-in file: {ex.Message}");
            }
            _logger?.Info("Signed out of Azure.");
        }

        // -------------------- secrets --------------------

        public static bool IsValidVaultName(string name) => name != null && VaultNamePattern.IsMatch(name);

        public static bool IsValidSecretName(string name) => name != null && SecretNamePattern.IsMatch(name);

        /// <summary>
        /// Returns the current value of a secret, from memory when it was read within the
        /// last 15 minutes. Throws SignInRequiredException or KeyVaultException.
        /// </summary>
        public async Task<string> GetSecretAsync(string vault, string name)
        {
            if (!IsValidVaultName(vault)) throw new ArgumentException("Invalid vault name.", nameof(vault));
            if (!IsValidSecretName(name)) throw new ArgumentException("Invalid secret name.", nameof(name));

            string cacheKey = vault + "/" + name;
            lock (_sync)
            {
                if (_state == null)
                    throw new SignInRequiredException("Not signed in to Azure.");
                if (_secrets.TryGetValue(cacheKey, out CachedValue cached) && cached.ExpiresUtc > DateTime.UtcNow)
                    return cached.Value;
            }

            string url = $"https://{vault}.vault.azure.net/secrets/{name}?api-version={ApiVersion}";
            string tenant = await GetVaultTenantAsync(vault, url);

            for (int attempt = 0; ; attempt++)
            {
                string accessToken = await GetAccessTokenAsync(tenant);
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                    using (HttpResponseMessage response = await _http.SendAsync(request))
                    {
                        string body = await response.Content.ReadAsStringAsync();
                        if (response.StatusCode == HttpStatusCode.Unauthorized && attempt == 0)
                        {
                            // The token may have expired or been revoked early: get a new one once.
                            lock (_sync) _accessTokens.Remove(tenant);
                            continue;
                        }
                        if (!response.IsSuccessStatusCode)
                            throw new KeyVaultException(response.StatusCode, DescribeKeyVaultError(response.StatusCode, body));

                        Dictionary<string, object> json = JsonCodec.DeserializeObject(body);
                        string value = json.TryGetValue("value", out object v) ? v as string : null;
                        if (value == null)
                            throw new KeyVaultException(HttpStatusCode.BadGateway, "Key Vault returned no value.");

                        lock (_sync)
                            _secrets[cacheKey] = new CachedValue { Value = value, ExpiresUtc = DateTime.UtcNow + SecretLifetime };
                        return value;
                    }
                }
            }
        }

        // The tenant that issues tokens for a vault, from its 401 challenge (cached per vault).
        private async Task<string> GetVaultTenantAsync(string vault, string url)
        {
            lock (_sync)
            {
                if (_vaultTenants.TryGetValue(vault, out string known)) return known;
            }

            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            using (HttpResponseMessage response = await _http.SendAsync(request))
            {
                string challenge = string.Join(", ", response.Headers.WwwAuthenticate);
                Match match = AuthorityPattern.Match(challenge);
                if (response.StatusCode != HttpStatusCode.Unauthorized || !match.Success)
                    throw new KeyVaultException(HttpStatusCode.BadGateway,
                        $"Could not find the tenant of the vault {vault} (HTTP {(int)response.StatusCode}).");

                string tenant = match.Groups[1].Value;
                lock (_sync) _vaultTenants[vault] = tenant;
                return tenant;
            }
        }

        // A Key Vault access token for a tenant, redeemed from the refresh token when needed.
        private async Task<string> GetAccessTokenAsync(string tenant)
        {
            await _tokenLock.WaitAsync();
            try
            {
                SignInState state;
                lock (_sync)
                {
                    if (_accessTokens.TryGetValue(tenant, out CachedValue cached) && cached.ExpiresUtc > DateTime.UtcNow)
                        return cached.Value;
                    state = _state;
                }
                if (state == null)
                    throw new SignInRequiredException("Not signed in to Azure.");

                var form = new Dictionary<string, string>
                {
                    ["client_id"] = ClientId,
                    ["grant_type"] = "refresh_token",
                    ["refresh_token"] = state.RefreshToken,
                    ["scope"] = VaultScope + " offline_access"
                };

                Dictionary<string, object> token;
                try
                {
                    token = await PostTokenAsync(tenant, form);
                }
                catch (TokenRequestException ex) when (ex.Error == "invalid_grant" || ex.Error == "interaction_required")
                {
                    throw new SignInRequiredException("The Azure sign-in has expired or needs to be renewed. Sign in again from Settings.");
                }

                string rotated = token.TryGetValue("refresh_token", out object rt) ? rt as string : null;
                string accessToken;
                lock (_sync)
                {
                    accessToken = StoreAccessToken(tenant, token);
                    if (!string.IsNullOrEmpty(rotated) && _state != null && rotated != _state.RefreshToken)
                    {
                        _state.RefreshToken = rotated;
                        state = _state;
                    }
                    else
                    {
                        state = null;
                    }
                }
                if (state != null) SaveState(state);
                return accessToken;
            }
            finally
            {
                _tokenLock.Release();
            }
        }

        // Caches an access token until five minutes before it expires. Call under _sync.
        private string StoreAccessToken(string tenant, Dictionary<string, object> token)
        {
            string accessToken = token.TryGetValue("access_token", out object at) ? at as string : null;
            if (string.IsNullOrEmpty(accessToken))
                throw new InvalidOperationException("The token response has no access token.");

            double lifetime = 3600;
            if (token.TryGetValue("expires_in", out object ei) && ei != null)
                double.TryParse(Convert.ToString(ei, System.Globalization.CultureInfo.InvariantCulture),
                    System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out lifetime);

            _accessTokens[tenant] = new CachedValue
            {
                Value = accessToken,
                ExpiresUtc = DateTime.UtcNow.AddSeconds(Math.Max(60, lifetime - 300))
            };
            return accessToken;
        }

        private sealed class TokenRequestException : Exception
        {
            public string Error { get; }
            public TokenRequestException(string error, string message) : base(message) { Error = error; }
        }

        private async Task<Dictionary<string, object>> PostTokenAsync(string tenant, Dictionary<string, string> form)
        {
            string url = $"{LoginHost}/{Uri.EscapeDataString(tenant)}/oauth2/v2.0/token";
            using (var content = new FormUrlEncodedContent(form))
            using (HttpResponseMessage response = await _http.PostAsync(url, content))
            {
                string body = await response.Content.ReadAsStringAsync();
                Dictionary<string, object> json;
                try
                {
                    json = JsonCodec.DeserializeObject(body);
                }
                catch (FormatException)
                {
                    throw new TokenRequestException("invalid_response", $"The token endpoint answered HTTP {(int)response.StatusCode} without JSON.");
                }

                if (!response.IsSuccessStatusCode || json.ContainsKey("error"))
                {
                    string error = json.TryGetValue("error", out object e) ? e as string : "http_" + (int)response.StatusCode;
                    string description = json.TryGetValue("error_description", out object d) ? d as string : null;
                    // The first line of the description has the AADSTS code and reason; it carries no secret.
                    string reason = (description ?? "").Split('\n')[0].Trim();
                    _logger?.Warn($"Azure token request to tenant {tenant} failed: {error} {reason}");
                    throw new TokenRequestException(error, $"{error}: {reason}");
                }
                return json;
            }
        }

        private static string DescribeKeyVaultError(HttpStatusCode status, string body)
        {
            string code = null, message = null;
            try
            {
                Dictionary<string, object> json = JsonCodec.DeserializeObject(body);
                if (json.TryGetValue("error", out object e) && e is Dictionary<string, object> error)
                {
                    code = error.TryGetValue("code", out object c) ? c as string : null;
                    message = error.TryGetValue("message", out object m) ? m as string : null;
                }
            }
            catch (FormatException) { }

            return $"Key Vault answered HTTP {(int)status}" + (code != null ? $" ({code})" : "") + (message != null ? $": {message}" : ".");
        }

        // -------------------- helpers --------------------

        private static async Task WriteBrowserPage(HttpListenerContext context, string message)
        {
            string html = "<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>WelsonJS</title></head>" +
                "<body style=\"font-family:Segoe UI,sans-serif;margin:3em\"><p>" + WebUtility.HtmlEncode(message) + "</p></body></html>";
            byte[] data = Encoding.UTF8.GetBytes(html);
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.ContentLength64 = data.Length;
            context.Response.Headers["Cache-Control"] = "no-store";
            await context.Response.OutputStream.WriteAsync(data, 0, data.Length);
            context.Response.OutputStream.Close();
        }

        // The account and home tenant from the ID token. The token comes straight from the
        // token endpoint over TLS, so its claims are read without checking the signature.
        private static void ReadIdToken(string idToken, out string account, out string tenant)
        {
            account = null;
            tenant = null;
            if (string.IsNullOrEmpty(idToken)) return;
            string[] parts = idToken.Split('.');
            if (parts.Length < 2) return;
            try
            {
                string payload = parts[1].Replace('-', '+').Replace('_', '/');
                payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
                Dictionary<string, object> claims = JsonCodec.DeserializeObject(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
                account = (claims.TryGetValue("preferred_username", out object u) ? u as string : null)
                    ?? (claims.TryGetValue("upn", out object upn) ? upn as string : null);
                tenant = claims.TryGetValue("tid", out object tid) ? tid as string : null;
            }
            catch (Exception)
            {
                // The claims only label the sign-in; tokens still work without them.
            }
        }

        private SignInState LoadState()
        {
            try
            {
                if (!File.Exists(StatePath)) return null;
                byte[] plain = ProtectedData.Unprotect(File.ReadAllBytes(StatePath), null, DataProtectionScope.CurrentUser);
                Dictionary<string, object> json = JsonCodec.DeserializeObject(Encoding.UTF8.GetString(plain));
                string refreshToken = json.TryGetValue("refresh_token", out object rt) ? rt as string : null;
                if (string.IsNullOrEmpty(refreshToken)) return null;
                return new SignInState
                {
                    RefreshToken = refreshToken,
                    Account = json.TryGetValue("account", out object a) ? a as string : null,
                    HomeTenant = json.TryGetValue("tenant", out object t) ? t as string : null
                };
            }
            catch (Exception ex)
            {
                _logger?.Warn($"Could not read the Azure sign-in file; sign in again: {ex.Message}");
                return null;
            }
        }

        private void SaveState(SignInState state)
        {
            try
            {
                string json = JsonCodec.Serialize(new Dictionary<string, object>
                {
                    ["refresh_token"] = state.RefreshToken,
                    ["account"] = state.Account,
                    ["tenant"] = state.HomeTenant
                });
                byte[] protectedData = ProtectedData.Protect(Encoding.UTF8.GetBytes(json), null, DataProtectionScope.CurrentUser);
                Directory.CreateDirectory(Path.GetDirectoryName(StatePath));
                File.WriteAllBytes(StatePath, protectedData);
            }
            catch (Exception ex)
            {
                _logger?.Error($"Could not save the Azure sign-in: {ex.Message}");
            }
        }

        private static byte[] Sha256(byte[] data)
        {
            using (var sha = SHA256.Create()) return sha.ComputeHash(data);
        }

        private static string RandomUrlSafe(int bytes)
        {
            byte[] buffer = new byte[bytes];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(buffer);
            return Base64Url(buffer);
        }

        private static string Base64Url(byte[] data) =>
            Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
