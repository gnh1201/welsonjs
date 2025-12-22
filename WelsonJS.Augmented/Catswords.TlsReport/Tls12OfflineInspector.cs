// Tls12OfflineInspector.cs (single file)
// Offline-only TLS 1.2 readiness inspector for Windows.
// - No network probing / no actual TLS handshake.
// - Produces structured report with Pass/Info/Warn/Fail items.
//
// Example:
//   var report = Tls12OfflineInspector.Evaluate();
//   Console.WriteLine(report.ToText());

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Security.Principal;
using Microsoft.Win32;

namespace Catswords.TlsReport
{
    public static class Tls12OfflineInspector
    {
        // ----------------------------
        // Public entry point
        // ----------------------------

        public static Report Evaluate()
        {
            var ctx = new Context();
            var items = new List<Item>();
            if (!IsAdministrator())
                items.Add(Warn("Administrator privileges", "Not running as Administrator; some registry checks may not be accessible."));

            // 0) Environment / runtime surface
            items.Add(CheckOsVersion(ctx));
            items.Add(CheckProcessBitness(ctx));
            items.Add(CheckTls12EnumAvailable(ctx));
            items.Add(CheckSslProtocolsTls12Available(ctx));

            // 1) OS crypto DLL presence (sanity)
            items.Add(CheckSystemDllPresence(ctx, "schannel.dll"));
            items.Add(CheckSystemDllPresence(ctx, "bcrypt.dll"));
            items.Add(CheckSystemDllPresence(ctx, "crypt32.dll"));
            items.Add(CheckSystemDllPresence(ctx, "ncrypt.dll"));

            // 2) Schannel protocol policy
            items.Add(CheckSchannelTls12KeyPresence(ctx));
            items.Add(CheckSchannelTls12ClientPolicy(ctx));
            items.Add(CheckSchannelTls12ServerPolicy(ctx));
            items.Add(CheckLegacyProtocolKeyHints(ctx));

            // 3) WinHTTP defaults / legacy guidance
            items.Add(CheckWinHttpDefaultSecureProtocols(ctx));
            items.Add(CheckWinHttpDefaultSecureProtocolsIncludesTls12(ctx));

            // 4) .NET Framework policy keys
            items.Add(CheckDotNetStrongCrypto(ctx));
            items.Add(CheckDotNetSystemDefaultTlsVersions(ctx));
            items.Add(CheckDotNetV2StrongCrypto(ctx));
            items.Add(CheckDotNetV2SystemDefaultTlsVersions(ctx));

            // 5) Process-level legacy setting
            items.Add(CheckServicePointManagerProtocol(ctx));

            // 6) Cipher suite policy / crypto hardening
            items.Add(CheckCipherSuitePolicyKey(ctx));
            items.Add(CheckCipherSuitePolicyFunctions(ctx));
            items.Add(CheckSchannelHashesAndKeyExchangeExplicitDisables(ctx));
            items.Add(CheckSchannelStrongCiphersExplicitDisableHeuristic(ctx));

            // 7) FIPS policy
            items.Add(CheckFipsPolicy(ctx));

            // 8) Proxy hints
            items.Add(CheckWinHttpProxyPresence(ctx));
            items.Add(CheckWinInetUserProxyPresence(ctx));

            // 9) Schannel logging (diagnostics)
            items.Add(CheckSchannelEventLogging(ctx));

            return BuildReport(items);
        }

        // ----------------------------
        // Context
        // ----------------------------

        private sealed class Context
        {
            public Version OsVersion = Environment.OSVersion.Version;
            public bool Is64BitProcess = (IntPtr.Size == 8);
            public string SystemDir = Environment.SystemDirectory.TrimEnd('\\');
        }

        // ----------------------------
        // Check factories: Pass/Info/Warn/Fail
        // ----------------------------

