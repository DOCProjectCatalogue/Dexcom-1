''' <summary>
''' Author: Jay Lagorio
''' Date: May 22, 2016
''' Summary: Parses a raw database page into a MeterDatabaseRecord.
''' </summary>

Public Class MeterDatabaseRecord
    Inherits DatabaseRecord

    ' The glucose value entered into the reader
    Private pMeterGlucose As UShort = 0

    ' The time at which the meter entry was made
    Private pMeterTimeSeconds As UInteger = 0

    ' Data offsets and lengths
    Public Const MeterDataRecordLength As Integer = 16
    Private Const MeterGlucoseOffset As Integer = 8
    Private Const MeterTimeOffset As Integer = 10

    ''' <summary>
    ''' Reads a DatabasePage starting at byte 0 to create a MeterDatabaseRecord
    ''' </summary>
    ''' <param name="DatabasePage"></param>
    Public Sub New(ByRef DatabasePage As DatabasePage)
        Me.New(DatabasePage, 0)
        pRecordLength = MeterDataRecordLength
    End Sub

    ''' <summary>
    ''' Reads a DatabasePage starting from the passed offset into a MeterDatabaseRecord.
    ''' </summary>
    ''' <param name="DatabasePage">A DatabasePage from the receiver</param>
    ''' <param name="Offset">The offset at from which to create the record</param>
    Public Sub New(ByRef DatabasePage As DatabasePage, ByVal Offset As Integer)
        MyBase.New(DatabasePage, Offset, MeterDataRecordLength)
        pRecordType = DatabasePage.RecordType.MeterData
        pMeterGlucose = BitConverter.ToUInt16(pRecordBytes, MeterGlucoseOffset)
        pMeterTimeSeconds = BitConverter.ToUInt32(pRecordBytes, MeterTimeOffset)
    End Sub

    ''' <summary>
    ''' Returns the result of the meter reading in mg/dL.
    ''' </summary>
    ''' <returns>A UShort representing the result of the meter reading.</returns>
    Public ReadOnly Property MeterGlucose As UShort
        Get
            Return pMeterGlucose
        End Get
    End Property

    ''' <summary>
    ''' Returns the time of the meter reading.
    ''' </summary>
    ''' <returns>A DateTime representing the time at which the meter reading was taken.</returns>
    Public ReadOnly Property MeterTime As DateTime
        Get
            Return Utilities.GetReceiverTime(pMeterTimeSeconds)
        End Get
    End Property
End Class
