' SeedAlgorithm.vb (WelsonJS.Cryptography)
' SPDX-License-Identifier: MIT
' SPDX-FileCopyrightText: 2025 Namhyeon Go <gnh1201@catswords.re.kr>, Catswords OSS And WelsonJS Contributors
' https://github.com/gnh1201/welsonjs
' 
Imports System.Security.Cryptography

Public Class SeedAlgorithm
    Inherits SymmetricAlgorithm

    Public Sub New()
        LegalBlockSizesValue = New KeySizes() {New KeySizes(128, 128, 0)}
        LegalKeySizesValue = New KeySizes() {New KeySizes(128, 128, 0)}

        Me.BlockSize = 128
        Me.KeySize = 128
        Me.FeedbackSize = 128

        Me.Mode = CipherMode.ECB
        Me.Padding = PaddingMode.PKCS7

        Me.Key = New Byte(15) {}
        Me.IV = New Byte(15) {}
    End Sub

    Public Overrides Sub GenerateKey()
        Using rng As New RNGCryptoServiceProvider()
            rng.GetBytes(Me.Key)
        End Using
    End Sub

    Public Overrides Sub GenerateIV()
        Using rng As New RNGCryptoServiceProvider()
            rng.GetBytes(Me.IV)
        End Using
    End Sub

    Public Overrides Function CreateEncryptor(rgbKey As Byte(), rgbIV As Byte()) As ICryptoTransform
        Return CreateTransform(rgbKey, rgbIV, True)
    End Function

    Public Overrides Function CreateDecryptor(rgbKey As Byte(), rgbIV As Byte()) As ICryptoTransform
        Return CreateTransform(rgbKey, rgbIV, False)
    End Function

    Private Function CreateTransform(key As Byte(), iv As Byte(), encrypt As Boolean) As ICryptoTransform
        Select Case Me.Mode
            Case CipherMode.ECB
                Return New SeedEcbTransform(key, encrypt, Me.Padding)
            Case Else
                Throw New NotSupportedException("This mode not supported yet")
        End Select
    End Function

    ' TODO: CCM, GCM, CMAC
End Class