        private static Item Pass(string id, string detail) => new Item(id, Level.Pass, detail);
        private static Item Info(string id, string detail) => new Item(id, Level.Info, detail);
        private static Item Warn(string id, string detail) => new Item(id, Level.Warn, detail);
        private static Item Fail(string id, string detail) => new Item(id, Level.Fail, detail);

        // ----------------------------
        // Checks: Environment / runtime
        // ----------------------------

        private static Item CheckOsVersion(Context ctx)
        {
            string hint = GetWindowsNameHint(ctx.OsVersion);

            if (ctx.OsVersion.Major >= 10)
                return Info("OS Version", "Windows " + hint + " (" + ctx.OsVersion + "). TLS 1.2 is generally available by default.");

            if (ctx.OsVersion.Major == 6 && ctx.OsVersion.Minor >= 3)
                return Info("OS Version", "Windows " + hint + " (" + ctx.OsVersion + "). TLS 1.2 exists, but legacy WinHTTP/.NET policy may require updates/registry.");

            return Warn("OS Version", "Windows " + hint + " (" + ctx.OsVersion + "). TLS 1.2 readiness is uncertain on older OS.");
        }

        private static Item CheckProcessBitness(Context ctx)
        {
            return Info("Process Bitness", ctx.Is64BitProcess ? "64-bit process" : "32-bit process");
        }

        private static Item CheckTls12EnumAvailable(Context ctx)
        {
            try
            {
                var _ = SecurityProtocolType.Tls12;
                return Pass("Runtime Enum: SecurityProtocolType.Tls12", "Available.");
            }
            catch
            {
                return Fail("Runtime Enum: SecurityProtocolType.Tls12", "Not available. Runtime is likely too old.");
            }
        }

        private static Item CheckSslProtocolsTls12Available(Context ctx)
        {
            try
            {
                var _ = SslProtocols.Tls12;
                return Pass("Runtime Enum: SslProtocols.Tls12", "Available.");
            }
            catch
            {
                return Fail("Runtime Enum: SslProtocols.Tls12", "Not available. Runtime is likely too old.");
            }
        }

        // ----------------------------
        // Checks: DLL presence
        // ----------------------------

        private static Item CheckSystemDllPresence(Context ctx, string dll)
        {
            string path = ctx.SystemDir + "\\" + dll;
            bool exists;
            try { exists = System.IO.File.Exists(path); }
            catch { exists = false; }

            if (exists) return Pass("System DLL present: " + dll, path);
            return Fail("System DLL present: " + dll, "Missing: " + path);
        }

        // ----------------------------
        // Checks: Schannel protocol policy
        // ----------------------------

        private static Item CheckSchannelTls12KeyPresence(Context ctx)
        {
            const string basePath = @"SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.2";
            bool clientExists = RegistryKeyExists(Registry.LocalMachine, basePath + @"\Client");
            bool serverExists = RegistryKeyExists(Registry.LocalMachine, basePath + @"\Server");

            if (clientExists || serverExists)
                return Info("Schannel TLS 1.2 keys present", "Client=" + clientExists + ", Server=" + serverExists + " (absence is normal on modern Windows).");

            return Info("Schannel TLS 1.2 keys present", "No explicit TLS 1.2 keys found (common on modern Windows; defaults may still enable TLS 1.2).");
        }

        private static Item CheckSchannelTls12ClientPolicy(Context ctx)
        {
            const string k = @"SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.2\Client";
            var enabled = ReadDwordHKLM(k, "Enabled");
            var disabledByDefault = ReadDwordHKLM(k, "DisabledByDefault");

            if (enabled == null && disabledByDefault == null)
                return Info("Schannel TLS 1.2 Client policy", "No explicit policy (common on modern Windows).");

            if (enabled == 0 || disabledByDefault == 1)
                return Fail("Schannel TLS 1.2 Client policy", "TLS 1.2 Client appears disabled (Enabled=0 or DisabledByDefault=1).");

            if (enabled == 1 && (disabledByDefault == null || disabledByDefault == 0))
                return Pass("Schannel TLS 1.2 Client policy", "Enabled=1 and DisabledByDefault=0 (or missing).");

            return Warn("Schannel TLS 1.2 Client policy", "Ambiguous: Enabled=" + ToS(enabled) + ", DisabledByDefault=" + ToS(disabledByDefault) + ".");
        }

