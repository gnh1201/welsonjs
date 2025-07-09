' SeedEcbTransform.cs (WelsonJS.Cryptography)
' SPDX-License-Identifier: MIT
' SPDX-FileCopyrightText: 2025 Namhyeon Go <gnh1201@catswords.re.kr>, Catswords OSS And WelsonJS Contributors
' https://github.com/gnh1201/welsonjs

Imports System.Security.Cryptography

Public Class SeedEcbTransform
    Implements ICryptoTransform

    Private ReadOnly rnd As New Random()
    Private ReadOnly seedCore As SeedCore
    Private ReadOnly encrypt As Boolean
    Private ReadOnly paddingMode As PaddingMode

    Public Sub New(key As Byte(), encryptMode As Boolean, Optional mode As PaddingMode = PaddingMode.PKCS7)
        seedCore = New SeedCore(key)
        encrypt = encryptMode
        paddingMode = mode
    End Sub

    Public ReadOnly Property InputBlockSize As Integer Implements ICryptoTransform.InputBlockSize
        Get
            Return 16
        End Get
    End Property

    Public ReadOnly Property OutputBlockSize As Integer Implements ICryptoTransform.OutputBlockSize
        Get
            Return 16
        End Get
    End Property

    Public ReadOnly Property CanTransformMultipleBlocks As Boolean Implements ICryptoTransform.CanTransformMultipleBlocks
        Get
            Return True
        End Get
    End Property

    Public ReadOnly Property CanReuseTransform As Boolean Implements ICryptoTransform.CanReuseTransform
        Get
            Return True
        End Get
    End Property

    Public Function TransformBlock(input() As Byte, inputOffset As Integer, inputCount As Integer,
                                   output() As Byte, outputOffset As Integer) As Integer Implements ICryptoTransform.TransformBlock
        If inputCount <= 0 Then Return 0

        Dim blockSize = InputBlockSize
        Dim remaining = inputCount
        Dim inPtr = inputOffset
        Dim outPtr = outputOffset

        While remaining >= blockSize
            If encrypt Then
                seedCore.EncryptBlock(input, inPtr, output, outPtr)
            Else
                seedCore.DecryptBlock(input, inPtr, output, outPtr)
            End If
            inPtr += blockSize
            outPtr += blockSize
            remaining -= blockSize
        End While

        Return inputCount - remaining
    End Function

    Public Function TransformFinalBlock(input() As Byte, inputOffset As Integer, inputCount As Integer) As Byte() Implements ICryptoTransform.TransformFinalBlock
        Dim blockSize = InputBlockSize
        Dim buffer() As Byte

        If encrypt Then
            Dim paddedLength As Integer
            Select Case paddingMode
                Case PaddingMode.None
                    If (inputCount Mod blockSize) <> 0 Then
                        Throw New CryptographicException("Input data is not a multiple of block size and PaddingMode is None.")
                    End If
                    paddedLength = inputCount

                Case PaddingMode.Zeros
                    paddedLength = ((inputCount + blockSize - 1) \ blockSize) * blockSize

                Case PaddingMode.PKCS7, PaddingMode.ANSIX923, PaddingMode.ISO10126
                    Dim padLen = blockSize - (inputCount Mod blockSize)
                    If padLen = 0 Then padLen = blockSize
                    paddedLength = inputCount + padLen

                Case Else
                    Throw New NotSupportedException("Unsupported padding mode: " & paddingMode.ToString())
            End Select

            buffer = New Byte(paddedLength - 1) {}
            Array.Copy(input, inputOffset, buffer, 0, inputCount)

            Dim padVal As Byte = CByte(paddedLength - inputCount)
            Select Case paddingMode
                Case PaddingMode.PKCS7
                    For i = inputCount To paddedLength - 1
                        buffer(i) = padVal
                    Next
                Case PaddingMode.ANSIX923
                    For i = inputCount To paddedLength - 2
                        buffer(i) = 0
                    Next
                    buffer(paddedLength - 1) = padVal
                Case PaddingMode.ISO10126
                    For i = inputCount To paddedLength - 2
                        buffer(i) = CByte(rnd.Next(0, 256))
                    Next
                    buffer(paddedLength - 1) = padVal
            End Select

            For i = 0 To buffer.Length - 1 Step blockSize
                seedCore.EncryptBlock(buffer, i, buffer, i)
            Next
            Return buffer

        Else
            If (inputCount Mod blockSize) <> 0 Then
                Throw New CryptographicException("Encrypted data is not a multiple of block size.")
            End If

            buffer = New Byte(inputCount - 1) {}
            TransformBlock(input, inputOffset, inputCount, buffer, 0)

            Dim padVal As Integer = buffer(buffer.Length - 1)
            If padVal <= 0 OrElse padVal > blockSize Then
                Throw New CryptographicException("Invalid padding.")
            End If

            Select Case paddingMode
                Case PaddingMode.PKCS7
                    For i = buffer.Length - padVal To buffer.Length - 1
                        If buffer(i) <> padVal Then
                            Throw New CryptographicException("Invalid PKCS7 padding value.")
                        End If
                    Next
                Case PaddingMode.ANSIX923
                    For i = buffer.Length - padVal To buffer.Length - 2
                        If buffer(i) <> 0 Then
                            Throw New CryptographicException("Invalid ANSIX923 padding value.")
                        End If
                    Next
                Case PaddingMode.ISO10126
                    ' no need to check random bytes, only length byte matters
                Case Else
                    Throw New NotSupportedException("Unsupported padding mode: " & paddingMode.ToString())
            End Select

            Dim result(buffer.Length - padVal - 1) As Byte
            Array.Copy(buffer, 0, result, 0, result.Length)
            Return result
        End If
    End Function

    Public Sub Dispose() Implements IDisposable.Dispose
        ' No resources to dispose
    End Sub
End Class
