' SeedEcbTransform.cs (WelsonJS.Cryptography)
' SPDX-License-Identifier: MIT
' SPDX-FileCopyrightText: 2025 Namhyeon Go <gnh1201@catswords.re.kr>, Catswords OSS And WelsonJS Contributors
' https://github.com/gnh1201/welsonjs
' 
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
                    ' None 패딩은 추가 블록 없음
                    paddedLength = inputCount

                Case PaddingMode.Zeros
                    ' Zeros 패딩은 추가 블록 필요 없음
                    paddedLength = ((inputCount + blockSize - 1) \ blockSize) * blockSize

                Case PaddingMode.PKCS7, PaddingMode.ANSIX923, PaddingMode.ISO10126
                    ' PKCS7. ANSIX923, ISO10126 패딩은 입력이 블록 배수면 +1 블록 추가
                    ' (설명) 블록암호에서 블록 길이와 같은 길이의 원문을 넣으면, 암호화문 길이가 원문 길이의 2배가 되는 원인은 여기에 기인한다.
                    Dim fullBlocks As Integer = inputCount \ blockSize
                    Dim remainder As Integer = inputCount Mod blockSize
                    If remainder = 0 Then
                        paddedLength = (fullBlocks + 1) * blockSize ' 추가 블록 붙임
                    Else
                        paddedLength = (fullBlocks + 1) * blockSize
                    End If

                Case Else
                    Throw New NotSupportedException("Unsupported padding mode: " & paddingMode.ToString())
            End Select

            buffer = New Byte(paddedLength - 1) {}
            Array.Copy(input, inputOffset, buffer, 0, inputCount)

            If paddingMode = PaddingMode.PKCS7 Then
                Dim padValue As Byte = CByte(paddedLength - inputCount)
                For i As Integer = inputCount To paddedLength - 1
                    buffer(i) = padValue
                Next

            ElseIf paddingMode = PaddingMode.ANSIX923 Then
                Dim padValue As Byte = CByte(paddedLength - inputCount)
                For i As Integer = inputCount To paddedLength - 2
                    buffer(i) = 0
                Next
                buffer(paddedLength - 1) = padValue

            ElseIf paddingMode = PaddingMode.ISO10126 Then
                Dim padValue As Byte = CByte(paddedLength - inputCount)
                For i As Integer = inputCount To paddedLength - 2
                    buffer(i) = CByte(rnd.Next(0, 256))
                Next
                buffer(paddedLength - 1) = padValue
            End If

            TransformBlock(buffer, 0, paddedLength, buffer, 0)
            Return buffer

        Else
            ' Decryption
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
                            Throw New CryptographicException("Invalid PKCS7 padding value.")
                        End If
                    Next
                    Dim unpaddedLength As Integer = buffer.Length - padValue
                    If unpaddedLength < 0 Then
                        Throw New CryptographicException("Invalid unpadded length.")
                    End If
                    Dim result(unpaddedLength - 1) As Byte
                    Array.Copy(buffer, 0, result, 0, unpaddedLength)
                    Return result

                Case PaddingMode.ANSIX923
                    Dim padValue As Integer = buffer(buffer.Length - 1)
                    If padValue <= 0 OrElse padValue > blockSize Then
                        Throw New CryptographicException("Invalid ANSIX923 padding.")
                    End If
                    For i As Integer = buffer.Length - padValue To buffer.Length - 2
                        If buffer(i) <> 0 Then
                            Throw New CryptographicException("Invalid ANSIX923 padding value.")
                        End If
                    Next
                    Dim unpaddedLengthAnsix As Integer = buffer.Length - padValue
                    If unpaddedLengthAnsix < 0 Then
                        Throw New CryptographicException("Invalid unpadded length.")
                    End If
                    Dim resultAnsix(unpaddedLengthAnsix - 1) As Byte
                    Array.Copy(buffer, 0, resultAnsix, 0, unpaddedLengthAnsix)
                    Return resultAnsix

                Case PaddingMode.ISO10126
                    Dim padValue As Integer = buffer(buffer.Length - 1)
                    If padValue <= 0 OrElse padValue > blockSize Then
                        Throw New CryptographicException("Invalid ISO10126 padding.")
                    End If
                    ' Check the last byte (length)
                    Dim unpaddedLengthIso As Integer = buffer.Length - padValue
                    If unpaddedLengthIso < 0 Then
                        Throw New CryptographicException("Invalid unpadded length.")
                    End If
                    Dim resultIso(unpaddedLengthIso - 1) As Byte
                    Array.Copy(buffer, 0, resultIso, 0, unpaddedLengthIso)
                    Return resultIso

                Case Else
                    Throw New NotSupportedException("Unsupported padding mode: " & paddingMode.ToString())
            End Select
        End If
    End Function

    Public Sub Dispose() Implements IDisposable.Dispose
        ' Nothing
    End Sub
End Class