        private static Item CheckSchannelTls12ServerPolicy(Context ctx)
        {
            const string k = @"SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.2\Server";
            var enabled = ReadDwordHKLM(k, "Enabled");
            var disabledByDefault = ReadDwordHKLM(k, "DisabledByDefault");

            if (enabled == null && disabledByDefault == null)
                return Info("Schannel TLS 1.2 Server policy", "No explicit policy (common on modern Windows).");

            if (enabled == 0 || disabledByDefault == 1)
                return Warn("Schannel TLS 1.2 Server policy", "TLS 1.2 Server appears disabled (may be intentional if not a server).");

            if (enabled == 1 && (disabledByDefault == null || disabledByDefault == 0))
                return Pass("Schannel TLS 1.2 Server policy", "Enabled=1 and DisabledByDefault=0 (or missing).");

            return Warn("Schannel TLS 1.2 Server policy", "Ambiguous: Enabled=" + ToS(enabled) + ", DisabledByDefault=" + ToS(disabledByDefault) + ".");
        }

        private static Item CheckLegacyProtocolKeyHints(Context ctx)
        {
            bool tls12ClientKey = RegistryKeyExists(Registry.LocalMachine,
                @"SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.2\Client");

            bool tls10ClientKey = RegistryKeyExists(Registry.LocalMachine,
                @"SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.0\Client");
            bool tls11ClientKey = RegistryKeyExists(Registry.LocalMachine,
                @"SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.1\Client");
            bool ssl3ClientKey = RegistryKeyExists(Registry.LocalMachine,
                @"SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\SSL 3.0\Client");

            if (!tls12ClientKey && (tls10ClientKey || tls11ClientKey || ssl3ClientKey))
                return Warn("Legacy protocol policy hints", "Found SSL3/TLS1.0/TLS1.1 policy keys while TLS1.2 client key is absent. Review hardening baseline.");

            return Info("Legacy protocol policy hints", "No strong legacy-protocol policy hints detected (presence/absence is not definitive).");
        }

        // ----------------------------
        // Checks: WinHTTP defaults
        // ----------------------------

        private static Item CheckWinHttpDefaultSecureProtocols(Context ctx)
        {
            const string k64 = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Internet Settings\WinHttp";
            const string k32 = @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Internet Settings\WinHttp";

            var v64 = ReadDwordHKLM(k64, "DefaultSecureProtocols");
            var v32 = ReadDwordHKLM(k32, "DefaultSecureProtocols");

            if (v64 == null && v32 == null)
                return Info("WinHTTP DefaultSecureProtocols", "Not set (normal on modern Windows; may be needed on legacy OS like Win7/2012).");

            return Info("WinHTTP DefaultSecureProtocols", "x64=" + ToHex(v64) + ", x86=" + ToHex(v32));
        }

        private static Item CheckWinHttpDefaultSecureProtocolsIncludesTls12(Context ctx)
        {
            const string k64 = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Internet Settings\WinHttp";
            const string k32 = @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Internet Settings\WinHttp";

            var v64 = ReadDwordHKLM(k64, "DefaultSecureProtocols");
            var v32 = ReadDwordHKLM(k32, "DefaultSecureProtocols");

            if (v64 == null && v32 == null)
                return Info("WinHTTP DefaultSecureProtocols includes TLS 1.2", "DefaultSecureProtocols not present (unknown/default).");

            const int TLS12_FLAG = 0x00000800;
            bool ok64 = v64 != null && (v64.Value & TLS12_FLAG) == TLS12_FLAG;
            bool ok32 = v32 != null && (v32.Value & TLS12_FLAG) == TLS12_FLAG;

            if (ok64 || ok32)
                return Pass("WinHTTP DefaultSecureProtocols includes TLS 1.2", "Detected TLS 1.2 flag. x64=" + ToHex(v64) + ", x86=" + ToHex(v32));

            return Warn("WinHTTP DefaultSecureProtocols includes TLS 1.2", "TLS 1.2 flag not detected. x64=" + ToHex(v64) + ", x86=" + ToHex(v32));
        }

