Imports Dexcom
Imports Windows.Storage.Streams
Imports System.Text.UTF8Encoding

''' <summary>
''' Author: Jay Lagorio
''' Date: June 5, 2016
''' Summary: Detects, connects, and exchanges data with a Dexcom Receiver using Bluetooth LE.
''' </summary>

Public Class BLEInterface
    Inherits DeviceInterface

    ' The Cradle Service is the initial entry point into interacting with the device
    Private Const CradleServiceGUID As String = "F0ABA0B1-EBFA-F96F-28DA-076C35A521DB"
    Private Const CradleService2GUID As String = "F0ACA0B1-EBFA-F96F-28DA-076C35A521DB"
    Private Const GattInterfaceClassGUID As String = "6E3BB679-4372-40C8-9EAA-4509DF260CD8"

    ' Characteristics associated with Dexcom Share devices (Version 1)
    Private Const AuthenticationCodeGUID As String = "F0ABACAC-EBFA-F96F-28DA-076C35A521DB"
    Private Const ShareMessageReceiverGUID As String = "F0ABB20A-EBFA-F96F-28DA-076C35A521DB"
    Private Const ShareMessageResponseGUID As String = "F0ABB20B-EBFA-F96F-28DA-076C35A521DB"
    Private Const CommandGUID As String = "F0ABB0CC-EBFA-F96F-28DA-076C35A521DB"
    Private Const ResponseGUID As String = "F0ABB0CD-EBFA-F96F-28DA-076C35A521DB"
    Private Const HeartBeatGUID As String = "F0AB2B18-EBFA-F96F-28DA-076C35A521DB"

    ' Characteristics associated with Dexcom Share 2 devices (Version 2)
    Private Const AuthenticationCode2GUID As String = "F0ACACAC-EBFA-F96F-28DA-076C35A521DB"
    Private Const ShareMessageReceiver2GUID As String = "F0ACB20A-EBFA-F96F-28DA-076C35A521DB"
    Private Const ShareMessageResponse2GUID As String = "F0ACB20B-EBFA-F96F-28DA-076C35A521DB"
    Private Const Command2GUID As String = "F0ACB0CC-EBFA-F96F-28DA-076C35A521DB"
    Private Const Response2GUID As String = "F0ACB0CD-EBFA-F96F-28DA-076C35A521DB"
    Private Const HeartBeat2GUID As String = "F0AC2B18-EBFA-F96F-28DA-076C35A521DB"

    ' The default number of seconds to wait for data reception or authentication from the device
    Private Const ReceiveDelayMilliseconds As Integer = 100
    Private Const ReceiveTimeoutSeconds As Integer = 4
    Private Const AuthenticateTimeoutSeconds As Integer = 15

    ' Stores the last time a communication was received from the device. Exceeding
    ' that threshold will require a new connection process.
    Private pLastCommunicationTime As DateTime = DateTime.MinValue
    Private Const LastCommunicationTimeoutSeconds As Integer = 180

    ' The Bluetooth name advertised by Dexcom devices
    Private Const DexcomBluetoothDeviceName As String = "DEXCOMRX"

    ' States indicating the connection status to the device
    Private Enum ConnectionStates
        NotConnected = 0        ' Not connected at all
        MustAuthenticate = 1    ' Connection established, no authentication done
        Authenticating = 2      ' In the process of doing authentication
        Connected = 3           ' Connected and authenticated
    End Enum

    ' Connection state tracker since interaction with the Heartbeat
    ' characteristic happens on another thread during authentication
    Private pConnectionState As ConnectionStates = ConnectionStates.NotConnected

    ' The Share version on the device
    Private pShareVersion As Integer = 0

    ' Bytes transferred from the device in response to a command. This is
    ' done using a member variable because the Receiver still has to call the
    ' ReceivePacketBytes() command.
    Private pReceivedBytes As New Collection(Of Byte())

    ' The primary device service on the device
    Private pCradleDeviceService As GattDeviceService = Nothing

    ' Write the device serial number to this characteristic to authenticate to the device
    Private pAuthenticationCharacteristic As GattCharacteristic = Nothing

    ' Write commands to the device on this chracteristic
    Private pShareMessageReceiverCharacteristic As GattCharacteristic = Nothing

    ' This characteristic will send responses to commands once processed
    Private pShareMessageResponseCharacteristic As GattCharacteristic = Nothing

    ' Not used
    Private pCommandCharacteristic As GattCharacteristic = Nothing
    Private pResponseCharacteristic As GattCharacteristic = Nothing

    ' The heartbeat characteristic response to authentication requests and
    ' is the session keep-alive mechanism for sessions
    Private pHeartbeatCharacteristic As GattCharacteristic = Nothing

    ''' <summary>
    ''' Sets the interface name and default receive timeout
    ''' </summary>
    Sub New()
        pInterfaceName = "BLE"
        ReceiveTimeout = ReceiveTimeoutSeconds
    End Sub

    ''' <summary>
    ''' Returns the string provided by Windows that opens a handle to the device.
    ''' </summary>
    ''' <returns>A String Windows uses to open a connection to the device</returns>
    Public Overrides ReadOnly Property DeviceID As String
        Get
            Return pDeviceID
        End Get
    End Property

    ''' <summary>
    ''' Returns the friendly name of the device provided by Windows.
    ''' </summary>
    ''' <returns>A string suitable for display in a user interface</returns>
    Public Overrides ReadOnly Property DisplayName As String
        Get
            Return pDisplayName
        End Get
    End Property

    ''' <summary>
    ''' This property must be set before connecting to a Dexcom Receiver over
    ''' this interface.
    ''' </summary>
    ''' <returns>A String containing the device serial number set by the user</returns>
    Public Property SerialNumber As String

    ''' <summary>
    ''' The time in seconds to timeout when waiting for data from the 
    ''' device. This defaults to 5 seconds.
    ''' </summary>
    ''' <returns>The previously set receive timeout, defaults to 5 seconds</returns>
    Public Property ReceiveTimeout As Integer

    ''' <summary>
    ''' Returns a Collection of Dexcom Receiver devices connected to the system using interfaces derived from this class.
    ''' </summary>
    ''' <returns>A Collection of devices to connect to using an interface derived from this class</returns>
    Public Overrides Async Function GetAvailableDevices() As Task(Of Collection(Of DeviceConnection))
        Dim DeviceCollection As New Collection(Of DeviceConnection)
        Return Await GetAvailableDevices(DeviceCollection)
    End Function

    ''' <summary>
    ''' Returns a Collection of Dexcom Receiver devices connected to the system using interfaces derived from this class.
    ''' </summary>
    ''' <param name="AvailableDevices">A Collection of DeviceConnection structures to be supplemented</param>
    ''' <returns>A Collection of devices available for connection, including DeviceConnection passed to the function</returns>
    Public Overrides Async Function GetAvailableDevices(AvailableDevices As Collection(Of DeviceConnection)) As Task(Of Collection(Of DeviceConnection))
        If AvailableDevices Is Nothing Then AvailableDevices = New Collection(Of DeviceConnection)

        ' Look through all devices connected to the machine for devices with the name DEXCOMRX
        Dim DevInfoColl As DeviceInformationCollection = Await DeviceInformation.FindAllAsync()
        For i = 0 To DevInfoColl.Count - 1
            If DevInfoColl(i).Name.ToUpper = DexcomBluetoothDeviceName Then

                ' If a device is found check to see whether it's the first or second version of Share
                If (DevInfoColl(i).Id.ToUpper.Contains(CradleServiceGUID) Or DevInfoColl(i).Id.ToUpper.Contains(CradleService2GUID)) And DevInfoColl(i).Id.ToUpper.Contains(GattInterfaceClassGUID) Then
                    ' Include in the list of connectable devices
                    Dim Device As New DeviceConnection
                    Device.DeviceId = DevInfoColl(i).Id
                    Device.DisplayName = DevInfoColl(i).Name
                    Device.InterfaceName = pInterfaceName

                    Call AvailableDevices.Add(Device)
                End If
            End If
        Next

        Return AvailableDevices
    End Function

    ''' <summary>
    ''' Attempts to connect to the passed device over the interface represented by the derivative class. For the BLE
    ''' interface the SerialNumber property must be set to the serial number of the device or the connection will fail.
    ''' If a connection has been previously established and is within the last communication timeout period the function
    ''' will return True but won't actually attempt to connect to the device.
    ''' </summary>
    ''' <param name="AvailableConnection">An AvailableConnection structure representing the device to connect to</param>
    ''' <returns>True if the device is connected, False otherwise</returns>
    Friend Overrides Async Function Connect(AvailableConnection As DeviceConnection) As Task(Of Boolean)
        ' If the calling function passes a connection with the wrong interface fail the call
        If AvailableConnection.InterfaceName <> pInterfaceName Then Return False

        ' If the connection is already established and within the timeout period don't establish the connection
        If pConnectionState = ConnectionStates.Connected Then
            If pLastCommunicationTime.Add(New TimeSpan(0, 0, LastCommunicationTimeoutSeconds)) > DateTime.Now Then
                Return True
            Else
                ' Clean up the Bluetooth variables before attempting to reestablish the connection.
                Call DisposeConnection()
            End If
        End If

        ' When connecting over BLE the serial number must be known in advance
        If SerialNumber = "" Then Return False

        ' Connect to the cradle service
        Dim pCradleDeviceService As GattDeviceService
        Try
            pCradleDeviceService = Await GattDeviceService.FromIdAsync(AvailableConnection.DeviceId)
        Catch ex As Exception
            pCradleDeviceService = Nothing
        End Try

        If Not pCradleDeviceService Is Nothing Then
            ' Check the version of the cradle service and search for characteristics accordingly
            If AvailableConnection.DeviceId.ToUpper.Contains(CradleServiceGUID) Then
                pShareVersion = 1
                GetCharacteristic(pCradleDeviceService, AuthenticationCodeGUID, pAuthenticationCharacteristic)
                GetCharacteristic(pCradleDeviceService, ShareMessageReceiverGUID, pShareMessageReceiverCharacteristic)
                GetCharacteristic(pCradleDeviceService, ShareMessageResponseGUID, pShareMessageResponseCharacteristic)
                GetCharacteristic(pCradleDeviceService, CommandGUID, pCommandCharacteristic)
                GetCharacteristic(pCradleDeviceService, ResponseGUID, pResponseCharacteristic)
                GetCharacteristic(pCradleDeviceService, HeartBeatGUID, pHeartbeatCharacteristic)
            ElseIf AvailableConnection.DeviceId.ToUpper.Contains(CradleService2GUID) Then
                pShareVersion = 2
                GetCharacteristic(pCradleDeviceService, AuthenticationCode2GUID, pAuthenticationCharacteristic)
                GetCharacteristic(pCradleDeviceService, ShareMessageReceiver2GUID, pShareMessageReceiverCharacteristic)
                GetCharacteristic(pCradleDeviceService, ShareMessageResponse2GUID, pShareMessageResponseCharacteristic)
                GetCharacteristic(pCradleDeviceService, Command2GUID, pCommandCharacteristic)
                GetCharacteristic(pCradleDeviceService, Response2GUID, pResponseCharacteristic)
                GetCharacteristic(pCradleDeviceService, HeartBeat2GUID, pHeartbeatCharacteristic)
            End If

            ' Handler for Heartbeat, used for authentication and session keep-alive. Set the
            ' characteristic for notifications for when data comes in.
            AddHandler pHeartbeatCharacteristic.ValueChanged, AddressOf OnHeartbeat
            Await pHeartbeatCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify)

            ' Handler for data responses, used to collect data in response to commands sent to the device. Set
            ' characteristic for notifications when data comes in.
            AddHandler pShareMessageResponseCharacteristic.ValueChanged, AddressOf OnShareMessageResponse
            Await pShareMessageResponseCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify)

            ' Check to see if the authentication handler was found
            If Not pAuthenticationCharacteristic Is Nothing Then
                ' Set the state to indicate authentication is in progress
                pConnectionState = ConnectionStates.Authenticating
                Dim ByteWriter As New DataWriter
                ByteWriter.WriteBytes(UTF8.GetBytes(SerialNumber & "000000"))

                ' Send the serial number (plus six 0s) to the device 
                If Await pAuthenticationCharacteristic.WriteValueAsync(ByteWriter.DetachBuffer, GattWriteOption.WriteWithResponse) = GattCommunicationStatus.Success Then
                    ' Set a five second timeout for the device to respond correctly. If this
                    ' doesn't happen the serial number was probably wrong.
                    Dim Timeout As New TimeSpan(0, 0, AuthenticateTimeoutSeconds)
                    Dim StartTime As DateTime = DateTime.Now

                    ' Wait for the Heartbeat to happen on another thread or until
                    ' the timeout expires
                    While (DateTime.Now < (StartTime + Timeout))
                        Await Task.Yield()
                        Await Task.Delay(ReceiveDelayMilliseconds)
                        If pConnectionState = ConnectionStates.Connected Then
                            ' If this connection state was set in OnHeartbeat that means the
                            ' serial number was correct and the device is authenticated
                            Return True
                        End If
                    End While
                End If
            End If
        End If

        ' Last ditch effort to check and see if a connection was authenticated
        If pConnectionState = ConnectionStates.Connected Then
            ' If this connection state was set in OnHeartbeat that means the
            ' serial number was correct and the device is authenticated
            Return True
        End If

        Return False
    End Function

    ''' <summary>
    ''' Attempts to recreate a previous connection. If a connection has been previously established and is
    ''' within the last communication timeout period the function will return True but won't actually 
    ''' attempt to connect to the device.
    ''' </summary>
    ''' <returns>True if the connection was reestablished, False otherwise.</returns>
    Friend Overrides Async Function Connect() As Task(Of Boolean)
        ' Don't attempt to recreate the connection unless it's after the communication timeout.
        If pConnectionState = ConnectionStates.Connected Then
            If pLastCommunicationTime.Add(New TimeSpan(0, 0, LastCommunicationTimeoutSeconds)) > DateTime.Now Then
                Return True
            Else
                ' Clean up the Bluetooth variables before attempting to reestablish the connection.
                Call DisposeConnection()
            End If
        End If

        ' When connecting over BLE the serial number must be known in advance
        If SerialNumber = "" Then Return False

        ' Rebuild the DeviceConnection structure
        If pDeviceID <> "" Then
            Dim Connection As New DeviceConnection
            Connection.DeviceId = pDeviceID
            Connection.DisplayName = pDisplayName
            Connection.InterfaceName = pInterfaceName

            Return Await Connect(Connection)
        End If

        Return False
    End Function

    ''' <summary>
    ''' Due to the wireless nature of the interface connections aren't disconnected. Connections are
    ''' only actually disconnected once the communication timeout period is exceeded.
    ''' </summary>
    ''' <returns>True always.</returns>
    Friend Overrides Async Function Disconnect() As Task(Of Boolean)
        Await Task.Yield()
        Return True
    End Function

    ''' <summary>
    ''' Destroys and disposes of all the Bluetooth-related variables and classes.
    ''' </summary>
    Private Sub DisposeConnection()
        ' Mark the connection as closed
        pConnectionState = ConnectionStates.NotConnected

        ' Remove event handler for data received from the device
        RemoveHandler pShareMessageResponseCharacteristic.ValueChanged, AddressOf OnShareMessageResponse
        pShareMessageReceiverCharacteristic = Nothing

        ' Remove the event handler for heartbeat data
        RemoveHandler pHeartbeatCharacteristic.ValueChanged, AddressOf OnHeartbeat
        pHeartbeatCharacteristic = Nothing

        ' Destroy other characteristics
        pShareMessageResponseCharacteristic = Nothing
        pCommandCharacteristic = Nothing
        pResponseCharacteristic = Nothing
        pAuthenticationCharacteristic = Nothing

        ' Dispose and destroy the device and services
        pCradleDeviceService.Device.Dispose()
        pCradleDeviceService.Dispose()
        pCradleDeviceService = Nothing
    End Sub

    ''' <summary>
    ''' Attempts to retrieve any available bytes from the device.
    ''' </summary>
    ''' <returns>An array of bytes retrieved from the device, Nothing otherwise</returns>
    Friend Overrides Async Function ReceivePacketBytes() As Task(Of Byte())
        'Dim DataPayloads As New Collection(Of Byte())

        ' Set a five second timeout for bytes coming in
        Dim Timeout As New TimeSpan(0, 0, ReceiveTimeout)

        ' Keep track of how many bytes have been delivered
        Dim TotalByteCount As Integer = 0

        ' Bytes are received in 20 byte increments from another thread. Go
        ' through the timing loop at least once to get the initial bytes
        ' and continue through as the bytes pour in during the time less
        ' than the timeout. When the two CRC16 bytes at the end of the packet
        ' are whats calculated over the packet then the receive event is done.
        Dim ReceivedBytes As Boolean = False
        Dim FinalDataPayload(0) As Byte
        Do
            ' Wait for bytes to come in or for the timeout to expire
            ReceivedBytes = False
            Dim StartTime As DateTime = DateTime.Now
            While pReceivedBytes.Count = 0 And (DateTime.Now < (StartTime + Timeout))
                Await Task.Delay(ReceiveDelayMilliseconds)
                Await Task.Yield()
            End While

            ' If a payload came in add it to the local collection and remove it
            ' from the collection of data waiting to be processed.
            While pReceivedBytes.Count > 0
                ' Extend the length of the buffer and copy new packet payload data
                ReDim Preserve FinalDataPayload(TotalByteCount + pReceivedBytes(0).Count - 1)
                Call Array.Copy(pReceivedBytes(0), 0, FinalDataPayload, TotalByteCount, pReceivedBytes(0).Count)

                ' Increase the total size of the packet and remove the bytes just received
                TotalByteCount += pReceivedBytes(0).Count
                Call pReceivedBytes.RemoveAt(0)

                ' Indicate that bytes came in during this loop so
                ' run the loop again looking for more data.
                ReceivedBytes = True
                Await Task.Yield()
            End While
        Loop While ReceivedBytes And (Not Packet.IsPacketComplete(FinalDataPayload))

        ' Take whatever is in the member variable set (or not) by OnShareMessageResponse
        ' and return it to the calling function, clearing the received data. If the timeout
        ' expired before any data was retrieved this function will return Nothing.
        Return FinalDataPayload
    End Function

    ''' <summary>
    ''' Sends the provided array of bytes to the device.
    ''' </summary>
    ''' <param name="BytesToSend">An array of bytes to send to the device</param>
    ''' <returns>True if the data was successfully sent to the device, False otherwise</returns>
    Friend Overrides Async Function SendPacketBytes(BytesToSend() As Byte) As Task(Of Boolean)
        ' Thing you write to: ShareMessageReceiver

        ' Verify a device connection has been made
        If pShareMessageReceiverCharacteristic Is Nothing Then Return False

        ' Make sure that the first two bytes 0x01 are to satisfy the BLE protocol. Otherwise
        ' the packets are the same as if it were a USB interface. Use +1 here because 
        ' the length itself will already create an extra byte
        Dim BytesForBLE(BytesToSend.Length + 1) As Byte
        BytesForBLE(0) = 1
        BytesForBLE(1) = 1
        Array.Copy(BytesToSend, 0, BytesForBLE, 2, BytesToSend.Length)

        ' Create a DataWriter and attach it to the device OutputStream, write
        ' the passed bytes
        Dim DataWriter As New DataWriter()
        Call DataWriter.WriteBytes(BytesForBLE)

        Try
            ' Send the bytes to the device
            If Await pShareMessageReceiverCharacteristic.WriteValueAsync(DataWriter.DetachBuffer, GattWriteOption.WriteWithResponse) = GattCommunicationStatus.Success Then
                ' Indicate to the destination device that we're done sending data
                If Await pShareMessageResponseCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Indicate) Then
                    Return True
                End If
            End If
        Catch ex As Exception
            Return False
        End Try

        Return True
    End Function

    ''' <summary>
    ''' A callback when the Heartbeat characteristic has data to send from the device.
    ''' </summary>
    ''' <param name="sender">Characteristic with data to send</param>
    ''' <param name="e">Parameters that indicate success/failure and applicable data</param>
    Private Async Sub OnHeartbeat(sender As GattCharacteristic, e As GattValueChangedEventArgs)
        ' Reset the notification state for the Heartbeat characteristic
        Await pHeartbeatCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify)

        ' Get data transferred from the device. It should always be &HBB. If we're in the authentication
        ' process this value indicates that the device has been authenticated and the connection is finalized.
        Dim BuffBytes() As Byte = e.CharacteristicValue.ToArray
        If BuffBytes(0) = &HBB Then
            ' Authentication complete
            pLastCommunicationTime = DateTime.Now
            pConnectionState = ConnectionStates.Connected
        End If
    End Sub

    ''' <summary>
    ''' A callback that transfers data from the device when a command is sent to the device.
    ''' </summary>
    ''' <param name="sender">The characteristic with data to transfer</param>
    ''' <param name="e">Values that indicate the success/failure of the transfer and the data involved</param>
    Private Async Sub OnShareMessageResponse(sender As GattCharacteristic, e As GattValueChangedEventArgs)
        ' Reset the notification status of the characteristic
        Await pResponseCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify)

        ' When data is received copy it into a member variable so that the next call to ReceivePacketBytes
        ' will retrieve the bytes from the member variable and reset it to Nothing. When the caller calls
        ' ReceivePacketBytes and it is Nothing it will wait up to five seconds
        Call pReceivedBytes.Add(e.CharacteristicValue.ToArray)
        pLastCommunicationTime = DateTime.Now
    End Sub

    ''' <summary>
    ''' Searches for a GattCharacteristic defined by the passed GUID in the GattDeviceService.
    ''' </summary>
    ''' <param name="GattDeviceService">The GattDeviceService with the GattCharacteristic being searched for</param>
    ''' <param name="GUID">The GUID of the GattCharacteristic being searched for</param>
    ''' <param name="GattCharacteristic">The GattCharacteristic that was located, if it was located.</param>
    ''' <returns>True indicates GattCharacteristic has the characteristic being searched for, False indicates failure.</returns>
    Private Function GetCharacteristic(ByRef GattDeviceService As GattDeviceService, ByVal GUID As String, ByRef GattCharacteristic As GattCharacteristic) As Boolean
        Dim List As IReadOnlyList(Of GattCharacteristic) = GattDeviceService.GetCharacteristics(New Guid(GUID))

        ' Look through any applicable characteristics, returning the first one found
        For Each Characteristic As GattCharacteristic In List
            GattCharacteristic = Characteristic
            Return True
        Next

        Return False
    End Function
End Class
