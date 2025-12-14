' AriaCore.vb (WelsonJS.Cryptography)
' SPDX-License-Identifier: MIT
' SPDX-FileCopyrightText: 2025 Namhyeon Go <gnh1201@catswords.re.kr>, Catswords OSS And WelsonJS Contributors
' https://github.com/gnh1201/welsonjs
' 
Public Class AriaCore
    Private S(3, 255) As Byte
    Private KRK(2, 15) As Byte
    Private roundKeyEnc(271) As Byte
    Private roundKeyDec(271) As Byte
    Private R As Integer

    Private ReadOnly KeyBits As Integer

    Public Sub New(key As Byte())
        If key.Length Mod 8 <> 0 Or key.Length < 16 Or key.Length > 32 Then
            Throw New ArgumentException($"ARIA key must be 16, 24, or 32 bytes. Your key length is {key.Length} bytes")
        End If

        InitConstants()

        KeyBits = key.Length * 8
        roundKeyEnc = New Byte(271) {}
        roundKeyDec = New Byte(271) {}
        R = EncKeySetup(key, roundKeyEnc)
        DecKeySetup(key, roundKeyDec)
    End Sub

    Public Sub EncryptBlock(input() As Byte, inOffset As Integer, output() As Byte, outOffset As Integer)
        Dim block(15) As Byte
        Array.Copy(input, inOffset, block, 0, 16)
        Dim encrypted(15) As Byte
        Crypt(block, R, roundKeyEnc, encrypted)
        Array.Copy(encrypted, 0, output, outOffset, 16)
    End Sub

    Public Sub DecryptBlock(input() As Byte, inOffset As Integer, output() As Byte, outOffset As Integer)
        Dim block(15) As Byte
        Array.Copy(input, inOffset, block, 0, 16)
        Dim decrypted(15) As Byte
        Crypt(block, R, roundKeyDec, decrypted)
        Array.Copy(decrypted, 0, output, outOffset, 16)
    End Sub

    Public Sub InitConstants()
        KRK = New Byte(,) {
            {
                &H51, &H7C, &HC1, &HB7, &H27, &H22, &HA, &H94, &HFE, &H13, &HAB, &HE8, &HFA, &H9A, &H6E, &HE0
            },
            {
                &H6D, &HB1, &H4A, &HCC, &H9E, &H21, &HC8, &H20, &HFF, &H28, &HB1, &HD5, &HEF, &H5D, &HE2, &HB0
            },
            {
                &HDB, &H92, &H37, &H1D, &H21, &H26, &HE9, &H70, &H3, &H24, &H97, &H75, &H4, &HE8, &HC9, &HE
            }
        }

        S = New Byte(,) {
        {
            &H63, &H7C, &H77, &H7B, &HF2, &H6B, &H6F, &HC5, &H30, &H1, &H67, &H2B, &HFE, &HD7, &HAB, &H76,
            &HCA, &H82, &HC9, &H7D, &HFA, &H59, &H47, &HF0, &HAD, &HD4, &HA2, &HAF, &H9C, &HA4, &H72, &HC0,
            &HB7, &HFD, &H93, &H26, &H36, &H3F, &HF7, &HCC, &H34, &HA5, &HE5, &HF1, &H71, &HD8, &H31, &H15,
            &H4, &HC7, &H23, &HC3, &H18, &H96, &H5, &H9A, &H7, &H12, &H80, &HE2, &HEB, &H27, &HB2, &H75,
            &H9, &H83, &H2C, &H1A, &H1B, &H6E, &H5A, &HA0, &H52, &H3B, &HD6, &HB3, &H29, &HE3, &H2F, &H84,
            &H53, &HD1, &H0, &HED, &H20, &HFC, &HB1, &H5B, &H6A, &HCB, &HBE, &H39, &H4A, &H4C, &H58, &HCF,
            &HD0, &HEF, &HAA, &HFB, &H43, &H4D, &H33, &H85, &H45, &HF9, &H2, &H7F, &H50, &H3C, &H9F, &HA8,
            &H51, &HA3, &H40, &H8F, &H92, &H9D, &H38, &HF5, &HBC, &HB6, &HDA, &H21, &H10, &HFF, &HF3, &HD2,
            &HCD, &HC, &H13, &HEC, &H5F, &H97, &H44, &H17, &HC4, &HA7, &H7E, &H3D, &H64, &H5D, &H19, &H73,
            &H60, &H81, &H4F, &HDC, &H22, &H2A, &H90, &H88, &H46, &HEE, &HB8, &H14, &HDE, &H5E, &HB, &HDB,
            &HE0, &H32, &H3A, &HA, &H49, &H6, &H24, &H5C, &HC2, &HD3, &HAC, &H62, &H91, &H95, &HE4, &H79,
            &HE7, &HC8, &H37, &H6D, &H8D, &HD5, &H4E, &HA9, &H6C, &H56, &HF4, &HEA, &H65, &H7A, &HAE, &H8,
            &HBA, &H78, &H25, &H2E, &H1C, &HA6, &HB4, &HC6, &HE8, &HDD, &H74, &H1F, &H4B, &HBD, &H8B, &H8A,
            &H70, &H3E, &HB5, &H66, &H48, &H3, &HF6, &HE, &H61, &H35, &H57, &HB9, &H86, &HC1, &H1D, &H9E,
            &HE1, &HF8, &H98, &H11, &H69, &HD9, &H8E, &H94, &H9B, &H1E, &H87, &HE9, &HCE, &H55, &H28, &HDF,
            &H8C, &HA1, &H89, &HD, &HBF, &HE6, &H42, &H68, &H41, &H99, &H2D, &HF, &HB0, &H54, &HBB, &H16
        },
        {
            &HE2, &H4E, &H54, &HFC, &H94, &HC2, &H4A, &HCC, &H62, &HD, &H6A, &H46, &H3C, &H4D, &H8B, &HD1,
            &H5E, &HFA, &H64, &HCB, &HB4, &H97, &HBE, &H2B, &HBC, &H77, &H2E, &H3, &HD3, &H19, &H59, &HC1,
            &H1D, &H6, &H41, &H6B, &H55, &HF0, &H99, &H69, &HEA, &H9C, &H18, &HAE, &H63, &HDF, &HE7, &HBB,
            &H0, &H73, &H66, &HFB, &H96, &H4C, &H85, &HE4, &H3A, &H9, &H45, &HAA, &HF, &HEE, &H10, &HEB,
            &H2D, &H7F, &HF4, &H29, &HAC, &HCF, &HAD, &H91, &H8D, &H78, &HC8, &H95, &HF9, &H2F, &HCE, &HCD,
            &H8, &H7A, &H88, &H38, &H5C, &H83, &H2A, &H28, &H47, &HDB, &HB8, &HC7, &H93, &HA4, &H12, &H53,
            &HFF, &H87, &HE, &H31, &H36, &H21, &H58, &H48, &H1, &H8E, &H37, &H74, &H32, &HCA, &HE9, &HB1,
            &HB7, &HAB, &HC, &HD7, &HC4, &H56, &H42, &H26, &H7, &H98, &H60, &HD9, &HB6, &HB9, &H11, &H40,
            &HEC, &H20, &H8C, &HBD, &HA0, &HC9, &H84, &H4, &H49, &H23, &HF1, &H4F, &H50, &H1F, &H13, &HDC,
            &HD8, &HC0, &H9E, &H57, &HE3, &HC3, &H7B, &H65, &H3B, &H2, &H8F, &H3E, &HE8, &H25, &H92, &HE5,
            &H15, &HDD, &HFD, &H17, &HA9, &HBF, &HD4, &H9A, &H7E, &HC5, &H39, &H67, &HFE, &H76, &H9D, &H43,
            &HA7, &HE1, &HD0, &HF5, &H68, &HF2, &H1B, &H34, &H70, &H5, &HA3, &H8A, &HD5, &H79, &H86, &HA8,
            &H30, &HC6, &H51, &H4B, &H1E, &HA6, &H27, &HF6, &H35, &HD2, &H6E, &H24, &H16, &H82, &H5F, &HDA,
            &HE6, &H75, &HA2, &HEF, &H2C, &HB2, &H1C, &H9F, &H5D, &H6F, &H80, &HA, &H72, &H44, &H9B, &H6C,
            &H90, &HB, &H5B, &H33, &H7D, &H5A, &H52, &HF3, &H61, &HA1, &HF7, &HB0, &HD6, &H3F, &H7C, &H6D,
            &HED, &H14, &HE0, &HA5, &H3D, &H22, &HB3, &HF8, &H89, &HDE, &H71, &H1A, &HAF, &HBA, &HB5, &H81
        },
        {
            &H52, &H9, &H6A, &HD5, &H30, &H36, &HA5, &H38, &HBF, &H40, &HA3, &H9E, &H81, &HF3, &HD7, &HFB,
            &H7C, &HE3, &H39, &H82, &H9B, &H2F, &HFF, &H87, &H34, &H8E, &H43, &H44, &HC4, &HDE, &HE9, &HCB,
            &H54, &H7B, &H94, &H32, &HA6, &HC2, &H23, &H3D, &HEE, &H4C, &H95, &HB, &H42, &HFA, &HC3, &H4E,
            &H8, &H2E, &HA1, &H66, &H28, &HD9, &H24, &HB2, &H76, &H5B, &HA2, &H49, &H6D, &H8B, &HD1, &H25,
            &H72, &HF8, &HF6, &H64, &H86, &H68, &H98, &H16, &HD4, &HA4, &H5C, &HCC, &H5D, &H65, &HB6, &H92,
            &H6C, &H70, &H48, &H50, &HFD, &HED, &HB9, &HDA, &H5E, &H15, &H46, &H57, &HA7, &H8D, &H9D, &H84,
            &H90, &HD8, &HAB, &H0, &H8C, &HBC, &HD3, &HA, &HF7, &HE4, &H58, &H5, &HB8, &HB3, &H45, &H6,
            &HD0, &H2C, &H1E, &H8F, &HCA, &H3F, &HF, &H2, &HC1, &HAF, &HBD, &H3, &H1, &H13, &H8A, &H6B,
            &H3A, &H91, &H11, &H41, &H4F, &H67, &HDC, &HEA, &H97, &HF2, &HCF, &HCE, &HF0, &HB4, &HE6, &H73,
            &H96, &HAC, &H74, &H22, &HE7, &HAD, &H35, &H85, &HE2, &HF9, &H37, &HE8, &H1C, &H75, &HDF, &H6E,
            &H47, &HF1, &H1A, &H71, &H1D, &H29, &HC5, &H89, &H6F, &HB7, &H62, &HE, &HAA, &H18, &HBE, &H1B,
            &HFC, &H56, &H3E, &H4B, &HC6, &HD2, &H79, &H20, &H9A, &HDB, &HC0, &HFE, &H78, &HCD, &H5A, &HF4,
            &H1F, &HDD, &HA8, &H33, &H88, &H7, &HC7, &H31, &HB1, &H12, &H10, &H59, &H27, &H80, &HEC, &H5F,
            &H60, &H51, &H7F, &HA9, &H19, &HB5, &H4A, &HD, &H2D, &HE5, &H7A, &H9F, &H93, &HC9, &H9C, &HEF,
            &HA0, &HE0, &H3B, &H4D, &HAE, &H2A, &HF5, &HB0, &HC8, &HEB, &HBB, &H3C, &H83, &H53, &H99, &H61,
            &H17, &H2B, &H4, &H7E, &HBA, &H77, &HD6, &H26, &HE1, &H69, &H14, &H63, &H55, &H21, &HC, &H7D
        },
        {
            &H30, &H68, &H99, &H1B, &H87, &HB9, &H21, &H78, &H50, &H39, &HDB, &HE1, &H72, &H9, &H62, &H3C,
            &H3E, &H7E, &H5E, &H8E, &HF1, &HA0, &HCC, &HA3, &H2A, &H1D, &HFB, &HB6, &HD6, &H20, &HC4, &H8D,
            &H81, &H65, &HF5, &H89, &HCB, &H9D, &H77, &HC6, &H57, &H43, &H56, &H17, &HD4, &H40, &H1A, &H4D,
            &HC0, &H63, &H6C, &HE3, &HB7, &HC8, &H64, &H6A, &H53, &HAA, &H38, &H98, &HC, &HF4, &H9B, &HED,
            &H7F, &H22, &H76, &HAF, &HDD, &H3A, &HB, &H58, &H67, &H88, &H6, &HC3, &H35, &HD, &H1, &H8B,
            &H8C, &HC2, &HE6, &H5F, &H2, &H24, &H75, &H93, &H66, &H1E, &HE5, &HE2, &H54, &HD8, &H10, &HCE,
            &H7A, &HE8, &H8, &H2C, &H12, &H97, &H32, &HAB, &HB4, &H27, &HA, &H23, &HDF, &HEF, &HCA, &HD9,
            &HB8, &HFA, &HDC, &H31, &H6B, &HD1, &HAD, &H19, &H49, &HBD, &H51, &H96, &HEE, &HE4, &HA8, &H41,
            &HDA, &HFF, &HCD, &H55, &H86, &H36, &HBE, &H61, &H52, &HF8, &HBB, &HE, &H82, &H48, &H69, &H9A,
            &HE0, &H47, &H9E, &H5C, &H4, &H4B, &H34, &H15, &H79, &H26, &HA7, &HDE, &H29, &HAE, &H92, &HD7,
            &H84, &HE9, &HD2, &HBA, &H5D, &HF3, &HC5, &HB0, &HBF, &HA4, &H3B, &H71, &H44, &H46, &H2B, &HFC,
            &HEB, &H6F, &HD5, &HF6, &H14, &HFE, &H7C, &H70, &H5A, &H7D, &HFD, &H2F, &H18, &H83, &H16, &HA5,
            &H91, &H1F, &H5, &H95, &H74, &HA9, &HC1, &H5B, &H4A, &H85, &H6D, &H13, &H7, &H4F, &H4E, &H45,
            &HB2, &HF, &HC9, &H1C, &HA6, &HBC, &HEC, &H73, &H90, &H7B, &HCF, &H59, &H8F, &HA1, &HF9, &H2D,
            &HF2, &HB1, &H0, &H94, &H37, &H9F, &HD0, &H2E, &H9C, &H6E, &H28, &H3F, &H80, &HF0, &H3D, &HD3,
            &H25, &H8A, &HB5, &HE7, &H42, &HB3, &HC7, &HEA, &HF7, &H4C, &H11, &H33, &H3, &HA2, &HAC, &H60
        }
    }
    End Sub

    Public Sub DL(ByRef input() As Byte, ByRef output() As Byte)
        Dim T As Byte
        T = input(3) Xor input(4) Xor input(9) Xor input(14)
        output(0) = input(6) Xor input(8) Xor input(13) Xor T
        output(5) = input(1) Xor input(10) Xor input(15) Xor T
        output(11) = input(2) Xor input(7) Xor input(12) Xor T
        output(14) = input(0) Xor input(5) Xor input(11) Xor T
        T = input(2) Xor input(5) Xor input(8) Xor input(15)
        output(1) = input(7) Xor input(9) Xor input(12) Xor T
        output(4) = input(0) Xor input(11) Xor input(14) Xor T
        output(10) = input(3) Xor input(6) Xor input(13) Xor T
        output(15) = input(1) Xor input(4) Xor input(10) Xor T
        T = input(1) Xor input(6) Xor input(11) Xor input(12)
        output(2) = input(4) Xor input(10) Xor input(15) Xor T
        output(7) = input(3) Xor input(8) Xor input(13) Xor T
        output(9) = input(0) Xor input(5) Xor input(14) Xor T
        output(12) = input(2) Xor input(7) Xor input(9) Xor T
        T = input(0) Xor input(7) Xor input(10) Xor input(13)
        output(3) = input(5) Xor input(11) Xor input(14) Xor T
        output(6) = input(2) Xor input(9) Xor input(12) Xor T
        output(8) = input(1) Xor input(4) Xor input(15) Xor T
        output(13) = input(3) Xor input(6) Xor input(8) Xor T
    End Sub

    Public Sub RotXOR(ByRef s() As Byte, n As Integer, ByRef t() As Byte, Optional offset As Integer = 0)
        Dim i As Integer, q As Integer = n \ 8 : n = n Mod 8
        For i = 0 To 15
            t((q + i) Mod 16 + offset) = t((q + i) Mod 16 + offset) Xor (s(i) >> n)
            If n <> 0 Then
                t((q + i + 1) Mod 16 + offset) = t((q + i + 1) Mod 16 + offset) Xor (s(i) << (8 - n))
            End If
        Next
    End Sub

    Public Function EncKeySetup(ByRef w0() As Byte, ByRef e() As Byte) As Integer
        Dim i As Integer
        Dim R As Integer = (KeyBits + 256) \ 32, q As Integer
        Dim t(15), w1(15), w2(15), w3(15) As Byte
        q = (KeyBits - 128) \ 64
        For i = 0 To 15 : t(i) = S(i Mod 4, KRK(q, i) Xor w0(i)) : Next
        DL(t, w1)
        If R = 14 Then For i = 0 To 7 : w1(i) = w1(i) Xor w0(16 + i) : Next
        If R = 16 Then For i = 0 To 15 : w1(i) = w1(i) Xor w0(16 + i) : Next
        q = If(q = 2, 0, q + 1)
        For i = 0 To 15 : t(i) = S((2 + i) Mod 4, KRK(q, i) Xor w1(i)) : Next
        DL(t, w2)
        For i = 0 To 15 : w2(i) = w2(i) Xor w0(i) : Next
        q = If(q = 2, 0, q + 1)
        For i = 0 To 15 : t(i) = S(i Mod 4, KRK(q, i) Xor w2(i)) : Next
        DL(t, w3)
        For i = 0 To 15 : w3(i) = w3(i) Xor w1(i) : Next
        For i = 0 To 16 * (R + 1) - 1 : e(i) = 0 : Next
        RotXOR(w0, 0, e) : RotXOR(w1, 19, e)
        RotXOR(w1, 0, e, 16) : RotXOR(w2, 19, e, 16)
        RotXOR(w2, 0, e, 32) : RotXOR(w3, 19, e, 32)
        RotXOR(w3, 0, e, 48) : RotXOR(w0, 19, e, 48)
        RotXOR(w0, 0, e, 64) : RotXOR(w1, 31, e, 64)
        RotXOR(w1, 0, e, 80) : RotXOR(w2, 31, e, 80)
        RotXOR(w2, 0, e, 96) : RotXOR(w3, 31, e, 96)
        RotXOR(w3, 0, e, 112) : RotXOR(w0, 31, e, 112)
        RotXOR(w0, 0, e, 128) : RotXOR(w1, 67, e, 128)
        RotXOR(w1, 0, e, 144) : RotXOR(w2, 67, e, 144)
        RotXOR(w2, 0, e, 160) : RotXOR(w3, 67, e, 160)
        RotXOR(w3, 0, e, 176) : RotXOR(w0, 67, e, 176)
        RotXOR(w0, 0, e, 192) : RotXOR(w1, 97, e, 192)
        If R > 12 Then
            RotXOR(w1, 0, e, 208) : RotXOR(w2, 97, e, 208)
            RotXOR(w2, 0, e, 224) : RotXOR(w3, 97, e, 224)
        End If
        If R > 14 Then
            RotXOR(w3, 0, e, 240) : RotXOR(w0, 97, e, 240)
            RotXOR(w0, 0, e, 256) : RotXOR(w1, 109, e, 256)
        End If
        Return R
    End Function

    Public Sub DecKeySetup(ByRef w0() As Byte, ByRef d() As Byte)
        Dim r As Integer = EncKeySetup(w0, d)
        Dim t(15) As Byte, i, j As Integer
        For j = 0 To 15
            t(j) = d(j)
            d(j) = d(16 * r + j)
            d(16 * r + j) = t(j)
        Next
        For i = 1 To r \ 2
            Dim input1(15), input2(15), output1(15), output2(15) As Byte
            Array.Copy(d, i * 16, input1, 0, 16)
            DL(input1, output1)
            Array.Copy(d, (r - i) * 16, input2, 0, 16)
            DL(input2, output2)
            Array.Copy(output2, 0, d, i * 16, 16)
            Array.Copy(output1, 0, d, (r - i) * 16, 16)
        Next
    End Sub

    Public Sub Crypt(ByRef p() As Byte, R As Integer, ByRef e() As Byte, ByRef c() As Byte)
        Dim i, j As Integer
        Dim t(15) As Byte
        Dim eOffset As Integer = 0
        For j = 0 To 15 : c(j) = p(j) : Next
        For i = 0 To (R \ 2) - 1
            For j = 0 To 15 : t(j) = S(j Mod 4, e(eOffset + j) Xor c(j)) : Next
            DL(t, c)
            eOffset += 16
            For j = 0 To 15 : t(j) = S((2 + j) Mod 4, e(eOffset + j) Xor c(j)) : Next
            DL(t, c)
            eOffset += 16
        Next
        DL(c, t)
        For j = 0 To 15 : c(j) = e(eOffset + j) Xor t(j) : Next
    End Sub
End Class
