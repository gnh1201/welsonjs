////////////////////////////////////////////////////////////////////////
// Example Database API
////////////////////////////////////////////////////////////////////////

var LIB = require("lib/std");

exports.VERSIONINFO = "Database Lib (db.js) version 1.0";
exports.global = global;
exports.require = global.require;

exports.open = function(cs) {
    var instance = {};

    // Create a database connection and open the database, setting isolation level
    // and timeouts
    var open = function(cs) {
        try {
            instance.Connection = LIB.CreateObject("ADODB.Connection");
        } catch (e) {
            console.log("Failed to create ADODB.Connection, error = " + e.number + ", " + e.description);
            return;
        }
        instance.ConnectionString = cs;
        instance.Connection.open(instance.ConnectionString);
        instance.Connection.IsolationLevel = 256; // Uncommitted Reads
        instance.Connection.CommandTimeout = 300; // 5 minute command timeout
    };

    // Open the database
    open(cs);

    // Close and re-open the database.
    instance.reopen = function() {
        instance.Connection.close();
        open(instance.ConnectionString);
    };

    // instance.Query
    //	Run a read only query on the database.  Returns a recordset
    instance.query = function(sql) {
        var RS = LIB.CreateObject("ADODB.Recordset");
        RS.LockType = 1; // adLockReadOnly
        RS.CursorType = 1; // adOpenKeySet
        console.log("> " + sql);
        RS.Open(sql, instance.Connection);
        return RS;
    };

    // instance.exec
    //	Run a statement (update or insert) and return true if successful
    instance.exec = function(sql) {
        console.log("> " + sql);
        var ok;
        try {
            ok = instance.Connection.Execute(sql);
        } catch (e) {
            ok = null;
            LIB.emailError(e, sql);
            instance.lastError = e;
        }
        return ok;
    };

    instance.insert = function(table, columns) {
        console.log("> insert into " + table + " some data!");
        var RS = LIB.CreateObject("ADODB.Recordset");
        RS.open(table, instance.Connection, 2, 3, 2);
        RS.addNew();
        for (var col in columns) {
            RS(col).value = columns[col] || null;
        }
        RS.Update();
        RS.moveLast();
        var id = RS(0).value;
        RS.close();
        return id;
    };

    instance.close = function() {};

    return instance;
}

exports.blob2Text = function(blobField, charset) {
    var stream = LIB.CreateObject("ADODB.Stream");
    stream.Charset = (charset || "us-ascii").replace(/;$/, "");
    stream.Type = 1;
    stream.Open();
    console.log("WRITE STREAM");
    stream.Write(blobField);
    console.log("DONE WRITE STREAM");
    stream.Position = 0;
    stream.Type = 2;
    var text = stream.ReadText(-1);
    stream.Close();
    return text;
};

exports.saveBlob = function(filename, blobField) {
    try {
        var stream = LIB.CreateObject("ADODB.Stream");
        stream.Type = 1;
        stream.Open();
        stream.Write(blobField);
        stream.Position = 0;
        stream.saveToFile(filename, 2);
        stream.Close();
        return true;
    } catch (e) {
        console.log("ERROR " + e.number + " saving blob: " + e.description);
    }
};

exports.quoteString = function(s) {
    return "'" + s.replace(/\'/g, "''") + "'";
};
