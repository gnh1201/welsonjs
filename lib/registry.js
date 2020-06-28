////////////////////////////////////////////////////////////////////////
// Registry API
////////////////////////////////////////////////////////////////////////

var module = { VERSIONINFO: "Registry Module (registry.js) version 0.1", global: global };

// http://apidock.com/ruby/Win32/Registry/Constants
module.HKCR = 0x80000000;
module.HKCU = 0x80000001;
module.HKLM = 0x80000002;

module.STRING = 0;
module.BINARY = 1;
module.DWORD = 2;
module.QWORD = 3;

module.provider = function(computer)
{
    var computer = (typeof(computer) !== "undefined") ? computer : ".";
    var reg = GetObject("winmgmts:\\\\" + computer + "\\root\\default:StdRegProv");
    return reg;
}

module.write = function(hKey, path, key, value, valType, computer)
{
    var reg = module.provider(computer);

    reg.CreateKey(hKey, path);

    if (valType == module.STRING)
        reg.SetStringValue(hKey, path, key, value);
    else if (valType == module.DWORD)
        reg.SetDWORDValue(hKey, path, key, value);
    else if (valType == module.QWORD)
        reg.SetQWORDValue(hKey, path, key, value);
    else if (valType == module.BINARY)
        reg.SetBinaryValue(hKey, path, key, value);
}

module.read = function(hKey, path, key, valType, computer)
{
    var reg = module.provider(computer);

    var methodName = "";
    if (valType == module.STRING)
        methodName = "GetStringValue";
    else if (valType == module.DWORD)
        methodName = "GetDWORDValue";
    else if (valType == module.QWORD)
        methodName = "GetQWORDValue";
    else if (valType == module.BINARY)
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

module.destroy = function(hKey, path, key, computer)
{
    var reg = module.provider(computer);
    var loc = (key == "") ? path : path + "\\" + key;
    return reg.DeleteKey(hKey, loc);
}

/*
// DEPRECATED
module.create = function(hiveKey, path, key, computer)
{
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

return module;
