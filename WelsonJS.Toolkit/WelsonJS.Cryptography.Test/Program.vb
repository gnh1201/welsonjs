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

        cipher.Key = {&H88, &HE3, &H4F, &H8F, &H8, &H17, &H79, &HF1, &HE9, &HF3, &H94, &H37, &HA, &HD4, &H5, &H89}
        ' cipher.IV = {&H26, &H8D, &H66, &HA7, &H35, &HA8, &H1A, &H81, &H6F, &HBA, &HD9, &HFA, &H36, &H16, &H25, &H1}
        cipher.Mode = CipherMode.ECB
        cipher.Padding = PaddingMode.PKCS7

        RunTest(cipher)
    End Sub

    Public Sub RunTest(cipher As SymmetricAlgorithm)
        Dim inputBytes As Byte() = {&H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &HFE}

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
        Return transformer.TransformFinalBlock(input, 0, input.Length)
    End Function

    Private Sub PrintHex(data As Byte())
        For Each b As Byte In data
            Console.Write("{0:X2} ", b)
        Next
        Console.WriteLine()
    End Sub
End Module