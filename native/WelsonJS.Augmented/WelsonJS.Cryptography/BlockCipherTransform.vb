' BlockCipherTransform.vb (WelsonJS.Cryptography)
' SPDX-License-Identifier: MIT
' SPDX-FileCopyrightText: 2025 Namhyeon Go <gnh1201@catswords.re.kr>, Catswords OSS And WelsonJS Contributors
' https://github.com/gnh1201/welsonjs
'
Imports System.Security.Cryptography

''' <summary>
''' The modes of operation of <see cref="BlockCipherTransform"/>. CipherMode has no counter
''' mode, so this enumeration adds CTR to the five modes that CipherMode names.
''' </summary>
Public Enum BlockCipherMode
    ''' <summary>Electronic codebook: each block is encrypted on its own.</summary>
    ECB
    ''' <summary>Cipher block chaining (NIST SP 800-38A).</summary>
    CBC
    ''' <summary>Cipher feedback with a segment of 8 to block-size bits (NIST SP 800-38A).</summary>
    CFB
    ''' <summary>Output feedback with full-block feedback (NIST SP 800-38A).</summary>
    OFB
    ''' <summary>CBC with ciphertext stealing, variant CS3 (NIST SP 800-38A Addendum, RFC 3962): no padding, any length of at least one block.</summary>
    CTS
    ''' <summary>Counter mode (NIST SP 800-38A): the IV is the first counter block, incremented as one big-endian number; no padding.</summary>
    CTR
End Enum

