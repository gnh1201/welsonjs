' HightEcbTransform.vb (WelsonJS.Cryptography)
' SPDX-License-Identifier: MIT
' SPDX-FileCopyrightText: 2025 Namhyeon Go <gnh1201@catswords.re.kr>, Catswords OSS And WelsonJS Contributors
' https://github.com/gnh1201/welsonjs
'
Imports System.Security.Cryptography

''' <summary>
''' HIGHT in ECB mode as an ICryptoTransform; kept for compatibility. It is
''' <see cref="BlockCipherTransform"/> with <see cref="BlockCipherMode.ECB"/>.
''' </summary>
Public Class HightEcbTransform
    Inherits BlockCipherTransform

    Public Sub New(key As Byte(), encryptMode As Boolean, Optional mode As PaddingMode = PaddingMode.PKCS7)
        MyBase.New(New HightCore(key), BlockCipherMode.ECB, Nothing, encryptMode, mode)
    End Sub
End Class
