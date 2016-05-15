''' <summary>
''' Author: Jay Lagorio
''' Date: May 15, 2016
''' Summary: Calculates and verifies the CRC of a packet to or from a Dexcom Receiver. This
''' class isn't formally documented here but was retrieved from StackOverflow:
''' http://stackoverflow.com/questions/34652968/crc16-ccitt-from-a-byte-array
''' </summary>

Friend Class CRC
    Public Enum InitialCRCValue
        Zeroes = 0
        NonZero1 = &HFFFF
        NonZero2 = &H1D0F
    End Enum

    Private Const poly As UShort = &H1021
    Private table(255) As UShort
    Private intValue As UShort = 0

    Public Sub New(ByVal initialvalue As InitialCRCValue)
        Me.intValue = CUShort(initialvalue)
        Dim temp, a As UShort
        For i As Integer = 0 To table.Length - 1
            temp = 0
            a = CUShort(i << 8)
            For j As Integer = 0 To 7
                If ((temp Xor a) And &H8000) <> 0 Then
                    temp = CUShort((temp << 1) Xor poly)
                Else
                    temp <<= 1
                End If
                a <<= 1
            Next
            table(i) = temp
        Next
    End Sub

    Public Function ComputeCheckSum(ByVal bytes As Byte()) As UShort
        Return ComputeCheckSum(bytes, bytes.Length)
    End Function

    Public Function ComputeCheckSum(ByVal bytes As Byte(), ByVal byteslen As Integer) As UShort
        Dim crc As UShort = Me.intValue
        For i As Integer = 0 To byteslen - 1
            crc = CUShort(((crc << 8) Xor table(((crc >> 8) Xor (&HFF And bytes(i))))))
        Next
        Return crc
    End Function
End Class