''' <summary>
''' An ICryptoTransform that runs a block cipher in one of the modes of
''' <see cref="BlockCipherMode"/>. It works with CryptoStream: blocks are processed as they
''' arrive, and only what the last call needs (the block that carries the padding, or the
''' last two blocks for CTS) is kept back until TransformFinalBlock.
''' </summary>
Public Class BlockCipherTransform
    Implements ICryptoTransform

    Private ReadOnly cipher As IBlockCipher
    Private ReadOnly operationMode As BlockCipherMode
    Private ReadOnly encrypting As Boolean
    Private ReadOnly padding As PaddingMode
    Private ReadOnly blockSize As Integer
    Private ReadOnly segmentSize As Integer     ' bytes per step: the block size, or the CFB segment
    Private ReadOnly initialIV() As Byte

    Private register() As Byte                  ' CBC/CTS chaining value, CFB shift register, OFB/CTR input block
    Private keystream() As Byte                 ' CTR: the current keystream block
    Private keystreamUsed As Integer
    Private pending() As Byte                   ' input kept back for TransformFinalBlock
    Private pendingCount As Integer
    Private disposed As Boolean

    ''' <param name="cipher">The block cipher, with its key set.</param>
    ''' <param name="operationMode">The mode of operation.</param>
    ''' <param name="iv">The initialization vector (the first counter block for CTR), one block long. Not used for ECB.</param>
    ''' <param name="encrypt">True to encrypt, False to decrypt.</param>
    ''' <param name="padding">The padding for ECB, CBC, CFB and OFB. CTS and CTR do not pad.</param>
    ''' <param name="feedbackSize">The CFB segment size in bits (8 to the block size, a multiple of 8); 0 for the block size. OFB supports only the block size.</param>
    Public Sub New(cipher As IBlockCipher, operationMode As BlockCipherMode, iv() As Byte, encrypt As Boolean,
                   Optional padding As PaddingMode = PaddingMode.PKCS7, Optional feedbackSize As Integer = 0)
        If cipher Is Nothing Then Throw New ArgumentNullException(NameOf(cipher))
        Me.cipher = cipher
        Me.operationMode = operationMode
        Me.encrypting = encrypt
        Me.padding = padding
        Me.blockSize = cipher.BlockSize

        If blockSize <= 0 Then Throw New ArgumentException("The block size of the cipher must be positive.", NameOf(cipher))

        Select Case padding
            Case PaddingMode.None, PaddingMode.PKCS7, PaddingMode.Zeros, PaddingMode.ANSIX923, PaddingMode.ISO10126
            Case Else
                Throw New NotSupportedException("Unsupported padding mode: " & padding.ToString())
        End Select

        Dim feedbackBytes As Integer = If(feedbackSize = 0, blockSize, feedbackSize \ 8)
        If feedbackSize <> 0 AndAlso (feedbackSize Mod 8 <> 0 OrElse feedbackBytes < 1 OrElse feedbackBytes > blockSize) Then
            Throw New CryptographicException("The feedback size must be a multiple of 8 bits between 8 and the block size.")
        End If

        Select Case operationMode
            Case BlockCipherMode.ECB, BlockCipherMode.CBC, BlockCipherMode.CTS, BlockCipherMode.CTR
                segmentSize = blockSize
            Case BlockCipherMode.CFB
                segmentSize = feedbackBytes
            Case BlockCipherMode.OFB
                If feedbackBytes <> blockSize Then
                    Throw New NotSupportedException("OFB supports only a feedback size equal to the block size.")
                End If
                segmentSize = blockSize
            Case Else
                Throw New NotSupportedException("Unsupported mode: " & operationMode.ToString())
        End Select

        If operationMode = BlockCipherMode.ECB Then
            initialIV = New Byte(blockSize - 1) {}
        Else
            If iv Is Nothing Then Throw New ArgumentNullException(NameOf(iv), "The " & operationMode.ToString() & " mode needs an IV.")
            If iv.Length <> blockSize Then
                Throw New CryptographicException("The IV must be " & blockSize & " bytes, one block, long.")
            End If
            initialIV = CType(iv.Clone(), Byte())
        End If

        pending = New Byte(2 * blockSize - 1) {}
        Reset()
    End Sub

    Public ReadOnly Property InputBlockSize As Integer Implements ICryptoTransform.InputBlockSize
        Get
            Return segmentSize
        End Get
    End Property

    Public ReadOnly Property OutputBlockSize As Integer Implements ICryptoTransform.OutputBlockSize
        Get
            Return segmentSize
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

    ' Returns to the state before the first block, as after TransformFinalBlock.
    Private Sub Reset()
        register = CType(initialIV.Clone(), Byte())
        keystream = New Byte(blockSize - 1) {}
        keystreamUsed = blockSize
        Array.Clear(pending, 0, pending.Length)
        pendingCount = 0
    End Sub

    ' Whether decryption removes padding, so the last segment must wait for TransformFinalBlock.
    Private ReadOnly Property RemovesPadding As Boolean
        Get
            Return Not encrypting AndAlso
                (padding = PaddingMode.PKCS7 OrElse padding = PaddingMode.ANSIX923 OrElse padding = PaddingMode.ISO10126) AndAlso
                operationMode <> BlockCipherMode.CTS AndAlso operationMode <> BlockCipherMode.CTR
        End Get
    End Property

    Public Function TransformBlock(inputBuffer() As Byte, inputOffset As Integer, inputCount As Integer,
                                   outputBuffer() As Byte, outputOffset As Integer) As Integer Implements ICryptoTransform.TransformBlock
        CheckState()
        CheckBuffer(inputBuffer, inputOffset, inputCount, NameOf(inputBuffer))
        If outputBuffer Is Nothing Then Throw New ArgumentNullException(NameOf(outputBuffer))

        If operationMode = BlockCipherMode.CTR Then
            CheckBuffer(outputBuffer, outputOffset, inputCount, NameOf(outputBuffer))
            XorKeystream(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset)
            Return inputCount
        End If

        ' The input joined to what was kept back by the previous call.
        Dim total As Integer = pendingCount + inputCount
        Dim data(total - 1) As Byte
        Array.Copy(pending, 0, data, 0, pendingCount)
        Array.Copy(inputBuffer, inputOffset, data, pendingCount, inputCount)

        ' How much to keep back: CTS keeps the last two blocks (more than one block and at most
        ' two), a decryption that removes padding keeps the last segment.
        Dim process As Integer
        If operationMode = BlockCipherMode.CTS Then
            If total > 2 * blockSize Then
                Dim blocks As Integer = (total - 2 * blockSize + blockSize - 1) \ blockSize
                process = blocks * blockSize
            Else
                process = 0
            End If
        ElseIf RemovesPadding Then
            process = Math.Max(0, ((total - 1) \ segmentSize) * segmentSize)
        Else
            process = (total \ segmentSize) * segmentSize
        End If

        If process > 0 Then
            CheckBuffer(outputBuffer, outputOffset, process, NameOf(outputBuffer))
            ProcessSegments(data, 0, process, outputBuffer, outputOffset)
        End If

        pendingCount = total - process
        If pendingCount > pending.Length Then pending = New Byte(pendingCount - 1) {}
        Array.Copy(data, process, pending, 0, pendingCount)
        Array.Clear(data, 0, data.Length)
        Return process
    End Function

    Public Function TransformFinalBlock(inputBuffer() As Byte, inputOffset As Integer, inputCount As Integer) As Byte() Implements ICryptoTransform.TransformFinalBlock
        CheckState()
        CheckBuffer(inputBuffer, inputOffset, inputCount, NameOf(inputBuffer))

        Dim data(pendingCount + inputCount - 1) As Byte
        Array.Copy(pending, 0, data, 0, pendingCount)
        Array.Copy(inputBuffer, inputOffset, data, pendingCount, inputCount)

        Try
            Select Case operationMode
                Case BlockCipherMode.CTR
                    Dim output(data.Length - 1) As Byte
                    XorKeystream(data, 0, data.Length, output, 0)
                    Return output

                Case BlockCipherMode.CTS
                    Return FinalCts(data)

                Case BlockCipherMode.ECB, BlockCipherMode.CBC
                    If encrypting Then
                        Dim padded() As Byte = AddPadding(data, blockSize)
                        If padded.Length Mod blockSize <> 0 Then
                            Throw New CryptographicException("The input is not a multiple of the block size and the padding mode is None.")
                        End If
                        ProcessSegments(padded, 0, padded.Length, padded, 0)
                        Return padded
                    Else
                        If data.Length Mod blockSize <> 0 Then
                            Throw New CryptographicException("The encrypted data is not a multiple of the block size.")
                        End If
                        ProcessSegments(data, 0, data.Length, data, 0)
                        Return RemovePadding(data, blockSize)
                    End If

                Case Else ' CFB, OFB
                    If encrypting Then
                        Dim padded() As Byte = AddPadding(data, segmentSize)
                        Return StreamFinal(padded)
                    ElseIf RemovesPadding Then
                        If data.Length Mod segmentSize <> 0 Then
                            Throw New CryptographicException("The encrypted data is not a multiple of the segment size.")
                        End If
                        ProcessSegments(data, 0, data.Length, data, 0)
                        Return RemovePadding(data, segmentSize)
                    Else
                        Return StreamFinal(data)
                    End If
            End Select
        Finally
            Reset()
        End Try
    End Function

    ' CFB or OFB over whole segments and, without padding, a shorter last segment.
    Private Function StreamFinal(data() As Byte) As Byte()
        Dim whole As Integer = (data.Length \ segmentSize) * segmentSize
        Dim output(data.Length - 1) As Byte
        If whole > 0 Then ProcessSegments(data, 0, whole, output, 0)
        Dim rest As Integer = data.Length - whole
        If rest > 0 Then
            ' A partial segment uses the leftmost bytes of the next cipher output; the register
            ' is not used again.
            Dim o(blockSize - 1) As Byte
            cipher.EncryptBlock(register, 0, o, 0)
            For i As Integer = 0 To rest - 1
                output(whole + i) = data(whole + i) Xor o(i)
            Next
        End If
        Return output
    End Function

    ' Processes count bytes (a multiple of the segment size) and advances the chaining state.
    ' Input and output may be the same buffer.
    Private Sub ProcessSegments(input() As Byte, inOffset As Integer, count As Integer, output() As Byte, outOffset As Integer)
        Dim b As Integer = blockSize
        Dim s As Integer = segmentSize
        Dim tmp(b - 1) As Byte

        Select Case operationMode
            Case BlockCipherMode.ECB
                For p As Integer = 0 To count - 1 Step b
                    If encrypting Then
                        cipher.EncryptBlock(input, inOffset + p, output, outOffset + p)
                    Else
                        cipher.DecryptBlock(input, inOffset + p, output, outOffset + p)
                    End If
                Next

            Case BlockCipherMode.CBC, BlockCipherMode.CTS
                For p As Integer = 0 To count - 1 Step b
                    If encrypting Then
                        For i As Integer = 0 To b - 1
                            tmp(i) = input(inOffset + p + i) Xor register(i)
                        Next
                        cipher.EncryptBlock(tmp, 0, register, 0)
                        Array.Copy(register, 0, output, outOffset + p, b)
                    Else
                        Array.Copy(input, inOffset + p, tmp, 0, b)  ' the next chaining value
                        cipher.DecryptBlock(input, inOffset + p, output, outOffset + p)
                        For i As Integer = 0 To b - 1
                            output(outOffset + p + i) = output(outOffset + p + i) Xor register(i)
                        Next
                        Array.Copy(tmp, 0, register, 0, b)
                    End If
                Next

            Case BlockCipherMode.CFB
                Dim o(b - 1) As Byte
                Dim c(s - 1) As Byte
                For p As Integer = 0 To count - 1 Step s
                    cipher.EncryptBlock(register, 0, o, 0)
                    For i As Integer = 0 To s - 1
                        Dim x As Byte = input(inOffset + p + i)
                        Dim y As Byte = x Xor o(i)
                        c(i) = If(encrypting, y, x)   ' the ciphertext segment feeds the register
                        output(outOffset + p + i) = y
                    Next
                    ' Shift the register left by one segment and append the ciphertext segment.
                    Array.Copy(register, s, register, 0, b - s)
                    Array.Copy(c, 0, register, b - s, s)
                Next

            Case BlockCipherMode.OFB
                For p As Integer = 0 To count - 1 Step b
                    cipher.EncryptBlock(register, 0, register, 0)
                    For i As Integer = 0 To b - 1
                        output(outOffset + p + i) = input(inOffset + p + i) Xor register(i)
                    Next
                Next

            Case BlockCipherMode.CTR
                XorKeystream(input, inOffset, count, output, outOffset)
        End Select

        Array.Clear(tmp, 0, tmp.Length)
    End Sub

    ' CTR: XORs the input with the keystream E(counter), E(counter + 1), ...
    Private Sub XorKeystream(input() As Byte, inOffset As Integer, count As Integer, output() As Byte, outOffset As Integer)
        For i As Integer = 0 To count - 1
            If keystreamUsed = blockSize Then
                cipher.EncryptBlock(register, 0, keystream, 0)
                IncrementCounter(register)
                keystreamUsed = 0
            End If
            output(outOffset + i) = input(inOffset + i) Xor keystream(keystreamUsed)
            keystreamUsed += 1
        Next
    End Sub

    ' Adds one to the counter block, read as one big-endian number.
    Private Shared Sub IncrementCounter(counter() As Byte)
        For i As Integer = counter.Length - 1 To 0 Step -1
            counter(i) = CByte((CInt(counter(i)) + 1) And &HFF)
            If counter(i) <> 0 Then Exit For
        Next
    End Sub

    ' CBC with ciphertext stealing, CS3: the last two blocks are always swapped, and the last
    ' one is shortened to the length of the last plaintext block. Exactly one block is plain CBC.
    Private Function FinalCts(data() As Byte) As Byte()
        Dim b As Integer = blockSize
        Dim n As Integer = data.Length
        If n < b Then
            Throw New CryptographicException("CTS needs at least one block of input.")
        End If

        Dim output(n - 1) As Byte
        If n = b Then
            ProcessSegments(data, 0, b, output, 0)
            Return output
        End If

        Dim m As Integer = (n + b - 1) \ b         ' number of blocks, the last one may be partial
        Dim d As Integer = n - (m - 1) * b         ' length of the last block, 1 to b
        Dim head As Integer = (m - 2) * b          ' the blocks before the last two
        If head > 0 Then ProcessSegments(data, 0, head, output, 0)

        If encrypting Then
            ' E1 = E(P[m-1] xor X), Cfull = E((P[m] || zeros) xor E1); output Cfull, then E1 cut to d bytes.
            Dim e1(b - 1) As Byte
            For i As Integer = 0 To b - 1
                e1(i) = data(head + i) Xor register(i)
            Next
            cipher.EncryptBlock(e1, 0, e1, 0)
            Dim x(b - 1) As Byte
            For i As Integer = 0 To b - 1
                Dim p As Byte = If(i < d, data(head + b + i), CByte(0))
                x(i) = p Xor e1(i)
            Next
            cipher.EncryptBlock(x, 0, x, 0)
            Array.Copy(x, 0, output, head, b)
            Array.Copy(e1, 0, output, head + b, d)
        Else
            ' D = D(Cfull) = (P[m] || zeros) xor E1, so E1 = C* || D[d..], P[m] = C* xor D[0..d-1],
            ' and P[m-1] = D(E1) xor X.
            Dim dd(b - 1) As Byte
            cipher.DecryptBlock(data, head, dd, 0)
            Dim e1(b - 1) As Byte
            For i As Integer = 0 To b - 1
                e1(i) = If(i < d, data(head + b + i), dd(i))
            Next
            For i As Integer = 0 To d - 1
                output(head + b + i) = dd(i) Xor e1(i)
            Next
            Dim p1(b - 1) As Byte
            cipher.DecryptBlock(e1, 0, p1, 0)
            For i As Integer = 0 To b - 1
                output(head + i) = p1(i) Xor register(i)
            Next
        End If
        Return output
    End Function

    ' Pads data to a multiple of unit bytes. None returns the data as it is.
    Private Function AddPadding(data() As Byte, unit As Integer) As Byte()
        Dim rest As Integer = data.Length Mod unit
        Select Case padding
            Case PaddingMode.None
                Return data
            Case PaddingMode.Zeros
                If rest = 0 Then Return data
                Dim zeros(data.Length + unit - rest - 1) As Byte
                Array.Copy(data, zeros, data.Length)
                Return zeros
            Case Else
                Dim padLength As Integer = unit - rest
                Dim padded(data.Length + padLength - 1) As Byte
                Array.Copy(data, padded, data.Length)
                Dim filler() As Byte = Nothing
                If padding = PaddingMode.ISO10126 Then filler = RandomBytes(padLength)
                For i As Integer = data.Length To padded.Length - 2
                    Select Case padding
                        Case PaddingMode.PKCS7
                            padded(i) = CByte(padLength)
                        Case PaddingMode.ANSIX923
                            padded(i) = 0
                        Case PaddingMode.ISO10126
                            padded(i) = filler(i - data.Length)
                    End Select
                Next
                padded(padded.Length - 1) = CByte(padLength)
                Return padded
        End Select
    End Function

    ' Removes the padding of decrypted data. None and Zeros return the data as it is.
    Private Function RemovePadding(data() As Byte, unit As Integer) As Byte()
        If padding = PaddingMode.None OrElse padding = PaddingMode.Zeros OrElse data.Length = 0 Then Return data

        Dim padLength As Integer = data(data.Length - 1)
        Dim valid As Boolean = padLength >= 1 AndAlso padLength <= unit AndAlso padLength <= data.Length
        If valid Then
            For i As Integer = data.Length - padLength To data.Length - 2
                Select Case padding
                    Case PaddingMode.PKCS7
                        If data(i) <> padLength Then valid = False
                    Case PaddingMode.ANSIX923
                        If data(i) <> 0 Then valid = False
                End Select
            Next
        End If
        If Not valid Then
            Array.Clear(data, 0, data.Length)
            Throw New CryptographicException("Padding is invalid and cannot be removed.")
        End If

        Dim result(data.Length - padLength - 1) As Byte
        Array.Copy(data, result, result.Length)
        Array.Clear(data, 0, data.Length)
        Return result
    End Function

    Private Sub CheckState()
        If disposed Then Throw New ObjectDisposedException(Me.GetType().Name)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub

    Protected Overridable Sub Dispose(disposing As Boolean)
        If disposed Then Return
        If register IsNot Nothing Then Array.Clear(register, 0, register.Length)
        If keystream IsNot Nothing Then Array.Clear(keystream, 0, keystream.Length)
        If pending IsNot Nothing Then Array.Clear(pending, 0, pending.Length)
        disposed = True
    End Sub
End Class
