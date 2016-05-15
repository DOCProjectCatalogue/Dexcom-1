''' <summary>
''' Author: Jay Lagorio
''' Date: May 15, 2016
''' Summary: Parses a raw database page into an EGVData record.
''' </summary>

Public Class EGVDatabaseRecord
    Inherits DatabaseRecord

    ' The glucose and trend record fields are used to store more than the raw
    ' value. The masks below are used to find the display values for those fields.
    Private Const EGV_VALUE_MASK As UInteger = &H3FF
    Private Const EGV_DISPLAY_ONLY_MASK As UInteger = &H8000
    Private Const EGV_TREND_ARROW_MASK As UInteger = &HF
    Private Const EGV_NOISE_MASK As UInteger = &H70

    ' The glucose measurement recorded in the database record.
    Private pGlucoseLevel As UShort

    ' The trend arrow associated with the database entry.
    Private pTrendArrow As Byte

    ''' <summary>
    ''' Some glucose values are reserved and have a special meaning.
    ''' </summary>
    Public Enum SpecialGlucoseValues
        Unknown = 0
        SensorNotActive = 1
        MinimalDeviation = 2
        NoAntenna = 3
        SensorNotCalibrated = 5
        CountsDeviation = 6
        AbsoluteDeviation = 9
        PowerDeviation = 10
        BadRF = 12
    End Enum

    ''' <summary>
    ''' Arrows indicate the device interpretation of the measurement trend
    ''' associated with this database value.
    ''' </summary>
    Public Enum TrendArrows
        Unknown = 0         ' An arrow direction wasn't recognized
        DoubleUp = 1        ' Two up arrows
        SingleUp = 2        ' A single up arrow
        FortyFiveUp = 3     ' An arrow pointing forty-five degrees up
        Flat = 4            ' An arrow pointing horizontal and to the right
        FortyFiveDown = 5   ' An arrow pointing forty-five degrees down
        SingleDown = 6      ' A single down arrow
        DoubleDown = 7      ' Two down arrows
        NotComputable = 8   ' The arrow indicator wasn't computable
        OutOfRange = 9     ' The measurement was out of range
    End Enum

    ' Data offsets and lengths
    Private Const EGVDatabaseRecordLength As Integer = 13
    Private Const GlucoseLevelOffset As Integer = 8
    Private Const TrendArrowOffset As Integer = 10

    ''' <summary>
    ''' Sets the length in bytes of each of this type of record.
    ''' </summary>
    Shared Sub New()
        pRecordLength = EGVDatabaseRecordLength
    End Sub

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
        MyBase.New(DatabasePage, Offset)
        pRecordType = DatabasePage.RecordType.EGVData
        pGlucoseLevel = BitConverter.ToUInt16(pRecordBytes, GlucoseLevelOffset)
        pTrendArrow = pRecordBytes(TrendArrowOffset)
    End Sub

    ''' <summary>
    ''' Returns the glucose measurement in the database record.
    ''' </summary>
    ''' <returns>A short containing the glucose measurement</returns>
    Public ReadOnly Property GlucoseLevel As UShort
        Get
            Return pGlucoseLevel And EGV_VALUE_MASK
        End Get
    End Property

    ''' <summary>
    ''' Returns the direction the indication arrows point for this record.
    ''' </summary>
    ''' <returns>A value of type TrendArrows associated with this record</returns>
    Public ReadOnly Property TrendArrow As TrendArrows
        Get
            Return pTrendArrow And EGV_TREND_ARROW_MASK
        End Get
    End Property

    ''' <summary>
    ''' Returns the noise value of the record
    ''' </summary>
    ''' <returns>A short indicating the level of noise in the reading</returns>
    Public ReadOnly Property Noise As UShort
        Get
            Return (pTrendArrow And EGV_NOISE_MASK) >> 4
        End Get
    End Property
End Class
