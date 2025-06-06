// msmq.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
// references:
// https://gist.github.com/vladaman/6375841
// https://docs.microsoft.com/en-us/previous-versions/windows/desktop/msmq/ms703952(v=vs.85)
// https://docs.microsoft.com/en-us/previous-versions/windows/desktop/msmq/ms705286(v=vs.85)
// 
var MSMQObject = function(queueName) {
    var MQ_SEND_ACCESS = 2;
    var MQ_DENY_NONE = 0;
    var MQ_RECEIVE_ACCESS = 1;

    var MQ_NO_TRANSACTION = 0;
    var MQ_MTS_TRANSACTION = 1;
    var MQ_SINGLE_MESSAGE = 3;

    this.machineName = "localhost";
    this.queueName = queueName;

    this.queueInfo = null;
    this.queue = null;

    this.create = function() {
        try {
            this.queueInfo = CreateObject("MSMQ.MSMQQueueInfo");
            this.queueInfo.PathName = ".\\private$\\" + this.queueName;
        } catch (e) {
            console.error("MSMQObject.create() ->", e.message);
        }
        return this;
    }

    this.open = function() {
        this.queue = this.queueInfo.Open(MQ_SEND_ACCESS, MQ_DENY_NONE);
        return this;
    }

    // For example: .send("Sample Body", "LabelXX");
    this.send = function(body, label) {
        var mqmsg = CreateObject("MSMQ.MSMQMessage");
        mqmsg.Body = body 
        mqmsg.Label = label 
        mqmsg.Send(this.queue);
        return mqmsg;
    };

    this.recv = function() {
        return this.queue.Receive(MQ_NO_TRANSACTION, false, true, 20000, false);
    };

    this.create();
};

exports.MSMQObject = MSMQObject;
exports.open = function(queueName) {
    return (new MSMQObject(queueName)).open();
};
