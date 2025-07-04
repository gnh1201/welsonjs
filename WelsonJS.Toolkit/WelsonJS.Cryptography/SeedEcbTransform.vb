' SeedEcbTransform.cs (WelsonJS.Cryptography)
' SPDX-License-Identifier: MIT
' SPDX-FileCopyrightText: 2025 Namhyeon Go <gnh1201@catswords.re.kr>, Catswords OSS And WelsonJS Contributors
' https://github.com/gnh1201/welsonjs
' 
Imports System.Security.Cryptography

Public Class SeedEcbTransform
    Implements ICryptoTransform

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
        If inputCount <= 0 Then
            Return 0
        End If

        Dim blockSize = InputBlockSize

        For i As Integer = 0 To inputCount - 1 Step blockSize
            If encrypt Then
                seedCore.EncryptBlock(input, inputOffset + i, output, outputOffset + i)
            Else
                seedCore.DecryptBlock(input, inputOffset + i, output, outputOffset + i)
            End If
        Next

        Return inputCount
    End Function

    Public Function TransformFinalBlock(input() As Byte, inputOffset As Integer, inputCount As Integer) As Byte() Implements ICryptoTransform.TransformFinalBlock
        If inputCount = 0 Then
            Return Array.Empty(Of Byte)()
        End If

        Dim blockSize As Integer = InputBlockSize
        Dim paddedLength As Integer
        Dim buffer() As Byte

        If encrypt Then
            Select Case paddingMode
                Case PaddingMode.None
                    If (inputCount Mod blockSize) <> 0 Then
                        Throw New CryptographicException("Input data is not a multiple of block size and PaddingMode is None.")
                    End If
                    paddedLength = inputCount

                Case PaddingMode.Zeros
                    paddedLength = ((inputCount + blockSize - 1) \ blockSize) * blockSize

                Case PaddingMode.PKCS7
                    paddedLength = ((inputCount + blockSize - 1) \ blockSize) * blockSize

                Case Else
                    Throw New NotSupportedException("Unsupported padding mode: " & paddingMode.ToString())
            End Select

            buffer = New Byte(paddedLength - 1) {}
            Array.Copy(input, inputOffset, buffer, 0, inputCount)

            If paddingMode = PaddingMode.Zeros Then
                ' All filling to zero
            ElseIf paddingMode = PaddingMode.PKCS7 Then
                Dim padValue As Byte = CByte(paddedLength - inputCount)
                For i As Integer = inputCount To paddedLength - 1
                    buffer(i) = padValue
                Next
            End If

            TransformBlock(buffer, 0, paddedLength, buffer, 0)
            Return buffer

        Else
            ' Decrpytion
            If (inputCount Mod blockSize) <> 0 Then
                Throw New CryptographicException("Encrypted data is not a multiple of block size.")
            End If

            buffer = New Byte(inputCount - 1) {}
            TransformBlock(input, inputOffset, inputCount, buffer, 0)

            Select Case paddingMode
                Case PaddingMode.None
                    Return buffer

                Case PaddingMode.Zeros
                    Dim trimLength As Integer = buffer.Length
                    While trimLength > 0 AndAlso buffer(trimLength - 1) = 0
                        trimLength -= 1
                    End While
                    Dim result(trimLength - 1) As Byte
                    Array.Copy(buffer, 0, result, 0, trimLength)
                    Return result

                Case PaddingMode.PKCS7
                    Dim padValue As Integer = buffer(buffer.Length - 1)
                    If padValue <= 0 OrElse padValue > blockSize Then
                        Throw New CryptographicException("Invalid PKCS7 padding.")
                    End If
                    For i As Integer = buffer.Length - padValue To buffer.Length - 1
                        If buffer(i) <> padValue Then
                            Throw New CryptographicException("Invalid PKCS7 padding.")
                        End If
                    Next
                    Dim result(buffer.Length - padValue - 1) As Byte
                    Array.Copy(buffer, 0, result, 0, result.Length)
                    Return result

                Case Else
                    Throw New NotSupportedException("Unsupported padding mode: " & paddingMode.ToString())
            End Select
        End If
    End Function

    Public Sub Dispose() Implements IDisposable.Dispose
        ' Nothing
    End Sub
End Class