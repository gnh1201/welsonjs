' BlockCipherGcm.vb (WelsonJS.Cryptography)
' SPDX-License-Identifier: MIT
' SPDX-FileCopyrightText: 2025 Namhyeon Go <gnh1201@catswords.re.kr>, Catswords OSS And WelsonJS Contributors
' https://github.com/gnh1201/welsonjs
'
Imports System.Security.Cryptography

''' <summary>
''' Galois/Counter Mode (NIST SP 800-38D), authenticated encryption with associated data,
''' for a block cipher with a 128-bit block (ARIA, SEED). The methods follow
''' System.Security.Cryptography.AesGcm: Encrypt fills the ciphertext and the tag, and
''' Decrypt checks the tag before it returns any plaintext.
''' </summary>
Public NotInheritable Class BlockCipherGcm
    Implements IDisposable

    Private ReadOnly cipher As IBlockCipher
    Private hHigh As ULong   ' the hash subkey H = E(0^128)
    Private hLow As ULong
    Private disposed As Boolean

    Public Sub New(cipher As IBlockCipher)
        If cipher Is Nothing Then Throw New ArgumentNullException(NameOf(cipher))
        If cipher.BlockSize <> 16 Then
            Throw New ArgumentException("GCM needs a block cipher with a 128-bit block.", NameOf(cipher))
        End If
        Me.cipher = cipher
        Dim h(15) As Byte
        cipher.EncryptBlock(h, 0, h, 0)
        hHigh = ReadUInt64(h, 0)
        hLow = ReadUInt64(h, 8)
        Array.Clear(h, 0, h.Length)
    End Sub

    ''' <summary>Whether a tag of this many bytes is allowed: 4, 8, or 12 to 16 (NIST SP 800-38D).</summary>
    Public Shared Function IsValidTagSize(tagSize As Integer) As Boolean
        Return tagSize = 4 OrElse tagSize = 8 OrElse (tagSize >= 12 AndAlso tagSize <= 16)
    End Function

    ''' <summary>Encrypts the plaintext into the ciphertext (the same length) and writes the tag (its length chooses the tag size).</summary>
    Public Sub Encrypt(nonce() As Byte, plaintext() As Byte, ciphertext() As Byte, tag() As Byte, Optional associatedData() As Byte = Nothing)
        CheckArguments(nonce, plaintext, ciphertext, tag)
        Dim j0() As Byte = PreCounterBlock(nonce)
        Dim counter() As Byte = CType(j0.Clone(), Byte())
        Increment32(counter)
        CounterXor(counter, plaintext, ciphertext)
        ComputeTag(j0, associatedData, ciphertext, tag)
    End Sub

    ''' <summary>Checks the tag and, if it matches, decrypts the ciphertext into the plaintext. Throws CryptographicException if it does not match.</summary>
    Public Sub Decrypt(nonce() As Byte, ciphertext() As Byte, tag() As Byte, plaintext() As Byte, Optional associatedData() As Byte = Nothing)
        CheckArguments(nonce, plaintext, ciphertext, tag)
        Dim j0() As Byte = PreCounterBlock(nonce)
        Dim expected(tag.Length - 1) As Byte
        ComputeTag(j0, associatedData, ciphertext, expected)
        If Not FixedTimeEquals(expected, tag) Then
            Array.Clear(plaintext, 0, plaintext.Length)
            Throw New CryptographicException("The computed authentication tag did not match the input authentication tag.")
        End If
        Dim counter() As Byte = CType(j0.Clone(), Byte())
        Increment32(counter)
        CounterXor(counter, ciphertext, plaintext)
    End Sub

    ''' <summary>Encrypts and returns the ciphertext followed by the tag.</summary>
    Public Function Seal(nonce() As Byte, plaintext() As Byte, Optional associatedData() As Byte = Nothing, Optional tagSize As Integer = 16) As Byte()
        If plaintext Is Nothing Then Throw New ArgumentNullException(NameOf(plaintext))
        If Not IsValidTagSize(tagSize) Then Throw New ArgumentException("The tag size must be 4, 8, or 12 to 16 bytes.", NameOf(tagSize))
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
        If Not IsValidTagSize(tagSize) Then Throw New ArgumentException("The tag size must be 4, 8, or 12 to 16 bytes.", NameOf(tagSize))
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
        If disposed Then Throw New ObjectDisposedException(NameOf(BlockCipherGcm))
        If nonce Is Nothing Then Throw New ArgumentNullException(NameOf(nonce))
        If nonce.Length = 0 Then Throw New ArgumentException("The nonce cannot be empty.", NameOf(nonce))
        If plaintext Is Nothing Then Throw New ArgumentNullException(NameOf(plaintext))
        If ciphertext Is Nothing Then Throw New ArgumentNullException(NameOf(ciphertext))
        If tag Is Nothing Then Throw New ArgumentNullException(NameOf(tag))
        If plaintext.Length <> ciphertext.Length Then
            Throw New ArgumentException("The plaintext and the ciphertext must have the same length.")
        End If
        If Not IsValidTagSize(tag.Length) Then
            Throw New ArgumentException("The tag size must be 4, 8, or 12 to 16 bytes.", NameOf(tag))
        End If
    End Sub

    ' J0: the nonce followed by 00000001 for a 96-bit nonce, GHASH(nonce || padding || length) otherwise.
    Private Function PreCounterBlock(nonce() As Byte) As Byte()
        If nonce.Length = 12 Then
            Dim j0(15) As Byte
            Array.Copy(nonce, j0, 12)
            j0(15) = 1
            Return j0
        End If
        Return GHash(Nothing, nonce)
    End Function

    Private Sub ComputeTag(j0() As Byte, associatedData() As Byte, ciphertext() As Byte, tag() As Byte)
        Dim s() As Byte = GHash(associatedData, ciphertext)
        Dim e(15) As Byte
        cipher.EncryptBlock(j0, 0, e, 0)
        For i As Integer = 0 To tag.Length - 1
            tag(i) = e(i) Xor s(i)
        Next
    End Sub

    ' GCTR: XORs the input with E(counter), E(inc32(counter)), ...
    Private Sub CounterXor(counter() As Byte, input() As Byte, output() As Byte)
        Dim keystream(15) As Byte
        For p As Integer = 0 To input.Length - 1 Step 16
            cipher.EncryptBlock(counter, 0, keystream, 0)
            Increment32(counter)
            Dim n As Integer = Math.Min(16, input.Length - p)
            For i As Integer = 0 To n - 1
                output(p + i) = input(p + i) Xor keystream(i)
            Next
        Next
        Array.Clear(keystream, 0, keystream.Length)
    End Sub

    ' inc32: adds one to the rightmost 32 bits, modulo 2^32.
    Private Shared Sub Increment32(block() As Byte)
        For i As Integer = 15 To 12 Step -1
            block(i) = CByte((CInt(block(i)) + 1) And &HFF)
            If block(i) <> 0 Then Exit For
        Next
    End Sub

    ' GHASH_H(A || pad || C || pad || [len(A)]64 || [len(C)]64).
    Private Function GHash(a() As Byte, c() As Byte) As Byte()
        Dim yHigh As ULong = 0
        Dim yLow As ULong = 0
        Dim aLength As Integer = If(a Is Nothing, 0, a.Length)
        If aLength > 0 Then Absorb(a, yHigh, yLow)
        If c.Length > 0 Then Absorb(c, yHigh, yLow)
        yHigh = yHigh Xor (CULng(aLength) * 8UL)
        yLow = yLow Xor (CULng(c.Length) * 8UL)
        Multiply(yHigh, yLow)
        Dim result(15) As Byte
        WriteUInt64(yHigh, result, 0)
        WriteUInt64(yLow, result, 8)
        Return result
    End Function

    ' Y = (Y xor block) * H for each 16-byte block, the last one padded with zeros.
    Private Sub Absorb(data() As Byte, ByRef yHigh As ULong, ByRef yLow As ULong)
        Dim block(15) As Byte
        For p As Integer = 0 To data.Length - 1 Step 16
            Dim n As Integer = Math.Min(16, data.Length - p)
            Array.Clear(block, 0, 16)
            Array.Copy(data, p, block, 0, n)
            yHigh = yHigh Xor ReadUInt64(block, 0)
            yLow = yLow Xor ReadUInt64(block, 8)
            Multiply(yHigh, yLow)
        Next
    End Sub

    ' X = X * H in GF(2^128), with the bit order of SP 800-38D (the first bit is the
    ' coefficient of x^0) and R = 11100001 || 0^120.
    Private Sub Multiply(ByRef xHigh As ULong, ByRef xLow As ULong)
        Dim zHigh As ULong = 0
        Dim zLow As ULong = 0
        Dim vHigh As ULong = hHigh
        Dim vLow As ULong = hLow
        For i As Integer = 0 To 127
            Dim bit As ULong
            If i < 64 Then
                bit = (xHigh >> (63 - i)) And 1UL
            Else
                bit = (xLow >> (127 - i)) And 1UL
            End If
            If bit <> 0UL Then
                zHigh = zHigh Xor vHigh
                zLow = zLow Xor vLow
            End If
            Dim carry As ULong = vLow And 1UL
            vLow = (vLow >> 1) Or (vHigh << 63)
            vHigh = vHigh >> 1
            If carry <> 0UL Then vHigh = vHigh Xor &HE100000000000000UL
        Next
        xHigh = zHigh
        xLow = zLow
    End Sub

    Private Shared Function ReadUInt64(data() As Byte, offset As Integer) As ULong
        Dim v As ULong = 0
        For i As Integer = 0 To 7
            v = (v << 8) Or data(offset + i)
        Next
        Return v
    End Function

    Private Shared Sub WriteUInt64(v As ULong, data() As Byte, offset As Integer)
        For i As Integer = 7 To 0 Step -1
            data(offset + i) = CByte(v And &HFFUL)
            v = v >> 8
        Next
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        hHigh = 0
        hLow = 0
        disposed = True
    End Sub
End Class
