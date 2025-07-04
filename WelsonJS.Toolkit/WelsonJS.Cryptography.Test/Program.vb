' Program.cs (WelsonJS.Cryptography.Test)
' SPDX-License-Identifier: MIT
' SPDX-FileCopyrightText: 2025 Namhyeon Go <gnh1201@catswords.re.kr>, Catswords OSS And WelsonJS Contributors
' https://github.com/gnh1201/welsonjs
' 
Imports System.IO
Imports System.Security.Cryptography
Imports System.Text

Module Program
    Sub Main(args As String())
        Console.WriteLine("Start SEED encryption and decryption test")
        Dim cipher As New WelsonJS.Cryptography.SeedAlgorithm()

        cipher.Key = {&H2B, &H7E, &H15, &H16, &H28, &HAE, &HD2, &HA6, &HAB, &HF7, &H15, &H88, &H9, &HCF, &H4F, &H3C}
        cipher.IV = {&H26, &H8D, &H66, &HA7, &H35, &HA8, &H1A, &H81, &H6F, &HBA, &HD9, &HFA, &H36, &H16, &H25, &H1}
        cipher.Mode = CipherMode.ECB
        cipher.Padding = PaddingMode.PKCS7

        RunTest(cipher)
    End Sub

    Public Sub RunTest(cipher As SymmetricAlgorithm)
        Dim inputBytes As Byte() = {&H0, &H1, &H2, &H3, &H4, &H5, &H6, &H7, &H8, &H9, &HA, &HB, &HC, &HD, &HE, &HF, &H0, &H1}

        Console.WriteLine("Original bytes (HEX):")
        PrintHex(inputBytes)

        Dim encryptor As ICryptoTransform = cipher.CreateEncryptor()
        Dim encrypted As Byte() = ApplyTransform(encryptor, inputBytes)
        Console.WriteLine("Encrypted (HEX):")
        PrintHex(encrypted)

        Dim decryptor As ICryptoTransform = cipher.CreateDecryptor()
        Dim decrypted As Byte() = ApplyTransform(decryptor, encrypted)
        Console.WriteLine("Decrypted (HEX):")
        PrintHex(decrypted)
    End Sub

    Private Function ApplyTransform(transformer As ICryptoTransform, input As Byte()) As Byte()
        Using ms As New MemoryStream()
            Using cs As New CryptoStream(ms, transformer, CryptoStreamMode.Write)
                cs.Write(input, 0, input.Length)
                cs.FlushFinalBlock()
                Return ms.ToArray()
            End Using
        End Using
    End Function

    Private Sub PrintHex(data As Byte())
        For Each b As Byte In data
            Console.Write("{0:X2} ", b)
        Next
        Console.WriteLine()
    End Sub
End Module