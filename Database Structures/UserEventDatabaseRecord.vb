''' <summary>
''' Author: Jay Lagorio
''' Date: June 12, 2016
''' Summary: Parses a raw database page into a UserEventDatabaseRecord.
''' </summary>

Public Class UserEventDatabaseRecord
    Inherits DatabaseRecord

    ' Data offsets and lengths
    Public Const UserEventDatabaseRecordLength As Integer = 20
    Private Const EventTypeOffset As Integer = 8
    Private Const EventSubtypeOffset As Integer = 9
    Private Const EventValueOffset As Integer = 14

    ''' <summary>
    ''' The type of event logged in the receiver
    ''' </summary>
    Public Enum EventTypes
        None = 0
        Carbs = 1
        Insulin = 2
        Health = 3
        Exercise = 4
    End Enum

    ''' <summary>
    ''' Event subtypes for Health and Exercise EventTypes.
    ''' </summary>
    Public Enum EventSubtypes
        HealthNone = 0
        HealthIllness = 1
        HealthStress = 2
        HealthHighSymptoms = 3
        HealthLowSymptoms = 4
        HealthCycle = 5
        HealthAlcohol = 6
        ExerciseNone = 0
        ExerciseLight = 1
        ExerciseMedium = 2
        ExerciseHeavy = 3
    End Enum

    ' Holds the type of event from the database record
    Private pEventType As Integer

    ' Holds the subtype of event if the event is Health or Exercise
    Private pEventSubtype As Integer

    ' Holds the value of the event
    Private pEventValue As Double

    ''' <summary>
    ''' Creates a record from the payload of a DatabasePage.
    ''' </summary>
    ''' <param name="DatabasePage">The DatabasePage object read from the device</param>
    Sub New(ByRef DatabasePage As DatabasePage)
        Me.New(DatabasePage, 0)
        pRecordLength = UserEventDatabaseRecordLength
    End Sub

    ''' <summary>
    ''' Creates a record from the payload of a DatabasePage from the specified offset of the 
    ''' payload. Throws a FormatException when the byte offset and record length are invalid.
    ''' </summary>
    ''' <param name="DatabasePage">The DatabasePage object read from the device</param>
    ''' <param name="Offset">The offset to start reading from</param>
    Sub New(ByRef DatabasePage As DatabasePage, ByVal Offset As Integer)
        MyBase.New(DatabasePage, Offset, UserEventDatabaseRecordLength)
        pRecordType = DatabasePage.RecordType.UserEventData
        pEventType = pRecordBytes(EventTypeOffset)
        pEventSubtype = pRecordBytes(EventSubtypeOffset)
        pEventValue = CDbl(BitConverter.ToUInt32(pRecordBytes, EventValueOffset))

        If pEventType = EventTypes.Insulin Then
            pEventValue /= 100.0
        End If
    End Sub

    ''' <summary>
    ''' The event type from the database record.
    ''' </summary>
    ''' <returns>A value from EventTypes if the event is recognized</returns>
    Public ReadOnly Property EventType As EventTypes
        Get
            Return pEventType
        End Get
    End Property

    ''' <summary>
    ''' The event subtype, if any, if the event is a Health or Exercise type. If the event is not
    ''' a Health or Exercise type this value is undefined.
    ''' </summary>
    ''' <returns>A value representing the event subtype</returns>
    Public ReadOnly Property EventSubtype As EventSubtypes
        Get
            Return pEventType
        End Get
    End Property

    ''' <summary>
    ''' The value of the event, e.g., the amount of insulin given or carbs eaten.
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property EventValue As Double
        Get
            Return pEventValue
        End Get
    End Property
End Class
