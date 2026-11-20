' BlockCipherCmac.vb (WelsonJS.Cryptography)
' SPDX-License-Identifier: MIT
' SPDX-FileCopyrightText: 2025 Namhyeon Go <gnh1201@catswords.re.kr>, Catswords OSS And WelsonJS Contributors
' https://github.com/gnh1201/welsonjs
'
Imports System.Security.Cryptography

''' <summary>
''' CMAC (NIST SP 800-38B, RFC 4493): a message authentication code from a block cipher with
''' a 64-bit (HIGHT) or 128-bit (ARIA, SEED) block. It is a HashAlgorithm, so ComputeHash,
''' TransformBlock and CryptoStream work as with HMAC; the hash is one block long.
''' </summary>
Public NotInheritable Class BlockCipherCmac
    Inherits HashAlgorithm

    Private ReadOnly cipher As IBlockCipher
    Private ReadOnly blockSize As Integer
    Private ReadOnly k1() As Byte
    Private ReadOnly k2() As Byte
    Private ReadOnly macState() As Byte
    Private ReadOnly buffer() As Byte
    Private bufferCount As Integer

    Public Sub New(cipher As IBlockCipher)
        If cipher Is Nothing Then Throw New ArgumentNullException(NameOf(cipher))
        blockSize = cipher.BlockSize
        If blockSize <> 8 AndAlso blockSize <> 16 Then
            Throw New ArgumentException("CMAC needs a block cipher with a 64-bit or 128-bit block.", NameOf(cipher))
        End If
        Me.cipher = cipher
        HashSizeValue = blockSize * 8

        ' The subkeys: L = E(0), K1 = dbl(L), K2 = dbl(K1).
        Dim l(blockSize - 1) As Byte
        cipher.EncryptBlock(l, 0, l, 0)
        k1 = Doubled(l)
        k2 = Doubled(k1)
        Array.Clear(l, 0, l.Length)

        macState = New Byte(blockSize - 1) {}
        buffer = New Byte(blockSize - 1) {}
    End Sub

    Public Overrides Sub Initialize()
        Array.Clear(macState, 0, macState.Length)
        Array.Clear(buffer, 0, buffer.Length)
        bufferCount = 0
    End Sub

    Protected Overrides Sub HashCore(array() As Byte, ibStart As Integer, cbSize As Integer)
        For i As Integer = ibStart To ibStart + cbSize - 1
            ' A full buffer is processed only when more data follows: the last block is
            ' treated differently in HashFinal.
            If bufferCount = blockSize Then
                ProcessBuffer()
            End If
            buffer(bufferCount) = array(i)
            bufferCount += 1
        Next
    End Sub

    Protected Overrides Function HashFinal() As Byte()
        Dim last(blockSize - 1) As Byte
        If bufferCount = blockSize Then
            ' A complete last block: XOR with K1.
            For i As Integer = 0 To blockSize - 1
                last(i) = buffer(i) Xor k1(i)
            Next
        Else
            ' An incomplete (or empty) last block: pad with 10...0 and XOR with K2.
            Array.Copy(buffer, last, bufferCount)
            last(bufferCount) = &H80
            For i As Integer = 0 To blockSize - 1
                last(i) = last(i) Xor k2(i)
            Next
        End If
        For i As Integer = 0 To blockSize - 1
            macState(i) = macState(i) Xor last(i)
        Next
        cipher.EncryptBlock(macState, 0, macState, 0)
        Dim mac() As Byte = CType(macState.Clone(), Byte())
        Initialize()
        Return mac
    End Function

    Private Sub ProcessBuffer()
        For i As Integer = 0 To blockSize - 1
            macState(i) = macState(i) Xor buffer(i)
        Next
        cipher.EncryptBlock(macState, 0, macState, 0)
        bufferCount = 0
    End Sub

    ' Multiplication by x in GF(2^b): shift left by one bit, and XOR the last byte with
    ' 0x87 (b = 128) or 0x1B (b = 64) when the bit shifted out was set.
    Private Function Doubled(value() As Byte) As Byte()
        Dim result(blockSize - 1) As Byte
        Dim carry As Integer = 0
        For i As Integer = blockSize - 1 To 0 Step -1
            Dim v As Integer = value(i)
            result(i) = CByte(((v << 1) Or carry) And &HFF)
            carry = v >> 7
        Next
        If carry <> 0 Then
            result(blockSize - 1) = result(blockSize - 1) Xor CByte(If(blockSize = 16, &H87, &H1B))
        End If
        Return result
    End Function

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            Array.Clear(k1, 0, k1.Length)
            Array.Clear(k2, 0, k2.Length)
            Array.Clear(macState, 0, macState.Length)
            Array.Clear(buffer, 0, buffer.Length)
        End If
        MyBase.Dispose(disposing)
    End Sub
End Class
