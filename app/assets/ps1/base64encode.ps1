[string]$sStringToEncode=$args[0]
$sEncodedString=[Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($sStringToEncode))
write-host $sEncodedString