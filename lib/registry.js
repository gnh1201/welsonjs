////////////////////////////////////////////////////////////////////////
// Registry API
////////////////////////////////////////////////////////////////////////

var SHELL = require("lib/shell");

exports.VERSIONINFO = "Registry Module (registry.js) version 0.1";
exports.global = global;
exports.require = global.require;

// http://apidock.com/ruby/Win32/Registry/Constants
exports.HKCR = 0x80000000;
exports.HKCU = 0x80000001;
exports.HKLM = 0x80000002;

exports.STRING = 0;
exports.BINARY = 1;
exports.DWORD = 2;
exports.QWORD = 3;

exports.provider = function(computer) {
    var computer = (typeof(computer) !== "undefined") ? computer : ".";
    var reg = GetObject("winmgmts:\\\\" + computer + "\\root\\default:StdRegProv");
    return reg;
}

exports.write = function(hKey, path, key, value, valType, computer) {
    var reg = exports.provider(computer);

    reg.CreateKey(hKey, path);

    if (valType == exports.STRING)
        reg.SetStringValue(hKey, path, key, value);
    else if (valType == exports.DWORD)
        reg.SetDWORDValue(hKey, path, key, value);
    else if (valType == exports.QWORD)
        reg.SetQWORDValue(hKey, path, key, value);
    else if (valType == exports.BINARY)
        reg.SetBinaryValue(hKey, path, key, value);
}

exports.read = function(hKey, path, key, valType, computer) {
    var reg = exports.provider(computer);

    var methodName = "";
    if (valType == exports.STRING)
        methodName = "GetStringValue";
    else if (valType == exports.DWORD)
        methodName = "GetDWORDValue";
    else if (valType == exports.QWORD)
        methodName = "GetQWORDValue";
    else if (valType == exports.BINARY)
        methodName = "GetBinaryValue";

    if (methodName == "")
        return;

    var method = reg.Methods_.Item(methodName);
    var inparams = method.InParameters.SpawnInstance_();

    inparams.hDefKey = hKey;
    inparams.sSubKeyName = path;
    inparams.sValueName = key;

    var outparams = reg.ExecMethod_(method.Name, inparams);

    return outparams;
}

exports.destroy = function(hKey, path, key, computer) {
    var reg = exports.provider(computer);
    var loc = (key == "") ? path : path + "\\" + key;
    return reg.DeleteKey(hKey, loc);
}

/*
// DEPRECATED
exports.create = function(hiveKey, path, key, computer) {
	console.log("Warning! Registry.create method is DEPRECATED.");

    var computer = (typeof(computer) !== "undefined") ? computer : ".";
    var sw = new ActiveXObject("WbemScripting.SWbemLocator");
    var root = sw.ConnectServer(computer, "root\\default");
    var reg = root.get("StdRegProv");

    var enumKey = reg.Methods_.Item("EnumKey");

    var inParams = enumKey.InParameters.SpawnInstance_();
    inParams.hDefKey = hiveKey;
    inParams.sSubKeyName = path;

    var outParam = reg.ExecMethod_(enumKey.Name, inParams);

    if (outParam.ReturnValue != 0)
        return false;

    if (outParam.sNames)
    {
        var subKeys = outParam.sNames.toArray();

        for (var i = 0; i < subKeys.length; ++i)
        {
            if (subkeys[i].toUpperCase() == key.toUpperCase())
                return true;
        }
    }

    var createKey = reg.Methods_.Item("CreateKey");
    var createArgs = createKey.InParameters.SpawnInstance_();
    createArgs.hDefKey = hiveKey;
    createArgs.sSubKeyName = path + "\\" + key;

    var createRet = reg.ExecMethod_(createKey.Name, createArgs);
    return createRet.returnValue == 0;
}
*/

exports.importFromFile = function(fileName) {
    var arguments = [];

    arguments.push("reg");
    arguments.push("import");
    arguments.push(fileName);

    return SHELL.exec(arguments.join(' '));
};
