' AriaAlgorithm.vb (WelsonJS.Cryptography)
' SPDX-License-Identifier: MIT
' SPDX-FileCopyrightText: 2025 Namhyeon Go <gnh1201@catswords.re.kr>, Catswords OSS And WelsonJS Contributors
' https://github.com/gnh1201/welsonjs
'
Imports System.Security.Cryptography

''' <summary>
''' ARIA (KS X 1213, RFC 5794), a 128-bit block cipher with a 128, 192 or 256-bit key, as a
''' SymmetricAlgorithm. Through Mode: ECB (the default), CBC, CFB, OFB and CTS; through their own
''' methods: CTR, GCM, CCM and CMAC. See <see cref="BlockCipherAlgorithm"/>.
''' </summary>
Public Class AriaAlgorithm
    Inherits BlockCipherAlgorithm

    Public Sub New()
        LegalBlockSizesValue = New KeySizes() {New KeySizes(128, 128, 0)}
        LegalKeySizesValue = New KeySizes() {New KeySizes(128, 256, 64)}

        Me.BlockSize = 128
        Me.KeySize = 128
        Me.FeedbackSize = 128

        Me.Mode = CipherMode.ECB
        Me.Padding = PaddingMode.PKCS7

        Me.Key = New Byte(15) {}
        Me.IV = New Byte(15) {}
    End Sub

    Protected Overrides Function CreateCipherCore(key() As Byte) As IBlockCipher
        Return New AriaCore(key)
    End Function
End Class
