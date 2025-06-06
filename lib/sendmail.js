// sendmail.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
// Sendmail using either CDO or Persits.MailSender
//
/////////////////////////////////////////////////////////////////////////////////
// Private APIs / Utility functions
/////////////////////////////////////////////////////////////////////////////////

var usePersitsMailSender = false;
var useSSL = false;

/////////////////////////////////////////////////////////////////////////////////
// Send Mail Message
/////////////////////////////////////////////////////////////////////////////////

function sendmail(msg) {
    var ok, MAIL;

    console.log("SENDMAIL: " + msg.To);

    // Which method we use depends on the system.  On some versions of
    // Persits.MailSender it does not support adding of Message-ID
    // so we have to use CDO (which is the preferred option anyway).
    if (exports.usePersitsMailSender) {

        // Use Persits AspEmail to send mail
        try {
            MAIL = CreateObject("Persits.MailSender");
        } catch (e) {
            console.log("ERROR " + e.number + ", " + e.description);
            throw e;
        }

        console.log("USING PERSITS MAIL SENDER");
        console.log("MAIL FROM " + msg.From);
        console.log("MAIL TO " + msg.To);
        console.log("SUBJECT " + msg.Subject);

        MAIL.Host = msg.MAILHOST;
        MAIL.From = msg.From;
        if (msg.Name) MAIL.FromName = msg.Name;
        MAIL.AddAddress(msg.To);
        MAIL.Subject = msg.Subject;
        if (msg.cc) MAIL.AddCC(msg.Cc);
        MAIL.IsHTML = msg.IsHTML;
        MAIL.Body = msg.Body;
        MAIL.addCustomHeader("Reply-To: <" + msg.ReplyTo + ">");
        console.log("Reply-To: <" + msg.ReplyTo + ">");
        if (msg.id) {
            console.log("Message-ID: <" + msg.id + ">");
            MAIL.addCustomHeader("Message-ID: <" + msg.id + ">");
        }

    } else {

        // Use CDO objects to send mail.  Setup SMTP server details.
        var CONF = CreateObject("CDO.Configuration");
        CONF.Fields("http://schemas.microsoft.com/cdo/configuration/sendusing") = 2;
        CONF.Fields("http://schemas.microsoft.com/cdo/configuration/smtpserver") = msg.MAILHOST;
        CONF.Fields("http://schemas.microsoft.com/cdo/configuration/smtpserverport") = msg.MAILPORT || 25;
        CONF.Fields("http://schemas.microsoft.com/cdo/configuration/smtpauthenticate") = 0;
        CONF.Fields("http://schemas.microsoft.com/cdo/configuration/smtpusessl") = (!useSSL ? 0 : 1);
        CONF.Fields.Update();

        // Create the email message
        MAIL = CreateObject("CDO.Message");
        MAIL.Configuration = CONF;
        CONF = null;
        if (msg.Name) {
            MAIL.From = '"' + msg.Name + '" <' + msg.From + '>';
        } else {
            MAIL.From = msg.From;
        }
        MAIL.To = msg.To;
        if (msg.Cc) MAIL.Cc = msg.cc;
        MAIL.Subject = msg.Subject;
        MAIL.Fields("urn:schemas:mailheader:reply-to") = "<" + msg.ReplyTo + ">";
        MAIL.Fields("urn:schemas:mailheader:message-id") = "<" + msg.id + ">";
        if (msg.IsHTML) {
            MAIL.HTMLBody = msg.Body;
        } else {
            MAIL.TextBody = msg.Body;
        }
        MAIL.Fields.Update();
    }

    try {
        console.log("Sending email To " + msg.To + (msg.Cc ? " (Cc " + msg.Cc + ")" : ""));
        MAIL.Send();
        ok = true;
    } catch (e) {
        console.log(e.number + "," + e.description);
        ok = false;
        console.log("failed");
    }

    MAIL = null;
    return ok;
};

exports.sendmail = sendmail;

exports.VERSIONINFO = "Sendmail Library (sendmail.js) version 0.1.1";
exports.global = global;
exports.require = global.require;
