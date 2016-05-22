''' <summary>
''' Author: Jay Lagorio
''' Date: May 22, 2016
''' Summary: Parses a raw database page into an InsertionTime database record.
''' </summary>

Public Class InsertionDatabaseRecord
    Inherits DatabaseRecord

    ' The insertion time in the record as recorded by the device. This is
    ' not necessarily the same as the system time or display time.
    Private pInsertionTimeSeconds As UInteger

    ' The state of the insertion record as specific in the InsertionStates Enum.
    Private pInsertionState As Byte

    ''' <summary>
    ''' The state of the insertion at the time of the record.
    ''' </summary>
    Public Enum InsertionStates
        Unknown = 0             ' The insertion state was unknown
        Removed = 1             ' The sensor was removed
        Expired = 2             ' The sensor expired
        ResidualDeviation = 3
        CountsDeviation = 4
        SecondSession = 5       ' The sensor was renewed for a second session
        OffTimeLoss = 6
        Started = 7             ' The session was started
        BadTransmitter = 8      ' The transmitter was bad
        ManufacturingMode = 9   ' The transmitter was started in manufacturing mode
    End Enum

    ' Data offsets and lengths
    Public Const InsertionDatabaseRecordLength As Integer = 15
    Private Const InsertionTimeSecondsOffset As Integer = 8
    Private Const InsertionStateOffset As Integer = 12

    ''' <summary>
    ''' Creates a record from the payload of a DatabasePage.
    ''' </summary>
    ''' <param name="DatabasePage">The DatabasePage object read from the device</param>
    Sub New(ByRef DatabasePage As DatabasePage)
        Me.New(DatabasePage, 0)
        pRecordLength = InsertionDatabaseRecordLength
    End Sub

    ''' <summary>
    ''' Creates a record from the payload of a DatabasePage from the specified offset of the 
    ''' payload. Throws a FormatException when the byte offset and record length are invalid.
    ''' </summary>
    ''' <param name="DatabasePage">The DatabasePage object read from the device</param>
    ''' <param name="Offset">The offset to start reading from</param>
    Public Sub New(ByRef DatabasePage As DatabasePage, ByVal Offset As Integer)
        MyBase.New(DatabasePage, Offset, InsertionDatabaseRecordLength)
        pRecordType = DatabasePage.RecordType.InsertionData
        pInsertionTimeSeconds = BitConverter.ToUInt32(pRecordBytes, InsertionTimeSecondsOffset)
        pInsertionState = pRecordBytes(InsertionStateOffset)
    End Sub

    ''' <summary>
    ''' The time at which the sensor was inserted.
    ''' </summary>
    ''' <returns>A DateTime representing the sensor insertion time.</returns>
    Public ReadOnly Property InsertionTime As DateTime
        Get
            Return Utilities.GetReceiverTime(pInsertionTimeSeconds)
        End Get
    End Property

    ''' <summary>
    ''' The state of the sensor indicated in the record.
    ''' </summary>
    ''' <returns>A value in InsertionStates representing the state of the sensor in the record</returns>
    Public ReadOnly Property InsertionState As InsertionStates
        Get
            Return pInsertionState
        End Get
    End Property
End Class
