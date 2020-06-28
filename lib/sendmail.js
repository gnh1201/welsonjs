//////////////////////////////////////////////////////////////////////////////////
//
//	sendmail.js
//
//	Sendmail using either CDO or Persits.MailSender
//
/////////////////////////////////////////////////////////////////////////////////

var LIB = require('lib/std');

/////////////////////////////////////////////////////////////////////////////////
// Private APIs / Utility functions
/////////////////////////////////////////////////////////////////////////////////

var module = { global: global, require: global.require };
module.VERSIONINFO = "sendmail Lib (sendmail.js) version 0.1";

/////////////////////////////////////////////////////////////////////////////////
// Send Mail Message
/////////////////////////////////////////////////////////////////////////////////

module.sendmail = function(msg) {
  var ok, MAIL;

  DBG("SENDMAIL: " + msg.To);

  // Which method we use depends on the system.  On some versions of
  // Persits.MailSender it does not support adding of Message-ID
  // so we have to use CDO (which is the preferred option anyway).
  if (module.usePersitsMailSender) {

    // Use Persits AspEmail to send mail
    try {
      MAIL = module.CreateObject("Persits.MailSender");
    } catch(e) {
      DBG("ERROR " + e.number + ", " + e.description);
      throw e;
    }

    DBG("USING PERSITS MAIL SENDER");
    DBG("MAIL FROM " + msg.From);
    DBG("MAIL TO " + msg.To);
    DBG("SUBJECT " + msg.Subject);

    MAIL.Host = msg.MAILHOST;
    MAIL.From = msg.From;
    if (msg.Name) MAIL.FromName = msg.Name;
    MAIL.AddAddress(msg.To);
    MAIL.Subject = msg.Subject;
    if (msg.cc) MAIL.AddCC(msg.Cc);
    MAIL.IsHTML = msg.IsHTML;
    MAIL.Body = msg.Body;
    MAIL.addCustomHeader("Reply-To: <" + msg.ReplyTo + ">");
    DBG("Reply-To: <" + msg.ReplyTo + ">");
    if (msg.id) {
      DBG("Message-ID: <" + msg.id + ">");
      MAIL.addCustomHeader("Message-ID: <" + msg.id + ">");
    }

  } else {

    // Use CDO objects to send mail.  Setup SMTP server details.
    var CONF = LIB.CreateObject("CDO.Configuration");
    CONF.Fields("http://schemas.microsoft.com/cdo/configuration/sendusing") = 2;
    CONF.Fields("http://schemas.microsoft.com/cdo/configuration/smtpserver") = msg.MAILHOST;
    CONF.Fields("http://schemas.microsoft.com/cdo/configuration/smtpserverport") = msg.MAILPORT || 25;
    CONF.Fields("http://schemas.microsoft.com/cdo/configuration/smtpauthenticate") = 0;
    CONF.Fields("http://schemas.microsoft.com/cdo/configuration/smtpusessl") = 0;
    CONF.Fields.Update();

    // Create the email message
    MAIL = LIB.CreateObject("CDO.Message");
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
    DBG("Sending email To " + msg.To + (msg.Cc ? " (Cc " + msg.Cc + ")" : ""));
    MAIL.Send();
    ok = true;
  } catch(e) {
    DBG(e.number + "," + e.description);
    ok = false;
    DBG("failed");
  }

  MAIL = null;
  return ok;
};

/////////////////////////////////////////////////////////////////////////////////

return module;
