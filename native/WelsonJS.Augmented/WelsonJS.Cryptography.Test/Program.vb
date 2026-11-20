' Program.vb (WelsonJS.Cryptography.Test)
' SPDX-License-Identifier: MIT
' SPDX-FileCopyrightText: 2025 Namhyeon Go <gnh1201@catswords.re.kr>, Catswords OSS And WelsonJS Contributors
' https://github.com/gnh1201/welsonjs
'
' Checks WelsonJS.Cryptography against published test vectors and, for the modes of
' operation, against the AES implementation of .NET. Exits with 1 if a check fails.
'
'   dotnet run --project WelsonJS.Cryptography.Test
'
Imports System.IO
Imports System.Security.Cryptography
Imports WelsonJS.Cryptography

Module Program
    Private failures As Integer = 0
    Private checks As Integer = 0
    Private ReadOnly random As New Random(20261120)

    Function Main(args As String()) As Integer
        CipherVectors()
        ModeVectors()
        ModesAgainstDotNet()
        AuthenticatedModes()
        Cmac()
        Streaming()
        ErrorsAndApi()

        Console.WriteLine()
        Console.WriteLine("{0} checks, {1} failed", checks, failures)
        Return If(failures = 0, 0, 1)
    End Function

    ' ---------------------------------------------------------------- block ciphers (ECB)

    Sub CipherVectors()
        Section("Block ciphers: published test vectors")

        ' ARIA, RFC 5794 Appendix A
        Ecb(New AriaAlgorithm(), "ARIA-128 (RFC 5794 A.1)", "000102030405060708090a0b0c0d0e0f", "00112233445566778899aabbccddeeff", "d718fbd6ab644c739da95f3be6451778")
        Ecb(New AriaAlgorithm(), "ARIA-192 (RFC 5794 A.2)", "000102030405060708090a0b0c0d0e0f1011121314151617", "00112233445566778899aabbccddeeff", "26449c1805dbe7aa25a468ce263a9e79")
        Ecb(New AriaAlgorithm(), "ARIA-256 (RFC 5794 A.3)", "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f", "00112233445566778899aabbccddeeff", "f92bd7c79fb72e2f2b8f80c1972d24fc")

        ' SEED, RFC 4269 Appendix B
        Ecb(New SeedAlgorithm(), "SEED (RFC 4269 B.1)", "00000000000000000000000000000000", "000102030405060708090a0b0c0d0e0f", "5ebac6e0054e166819aff1cc6d346cdb")
        Ecb(New SeedAlgorithm(), "SEED (RFC 4269 B.2)", "000102030405060708090a0b0c0d0e0f", "00000000000000000000000000000000", "c11f22f201405050844835 97e4370f43".Replace(" ", ""))
        Ecb(New SeedAlgorithm(), "SEED (RFC 4269 B.3)", "4706480851e61be85d74bfb3fd956185", "83a2f8a288641fb9a4e9a5cc2f131c7d", "ee54d13ebcae706d226bc3142cd40d4a")
        Ecb(New SeedAlgorithm(), "SEED (RFC 4269 B.4)", "28dbc3bc49ffd87dcfa509b11d422be7", "b41e6be2eba84a148e2eed84593c5ec7", "9b9b7bfcd1813cb95d0b3618f40f5122")

        ' HIGHT, the KISA reference code (KISA_HIGHT_ECB.c, as collected in Crypto++ TestVectors/hight.txt)
        Ecb(New HightAlgorithm(), "HIGHT (KISA ECB 1)", "88e34f8f081779f1e9f394370ad40589", "d76d0d18327ec562", "e4bc2e312277e4dd")
        Ecb(New HightAlgorithm(), "HIGHT (KISA ECB 2)", "2923be84e16cd6ae529049f1f1bbe9eb", "b3a6db3c870c3e99", "23cad1a3cddf7eab")
        Ecb(New HightAlgorithm(), "HIGHT (KISA ECB 3)", "245e0d1c06b747deb3124dc843bb8ba6", "1f035a7d0938251f", "52bd91bb26f8ed99")
        Ecb(New HightAlgorithm(), "HIGHT (KISA ECB 7)", "b2eac9cc53bf67d6bf14d67e2ddc8e66", "83ef574961ff698f", "3921c83bfe5e596e")
    End Sub

    Sub Ecb(alg As BlockCipherAlgorithm, name As String, key As String, plaintext As String, ciphertext As String)
        alg.Key = Hex(key)
        alg.Mode = CipherMode.ECB
        alg.Padding = PaddingMode.None
        Dim encrypted = alg.CreateEncryptor().TransformFinalBlock(Hex(plaintext), 0, Hex(plaintext).Length)
        Check(name & " encrypt", encrypted, Hex(ciphertext))
        Dim decrypted = alg.CreateDecryptor().TransformFinalBlock(Hex(ciphertext), 0, Hex(ciphertext).Length)
        Check(name & " decrypt", decrypted, Hex(plaintext))
    End Sub

    ' ---------------------------------------------------------------- modes: published vectors

    Sub ModeVectors()
        Section("Modes of operation: published test vectors")

        ' SEED-CBC, RFC 4196 section 4, cases 1 and 2
        Dim seed As New SeedAlgorithm() With {.Mode = CipherMode.CBC, .Padding = PaddingMode.None}
        Cbc(seed, "SEED-CBC (RFC 4196 case 1)", "ed2401ad22fa255991bafdb01fefd697", "93eb149f92c9905bae5cd34da06c3c8e",
            "b40d7003d9b6904b35622750c91a24575bb9a632364aa26e3ac0cf3a9c9d0dcb",
            "f072c5b1a0588c105af8301adcd91dd067f6822155304bf3aad75ceb44341c25")
        Cbc(seed, "SEED-CBC (RFC 4196 case 2)", "88e34f8f081779f1e9f394370ad40589", "268d66a735a81a816fbad9fa36162501",
            "d76d0d18327ec562b15e6bc365ac0c0f8d41e0bb938568aeebfd92ed1affa096394d20fc5277ddfc4de8b0fce1eb2b93d4ae40ef4768c613b50b8942f7d4b9b3",
            "a293eae9d9aebfac37ba714bd774e427e8b706d7e7d9a097228639e0b62b3b34ced11609cef2abaaec2edf979308f379c31527a8267783e5cba3538982b48d06")

        ' HIGHT-CBC and HIGHT-CTR, the KISA reference code (KISA_HIGHT_CBC.c, KISA_HIGHT_CTR.c); the
        ' CTR counter starts at ...00fe, so it carries into the next byte.
        Dim block16 = "000102030405060708090a0b0c0d0e0f"
        Dim hight As New HightAlgorithm() With {.Mode = CipherMode.CBC, .Padding = PaddingMode.None}
        Cbc(hight, "HIGHT-CBC (KISA, 64 bytes)", "88e34f8f081779f1e9f394370ad40589", "268d66a735a81a81", block16 & block16 & block16 & block16,
            "ce1595085a188c28c18d7708d9c125864b3daf2bf20d5247634a00012fa0e9f0449de2bcc168516c0e2745ac37c1a6743a1ea51eb407d14ec8c4b435ca05cc62")
        Dim hightPlain = Hex(block16 & block16 & block16 & block16 & "00")
        Check("HIGHT-CTR (KISA, 65 bytes)", hight.CreateCtrTransform(Hex("88e34f8f081779f1e9f394370ad40589"), Hex("00000000000000fe")).TransformFinalBlock(hightPlain, 0, hightPlain.Length),
              Hex("b3d1fffcc2a19bc0130dc1621c5839988ad7c59b40a2d5b9577adf09b6a19ca3d76a453bf70b0b6c784e51b768d31cefb35ef7fa5f075b099f60491e609fc213f2"))

        ' ARIA-CTR, RFC 8269 A.1 (the encrypted RTP payload; the IV is the first counter block)
        Dim payload = Hex("f57af5fd4ae19562976ec57a5a7ad55a5af5c5e5c5fdf5c55ad57a4a7272d57262e9729566ed66e97ac54a4a5a7ad5e1" &
                          "5ae5fdd5fd5ac5d56ae56ad5c572d54ae54ac55a956afd6aed5a4ac562957a9516991691d572fd14e97ae962ed7a9f4a" &
                          "955af572e162f57a956666e17ae1f54a95f566d54a66e16e4afd6a9f7ae1c5c55ae5d56afde916c5e94a6ec56695e14a" &
                          "fde1148416e94ad57ac5146ed59d1cc5")
        Dim aria As New AriaAlgorithm()
        Dim ctrIv = Hex("cd3a7c42e69915ed7a2a263985640000")
        Dim ctr128 = aria.CreateCtrTransform(Hex("0c5ffd37a11edc42c325287fc0604f2e"), ctrIv).TransformFinalBlock(payload, 0, payload.Length)
        Check("ARIA-128-CTR (RFC 8269 A.1.1)", ctr128, Hex(
            "1bf753f412e6f35058cc398dc851aae3a6ccdcb463fbed9cfb3de2fb76fdffa9e481f5efb64c92487f59dabbc7cc72da" &
            "092485f3fbad87888820b86037311fa44330e18a59a1e1338ba2c21458493a57463475c54691f91cec785429119e0dfc" &
            "d9048f90e07fecd50b528e8c62ee6e71445de5d7f659405135aff3604c2ca4ff4aaca40809cb9eee42cc4ad232307570" &
            "81ca289f2851d3315e9568b501fdce6d"))
        Dim ctr256 = aria.CreateCtrTransform(Hex("0c5ffd37a11edc42c325287fc0604f2e3e8cd5671a00fe3216aa5eb105783b54"), ctrIv).TransformFinalBlock(payload, 0, payload.Length)
        Check("ARIA-256-CTR (RFC 8269 A.1.2)", ctr256, Hex(
            "c424c59fd5696305e5b13d8e8ca7656617ccd7471088af9debf07b55c750f804a5ac2b737be48140958a9b420524112a" &
            "e72e4da5bca59d2b1019ddd7dbdc30b43d5f046152ced40947d62d2c93e7b8e50f02db2b6b61b010e4c1566884de1fa9" &
            "702cdf8157e8aedfe3dd77c76bb50c25ae4d624615c15acfdeeb5f79482aaa01d3e4c05eb601eca2bd10518e9d46b021" &
            "16359232e9eac0fabd05235dd09e6dea"))
        Dim back = aria.CreateCtrTransform(Hex("0c5ffd37a11edc42c325287fc0604f2e"), ctrIv).TransformFinalBlock(ctr128, 0, ctr128.Length)
        Check("ARIA-128-CTR decrypt", back, payload)

        ' ARIA-GCM, RFC 8269 A.2 (the encrypted payload ends with the 16-byte tag)
        Dim nonce = Hex("000020e8f5eb00000000315e")
        Dim aad = Hex("8008315ebf2e6fe020e8f5eb")
        Gcm(aria, "ARIA-128-GCM (RFC 8269 A.2.1)", "e91e5e75da65554a48181f3846349562", nonce, aad, payload,
            "4d8a9a0675550c704b17d8c9ddc81a5cd6f7da34f2fe1b3db7cb3dfb9697102ea0f3c1fc2dbc873d44bceeae8e4442974ba21ff6789d3272613fb9631a7cf3f1" &
            "4bacbeb421633a90ffbe58c2fa6bdca534f10d0de0502ce1d531b6336e58878278531e5c22bc6c85bbd784d78d9e680aa19031aaf89101d669d7a3965c1f7e16" &
            "229d7463e0535f4e253f5d18187d40b8ae0f564bd970b5e7e2adfb211e89a9535abace3f37f5a736f4be984bbffbedc1")
        Gcm(aria, "ARIA-256-GCM (RFC 8269 A.2.2)", "0c5ffd37a11edc42c325287fc0604f2e3e8cd5671a00fe3216aa5eb105783b54", nonce, aad, payload,
            "6f9e4bcbc8c85fc0128fb1e4a0a20cb9932ff74581f54fc013dd054b19f99371425b352d97d3f337b90b63d1b082adeeea9d2d7391897d591b985e55fb50cb53" &
            "50cf7d38dc27dda127c078a149c8eb98083d66363a46e3726af217d3a00275ad5bf772c7610ea4c23006878f0ee69a8397703169a419303f40b72e4573714d19" &
            "e2697df61e7c7252e5abc6bade876ac4961bfac4d5e867afca351a48aed52822e210d6ced2cf430ff841472915e7ef48")

        ' AES through the generic modes: NIST SP 800-38A F.2.1, F.3.13 (CFB128), F.3.7 (CFB8), F.4.1, F.5.1
        Dim aesKey = Hex("2b7e151628aed2a6abf7158809cf4f3c")
        Dim iv = Hex("000102030405060708090a0b0c0d0e0f")
        Dim p = Hex("6bc1bee22e409f96e93d7e117393172aae2d8a571e03ac9c9eb76fac45af8e5130c81c46a35ce411e5fbc1191a0a52eff69f2445df4f9b17ad2b417be66c3710")
        AesMode("AES-128-CBC (SP 800-38A F.2.1)", BlockCipherMode.CBC, aesKey, iv, 0, p,
                "7649abac8119b246cee98e9b12e9197d5086cb9b507219ee95db113a917678b273bed6b8e3c1743b7116e69e222295163ff1caa1681fac09120eca307586e1a7")
        AesMode("AES-128-CFB128 (SP 800-38A F.3.13)", BlockCipherMode.CFB, aesKey, iv, 128, p,
                "3b3fd92eb72dad20333449f8e83cfb4ac8a64537a0b3a93fcde3cdad9f1ce58b26751f67a3cbb140b1808cf187a4f4dfc04b05357c5d1c0eeac4c66f9ff7f2e6")
        AesMode("AES-128-CFB8 (SP 800-38A F.3.7)", BlockCipherMode.CFB, aesKey, iv, 8, Hex("6bc1bee22e409f96e93d7e117393172aae2d"),
                "3b79424c9c0dd436bace9e0ed4586a4f32b9")
        AesMode("AES-128-OFB (SP 800-38A F.4.1)", BlockCipherMode.OFB, aesKey, iv, 0, p,
                "3b3fd92eb72dad20333449f8e83cfb4a7789508d16918f03f53c52dac54ed8259740051e9c5fecf64344f7a82260edcc304c6528f659c77866a510d9c1d6ae5e")
        AesMode("AES-128-CTR (SP 800-38A F.5.1)", BlockCipherMode.CTR, aesKey, Hex("f0f1f2f3f4f5f6f7f8f9fafbfcfdfeff"), 0, p,
                "874d6191b620e3261bef6864990db6ce9806f66b7970fdff8617187bb9fffdff5ae4df3edbd5d35e5b4f09020db03eab1e031dda2fbe03d1792170a0f3009cee")

        ' AES CBC with ciphertext stealing, RFC 3962 Appendix B (CS3, zero IV)
        Dim ctsKey = Hex("636869636b656e207465726979616b69")
        Dim zero(15) As Byte
        Dim text = Hex("4920776f756c64206c696b65207468652047656e6572616c20476175277320436869636b656e2c20706c656173652c20616e6420776f6e746f6e20736f75702e")
        Dim ctsCases = New String()() {
            New String() {"17", "c6353568f2bf8cb4d8a580362da7ff7f97"},
            New String() {"31", "fc00783e0efdb2c1d445d4c8eff7ed2297687268d6ecccc0c07b25e25ecfe5"},
            New String() {"32", "39312523a78662d5be7fcbcc98ebf5a897687268d6ecccc0c07b25e25ecfe584"},
            New String() {"47", "97687268d6ecccc0c07b25e25ecfe584b3fffd940c16a18c1b5549d2f838029e39312523a78662d5be7fcbcc98ebf5"},
            New String() {"48", "97687268d6ecccc0c07b25e25ecfe5849dad8bbb96c4cdc03bc103e1a194bbd839312523a78662d5be7fcbcc98ebf5a8"},
            New String() {"64", "97687268d6ecccc0c07b25e25ecfe58439312523a78662d5be7fcbcc98ebf5a84807efe836ee89a526730dbc2f7bc8409dad8bbb96c4cdc03bc103e1a194bbd8"}}
        For Each c In ctsCases
            Dim n = Integer.Parse(c(0))
            Dim input(n - 1) As Byte
            Array.Copy(text, input, n)
            AesMode("AES-128-CTS (RFC 3962, " & n & " bytes)", BlockCipherMode.CTS, ctsKey, zero, 0, input, c(1))
        Next

        ' AES-GCM with a 64-bit IV (GCM specification, test case 5), which takes the GHASH path for J0
        Dim gcmCipher As New BlockCipherGcm(New AesBlockCipher(Hex("feffe9928665731c6d6a8f9467308308")))
        Dim tc5p = Hex("d9313225f88406e5a55909c5aff5269a86a7a9531534f7da2e4c303d8a318a721c3c0c95956809532fcf0e2449a6b525b16aedf5aa0de657ba637b39")
        Dim tc5a = Hex("feedfacedeadbeeffeedfacedeadbeefabaddad2")
        Dim sealed = gcmCipher.Seal(Hex("cafebabefacedbad"), tc5p, tc5a)
        Check("AES-128-GCM, 8-byte IV (GCM test case 5)", sealed, Hex(
            "61353b4c2806934a777ff51fa22a4755699b2a714fcdc6f83766e5f97b6c742373806900e49f24b22b097544d4896b424989b5e1ebac0f07c23f4598" &
            "3612d2e79e3b0785561be14aaca2fccb"))
        Check("AES-128-GCM, 8-byte IV open", gcmCipher.Open(Hex("cafebabefacedbad"), sealed, tc5a), tc5p)
    End Sub

    Sub Cbc(alg As BlockCipherAlgorithm, name As String, key As String, iv As String, plaintext As String, ciphertext As String)
        alg.Key = Hex(key)
        alg.IV = Hex(iv)
        Check(name & " encrypt", alg.CreateEncryptor().TransformFinalBlock(Hex(plaintext), 0, Hex(plaintext).Length), Hex(ciphertext))
        Check(name & " decrypt", alg.CreateDecryptor().TransformFinalBlock(Hex(ciphertext), 0, Hex(ciphertext).Length), Hex(plaintext))
    End Sub

    Sub Gcm(alg As BlockCipherAlgorithm, name As String, key As String, nonce As Byte(), aad As Byte(), plaintext As Byte(), sealedHex As String)
        Dim gcmCipher = alg.CreateGcm(Hex(key))
        Dim sealed = gcmCipher.Seal(nonce, plaintext, aad)
        Check(name & " seal", sealed, Hex(sealedHex))
        Check(name & " open", gcmCipher.Open(nonce, sealed, aad), plaintext)
        sealed(sealed.Length - 1) = sealed(sealed.Length - 1) Xor CByte(1)
        Throws(name & " rejects a changed tag", Sub() gcmCipher.Open(nonce, sealed, aad))
    End Sub

    Sub AesMode(name As String, mode As BlockCipherMode, key As Byte(), iv As Byte(), feedback As Integer, plaintext As Byte(), ciphertext As String)
        Dim enc As New BlockCipherTransform(New AesBlockCipher(key), mode, iv, True, PaddingMode.None, feedback)
        Check(name & " encrypt", enc.TransformFinalBlock(plaintext, 0, plaintext.Length), Hex(ciphertext))
        Dim dec As New BlockCipherTransform(New AesBlockCipher(key), mode, iv, False, PaddingMode.None, feedback)
        Check(name & " decrypt", dec.TransformFinalBlock(Hex(ciphertext), 0, Hex(ciphertext).Length), plaintext)
    End Sub

    ' ---------------------------------------------------------------- modes: against .NET AES

    Sub ModesAgainstDotNet()
        Section("Modes of operation: the same output as .NET AES (random data)")
        Dim modes = New Object()() {
            New Object() {"CBC", CipherMode.CBC, BlockCipherMode.CBC, 128},
            New Object() {"ECB", CipherMode.ECB, BlockCipherMode.ECB, 128},
            New Object() {"CFB128", CipherMode.CFB, BlockCipherMode.CFB, 128},
            New Object() {"CFB8", CipherMode.CFB, BlockCipherMode.CFB, 8}}
        Dim paddings = New PaddingMode() {PaddingMode.PKCS7, PaddingMode.ANSIX923, PaddingMode.Zeros, PaddingMode.None}
        For Each m In modes
            For Each padding In paddings
                Dim ok = True
                For trial As Integer = 0 To 59
                    Dim length = random.Next(0, 100)
                    Dim feedback = CInt(m(3))
                    If padding = PaddingMode.None AndAlso length Mod (feedback \ 8) <> 0 Then length -= length Mod (feedback \ 8)
                    If padding = PaddingMode.None AndAlso CType(m(1), CipherMode) <> CipherMode.CFB Then length -= length Mod 16
                    Dim key = RandomBytes(16), iv = RandomBytes(16), data = RandomBytes(length)
                    Using aes = System.Security.Cryptography.Aes.Create()
                        aes.Key = key : aes.IV = iv : aes.Mode = CType(m(1), CipherMode) : aes.Padding = padding : aes.FeedbackSize = feedback
                        Dim expected = aes.CreateEncryptor().TransformFinalBlock(data, 0, data.Length)
                        Dim actual = New BlockCipherTransform(New AesBlockCipher(key), CType(m(2), BlockCipherMode), iv, True, padding, feedback).TransformFinalBlock(data, 0, data.Length)
                        If Not Same(actual, expected) Then ok = False
                        Dim mine = New BlockCipherTransform(New AesBlockCipher(key), CType(m(2), BlockCipherMode), iv, False, padding, feedback).TransformFinalBlock(expected, 0, expected.Length)
                        Dim theirs = aes.CreateDecryptor().TransformFinalBlock(expected, 0, expected.Length)
                        If Not Same(mine, theirs) Then ok = False
                    End Using
                Next
                CheckTrue("AES-" & CStr(m(0)) & " " & padding.ToString() & ", 60 random messages", ok)
            Next
        Next
    End Sub

    ' ---------------------------------------------------------------- GCM, CCM

    Sub AuthenticatedModes()
        Section("GCM and CCM: the same output as .NET AesGcm and AesCcm (random data)")
        Dim okGcm = True
        For trial As Integer = 0 To 299
            Dim key = RandomBytes(If(trial Mod 3 = 0, 32, 16)), nonce = RandomBytes(12)
            Dim data = RandomBytes(random.Next(0, 90)), aad = RandomBytes(random.Next(0, 40))
            Dim tagSize = 12 + random.Next(0, 5)
            Dim expectedC(data.Length - 1) As Byte, expectedT(tagSize - 1) As Byte
            Using reference As New AesGcm(key)
                reference.Encrypt(nonce, data, expectedC, expectedT, aad)
            End Using
            Dim gcmCipher As New BlockCipherGcm(New AesBlockCipher(key))
            Dim c(data.Length - 1) As Byte, t(tagSize - 1) As Byte
            gcmCipher.Encrypt(nonce, data, c, t, aad)
            If Not Same(c, expectedC) OrElse Not Same(t, expectedT) Then okGcm = False
            Dim back(data.Length - 1) As Byte
            gcmCipher.Decrypt(nonce, c, t, back, aad)
            If Not Same(back, data) Then okGcm = False
        Next
        CheckTrue("AES-GCM, 300 random messages (tags 12 to 16 bytes)", okGcm)

        Dim okCcm = True
        For trial As Integer = 0 To 299
            Dim key = RandomBytes(If(trial Mod 3 = 0, 32, 16)), nonce = RandomBytes(7 + random.Next(0, 7))
            Dim data = RandomBytes(random.Next(0, 90)), aad = RandomBytes(random.Next(0, 40))
            Dim tagSize = 4 + 2 * random.Next(0, 7)
            Dim expectedC(data.Length - 1) As Byte, expectedT(tagSize - 1) As Byte
            Using reference As New AesCcm(key)
                reference.Encrypt(nonce, data, expectedC, expectedT, aad)
            End Using
            Dim ccmCipher As New BlockCipherCcm(New AesBlockCipher(key))
            Dim c(data.Length - 1) As Byte, t(tagSize - 1) As Byte
            ccmCipher.Encrypt(nonce, data, c, t, aad)
            If Not Same(c, expectedC) OrElse Not Same(t, expectedT) Then okCcm = False
            Dim back(data.Length - 1) As Byte
            ccmCipher.Decrypt(nonce, c, t, back, aad)
            If Not Same(back, data) Then okCcm = False
        Next
        CheckTrue("AES-CCM, 300 random messages (nonces 7 to 13, tags 4 to 16 bytes)", okCcm)

        ' The library's own ciphers in GCM and CCM: round trip, and a changed byte is refused.
        For Each alg As BlockCipherAlgorithm In New BlockCipherAlgorithm() {New AriaAlgorithm(), New SeedAlgorithm()}
            alg.GenerateKey()
            Dim name = alg.GetType().Name.Replace("Algorithm", "").ToUpperInvariant()
            Dim data = RandomBytes(77), aad = RandomBytes(9)
            Dim g = alg.CreateGcm(), gn = RandomBytes(12)
            Dim gs = g.Seal(gn, data, aad)
            Check(name & "-GCM round trip", g.Open(gn, gs, aad), data)
            gs(5) = gs(5) Xor CByte(&H80)
            Throws(name & "-GCM rejects a changed ciphertext", Sub() g.Open(gn, gs, aad))
            Dim cc = alg.CreateCcm(), cn = RandomBytes(11)
            Dim cs = cc.Seal(cn, data, aad, 8)
            Check(name & "-CCM round trip", cc.Open(cn, cs, aad, 8), data)
            Throws(name & "-CCM rejects other associated data", Sub() cc.Open(cn, cs, RandomBytes(9), 8))
        Next
        Throws("HIGHT has no GCM (64-bit block)", Sub() Call New HightAlgorithm().CreateGcm())
        Throws("HIGHT has no CCM (64-bit block)", Sub() Call New HightAlgorithm().CreateCcm())
    End Sub

    ' ---------------------------------------------------------------- CMAC

    Sub Cmac()
        Section("CMAC")
        ' AES-CMAC, RFC 4493 section 4
        Dim key = Hex("2b7e151628aed2a6abf7158809cf4f3c")
        Dim m = Hex("6bc1bee22e409f96e93d7e117393172aae2d8a571e03ac9c9eb76fac45af8e5130c81c46a35ce411e5fbc1191a0a52eff69f2445df4f9b17ad2b417be66c3710")
        Dim cases = New Object()() {
            New Object() {0, "bb1d6929e95937287fa37d129b756746"},
            New Object() {16, "070a16b46b4d4144f79bdd9dd04a287c"},
            New Object() {40, "dfa66747de9ae63030ca32611497c827"},
            New Object() {64, "51f0bebf7e3b9d92fc49741779363cfe"}}
        For Each c In cases
            Dim n = CInt(c(0))
            Using mac As New BlockCipherCmac(New AesBlockCipher(key))
                Check("AES-CMAC (RFC 4493, " & n & " bytes)", mac.ComputeHash(m, 0, n), Hex(CStr(c(1))))
            End Using
        Next

        ' Fed in pieces, the MAC is the same as in one call (64-bit and 128-bit blocks).
        For Each alg As BlockCipherAlgorithm In New BlockCipherAlgorithm() {New AriaAlgorithm(), New SeedAlgorithm(), New HightAlgorithm()}
            alg.GenerateKey()
            Dim name = alg.GetType().Name.Replace("Algorithm", "").ToUpperInvariant()
            Dim ok = True
            For trial As Integer = 0 To 49
                Dim data = RandomBytes(random.Next(0, 70))
                Dim whole As Byte(), pieces As Byte()
                Using mac = alg.CreateCmac()
                    whole = mac.ComputeHash(data)
                End Using
                Using mac = alg.CreateCmac()
                    Dim pos = 0
                    While pos < data.Length
                        Dim n = Math.Min(random.Next(1, 20), data.Length - pos)
                        mac.TransformBlock(data, pos, n, Nothing, 0)
                        pos += n
                    End While
                    mac.TransformFinalBlock(New Byte() {}, 0, 0)
                    pieces = mac.Hash
                End Using
                If Not Same(whole, pieces) OrElse whole.Length <> alg.BlockSize \ 8 Then ok = False
            Next
            CheckTrue(name & "-CMAC, 50 messages in one call and in pieces", ok)
        Next
    End Sub

    ' ---------------------------------------------------------------- CryptoStream

    Sub Streaming()
        Section("CryptoStream: chunked writes give the one-call result, and decryption restores the data")
        Dim algs = New BlockCipherAlgorithm() {New AriaAlgorithm(), New SeedAlgorithm(), New HightAlgorithm()}
        Dim cfgs = New Object()() {
            New Object() {CipherMode.ECB, 0}, New Object() {CipherMode.CBC, 0}, New Object() {CipherMode.CFB, 0},
            New Object() {CipherMode.CFB, 8}, New Object() {CipherMode.OFB, 0}, New Object() {CipherMode.CTS, 0}}
        Dim paddings = New PaddingMode() {PaddingMode.PKCS7, PaddingMode.ANSIX923, PaddingMode.ISO10126, PaddingMode.Zeros, PaddingMode.None}
        For Each alg In algs
            Dim name = alg.GetType().Name.Replace("Algorithm", "").ToUpperInvariant()
            Dim block = alg.BlockSize \ 8
            For Each cfg In cfgs
                Dim mode = CType(cfg(0), CipherMode)
                For Each padding In paddings
                    If mode = CipherMode.CTS AndAlso padding <> PaddingMode.None Then Continue For
                    alg.Mode = mode
                    alg.Padding = padding
                    alg.FeedbackSize = If(CInt(cfg(1)) = 0, alg.BlockSize, CInt(cfg(1)))
                    Dim ok = True
                    For trial As Integer = 0 To 24
                        alg.GenerateKey() : alg.GenerateIV()
                        Dim length = random.Next(0, 70)
                        If mode = CipherMode.CTS Then length = Math.Max(length, block)
                        If (mode = CipherMode.ECB OrElse mode = CipherMode.CBC) AndAlso padding = PaddingMode.None Then length -= length Mod block
                        Dim data = RandomBytes(length)
                        Dim once = alg.CreateEncryptor().TransformFinalBlock(data, 0, data.Length)
                        Dim streamed = Transform(alg.CreateEncryptor(), data)
                        If padding <> PaddingMode.ISO10126 AndAlso Not Same(once, streamed) Then ok = False
                        Dim back = Transform(alg.CreateDecryptor(), streamed)
                        If padding = PaddingMode.Zeros Then
                            ' Zeros cannot be told from data: the decryption keeps them.
                            If back.Length < data.Length Then
                                ok = False
                            Else
                                ReDim Preserve back(data.Length - 1)
                            End If
                        End If
                        If Not Same(back, data) Then ok = False
                    Next
                    Dim label = If(mode = CipherMode.CFB, "CFB" & alg.FeedbackSize, mode.ToString())
                    CheckTrue(name & "-" & label & " " & padding.ToString(), ok)
                Next
            Next

            ' CTR
            Dim okCtr = True
            For trial As Integer = 0 To 24
                alg.GenerateKey() : alg.GenerateIV()
                Dim data = RandomBytes(random.Next(0, 70))
                Dim once = alg.CreateCtrTransform().TransformFinalBlock(data, 0, data.Length)
                If Not Same(Transform(alg.CreateCtrTransform(), data), once) Then okCtr = False
                If Not Same(Transform(alg.CreateCtrTransform(), once), data) Then okCtr = False
            Next
            CheckTrue(name & "-CTR", okCtr)
        Next
    End Sub

    ' Writes the data through a CryptoStream in random pieces.
    Function Transform(t As ICryptoTransform, data As Byte()) As Byte()
        Using ms As New MemoryStream()
            Using cs As New CryptoStream(ms, t, CryptoStreamMode.Write)
                Dim pos = 0
                While pos < data.Length
                    Dim n = Math.Min(random.Next(1, 23), data.Length - pos)
                    cs.Write(data, pos, n)
                    pos += n
                End While
                cs.FlushFinalBlock()
                Return ms.ToArray()
            End Using
        End Using
    End Function

    ' ---------------------------------------------------------------- errors and API

    Sub ErrorsAndApi()
        Section("Errors and the SymmetricAlgorithm API")
        Dim aria As New AriaAlgorithm() With {.Mode = CipherMode.CBC, .Padding = PaddingMode.PKCS7}
        aria.GenerateKey()
        aria.GenerateIV()
        CheckTrue("GenerateKey sets a random key", Not Same(aria.Key, New Byte(15) {}))
        CheckTrue("GenerateIV sets a random IV", Not Same(aria.IV, New Byte(15) {}))
        Dim ct = aria.CreateEncryptor().TransformFinalBlock(RandomBytes(20), 0, 20)
        ct(ct.Length - 1) = ct(ct.Length - 1) Xor CByte(&H55)
        Throws("CBC rejects a broken padding", Sub() aria.CreateDecryptor().TransformFinalBlock(ct, 0, ct.Length))
        aria.Padding = PaddingMode.None
        Throws("CBC without padding rejects a partial block", Sub() aria.CreateEncryptor().TransformFinalBlock(New Byte(4) {}, 0, 5))
        aria.Mode = CipherMode.CTS
        Throws("CTS rejects less than one block", Sub() aria.CreateEncryptor().TransformFinalBlock(New Byte(4) {}, 0, 5))
        aria.Mode = CipherMode.OFB
        aria.FeedbackSize = 8
        Throws("OFB rejects a feedback size other than the block size", Sub() aria.CreateEncryptor())
        aria.FeedbackSize = 128
        Throws("A key of the wrong size is refused", Sub() aria.CreateEncryptor(New Byte(9) {}, New Byte(15) {}))

        ' The ECB transform classes of earlier versions still work.
        Dim key = RandomBytes(16), data = RandomBytes(33)
        Dim e = New AriaEcbTransform(key, True).TransformFinalBlock(data, 0, data.Length)
        Check("AriaEcbTransform round trip", New AriaEcbTransform(key, False).TransformFinalBlock(e, 0, e.Length), data)
        e = New SeedEcbTransform(key, True).TransformFinalBlock(data, 0, data.Length)
        Check("SeedEcbTransform round trip", New SeedEcbTransform(key, False).TransformFinalBlock(e, 0, e.Length), data)
        e = New HightEcbTransform(key, True).TransformFinalBlock(data, 0, data.Length)
        Check("HightEcbTransform round trip", New HightEcbTransform(key, False).TransformFinalBlock(e, 0, e.Length), data)

        ' A transform can be used again after TransformFinalBlock.
        Dim seed As New SeedAlgorithm() With {.Mode = CipherMode.CBC}
        seed.GenerateKey() : seed.GenerateIV()
        Dim t = seed.CreateEncryptor()
        Dim first = t.TransformFinalBlock(data, 0, data.Length)
        Check("A transform is reusable after the final block", t.TransformFinalBlock(data, 0, data.Length), first)
    End Sub

    ' ---------------------------------------------------------------- helpers

    ' AES as an IBlockCipher, from the AES of .NET in ECB mode: used to check the modes.
    Private Class AesBlockCipher
        Implements IBlockCipher
        Private ReadOnly enc As ICryptoTransform
        Private ReadOnly dec As ICryptoTransform
        Public Sub New(key As Byte())
            Dim aes = System.Security.Cryptography.Aes.Create()
            aes.Mode = CipherMode.ECB
            aes.Padding = PaddingMode.None
            aes.Key = key
            enc = aes.CreateEncryptor()
            dec = aes.CreateDecryptor()
        End Sub
        Public ReadOnly Property BlockSize As Integer Implements IBlockCipher.BlockSize
            Get
                Return 16
            End Get
        End Property
        Public Sub EncryptBlock(input() As Byte, inOffset As Integer, output() As Byte, outOffset As Integer) Implements IBlockCipher.EncryptBlock
            Dim tmp(15) As Byte
            enc.TransformBlock(input, inOffset, 16, tmp, 0)
            Array.Copy(tmp, 0, output, outOffset, 16)
        End Sub
        Public Sub DecryptBlock(input() As Byte, inOffset As Integer, output() As Byte, outOffset As Integer) Implements IBlockCipher.DecryptBlock
            Dim tmp(15) As Byte
            dec.TransformBlock(input, inOffset, 16, tmp, 0)
            Array.Copy(tmp, 0, output, outOffset, 16)
        End Sub
    End Class

    Sub Section(title As String)
        Console.WriteLine()
        Console.WriteLine("== " & title)
    End Sub

    Sub Check(name As String, actual As Byte(), expected As Byte())
        CheckTrue(name, Same(actual, expected), If(Same(actual, expected), "", "got " & ToHex(actual) & ", expected " & ToHex(expected)))
    End Sub

    Sub CheckTrue(name As String, ok As Boolean, Optional detail As String = "")
        checks += 1
        If Not ok Then failures += 1
        Console.WriteLine("{0} {1}{2}", If(ok, "OK  ", "FAIL"), name, If(detail = "", "", ": " & detail))
    End Sub

    Sub Throws(name As String, action As Action)
        Dim thrown = False
        Try
            action()
        Catch ex As Exception When TypeOf ex Is CryptographicException OrElse TypeOf ex Is ArgumentException OrElse TypeOf ex Is NotSupportedException
            thrown = True
        End Try
        CheckTrue(name, thrown)
    End Sub

    Function Same(a As Byte(), b As Byte()) As Boolean
        If a Is Nothing OrElse b Is Nothing OrElse a.Length <> b.Length Then Return False
        For i = 0 To a.Length - 1
            If a(i) <> b(i) Then Return False
        Next
        Return True
    End Function

    Function RandomBytes(n As Integer) As Byte()
        Dim b(n - 1) As Byte
        random.NextBytes(b)
        Return b
    End Function

    Function Hex(s As String) As Byte()
        Dim b(s.Length \ 2 - 1) As Byte
        For i = 0 To b.Length - 1
            b(i) = Convert.ToByte(s.Substring(2 * i, 2), 16)
        Next
        Return b
    End Function

    Function ToHex(b As Byte()) As String
        If b Is Nothing Then Return "(null)"
        Return BitConverter.ToString(b).Replace("-", "").ToLowerInvariant()
    End Function
End Module
