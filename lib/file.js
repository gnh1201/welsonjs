//////////////////////////////////////////////////////////////////////////////////
//
//	file-lib.js
//
//	Common routines.  Defines LIB object which contains the API, as well as
//	a global DBG function.
//
/////////////////////////////////////////////////////////////////////////////////

var LIB = require('lib/std');

/////////////////////////////////////////////////////////////////////////////////
// Private APIs / Utility functions
/////////////////////////////////////////////////////////////////////////////////

var module = { global: global, require: global.require };
module.VERSIONINFO = "File Lib (file-libs.js) version 0.1";

/////////////////////////////////////////////////////////////////////////////////
// module.fileExists
/////////////////////////////////////////////////////////////////////////////////

module.fileExists = function(FN) {
  var FSO = module.CreateObject("Scripting.FileSystemObject");
  var exists = FSO.FileExists(FN);
  FSO = null;
  return exists;
};

/////////////////////////////////////////////////////////////////////////////////
// module.folderExists
/////////////////////////////////////////////////////////////////////////////////

module.folderExists = function(FN) {
  var FSO = module.CreateObject("Scripting.FileSystemObject");
  var exists = FSO.FolderExists(FN);
  FSO = null;
  return exists;
};

/////////////////////////////////////////////////////////////////////////////////
// module.fileGet
/////////////////////////////////////////////////////////////////////////////////

module.fileGet = function(FN) {
  var FSO = module.CreateObject("Scripting.FileSystemObject");
  var file = FSO.GetFile(FN);
  FSO = null;
  return file;
};

/////////////////////////////////////////////////////////////////////////////////
// module.readFile
//	Read the conents of the pass filename and return as a string
/////////////////////////////////////////////////////////////////////////////////

module.readFile = function(FN) {
  var FSO = module.CreateObject("Scripting.FileSystemObject");
  var T = null;
  try {
    var TS = FSO.OpenTextFile(FN,1);
    if (TS.AtEndOfStream) return "";
    T = TS.ReadAll();
    TS.Close();
    TS = null;
  } catch(e) {
    DBG("ERROR! " + e.number + ", " + e.description + ", FN=" + FN);
  }
  FSO = null;
  return T;
};

/////////////////////////////////////////////////////////////////////////////////
// module.writeFile
//	Write the passed content to named disk file
/////////////////////////////////////////////////////////////////////////////////

module.writeFile = function(FN, content, charset) {
  var ok;
  if (charset) {
    DBG("WRITE TO DISK USING ADODB.Stream CHARSET " + charset);
    try {
      var fsT = module.CreateObject("ADODB.Stream");
      fsT.Type = 2; 						// save as text/string data.
      fsT.Charset = charset;				// Specify charset For the source text data.
      fsT.Open();
      fsT.WriteText(content);
      fsT.SaveToFile(FN, 2);				// save as binary to disk
      ok = true;
    } catch(e) {
      DBG("ADODB.Stream: ERROR! " + e.number + ", " + e.description + ", FN=" + FN);
    }
  } else {
    DBG("WRITE TO DISK USING OpenTextFile CHARSET ascii");
    var FSO = module.CreateObject("Scripting.FileSystemObject");
    try {
      var TS = FSO.OpenTextFile(FN,2,true,0);					// ascii
      TS.Write(content);
      TS.Close();
      TS = null;
      ok = true;
    } catch(e) {
      DBG("OpenTextFile: ERROR! " + e.number + ", " + e.description + ", FN=" + FN);
    }
    FSO = null;
  }
  return ok;
};

/////////////////////////////////////////////////////////////////////////////////
// module.moveFile
/////////////////////////////////////////////////////////////////////////////////

module.moveFile = function(FROM, TO) {
  var FSO = module.CreateObject("Scripting.FileSystemObject");
  var res = FSO.MoveFile(FROM, TO);
  FSO = null;
  return res;
};

/////////////////////////////////////////////////////////////////////////////////
// module.createFolder
/////////////////////////////////////////////////////////////////////////////////

module.createFolder = function(FN) {
  var FSO = module.CreateObject("Scripting.FileSystemObject");
  var res = FSO.CreateFolder(FN);
  FSO = null;
  return res;
};

/////////////////////////////////////////////////////////////////////////////////

return module;
