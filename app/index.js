// index.js - The entrypoint on WelsonJS GUI envionment
// Namhyeon Go <abuse@catswords.net>
// https://github.com/gnh1201/welsonjs
var FILE = require("lib/file");
var SHELL = require("lib/shell");
var Browser = require("lib/browser");
var Router = require("lib/router").Router;

// using jsrender
Router.setRender(function(filename, data) {
    var template = FILE.readFile(filename, FILE.CdoCharset.CdoUTF_8);
    var tmpl = $.templates(template);
    addContentItem(filename, tmpl.render(data));

    // check all links
    Object.values(document.getElementsByTagName("a")).map(function(x) {
        x.addEventListener("click", function(e) {
            var href = e.target.getAttribute("href"); // NOTE: x.href != x.getAttribute("href")
            if (href.indexOf('/') == 0) {
                e.preventDefault();
                Router.go(href);
            }
        });
    });
});

// main
Router.add('/', function(render) {
    render("app\\signin.html", {});
});

// test
Router.add('/test', function(render) {
    var test_profile_filepath = "data/test-oss-korea-2023.json";

    window.test_start = function(test_id) {
        SHELL.show(["cscript", "app.js", "testloader", test_id, test_profile_filepath]);
    };

    window.gui_check = function() {
        var text1 = SHELL.exec("echo hello world!");
        alert(text1);

        var text2 = require("lib/system").getOS();
        alert(text2);

        alert("모든 메시지가 정상적으로 보였다면 테스트에 성공한 것입니다.");
    };

    var content = FILE.readFile(test_profile_filepath, FILE.CdoCharset.CdoUTF_8);
    var data = JSON.parse(content);
    render("app/test.html", {
        "data": data
    });
});

// nodepad
Router.add('/notepad', function(render) {
    // load resources
    Browser.addStylesheet("app/assets/mixed/summernote-0.8.21-proposal/summernote-lite.css");
    Browser.waitUntil(function(test, ttl) {
        Browser.addScript("app/assets/mixed/summernote-0.8.21-proposal/summernote-lite.js", function(el) {
            // set DOM id
            var target_dom_id = "summernote";

            // load HTML
            render("app/notepad.html", {
                "target_dom_id": target_dom_id
            });

            // load Summernote (wysiwyg editor)
            $('#' + target_dom_id).summernote({
                minHeight: 300
            });

        }, test, ttl);
    }, function(el) {
        return $.summernote;
    });

    document.getElementById("useragent").innerHTML = window.navigator.userAgent;
});

Router.add('/components', function(render) {
    render("app\\components.html", {});

    console.log("example log message");
    console.warn("example warning message");
    console.info("example information message");
    console.error("example error message");
});


// clear
Browser.setContent("");

// initialize the layout
var config = {
    settings: {
        hasHeaders: true,
        constrainDragToContainer: false,
        reorderEnabled: false,
        selectionEnabled: false,
        popoutWholeStack: false,
        blockedPopoutsThrowError: false,
        closePopoutsOnUnload: false,
        showPopoutIcon: false,
        showMaximiseIcon: false,
        showCloseIcon: false
    },
    content: [{
        type: 'stack',
        isClosable: false,
        content: []
    }]
};

var myLayout = new GoldenLayout(config);

myLayout.registerComponent('example', function(container, state) {
    container.getElement().html('<div>' + state.text + '</div>');
});

myLayout.init();

var addContentItem = function(title, text) {
    var contentItemConfig = {
        title: title,
        id: title,
        type: 'component',
        componentName: 'example',
        componentState: {
            text: text
        }
    };

    var contentItems = myLayout.root.getItemsById(title);
    var contentItemsCount = contentItems.length;
    if (contentItemsCount > 0) {
        contentItems[0].element.show();
    } else {
        myLayout.root.contentItems[0].addChild(contentItemConfig);
    }
};

// enable move the window with mouse drag and drop
(function(grip) {
    var oX, oY,
        mouseDown = function(e) {
            if (e.offsetY + e.offsetX < 0) return;
            oX = e.screenX;
            oY = e.screenY;
            window.addEventListener("mousemove", mouseMove);
            window.addEventListener("mouseup", mouseUp);
        },
        mouseMove = function(e) {
            window.moveTo(screenX + e.screenX - oX, screenY + e.screenY - oY);
            oX = e.screenX;
            oY = e.screenY;
        },
        gripMouseMove = function(e) {
            this.style.cursor = (e.offsetY + e.offsetX > -1) ? "move" : "default";
        },
        mouseUp = function(e) {
            window.removeEventListener("mousemove", mouseMove);
            window.removeEventListener("mouseup", mouseUp);
        };

    grip.addEventListener("mousedown", mouseDown);
    grip.addEventListener("mousemove", gripMouseMove);
})($(".lm_header")[0]);

// go to main
Router.go('/');