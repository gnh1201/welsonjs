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

        ' SEED algorithm
        Console.WriteLine("Start SEED encryption and decryption test")
        Dim seedCipher As New WelsonJS.Cryptography.SeedAlgorithm()
        seedCipher.Key = {&H9F, &H38, &H79, &HB9, &H64, &HCB, &H88, &H49, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0}
        ' seedCipher.IV = {&H26, &H8D, &H66, &HA7, &H35, &HA8, &H1A, &H81, &H6F, &HBA, &HD9, &HFA, &H36, &H16, &H25, &H1}
        seedCipher.Mode = CipherMode.ECB
        seedCipher.Padding = PaddingMode.PKCS7
        RunTest(seedCipher)
        Console.WriteLine()

        ' ARIA algorithm
        Console.WriteLine("Start ARIA encryption and decryption test")
        Dim ariaCipher As New WelsonJS.Cryptography.AriaAlgorithm()
        ariaCipher.Key = {&H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0}
        ' ariaChiper.IV = {&H0F, &H1E, &H2D, &H3C, &H4B, &H5A, &H69, &H78, &H87, &H96, &HA5, &HB4, &HC3, &HD2, &HE1, &HF0}
        ariaCipher.Mode = CipherMode.ECB
        ariaCipher.Padding = PaddingMode.PKCS7
        RunTest(ariaCipher)
        Console.WriteLine()

        ' HIGHT algorithm
        Console.WriteLine("Start HIGHT encryption and decryption test")
        Dim hightCipher As New WelsonJS.Cryptography.HightAlgorithm()
        hightCipher.Key = {&H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0}
        ' hightCipher.IV = {&H0F, &H1E, &H2D, &H3C, &H4B, &H5A, &H69, &H78, &H87, &H96, &HA5, &HB4, &HC3, &HD2, &HE1, &HF0}
        hightCipher.Mode = CipherMode.ECB
        hightCipher.Padding = PaddingMode.PKCS7
        RunTest(hightCipher)
        Console.WriteLine()

    End Sub

    Public Sub RunTest(cipher As SymmetricAlgorithm)
        ' Dim inputBytes As Byte() = {&H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0} ' SEED test vector
        Dim inputBytes As Byte() = {&H80, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0} ' ARIA test vector
        ' Dim inputBytes As Byte() = {&H80, &H0, &H0, &H0, &H0, &H0, &H0, &H0} ' HIGHT test vector
        Console.WriteLine("Key (HEX):")
        PrintHex(cipher.Key)

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