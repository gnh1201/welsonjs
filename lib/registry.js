////////////////////////////////////////////////////////////////////////
// Registry API
////////////////////////////////////////////////////////////////////////
var scope = {
    VERSIONINFO: "Registry Module (registry.js) version 0.1",
    global: global,
	require: global.require
};

// http://apidock.com/ruby/Win32/Registry/Constants
scope.HKCR = 0x80000000;
scope.HKCU = 0x80000001;
scope.HKLM = 0x80000002;

scope.STRING = 0;
scope.BINARY = 1;
scope.DWORD = 2;
scope.QWORD = 3;

scope.provider = function(computer) {
    var computer = (typeof(computer) !== "undefined") ? computer : ".";
    var reg = GetObject("winmgmts:\\\\" + computer + "\\root\\default:StdRegProv");
    return reg;
}

scope.write = function(hKey, path, key, value, valType, computer) {
    var reg = scope.provider(computer);

    reg.CreateKey(hKey, path);

    if (valType == scope.STRING)
        reg.SetStringValue(hKey, path, key, value);
    else if (valType == scope.DWORD)
        reg.SetDWORDValue(hKey, path, key, value);
    else if (valType == scope.QWORD)
        reg.SetQWORDValue(hKey, path, key, value);
    else if (valType == scope.BINARY)
        reg.SetBinaryValue(hKey, path, key, value);
}

scope.read = function(hKey, path, key, valType, computer) {
    var reg = scope.provider(computer);

    var methodName = "";
    if (valType == scope.STRING)
        methodName = "GetStringValue";
    else if (valType == scope.DWORD)
        methodName = "GetDWORDValue";
    else if (valType == scope.QWORD)
        methodName = "GetQWORDValue";
    else if (valType == scope.BINARY)
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

scope.destroy = function(hKey, path, key, computer) {
    var reg = scope.provider(computer);
    var loc = (key == "") ? path : path + "\\" + key;
    return reg.DeleteKey(hKey, loc);
}

// DEPRECATED
scope.create = function(hiveKey, path, key, computer) {
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

scope.import = function(scriptName) {
    var arguments = [];

    arguments.push("reg");
    arguments.push("import");
    arguments.push(scriptName + ".reg");

    return SHELL.exec(arguments.join(' '));
};

return scope;
