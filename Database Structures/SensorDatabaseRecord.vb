''' <summary>
''' Author: Jay Lagorio
''' Date: May 22, 2016
''' Summary: Pareses a raw database page into a SensorData record.
''' </summary>

Public Class SensorDatabaseRecord
    Inherits DatabaseRecord

    ' The unfiltered sensor value
    Private pUnfiltered As UInteger = 0

    ' The filtered sensor value
    Private pFiltered As UInteger = 0

    ' The signal strength at the time of the entry
    Private pRSSI As UShort = 0

    ' Data offsets and lengths
    Public Const SensorDataRecordLength As Integer = 20
    Private Const UnfilteredOffset As Integer = 8
    Private Const FilteredOffset As Integer = 12
    Private Const RSSIOffset As Integer = 16

    ''' <summary>
    ''' Reads a DatabasePage starting at byte 0 to create a SensorDatabaseRecord
    ''' </summary>
    ''' <param name="DatabasePage"></param>
    Public Sub New(ByRef DatabasePage As DatabasePage)
        Me.New(DatabasePage, 0)
        pRecordLength = SensorDataRecordLength
    End Sub

    ''' <summary>
    ''' Reads a DatabasePage starting from the passed offset into a SensorDatabaseRecord.
    ''' </summary>
    ''' <param name="DatabasePage">A DatabasePage from the receiver</param>
    ''' <param name="Offset">The offset at from which to create the record</param>
    Public Sub New(ByRef DatabasePage As DatabasePage, ByVal Offset As Integer)
        MyBase.New(DatabasePage, Offset, SensorDataRecordLength)
        pRecordType = DatabasePage.RecordType.SensorData
        pUnfiltered = BitConverter.ToUInt32(pRecordBytes, UnfilteredOffset)
        pFiltered = BitConverter.ToUInt32(pRecordBytes, FilteredOffset)
        pRSSI = BitConverter.ToUInt16(pRecordBytes, RSSIOffset)
    End Sub

    ''' <summary>
    ''' Returns unfiltered data from the sensor
    ''' </summary>
    ''' <returns>An unsigned integer representing raw, unfiltered data from the sensor</returns>
    Public ReadOnly Property Unfiltered As UInteger
        Get
            Return pUnfiltered
        End Get
    End Property

    ''' <summary>
    ''' Returns filtered data from the sensor
    ''' </summary>
    ''' <returns>An unsigned integer representing filtered data from the sensor</returns>
    Public ReadOnly Property Filtered As UInteger
        Get
            Return pFiltered
        End Get
    End Property

    ''' <summary>
    ''' Returns the RSSI from the sensor record
    ''' </summary>
    ''' <returns>An unsigned short representing the RSSI from the reading</returns>
    Public ReadOnly Property RSSI As UShort
        Get
            Return pRSSI
        End Get
    End Property
End Class
