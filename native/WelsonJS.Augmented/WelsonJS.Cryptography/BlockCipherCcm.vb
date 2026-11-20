' BlockCipherCcm.vb (WelsonJS.Cryptography)
' SPDX-License-Identifier: MIT
' SPDX-FileCopyrightText: 2025 Namhyeon Go <gnh1201@catswords.re.kr>, Catswords OSS And WelsonJS Contributors
' https://github.com/gnh1201/welsonjs
'
Imports System.Security.Cryptography

''' <summary>
''' Counter with CBC-MAC (NIST SP 800-38C, RFC 3610), authenticated encryption with
''' associated data, for a block cipher with a 128-bit block (ARIA, SEED). The nonce is
''' 7 to 13 bytes and the tag 4, 6, 8, 10, 12, 14 or 16 bytes. The methods follow
''' System.Security.Cryptography.AesCcm.
''' </summary>
Public NotInheritable Class BlockCipherCcm
    Implements IDisposable

    Private ReadOnly cipher As IBlockCipher
    Private disposed As Boolean

    Public Sub New(cipher As IBlockCipher)
        If cipher Is Nothing Then Throw New ArgumentNullException(NameOf(cipher))
        If cipher.BlockSize <> 16 Then
            Throw New ArgumentException("CCM needs a block cipher with a 128-bit block.", NameOf(cipher))
        End If
        Me.cipher = cipher
    End Sub

    ''' <summary>Whether a tag of this many bytes is allowed: 4, 6, 8, 10, 12, 14 or 16.</summary>
    Public Shared Function IsValidTagSize(tagSize As Integer) As Boolean
        Return tagSize >= 4 AndAlso tagSize <= 16 AndAlso tagSize Mod 2 = 0
    End Function

    ''' <summary>Whether a nonce of this many bytes is allowed: 7 to 13.</summary>
    Public Shared Function IsValidNonceSize(nonceSize As Integer) As Boolean
        Return nonceSize >= 7 AndAlso nonceSize <= 13
    End Function

    ''' <summary>Encrypts the plaintext into the ciphertext (the same length) and writes the tag (its length chooses the tag size).</summary>
    Public Sub Encrypt(nonce() As Byte, plaintext() As Byte, ciphertext() As Byte, tag() As Byte, Optional associatedData() As Byte = Nothing)
        CheckArguments(nonce, plaintext, ciphertext, tag)
        Dim t() As Byte = CbcMac(nonce, associatedData, plaintext, tag.Length)
        Dim s0() As Byte = CounterBlock(nonce, 0)
        cipher.EncryptBlock(s0, 0, s0, 0)
        For i As Integer = 0 To tag.Length - 1
            tag(i) = t(i) Xor s0(i)
        Next
        CounterXor(nonce, plaintext, ciphertext)
    End Sub

    ''' <summary>Decrypts the ciphertext and checks the tag. Throws CryptographicException, with the plaintext cleared, if the tag does not match.</summary>
    Public Sub Decrypt(nonce() As Byte, ciphertext() As Byte, tag() As Byte, plaintext() As Byte, Optional associatedData() As Byte = Nothing)
        CheckArguments(nonce, plaintext, ciphertext, tag)
        CounterXor(nonce, ciphertext, plaintext)
        Dim t() As Byte = CbcMac(nonce, associatedData, plaintext, tag.Length)
        Dim s0() As Byte = CounterBlock(nonce, 0)
        cipher.EncryptBlock(s0, 0, s0, 0)
        Dim expected(tag.Length - 1) As Byte
        For i As Integer = 0 To tag.Length - 1
            expected(i) = t(i) Xor s0(i)
        Next
        If Not FixedTimeEquals(expected, tag) Then
            Array.Clear(plaintext, 0, plaintext.Length)
            Throw New CryptographicException("The computed authentication tag did not match the input authentication tag.")
        End If
    End Sub

    ''' <summary>Encrypts and returns the ciphertext followed by the tag.</summary>
    Public Function Seal(nonce() As Byte, plaintext() As Byte, Optional associatedData() As Byte = Nothing, Optional tagSize As Integer = 16) As Byte()
        If plaintext Is Nothing Then Throw New ArgumentNullException(NameOf(plaintext))
        If Not IsValidTagSize(tagSize) Then Throw New ArgumentException("The tag size must be 4, 6, 8, 10, 12, 14 or 16 bytes.", NameOf(tagSize))
        Dim ciphertext(plaintext.Length - 1) As Byte
        Dim tag(tagSize - 1) As Byte
        Encrypt(nonce, plaintext, ciphertext, tag, associatedData)
        Dim result(ciphertext.Length + tagSize - 1) As Byte
        Array.Copy(ciphertext, result, ciphertext.Length)
        Array.Copy(tag, 0, result, ciphertext.Length, tagSize)
        Return result
    End Function

    ''' <summary>Splits the ciphertext and the tag that <see cref="Seal"/> joined, checks the tag and returns the plaintext.</summary>
    Public Function Open(nonce() As Byte, sealed() As Byte, Optional associatedData() As Byte = Nothing, Optional tagSize As Integer = 16) As Byte()
        If sealed Is Nothing Then Throw New ArgumentNullException(NameOf(sealed))
        If Not IsValidTagSize(tagSize) Then Throw New ArgumentException("The tag size must be 4, 6, 8, 10, 12, 14 or 16 bytes.", NameOf(tagSize))
        If sealed.Length < tagSize Then Throw New CryptographicException("The input is shorter than the tag.")
        Dim ciphertext(sealed.Length - tagSize - 1) As Byte
        Dim tag(tagSize - 1) As Byte
        Array.Copy(sealed, ciphertext, ciphertext.Length)
        Array.Copy(sealed, ciphertext.Length, tag, 0, tagSize)
        Dim plaintext(ciphertext.Length - 1) As Byte
        Decrypt(nonce, ciphertext, tag, plaintext, associatedData)
        Return plaintext
    End Function

    Private Sub CheckArguments(nonce() As Byte, plaintext() As Byte, ciphertext() As Byte, tag() As Byte)
        If disposed Then Throw New ObjectDisposedException(NameOf(BlockCipherCcm))
        If nonce Is Nothing Then Throw New ArgumentNullException(NameOf(nonce))
        If plaintext Is Nothing Then Throw New ArgumentNullException(NameOf(plaintext))
        If ciphertext Is Nothing Then Throw New ArgumentNullException(NameOf(ciphertext))
        If tag Is Nothing Then Throw New ArgumentNullException(NameOf(tag))
        If Not IsValidNonceSize(nonce.Length) Then
            Throw New ArgumentException("The nonce must be 7 to 13 bytes.", NameOf(nonce))
        End If
        If Not IsValidTagSize(tag.Length) Then
            Throw New ArgumentException("The tag size must be 4, 6, 8, 10, 12, 14 or 16 bytes.", NameOf(tag))
        End If
        If plaintext.Length <> ciphertext.Length Then
            Throw New ArgumentException("The plaintext and the ciphertext must have the same length.")
        End If
        Dim q As Integer = 15 - nonce.Length
        If q < 4 AndAlso CLng(plaintext.Length) >= (1L << (8 * q)) Then
            Throw New ArgumentException("The message is too long for a nonce of " & nonce.Length & " bytes.")
        End If
    End Sub

    ' CBC-MAC over B0, the encoded associated data and the message, each padded to a block.
    Private Function CbcMac(nonce() As Byte, associatedData() As Byte, message() As Byte, tagSize As Integer) As Byte()
        Dim q As Integer = 15 - nonce.Length
        Dim aLength As Integer = If(associatedData Is Nothing, 0, associatedData.Length)
        Dim x(15) As Byte

        ' B0: flags (Adata, (t - 2) / 2, q - 1), the nonce and the message length in q bytes.
        x(0) = CByte(If(aLength > 0, &H40, 0) Or (((tagSize - 2) \ 2) << 3) Or (q - 1))
        Array.Copy(nonce, 0, x, 1, nonce.Length)
        Dim length As Long = message.Length
        For i As Integer = 15 To 16 - q Step -1
            x(i) = CByte(length And &HFF)
            length >>= 8
        Next
        cipher.EncryptBlock(x, 0, x, 0)

        If aLength > 0 Then
            ' The length of the associated data in 2, 6 or 10 bytes, then the data.
            Dim header() As Byte
            If aLength < &HFF00 Then
                header = New Byte() {CByte(aLength >> 8), CByte(aLength And &HFF)}
            Else
                header = New Byte() {&HFF, &HFE, CByte((aLength >> 24) And &HFF), CByte((aLength >> 16) And &HFF),
                                     CByte((aLength >> 8) And &HFF), CByte(aLength And &HFF)}
            End If
            Dim encoded(header.Length + aLength - 1) As Byte
            Array.Copy(header, encoded, header.Length)
            Array.Copy(associatedData, 0, encoded, header.Length, aLength)
            Absorb(encoded, x)
        End If
        Absorb(message, x)
        Return x
    End Function

    ' X = E(X xor block) for each 16-byte block, the last one padded with zeros.
    Private Sub Absorb(data() As Byte, x() As Byte)
        For p As Integer = 0 To data.Length - 1 Step 16
            Dim n As Integer = Math.Min(16, data.Length - p)
            For i As Integer = 0 To n - 1
                x(i) = x(i) Xor data(p + i)
            Next
            cipher.EncryptBlock(x, 0, x, 0)
        Next
    End Sub

    ' Counter block i: flags (q - 1), the nonce and i in q bytes.
    Private Shared Function CounterBlock(nonce() As Byte, index As Long) As Byte()
        Dim q As Integer = 15 - nonce.Length
        Dim a(15) As Byte
        a(0) = CByte(q - 1)
        Array.Copy(nonce, 0, a, 1, nonce.Length)
        For i As Integer = 15 To 16 - q Step -1
            a(i) = CByte(index And &HFF)
            index >>= 8
        Next
        Return a
    End Function

    ' XORs the input with E(A1), E(A2), ...
    Private Sub CounterXor(nonce() As Byte, input() As Byte, output() As Byte)
        Dim keystream(15) As Byte
        Dim index As Long = 1
        For p As Integer = 0 To input.Length - 1 Step 16
            Dim a() As Byte = CounterBlock(nonce, index)
            cipher.EncryptBlock(a, 0, keystream, 0)
            index += 1
            Dim n As Integer = Math.Min(16, input.Length - p)
            For i As Integer = 0 To n - 1
                output(p + i) = input(p + i) Xor keystream(i)
            Next
        Next
        Array.Clear(keystream, 0, keystream.Length)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        disposed = True
    End Sub
End Class
