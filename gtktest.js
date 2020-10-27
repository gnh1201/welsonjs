var GTK = require("lib/gtk");

function main() {
    GTK.init(function() {
        var win = new GTK.Window();
        win.show();
    });
    
    //GTK.wait(function() {});
}

exports.main = main;
