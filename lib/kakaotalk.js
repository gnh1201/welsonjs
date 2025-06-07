// kakaotalk.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
// KakaoTalk (Instant Messaging Service in South Korea) automation testing (PoC) library
// 
var Toolkit = require("lib/toolkit");
var AutoIt = require("lib/autoit");

function KakaoTalk() {
    this.lastTemplate = "";
    this.nextTemplate = "wall.png";
    this.oAutoIt = AutoIt.create();

    // 사용할 메시지 지정
    this.message = {
        start: "ㅎㄱㅎㅈ",    // 하트 교환을 요청하는 메시지
        clipboardPaste: "^v",    // 클립보드에 복사된 내용(이미지)를 붙여넣기
        pushed: "ㄴ"   // 하트 번호 인증 (스크린샷 전송) 후 보낼 메시지. **카톡은 답글 모드에선 그림 첨부를 지원하지 않음**
    };
    this.setMessage = function(messageType, messageText) {
        this.message[messageType] = messageText;
    };

    // 템플릿 제어
    this.setLastTemplate = function(templateName) {
        this.lastTemplate = templateName;
    };
    this.setNextTemplate = function(templateName) {
        this.nextTemplate = templateName;
    };

    // x: 화면 좌표 X, y: 화면 좌표 Y, dx: 화면 좌표 X 보정값, dy: 화면 좌표 Y 보정값
    this.mouseMove = function(x, y, dx, dy) {
        this.oAutoIt.mouseMove(x + dx, y + dy);
    };

    // x: 화면 좌표 X, y: 화면 좌표 Y, dx: 화면 좌표 X 보정값, dy: 화면 좌표 Y 보정값
    this.mouseClick = function(x, y, dx, dy) {
        this.mouseMove(x, y, dx, dy);
        this.oAutoIt.mouseClick("left");
    };
    
    this.sendKeys = function(keys) {
        this.oAutoIt.send(keys);
    };

    this.check = function(params) {
        // 메시지 띄우기 (값이 정상적으로 들어오는지 확인용)
        //Toolkit.alert(JSON.stringify(params));

        // 전달 받은 좌표를 숫자로 변환
        var x = parseInt(params.position.x);
        var y = parseInt(params.position.y);
        
        // 템플릿 이름
        var templateName = params.fileName;

        // 각 클릭 단계별 처리
        switch (templateName) {
            case "wall.png":   // 입력창 클릭
                // 초기 상태에서 들어온 경우
                if (this.lastTemplate == "") {
                    this.mouseClick(x, y, -32, 32);   // 채팅창 클릭
                    sleep(1000);   // 1초 기다림
                    this.sendKeys(this.message.start);   // 메시지 입력
                    this.setLastTemplate(templateName);  // 현재 상태 저장
                    this.setNextTemplate("button_message_send.png");  // 다음 상태 설정
                }
                
                // 하트를 누르고 프로필 창 닫기까지 완료한 경우
                if (this.lastTemplate == "binary_button_close_profile_blured.png") {
                    this.mouseClick(x, y, -32, 32);   // 채팅창 클릭
                    sleep(1000);   // 1초 기다림
                    
                    this.sendKeys(this.message.clipboardPaste);   // 붙여넣기 명령
                    this.setLastTemplate(templateName);  // 현재 상태 저장
                    this.setNextTemplate("button_clipboard_send.png");  // 다음 상태 설정
                }    

                break;
            
            case "wall_reply_ready.png":
                // 하트 누른거 인증
                if (this.lastTemplate == "button_reply.png") {
                    this.mouseClick(x, y, -32, 56 + 32);   // 채팅창 클릭
                    sleep(1000);   // 1초 기다림
                    this.sendKeys(this.message.pushed);   // "눌렀다"는 메시지 입력
                    this.setLastTemplate(templateName);  // 현재 상태 저장
                    this.setNextTemplate("button_message_send.png");  // 다음 상태 설정
                }
                break;

            // 클립보드 전송
            case "button_clipboard_send.png":
                if (this.lastTemplate == "wall.png") {
                    this.mouseClick(x, y, 0, 0);   // 클립보드 전송 버튼 클릭
                    sleep(1000);   // 1초 기다림
                    this.setLastTemplate(templateName);  // 현재 상태 저장
                    this.setNextTemplate("mention_reply.png:-1");  // 기억해두었던 "나에게 답장" 위치 다시 찾기
                }
                break;
            
            case "button_message_send.png":   // 메시지 전송 버튼 찾았을 때
                // 이전 작업이 신규 메시지 전송일 때
                if (this.lastTemplate == "wall.png") {
                    this.mouseClick(x, y, 0, 0);   // 메시지 전송 버튼 클릭
                    sleep(1000);   // 1초 기다림
                    this.setLastTemplate(templateName);  // 현재 상태 저장
                    this.setNextTemplate("mention_reply.png");  // 다음 상태 설정
                }
                
                // 이전 작업이 답글 전송일 때
                if (this.lastTemplate == "wall_reply_ready.png") {
                    this.mouseClick(x, y, 0, 0);   // 메시지 전송 버튼 클릭
                    sleep(1000);   // 1초 기다림
                    this.setLastTemplate(templateName);  // 현재 상태 저장

					/**** 회기점 (여기서는 "ㅎㄱㅎㅈ" 메시지를 이미 보낸 상태이므로, 다시 메시지를 보내지 않고 다른 응답을 기다림.) ****/
                    this.setNextTemplate("mention_reply.png");  // 다음 상태 설정
                }
                break;
                
            case "mention_reply.png":  // 답글이 도착했을 때
                // 이전 작업이 메시지 전송 버튼 클릭이었을 때에만 진행
                if (this.lastTemplate == "button_message_send.png") {
                    this.mouseClick(x, y, -40, -9);   // 아바타 클릭
                    sleep(1000);   // 1초 기다림
                    this.setLastTemplate(templateName);  // 현재 상태 저장
                    this.setNextTemplate("binary_button_view_profile.png");  // 다음 상태 설정
                }
                
                // 하트 찍기를 완료하고 "나에게 답장" 메시지를 다시 찾은 경우 진행
                if (this.lastTemplate == "button_clipboard_send.png") {
                    this.mouseMove(x, y, 100, 30);   // 해당 메시지로 커서 이동
                    sleep(1000);   // 1초 기다림
                    this.setLastTemplate(templateName);  // 현재 상태 저장
                    this.setNextTemplate("button_reply.png");  // 다음 상태 설정
                }
                break;
                
            case "button_reply.png":   // 답글 버튼을 찾았을 때
                // "나에게 답장" 메시지를 다시 찾은 뒤, 답글 버튼을 눌렀을 때에만 진행
                if (this.lastTemplate == "mention_reply.png") {
                    this.mouseClick(x, y, 5, 5);   // 답글 버튼 클릭
                    sleep(1000);   // 1초 기다림
                    this.setLastTemplate(templateName);  // 현재 상태 저장
                    this.setNextTemplate("wall_reply_ready.png");  // 다음 상태 설정
                }
                break;

            case "binary_button_view_profile.png":  // 프로필 보기 버튼을 찾았을 때
                // 답글이 온 경우에만 진행
                if (this.lastTemplate == "mention_reply.png") {
                    this.mouseClick(x, y, 0, 0);   // 프로필 보기 버튼 클릭
                    sleep(1000);   // 1초 기다림
                    this.setLastTemplate(templateName);  // 현재 상태 저장
                    this.setNextTemplate("button_heart.png");  // 다음 상태 설정
                }
                break;

            case "button_heart.png":  // 하트 버튼 찾았을 때
                if (this.lastTemplate == "binary_button_view_profile.png") {
                    this.mouseClick(x, y, 0, 0);   // 하트 버튼 클릭
                    sleep(1000);   // 1초 기다림
                    this.setLastTemplate(templateName);  // 현재 상태 저장
                    this.setNextTemplate("button_heart_clicked.png");  // 다음 상태 설정
                }
                break;

            case "button_heart_clicked.png":  // 하트 버튼 클릭했을 때
                if (this.lastTemplate == "button_heart.png") {
                    this.setLastTemplate(templateName);  // 현재 상태 저장
                    this.setNextTemplate("binary_button_close_profile_blured.png");  // 다음 상태 설정
                }
                break;
            
            case "binary_button_close_profile_blured.png":    // 프로필 창을 닫기
                if (this.lastTemplate == "button_heart_clicked.png") {    // 하트 버튼을 찍고 들어왔으면
                    this.mouseClick(x, y, 3, 3);   // 프로필 창 닫기 버튼 클릭
                    sleep(1000);   // 1초 기다림
                    this.setLastTemplate(templateName);  // 현재 상태 저장
                    this.setNextTemplate("wall.png");  // 다음 상태 설정
                }
                break;
        }
    };
}

exports.KakaoTalk = KakaoTalk;
