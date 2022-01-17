[string]$sEncodedString=$args[0]
$sDecodedString=[System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($sEncodedString))
write-host "Encoded String:" $sDecodedString
