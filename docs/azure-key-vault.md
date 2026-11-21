# API keys from Azure Key Vault

WelsonJS scripts can take their API keys from **Azure Key Vault** instead of key files. WelsonJS.Launcher signs in to Azure with your own work or school account and reads the secrets. Scripts then receive the values from the launcher on this PC. Scripts never handle Azure sign-in, tokens or the token cache.

```
WelsonJS script (lib/credentials.js, "keyvault:<vault>/<secret>" in data/apikey.json)
   └─ GET http://localhost:3000/credentials/keyvault/<vault>/<secret>   (local scripts of the same user only)
        └─ WelsonJS.Launcher (Settings > Sign in to Azure...)
             └─ GET https://<vault>.vault.azure.net/secrets/<secret>?api-version=7.4
```

## Setup

1. **Vault access.** Your account needs the role **Key Vault Secrets User** (read only) on the vault, which must use Azure RBAC. For example:

   ```
   az role assignment create --assignee <your account> --role "Key Vault Secrets User" --scope <vault resource ID>
   ```

2. **Sign in.** In the launcher, choose **Settings > Sign in to Azure...**.
   - The default browser opens the Microsoft sign-in page. Pick your account.
   - The first time, you may be asked to let **WelsonJS Launcher** access Azure Key Vault on your behalf. If your organization does not let users consent to apps, an administrator has to approve it once.
   - The menu then shows **Sign out of Azure (account)**. The sign-in is kept when the launcher restarts.

3. **Refer to secrets** in `data/apikey.json`, next to the other forms:

   ```json
   {
       "openai": "keyvault:my-vault/openai-api-key",
       "anthropic": "file:data/anthropic_apikey.txt",
       "serpapi": "the key itself"
   }
   ```

   Scripts keep using `CRED.get("apikey", "openai")` from `lib/credentials.js`, as the provider modules (`lib/chatgpt.js`, `lib/anthropic.js`, ...) already do.

## Launcher settings

The settings are in `WelsonJS.Launcher.exe.config`:

| Setting | Use |
| --- | --- |
| `AzureClientId` | The Microsoft Entra ID app the launcher signs in with. The default, `5bd2e397-314a-4fc9-88db-4e2b2452fb50`, is the multi-tenant **WelsonJS Launcher** app of Catswords Research: everyone signs in with their own organization's account. You may register your own app instead: a public client with the redirect URI `http://localhost` and the delegated permission Azure Key Vault `user_impersonation`. |
| `AzureTenant` | The sign-in tenant. Empty means `organizations` (any work or school account). The tenant of each vault is found automatically, so this is only needed to restrict sign-in to one tenant. |

## How it works

### Sign-in and tokens (launcher)

- **Sign-in:** authorization code flow with PKCE, in the default browser, with a loopback redirect (`http://localhost:<free port>/`). The browser has 5 minutes to finish.
- **Refresh token:** kept in `%APPDATA%\WelsonJS\azure-signin.dat`, encrypted with DPAPI for the current Windows user. Only the launcher reads it. Sign out removes it.
- **Vault tenant:** found from the vault's `401` challenge (`WWW-Authenticate: Bearer authorization="https://login.microsoftonline.com/<tenant>"`). The refresh token is then redeemed at that tenant for a Key Vault token (`https://vault.azure.net/.default`).
- **Access tokens:** kept in memory until five minutes before they expire. On a `401` from Key Vault, a new one is taken once.
- **Secret values:** kept in the launcher's memory for 15 minutes, never on disk.
- **Dependencies:** none added. The OAuth requests are plain HTTP, JSON uses the launcher's `JsonCodec`, and DPAPI is part of .NET Framework.

### The local endpoint (launcher)

`GET /credentials/keyvault/<vault>/<secret>` answers `{"value": "..."}`. Because it hands out secrets, it is stricter than the launcher's other APIs:

- **Callers:** only local callers are accepted (loopback address and `Host`), as for the other APIs.
- **Browsers:** requests with browser headers (`Origin`, `Sec-Fetch-Site`, `Sec-Fetch-Mode`) are refused, so neither a web page nor the launcher's own editor page can read secrets.
- **Same user only:** at every start, the launcher writes a new random token to `%APPDATA%\WelsonJS\credentials.token`, readable only by the current user (inherited permissions removed). Callers send it in `X-WelsonJS-Credentials-Token`, so other users and service accounts of the PC are refused. The launcher's API key (`ResourceServerApiKey`) does not open this endpoint.
- **Responses:**

  | Status | Meaning |
  | --- | --- |
  | `200` | The value. |
  | `400` | Invalid vault or secret name. |
  | `401` | Sign-in required (`"code": "sign_in_required"`). |
  | `403` | The caller was refused, or the account has no access to the vault (`access_denied`). |
  | `404` | No such secret. |
  | `502` | Key Vault or the sign-in service failed. |
  | `503` | `AzureClientId` is not set. |

  Errors use `{"error": {"code", "message"}}`. Responses carry `Cache-Control: no-store`.
- **Logging:** the vault, the secret name and the outcome only, never values or tokens.

### The script side (`lib/credentials.js`)

- A `keyvault:` value is read when `CRED.get()` first asks for that provider, then kept in memory. Scripts that do not use Key Vault never contact the launcher.
- The request is `MSXML2.ServerXMLHTTP.6.0` to `http://localhost:3000`, with the token from `credentials.token`. `CRED.setKeyVaultOptions({"launcherUrl": ..., "timeout": ...})` changes the address and the receive timeout (60 seconds by default).
- On failure `get()` returns `null` and logs the reason:
  - the launcher is not running;
  - the launcher is not reachable;
  - sign-in is required;
  - access is denied;
  - the secret is not found;
  - the reference is invalid.

  Failures are not cached, so a later call tries again, for example after you sign in.

## Limits

- **The launcher must be running.** Scripts run with `cscript` while the launcher is closed, or by WelsonJS.Service under another account, get `null` for `keyvault:` values. Keep using `file:` there.
- **Same-user scope.** Any program that runs as your Windows user can read `credentials.token`, and so can ask the launcher for secrets, just as it could read key files. Put only the keys that the PC needs in the vault you use there.
- **Sign-in method.** Only browser sign-in with a work or school account. Device code sign-in, service principals and managed identities are not supported yet (see [#571](https://github.com/gnh1201/welsonjs/issues/571)).
