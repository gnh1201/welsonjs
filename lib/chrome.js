//////////////////////////////////////////////////////////////////////////////////
// Google Chrome API
/////////////////////////////////////////////////////////////////////////////////

var STD = require("lib/std");
var RAND = require("lib/rand");
var SHELL = require("lib/shell");
var SYS = require("lib/system");
var FILE = require("lib/file");
var HTTP = require("lib/http");
var Websocket = require("lib/websocket");
var AutoItX = require("lib/autoitx");

// for remote debugging
var pageEventId = 0;

var ChromeObject = function() {
    STD.EventableObject.apply(this, arguments);  // set this object to `eventable`
    
    this.workingDirectory = SYS.getEnvString("PROGRAMFILES") + "\\Google\\Chrome\\Application";
    this.binPath = SYS.getEnvString("PROGRAMFILES") + "\\Google\\:installedDir\\Application\\chrome.exe";
    //this.processID = 0;
    this.installedDir = "Chrome";
    this.profileName = "Default";
    this.userDataDir = null;

    // proxy
    this.isPreventProxy = false;
    this.proxy = {
        "protocol": "socks5",
        "host": "127.0.0.1",
        "port": 1080
    };
    this.inPrivate = false;

    // user agent
    this.userAgent = null;
    this.userAgents = [];

    // dependencies
    this.oAutoIt = null;

    // for remote debugging
    this.debuggingPort = 0;
    this.pageId = "";
    this.ws = Websocket.create();
    this.isAttached = false;
    this.pageList = [];
    this.title = "";
    this.frameIndex = -1;

    this.isPreventEvaluate = false;

    this.create = function() {
        this.oAutoIt = AutoItX.create().getInterface();
        return this;
    };

    this.setIsPreventEvaluate = function(flag) {
        this.isPreventEvaluate = true;
    };

    this.setBinPath = function(path) {
        this.binPath = path;
        return this;
    };

    this.setProfile = function(profileName, installedDir) {
        this.profileName = (profileName == "Default" ? "Chrome" : profileName);
        this.setInstalledDir(installedDir);
        this.workingDirectory = this.workingDirectory.replace(":installedDir", this.installedDir);
        this.binPath = this.binPath.replace(":installedDir", this.installedDir);
        //this.binPath = this.binPath.replace(":installedDir", "Chrome");
        return this;
    };

    this.clearProfile = function() {
        var FN = SHELL.getPathOfMyDocuments() + "\\UserData_Chrome_" + this.profileName + "\\" + this.profileName;
        while (FILE.folderExists(FN)) {
            try {
                return FILE.deleteFolder(FN);
            } catch (e) {
                console.warn("Can not clear the session! Resaon: " + e.message);
                console.warn("Retrying clear the profile...");
            }
        }
    };

    this.clear = function() {
        return this.clearProfile();
    };

    this.setUserDataDir = function(dirname) {
        if (dirname != null) {
            this.userDataDir = dirname;
        } else {
            this.userDataDir = SHELL.getPathOfMyDocuments() + "\\UserData_Chrome_" + this.profileName;
        }
        return this;
    };

    this.setInstalledDir = function(dirname) {
        if (dirname != null) {
            this.installedDir = dirname;
        }
        return this;
    };
    
    this.setProxy = function(obj) {
        this.proxy = obj;
        return this;
    };

    this.setProxyProtocol = function(s) {
        this.proxy.protocol = s;
        return this;
    };

    this.setProxyHost = function(s) {
        this.proxy.host = s;
        return this;
    };

    this.setProxyPort = function(port) {
        this.proxy.port = port;
        return this;
    };

    this.setIsPreventProxy = function(flag) {
        this.isPreventProxy = flag;
        return this;
    };

    this.setDebuggingPort = function(port) {
        this.debuggingPort = port;
        console.log("Enabled debugging port:", port);
        return this;
    };

    this.setFrameIndex = function(idx) {
        this.frameIndex = idx;
    };

    this.setPageId = function(s) {
        if (s == null) {
            var pageList = this.getPageList();
            if (pageList instanceof Array && pageList.length > 0) {
                this.pageId = pageList[0].id;
            }
        } else {
            this.pageId = s;
        }
    };

    this.createShoutcut = function(url) {
        if (!this.userDataDir) {
            this.userDataDir = SHELL.getPathOfMyDocuments() + "\\UserData_Chrome_" + this.profileName;
        }

        var cmd = [
            "cscript",
            "app.js",
            "shoutcut.legacy",
            "chrome",
            this.installedDir,
        ];

        // disable default browser check
        cmd.push("--no-default-browser-check");

        // disable popop blocking
        cmd.push("--disable-popup-blocking");

        // disable 3d
        cmd.push("--disable-3d-apis");

        // block non-proxyed webrtc traffic
        cmd.push("--force-webrtc-ip-handling-policy=disable-non-proxied-udp");

        // check incognito mode
        if (this.inPrivate) {
            cmd.push("--incognito");
        }

        // check debugging port
        if (this.debuggingPort > 0) {
            cmd.push("--remote-debugging-port=" + this.debuggingPort);
        }

        cmd.push("--profile-directory=\"" + this.profileName + "\"");
        cmd.push("--user-data-dir=\"" + this.userDataDir + "\"");
        cmd.push("\"" + url + "\"");

        SHELL.createShoutcut("Chrome Prototype (" + this.installedDir + ")", cmd.join(' '), SYS.getCurrentScriptDirectory());
    };

    this.setInPrivate = function(flag) {
        this.inPrivate = flag;
        return this;
    };

    this.setUserAgent = function(ua) {
        this.userAgent = ua;
        return this;
    };

    this.addUserAgent = function(ua) {
        this.userAgents.push(ua);
        return this;
    };

    this.addUserAgentsFromFile = function(filename) {
        var text = FILE.readFile(filename, "utf-8");
        var lines = text.split(/\r?\n/);
        for (var i = 0; i < lines.length; i++) {
            var line = lines[i].trim();
            if (line !== "") {
                this.userAgents.push(line);
            }
        }
        return this;
    };

    this.open = function(url) {
        this.setProfile(this.profileName, this.installedDir);

        // if the file does not exists, Check the 32bit installation folder again
        if (!FILE.fileExists(this.binPath)) {
            this.workingDirectory = SYS.getEnvString("PROGRAMFILES(X86)") + "\\Google\\Chrome\\Application";
            this.binPath = SYS.getEnvString("PROGRAMFILES(X86)") + "\\Google\\:installedDir\\Application\\chrome.exe";
            this.setProfile(this.profileName, this.installedDir);
        }

        // find profile
        if (!FILE.fileExists(this.binPath)) {
            console.error("ChromeObject.open() -> '" + this.profileName + "' profile does not exists. You have to create it.");
            return this;
        }

        // create shoutcut to desktop
        if (this.debuggingPort == 0) {
            this.createShoutcut();
        }

        /*
        var process;
        while (this.processID == 0) {
            try {
                if (!this.userDataDir) {
                    this.userDataDir = SHELL.getPathOfMyDocuments() + "\\UserData_Chrome_" + this.profileName;
                }
                var shell = SHELL.create().setWorkingDirectory(this.workingDirectory);
                var process = shell.createProcess([
                    "\"" + this.binPath + "\"",
                    "--profile-directory=\"" + this.profileName + "\"",
                    "--proxy-server=\"socks5://127.0.0.1:" + this.proxyPort + "\"",
                    "--user-data-dir=\"" + this.userDataDir + "\"",
                    "\"" + url + "\""
                ].join(" "));
                sleep(1500);
                this.processID = process.ProcessID;
                sleep(1500);
                shell.release();
            } catch (e) {
                console.error("ChromeObject.open() ->", e.message);
                sleep(1500);
            }
        }
        */

        try {
            // get user data directory
            if (!this.userDataDir) {
                this.userDataDir = SHELL.getPathOfMyDocuments() + "\\UserData_Chrome_" + this.profileName;
            }

            // connect to shell
            var shell = SHELL.create();
            shell.setWorkingDirectory(this.workingDirectory);

            // initialize
            var cmd = [];

            // enable inPrivate (incognito) mode
            if (this.inPrivate) {
                cmd.push("--incognito");
            }

            // enable debugging port
            if (this.debuggingPort > 0) {
                cmd.push("--remote-debugging-port=" + this.debuggingPort);
            }

            // disable default browser check
            cmd.push("--no-default-browser-check");

            // disable popop blocking
            cmd.push("--disable-popup-blocking");

            // disable 3D
            cmd.push("--disable-3d-apis");

            // block non-proxyed webrtc traffic
            cmd.push("--force-webrtc-ip-handling-policy=disable-non-proxied-udp");

            // set profile directory
            cmd.push("--profile-directory=\"" + this.profileName + "\"");

            // set proxy configuration
            if (this.proxy != null && this.isPreventProxy != true) {
                console.log("Enabled proxy server:", this.proxy.protocol + "://" + this.proxy.host + ":" + this.proxy.port);
                cmd.push("--proxy-server=\"" + this.proxy.protocol + "://" + this.proxy.host + ":" + this.proxy.port + "\"");
            }

            // set user data directory
            cmd.push("--user-data-dir=\"" + this.userDataDir + "\"");

            // choice user agent
            if (this.userAgents.length > 0) {
                this.setUserAgent(RAND.one(this.userAgents));
            }

            // set user agent
            if (this.userAgent != null) {
                cmd.push("--user-agent=\"" + this.userAgent + "\"");
            }

            // set URL
            cmd.push("\"" + url + "\"");
            console.log(cmd.join(" "));

            // run
            shell.runAs(this.binPath, cmd);
            sleep(300);

            // release shell
            shell.release();
        } catch (e) {
            console.error("ChromeObject.open() -> ", e.message);
            sleep(300);
        }

        return this;
    };
    
    this.getPageList = function() {
        var pageList = [];

        if (this.debuggingPort > 0) {
            try {
                var responseText = HTTP.get("http://127.0.0.1:" + this.debuggingPort + "/json");
                //console.info(responseText);
                pageList = JSON.parse(responseText);
                this.pageList = pageList;
                return pageList;
            } catch (e) {
                console.error("ChromeObject.getPageList() ->", e.message);
                return this.getPageList();
            }
        } else {
            console.error("Remote debugging unavailable");
            return pageList;
        }
    };

    this.getPageById = function(id) {
        return this.getPageList().find(function(x) {
            return (x.id == id);
        });
    };

    this.getPagesByUrl = function(url) {
        return this.getPageList().filter(function(x) {
            return (x.url.indexOf(url) == 0);
        });
    };

    this.getPagesByUrls = function(urls) {
        return this.getPageList().reduce(function(acc, x) {
            for (var i = 0; i < urls.length; i++) {
                if (x.url.indexOf(urls[i]) == 0) {
                    acc.push(x);
                }
            }
            return acc;
        }, []);
    };

    this.getPagesByTitle = function(title) {
        return this.getPageList().filter(function(x) {
            return (x.title.indexOf(title) == 0);
        });
    };

    this.getPagesByTitles = function(titles) {
        return this.getPageList().reduce(function(acc, x) {
            for (var i = 0; i < titles.length; i++) {
                if (x.title.indexOf(titles[i]) == 0) {
                    acc.push(x);
                }
            }
            return acc;
        }, []);
    };

    this.getParameterValue = function(paramName, defaultValue) {
        var paramValue = defaultValue;
        var page = this.getPageById(this.pageId);
        var params = page.url.split('&');

        for (var i = 0; i < params.length; i++) {
            if (params[i].indexOf(paramName + '=') == 0) {
                paramValue = params[i].substring((paramName + '=').length);
                //break;  // Do not break because will be get the last value
            }
        }

        return paramValue;
    };

    this.sendPageRPC = function(method, params) {
        var result = null;

        try {
            if (this.pageId != "") {
                result = this.ws.send("ws://127.0.0.1:" + this.debuggingPort + "/devtools/page/" + this.pageId, JSON.stringify({
                    "id": pageEventId,
                    "method": method,
                    "params": params
                }));
                pageEventId++;
                console.info("ChromeObject().sendPageRPC() -> Sent");
            } else {
                this.setPageId(null);
                if (this.pageId != "") {
                    result = this.sendPageRPC(method, params);
                } else {
                    console.error("Page not found");
                }
            }
        } catch (e) {
            console.log("ChromeObject.sendPageRPC() ->", e.message);
        }

        return result;
    };

    this.navigate = function(url) {
        return this.sendPageRPC("Page.navigate", {
            "url": url
        });
    };
    
    this.navigateEvaluately = function(url) {
        return this.evaluate('location.href = "' + url + '"');
    };

    this.evaluate = function(expression, frameIndex) {
        if (this.isPreventEvaluate) return;

        var frameIndex = (typeof frameIndex !== "undefined" ? frameIndex : this.frameIndex);

        if (frameIndex > -1) {
            expression = 'var __getFrame=function(e){return document.getElementsByTagName("frame")[e]},__getDocument=function(){return __getFrame(' + frameIndex + ').contentDocument||__getFrame(' + frameIndex + ').contentWindow.document},__getWindow=function(){return __getFrame(' + frameIndex + ').contentWindow};' + expression;
        } else {
            expression = 'var __getDocument=function(){return document},__getWindow=function(){return window};' + expression;
        }

        try {
            return this.sendPageRPC("Runtime.evaluate", {
                "expression": expression
            });
        } catch (e) {
            console.error("ChromeObject.evaluate() ->", e.message);
        }
    };

    this.getEvaluatedValue = function(expression) {
        try {
            var responseText = this.evaluate(expression);
            console.info(responseText);

            var result = JSON.parse(responseText).result.result.value;
            if (typeof(result) !== "undefined" && result != null) {
                return result;
            } else {
                return "";
            }
        } catch (e) {
            console.error("ChromeObject.getEvaluatedValue() ->", e.message);
            return "";
        }
    };

    this.exit = function() {
        return this.sendPageRPC("Browser.close", {});
    };
    
    this.close = function() {
        var response = this.sendPageRPC("Page.close", {});
        this.setPageId(null);
        return response;
    };

    this.terminate = function() {
        try {
            this.oAutoIt.WinKill(this.getTitle());
        } catch (e) {
            console.error("ChromeObject.terminate() ->", e.message);
        }
    };

    this.focus = function() {
        var title = "";

        if (this.debuggingPort > 0) {
            try {
                // set page id
                if (this.pageId == "") {
                    this.setPageId(null);
                }

                // get current title
                var _title = this.getTitle();

                // if not focused
                if (_title.indexOf(this.pageId.substring(0, 6)) < 0) {
                    // save previous title
                    this.title = _title;

                    // will be change title
                    title = this.title + " " + this.pageId.substring(0, 6);

                    // change webpage title for focusing window
                    this.setTitle(title);
                } else {
                    title = _title;   /// when it is already catch
                }

                // find window by title
                var pageList = this.getPageList();
                if (pageList.length > 0) {
                    this.oAutoIt.WinActivate(title);
                }
            } catch (e) {
                console.error("ChromeObject._focus() ->", e.message);
            }
        }

        // calling `onfocus` event
        this.dispatchEvent(new STD.Event("focus"));

        return title;
    };

    this.blur = function() {
        return this.setTitle(this.title);
    };

    this.getScrollHeight = function(selector) {
        return parseInt(this.getEvaluatedValue('__getDocument().querySelector("' + selector + '").scrollHeight'));
    };

    this.focusWithoutActivate = function() {
        if (this.debuggingPort > 0) {
            try {
                // if page ID is empty
                if (this.pageId == "") {
                    var pageList = this.getPageList();
                    if (pageList instanceof Array && pageList.length > 0) {
                        this.pageId = pageList[0].id;
                    }
                }

                // change webpage title for focusing window
                this.setTitle(this.pageId);
            } catch (e) {
                console.error("ChromeObject.focusWithoutActivate() ->", e.message);
            }
        }
    };

    this.autoAdjustByScreen = function(sX, sY, divX, divY) {
        // focus
        var title = this.focus();
        sleep(300);

        // adjust window position and size
        var bX = Math.floor(sX / divX);
        var bY = Math.floor(sY / divY);
        var x = this.getRandomInt(0, bX);
        var y = this.getRandomInt(0, bY);
        var w = this.getRandomInt(bX * 3, sX - bX);
        var h = this.getRandomInt(bY, sY - bY);
        this.oAutoIt.WinMove(title, "", x, y, w, h);

        // blur
        this.blur();
    };

    this.autoAdjustByWindow = function(sX, sY, w1, w2, h1, h2) {
        // catch focus
        var title = this.focus();
        sleep(300);

        // adjust window position and size
        var w = this.getRandomInt(w1, w2);
        var h = this.getRandomInt(h1, h2);
        var x = this.getRandomInt(0, (sX - w < 0 ? parseInt(sX * 0.2) : (sX - w)));
        var y = this.getRandomInt(0, (sY - h < 0 ? parseInt(sY * 0.2) : (sY - h)));
        this.oAutoIt.WinMove(title, "", x, y, w, h);

        // blur
        this.blur();
    };
    
    this.downMouseWheel = function(times) {
        if (this.debuggingPort > 0) {
            try {
                var pos = this.getScreenPosition();
                this.oAutoIt.MouseMove(pos.x + 100, pos.y + 100);
                this.oAutoIt.MouseWheel("down", times);
            } catch (e) {
                console.error("ChromeObject.downMouseWheel() ->", e.message);
            }
        }
    };

    this.upMouseWheel = function(times) {
        if (this.debuggingPort > 0) {
            try {
                var pos = this.getScreenPosition();
                this.oAutoIt.MouseMove(pos.x + 100, pos.y + 100);
                this.oAutoIt.MouseWheel("up", times);
            } catch (e) {
                console.error("ChromeObject.upMouseWheel() ->", e.message);
            }
        }
    };
    
    this.setTitle = function(title) {
        var i = 0, repeat = 2;

        if (this.debuggingPort > 0) {
            while (i < repeat) {
                this.evaluate('document.title = ' + this.__escape(title));
                i++;
            }
        }
    };

    this.getTitle = function() {
        if (this.debuggingPort > 0) {
            return this.getEvaluatedValue('document.title');
        }
    };

    this.getScreenPosition = function() {
        var result = this.getEvaluatedValue('(function() { return [__getWindow().screenX, __getWindow().screenY].join(","); })();');
        var pos = result.split(',');
        return {
            "x": parseInt(pos[0]),
            "y": parseInt(pos[1])
        };
    };

    this.getElementPosition = function(selector, startIndex) {
        var startIndex = (typeof startIndex !== 'undefined' ? startIndex : 0);
        var result;
        var pos = -1;

        if (startIndex > 0) {
            result = this.getEvaluatedValue('(function(k) { var rect = __getDocument().querySelectorAll("' + selector + '")[k].getBoundingClientRect(); return [parseInt(rect.left), parseInt(rect.top), parseInt(__getWindow().pageXOffset + rect.left), parseInt(__getWindow().pageYOffset + rect.top), parseInt(rect.width), parseInt(rect.height)].join(","); })(' + startIndex + ');');
        } else {
            result = this.getEvaluatedValue('(function() { var rect = __getDocument().querySelector("' + selector + '").getBoundingClientRect(); return [parseInt(rect.left), parseInt(rect.top), parseInt(__getWindow().pageXOffset + rect.left), parseInt(__getWindow().pageYOffset + rect.top), parseInt(rect.width), parseInt(rect.height)].join(","); })();');
        }

        pos = result.split(',');
        if (pos.length == 6) {
            return {
                "x": parseInt(pos[0]),
                "y": parseInt(pos[1]),
                "a": parseInt(pos[2]),
                "b": parseInt(pos[3]),
                "w": parseInt(pos[4]),
                "h": parseInt(pos[5])
            };
        } else {
            return {
                "x": -1,
                "y": -1,
                "a": -1,
                "b": -1,
                "g": -1,
                "d": -1
            };
        }
    };

    this.getElementPositionByText = function(selector, searchText) {
        var result;
        var pos = -1;
        var s = '(function() {'
            + '    var element = Object.values(__getDocument().querySelectorAll("' + selector + '")).find(function(x) {'
            + '        return (x.innerText.indexOf(' + this.__escape() + ') > -1);'
            + '    });'
            + '    if (element) {'
            + '        var rect = element.getBoundingClientRect();'
            + '        return [parseInt(rect.left), parseInt(rect.top), parseInt(__getWindow().pageXOffset + rect.left), parseInt(__getWindow().pageYOffset + rect.top), parseInt(rect.width), parseInt(rect.height)].join(",");'
            + '    }'
            + '})()'
        ;

        result = this.getEvaluatedValue(s);
        pos = result.split(',');
        if (pos.length == 6) {
            return {
                "x": parseInt(pos[0]),
                "y": parseInt(pos[1]),
                "a": parseInt(pos[2]),
                "b": parseInt(pos[3]),
                "w": parseInt(pos[4]),
                "h": parseInt(pos[5])
            };
        } else {
            return {
                "x": -1,
                "y": -1,
                "a": -1,
                "b": -1,
                "g": -1,
                "d": -1
            };
        }
    };

    this.getNestedElementPosition = function(selector, subSelector, searchText, startIndex) {
        var s = '';
        var startIndex = (typeof startIndex !== 'undefined' ? startIndex : 0);

        if (searchText.indexOf(':tokenize(') == 0) {
            searchText = searchText.substring(searchText.indexOf('(') + 1, searchText.lastIndexOf(')'));
            s += '(function() {'
                + '    var elements = Object.values(__getDocument().querySelectorAll("' + selector + '")).filter(function(x) {'
                + '        var el = x.querySelector("' + subSelector + '");'
                + '        var keywords = ' + this.__escape(searchText) + '.trim().split(" ");'
                + '        var text = el instanceof HTMLElement ? [el.innerText, el.getAttribute("aria-label"), el.getAttribute("class")].join(" ") : "";'
                + '        return (text.split(" ").filter(function(w) { return keywords.indexOf(w) > -1; }).length >= keywords.length);'
                + '    ' + (startIndex > 0 ? '}).slice(' + startIndex + ');' : '});')
                + '    if (elements.length > 0) {'
                + '        var element = elements[0];'
                + '        var rect = element.getBoundingClientRect();'
                + '        var elClassName = "welsonjs_" + parseInt(Math.random() * 1000000000);'
                + '        element.setAttribute("class", element.getAttribute("class") + " " + elClassName);'
                + '        return [parseInt(rect.left), parseInt(rect.top), parseInt(__getWindow().pageXOffset + rect.left), parseInt(__getWindow().pageYOffset + rect.top), parseInt(rect.width), parseInt(rect.height), "." + elClassName].join(",");' 
                + '    } else {'
                + '        return "";'
                + '    }'
                + '})()'
            ;
        }

        else if (searchText.indexOf(':p(') == 0) {
            var p = parseFloat(searchText.substring(searchText.indexOf('(') + 1, searchText.lastIndexOf(')')));
            if (p > 0) {
                s += '(function() {'
                    + '    var elements = Object.values(__getDocument().querySelectorAll("' + selector + '")).filter(function(x) {'
                    + '        return (Math.random() < ' + p + ');'
                    + '    ' + (startIndex > 0 ? '}).slice(' + startIndex + ');' : '});')
                    + '    if (elements.length > 0) {'
                    + '        var element = elements[0];'
                    + '        var rect = element.getBoundingClientRect();'
                    + '        var elClassName = "welsonjs_" + parseInt(Math.random() * 1000000000);'
                    + '        element.setAttribute("class", element.getAttribute("class") + " " + elClassName);'
                    + '        return [parseInt(rect.left), parseInt(rect.top), parseInt(__getWindow().pageXOffset + rect.left), parseInt(__getWindow().pageYOffset + rect.top), parseInt(rect.width), parseInt(rect.height), "." + elClassName].join(",");' 
                    + '    } else {'
                    + '        return "";'
                    + '    }'
                    + '})()'
                ;
            } else {
                s += '(function() {'
                    + '    var elements = Object.values(__getDocument().querySelectorAll("' + selector + '"));'
                    + '    ' + (startIndex > 0 ? 'elements = elements.slice(' + startIndex + ');' : '')
                    + '    if (elements.length > 0) {'
                    + '        var k = Math.floor(Math.random() * elements.length);'
                    + '        var element = elements[k];'
                    + '        var rect = element.getBoundingClientRect();'
                    + '        var elClassName = "welsonjs_" + parseInt(Math.random() * 1000000000);'
                    + '        element.setAttribute("class", element.getAttribute("class") + " " + elClassName);'
                    + '        return [parseInt(rect.left), parseInt(rect.top), parseInt(__getWindow().pageXOffset + rect.left), parseInt(__getWindow().pageYOffset + rect.top), parseInt(rect.width), parseInt(rect.height), "." + elClassName].join(",");' 
                    + '    } else {'
                    + '        return "";'
                    + '    }'
                    + '})()'
                ;
            }
        }

        else {
            s += '(function() {'
                + '    var elements = Object.values(__getDocument().querySelectorAll("' + selector + '")).filter(function(x) {'
                + '        var el = x.querySelector("' + subSelector + '");'
                + '        var searchText = ' + this.__escape(searchText) + '.trim();'
                + '        var text = el instanceof HTMLElement ? [el.innerText, el.getAttribute("aria-label"), el.getAttribute("class")].join(" ") : "";'
                + '        return (text.indexOf(searchText) > -1);'
                + '    ' + (startIndex > 0 ? '}).slice(' + startIndex + ');' : '});')
                + '    if (elements.length > 0) {'
                + '        var element = elements[0];'
                + '        var rect = element.getBoundingClientRect();'
                + '        var elClassName = "welsonjs_" + parseInt(Math.random() * 1000000000);'
                + '        element.setAttribute("class", element.getAttribute("class") + " " + elClassName);'
                + '        return [parseInt(rect.left), parseInt(rect.top), parseInt(__getWindow().pageXOffset + rect.left), parseInt(__getWindow().pageYOffset + rect.top), parseInt(rect.width), parseInt(rect.height), "." + elClassName].join(",");' 
                + '    } else {'
                + '        return "";'
                + '    }'
                + '})()'
            ;
        }

        var result = this.getEvaluatedValue(s);
        var pos = result.split(',');
        if (pos.length == 7) {
            return {
                "x": parseInt(pos[0]),
                "y": parseInt(pos[1]),
                "a": parseInt(pos[2]),
                "b": parseInt(pos[3]),
                "w": parseInt(pos[4]),
                "h": parseInt(pos[5]),
                "s": pos[6]
            };
        } else {
            return {
                "x": -1,
                "y": -1,
                "a": -1,
                "b": -1,
                "w": -1,
                "h": -1,
                "s": ""
            };
        }
    };

    this.triggerEventOnNestedElement = function(eventName, selector, subSelector, searchText, startIndex) {
        var s = '';
        var startIndex = (typeof startIndex !== 'undefined' ? startIndex : 0);

        if (searchText.indexOf(':tokenize(') == 0) {
            searchText = searchText.substring(searchText.indexOf('(') + 1, searchText.lastIndexOf(')'));
            s += '(function() {'
                + '    var elements = Object.values(__getDocument().querySelectorAll("' + selector + '")).filter(function(x) {'
                + '        var el = x.querySelector("' + subSelector + '");'
                + '        var keywords = ' + this.__escape(searchText) + '.trim().split(" ");'
                + '        var text = el instanceof HTMLElement ? [el.innerText, el.getAttribute("aria-label"), el.getAttribute("class")].join(" ") : "";'
                + '        return (text.split(" ").filter(function(w) { return keywords.indexOf(w) > -1; }).length >= keywords.length);'
                + '    ' + (startIndex > 0 ? '}).slice(' + startIndex + ');' : '});')
                + '    if (elements.length > 0) {'
                + '        elements[0].' + (eventName == 'click' ? 'click()' : 'dispatchEvent(new Event("' + eventName + '"))') + ';'
                + '    }'
                + '})()'
            ;
        } else if (searchText.indexOf(':p(') == 0) {
            var p = parseFloat(searchText.substring(searchText.indexOf('(') + 1, searchText.lastIndexOf(')')));
            if (p > 0) {
                s += '(function() {'
                    + '    var elements = Object.values(__getDocument().querySelectorAll("' + selector + '")).filter(function(x) {'
                    + '        return (Math.random() < ' + p + ');'
                    + '    ' + (startIndex > 0 ? '}).slice(' + startIndex + ');' : '});')
                    + '    if (elements.length > 0) {'
                    + '        elements[0].' + (eventName == 'click' ? 'click()' : 'dispatchEvent(new Event("' + eventName + '"))') + ';'
                    + '    }'
                    + '})()'
                ;
            } else {
                s += '(function() {'
                    + '    var elements = Object.values(__getDocument().querySelectorAll("' + selector + '"));'
                    + '    ' + (startIndex > 0 ? 'elements = elements.slice(' + startIndex + ');' : '')
                    + '    if (elements.length > 0) {'
                    + '        var k = Math.floor(Math.random() * elements.length);'
                    + '        elements[k].' + (eventName == 'click' ? 'click()' : 'dispatchEvent(new Event("' + eventName + '"))') + ';'
                    + '    }'
                    + '})()'
                ;
            }
        } else {
            s += '(function() {'
                + '    var elements = Object.values(__getDocument().querySelectorAll("' + selector + '")).filter(function(x) {'
                + '        var el = x.querySelector("' + subSelector + '");'
                + '        var searchText = ' + this.__escape(searchText) + '.trim();'
                + '        var text = el instanceof HTMLElement ? [el.innerText, el.getAttribute("aria-label"), el.getAttribute("class")].join(" ") : "";'
                + '        return (text.indexOf(searchText) > -1);'
                + '    ' + (startIndex > 0 ? '}).slice(' + startIndex + ');' : '});')
                + '    if (elements.length > 0) {'
                + '        elements[0].' + (eventName == 'click' ? 'click()' : 'dispatchEvent(new Event("' + eventName + '"))') + ';'
                + '    }'
                + '})()'
            ;
        }

        return this.evaluate(s);
    };

    this.getNestedElementIndex = function(selector, subSelector, searchText) {
        var s = '';

        s += '(function() {'
            + '    var elements = Object.values(__getDocument().querySelectorAll("' + selector + '"));'
            + '    var result = -1;'
            + '    for (var i = 0; i < elements.length; i++) {'
            + '        if (x.querySelector("' + subSelector + '").innerText.indexOf(' + this.__escape(searchText) + ') > -1) {'
            + '            result = i;'
            + '            break;'
            + '        }'
            + '    }'
            + '    return result;'
            + '})()'
        ;

        return parseInt(this.getEvaluatedValue(s));
    };

    this.getElementCount = function(selector) {
        return this.getEvaluatedValue('document.querySelectorAll("' + selector + '").length');
    };

    this.getPageHeight = function() {
        var height = 0;

        if (this.debuggingPort > 0) {
            var result = this.getEvaluatedValue('(function(obj) { return Math.max(obj.scrollHeight, obj.clientHeight); })(__getDocument().querySelector("html"))');
            height = parseInt(result);
        }

        return height;
    };
    
    this.setIsAttached = function(isAttached) {
        this.isAttached = isAttached;
        return this;
    };

    this.getRandomInt = function(min, max) {
        min = Math.ceil(min);
        max = Math.floor(max);
        return Math.floor(Math.random() * (max - min + 1)) + min;
    };
    
    this.checkDebuggingPort = function() {
        var isChecked = false;
        var isDone = false;

        while (!isDone) {
            try {
                if (this.debuggingPort > 0) {
                    var result = SHELL.exec("netstat -ano | findstr :" + this.debuggingPort);
                    if (result.indexOf(":" + this.debuggingPort) > -1) {
                        isChecked = true;
                    }
                }

                isDone = true;
            } catch(e) {
                sleep(1);
                console.error("ChromeObject.checkDebuggingPort() ->", e.message);
            }
        }

        return isChecked;
    };

    this.getCurrentUrl = function() {
        var page = this.getPageById(this.pageId);
        return page.url;
    };

    this.getCurrentDomain = function() {
        return this.getEvaluatedValue('__getDocument().domain') || '';
    };

    this.triggerEvent = function(eventName, selector) {
        if (selector.indexOf(':p(') < 0) {
            if (eventName == 'click') {
                return this.evaluate('__getDocument().querySelector("' + selector + '").click()');
            } else {
                return this.evaluate('__getDocument().querySelector("' + selector + '").dispatchEvent(new Event("' + eventName + '"))');
            }
        } else {
            var p = parseFloat(selector.substring(selector.indexOf('(') + 1, selector.indexOf(')')));
            var _selector = selector.substring(0, selector.indexOf(':'));
            if (p > 0) {
                return this.evaluate('(function(obj, p) { var element = Object.values(obj).find(function() { return (Math.random() < p); }); if(element) element.click(); })(__getDocument().querySelectorAll("' + _selector + '"), ' + p + ')');
            } else {
                return this.evaluate('(function(obj) { var elements = Object.values(obj); var element = elements[Math.floor(Math.random() * elements.length)]; if(element) element.click(); })(__getDocument().querySelectorAll("' + _selector + '"))');
            }
        }
    };
    
    this.triggerEventByFind = function(eventName, selector, searchText) {
        var s = '(function() {'
            + '    var element = Object.values(__getDocument().querySelectorAll("' + selector + '")).find(function(x) {'
            + '        return (x.innerText.indexOf(' + this.__escape(searchText) + ') > -1);'
            + '    });'
            + '    if (element) {'
            + '        element.' + (eventName == 'click' ? 'click()' : 'dispatchEvent(new Event("' + eventName + '"))') + ';'
            + '    }'
            + '})()'
        ;
        return this.evaluate(s);
    };

    this.triggerEventOnNestedFind = function(eventName, selector, subSelector, searchText) {
        var s = '(function() {'
            + '    var element = Object.values(__getDocument().querySelectorAll("' + selector + '")).find(function(x) {'
            + '        return (x.querySelector("' + subSelector + '").innerText.indexOf(' + this.__escape(searchText) + ') > -1);'
            + '    });'
            + '    if (element) {'
            + '        element.' + (eventName == 'click' ? 'click()' : 'dispatchEvent(new Event("' + eventName + '"))') + ';'
            + '    }'
            + '})()'
        ;
        return this.evaluate(s);
    };

    this.scrollTo = function(x, y) {
        return this.evaluate('__getWindow().scrollTo(parseInt(' + x + '), parseInt(' + y + '))');
    };

    this.scrollBy = function(dx, dy) {
        return this.evaluate('__getWindow().scrollBy(parseInt(' + dx + '), parseInt(' + dy + '))');
    };

    this.scrollToElement = function(selector, dx, dy) {
        return this.evaluate('(function(rect, dx, dy) { __getWindow().scrollTo(rect.x + dx, rect.y + dy); })(__getDocument().querySelector("' + selector + '").getBoundingClientRect(), parseInt("' + dx + '"), parseInt("' + dy + '"))');
    };

    this.reload = function() {
        //return this.sendPageRPC("Page.reload", {});
        return this.evaluate("__getWindow().reload()");
    };

    this.hasClass = function(seletctor, className) {
        try {
            var result = this.getEvaluatedValue('__getDocument().querySelector("' + seletctor + '").getAttribute("class")');
            if (typeof(result) === "string") {
                return (result.split(' ').indexOf(className) > -1);
            } else {
                return false;
            }
        } catch (e) {
            console.error("ChromeObject.hasClass() ->", e.message);
        }
    };

    this.getAttribute = function(selector, attributeName) {
        return this.getEvaluatedValue('__getDocument().querySelector("' + selector + '").getAttribute("' + attributeName + '")');
    };

    this.sendSpaceKey = function() {
        this.oAutoIt.Send("{SPACE}");
    };

    this.setValue = function(selector, value, repeat, searchIndex) {
        var s = value,
            i = 0,
            searchIndex = (typeof searchIndex !== "undefined" ? searchIndex : 0),
            repeat = (typeof repeat !== "undefined" ? repeat : 1)
        ;

        while (i < repeat) {
            if (searchIndex > 0) {
                this.evaluate('Object.values(__getDocument().querySelectorAll("' + selector + '"))[' + searchIndex + '].value = ' + this.__escape(s));
            } else {
                this.evaluate('__getDocument().querySelector("' + selector + '").value = ' + this.__escape(s));
            }
            i++;
        }
    };
    
    this.getText = function(selector) {
        return this.getEvaluatedValue('__getDocument().querySelector("' + selector + '").innerText');
    };

    this.setHTML = function(selector, value, repeat, searchIndex) {
        var s = value,
            i = 0,
            searchIndex = (typeof searchIndex !== "undefined" ? searchIndex : 0),
            repeat = (typeof repeat !== "undefined" ? repeat : 1)
        ;

        while (i < repeat) {
            if (searchIndex > 0) {
                this.evaluate('Object.values(__getDocument().querySelectorAll("' + selector + '"))[' + searchIndex + '].value = ' + this.__escape(s));
            } else {
                this.evaluate('__getDocument().querySelector("' + selector + '").innerHTML = ' + this.__escape(s));
            }
            i++;
        }
    };

    this.getHTML = function(selector) {
        return this.getEvaluatedValue('__getDocument().querySelector("' + selector + '").innerHTML');
    };

    this.traceMouseClick = function() {
        return this.evaluate('__getWindow().addEventListener("click",function(e){var t=e.clientX,n=e.clientY,l=__getDocument().createElement("div");l.style.position="absolute",l.style.width="20px",l.style.height="20px",l.style.backgroundColor="#00ff00",l.style.zIndex=99999,l.style.top=__getWindow().pageYOffset+n-10+"px",l.style.left=__getWindow().pageXOffset+t-10+"px",__getDocument().body.appendChild(l)});');
    };

    this.getWindowInnerHeight = function() {
        return parseInt(this.getEvaluatedValue('__getWindow().innerHeight'));
    };

    this.getDocumentScrollTop = function() {
        return parseInt(this.getEvaluatedValue('__getDocument().documentElement.scrollTop'));
    };

    // formula: y > 0 and y + h < ih
    this.isVisibleElementInViewport = function(elementPosition) {
        return (elementPosition.y > 0 && (elementPosition.y + elementPosition.h < this.getWindowInnerHeight()));
    };

    this.__escape = function(value) {
        return 'decodeURIComponent("' + encodeURIComponent(value) + '")';
    };

    this.create();
};
ChromeObject.prototype = new STD.EventableObject();
ChromeObject.prototype.constructor = ChromeObject;

