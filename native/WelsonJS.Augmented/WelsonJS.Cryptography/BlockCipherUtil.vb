' BlockCipherUtil.vb (WelsonJS.Cryptography)
' SPDX-License-Identifier: MIT
' SPDX-FileCopyrightText: 2025 Namhyeon Go <gnh1201@catswords.re.kr>, Catswords OSS And WelsonJS Contributors
' https://github.com/gnh1201/welsonjs
'
Imports System.Security.Cryptography

Friend Module BlockCipherUtil
    Private ReadOnly rng As RandomNumberGenerator = RandomNumberGenerator.Create()

    ''' <summary>Returns count random bytes from the system's cryptographic generator.</summary>
    Friend Function RandomBytes(count As Integer) As Byte()
        Dim bytes(count - 1) As Byte
        SyncLock rng
            rng.GetBytes(bytes)
        End SyncLock
        Return bytes
    End Function

    ''' <summary>Compares two byte arrays in time that depends only on their length.</summary>
    Friend Function FixedTimeEquals(a() As Byte, b() As Byte) As Boolean
        If a Is Nothing OrElse b Is Nothing OrElse a.Length <> b.Length Then Return False
        Dim diff As Integer = 0
        For i As Integer = 0 To a.Length - 1
            diff = diff Or (CInt(a(i)) Xor CInt(b(i)))
        Next
        Return diff = 0
    End Function

    ''' <summary>Checks the arguments of an ICryptoTransform or a buffer method.</summary>
    Friend Sub CheckBuffer(buffer() As Byte, offset As Integer, count As Integer, name As String)
        If buffer Is Nothing Then Throw New ArgumentNullException(name)
        If offset < 0 Then Throw New ArgumentOutOfRangeException(name, "The offset cannot be negative.")
        If count < 0 Then Throw New ArgumentOutOfRangeException(name, "The count cannot be negative.")
        If count > buffer.Length - offset Then Throw New ArgumentException("The offset and count exceed the length of the buffer.", name)
    End Sub

    ''' <summary>Returns a copy of the array, or an empty array for Nothing.</summary>
    Friend Function CopyOrEmpty(data() As Byte) As Byte()
        If data Is Nothing Then Return New Byte() {}
        Return CType(data.Clone(), Byte())
    End Function
End Module
