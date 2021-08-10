var GTK = require("lib/gtk");

function main() {
    GTK.init(function() {
        var win, table, button, entry, text, radiogroup, radio1, radio2;

        // create new window
        win = new GTK.Window({
            title: "WelsonJS GTK GUI Demo Application",
            width: 450,
            height: 400
        });
        
        // create new table
        table = new GTK.Table({
            rows: 50,
            columns: 50
        });
        win.setContainer(table);

        // create new button
        button = new GTK.Button({
            text: "Exit"
        });
        button.addEventListener("click", function() {
            GTK.exit();
        });
        table.attach(button, 41, 49, 45, 49);

        // create new entry
        entry = new GTK.Entry();
        table.attach(entry, 1, 40, 45, 49);
        entry.addEventListener("enter", function(event) {
            console.info(event.target.getText());
        });

        // create new textbox
        text = new GTK.TextBox();
        table.attach(text, 1, 49, 8, 44);
        
        // create new radiogroup
        radiogroup = new GTK.RadioGroup();

        // create new radio (Radio 1)
        radio1 = new GTK.RadioBox({
            text: "Yes",
            group: radiogroup
        });
        table.attach(radio1, 1, 10, 1, 4);

        // create new radio (Radio 2)
        radio2 = new GTK.RadioBox({
            text: "No",
            group: radiogroup
        });
        table.attach(radio2, 1, 10, 4, 7);

        // showing window
        win.show();

        // focusing entry
        entry.focus();
    });

    GTK.wait();
}

exports.main = main;
