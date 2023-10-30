////////////////////////////////////////////////////////////////////////
// Registry API
////////////////////////////////////////////////////////////////////////

var SHELL = require("lib/shell");

// http://apidock.com/ruby/Win32/Registry/Constants
var HKCR = 0x80000000;
var HKCU = 0x80000001;
var HKLM = 0x80000002;

var STRING = 0;
var BINARY = 1;
var DWORD = 2;
var QWORD = 3;

function getProvider(computer) {
    var computer = (typeof(computer) !== "undefined") ? computer : ".";
    var reg = GetObject("winmgmts:\\\\" + computer + "\\root\\default:StdRegProv");
    return reg;
}

function write(hKey, path, key, value, valType, computer) {
    var reg = getProvider(computer);

    reg.CreateKey(hKey, path);

    if (valType == STRING)
        reg.SetStringValue(hKey, path, key, value);
    else if (valType == DWORD)
        reg.SetDWORDValue(hKey, path, key, value);
    else if (valType == QWORD)
        reg.SetQWORDValue(hKey, path, key, value);
    else if (valType == BINARY)
        reg.SetBinaryValue(hKey, path, key, value);
}

function read(hKey, path, key, valType, computer) {
	/*
    var reg = getProvider(computer);

    var methodName = "";
    if (valType == STRING)
        methodName = "GetStringValue";
    else if (valType == DWORD)
        methodName = "GetDWORDValue";
    else if (valType == QWORD)
        methodName = "GetQWORDValue";
    else if (valType == BINARY)
        methodName = "GetBinaryValue";

    if (methodName == "")
		return;

    var method = reg.Methods_.Item(methodName);
    var inparams = method.InParameters.SpawnInstance_();

    inparams.hDefKey = hKey;
    inparams.sSubKeyName = path;
    inparams.sValueName = key;

	console.log(method.Name);
	console.log(JSON.stringify(inparams));

    var outparams = reg.ExecMethod_(method.Name, inparams);
    return outparams;
	*/

	// using IPC to read registry value
	var SHELL = require("lib/shell");
	var result = '';

	var hKeyStr = '';
	if (hKey == HKCR)
		hKeyStr = "HKCR";
	else if (hKey == HKCU)
		hKeyStr = "HKCU";
	else if (hKey == "HKLM")
		hKeyStr = "HKLM";
	
	if (hKeyStr != '') {
		result = (function(s) {
			s = s.substring(s.indexOf("REG_") + 4);
			s = s.substring(s.indexOf(' '));
			s = s.trim();
			return s;
		})(splitLn(SHELL.exec("reg query " + hKeyStr + "\\" + path))[2]);
	}

	return result;
}

function destroy(hKey, path, key, computer) {
    var reg = getProvider(computer);
    var loc = (key == "") ? path : path + "\\" + key;
    return reg.DeleteKey(hKey, loc);
}

/*
// DEPRECATED
function create(hiveKey, path, key, computer) {
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

function execFile(FN) {
    return SHELL.exec(["reg", "import", FN].join(' '));
}

exports.getProvider = getProvider;
exports.write = write;
exports.read = read;
exports.destroy = destroy;
//exports.create = create;   // DEPRECATED
exports.execFile = execFile;

exports.HKCR = HKCR;
exports.HKCU = HKCU;
exports.HKLM = HKLM;

exports.STRING = STRING;
exports.BINARY = BINARY;
exports.DWORD = DWORD;
exports.QWORD = QWORD;

exports.VERSIONINFO = "Registry Module (registry.js) version 0.2.1";
exports.global = global;
exports.require = global.require;