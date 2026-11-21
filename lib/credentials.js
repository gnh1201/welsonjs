// credentials.js
// Copyright 2019-2026, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
//
// Credential store. data/apikey.json maps a provider to its API key, written as
//   "file:<path>"                 the contents of a file (for example data/openai_apikey.txt),
//   "keyvault:<vault>/<secret>"   a secret of Azure Key Vault, read through WelsonJS.Launcher,
//   anything else                 the key itself.
//
// "keyvault:" values are read when get() first asks for them, then kept in memory. The
// launcher reads the secret with the Azure account signed in from its Settings menu
// (Sign in to Azure...) and answers GET /credentials/keyvault/<vault>/<secret> only to
// local scripts that send the access token it writes to %APPDATA%\WelsonJS\credentials.token
// at startup. When the launcher is not running, the account is not signed in or the
// secret cannot be read, get() returns null and logs why; the secret is never logged.
//
var FILE = require("lib/file");

var CREDENTIALS_DATA = [];

var FILE_PRELUDE = "file:";
var KEYVAULT_PRELUDE = "keyvault:";
var KEYVAULT_REFERENCE = /^([A-Za-z][A-Za-z0-9-]{1,22}[A-Za-z0-9])\/([A-Za-z0-9-]{1,127})$/;

var keyVaultOptions = {
    "launcherUrl": "http://localhost:3000",
    "timeout": 60000
};

function getTextFromFile(filename) {
    if (FILE.fileExists(filename)) {
        return FILE.readFile(filename, FILE.CdoCharset.CdoUTF_8);
    }

    return "";
}

function readFromFile(type, filename) {
    var data = JSON.parse(getTextFromFile(filename));

    for (var provider in data) {
        var s = String(data[provider]);

        if (s.indexOf(KEYVAULT_PRELUDE) == 0) {
            // Read from Key Vault when first needed (see get()).
            CREDENTIALS_DATA.push({
                "type": type,
                "provider": provider,
                "value": null,
                "keyvault": s.substring(KEYVAULT_PRELUDE.length)
            });
        } else if (s.indexOf(FILE_PRELUDE) == 0) {
            push(type, provider, getTextFromFile(s.substring(FILE_PRELUDE.length)));
        } else {
            push(type, provider, s);
        }
    }
}

function push(type, provider, value) {
    CREDENTIALS_DATA.push({
        "type": type,
        "provider": provider,
        "value": value
    });
}

function get(type, provider, index) {
    var index = index || 0;
    var matches = CREDENTIALS_DATA.reduce(function(a, x) {
        if (x.type == type && x.provider == provider) {
            a.push(x);
        }

        return a;
    }, []);

    if (matches.length - 1 < index) {
        return null;
    }

    var entry = matches[index];
    if (entry.keyvault && entry.value === null) {
        // Failures are not kept, so a later call tries again (for example after sign-in).
        entry.value = readFromKeyVault(entry.keyvault);
    }

    return entry.value;
}

// The launcher's per-start access token for /credentials/keyvault/.
function getLauncherAccessToken() {
    var appData = CreateObject("WScript.Shell").ExpandEnvironmentStrings("%APPDATA%");
    var path = appData + "\\WelsonJS\\credentials.token";
    var token = getTextFromFile(path);
    return token ? String(token).replace(/^\s+|\s+$/g, "") : "";
}

// Reads "<vault>/<secret>" through the launcher. Returns the value, or null after
// logging the reason.
function readFromKeyVault(reference) {
    var match = KEYVAULT_REFERENCE.exec(reference);
    if (!match) {
        console.error("credentials: invalid Key Vault reference \"keyvault:" + reference + "\" (use keyvault:<vault>/<secret>)");
        return null;
    }

    var token = getLauncherAccessToken();
    if (!token) {
        console.error("credentials: cannot read keyvault:" + reference + ": WelsonJS.Launcher is not running (no %APPDATA%\\WelsonJS\\credentials.token)");
        return null;
    }

    var url = String(keyVaultOptions.launcherUrl).replace(/\/+$/, "") +
        "/credentials/keyvault/" + match[1] + "/" + match[2];
    var status = 0, body = "";
    try {
        var xhr = CreateObject("MSXML2.ServerXMLHTTP.6.0");
        // resolve, connect, send, receive (ms): a sign-in may take the launcher a while.
        xhr.setTimeouts(5000, 5000, 10000, keyVaultOptions.timeout);
        xhr.open("GET", url, false);
        xhr.setRequestHeader("X-WelsonJS-Credentials-Token", token);
        xhr.send();
        status = xhr.status;
        body = String(xhr.responseText || "");
    } catch (e) {
        console.error("credentials: cannot read keyvault:" + reference + ": the launcher is not reachable at " + keyVaultOptions.launcherUrl + " (" + e.message + ")");
        return null;
    }

    var response = null;
    try {
        response = JSON.parse(body);
    } catch (e) {
        response = null;
    }

    if (status == 200 && response && typeof response.value === "string") {
        return response.value;
    }

    var message = (response && response.error && response.error.message) ? response.error.message : ("HTTP " + status);
    console.error("credentials: cannot read keyvault:" + reference + ": " + message);
    return null;
}

// Changes where and how long "keyvault:" values are requested:
// {"launcherUrl": "http://localhost:3000", "timeout": 60000}
function setKeyVaultOptions(options) {
    options = options || {};
    if (options.launcherUrl) keyVaultOptions.launcherUrl = String(options.launcherUrl);
    if (options.timeout) keyVaultOptions.timeout = Number(options.timeout);
}

readFromFile("apikey", "data/apikey.json");

exports.readFromFile = readFromFile;
exports.push = push;
exports.get = get;
exports.setKeyVaultOptions = setKeyVaultOptions;

exports.VERSIONINFO = "Credential store (credentials.js) version 0.2";
exports.AUTHOR = "gnh1201@catswords.re.kr";
exports.global = global;
exports.require = global.require;
