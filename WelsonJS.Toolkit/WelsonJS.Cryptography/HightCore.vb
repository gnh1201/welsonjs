Public Class HightCore
    Private ReadOnly roundKey(135) As Byte
    Private Shared ReadOnly Delta As Byte() = {
        &H5A, &H6D, &H36, &H1B, &HD, &H6, &H3, &H41,
        &H60, &H30, &H18, &H4C, &H66, &H33, &H59, &H2C,
        &H56, &H2B, &H15, &H4A, &H65, &H72, &H39, &H1C,
        &H4E, &H67, &H73, &H79, &H3C, &H5E, &H6F, &H37,
        &H5B, &H2D, &H16, &HB, &H5, &H42, &H21, &H50,
        &H28, &H54, &H2A, &H55, &H6A, &H75, &H7A, &H7D,
        &H3E, &H5F, &H2F, &H17, &H4B, &H25, &H52, &H29,
        &H14, &HA, &H45, &H62, &H31, &H58, &H6C, &H76,
        &H3B, &H1D, &HE, &H47, &H63, &H71, &H78, &H7C,
        &H7E, &H7F, &H3F, &H1F, &HF, &H7, &H43, &H61,
        &H70, &H38, &H5C, &H6E, &H77, &H7B, &H3D, &H1E,
        &H4F, &H27, &H53, &H69, &H34, &H1A, &H4D, &H26,
        &H13, &H49, &H24, &H12, &H9, &H4, &H2, &H1,
        &H40, &H20, &H10, &H8, &H44, &H22, &H11, &H48,
        &H64, &H32, &H19, &HC, &H46, &H23, &H51, &H68,
        &H74, &H3A, &H5D, &H2E, &H57, &H6B, &H35, &H5A
    }

    Public Sub New(userKey As Byte())
        If userKey.Length <> 16 Then Throw New ArgumentException("Key must be 16 bytes")
        KeySchedule(userKey)
    End Sub

    Private Sub KeySchedule(userKey As Byte())
        ' Whitening keys
        For i As Integer = 0 To 3
            roundKey(i) = userKey(i + 12)
            roundKey(i + 4) = userKey(i)
        Next

        ' Round keys (SK008 to SK135)
        For i As Integer = 0 To 7
            For j As Integer = 0 To 7
                Dim idx = 16 * i + j
                roundKey(8 + idx) = CByte((userKey((j - i) And 7) + Delta(idx)) And &HFF)
                roundKey(8 + idx + 8) = CByte((userKey(8 + ((j - i) And 7)) + Delta(idx + 8)) And &HFF)
            Next
        Next
    End Sub

    Private Function RotL(x As Byte, n As Integer) As Byte
        Return CByte(((x << n) Or (x >> (8 - n))) And &HFF)
    End Function

    Private Function F0(x As Byte) As Byte
        Return CByte(RotL(x, 1) Xor RotL(x, 2) Xor RotL(x, 7))
    End Function

    Private Function F1(x As Byte) As Byte
        Return CByte(RotL(x, 3) Xor RotL(x, 4) Xor RotL(x, 6))
    End Function

    Public Sub EncryptBlock(input() As Byte, inOffset As Integer, output() As Byte, outOffset As Integer)
        Dim X(7) As Byte
        ' Initial whitening
        X(0) = CByte((input(inOffset + 0) + roundKey(0)) And &HFF)
        X(1) = input(inOffset + 1)
        X(2) = CByte(input(inOffset + 2) Xor roundKey(1))
        X(3) = input(inOffset + 3)
        X(4) = CByte((input(inOffset + 4) + roundKey(2)) And &HFF)
        X(5) = input(inOffset + 5)
        X(6) = CByte(input(inOffset + 6) Xor roundKey(3))
        X(7) = input(inOffset + 7)

        For r As Integer = 0 To 31
            Dim T0 = CByte(F0(X(1)) Xor roundKey(8 + 4 * r + 0))
            Dim T1 = CByte(F1(X(3)) Xor roundKey(8 + 4 * r + 1))
            Dim T2 = CByte(F0(X(5)) Xor roundKey(8 + 4 * r + 2))
            Dim T3 = CByte(F1(X(7)) Xor roundKey(8 + 4 * r + 3))

            Dim tmp(7) As Byte
            tmp(0) = X(1)
            tmp(1) = CByte((X(2) + T0) And &HFF)
            tmp(2) = X(3)
            tmp(3) = CByte((X(0) + T1) And &HFF)
            tmp(4) = X(5)
            tmp(5) = CByte((X(6) + T2) And &HFF)
            tmp(6) = X(7)
            tmp(7) = CByte((X(4) + T3) And &HFF)

            Array.Copy(tmp, X, 8)
        Next

        ' Final whitening
        output(outOffset + 0) = CByte((X(0) + roundKey(4)) And &HFF)
        output(outOffset + 1) = X(1)
        output(outOffset + 2) = CByte(X(2) Xor roundKey(5))
        output(outOffset + 3) = X(3)
        output(outOffset + 4) = CByte((X(4) + roundKey(6)) And &HFF)
        output(outOffset + 5) = X(5)
        output(outOffset + 6) = CByte(X(6) Xor roundKey(7))
        output(outOffset + 7) = X(7)
    End Sub

    Public Sub DecryptBlock(input() As Byte, inOffset As Integer, output() As Byte, outOffset As Integer)
        Dim X(7) As Byte
        ' Final whitening undo
        X(0) = CByte((input(inOffset + 0) - roundKey(4)) And &HFF)
        X(1) = input(inOffset + 1)
        X(2) = CByte(input(inOffset + 2) Xor roundKey(5))
        X(3) = input(inOffset + 3)
        X(4) = CByte((input(inOffset + 4) - roundKey(6)) And &HFF)
        X(5) = input(inOffset + 5)
        X(6) = CByte(input(inOffset + 6) Xor roundKey(7))
        X(7) = input(inOffset + 7)

        For r As Integer = 31 To 0 Step -1
            Dim T3 = CByte(F1(X(6)) Xor roundKey(8 + 4 * r + 3))
            Dim T2 = CByte(F0(X(4)) Xor roundKey(8 + 4 * r + 2))
            Dim T1 = CByte(F1(X(2)) Xor roundKey(8 + 4 * r + 1))
            Dim T0 = CByte(F0(X(0)) Xor roundKey(8 + 4 * r + 0))

            Dim tmp(7) As Byte
            tmp(0) = CByte((X(3) - T1) And &HFF)
            tmp(1) = X(0)
            tmp(2) = CByte((X(1) - T0) And &HFF)
            tmp(3) = X(2)
            tmp(4) = CByte((X(7) - T3) And &HFF)
            tmp(5) = X(4)
            tmp(6) = CByte((X(5) - T2) And &HFF)
            tmp(7) = X(6)

            Array.Copy(tmp, X, 8)
        Next

        ' Initial whitening undo
        output(outOffset + 0) = CByte((X(0) - roundKey(0)) And &HFF)
        output(outOffset + 1) = X(1)
        output(outOffset + 2) = CByte(X(2) Xor roundKey(1))
        output(outOffset + 3) = X(3)
        output(outOffset + 4) = CByte((X(4) - roundKey(2)) And &HFF)
        output(outOffset + 5) = X(5)
        output(outOffset + 6) = CByte(X(6) Xor roundKey(3))
        output(outOffset + 7) = X(7)
    End Sub
End Class
