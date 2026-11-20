' BlockCipherAlgorithm.vb (WelsonJS.Cryptography)
' SPDX-License-Identifier: MIT
' SPDX-FileCopyrightText: 2025 Namhyeon Go <gnh1201@catswords.re.kr>, Catswords OSS And WelsonJS Contributors
' https://github.com/gnh1201/welsonjs
'
Imports System.Security.Cryptography

''' <summary>
''' The SymmetricAlgorithm of a block cipher of this library. CreateEncryptor and
''' CreateDecryptor support every CipherMode: ECB, CBC, CFB (FeedbackSize 8 to the block
''' size), OFB and CTS (ciphertext stealing, CS3). The other modes have their own methods:
''' <see cref="CreateCtrTransform()"/> (CTR), <see cref="CreateGcm()"/> and
''' <see cref="CreateCcm()"/> (authenticated encryption, 128-bit blocks only) and
''' <see cref="CreateCmac()"/> (message authentication).
''' </summary>
Public MustInherit Class BlockCipherAlgorithm
    Inherits SymmetricAlgorithm

    ''' <summary>Creates the block cipher for a key whose size has been checked.</summary>
    Protected MustOverride Function CreateCipherCore(key() As Byte) As IBlockCipher

    ''' <summary>Creates the block cipher with the current key.</summary>
    Public Function CreateBlockCipher() As IBlockCipher
        Return CreateBlockCipher(Me.Key)
    End Function

    ''' <summary>Creates the block cipher with the given key.</summary>
    Public Function CreateBlockCipher(key() As Byte) As IBlockCipher
        If key Is Nothing Then Throw New ArgumentNullException(NameOf(key))
        If Not ValidKeySize(key.Length * 8) Then
            Throw New CryptographicException("Specified key is not a valid size for this algorithm.")
        End If
        Return CreateCipherCore(CType(key.Clone(), Byte()))
    End Function

    ''' <summary>
    ''' The mode for CreateEncryptor and CreateDecryptor: ECB, CBC, CFB, OFB or CTS. Overridden
    ''' because the SymmetricAlgorithm of .NET Core and .NET 5+ accepts only ECB, CBC and CFB.
    ''' </summary>
    Public Overrides Property Mode As CipherMode
        Get
            Return ModeValue
        End Get
        Set(value As CipherMode)
            Select Case value
                Case CipherMode.ECB, CipherMode.CBC, CipherMode.CFB, CipherMode.OFB, CipherMode.CTS
                    ModeValue = value
                Case Else
                    Throw New CryptographicException("Specified cipher mode is not valid for this algorithm.")
            End Select
        End Set
    End Property

    Public Overrides Sub GenerateKey()
        KeyValue = RandomBytes(KeySizeValue \ 8)
    End Sub

    Public Overrides Sub GenerateIV()
        IVValue = RandomBytes(BlockSizeValue \ 8)
    End Sub

    Public Overrides Function CreateEncryptor(rgbKey() As Byte, rgbIV() As Byte) As ICryptoTransform
        Return BuildTransform(rgbKey, rgbIV, True)
    End Function

    Public Overrides Function CreateDecryptor(rgbKey() As Byte, rgbIV() As Byte) As ICryptoTransform
        Return BuildTransform(rgbKey, rgbIV, False)
    End Function

    Private Function BuildTransform(key() As Byte, iv() As Byte, encrypt As Boolean) As ICryptoTransform
        Dim selected As BlockCipherMode
        Select Case Me.Mode
            Case CipherMode.ECB
                selected = BlockCipherMode.ECB
            Case CipherMode.CBC
                selected = BlockCipherMode.CBC
            Case CipherMode.CFB
                selected = BlockCipherMode.CFB
            Case CipherMode.OFB
                selected = BlockCipherMode.OFB
            Case CipherMode.CTS
                selected = BlockCipherMode.CTS
            Case Else
                Throw New NotSupportedException("Unsupported cipher mode: " & Me.Mode.ToString())
        End Select
        Return New BlockCipherTransform(CreateBlockCipher(key), selected, iv, encrypt, Me.Padding, Me.FeedbackSize)
    End Function

    ''' <summary>Creates a CTR transform with the current key, and the current IV as the first counter block. CTR encrypts and decrypts alike.</summary>
    Public Function CreateCtrTransform() As ICryptoTransform
        Return CreateCtrTransform(Me.Key, Me.IV)
    End Function

    ''' <summary>Creates a CTR transform with the given key and first counter block (one block long). CTR encrypts and decrypts alike.</summary>
    Public Function CreateCtrTransform(rgbKey() As Byte, rgbCounter() As Byte) As ICryptoTransform
        Return New BlockCipherTransform(CreateBlockCipher(rgbKey), BlockCipherMode.CTR, rgbCounter, True, PaddingMode.None)
    End Function

    ''' <summary>Creates a transform for any <see cref="BlockCipherMode"/>, CTR included.</summary>
    Public Function CreateTransform(rgbKey() As Byte, rgbIV() As Byte, operationMode As BlockCipherMode, encrypt As Boolean) As ICryptoTransform
        Return New BlockCipherTransform(CreateBlockCipher(rgbKey), operationMode, rgbIV, encrypt, Me.Padding, Me.FeedbackSize)
    End Function

    ''' <summary>Creates GCM with the current key. Needs a 128-bit block (not HIGHT).</summary>
    Public Function CreateGcm() As BlockCipherGcm
        Return CreateGcm(Me.Key)
    End Function

    ''' <summary>Creates GCM with the given key. Needs a 128-bit block (not HIGHT).</summary>
    Public Function CreateGcm(rgbKey() As Byte) As BlockCipherGcm
        Return New BlockCipherGcm(CreateBlockCipher(rgbKey))
    End Function

    ''' <summary>Creates CCM with the current key. Needs a 128-bit block (not HIGHT).</summary>
    Public Function CreateCcm() As BlockCipherCcm
        Return CreateCcm(Me.Key)
    End Function

    ''' <summary>Creates CCM with the given key. Needs a 128-bit block (not HIGHT).</summary>
    Public Function CreateCcm(rgbKey() As Byte) As BlockCipherCcm
        Return New BlockCipherCcm(CreateBlockCipher(rgbKey))
    End Function

    ''' <summary>Creates CMAC with the current key.</summary>
    Public Function CreateCmac() As BlockCipherCmac
        Return CreateCmac(Me.Key)
    End Function

    ''' <summary>Creates CMAC with the given key.</summary>
    Public Function CreateCmac(rgbKey() As Byte) As BlockCipherCmac
        Return New BlockCipherCmac(CreateBlockCipher(rgbKey))
    End Function
End Class
