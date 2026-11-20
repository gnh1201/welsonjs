' IBlockCipher.vb (WelsonJS.Cryptography)
' SPDX-License-Identifier: MIT
' SPDX-FileCopyrightText: 2025 Namhyeon Go <gnh1201@catswords.re.kr>, Catswords OSS And WelsonJS Contributors
' https://github.com/gnh1201/welsonjs
'

''' <summary>
''' A block cipher with a key already set: it encrypts or decrypts one block at a time.
''' The modes of operation (<see cref="BlockCipherTransform"/>, <see cref="BlockCipherGcm"/>,
''' <see cref="BlockCipherCcm"/>, <see cref="BlockCipherCmac"/>) work with any implementation.
''' </summary>
Public Interface IBlockCipher
    ''' <summary>The block size in bytes (16 for ARIA and SEED, 8 for HIGHT).</summary>
    ReadOnly Property BlockSize As Integer

    ''' <summary>Encrypts one block from input at inOffset into output at outOffset. The two may overlap.</summary>
    Sub EncryptBlock(input() As Byte, inOffset As Integer, output() As Byte, outOffset As Integer)

    ''' <summary>Decrypts one block from input at inOffset into output at outOffset. The two may overlap.</summary>
    Sub DecryptBlock(input() As Byte, inOffset As Integer, output() As Byte, outOffset As Integer)
End Interface
