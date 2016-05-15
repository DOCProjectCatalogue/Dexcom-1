''' <summary>
''' Author: Jay Lagorio
''' Date: May 15, 2016
''' Summary: Utility functions useful across classes within the project.
''' </summary>

Friend Module Utilities
    ''' <summary>
    ''' Encodes decimal values into hexidecimal strings.
    ''' </summary>
    ''' <param name="Dec">The decimal value to encode.</param>
    ''' <returns>A string representing the hexidecimal value of the passed byte.</returns>
    Public Function DecimalToHex(ByVal Dec As Byte) As String
        Dim HexString As String

        If Dec < 10 Then
            ' Return the decimal value as the hex value
            HexString = Dec
        ElseIf Dec >= 10 And Dec < 16 Then
            ' Return the hex value A - F
            HexString = ChrW(Dec + AscW("A") - 10)
        Else
            ' Return two hex digits representing this one byte number
            HexString = DecimalToHex(Dec \ 16) & DecimalToHex(Dec Mod 16)
        End If

        Return HexString
    End Function

    ''' <summary>
    ''' Calculates the DateTime based on the number of seconds since January 1, 2009, which
    ''' is how the Dexcom Receiver stores time values.
    ''' </summary>
    ''' <param name="Seconds">Seconds since January 1, 2009 since an event on the Dexcom Receiver.</param>
    ''' <returns>Returns a DateTime representing when the passed event occurred.</returns>
    Public Function GetReceiverTime(ByVal Seconds As UInteger) As DateTime
        ' Some database records are stored with maxed out time values. For those return
        ' the time now.
        If Seconds < UInteger.MaxValue Then
            ' Time values are stored in seconds since January 1, 2009
            Return (New DateTime(2009, 1, 1)).AddSeconds(Seconds)
        Else
            Return DateTime.Now
        End If
    End Function
End Module
