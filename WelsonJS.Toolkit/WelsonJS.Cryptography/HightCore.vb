' HightCore.cs (WelsonJS.Cryptography)
' SPDX-License-Identifier: MIT
' SPDX-FileCopyrightText: 2025 Namhyeon Go <gnh1201@catswords.re.kr>, Catswords OSS And WelsonJS Contributors
' https://github.com/gnh1201/welsonjs
' 
Public Class HightCore
    Private ReadOnly roundKey(135) As Byte
    Private Shared ReadOnly DELTA As Byte() = {
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

    Private Shared ReadOnly F0_TABLE As Byte() = {
        &H0, &H86, &HD, &H8B, &H1A, &H9C, &H17, &H91, &H34, &HB2, &H39, &HBF, &H2E, &HA8, &H23, &HA5,
        &H68, &HEE, &H65, &HE3, &H72, &HF4, &H7F, &HF9, &H5C, &HDA, &H51, &HD7, &H46, &HC0, &H4B, &HCD,
        &HD0, &H56, &HDD, &H5B, &HCA, &H4C, &HC7, &H41, &HE4, &H62, &HE9, &H6F, &HFE, &H78, &HF3, &H75,
        &HB8, &H3E, &HB5, &H33, &HA2, &H24, &HAF, &H29, &H8C, &HA, &H81, &H7, &H96, &H10, &H9B, &H1D,
        &HA1, &H27, &HAC, &H2A, &HBB, &H3D, &HB6, &H30, &H95, &H13, &H98, &H1E, &H8F, &H9, &H82, &H4,
        &HC9, &H4F, &HC4, &H42, &HD3, &H55, &HDE, &H58, &HFD, &H7B, &HF0, &H76, &HE7, &H61, &HEA, &H6C,
        &H71, &HF7, &H7C, &HFA, &H6B, &HED, &H66, &HE0, &H45, &HC3, &H48, &HCE, &H5F, &HD9, &H52, &HD4,
        &H19, &H9F, &H14, &H92, &H3, &H85, &HE, &H88, &H2D, &HAB, &H20, &HA6, &H37, &HB1, &H3A, &HBC,
        &H43, &HC5, &H4E, &HC8, &H59, &HDF, &H54, &HD2, &H77, &HF1, &H7A, &HFC, &H6D, &HEB, &H60, &HE6,
        &H2B, &HAD, &H26, &HA0, &H31, &HB7, &H3C, &HBA, &H1F, &H99, &H12, &H94, &H5, &H83, &H8, &H8E,
        &H93, &H15, &H9E, &H18, &H89, &HF, &H84, &H2, &HA7, &H21, &HAA, &H2C, &HBD, &H3B, &HB0, &H36,
        &HFB, &H7D, &HF6, &H70, &HE1, &H67, &HEC, &H6A, &HCF, &H49, &HC2, &H44, &HD5, &H53, &HD8, &H5E,
        &HE2, &H64, &HEF, &H69, &HF8, &H7E, &HF5, &H73, &HD6, &H50, &HDB, &H5D, &HCC, &H4A, &HC1, &H47,
        &H8A, &HC, &H87, &H1, &H90, &H16, &H9D, &H1B, &HBE, &H38, &HB3, &H35, &HA4, &H22, &HA9, &H2F,
        &H32, &HB4, &H3F, &HB9, &H28, &HAE, &H25, &HA3, &H6, &H80, &HB, &H8D, &H1C, &H9A, &H11, &H97,
        &H5A, &HDC, &H57, &HD1, &H40, &HC6, &H4D, &HCB, &H6E, &HE8, &H63, &HE5, &H74, &HF2, &H79, &HFF
    }

    Private Shared ReadOnly F1_TABLE As Byte() = {
        &H0, &H58, &HB0, &HE8, &H61, &H39, &HD1, &H89, &HC2, &H9A, &H72, &H2A, &HA3, &HFB, &H13, &H4B,
        &H85, &HDD, &H35, &H6D, &HE4, &HBC, &H54, &HC, &H47, &H1F, &HF7, &HAF, &H26, &H7E, &H96, &HCE,
        &HB, &H53, &HBB, &HE3, &H6A, &H32, &HDA, &H82, &HC9, &H91, &H79, &H21, &HA8, &HF0, &H18, &H40,
        &H8E, &HD6, &H3E, &H66, &HEF, &HB7, &H5F, &H7, &H4C, &H14, &HFC, &HA4, &H2D, &H75, &H9D, &HC5,
        &H16, &H4E, &HA6, &HFE, &H77, &H2F, &HC7, &H9F, &HD4, &H8C, &H64, &H3C, &HB5, &HED, &H5D, &H5D,
        &H93, &HCB, &H23, &H7B, &HF2, &HAA, &H42, &H1A, &H51, &H9, &HE1, &HB9, &H30, &H68, &H80, &HD8,
        &H1D, &H45, &HAD, &HF5, &H7C, &H24, &HCC, &H94, &HDF, &H87, &H6F, &H37, &HBE, &HE6, &HE, &H56,
        &H98, &HC0, &H28, &H70, &HF9, &HA1, &H49, &H11, &H5A, &H2, &HEA, &HB2, &H3B, &H63, &H8B, &HD3,
        &H2C, &H74, &H9C, &HC4, &H4D, &H15, &HFD, &HA5, &HEE, &HB6, &H5E, &H6, &H8F, &HD7, &H3F, &H67,
        &HA9, &HF1, &H19, &H41, &HC8, &H90, &H78, &H20, &H6B, &H33, &HDB, &H83, &HA, &H52, &HBA, &HE2,
        &H27, &H7F, &H97, &HCF, &H46, &H1E, &HF6, &HAE, &HE5, &HBD, &H55, &HD, &H84, &HDC, &H34, &H6C,
        &HA2, &HFA, &H12, &H4A, &HC3, &H9B, &H73, &H2B, &H60, &H38, &HD0, &H88, &H1, &H59, &HB1, &HE9,
        &H3A, &H62, &H8A, &HD2, &H5B, &H3, &HEB, &HB3, &HF8, &HA0, &H48, &H10, &H99, &HC1, &H29, &H71,
        &HBF, &HE7, &HF, &H57, &HDE, &H86, &H6E, &H36, &H7D, &H25, &HCD, &H95, &H1C, &H44, &HAC, &HF4,
        &H31, &H69, &H81, &HD9, &H50, &H8, &HE0, &HB8, &HF3, &HAB, &H43, &H1B, &H92, &HCA, &H22, &H7A,
        &HB4, &HEC, &H4, &H5C, &HD5, &H8D, &H65, &H3D, &H76, &H2E, &HC6, &H9E, &H17, &H4F, &HA7, &HFF
    }

    Public Sub New(userKey As Byte())
        If userKey.Length <> 16 Then Throw New ArgumentException("Key must be 16 bytes")
        KeySchedule(userKey)
    End Sub

    Private Function F0(x As Byte) As Byte
        Return F0_TABLE(x)
    End Function

    Private Function F1(x As Byte) As Byte
        Return F1_TABLE(x)
    End Function

    Private Function Mod8(x As Integer) As Integer
        Return ((x Mod 8) + 8) Mod 8
    End Function

    Private Sub KeySchedule(key As Byte())
        If key.Length <> 16 Then Throw New ArgumentException("Key must be 16 bytes")

        ' 1. Set the whitening key (WK0 ~ WK7): Use MK[8..15]
        For i As Integer = 0 To 7
            roundKey(i) = key(8 + i)
        Next

        ' 2. Set the round key (key length is 128)
        For i As Integer = 0 To 7
            For j As Integer = 0 To 7
                Dim idx = 16 * i + j
                roundKey(8 + idx) = CByte((key(8 + Mod8(j - i)) + DELTA(idx)) And &HFF)
                roundKey(8 + idx + 8) = CByte((key(Mod8(j - i)) + DELTA(idx + 8)) And &HFF)
            Next
        Next
    End Sub

    Public Sub EncryptBlock(input() As Byte, inOffset As Integer, output() As Byte, outOffset As Integer)
        Dim XX(7) As Byte

        ' First Round
        XX(1) = input(inOffset + 1)
        XX(3) = input(inOffset + 3)
        XX(5) = input(inOffset + 5)
        XX(7) = input(inOffset + 7)

        XX(0) = CByte((input(inOffset + 0) + roundKey(0)) And &HFF)
        XX(2) = CByte(input(inOffset + 2) Xor roundKey(1))
        XX(4) = CByte((input(inOffset + 4) + roundKey(2)) And &HFF)
        XX(6) = CByte(input(inOffset + 6) Xor roundKey(3))

        Dim HIGHT_ENC As Action(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer) =
            Sub(k, i0, i1, i2, i3, i4, i5, i6, i7)
                XX(i0) = CByte((XX(i0) Xor (F0(XX(i1)) + roundKey(4 * k + 3))) And &HFF)
                XX(i2) = CByte((XX(i2) + (F1(XX(i3)) Xor roundKey(4 * k + 2))) And &HFF)
                XX(i4) = CByte((XX(i4) Xor (F0(XX(i5)) + roundKey(4 * k + 1))) And &HFF)
                XX(i6) = CByte((XX(i6) + (F1(XX(i7)) Xor roundKey(4 * k + 0))) And &HFF)
            End Sub

        Dim encRounds As Integer()() = {
            New Integer() {7, 6, 5, 4, 3, 2, 1, 0},
            New Integer() {6, 5, 4, 3, 2, 1, 0, 7},
            New Integer() {5, 4, 3, 2, 1, 0, 7, 6},
            New Integer() {4, 3, 2, 1, 0, 7, 6, 5},
            New Integer() {3, 2, 1, 0, 7, 6, 5, 4},
            New Integer() {2, 1, 0, 7, 6, 5, 4, 3},
            New Integer() {1, 0, 7, 6, 5, 4, 3, 2},
            New Integer() {0, 7, 6, 5, 4, 3, 2, 1}
        }

        For round As Integer = 2 To 33
            Dim seq As Integer() = encRounds((round - 2) Mod 8)
            HIGHT_ENC(round, seq(0), seq(1), seq(2), seq(3), seq(4), seq(5), seq(6), seq(7))
        Next

        output(outOffset + 1) = XX(2)
        output(outOffset + 3) = XX(4)
        output(outOffset + 5) = XX(6)
        output(outOffset + 7) = XX(0)

        output(outOffset + 0) = CByte((XX(1) + roundKey(4)) And &HFF)
        output(outOffset + 2) = CByte(XX(3) Xor roundKey(5))
        output(outOffset + 4) = CByte((XX(5) + roundKey(6)) And &HFF)
        output(outOffset + 6) = CByte(XX(7) Xor roundKey(7))
    End Sub

    Public Sub DecryptBlock(input() As Byte, inOffset As Integer, output() As Byte, outOffset As Integer)
        Dim XX(7) As Byte

        ' Initial Round
        XX(2) = input(inOffset + 1)
        XX(4) = input(inOffset + 3)
        XX(6) = input(inOffset + 5)
        XX(0) = input(inOffset + 7)

        XX(1) = CByte((input(inOffset + 0) - roundKey(4)) And &HFF)
        XX(3) = CByte(input(inOffset + 2) Xor roundKey(5))
        XX(5) = CByte((input(inOffset + 4) - roundKey(6)) And &HFF)
        XX(7) = CByte(input(inOffset + 6) Xor roundKey(7))

        ' HIGHT_DEC as Action
        Dim HIGHT_DEC As Action(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer) =
            Sub(k, i0, i1, i2, i3, i4, i5, i6, i7)
                XX(i1) = CByte((XX(i1) - (F1(XX(i2)) Xor roundKey(4 * k + 2))) And &HFF)
                XX(i3) = CByte((XX(i3) Xor (F0(XX(i4)) + roundKey(4 * k + 1))) And &HFF)
                XX(i5) = CByte((XX(i5) - (F1(XX(i6)) Xor roundKey(4 * k + 0))) And &HFF)
                XX(i7) = CByte((XX(i7) Xor (F0(XX(i0)) + roundKey(4 * k + 3))) And &HFF)
            End Sub

        ' Round pattern (same as encryption, reused)
        Dim decRounds As Integer()() = {
            New Integer() {7, 6, 5, 4, 3, 2, 1, 0},
            New Integer() {0, 7, 6, 5, 4, 3, 2, 1},
            New Integer() {1, 0, 7, 6, 5, 4, 3, 2},
            New Integer() {2, 1, 0, 7, 6, 5, 4, 3},
            New Integer() {3, 2, 1, 0, 7, 6, 5, 4},
            New Integer() {4, 3, 2, 1, 0, 7, 6, 5},
            New Integer() {5, 4, 3, 2, 1, 0, 7, 6},
            New Integer() {6, 5, 4, 3, 2, 1, 0, 7}
        }

        ' Apply decryption rounds
        For round As Integer = 33 To 2 Step -1
            Dim seq As Integer() = decRounds((33 - round) Mod 8)
            HIGHT_DEC(round, seq(0), seq(1), seq(2), seq(3), seq(4), seq(5), seq(6), seq(7))
        Next

        ' Final output
        output(outOffset + 1) = XX(1)
        output(outOffset + 3) = XX(3)
        output(outOffset + 5) = XX(5)
        output(outOffset + 7) = XX(7)

        output(outOffset + 0) = CByte((XX(0) - roundKey(0)) And &HFF)
        output(outOffset + 2) = CByte(XX(2) Xor roundKey(1))
        output(outOffset + 4) = CByte((XX(4) - roundKey(2)) And &HFF)
        output(outOffset + 6) = CByte(XX(6) Xor roundKey(3))
    End Sub
End Class
