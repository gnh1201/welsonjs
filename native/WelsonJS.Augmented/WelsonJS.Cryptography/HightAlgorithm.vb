' HightAlgorithm.vb (WelsonJS.Cryptography)
' SPDX-License-Identifier: MIT
' SPDX-FileCopyrightText: 2025 Namhyeon Go <gnh1201@catswords.re.kr>, Catswords OSS And WelsonJS Contributors
' https://github.com/gnh1201/welsonjs
'
Imports System.Security.Cryptography

''' <summary>
''' HIGHT (TTAS.KO-12.0040/R1, ISO/IEC 18033-3), a 64-bit block cipher with a 128-bit key, as a
''' SymmetricAlgorithm. Through Mode: ECB (the default), CBC, CFB, OFB and CTS; through their own
''' methods: CTR and CMAC. GCM and CCM need a 128-bit block. See <see cref="BlockCipherAlgorithm"/>.
''' </summary>
Public Class HightAlgorithm
    Inherits BlockCipherAlgorithm

    Public Sub New()
        LegalBlockSizesValue = New KeySizes() {New KeySizes(64, 64, 0)}
        LegalKeySizesValue = New KeySizes() {New KeySizes(128, 128, 0)}

        Me.BlockSize = 64
        Me.KeySize = 128
        Me.FeedbackSize = 64

        Me.Mode = CipherMode.ECB
        Me.Padding = PaddingMode.PKCS7

        Me.Key = New Byte(15) {}
        Me.IV = New Byte(7) {}
    End Sub

    Protected Overrides Function CreateCipherCore(key() As Byte) As IBlockCipher
        Return New HightCore(key)
    End Function
End Class