        // ----------------------------
        // Checks: .NET Framework policy keys
        // ----------------------------

        private static Item CheckDotNetStrongCrypto(Context ctx)
        {
            const string k64 = @"SOFTWARE\Microsoft\.NETFramework\v4.0.30319";
            const string k32 = @"SOFTWARE\Wow6432Node\Microsoft\.NETFramework\v4.0.30319";

            var v64 = ReadDwordHKLM(k64, "SchUseStrongCrypto");
            var v32 = ReadDwordHKLM(k32, "SchUseStrongCrypto");

            if (v64 == 1 || v32 == 1)
                return Pass(".NET SchUseStrongCrypto", "Enabled. x64=" + ToS(v64) + ", x86=" + ToS(v32));

            if (v64 == null && v32 == null)
                return Info(".NET SchUseStrongCrypto", "Not explicitly set (common on modern OS/.NET).");

            return Warn(".NET SchUseStrongCrypto", "Not set to 1. x64=" + ToS(v64) + ", x86=" + ToS(v32));
        }

        private static Item CheckDotNetSystemDefaultTlsVersions(Context ctx)
        {
            const string k64 = @"SOFTWARE\Microsoft\.NETFramework\v4.0.30319";
            const string k32 = @"SOFTWARE\Wow6432Node\Microsoft\.NETFramework\v4.0.30319";

            var v64 = ReadDwordHKLM(k64, "SystemDefaultTlsVersions");
            var v32 = ReadDwordHKLM(k32, "SystemDefaultTlsVersions");

            if (v64 == 1 || v32 == 1)
                return Pass(".NET SystemDefaultTlsVersions", "Enabled. x64=" + ToS(v64) + ", x86=" + ToS(v32));

            if (v64 == null && v32 == null)
                return Info(".NET SystemDefaultTlsVersions", "Not explicitly set (common on modern OS/.NET).");

            return Warn(".NET SystemDefaultTlsVersions", "Not set to 1. x64=" + ToS(v64) + ", x86=" + ToS(v32));
        }

        private static Item CheckDotNetV2StrongCrypto(Context ctx)
        {
            // .NET 2.0/3.0/3.5 line uses v2.0.50727.
            // On many systems, these values might not exist; treat as Info/Warn.
            const string k64 = @"SOFTWARE\Microsoft\.NETFramework\v2.0.50727";
            const string k32 = @"SOFTWARE\Wow6432Node\Microsoft\.NETFramework\v2.0.50727";

            var v64 = ReadDwordHKLM(k64, "SchUseStrongCrypto");
            var v32 = ReadDwordHKLM(k32, "SchUseStrongCrypto");

            if (v64 == 1 || v32 == 1)
                return Pass(".NET(v2.0.50727) SchUseStrongCrypto", "Enabled. x64=" + ToS(v64) + ", x86=" + ToS(v32));

            if (v64 == null && v32 == null)
            {
                // On many modern OS, this isn't set. For legacy .NET apps, warn (because default can be TLS 1.0).
                return Info(".NET(v2.0.50727) SchUseStrongCrypto", "Not explicitly set (common). Legacy .NET 2.0/3.5 apps may still default to older protocols.");
            }

            return Warn(".NET(v2.0.50727) SchUseStrongCrypto", "Not set to 1. x64=" + ToS(v64) + ", x86=" + ToS(v32));
        }

