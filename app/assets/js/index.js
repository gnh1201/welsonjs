var FILE = require("lib/file");
var serverUrl = "http://96.30.198.29/";

$(document).ready(function() {
    $(".logoutbox").css("display", "none");

    $("#loginform").ajaxForm({
        success: function(res) {
            if(res.success === false) {
                alert("로그인에 실패하였습니다. 다시 시도하여 주세요");
            } else {
                // 로그인 사용자의 토큰을 저장
                FILE.writeFile("token.txt", res.data.token, "utf-8");

                // 저장 여부 확인
                var isTokenExists = FILE.fileExists("token.txt");
                if(isTokenExists) {
                    // 성공하면 아이디 표시
                    $("#logged_username").text(res.data.username);

                    // 화면 전환
                    $(".loginbox").css("display", "none");
                    $(".logoutbox").css("display", "");
                } else {
                    alert("저장공간이 충분한지 확인하여 주세요");
                }
            }
        },
        error: function() {
            alert("잠시 후 다시 시도하여 주세요");
        }
    });

    // 기존 토큰 정보가 있는 경우
    var isTokenExists = FILE.fileExists("token.txt");
    if(isTokenExists) {
        var token = FILE.readFile("token.txt", "utf-8");
        $.get(serverUrl, {
            route: "api.authenticate",
            action: "checkToken",
            token: token
        }, function(res) {
            // 토큰 확인에 성공하면 아이디 표시
            $("#logged_username").text(res.data.username);

            // 화면 전환
            $(".loginbox").css("display", "none");
            $(".logoutbox").css("display", "");
        });
    }
    
    // 로그아웃
    $("#btn_logout").click(function() {
        // 토큰 파일 삭제
        FILE.deleteFile("token.txt");
        
        // todo: expire token in the database
        
        // 화면 전환
        $(".loginbox").css("display", "");
        $(".logoutbox").css("display", "none");

        alert("로그아웃 되었습니다.");
    });
});
