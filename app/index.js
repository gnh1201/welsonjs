var global = {};

var FILE = require("lib/file");
var SSLoader = require("ssloader");

__global.processNames = [];
__global.serverNames = [];

var loginSuccess = function(res) {
    // 성공하면 아이디 표시
    $("#logged_username").text(res.data.username);
    
    // 서버 및 프로세스 정보 수신
    var token = FILE.readFile("token.txt", "utf-8");
    $.get(__config.webapp.baseURL, {
        route: "detail.user",
        action: "api",
        token: token
    }, function(res) {
        // 사용자 프로세스 조회
        var $userProcesses = $("#userProcesses");
        for (var i in res.processes) {
            var row = res.processes[i];
            $userProcesses.append('<li class="show-logged remove-when-logout"><span><img src="app/assets/img/gear-2-16.png" alt="Process" /> ' + row.processname + '</span></li>');
            __global.processNames.push(row.processname);
        }

        // 사용자 서버 조회
        var $userServers = $("#userServers");
        for (var i in res.serverusers) {
            var row = res.serverusers[i];
            $userServers.append('<li class="show-logged remove-when-logout"><span><img src="app/assets/img/server-16.png" alt="Server" /> ' + row.servername + '</span></li>');
            __global.serverNames.push(row.servername);
        }
    });

    // 화면 전환
    $(".show-no-logged").css("display", "none");
    $(".show-logged").css("display", "");
};

$(document).ready(function() {
    $(".show-logged").css("display", "none");

    $("#loginform").attr("action", __config.webapp.baseURL);
    $("#loginform").ajaxForm({
        beforeSubmit: function() {
            console.info("로그인을 시도합니다. 잠시만 기다려 주세요.");
        },
        success: function(res) {
            if(res.success === false) {
                console.info("로그인에 실패하였습니다. 다시 시도하여 주세요");
            } else {
                // 로그인 사용자의 토큰을 저장
                FILE.writeFile("token.txt", res.data.token, "utf-8");

                // 저장 여부 확인
                var isTokenExists = FILE.fileExists("token.txt");
                if(isTokenExists) {
                    loginSuccess(res); // 로그인 성공 시 수행
                } else {
                    console.info("저장공간이 충분한지 확인하여 주세요");
                }
            }
        },
        error: function(xhr, status, error) {
			 var errorMessage = xhr.status + ': ' + xhr.statusText;
             console.info(xhr.responseText);
			 console.info('Error: ' + errorMessage);
        }
    });

    // 기존 토큰 정보가 있는 경우
    var isTokenExists = FILE.fileExists("token.txt");
    if(isTokenExists) {
        var token = FILE.readFile("token.txt", "utf-8");
        $.get(__config.webapp.baseURL, {
            route: "api.auth.json",
            action: "checkToken",
            token: token
        }, function(res) {
            loginSuccess(res); // 로그인 성공 시 수행
        });
    }

    // 닫기
    $("a[href='#exit']").click(function() {
        exit();
    });

    // 로그아웃
    $("#btn_logout").click(function() {
        // 토큰 파일 삭제
        FILE.deleteFile("token.txt");

        // 삭제할 객체를 모두 찾아 지움
        $(".remove-when-logout").remove();

        // 화면 전환
        $(".loginbox").css("display", "");
        $(".logoutbox").css("display", "none");
        $(".show-no-logged").css("display", "");
        $(".show-logged").css("display", "none");

        console.info("로그아웃 되었습니다.");
    });

    // 연결
    $("#btn_connect").click(function() {
        var isTokenExists = FILE.fileExists("token.txt");
        if(isTokenExists) {
            SSLoader.main();
        } else {
            console.info("로그인을 먼저 진행하여 주세요.");
        }
    });

    // 종료
    $("#btn_disconnect").click(function() {
        // todo
    });
});
