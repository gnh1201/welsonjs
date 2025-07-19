' AriaCore.vb (WelsonJS.Cryptography)
' SPDX-License-Identifier: MIT
' SPDX-FileCopyrightText: 2025 Namhyeon Go <gnh1201@catswords.re.kr>, Catswords OSS And WelsonJS Contributors
' https://github.com/gnh1201/welsonjs
'
' ARIA Core VB.NET Implementation with S-box, inverse S-box, and T-table generation
Public Class AriaCore
    Public Shared ReadOnly S1(255) As Byte
    Public Shared ReadOnly X1(255) As Byte
    Public Shared ReadOnly S2(255) As Byte
    Public Shared ReadOnly X2(255) As Byte
    Public Shared ReadOnly TS1(255) As UInteger
    Public Shared ReadOnly TS2(255) As UInteger
    Public Shared ReadOnly TX1(255) As UInteger
    Public Shared ReadOnly TX2(255) As UInteger

    Private roundKeys()() As UInteger
    Private roundCount As Integer = 12

    Public Sub New(key() As Byte)
        Select Case key.Length
            Case 16
                roundCount = 12
            Case 24
                roundCount = 14
            Case 32
                roundCount = 16
            Case Else
                Throw New ArgumentException("Only 128, 192, or 256-bit keys are supported.")
        End Select
        GenerateRoundKeys(key)
    End Sub

    Public Sub EncryptBlock(input() As Byte, inOffset As Integer, output() As Byte, outOffset As Integer)
        Dim x(3) As UInteger
        For i = 0 To 3
            x(i) = BitConverter.ToUInt32(input, inOffset + i * 4)
        Next

        For i = 0 To 3
            x(i) = x(i) Xor roundKeys(0)(i)
        Next

        For r = 1 To roundCount - 1
            If r = 3 Or r = 7 Then
                x = FL(x, roundKeys(r))
            End If
            x = FO(x)
            For i = 0 To 3
                x(i) = x(i) Xor roundKeys(r)(i)
            Next
        Next

        x = FO(x)

        For i = 0 To 3
            x(i) = x(i) Xor roundKeys(roundCount)(i)
        Next

        For i = 0 To 3
            Dim b() As Byte = BitConverter.GetBytes(x(i))
            Array.Copy(b, 0, output, outOffset + i * 4, 4)
        Next
    End Sub

    Public Sub DecryptBlock(input() As Byte, inOffset As Integer, output() As Byte, outOffset As Integer)
        Dim x(3) As UInteger
        For i = 0 To 3
            x(i) = BitConverter.ToUInt32(input, inOffset + i * 4)
        Next

        For i = 0 To 3
            x(i) = x(i) Xor roundKeys(roundCount)(i)
        Next

        For r = roundCount - 1 To 1 Step -1
            x = RFO(x)
            If r = 3 Or r = 7 Then
                x = FLInv(x, roundKeys(r))
            End If
            For i = 0 To 3
                x(i) = x(i) Xor roundKeys(r)(i)
            Next
        Next

        x = RFO(x)

        For i = 0 To 3
            x(i) = x(i) Xor roundKeys(0)(i)
        Next

        For i = 0 To 3
            Dim b() As Byte = BitConverter.GetBytes(x(i))
            Array.Copy(b, 0, output, outOffset + i * 4, 4)
        Next
    End Sub

    Private Function FO(x() As UInteger) As UInteger()
        Dim y(3) As UInteger
        For i = 0 To 3
            Dim b0 As Byte = (x(i) >> 24) And &HFF
            Dim b1 As Byte = (x(i) >> 16) And &HFF
            Dim b2 As Byte = (x(i) >> 8) And &HFF
            Dim b3 As Byte = x(i) And &HFF
            y(i) = (CUInt(S1(b0)) << 24) Or (CUInt(S2(b1)) << 16) Or (CUInt(S1(b2)) << 8) Or S2(b3)
        Next
        Return M(y)
    End Function

    Private Function RFO(x() As UInteger) As UInteger()
        Dim y(3) As UInteger
        y = M(x)
        For i = 0 To 3
            Dim b0 As Byte = (y(i) >> 24) And &HFF
            Dim b1 As Byte = (y(i) >> 16) And &HFF
            Dim b2 As Byte = (y(i) >> 8) And &HFF
            Dim b3 As Byte = y(i) And &HFF
            x(i) = (CUInt(X1(b0)) << 24) Or (CUInt(X2(b1)) << 16) Or (CUInt(X1(b2)) << 8) Or X2(b3)
        Next
        Return x
    End Function

    Private Function M(x() As UInteger) As UInteger()
        Dim y(3) As UInteger
        y(0) = x(0) Xor RotateLeft(x(1), 8) Xor RotateLeft(x(2), 16) Xor RotateLeft(x(3), 24)
        y(1) = x(1) Xor RotateLeft(x(2), 8) Xor RotateLeft(x(3), 16) Xor RotateLeft(x(0), 24)
        y(2) = x(2) Xor RotateLeft(x(3), 8) Xor RotateLeft(x(0), 16) Xor RotateLeft(x(1), 24)
        y(3) = x(3) Xor RotateLeft(x(0), 8) Xor RotateLeft(x(1), 16) Xor RotateLeft(x(2), 24)
        Return y
    End Function

    Private Function RotateLeft(val As UInteger, bits As Integer) As UInteger
        Return ((val << bits) Or (val >> (32 - bits))) And &HFFFFFFFFUI
    End Function

    Private Sub GenerateRoundKeys(key() As Byte)
        roundKeys = New UInteger(roundCount)() {}
        For r = 0 To roundCount
            roundKeys(r) = New UInteger(3) {}
        Next

        Dim w0(3), w1(3), w2(3), w3(3) As UInteger
        Dim tempKey(7) As UInteger
        For i = 0 To (key.Length \ 4) - 1
            tempKey(i) = BitConverter.ToUInt32(key, i * 4)
        Next

        For i = 0 To 3
            w0(i) = tempKey(i)
        Next

        Dim c1 As UInteger() = {&H517CC1B7UI, &H27220A94UI, &HFE13ABE8UI, &HFA9A6EE0UI}
        Dim c2 As UInteger() = {&H6DB14ACCUI, &H9E21C820UI, &HFF28B1D5UI, &HEE36D2E6UI}
        Dim c3 As UInteger() = {&HDB92F2FBUI, &H61A64DF2UI, &HDC04B4DFUI, &H1BF429C3UI}

        w1 = FO(XorBlock(w0, c1))
        w2 = FO(XorBlock(w1, c2))
        w3 = FO(XorBlock(w2, c3))

        Dim rk(,) As UInteger = {
            {0, 19}, {1, 31}, {2, 19}, {3, 31},
            {0, 19}, {1, 31}, {2, 19}, {3, 31},
            {0, 19}, {1, 31}, {2, 19}, {3, 31},
            {0, 19}, {1, 31}, {2, 19}, {3, 31}
        }
        For r = 0 To roundCount
            For i = 0 To 3
                Select Case r
                    Case < 4 : roundKeys(r)(i) = RotateLeft(w1(i), rk(r, 1))
                    Case < 8 : roundKeys(r)(i) = RotateLeft(w2(i), rk(r, 1))
                    Case < 12 : roundKeys(r)(i) = RotateLeft(w3(i), rk(r, 1))
                    Case < 16 : roundKeys(r)(i) = RotateLeft(w0(i), rk(r, 1))
                End Select
            Next
        Next
    End Sub

    ' ----- Tables -----
    Shared Sub New()
        Dim exp(255) As Integer
        Dim log(255) As Integer
        exp(0) = 1
        For i = 1 To 255
            Dim j As Integer = (exp(i - 1) << 1) Xor exp(i - 1)
            If (j And &H100) <> 0 Then j = j Xor &H11B
            exp(i) = j
        Next
        For i = 1 To 254
            log(exp(i)) = i
        Next

        Dim A(7, 7) As Integer
        Dim AInit(,) As Integer = {
            {1, 0, 0, 0, 1, 1, 1, 1},
            {1, 1, 0, 0, 0, 1, 1, 1},
            {1, 1, 1, 0, 0, 0, 1, 1},
            {1, 1, 1, 1, 0, 0, 0, 1},
            {1, 1, 1, 1, 1, 0, 0, 0},
            {0, 1, 1, 1, 1, 1, 0, 0},
            {0, 0, 1, 1, 1, 1, 1, 0},
            {0, 0, 0, 1, 1, 1, 1, 1}
        }
        For i = 0 To 7 : For j = 0 To 7 : A(i, j) = AInit(i, j) : Next : Next

        Dim B(7, 7) As Integer
        Dim BInit(,) As Integer = {
            {0, 1, 0, 1, 1, 1, 1, 0},
            {0, 0, 1, 1, 1, 1, 0, 1},
            {1, 1, 0, 1, 0, 1, 1, 1},
            {1, 0, 0, 1, 1, 1, 0, 1},
            {0, 0, 1, 0, 1, 1, 0, 0},
            {1, 0, 0, 0, 0, 0, 0, 1},
            {0, 1, 0, 1, 1, 1, 0, 1},
            {1, 1, 0, 1, 0, 0, 1, 1}
        }
        For i = 0 To 7 : For j = 0 To 7 : B(i, j) = BInit(i, j) : Next : Next

        For i = 0 To 255
            Dim t As Integer = 0, p As Integer
            If i = 0 Then
                p = 0
            Else
                p = exp(255 - log(i))
            End If
            For j = 0 To 7
                Dim s As Integer = 0
                For k = 0 To 7
                    If ((p >> (7 - k)) And 1) <> 0 Then
                        s = s Xor A(k, j)
                    End If
                Next
                t = (t << 1) Xor s
            Next
            t = t Xor &H63
            S1(i) = CByte(t)
            X1(t) = CByte(i)
        Next

        For i = 0 To 255
            Dim t As Integer = 0, p As Integer
            If i = 0 Then
                p = 0
            Else
                p = exp((247 * log(i)) Mod 255)
            End If
            For j = 0 To 7
                Dim s As Integer = 0
                For k = 0 To 7
                    If ((p >> k) And 1) <> 0 Then
                        s = s Xor B(7 - j, k)
                    End If
                Next
                t = (t << 1) Xor s
            Next
            t = t Xor &HE2
            S2(i) = CByte(t)
            X2(t) = CByte(i)
        Next

        For i = 0 To 255
            TS1(i) = CUInt(&H10101 * (S1(i) And &HFF))
            TS2(i) = CUInt(&H1000101 * (S2(i) And &HFF))
            TX1(i) = CUInt(&H1010001 * (X1(i) And &HFF))
            TX2(i) = CUInt(&H1010100 * (X2(i) And &HFF))
        Next
    End Sub
    Private Function XorBlock(a() As UInteger, b() As UInteger) As UInteger()
        Dim r(3) As UInteger
        For i = 0 To 3
            r(i) = a(i) Xor b(i)
        Next
        Return r
    End Function

    Private Function FL(x() As UInteger, k() As UInteger) As UInteger()
        Dim y(3) As UInteger
        y(0) = x(0) Xor RotateLeft((x(1) And k(0)), 1)
        y(1) = x(1) Xor (y(0) Or k(1))
        y(2) = x(2) Xor RotateLeft((x(3) Or k(2)), 1)
        y(3) = x(3) Xor (y(2) And k(3))
        Return y
    End Function

    Private Function FLInv(x() As UInteger, k() As UInteger) As UInteger()
        Dim y(3) As UInteger
        y(3) = x(3) Xor ((x(2) Or k(2)) And &HFFFFFFFFUI)
        y(2) = x(2) Xor RotateLeft((y(3) Or k(2)), 1)
        y(1) = x(1) Xor ((x(0) And k(0)) And &HFFFFFFFFUI)
        y(0) = x(0) Xor RotateLeft((y(1) And k(0)), 1)
        Return y
    End Function

End Class