        private static Item CheckDotNetV2SystemDefaultTlsVersions(Context ctx)
        {
            // .NET 2.0/3.0/3.5 line uses v2.0.50727.
            const string k64 = @"SOFTWARE\Microsoft\.NETFramework\v2.0.50727";
            const string k32 = @"SOFTWARE\Wow6432Node\Microsoft\.NETFramework\v2.0.50727";

            var v64 = ReadDwordHKLM(k64, "SystemDefaultTlsVersions");
            var v32 = ReadDwordHKLM(k32, "SystemDefaultTlsVersions");

            if (v64 == 1 || v32 == 1)
                return Pass(".NET(v2.0.50727) SystemDefaultTlsVersions", "Enabled. x64=" + ToS(v64) + ", x86=" + ToS(v32));

            if (v64 == null && v32 == null)
            {
                // Same rationale: not always present, but matters for legacy CLR apps.
                return Info(".NET(v2.0.50727) SystemDefaultTlsVersions", "Not explicitly set (common). Legacy .NET 2.0/3.5 apps may not follow OS default TLS without updates/keys.");
            }

            return Warn(".NET(v2.0.50727) SystemDefaultTlsVersions", "Not set to 1. x64=" + ToS(v64) + ", x86=" + ToS(v32));
        }

        // ----------------------------
        // Checks: Process-level (legacy)
        // ----------------------------

