''' <summary>
''' Author: Jay Lagorio
''' Date: May 15, 2016
''' Summary: Represents the basic information required to constitute a database record from a Dexcom Receiver.
''' </summary>

Public MustInherit Class DatabaseRecord
    ' The length of each record on a database page. Must be set in Shared Sub New of the
    ' derivative class.
    Friend Shared pRecordLength As Integer

    ' Bytes representing the individual record from the DatabasePage at the
    ' offset specified.
    Friend pRecordBytes() As Byte

    ' The record type of each record in the page. Must be a value from 
    ' DatabasePage.RecordType and set in Sub New of the derivative class.
    Friend pRecordType As Integer

    ' The number of seconds that passed since the Dexcom Receiver epoch at GMT.
    Friend pSystemTimeSeconds As UInteger

    ' The number of seconds that passed since the Dexcom Receiver epoch in the local timezone.
    Friend pDisplayTimeSeconds As UInteger

    ' Data offsets
    Private Const SystemTimeOffset As Integer = 0
    Private Const DisplayTimeOffset As Integer = 4

    ''' <summary>
    ''' Creates a record from the payload of a DatabasePage.
    ''' </summary>
    ''' <param name="DatabasePage">The DatabasePage object read from the device</param>
    Sub New(ByRef DatabasePage As DatabasePage)
        Me.New(DatabasePage, 0)
    End Sub

    ''' <summary>
    ''' Creates a record from the payload of a DatabasePage from the specified offset of the 
    ''' payload. Throws a FormatException when the byte offset and record length are invalid.
    ''' </summary>
    ''' <param name="DatabasePage">The DatabasePage object read from the device</param>
    ''' <param name="Offset">The offset to start reading from</param>
    Sub New(ByRef DatabasePage As DatabasePage, ByVal Offset As Integer)
        ' Check that the payload length is longer than one record and that the length is also
        ' greater than the starting offset + the record length.
        If (DatabasePage.Payload.Length >= RecordLength) And (DatabasePage.Payload.Length > (Offset + RecordLength)) Then
            ' Copy the bytes from the payload for this individual record
            ReDim pRecordBytes(RecordLength - 1)
            Array.ConstrainedCopy(DatabasePage.Payload, Offset, pRecordBytes, 0, pRecordBytes.Length)

            ' Read the system time (GMT) and display time (local timezone) indicating
            ' when the record was created.
            pSystemTimeSeconds = BitConverter.ToUInt32(pRecordBytes, SystemTimeOffset)
            pDisplayTimeSeconds = BitConverter.ToUInt32(pRecordBytes, DisplayTimeOffset)
        Else
            Throw New FormatException("The payload length is smaller than the record length or the offset overruns the payload and record length.")
        End If
    End Sub

    ''' <summary>
    ''' Returns the length of an individual record of the database type derived from this class.
    ''' </summary>
    ''' <returns>The length of an individual record in bytes</returns>
    Public Shared ReadOnly Property RecordLength As Integer
        Get
            Return pRecordLength
        End Get
    End Property

    ''' <summary>
    ''' Returns the System Time at GMT of the individual record.
    ''' </summary>
    ''' <returns>A DateTime at GMT when the record was created.</returns>
    Public ReadOnly Property SystemTime As DateTime
        Get
            Return Utilities.GetReceiverTime(pSystemTimeSeconds)
        End Get
    End Property

    ''' <summary>
    ''' Returns the Display Time at local time of the individual record.
    ''' </summary>
    ''' <returns>A DateTime at local time when the record was created.</returns>
    Public ReadOnly Property DisplayTime As DateTime
        Get
            Return Utilities.GetReceiverTime(pDisplayTimeSeconds)
        End Get
    End Property

    ''' <summary>
    ''' Returns the type of record as a value in DatabasePage.RecordType.
    ''' </summary>
    ''' <returns>The type of record derived from this class</returns>
    Public ReadOnly Property RecordType As DatabasePage.RecordType
        Get
            Return pRecordType
        End Get
    End Property
End Class
