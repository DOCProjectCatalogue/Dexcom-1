Imports Windows.Storage.Streams

''' <summary>
''' Author: Jay Lagorio
''' Date: May 22, 2016
''' Summary: Detects, connects, and exchanges data with a Dexcom Receiver using Serial-over-USB.
''' </summary>

Public Class USBInterface
    Inherits DeviceInterface

    Private pDevice As SerialDevice

    ' Vendor and Product IDs that identify the device
    Private Const DexcomUSBVendorID As UShort = &H22A3
    Private Const DexcomUSBProductID As UShort = &H47
    Private Const ReceiveTimeoutMS As Integer = 1000

    ''' <summary>
    ''' Sets the interface name for later use
    ''' </summary>
    Sub New()
        pInterfaceName = "USB"
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
    ''' Returns a Collection of Dexcom Receiver devices connected to the system using interfaces derived from this class.
    ''' </summary>
    ''' <returns>A Collection of devices to connect to using an interface derived from this class</returns>
    Public Overrides Async Function GetAvailableDevices() As Task(Of Collection(Of DeviceConnection))
        Dim Results As New Collection(Of DeviceConnection)

        ' Get a selector specifying the Dexcom VID/PID and search for matching devices connected to the system
        Dim DexcomUSBVIDPID As String = SerialDevice.GetDeviceSelectorFromUsbVidPid(DexcomUSBVendorID, DexcomUSBProductID)
        Dim DevInfoCollection As DeviceInformationCollection = Await DeviceInformation.FindAllAsync(DexcomUSBVIDPID)
        Dim DeviceInfo As DeviceInformation = Nothing

        For Each Device As DeviceInformation In DevInfoCollection
            ' For each matching device return a structure describing that device
            Dim Connection As New DeviceConnection
            Connection.DisplayName = Device.Name
            Connection.DeviceId = Device.Id
            Connection.InterfaceName = pInterfaceName
            Call Results.Add(Connection)
        Next

        Return Results
    End Function

    ''' <summary>
    ''' Returns a Collection of Dexcom Receiver devices connected to the system using interfaces derived from this class.
    ''' </summary>
    ''' <param name="AvailableDevices">A Collection of DeviceConnection structures to be supplemented</param>
    ''' <returns>A Collection of devices available for connection, including DeviceConnection passed to the function</returns>
    Public Overrides Async Function GetAvailableDevices(ByVal AvailableDevices As Collection(Of DeviceConnection)) As Task(Of Collection(Of DeviceConnection))
        ' If the Collection passed wasn't initialized create a new instance
        If AvailableDevices Is Nothing Then
            AvailableDevices = New Collection(Of DeviceConnection)
        End If

        ' Get a selector specifying the Dexcom VID/PID and search for matching devices connected to the system
        Dim DexcomUSBVIDPID As String = SerialDevice.GetDeviceSelectorFromUsbVidPid(DexcomUSBVendorID, DexcomUSBProductID)
        Dim DevInfoCollection As DeviceInformationCollection = Await DeviceInformation.FindAllAsync(DexcomUSBVIDPID)
        Dim DeviceInfo As DeviceInformation = Nothing

        For Each Device As DeviceInformation In DevInfoCollection
            ' For each matching device return a structure describing that device. Devices
            ' found are added to the existing Collection of devices passed.
            Dim Connection As New DeviceConnection
            Connection.DisplayName = Device.Name
            Connection.DeviceId = Device.Id
            Connection.InterfaceName = pInterfaceName
            Call AvailableDevices.Add(Connection)
        Next

        Return AvailableDevices
    End Function

    ''' <summary>
    ''' Attempts to connect to the passed device over the interface represented by the derivative class.
    ''' </summary>
    ''' <param name="AvailableConnection">An AvailableConnection structure representing the device to connect to</param>
    ''' <returns>True if the device is connected, False otherwise</returns>
    Friend Overrides Async Function Connect(ByVal AvailableConnection As DeviceConnection) As Task(Of Boolean)
        ' If the calling function passes a connection with the wrong interface fail the call
        If AvailableConnection.InterfaceName <> pInterfaceName Then
            Return False
        End If

        Try
            ' Attempt to connect to the device and set some properties
            pDevice = Await SerialDevice.FromIdAsync(AvailableConnection.DeviceId)
            pDisplayName = AvailableConnection.DisplayName
            pDeviceID = AvailableConnection.DeviceId
        Catch Ex As Exception
            Return False
        End Try

        ' If the connection succeeds set the read timeout and return True
        If Not pDevice Is Nothing Then
            pDevice.ReadTimeout = TimeSpan.FromMilliseconds(ReceiveTimeoutMS)
            Return True
        End If

        Return False
    End Function

    ''' <summary>
    ''' Sends the provided array of bytes to the device.
    ''' </summary>
    ''' <param name="BytesToSend">An array of bytes to send to the device</param>
    ''' <returns>True if the data was successfully sent to the device, False otherwise</returns>
    Friend Overrides Async Function SendPacketBytes(ByVal BytesToSend() As Byte) As Task(Of Boolean)
        ' Verify a device connection has been made
        If pDevice Is Nothing Then Return False

        ' Create a DataWriter and attach it to the device OutputStream, write
        ' the passed bytes
        Dim DataWriter As New DataWriter(pDevice.OutputStream)
        Call DataWriter.WriteBytes(BytesToSend)

        ' Commit the bytes to the backing store
        Try
            Await DataWriter.StoreAsync()
        Catch ex As Exception
            Return False
        End Try

        Return True
    End Function

    ''' <summary>
    ''' Attempts to retrieve any available bytes from the device.
    ''' </summary>
    ''' <returns>An array of bytes retrieved from the device, Nothing otherwise</returns>
    Friend Overrides Async Function ReceivePacketBytes() As Task(Of Byte())
        ' Verify a device connection has been made
        If pDevice Is Nothing Then Return Nothing

        ' Create a byte array to hold the received data and use a DataReader
        ' to attempt to read the data
        Dim DataReader As New DataReader(pDevice.InputStream)
        DataReader.InputStreamOptions = InputStreamOptions.Partial

        ' Attempt to load the data, return Nothing if the attempt fails
        Dim ReceivedBytesCount As Integer
        Try
            ReceivedBytesCount = Await DataReader.LoadAsync(Packet.MaximumPacketLength)
        Catch Ex As Exception
            Return Nothing
        End Try

        ' Data was received, pull bytes out and return them
        Return DataReader.ReadBuffer(ReceivedBytesCount).ToArray()
    End Function

    ''' <summary>
    ''' Disconnect the underlying SerialPort interface.
    ''' </summary>
    ''' <returns>True always.</returns>
    Friend Overrides Async Function Disconnect() As Task(Of Boolean)
        Try
            Await Task.Yield

            ' For some reason this call occasionally crashes the app
            ' with a native exception that doesn't trigger the exception handler.
            Call pDevice.Dispose()
        Catch ex As Exception
            Return False
        End Try
        Return True
    End Function
End Class
