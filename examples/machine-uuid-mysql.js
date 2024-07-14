// machine-uuid-mysql.js
var SYS = require('lib/system');
var client = require('lib/catproxy');

/*
CREATE TABLE `machines` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `uuid` varchar(255) NOT NULL,
  `created_at` datetime NOT NULL DEFAULT current_timestamp(),
  `updated_at` datetime NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id`),
  UNIQUE KEY `uuid` (`uuid`)
)
*/

var worker = client.create("http://localhost");
worker.set_method("relay_mysql_query");
worker.set_env("mysql_username", "myusername");
worker.set_env("mysql_password", "mypassword");
worker.set_env("mysql_database", "mydatabase");

var machine_uuid = SYS.getUUID();

function register() {
   return worker.exec("insert machines (uuid) values ('" + machine_uuid + "')");
}

function main(args) {
    var result = register();
    var last_id = result.last_id;
    
    console.log("LAST_ID: " + last_id);
}

exports.main = main;
