// testloader.js
// Namhyeon Go <abuse@catswords.net>
// https://github.com/gnh1201/welsonjs

// load libraries
var FILE = require("lib/file");

// implement the tests
var test_implements = {
    // Ref 1: https://gist.github.com/CityRay/c56e4fa874af9370cc1a367bd43095b0
    "es5_polyfills": function() {
        var parseIntIgnoresLeadingZeros = (function () {
            return parseInt('010', 10) === 10;
        }());
        console.log("parseIntIgnoresLeadingZeros: " + String(parseIntIgnoresLeadingZeros) + " (Default on the built-in engine: true)");

        var DateISOString = (function () {
            return !!(Date && Date.prototype && Date.prototype.toISOString);
        }());
        console.log("DateISOString: " + String(DateISOString) + " (Default on the built-in engine: false)");

        var result = !!(parseIntIgnoresLeadingZeros && DateISOString);

        console.log(result ?
            "ECMAScript 5 수준의 런타임입니다. 필수 테스트를 모두 통과하였습니다." :
            "ECMAScript 5 수준의 런타임이 아닙니다. 필수 테스트 중 하나를 실패하였습니다.");
    },

    // Ref 1: https://learn.microsoft.com/en-us/previous-versions/windows/desktop/regprov/stdregprov
    "registry_find_provider": function() {
        var REG = require("lib/registry");

        console.log("레지스트리 제공자(StdRegProv) 클래스에 접근을 시도합니다.");
        var provider = REG.getProvider();
        
        console.log(typeof provider !== "undefined" ?
            "레지스트리 제공자 클래스에 성공적으로 연결되었습니다." :
            "레지스트리 제공자 클래스에 연결하는데 문제가 있습니다.");
    },

    "registry_write": function() {
        var REG = require("lib/registry");
        var appName = "welsonjs";
        
        console.log("레지스트리에 쓰기를 시도합니다.");
        REG.write(REG.HKCU, appName + "\\WelsonJSTest", "", "here are in", REG.STRING);
        console.log("레지스트리에 쓰기를 완료하였습니다.");
    },

    "registry_read": function() {
        var REG = require("lib/registry");
        var appName = "welsonjs";
        
        console.log("레지스트리 읽기를 시도합니다.");
        var response = REG.read(REG.HKCU, appName + "\\WelsonJSTest", "", REG.STRING);
        console.log("읽음: " + response);
        console.log(response === "here are in" ?
            "레지스트리를 정상적으로 읽었습니다." :
            "레지스트리를 읽는데 문제가 있습니다.");
    },

    "wmi_create_object": function() {
        var WMI = require("lib/wmi");
        console.log(typeof WMI.create().interface !== "undefined" ?
            "WMI 객체를 정상적으로 생성하였습니다." :
            "WMI 객체를 생성하는데 문제가 있습니다.");
    },

    "wmi_execute_query": function() {
        var WMI = require("lib/wmi");

        var queryString = "SELECT Caption FROM Win32_OperatingSystem";
        console.log("실행할 쿼리: " + queryString);

        var result = WMI.execQuery(queryString).fetch().get("Caption").trim();
        console.log("현재 윈도우즈 배포판 종류: " + result);
    },

    "wmi_result_query": function() {
        var WMI = require("lib/wmi");

        var queryString = "SELECT Caption FROM Win32_OperatingSystem";
        console.log("실행할 쿼리: " + queryString);

        var result = WMI.execQuery(queryString).fetch().get("Caption").trim();
        console.log("현재 윈도우즈 배포판 종류: " + result);
    },

    "shell_create_object": function() {
        var SHELL = require("lib/shell");
        console.log(typeof SHELL.create().interface !== "undefined" ?
            "윈도우즈 쉘(Shell) 객체를 정상적으로 생성하였습니다." :
            "윈도우즈 쉘(Shell) 객체를 생성하는데 문제가 있습니다.");
    },

    "shell_build_command_line": function() {
        var SHELL = require("lib/shell");
        console.log("쉘 명령의 무결성을 훼손시킬 수 있는 경우(따옴표, 쌍따옴표, 띄어쓰기 등을 포함한 경우)를 테스트합니다.");

        // 모든 경우를 하나의 배열에 포함
        var test_args = [
            "ppap.exe",
            "\"pen\"",
            "'apple'",
            "apple pen",
            "pineapple pen"
        ];
        
        // 입력과 출력 확인
        console.log("입력 인자: " + test_args.join(" / "));
        console.log("출력 문자열: " + SHELL.build(test_args));
        console.log("정상적으로 명령행이 만들어졌는지 확인하세요.");
    },

    "shell_set_charset": function() {
        var SHELL = require("lib/shell");

        var ShellInstance = SHELL.create();
        console.log("현재 기준 문자셋: " + ShellInstance.charset);
        console.log("기준 문자셋을 유니코드(UTF-8)로 변경합니다.");
        ShellInstance.setCharset(SHELL.CdoCharset.CdoUTF_8);
        console.log("현재 기준 문자셋: " + ShellInstance.charset);
    },

    "shell_working_directory": function() {
        var SHELL = require("lib/shell");

        var ShellInstance = SHELL.create();
        console.log("현재 작업 폴더: " + ShellInstance.workingDirectory);

        console.log("작업 폴더를 변경합니다.");
        ShellInstance.setWorkingDirectory(ShellInstance.workingDirectory + "\\data");
        console.log("현재 작업 폴더: " + ShellInstance.workingDirectory);
    },

    "shell_create_process": function() {
        var SHELL = require("lib/shell");

        var shell = SHELL.create();
        var process = shell.createProcess("calc.exe");
        sleep(1500);
        var processId = process.ProcessId;
        console.log("프로세스 ID:", processId);
        sleep(1500);
        shell.release();

        console.log("계산기가 정상적으로 실행되었는지 확인하세요.");
    },

    "shell_execute": function() {
        var SHELL = require("lib/shell");

        var response = SHELL.exec("echo done");
        console.log(response == "done" ?
            "쉘(Shell) 출력이 정상적으로 작동합니다." :
            "쉘(Shell) 출력에 문제가 있습니다.");
    },

    "shell_run": function() {
        var SHELL = require("lib/shell");

        SHELL.run("calc");
        console.log("잠시 후 계산기 프로그램이 열리는지 확인하여 주세요.");
    },

    "shell_run_as": function() {
        var SHELL = require("lib/shell");

        console.log("관리자 권한 요청을 확인하세요.");
        SHELL.runAs("calc");
        console.log("잠시 후 계산기 프로그램이 열리는지 확인하여 주세요.");
    },

    "shell_find_my_documents": function() {
        var SHELL = require("lib/shell");

        console.log("내 문서 폴더 위치: " + SHELL.getPathOfMyDocuments());
    },

    "shell_release": function() {
        var SHELL = require("lib/shell");

        var ShellInstance = SHELL.create();
        ShellInstance.release();

        console.log(ShellInstance.interface != null ?
            "정상적으로 해제되지 않았습니다." :
            "정상적으로 해제되었습니다.");
    },

    "powershell_set_command": function() {
        var PS = require("lib/powershell");
        var PSInstance = PS.create();
        PSInstance.load("Write-Output \"hello world\"");
        console.log("설정된 명령어: " + PSInstance.target);
    },

    "powershell_set_file": function() {
        var PS = require("lib/powershell");
        var PSInstance = PS.create();
        PSInstance.loadFile("app\\assets\\ps1\\helloworld");
        console.log("설정된 파일: " + PSInstance.target);
    },

    "powershell_set_uri": function() {
        var PS = require("lib/powershell");
        var PSInstance = PS.create();
        PSInstance.loadURI("data:text/plain;base64,V3JpdGUtT3V0cHV0ICJoZWxsbyB3b3JsZCI=");
        console.log("설정된 URI: " + PSInstance.target);
    },

    "powershell_execute": function() {
        var PS = require("lib/powershell");
        var response = PS.execScript("app\\assets\\ps1\\helloworld");
        console.log("실행 결과 값: " + response);
    },

    "powershell_run_as": function() {
        var PS = require("lib/powershell");
        var response = PS.runAs("app\\assets\\ps1\\helloworld");
        console.log("실행 결과 값: " + response);
    },

    "system_resolve_env": function() {
        var SYS = require("lib/system");

        console.log("기본 프로그램 설치 폴더: " + SYS.getEnvString("PROGRAMFILES"));
        console.log("기본 프로그램 설치 폴더가 정상적인 위치를 가르키고 있는지 확인하세요.");
    },

    "system_check_as": function() {
        var SYS = require("lib/system");

        console.log(SYS.isElevated() ?
            "이 런타임은 관리자 모드에서 실행되고 있지 않습니다." :
            "이 런타임은 관리자 모드에서 실행되고 있습니다.");
    },

    "system_get_os_version": function() {
        var SYS = require("lib/system");
        
        console.log("현재 사용중인 운영체제: " + SYS.getOS());
    },

    "system_get_architecture": function() {
        var SYS = require("lib/system");
        
        console.log("현재 사용중인 아키텍처: " + SYS.getArch());
    },

    "system_get_uuid": function() {
        var SYS = require("lib/system");

        console.log("디바이스 고유 번호: " + SYS.getUUID());
    },

    "system_get_working_directory": function() {
        var SYS = require("lib/system");

        console.log("현재 작업 폴더: " + SYS.getCurrentWorkingDirectory());
    },

    "system_get_script_directory": function() {
        var SYS = require("lib/system");

        console.log("현재 스크립트 폴더: " + SYS.getCurrentScriptDirectory());
    },

    "system_get_network_interfaces": function() {
        var SYS = require("lib/system");

        var netInterfaces = SYS.getNetworkInterfaces();
        netInterfaces.forEach(function(x) {
            console.log(x.Caption);
        });
    },

    "system_get_process_list": function() {
        var SYS = require("lib/system");
        
        var processList = SYS.getProcessList();
        processList.forEach(function(x) {
            console.log(x.Caption, x.ProcessId);
        });
    },

    "system_get_process_list_by_name": function() {
        var SYS = require("lib/system");
        
        var processList = SYS.getProcessListByName("explorer.exe");
        processList.forEach(function(x) {
            console.log(x.Caption, x.ProcessId);
        });
    },

    "system_register_uri": function() {
        console.log("웹 브라우저를 열고 주소창에 아래 주소를 입력하세요.");
        console.log("welsonjs:///?application=mscalc");
    },

    "system_pipe_ipc": function() {
        var SHELL = require("lib/shell");
        
        for (var i = 0; i < 3; i++) {
            SHELL.show(["cscript", "app.js", "examples/ipctest"]);
        }
    },

    "vhid_find_window": function() {
        console.log("동일한 기능이 포함된 chromium_send_click 또는 chromium_send_keys 테스트를 수행하십시오");
    },

    "vhid_send_click": function() {
        console.log("동일한 기능이 포함된 chromium_send_click 테스트를 수행하십시오");
    },

    "vhid_send_keys": function() {
        console.log("동일한 기능이 포함된 chromium_send_keys 테스트를 수행하십시오");
    },

    "vhid_send_key_enter": function() {
        var Chrome = require("lib/chrome");
        var wbInstance = Chrome.startDebugInPrivate("https://en.key-test.ru/", null, "welsonjs_test", 9222, true);

        console.log("참고: vhid_send_key_enter 테스트의 엔터 키 기능은 웹 브라우저가 키코드를 처리하도록 작성되었습니다.");
        console.log("5초 후 명령을 수행합니다.");
        sleep(5000);

        wbInstance.focus();
        wbInstance.sendEnterKey();
        sleep(1000);
    },

    "vhid_send_key_functions": function() {
        var Toolkit = require("lib/toolkit");
        var Chrome = require("lib/chrome");
        var wbInstance = Chrome.startDebugInPrivate("https://example.org", null, "welsonjs_test", 9222, true);

        console.log("웹 브라우저를 열고 F1 키를 눌러 도움말 페이지를 열겠습니다.");
        console.log("5초 후 명령을 수행합니다.");
        sleep(5000);

        wbInstance.focus();
        Toolkit.sendFnKey(wbInstance.pageId.substring(0, 6), 1);  // F1 키 누름
    },

    "vhid_alert": function() {
        var Toolkit = require("lib/toolkit");

        Toolkit.alert("잘 보이면 테스트에 성공한 것입니다.");
    },
 
    "vhid_confirm": function() {
        var Toolkit = require("lib/toolkit");
        
        var answer = Toolkit.confirm("예 또는 아니오를 눌러주세요");
        console.log(String(answer));
    },

    "vhid_prompt": function() {
        var Toolkit = require("lib/toolkit");

        var answer = Toolkit.prompt("Enter your input:");
        console.log(String(answer));
    },

    "network_http_get": function() {
        var HTTP = require("lib/http");

        console.log("HTTP GET 요청으로 내 외부 IP를 확인합니다.");
        var response = HTTP.get("https://api.ipify.org/", {}, {});
        console.log("내 외부 IP: " + response);
    },

    "network_http_post": function() {
        var HTTP = require("lib/http");

        var response = HTTP.get("https://httpbin.org/post", {}, {});
        console.log("응답: " + response);
    },

    "network_http_extended": function() {
        var HTTP = require("lib/http");

        var response1 = HTTP.put("https://httpbin.org/put", {}, {});
        console.log("응답: " + response1);
        
        var response2 = HTTP.patch("https://httpbin.org/patch", {}, {});
        console.log("응답: " + response2);
    },

    "network_attach_debugger": function() {
        var HTTP = require("lib/http");

        HTTP.create("CURL")
            .attachDebugger("FIDDLER")
            .get("https://api.ipify.org/");
    },

    "network_detect_charset": function() {
        var HTTP = require("lib/http");

        var response = HTTP.get("https://example.org/", {}, {});
        var charset = HTTP.create("CURL").detectCharset(response);

        console.log("감지된 문자셋: " + charset);
    },

    "network_detect_http_ssl": function() {
        var HTTP = require("lib/http");

        var response = HTTP.create("CURL").get("https://example.org/");
        var charset = HTTP.create("CURL").detectCharset(response);
    },

    "network_send_icmp": function() {
        var SYS = require("lib/system");

        console.log("1.1.1.1로 PING(ICMP) 전송");
        console.log("응답 시간: " + SYS.ping("1.1.1.1") + "ms");
    },

    "extramath_dtm": function() {
        var ExtraMath = require("lib/extramath");
        
        var a = "this is an apple";
        var b = "this is red apple";
        
        var dtm = new ExtraMath.DTM();
        dtm.add(a);
        dtm.add(b);
        var mat = dtm.toArray();

        console.log("Original sentance");
        console.log(a);
        console.log(b);

        console.log("Create a DTM(Document-Term Matrix)");
        console.log(mat[0].join(' '));
        console.log(mat[1].join(' '));
    },

    "extramath_cosine_similarity": function() {
        var ExtraMath = require("lib/extramath");
        
        var a = "this is an apple";
        var b = "this is red apple";

        var dtm = new ExtraMath.DTM();
        dtm.add(a);
        dtm.add(b);
        var mat = dtm.toArray();

        console.log("Original sentance");
        console.log(a);
        console.log(b);

        console.log("Create a DTM(Document-Term Matrix)");
        console.log(mat[0].join(' '));
        console.log(mat[1].join(' '));

        console.log("Measure Cosine Similarity");
        console.log('' + ExtraMath.arrayCos(mat[0], mat[1]));
        console.log('' + ExtraMath.measureSimilarity(a, b));
    },

    "base64_encode": function() {
        var BASE64 = require("lib/base64");
        
        var original_text = "hello world";
        var encoded_text = BASE64.encode(original_text);

        console.log("원문: " + original_text);
        console.log("인코딩된 문자: " + encoded_text);
        
        console.log(encoded_text == "aGVsbG8gd29ybGQ=" ?
            "인코딩된 문자가 기대값과 일치합니다." :
            "인코딩된 문자가 기대값과 불일치합니다.");
    },

    "base64_decode": function() {
        var BASE64 = require("lib/base64");

        var encoded_text = "aGVsbG8gd29ybGQ=";
        var original_text = BASE64.decode(encoded_text);
        
        console.log("인코딩된 문자: " + encoded_text);
        console.log("원문: " + original_text);

        console.log(original_text == "hello world" ?
            "디코드된 문자가 기대값과 일치합니다." :
            "디코드된 문자가 기대값과 불일치합니다.");
    },

    "chromium_run": function() {
        var Chrome = require("lib/chrome");
        Chrome.startDebugInPrivate("https://example.org", null, "welsonjs_test", 9222, true);
    },

    "chromium_create_profile": function() {
        var Chrome = require("lib/chrome");
        console.log("welsonjs_test 프로파일을 생성합니다. (웹 브라우저를 실행하여 생성합니다.)");
        Chrome.startDebug("https://example.org", null, "welsonjs_test", 9222, true);
    },

    "chromium_run_incognito": function() {
        var Chrome = require("lib/chrome");
        Chrome.startDebugInPrivate("https://example.org", null, "welsonjs_test", 9222, true);
    },

    "chromium_navigate": function() {
        var Chrome = require("lib/chrome");
        var wbInstance = Chrome.startDebugInPrivate("https://example.org", null, "welsonjs_test", 9222, true);
        
        console.log("5초 후 구글로 이동합니다.");
        sleep(5000);

        wbInstance.setPageId(null);
        wbInstance.navigate("https://google.com");
    },

    "chromium_get_active_pages": function() {
        var Chrome = require("lib/chrome");
        var wbInstance = Chrome.startDebugInPrivate("https://example.org", null, "welsonjs_test", 9222, true);
        
        console.log("5초 후 페이지 목록을 불러옵니다.");
        sleep(5000);

        var pageList = wbInstance.getPageList();
        pageList.forEach(function(x) {
            console.log(x.title);
        });
    },

    "chromium_find_page_by_id": function() {
        var Chrome = require("lib/chrome");
        var wbInstance = Chrome.startDebugInPrivate("https://example.org", null, "welsonjs_test", 9222, true);
        
        console.log("5초 후 페이지 ID를 불러옵니다.");
        sleep(5000);

        var pageId = wbInstance.getPageList()[0].id;
        console.log("페이지 ID: " + pageId);

        console.log("페이지 ID로 페이지 찾기");
        var page = wbInstance.getPageById(pageId);

        console.log("페이지 제목: " + page.title);
    },

    "chromium_find_pages_by_title": function() {
        var Chrome = require("lib/chrome");
        var wbInstance = Chrome.startDebugInPrivate("https://example.org", null, "welsonjs_test", 9222, true);
        
        console.log("5초 후 페이지 타이틀을 불러옵니다.");
        sleep(5000);

        var pageTitle = wbInstance.getPageList()[0].title;
        console.log("페이지 타이틀: " + pageTitle);

        console.log("페이지 타이틀로 페이지 찾기");
        var pages = wbInstance.getPagesByTitle(pageTitle);   // 타이틀로 찾는 경우 복수형으로 반환됨

        console.log("페이지 ID: " + pages[0].id);
    },

    "chromium_move_focused": function() {
        var Chrome = require("lib/chrome");
        
        var wbInstances = [];
        wbInstances.push(Chrome.startDebugInPrivate("https://example.org", null, "welsonjs_test", 9222, true));
        sleep(2000);
        wbInstances.push(Chrome.startDebugInPrivate("https://naver.com", null, "welsonjs_test_2", 9223, true));
        sleep(2000);
        wbInstances.push(Chrome.startDebugInPrivate("https://daum.net", null, "welsonjs_test_3", 9224, true));
        sleep(2000);

        var wbInstance = wbInstances[1];
        wbInstance.setPageId(null);
        wbInstance.focus();
    },

    "chromium_adjust_window_size": function() {
        var Chrome = require("lib/chrome");
        var wbInstance = Chrome.startDebugInPrivate("https://example.org", null, "welsonjs_test", 9222, true);

        console.log("5초 후 명령을 수행합니다.");
        sleep(5000);

        wbInstance.focus();
        wbInstance.autoAdjustByWindow(10, 10, 1024, 1280, 720, 768);
    },

    "chromium_get_element_position": function() {
        var Chrome = require("lib/chrome");
        var wbInstance = Chrome.startDebugInPrivate("https://example.org", null, "welsonjs_test", 9222, true);
        
        console.log("5초 후 명령을 수행합니다.");
        sleep(5000);
        
        console.log(JSON.stringify(wbInstance.getElementPosition("p")));
    },

    "chromium_get_mapreduced_element_position": function() {
        var Chrome = require("lib/chrome");
        var wbInstance = Chrome.startDebugInPrivate("https://example.org", null, "welsonjs_test", 9222, true);
        
        console.log("5초 후 명령을 수행합니다.");
        sleep(5000);

        console.log("More information... 버튼의 위치를 탐색");
        console.log(JSON.stringify(wbInstance.getNestedElementPosition("p", ":self", "More information...")));
    },

    "chromium_set_value_to_textbox": function() {
        var Chrome = require("lib/chrome");
        var wbInstance = Chrome.startDebugInPrivate("https://html5doctor.com/demos/forms/forms-example.html", null, "welsonjs_test", 9222, true);
        
        console.log("5초 후 명령을 수행합니다.");
        sleep(5000);

        wbInstance.setValue("#given-name", "길동");
        wbInstance.setValue("#family-name", "홍");
    },

    "chromium_send_click": function() {
        var RAND = require("lib/rand");
        var Chrome = require("lib/chrome");
        var wbInstance = Chrome.startDebugInPrivate("https://html5doctor.com/demos/forms/forms-example.html", null, "welsonjs_test", 9222, true);
        
        console.log("5초 후 명령을 수행합니다.");
        sleep(5000);
        
        wbInstance.focus();
        wbInstance.traceMouseClick();

        while (true) {
            wbInstance.vMouseClick(RAND.getInt(20, 200), RAND.getInt(20, 200));
            sleep(2000);
        }
    },

    "chromium_send_keys": function() {
        var Chrome = require("lib/chrome");
        var wbInstance = Chrome.startDebugInPrivate("https://html5doctor.com/demos/forms/forms-example.html", null, "welsonjs_test", 9222, true);
        
        console.log("5초 후 명령을 수행합니다.");
        sleep(5000);
        
        wbInstance.focus();
        wbInstance.traceMouseClick();

        console.log("이메일 필드 위치 찾기");
        var emailInputPosition = wbInstance.getElementPosition("#email");
        wbInstance.vMouseClick(emailInputPosition.x + 2, emailInputPosition.y + 2);
        sleep(1000);
        
        console.log("가상 키 보내기");
        wbInstance.vSendKeys("hong@example.org");
    },

    "chromium_auto_scroll_until_end": function() {
        var Chrome = require("lib/chrome");
        var wbInstance = Chrome.startDebugInPrivate("https://en.wikipedia.org/wiki/Lorem_ipsum", null, "welsonjs_test", 9222, true);

        console.log("5초 후 명령을 수행합니다.");
        sleep(5000);
        
        wbInstance.focus();
        wbInstance.traceMouseClick();

        console.log("스크롤이 끝날 때까지 계속 스크롤을 조정합니다.");

        while(!wbInstance.isPageScrollEnded()) {
            wbInstance.scrollBy(0, 150);
            sleep(2000);
        }
    },

    "grpc_run_server": function() {
        var SHELL = require("lib/shell");

        SHELL.show(["cscript", "app.js", "grpcloader"]);
    },

    "grpc_receive_command": function() {
        var SHELL = require("lib/shell");

        SHELL.show(["grpcloader_test.bat"]);
    },

    "gui_check": function() {
        console.log("GUI 환경에서 실행하여 주십시오");
    },
    
    // profile: data/test-misc-20231107.json
    "gtkserver_test": function() {
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
    },
    
    // profile: data/test-misc-20231107.json
    "toolkit_msedge_test": function() {
        var Chrome = require("lib/chrome");
        var Toolkit = require("lib/toolkit");
        
        var wbInstance = Chrome.create().setVendor("msedge").open("https://google.com");
        sleep(5000);
        //console.log(wbInstance.getHTML("body"));

        wbInstance.focus();
        wbInstance.traceMouseClick();
        Toolkit.sendClick("Google", 30, 30, 1);
    },
    
    // profile: data/test-misc-20231107.json
    "squel_sqlmap_test": function() {
        console.log(squel.select({ separator: "\n" })
            .from("students")
            .field("name")
            .field("MIN(test_score)")
            .field("MAX(test_score)")
            .field("GROUP_CONCAT(DISTINCT test_score ORDER BY test_score DESC SEPARATOR ' ')")
            .group("name")
            .toString());
    },
    
    // profile: data/test-msoffice-20231219.json
    "open_excel_file": function() {
        var Office = require("lib/msoffice");
        var excel = new Office.Excel();   // Create an Excel instance
        excel.open("data\\example.xlsx");   // Open the Excel window
    },
    
    "open_excel_with_chatgpt": function() {
        // Load libraries
        var Office = require("lib/msoffice");
        var ChatGPT = require("lib/chatgpt");

        // Create an Excel instance
        var excel = new Office.Excel();

        // List of questions
        var questions = [
            "Which one does Mom like, and which one does Dad like?",
            "If 100 billion won is deposited into my bank account without my knowledge, what would I do?",
            "If my friend passed out from drinking, and Arnold Schwarzenegger suggests having a drink together alone, is it okay to ditch my friend and go with him?",
            "If there's a snake in our tent during the company camping trip, should I wake up the manager, or should I escape on my own without waking him up?"
        ];

        // Open the Excel window
        excel.open();
        
        // Answer to questions
        var i = 1;
        questions.forEach(function(x) {
            var answer = ChatGPT.chat(x);
            console.log("Answer:", answer);
            excel.getCellByPosition(i, 1).setValue(answer);
            i++;
        });

        // Close the Excel window
        //excel.close();
    },
    
    "open_powerpoint_file": function() {
        var Office = require("lib/msoffice");   // Load libraries
        var powerpoint = new Office.PowerPoint();   // Create a PowerPoint instance
        powerpoint.open("data\\example.pptx");   // Open the PowerPoint window
    },
    
    "open_word_file": function() {
        var Office = require("lib/msoffice");    // Load libraries
        var word = new Office.Word();   // Create an Word instance
        word.open("data\\example.docx");   // Open the Word window
    },
    
    "sharedmemory": function() {
        var Toolkit = require("lib/toolkit");
        var mem = new Toolkit.NamedSharedMemory("welsonjs_test");

        console.log("Writing a text the to shared memory...");
        mem.writeText("nice meet you");
        
        console.log("Reading a text from the shared memory...");
        console.log(mem.readText() + ", too");
        
        console.log("Cleaning the shared memory...");
        mem.clear()
        console.log(mem.readText());

        mem.close()
        console.log("Closing the shared memory...");

        console.log("Done");
    },
    
    "sharedmemory_write": function() {
        var Toolkit = require("lib/toolkit");
        var mem = new Toolkit.NamedSharedMemory("welsonjs_test");

        console.log("Writing a text to the shared memory...");
        mem.writeText("nice meet you");

        console.log("Done");
    },

    "sharedmemory_read": function() {
        var Toolkit = require("lib/toolkit");
        var mem = new Toolkit.NamedSharedMemory("welsonjs_test");

        console.log("Reading a text from the shared memory...");
        console.log(mem.readText() + ", too");

        console.log("Cleaning the shared memory...");
        mem.clear()
        console.log(mem.readText());

        mem.close()
        console.log("Closing the shared memory...");

        console.log("Done");
    },

    "sharedmemory_listener": function() {
        var Toolkit = require("lib/toolkit");
        var targets = (function() {
            var s = Toolkit.prompt("Input the shared memory names (Comma seperated)");
            return s.split(',');
        })();

        if (!targets) {
            console.log("Aborted.");
        } else {
            // Open the shared memory
            var memories = targets.map(function(x) {
                return [x, new Toolkit.NamedSharedMemory(x)];
            });

            // Open the second process will be communicate
            Toolkit.openProcess();

            // Listen the shared memory
            console.log("Listening the shared memory:", targets.join(', '));
            while (true) {
                console.log(new Date().toISOString());
                memories.forEach(function(x) {
                    var name = x[0];
                    var mem = x[1];
                    console.log(name + ": ", mem.readText());
                });
                sleep(100);
            }
        }
    },

    "string_split": function() {
        var a = "monkey:red:apple:delicious:banana:long:train:fast:airplane:high:everest:sharp:seringue:painful";
        var b = a.split(':').join(':');
        var c = "a=1=b=2=c=3";
        var d = c.split('=').join('=');
        
        if (a == b && c == d) {
            console.log("PASS");
        } else {
            console.log("FAILED");
            console.log(a);
            console.log(b);
            console.log(c);
            console.log(d);
        }
    }
};

function main(args) {
    // EXAMPLE: cscript app.js testloader <es5_polyfills> <data\test-oss-20231030.json>
    if (args.length > 0) {
        var test_id = args[0];
        var profilefile = args[1];

        // load the test profile
        var profile = JSON.parse(FILE.readFile(profilefile, FILE.CdoCharset.CdoUTF_8));

        // do test
        if (test_id in test_implements) {
            var test = profile.tests.find(function(x) {
                return (x.id == test_id);
            });
            var description = test.description;

            console.log("Test ID: " + test_id);
            console.log("Will be test: " + description);

            try {
                test_implements[test_id]();
            } catch (e) {
                console.error("ERROR: " + e.message)
            }

            console.log("Will be close this window after 90 seconds");
            sleep(90 * 1000);
        }
    } else {
        console.log("hello world!");
    }
}

exports.main = main;