exports.create = function(profileName) {
    return (new ChromeObject()).setProfile(profileName, null);
};

exports.start = function(url, proxyPort, profileName, userDataDir, installedDir) {
    return (new ChromeObject())
        .setProxyPort(proxyPort)
        .setProfile(profileName, installedDir)
        .setUserDataDir(userDataDir)
        .open(url)
    ;
};

exports.startWithDebugging = function(url, proxy, profileName, debuggingPort) {
    return (new ChromeObject())
        .setProxy(proxy)
        .setProfile(profileName, null)
        .setUserDataDir(null)
        .setDebuggingPort(debuggingPort)
        .open(url)
    ;
};

exports.startDebug = function(url, proxy, profileName, debuggingPort, isPreventProxy) {
    return (new ChromeObject())
        .setProxy(proxy)
        .setProfile(profileName, null)
        .setUserDataDir(null)
        .setDebuggingPort(debuggingPort)
        .setIsPreventProxy(isPreventProxy)
        .addUserAgentsFromFile("data\\Chrome.txt")
        .addUserAgentsFromFile("data\\Edge.txt")
        .addUserAgentsFromFile("data\\Safari.txt")
        .open(url)
    ;
};

exports.VERSIONINFO = "Chrome Web Browser Debugging Interface (chrome.js) version 0.2";
exports.global = global;
exports.require = global.require;