        private static Item CheckServicePointManagerProtocol(Context ctx)
        {
            try
            {
                var current = ServicePointManager.SecurityProtocol;
                bool hasTls12 = (current & SecurityProtocolType.Tls12) == SecurityProtocolType.Tls12;

                if (hasTls12)
                    return Pass("ServicePointManager.SecurityProtocol includes TLS 1.2", current.ToString());

                return Info("ServicePointManager.SecurityProtocol includes TLS 1.2",
                    current.ToString() + " (process-level; can be set at runtime for legacy stacks).");
            }
            catch (Exception ex)
            {
                return Warn("ServicePointManager.SecurityProtocol includes TLS 1.2",
                    "Could not read: " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        // ----------------------------
        // Checks: Cipher suite policy / crypto hardening
        // ----------------------------

        private static Item CheckCipherSuitePolicyKey(Context ctx)
        {
            const string k = @"SOFTWARE\Policies\Microsoft\Cryptography\Configuration\SSL\00010002";
            bool exists = RegistryKeyExists(Registry.LocalMachine, k);

            if (!exists)
                return Info("Cipher suite policy key", "No explicit policy key found (default cipher suite selection applies).");

            return Info("Cipher suite policy key", "Policy key exists (cipher suites may be explicitly controlled).");
        }

        private static Item CheckCipherSuitePolicyFunctions(Context ctx)
        {
            const string k = @"SOFTWARE\Policies\Microsoft\Cryptography\Configuration\SSL\00010002";
            if (!RegistryKeyExists(Registry.LocalMachine, k))
                return Info("Cipher suite policy Functions", "Policy key not present.");

            var funcs = ReadMultiStringHKLM(k, "Functions");
            if (funcs == null)
                return Fail("Cipher suite policy Functions", "Policy key exists, but Functions value is missing, unreadable, or not REG_MULTI_SZ. This can block TLS handshakes.");
            if (funcs.Length == 0)
                return Fail("Cipher suite policy Functions", "Functions list is empty. This can block TLS handshakes.");
            return Info("Cipher suite policy Functions", "Functions count=" + funcs.Length);
        }

        private static Item CheckSchannelHashesAndKeyExchangeExplicitDisables(Context ctx)
        {
            var checks = new (string key, string label)[]
            {
            (@"SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Hashes\SHA", "SHA"),
            (@"SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Hashes\SHA256", "SHA256"),
            (@"SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Hashes\SHA384", "SHA384"),
            (@"SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\KeyExchangeAlgorithms\PKCS", "RSA (PKCS)"),
            (@"SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\KeyExchangeAlgorithms\ECDH", "ECDH"),
            (@"SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\KeyExchangeAlgorithms\Diffie-Hellman", "DH"),
            };

            var disabled = new List<string>();
            foreach (var c in checks)
            {
                var enabled = ReadDwordHKLM(c.key, "Enabled");
                if (enabled == 0) disabled.Add(c.label);
            }

            if (disabled.Count > 0)
                return Warn("Schannel Hash/KeyExchange disables", "Explicitly disabled: " + string.Join(", ", disabled));

            return Info("Schannel Hash/KeyExchange disables", "No explicit disables detected for common Hash/KeyExchange components.");
        }

        private static Item CheckSchannelStrongCiphersExplicitDisableHeuristic(Context ctx)
        {
            var aesKeys = new[]
            {
            @"SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Ciphers\AES 128/128",
            @"SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Ciphers\AES 256/256",
        };

            var disabled = new List<string>();
            foreach (var k in aesKeys)
            {
                var enabled = ReadDwordHKLM(k, "Enabled");
                if (enabled == 0) disabled.Add(k.Split('\\').Last());
            }

            if (disabled.Count > 0)
                return Warn("Schannel AES ciphers explicitly disabled", "Disabled: " + string.Join(", ", disabled));

            return Info("Schannel AES ciphers explicitly disabled", "No explicit AES cipher disable detected (absence is normal).");
        }

        // ----------------------------
        // Checks: FIPS policy
        // ----------------------------

        private static Item CheckFipsPolicy(Context ctx)
        {
            const string k = @"SYSTEM\CurrentControlSet\Control\Lsa\FipsAlgorithmPolicy";
            var enabled = ReadDwordHKLM(k, "Enabled");

            if (enabled == 1)
                return Warn("FIPS policy", "Enabled=1. Crypto/TLS behavior may change depending on libraries and cipher suite policies.");
            if (enabled == 0)
                return Info("FIPS policy", "Enabled=0.");
            return Info("FIPS policy", "Not set (unknown/default).");
        }

        // ----------------------------
        // Checks: Proxy hints
        // ----------------------------

        private static Item CheckWinHttpProxyPresence(Context ctx)
        {
            const string k = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Internet Settings\Connections";
            var bytes = ReadBinaryHKLM(k, "WinHttpSettings");

            if (bytes == null || bytes.Length == 0)
                return Info("WinHTTP proxy presence", "WinHttpSettings not found (no evidence of WinHTTP proxy config).");

            return Info("WinHTTP proxy presence", "WinHttpSettings exists (proxy may be configured; can affect certs/endpoints).");
        }

        private static Item CheckWinInetUserProxyPresence(Context ctx)
        {
            const string k = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";
            var proxyEnable = ReadDwordHKCU(k, "ProxyEnable");
            var proxyServer = ReadStringHKCU(k, "ProxyServer");

            if (proxyEnable == 1 && !string.IsNullOrEmpty(proxyServer))
                return Info("WinINet user proxy", "ProxyEnable=1, ProxyServer=" + proxyServer);

            return Info("WinINet user proxy", "No obvious per-user proxy configuration detected.");
        }

        // ----------------------------
        // Checks: Schannel event logging
        // ----------------------------

        private static Item CheckSchannelEventLogging(Context ctx)
        {
            const string k = @"SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL";
            var v = ReadDwordHKLM(k, "EventLogging");

            if (v == null)
                return Info("Schannel EventLogging", "Not set (default).");

            return Info("Schannel EventLogging", "EventLogging=" + v + " (enables Schannel logs in Event Viewer).");
        }

        // ----------------------------
        // Report build
        // ----------------------------

        private static Report BuildReport(List<Item> items)
        {
            bool tls12ClientDisabled = items.Any(i => i.Id == "Schannel TLS 1.2 Client policy" && i.Level == Level.Fail);
            bool missingCryptoDll = items.Any(i => i.Id.StartsWith("System DLL present: ", StringComparison.OrdinalIgnoreCase) && i.Level == Level.Fail);
            bool cipherPolicyEmpty = items.Any(i => i.Id == "Cipher suite policy Functions" && i.Level == Level.Fail);

            bool anyWarn = items.Any(i => i.Level == Level.Warn);

            Readiness readiness;
            if (tls12ClientDisabled || missingCryptoDll || cipherPolicyEmpty)
                readiness = Readiness.NotReady;
            else if (anyWarn)
                readiness = Readiness.ProbablyReadyButRisky;
            else
                readiness = Readiness.ProbablyReady;

            var rec = BuildRecommendations(items, readiness);
            return new Report(readiness, rec, items);
        }

        private static string[] BuildRecommendations(List<Item> items, Readiness readiness)
        {
            var rec = new List<string>();

            bool tls12ClientDisabled = items.Any(i => i.Id == "Schannel TLS 1.2 Client policy" && i.Level == Level.Fail);
            bool winhttpTls12FlagWarn = items.Any(i => i.Id == "WinHTTP DefaultSecureProtocols includes TLS 1.2" && i.Level == Level.Warn);
            bool dotnetWarn =
                items.Any(i => i.Id == ".NET SchUseStrongCrypto" && i.Level == Level.Warn) ||
                items.Any(i => i.Id == ".NET SystemDefaultTlsVersions" && i.Level == Level.Warn);
            bool cipherPolicyEmpty = items.Any(i => i.Id == "Cipher suite policy Functions" && i.Level == Level.Fail);
            bool aesDisabled = items.Any(i => i.Id == "Schannel AES ciphers explicitly disabled" && i.Level == Level.Warn);
            bool fipsEnabled = items.Any(i => i.Id == "FIPS policy" && i.Level == Level.Warn);

            if (tls12ClientDisabled)
                rec.Add("TLS 1.2 is explicitly disabled in Schannel Client policy. Enable it (Enabled=1, DisabledByDefault=0) or remove the disabling baseline.");

            if (cipherPolicyEmpty)
                rec.Add("Cipher suite policy exists but Functions list is empty. Populate cipher suites or remove the policy; otherwise TLS handshakes will fail.");

            if (winhttpTls12FlagWarn)
                rec.Add("WinHTTP DefaultSecureProtocols does not show TLS 1.2 flag. On legacy OS, apply KB3140245 guidance and include TLS 1.2 in DefaultSecureProtocols.");

            if (dotnetWarn)
                rec.Add("For legacy .NET Framework apps, consider setting SchUseStrongCrypto=1 and SystemDefaultTlsVersions=1 under .NETFramework\\v4.0.30319 (both 64/32-bit).");

            if (aesDisabled)
                rec.Add("AES cipher(s) are explicitly disabled in Schannel. Modern TLS 1.2 endpoints typically require AES; re-enable unless you have a specific policy reason.");

            if (fipsEnabled)
                rec.Add("FIPS policy is enabled. Ensure your cipher suite policy and TLS stacks remain compatible with FIPS constraints.");

            if (rec.Count == 0)
            {
                if (readiness == Readiness.ProbablyReady)
                    rec.Add("No offline red flags detected. TLS 1.2 is probably supported by OS/runtime policy.");
                else
                    rec.Add("Some offline hints suggest risk. If failures occur, review Schannel policy, cipher suite policy, WinHTTP defaults, and .NET strong crypto settings.");
            }

            return rec.ToArray();
        }

        // ----------------------------
        // Models
        // ----------------------------

        public enum Level { Pass, Info, Warn, Fail }
        public enum Readiness { ProbablyReady, ProbablyReadyButRisky, NotReady }

        public sealed class Item
        {
            public string Id { get; }
            public Level Level { get; }
            public string Detail { get; }

            public Item(string id, Level level, string detail)
            {
                Id = id ?? "";
                Level = level;
                Detail = detail ?? "";
            }

            public override string ToString() => "[" + Level + "] " + Id + " - " + Detail;
        }

        public sealed class Report
        {
            public Readiness Readiness { get; }
            public IReadOnlyList<string> Recommendations { get; }
            public IReadOnlyList<Item> Items { get; }

            public Report(Readiness readiness, IReadOnlyList<string> recommendations, IReadOnlyList<Item> items)
            {
                Readiness = readiness;
                Recommendations = recommendations ?? Array.Empty<string>();
                Items = items ?? Array.Empty<Item>();
            }

            public string ToText()
            {
                var lines = new List<string>();
                lines.Add("TLS 1.2 Offline Readiness: " + Readiness);
                lines.Add("");

                if (Recommendations.Count > 0)
                {
                    lines.Add("Recommendations:");
                    for (int i = 0; i < Recommendations.Count; i++)
                        lines.Add("  - " + Recommendations[i]);
                    lines.Add("");
                }

                lines.Add("Checks:");
                foreach (var item in Items)
                    lines.Add("  " + item.ToString());

                return string.Join(Environment.NewLine, lines.ToArray());
            }
        }

        // ----------------------------
        // Registry helpers
        // ----------------------------

        private static bool RegistryKeyExists(RegistryKey root, string subKeyPath)
        {
            try { using (var k = root.OpenSubKey(subKeyPath, false)) return k != null; }
            catch { return false; }
        }

        private static int? ReadDwordHKLM(string subKeyPath, string valueName)
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(subKeyPath, false))
                {
                    if (key == null) return null;
                    object v = key.GetValue(valueName, null);
                    if (v == null) return null;
                    if (v is int i) return i;
                    if (v is long l) return (int)l; // Handle 64-bit to 32-bit conversion
                    if (v is byte[] b && b.Length >= 4) return BitConverter.ToInt32(b, 0);
                    return null;
                }
            }
            catch { return null; }
        }

