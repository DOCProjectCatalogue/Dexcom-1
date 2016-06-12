Imports System.Xml.Serialization
Imports System.Text.UTF8Encoding

''' <summary>
''' Author: Jay Lagorio
''' Date: June 12, 2016
''' Summary: Implmements the ability to connect to and query a Dexcom Receiver.
''' </summary>

Public Class Receiver

    ' The interface used to communicate with the Dexcom receiver.
    Private pDexcomInterface As DeviceInterface

    ' The Serial Number of the Dexcom Receiver - requires prior knowledge if using
    ' Bluetooth but can be retrieved without authentication over USB.
    Private pSerialNumber As String

    ' The hardware part number
    Private pHardwarePartNumber As String

    ' The hardware revision number
    Private pHardwareRevision As String

    ' The date and time the device was created
    Private pDateTimeCreated As DateTime

    ' A GUID uniquely identifying the Receiver
    Private pHardwareID As String

    ' The version of the database schema the device uses.
    Private pSchemaVersion As String

    ' The version of the API exposed by the device.
    Private pApiVersion As String

    ' The API version of the test API.
    Private pTestApiVersion As String

    ' A short string identifying the receiver product type
    Private pProductId As String

    ' A user-friendly string describing the receiver product type
    Private pProductName As String

    ' The number of the software published on the device.
    Private pSoftwareNumber As String

    ' The overall version of Firmware loaded onto the device.
    Private pFirmwareVersion As String

    ' The version of the Port software in this firmware version.
    Private pPortVersion As String

    ' The version of the RF software in this firmware version.
    Private pRFVersion As String

    ' The revision number of the DexBoot software in this firmware version.
    Private pDexBootVersion As String

    ' The version of the BLE software in this firmware version.
    Private pBLEVersion As String

    ' The version of the BLE Soft Device software in this firmware version.
    Private pBLESoftDeviceVersion As String

    ' The type of receiver device (G4 or G5)
    Private pDeviceType As DeviceTypes

    ''' <summary>
    ''' Indicates the present state of the battery
    ''' </summary>
    Public Enum BatteryStates
        Unknown = 0
        Charging = 1
        NotCharging = 2
        NTCFault = 3
        BadBattery = 4
    End Enum

    ''' <summary>
    ''' The glucose measurement unit on the device
    ''' </summary> w
    Public Enum GlucoseUnits
        Unknown = 0
        mgdl = 1
        mmolL = 2
    End Enum

    ''' <summary>
    ''' Clock mode on the device (12 hour clock vs. 24 hour clock)
    ''' </summary>
    Public Enum ClockModes
        TwentyFourHourClock = 0
        TwelveHourClock = 1
    End Enum

    ''' <summary>
    ''' Indicates the present state of the charging circuitry and
    ''' the current level going into the USB port
    ''' </summary>
    Public Enum ChargerCurrentSettings
        Off = 0
        Power100mA = 1
        Power500mA = 2
        PowerMax = 3
        PowerSuspended = 4
    End Enum

    ''' <summary>
    ''' The range of valid database pages for a given database
    ''' </summary>
    Public Structure DatabasePageRange
        Dim RangeStart As Integer
        Dim RangeEnd As Integer
    End Structure

    ''' <summary>
    ''' Identifies whether the device is a G4 or G5 receiver.
    ''' </summary>
    Public Enum DeviceTypes
        Unknown = 0
        G4Receiver = 1
        G5Receiver = 2
    End Enum

    ''' <summary>
    ''' Creates a device using the specified Device Interface
    ''' </summary>
    ''' <param name="DexcomInterface">A DeviceInterface used to connect to the Dexcom Receiver</param>
    Sub New(ByRef DexcomInterface As DeviceInterface)
        pDexcomInterface = DexcomInterface
    End Sub

    ''' <summary>
    ''' The display name of the device as shown in Windows.
    ''' </summary>
    ''' <returns>A String containing the friendly name of the device</returns>
    Public ReadOnly Property ReceiverName As String
        Get
            Return pDexcomInterface.DisplayName
        End Get
    End Property

    ''' <summary>
    ''' Disconnects from the device so it can be connected to later.
    ''' </summary>
    ''' <returns>True if the device is disconnected, False otherwise</returns>
    Public Async Function Disconnect() As Task(Of Boolean)
        Try
            Return Await pDexcomInterface.Disconnect()
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Initiates a connection to a Dexcom Receiver using the specified connection
    ''' </summary>
    ''' <param name="Connection">Returned by a DeviceInterface which describes the connection to the device</param>
    ''' <returns>True if the device has been successfully connected and its properties loaded, False otherwise</returns>
    Public Async Function Connect(ByVal Connection As DeviceInterface.DeviceConnection) As Task(Of Boolean)
        ' Attempt to connect to the device
        Return Await pDexcomInterface.Connect(Connection)
    End Function

    ''' <summary>
    ''' Attempts to reconnect to a device if a previous connection existed.
    ''' </summary>
    ''' <returns>True if the connection was reestablished, False otherwise</returns>
    Public Async Function Connect() As Task(Of Boolean)
        If Not pDexcomInterface Is Nothing Then
            Return Await pDexcomInterface.Connect()
        End If

        Return False
    End Function

    ''' <summary>
    ''' Fetches device attributes such as the manufacturer, serial number, hardware IDs, and
    ''' other data elements from the device at first connection.
    ''' </summary>
    ''' <returns>True if getting the data was successful, False otherwise</returns>
    Private Async Function GetManufacturingData() As Task(Of Boolean)
        Dim DatabasePage As DatabasePage
        Dim ManufacturingDataText As String
        Dim ManufacturingStream As MemoryStream
        Dim ManufacturingDataSerializer As XmlSerializer
        Dim ManufacturingData As ManufacturingParameters = Nothing

        Try
            ' Get the first page of the ManufacturingData database. It has XML content
            ' on it that we need.
            DatabasePage = Await GetDatabasePage(DatabasePartitions.ManufacturingData, 0)
            ManufacturingDataText = DatabasePage.GetPageXMLContent()
        Catch ex As Exception
            ' Looks like something went wrong - abort the connection process
            Return False
        End Try

        Try
            ' Turn the bytes received from the DatabasePage into a stream and deserialize
            ' it to get properties.
            ManufacturingStream = New MemoryStream(UTF8.GetBytes(ManufacturingDataText))
            ManufacturingDataSerializer = New XmlSerializer(GetType(ManufacturingParameters))
            ManufacturingData = ManufacturingDataSerializer.Deserialize(ManufacturingStream)
        Catch Ex As Exception
            ' Sometimes, over Bluetooth, the manufacturing data comes in out of order and can't be 
            ' put together again properly, resulting in a bad XML tag. We'll ignore it so the connection
            ' process can continue but the important part is that the serial number must have already
            ' been passed to the device for authentication. This problem doesn't crop up when connected via USB.
            ManufacturingData = Nothing
        End Try

        ' Record the device attributes from the structure if data was retrieved
        If Not ManufacturingData Is Nothing Then
            pSerialNumber = ManufacturingData.SerialNumber
            pHardwarePartNumber = ManufacturingData.HardwarePartNumber
            pHardwareRevision = ManufacturingData.HardwareRevision
            pDateTimeCreated = DateTime.Parse(ManufacturingData.DateTimeCreated)
            pHardwareID = ManufacturingData.HardwareId
            Return True
        Else
            pSerialNumber = ""
            pHardwareID = ""
            pHardwarePartNumber = ""
            pHardwareRevision = ""
        End If

        Return False
    End Function

    ''' <summary>
    ''' Fetches device attributes such as the software version of device components </summary>
    ''' <returns>True if the request succeeds, False otherwise</returns>
    Private Async Function GetFirmwareData() As Task(Of Boolean)
        Dim DatabasePage As DatabasePage = Nothing
        Dim FirmwareHeaderText As String = ""
        Dim FirmwareStream As MemoryStream = Nothing
        Dim FirmwareHeaderSerializer As XmlSerializer = Nothing
        Dim FirmwareHeader As FirmwareHeader = Nothing

        Dim RequestPacket As New Packet(Packet.Commands.ReadFirmwareHeader)
        If Await pDexcomInterface.SendPacketBytes(RequestPacket.GetPacketBytes()) Then
            Dim ResponsePacket As New Packet(Await pDexcomInterface.ReceivePacketBytes())
            FirmwareHeaderText = UTF8.GetString(ResponsePacket.PayloadData())
        End If

        If FirmwareHeaderText <> "" Then
            Try
                ' Turn the bytes received from the DatabasePage into a stream and deserialize
                ' it to get properties.
                FirmwareStream = New MemoryStream(UTF8.GetBytes(FirmwareHeaderText))
                FirmwareHeaderSerializer = New XmlSerializer(GetType(FirmwareHeader))
                FirmwareHeader = FirmwareHeaderSerializer.Deserialize(FirmwareStream)
            Catch Ex As Exception
                ' Sometimes, over Bluetooth, the manufacturing data comes in out of order and can't be 
                ' put together again properly, resulting in a bad XML tag. We'll ignore it so the connection
                ' process can continue but the important part is that the serial number must have already
                ' been passed to the device for authentication. This problem doesn't crop up when connected via USB.
                FirmwareHeader = Nothing
            End Try
        End If

        ' Record the device attributes from the structure if data was retrieved
        If Not FirmwareHeader Is Nothing Then
            pSchemaVersion = FirmwareHeader.SchemaVersion
            pApiVersion = FirmwareHeader.ApiVersion
            pTestApiVersion = FirmwareHeader.TestApiVersion
            pProductId = FirmwareHeader.ProductId
            pProductName = FirmwareHeader.ProductName
            pSoftwareNumber = FirmwareHeader.SoftwareNumber
            pFirmwareVersion = FirmwareHeader.FirmwareVersion
            pPortVersion = FirmwareHeader.PortVersion
            pRFVersion = FirmwareHeader.RFVersion
            pDexBootVersion = FirmwareHeader.DexBootVersion
            pBLEVersion = FirmwareHeader.BLEVersion
            pBLESoftDeviceVersion = FirmwareHeader.BLESoftDeviceVersion
            Return True
        Else
            pSchemaVersion = ""
            pApiVersion = ""
            pTestApiVersion = ""
            pProductId = ""
            pProductName = ""
            pSoftwareNumber = ""
            pFirmwareVersion = ""
            pPortVersion = ""
            pRFVersion = ""
            pDexBootVersion = ""
            pBLEVersion = ""
            pBLESoftDeviceVersion = ""
        End If

        Return False
    End Function

    ''' <summary>
    ''' Returns the Hardware Board ID.
    ''' </summary>
    ''' <returns>A hex string containing the board ID</returns>
    Public Async Function GetHardwareBoardID() As Task(Of String)
        Dim RequestPacket As New Packet(Packet.Commands.ReadHardwareBoardID)
        Dim ResponseText As String = ""
        If Await pDexcomInterface.SendPacketBytes(RequestPacket.GetPacketBytes()) Then
            Dim ResponsePacket As New Packet(Await pDexcomInterface.ReceivePacketBytes())

            'Format like this:  0003
            For i = ResponsePacket.PayloadData.Count - 1 To 0 Step -1
                Dim HexChar As String = DecimalToHex(ResponsePacket.PayloadData(i))
                If HexChar.Length < 2 Then
                    HexChar = "0" & HexChar
                End If
                ResponseText &= HexChar
            Next
        End If

        Return ResponseText
    End Function

    ''' <summary>
    ''' Generates a simple ACK response from the device to test communication
    ''' </summary>
    ''' <returns>True if the ping was successful, False otherwise</returns>
    Public Async Function Ping() As Task(Of Boolean)
        Dim RequestPacket As New Packet(Packet.Commands.Ping)
        If Await pDexcomInterface.SendPacketBytes(RequestPacket.GetPacketBytes()) Then
            Dim ResponsePacket As Packet
            Try
                ResponsePacket = New Packet(Await pDexcomInterface.ReceivePacketBytes())
            Catch ex As Exception
                ' If the packet failed to build or wasn't received we fail out
                Return False
            End Try

            ' Check to make sure we got an ACK response code, otherwise something could
            ' be wrong with the CRC
            If ResponsePacket.CommandId = Packet.ResponseCodes.Ack Then
                Return True
            End If
        End If

        Return False
    End Function

    ''' <summary>
    ''' Returns a String with the currently paired Transmitter ID.
    ''' </summary>
    ''' <returns>A string containing the ID of the currently paired Transmitter</returns>
    Public Async Function GetTransmitterID() As Task(Of String)
        Dim RequestPacket As New Packet(Packet.Commands.ReadTransmitterID)
        If Await pDexcomInterface.SendPacketBytes(RequestPacket.GetPacketBytes()) Then
            Dim ResponsePacket As New Packet(Await pDexcomInterface.ReceivePacketBytes())
            Return UTF8.GetString(ResponsePacket.PayloadData())
        End If

        Return ""
    End Function

    ''' <summary>
    ''' Returns the battery percentage of the connected device.
    ''' </summary>
    ''' <returns>A Double between 0 and 1 indicating the battery percentage</returns>
    Public Async Function GetBatteryLevel() As Task(Of Double)
        Dim RequestPacket As New Packet(Packet.Commands.ReadBatteryLevel)
        If Await pDexcomInterface.SendPacketBytes(RequestPacket.GetPacketBytes()) Then
            Dim ResponsePacket As New Packet(Await pDexcomInterface.ReceivePacketBytes())
            ' Divide the returned value by 100 to get the percentage between 0 and 1
            Return CDbl(ResponsePacket.PayloadData(0) / 100.0)
        End If

        Return -1
    End Function

    ''' <summary>
    ''' Returns the state of the battery.
    ''' </summary>
    ''' <returns>A value from the BatteryStates Enum describing the state of the battery</returns>
    Public Async Function GetBatteryState() As Task(Of BatteryStates)
        Dim RequestPacket As New Packet(Packet.Commands.ReadBatteryState)
        If Await pDexcomInterface.SendPacketBytes(RequestPacket.GetPacketBytes()) Then
            Dim ResponsePacket As New Packet(Await pDexcomInterface.ReceivePacketBytes())
            Return ResponsePacket.PayloadData(0)
        End If

        Return BatteryStates.Unknown
    End Function

    ''' <summary>
    ''' Returns a DateTime representing the UTC value of the real-time clock on the device.
    ''' </summary>
    ''' <returns>A DateTime containing to time on the RTC</returns>
    Public Async Function GetRealTimeClock() As Task(Of DateTime)
        Dim RequestPacket As New Packet(Packet.Commands.ReadRealTimeClock)
        If Await pDexcomInterface.SendPacketBytes(RequestPacket.GetPacketBytes()) Then
            Dim ResponsePacket As New Packet(Await pDexcomInterface.ReceivePacketBytes())

            ' [3][2][1][0] <- 0 changes the fastest
            Dim TotalSeconds As Integer = CInt(ResponsePacket.PayloadData(0)) +
            CInt(ResponsePacket.PayloadData(1) * &H100) +
            CInt(ResponsePacket.PayloadData(2) * &H10000) +
            CInt(ResponsePacket.PayloadData(3) * &H1000000)

            Return GetReceiverTime(TotalSeconds)
        End If

        Return DateTime.MinValue
    End Function

    ''' <summary>
    ''' Returns the system time in UTC on the device
    ''' </summary>
    ''' <returns>A DateTime representing system time or DateTime.MinValue if it could not be retrieved</returns>
    Public Async Function GetSystemTime() As Task(Of DateTime)
        Dim RequestPacket As New Packet(Packet.Commands.ReadSystemTime)
        If Await pDexcomInterface.SendPacketBytes(RequestPacket.GetPacketBytes()) Then
            Dim ResponsePacket As New Packet(Await pDexcomInterface.ReceivePacketBytes())

            ' [3][2][1][0] <- 0 changes the fastest
            Dim TotalSeconds As Integer = CInt(ResponsePacket.PayloadData(0)) +
            CInt(ResponsePacket.PayloadData(1) * &H100) +
            CInt(ResponsePacket.PayloadData(2) * &H10000) +
            CInt(ResponsePacket.PayloadData(3) * &H1000000)

            Return GetReceiverTime(TotalSeconds)
        End If

        Return DateTime.MinValue
    End Function

    ''' <summary>
    ''' Returns the offset of the clock from UTC to local time.
    ''' </summary>
    ''' <returns>A TimeSpan representing the difference between UTC and device local time or TimeSpam.MinValue if it couldn't be retrieved</returns>
    Public Async Function GetSystemTimeOffset() As Task(Of TimeSpan)
        Dim RequestPacket As New Packet(Packet.Commands.ReadSystemTimeOffset)
        If Await pDexcomInterface.SendPacketBytes(RequestPacket.GetPacketBytes()) Then
            Dim ResponsePacket As New Packet(Await pDexcomInterface.ReceivePacketBytes())

            Dim TotalSeconds As Integer = CInt(ResponsePacket.PayloadData(0)) +
            CInt(ResponsePacket.PayloadData(1) * &H100) +
            CInt(ResponsePacket.PayloadData(2) * &H10000) +
            CInt(ResponsePacket.PayloadData(3) * &H1000000)

            Return New TimeSpan(TotalSeconds * TimeSpan.TicksPerSecond)
        End If

        Return TimeSpan.MinValue
    End Function

    ''' <summary>
    ''' Returns a TimeSpan representing the offset between the device time and the display time.
    ''' </summary>
    ''' <returns>A TimeSpan of the difference between the device time and the display time or TimeSpan.MinValue if an error occurred</returns>
    Public Async Function GetDisplayTimeOffset() As Task(Of TimeSpan)
        Dim RequestPacket As New Packet(Packet.Commands.ReadDisplayTimeOffset)
        If Await pDexcomInterface.SendPacketBytes(RequestPacket.GetPacketBytes()) Then
            Dim ResponsePacket As New Packet(Await pDexcomInterface.ReceivePacketBytes())

            Dim TotalSeconds As Integer = CInt(ResponsePacket.PayloadData(0)) +
            CInt(ResponsePacket.PayloadData(1) * &H100) +
            CInt(ResponsePacket.PayloadData(2) * &H10000) +
            CInt(ResponsePacket.PayloadData(3) * &H1000000)

            Return New TimeSpan(TotalSeconds * TimeSpan.TicksPerSecond)
        End If

        Return TimeSpan.MinValue
    End Function

    ''' <summary>
    ''' Returns the unit of measurement for glucose.
    ''' </summary>
    ''' <returns>A value from the GlucoseUnits Enum representing the unit of measurement set on the device</returns>
    Public Async Function GetGlucoseUnit() As Task(Of GlucoseUnits)
        Dim RequestPacket As New Packet(Packet.Commands.ReadGlucoseUnit)
        If Await pDexcomInterface.SendPacketBytes(RequestPacket.GetPacketBytes()) Then
            Dim ResponsePacket As New Packet(Await pDexcomInterface.ReceivePacketBytes())

            ' The only byte in the payload contains the value we're looking for
            Return ResponsePacket.PayloadData(0)
        End If

        Return GlucoseUnits.Unknown
    End Function

    ''' <summary>
    ''' Returns whether the clock is in 12 hour mode or 24 hour mode.
    ''' </summary>
    ''' <returns>Returns a value from the ClockModes Enum indicating the clock mode set on the device</returns>
    Public Async Function GetClockMode() As Task(Of ClockModes)
        Dim RequestPacket As New Packet(Packet.Commands.ReadClockMode)
        If Await pDexcomInterface.SendPacketBytes(RequestPacket.GetPacketBytes()) Then
            Dim ResponsePacket As New Packet(Await pDexcomInterface.ReceivePacketBytes())

            ' The only byte in the payload contains the value we're looking for
            Return ResponsePacket.PayloadData(0)
        End If

        Return ClockModes.TwentyFourHourClock
    End Function

    ''' <summary>
    ''' Returns the state of the charging ciruit.
    ''' </summary>
    ''' <returns>A value from the ChargerCurrentSettings Enum indicating the charging status of the device</returns>
    Public Async Function GetChargerCurrentSetting() As Task(Of ChargerCurrentSettings)
        Dim RequestPacket As New Packet(Packet.Commands.ReadCurrentChargerSettings)
        If Await pDexcomInterface.SendPacketBytes(RequestPacket.GetPacketBytes()) Then
            Dim ResponsePacket As New Packet(Await pDexcomInterface.ReceivePacketBytes())

            ' The only byte in the payload contains the value we're looking for
            Return ResponsePacket.PayloadData(0)
        End If

        Return ChargerCurrentSettings.Off
    End Function

    ''' <summary>
    ''' Returns device firmware settings.
    ''' </summary>
    ''' <returns>A String showing device firmware settings</returns>
    Public Async Function GetFirmwareSettings() As Task(Of String)
        Dim RequestPacket As New Packet(Packet.Commands.ReadFirmwareSettings)
        If Await pDexcomInterface.SendPacketBytes(RequestPacket.GetPacketBytes()) Then
            Dim ResponsePacket As New Packet(Await pDexcomInterface.ReceivePacketBytes())

            Return UTF8.GetString(ResponsePacket.PayloadData())
        End If

        Return ""
    End Function

    ''' <summary>
    ''' Returns all records in the specified database.
    ''' </summary>
    ''' <param name="DatabaseName">A String as defined in DatabasePartitions specifying the database to query</param>
    ''' <returns>A Collection of all DatabaseRecords from the specified database, the specific type of which will depend on the database in question</returns>
    Public Async Function GetDatabaseContents(ByVal DatabaseName As String) As Task(Of Collection(Of DatabaseRecord))
        Return Await GetDatabaseContents(DatabaseName, DateTime.MinValue)
    End Function

    ''' <summary>
    ''' Returns all records in the specified database.
    ''' </summary>
    ''' <param name="DatabaseName">A String as defined in DatabasePartitions specifying the database to query</param>
    ''' <returns>A Collection of all DatabaseRecords from the specified database, the specific type of which will depend on the database in question</returns>
    Public Async Function GetDatabaseContents(ByVal DatabaseName As String, ByVal StartingTime As DateTime) As Task(Of Collection(Of DatabaseRecord))
        Dim Results As New Collection(Of DatabaseRecord)

        ' Get the range of pages that make up this database and scan through backwards
        Dim PageRange As DatabasePageRange = Nothing
        Try
            PageRange = Await GetDatabasePageRange(DatabaseName)
        Catch Ex As FormatException
            PageRange = Nothing
        End Try

        ' Check to make sure the page range was successfully retrieved
        If Not CObj(PageRange) Is Nothing Then
            For i = PageRange.RangeEnd To PageRange.RangeStart Step -1
                ' Get the data associated with this database page and scan through it backwards
                Dim RawPage As DatabasePage = Nothing
                Try
                    RawPage = Await GetDatabasePage(DatabaseName, i)
                Catch ex As OverflowException
                    RawPage = Nothing
                End Try

                If Not RawPage Is Nothing Then
                    For j = RawPage.NumberOfRecords - 1 To 0 Step -1
                        Dim NewRecord As DatabaseRecord = Nothing

                        ' Build the structure from a class inheriting from DatabaseRecord
                        Select Case RawPage.DataRecordType
                            Case DatabasePage.RecordType.EGVData
                                NewRecord = New EGVDatabaseRecord(RawPage, j * EGVDatabaseRecord.EGVDatabaseRecordLength)
                            Case DatabasePage.RecordType.SensorData
                                NewRecord = New SensorDatabaseRecord(RawPage, j * SensorDatabaseRecord.SensorDataRecordLength)
                            Case DatabasePage.RecordType.MeterData
                                NewRecord = New MeterDatabaseRecord(RawPage, j * MeterDatabaseRecord.MeterDataRecordLength)
                            Case DatabasePage.RecordType.InsertionData
                                NewRecord = New InsertionDatabaseRecord(RawPage, j * InsertionDatabaseRecord.InsertionDatabaseRecordLength)
                            Case DatabasePage.RecordType.UserEventData
                                NewRecord = New UserEventDatabaseRecord(RawPage, j * UserEventDatabaseRecord.UserEventDatabaseRecordLength)
                            Case Else
                                ' This exception will need to be handled by the calling function
                                Throw New NotImplementedException
                        End Select

                        ' Once we cross 15 minutes behind the point at which the user is interested in
                        ' collecting data we stop, otherwise we add the record to the Collection.
                        If NewRecord.DisplayTime >= StartingTime Then
                            Call Results.Add(NewRecord)
                        ElseIf NewRecord.DisplayTime < StartingTime.Subtract(New TimeSpan(0, 15, 0)) Then
                            Return Results
                        End If
                    Next
                End If
            Next
        End If

        Return Results
    End Function

    ''' <summary>
    ''' Returns partition information for all databases.
    ''' </summary>
    ''' <returns>A string representing partition information for all databases or an empty string if an error occurrs</returns>
    Public Async Function GetDatabasePartitionInfo() As Task(Of String)
        Dim RequestPacket As New Packet(Packet.Commands.ReadDatabasePartitionInfo)
        If Await pDexcomInterface.SendPacketBytes(RequestPacket.GetPacketBytes()) Then
            Dim ResponsePacket As New Packet(Await pDexcomInterface.ReceivePacketBytes())
            Return UTF8.GetString(ResponsePacket.PayloadData())
        End If

        Return ""
    End Function

    ''' <summary>
    ''' Returns the range of pages available to query from the specified database.
    ''' </summary>
    ''' <param name="DatabaseName">A String from the constants in DatabasePartitions indicating the database to query</param>
    ''' <returns>A DatabasePageRange structure indicating the first and last pages of the specified database</returns>
    Private Async Function GetDatabasePageRange(ByVal DatabaseName As String) As Task(Of DatabasePageRange)
        ' The Payload byte is to indicate to the device which database to query
        Dim Payload() As Byte = {DatabasePartitions.GetID(DatabaseName)}

        Dim RequestPacket As New Packet(Packet.Commands.ReadDatabasePageRange, Payload)
        Dim DatabasePageRange As DatabasePageRange = Nothing
        If Await pDexcomInterface.SendPacketBytes(RequestPacket.GetPacketBytes()) Then
            Dim ResponsePacket As New Packet(Await pDexcomInterface.ReceivePacketBytes())
            If ResponsePacket.CommandId = Packet.ResponseCodes.Ack Then

                ' The payload returned is two four byte Integers that indicate the
                ' range of pages available to be queried
                DatabasePageRange = New DatabasePageRange
                If Not ResponsePacket.PayloadData Is Nothing Then
                    DatabasePageRange.RangeStart = BitConverter.ToUInt32(ResponsePacket.PayloadData, 0)
                    DatabasePageRange.RangeEnd = BitConverter.ToUInt32(ResponsePacket.PayloadData, 4)
                End If
            End If
        End If

        Return DatabasePageRange
    End Function

    ''' <summary>
    ''' Retrieves the specified database page from the named database.
    ''' </summary>
    ''' <param name="DatabaseName">String from the group of constants defined in DatabasePartitions indicating the database to read from</param>
    ''' <param name="PageToRead">Integer specifying which page to read</param>
    ''' <returns>Returns a DatabasePage with the content requested, or Nothing if an error occurrs</returns>
    Private Async Function GetDatabasePage(ByVal DatabaseName As String, ByVal PageToRead As Integer) As Task(Of DatabasePage)
        Dim DatabasePage As DatabasePage = Nothing
        Dim Payload(5) As Byte

        ' The database ID to read
        Payload(0) = DatabasePartitions.GetID(DatabaseName)
        ' The page in the database to read (copied as one byte)
        BitConverter.GetBytes(PageToRead).CopyTo(Payload, 1)
        ' Unresearched. A sentinel? The number of pages to return?
        Payload(5) = 1

        Dim RequestPacket As New Packet(Packet.Commands.ReadDatabasePages, Payload)
        If Await pDexcomInterface.SendPacketBytes(RequestPacket.GetPacketBytes()) Then
            Dim ResponsePacketBytes() As Byte = Await pDexcomInterface.ReceivePacketBytes()
            If Not ResponsePacketBytes Is Nothing Then
                Dim ResponsePacket As New Packet(ResponsePacketBytes)
                If ResponsePacket.CommandId = Packet.ResponseCodes.Ack Then
                    ' Create a DatabasePage with the byte stream returned by the device
                    DatabasePage = New DatabasePage(ResponsePacket.PayloadData)
                End If
            End If
        End If

        Return DatabasePage
    End Function

    ''' <summary>
    ''' The Serial Number of the device. This must be known prior to operations over Bluetooth
    ''' but can be queried directly over USB interfaces.
    ''' </summary>
    ''' <returns>A String containing the device Serial Number</returns>
    Public ReadOnly Property SerialNumber As String
        Get
            ' If there's a serial number as part of the underlying interface then return
            ' that. Otherwise attempt to get what was retrieved from the manufacturing data.
            If pDexcomInterface.GetType Is GetType(BLEInterface) Then
                Dim UnderlyingInterface As BLEInterface = pDexcomInterface
                If UnderlyingInterface.SerialNumber <> "" Then
                    Return UnderlyingInterface.SerialNumber
                End If
            End If

            If pSerialNumber = "" Then
                Dim GetDeviceData As Task = Task.Run(AddressOf GetManufacturingData)
                Call GetDeviceData.Wait()
            End If
            Return pSerialNumber
        End Get
    End Property

    ''' <summary>
    ''' Returns a string with the Hardware Part Number.
    ''' </summary>
    ''' <returns>A String containing the Hardware Part Number</returns>
    Public ReadOnly Property HardwarePartNumber As String
        Get
            ' If the attribute hasn't been retrieved then attempt to retrieve it.
            If pHardwarePartNumber = "" Then
                Dim GetDeviceData As Task = Task.Run(AddressOf GetManufacturingData)
                Call GetDeviceData.Wait()
            End If

            Return pHardwarePartNumber
        End Get
    End Property

    ''' <summary>
    ''' Returns a String with the device's Hardware Revision number.
    ''' </summary>
    ''' <returns>A string containing the Hardware Revision number</returns>
    Public ReadOnly Property HardwareRevision As String
        Get
            ' If the attribute hasn't been retrieved then attempt to retrieve it.
            If pHardwareRevision = "" Then
                Dim GetDeviceData As Task = Task.Run(AddressOf GetManufacturingData)
                Call GetDeviceData.Wait()
            End If

            Return pHardwareRevision
        End Get
    End Property

    ''' <summary>
    ''' Returns the date and time at which the device was created.
    ''' </summary>
    ''' <returns>A DateTime indicating when the device was created</returns>
    Public ReadOnly Property DateTimeCreated As DateTime
        Get
            ' If the attribute hasn't been retrieved then attempt to retrieve it.
            If pDateTimeCreated = "" Then
                Dim GetDeviceData As Task = Task.Run(AddressOf GetManufacturingData)
                Call GetDeviceData.Wait()
            End If

            Return pDateTimeCreated
        End Get
    End Property

    ''' <summary>
    ''' The Hardware ID of the device.
    ''' </summary>
    ''' <returns>A string containing a GUID representing the device unique identifier</returns>
    Public ReadOnly Property HardwareID As String
        Get
            ' If the attribute hasn't been retrieved then attempt to retrieve it.
            If pHardwareID = "" Then
                Dim GetDeviceData As Task = Task.Run(AddressOf GetManufacturingData)
                Call GetDeviceData.Wait()
            End If

            Return pHardwareID
        End Get
    End Property

    ''' <summary>
    ''' The version of the database schema the device uses.
    ''' </summary>
    ''' <returns>A string representing the SchemaVersion property from the device</returns>
    Public ReadOnly Property SchemaVersion As String
        Get
            ' If the attribute hasn't been retrieved then attempt to retrieve it.
            If pSchemaVersion = "" Then
                Dim GetFirmwareHeader As Task = Task.Run(AddressOf GetFirmwareData)
                Call GetFirmwareHeader.Wait()
            End If

            Return pSchemaVersion
        End Get
    End Property

    ''' <summary>
    ''' The version of the API exposed by the device.
    ''' </summary>
    ''' <returns>A string representing the ApiVersion property from the device</returns>
    Public ReadOnly Property ApiVersion As String
        Get
            ' If the attribute hasn't been retrieved then attempt to retrieve it.
            If pApiVersion = "" Then
                Dim GetFirmwareHeader As Task = Task.Run(AddressOf GetFirmwareData)
                Call GetFirmwareHeader.Wait()
            End If

            Return pApiVersion
        End Get
    End Property

    ''' <summary>
    ''' The API version of the test API.
    ''' </summary>
    ''' <returns>A string representing the TestApiVersion property from the device</returns>
    Public ReadOnly Property TestApiVersion As String
        Get
            ' If the attribute hasn't been retrieved then attempt to retrieve it.
            If pTestApiVersion = "" Then
                Dim GetFirmwareHeader As Task = Task.Run(AddressOf GetFirmwareData)
                Call GetFirmwareHeader.Wait()
            End If

            Return pTestApiVersion
        End Get
    End Property

    ''' <summary>
    ''' A short string identifying the receiver product type
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property ProductId As String
        Get
            ' If the attribute hasn't been retrieved then attempt to retrieve it.
            If pProductId = "" Then
                Dim GetFirmwareHeader As Task = Task.Run(AddressOf GetFirmwareData)
                Call GetFirmwareHeader.Wait()
            End If

            Return pProductId
        End Get
    End Property

    ''' <summary>
    ''' A user-friendly string describing the receiver product type
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property ProductName As String
        Get
            ' If the attribute hasn't been retrieved then attempt to retrieve it.
            If pProductName = "" Then
                Dim GetFirmwareHeader As Task = Task.Run(AddressOf GetFirmwareData)
                Call GetFirmwareHeader.Wait()
            End If

            Return pProductName
        End Get
    End Property

    ''' <summary>
    ''' The number of the software published on the device.
    ''' </summary>
    ''' <returns>Returns a product code identifying the loaded software</returns>
    Public ReadOnly Property SoftwareNumber As String
        Get
            ' If the attribute hasn't been retrieved then attempt to retrieve it.
            If pSoftwareNumber = "" Then
                Dim GetFirmwareHeader As Task = Task.Run(AddressOf GetFirmwareData)
                Call GetFirmwareHeader.Wait()
            End If

            Return pSoftwareNumber
        End Get
    End Property

    ''' <summary>
    ''' The overall version of Firmware loaded onto the device.
    ''' </summary>
    ''' <returns>A version string representing the overall firmware version</returns>
    Public ReadOnly Property FirmwareVersion As String
        Get
            ' If the attribute hasn't been retrieved then attempt to retrieve it.
            If pFirmwareVersion = "" Then
                Dim GetFirmwareHeader As Task = Task.Run(AddressOf GetFirmwareData)
                Call GetFirmwareHeader.Wait()
            End If

            Return pFirmwareVersion
        End Get
    End Property

    ''' <summary>
    ''' The version of the Port software in this firmware version.
    ''' </summary>
    ''' <returns>A version string representing the port version</returns>
    Public ReadOnly Property PortVersion As String
        Get
            ' If the attribute hasn't been retrieved then attempt to retrieve it.
            If pPortVersion = "" Then
                Dim GetFirmwareHeader As Task = Task.Run(AddressOf GetFirmwareData)
                Call GetFirmwareHeader.Wait()
            End If

            Return pPortVersion
        End Get
    End Property

    ''' <summary>
    ''' The version of the RF software in this firmware version.
    ''' </summary>
    ''' <returns>A version string representing the RF version</returns>
    Public ReadOnly Property RFVersion As String
        Get
            ' If the attribute hasn't been retrieved then attempt to retrieve it.
            If pRFVersion = "" Then
                Dim GetFirmwareHeader As Task = Task.Run(AddressOf GetFirmwareData)
                Call GetFirmwareHeader.Wait()
            End If

            Return pRFVersion
        End Get
    End Property

    ''' <summary>
    ''' The revision number of the DexBoot software in this firmware version.
    ''' </summary>
    ''' <returns>An String containing an integer representing the revision of DexBoot</returns>
    Public ReadOnly Property DexBootVersion As String
        Get
            ' If the attribute hasn't been retrieved then attempt to retrieve it.
            If pDexBootVersion = "" Then
                Dim GetFirmwareHeader As Task = Task.Run(AddressOf GetFirmwareData)
                Call GetFirmwareHeader.Wait()
            End If

            Return pDexBootVersion
        End Get
    End Property

    ''' <summary>
    ''' The version of the BLE software in this firmware version.
    ''' </summary>
    ''' <returns>A version string representing the BLE version</returns>
    Public ReadOnly Property BLEVersion As String
        Get
            ' If the attribute hasn't been retrieved then attempt to retrieve it.
            If pBLEVersion = "" Then
                Dim GetFirmwareHeader As Task = Task.Run(AddressOf GetFirmwareData)
                Call GetFirmwareHeader.Wait()
            End If

            Return pBLEVersion
        End Get
    End Property

    ''' <summary>
    ''' The version of the BLE Soft Device software in this firmware version.
    ''' </summary>
    ''' <returns>A version string representing the BLE Soft Device version</returns>
    Public ReadOnly Property BLESoftDeviceVersion As String
        Get
            ' If the attribute hasn't been retrieved then attempt to retrieve it.
            If pBLESoftDeviceVersion = "" Then
                Dim GetFirmwareHeader As Task = Task.Run(AddressOf GetFirmwareData)
                Call GetFirmwareHeader.Wait()
            End If

            Return pBLESoftDeviceVersion
        End Get
    End Property

    ''' <summary>
    ''' Returns the type of receiver connected, either G4 or G5, or Unknown otherwise.
    ''' </summary>
    ''' <returns>A DeviceTypes value representing the type of device, or Unknown if not recognized</returns>
    Public ReadOnly Property DeviceType As DeviceTypes
        Get
            If pDeviceType = DeviceTypes.Unknown Then
                If pProductId = "" Then
                    Dim GetFirmwareHeader As Task = Task.Run(AddressOf GetFirmwareData)
                    Call GetFirmwareHeader.Wait()
                End If

                ' There is only one G5 device (so far) but a few G4 devices. If the
                ' device isn't a G5 device but has G4 in the ProductId then treat it
                ' as a G4 device. Otherwise it is unknown.
                Select Case pProductId
                    Case "G5MobileReceiver"
                        Return pDeviceType = DeviceTypes.G5Receiver
                    Case Else
                        If pProductId.ToUpper.Contains("G4") Then
                            pDeviceType = DeviceTypes.G4Receiver
                        Else
                            pDeviceType = DeviceTypes.Unknown
                        End If
                End Select
            End If

            Return pDeviceType
        End Get
    End Property
End Class

