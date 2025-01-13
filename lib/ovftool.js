// ovftool.js
// Namhyeon Go <abuse@catswords.net>
// https://github.com/gnh1201/welsonjs
// 
var SHELL = require("lib/shell");
var CRED = require("lib/credentials");

function OVFObject() {
    this.binPath = "bin/x64/VMware-ovftool-4.6.3-24031167-win.x86_64/ovftool/ovftool.exe";
    this.hostname = "";
    this.resourceName = "";
    
    this.setBinPath = function(binPath) {
        this.binPath = binPath;
    };
	
	this.setHostname = function(hostname) {
		this.hostname = hostname;
	};
	
	this.setResourceName = function(resourceName) {
		this.resourceName = resourceName;
	};
   
    this.saveTo = function() {
        var cred = CRED.get("password", "ovftool");
        var connectionString = "vi://" +
			cred.username + ":" +
			encodeURIComponent(cred.password) + "@" +
			this.hostname + "/" + 
			this.resourceName;
		
		// todo
        
    };
}

function setCredential(username, password) {
    CRED.push("password", "ovftool", {
        "hostname": hostname,
        "username": username,
        "password": password,
    });
}

function create() {
    return new OVFObject();
}

exports.setCredential = setCredential;
exports.create = create;

exports.VERSIONINFO = "Broadcom/VMware OVF Tool interface (ovftool.js) version 0.1";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = global.require;