        private static string[] ReadMultiStringHKLM(string subKeyPath, string valueName)
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(subKeyPath, false))
                {
                    if (key == null) return null;
                    return key.GetValue(valueName, null) as string[];
                }
            }
            catch { return null; }
        }

        private static byte[] ReadBinaryHKLM(string subKeyPath, string valueName)
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(subKeyPath, false))
                {
                    if (key == null) return null;
                    return key.GetValue(valueName, null) as byte[];
                }
            }
            catch { return null; }
        }

        private static int? ReadDwordHKCU(string subKeyPath, string valueName)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(subKeyPath, false))
                {
                    if (key == null) return null;
                    object v = key.GetValue(valueName, null);
                    if (v == null) return null;
                    if (v is int i) return i;
                    if (v is byte[] b && b.Length >= 4) return BitConverter.ToInt32(b, 0);
                    return null;
                }
            }
            catch { return null; }
        }

        private static string ReadStringHKCU(string subKeyPath, string valueName)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(subKeyPath, false))
                {
                    if (key == null) return null;
                    return key.GetValue(valueName, null) as string;
                }
            }
            catch { return null; }
        }

        // ----------------------------
        // Formatting helpers
        // ----------------------------

        private static string ToS(int? v) => v.HasValue ? v.Value.ToString() : "null";
        private static string ToHex(int? v) => v.HasValue ? ("0x" + v.Value.ToString("X")) : "null";

        private static string GetWindowsNameHint(Version v)
        {
            if (v.Major >= 10) return "10/11+";
            if (v.Major == 6 && v.Minor == 3) return "8.1/2012 R2";
            if (v.Major == 6 && v.Minor == 2) return "8/2012";
            if (v.Major == 6 && v.Minor == 1) return "7/2008 R2";
            if (v.Major == 6 && v.Minor == 0) return "Vista/2008";
            return "Unknown";
        }

        // ----------------------------
        // Optional: Admin check (extension point)
        // ----------------------------

        public static bool IsAdministrator()
        {
            try
            {
                using (var identity = WindowsIdentity.GetCurrent())
                {
                    var principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch
            {
                return false;
            }
        }
    }
}