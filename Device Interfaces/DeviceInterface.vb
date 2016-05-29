''' <summary>
''' Author: Jay Lagorio
''' Date: May 29, 2016
''' Summary: Describes and interface used to connect to a Dexcom Receiver. The interface can be wired or
''' wireless but must allow the Receiver class to detect, connect, and exchange data with the device in a
''' way that is connection agnostic.
''' </summary>

Public MustInherit Class DeviceInterface

    ' The name of the interface the class uses to connect to the device. This
    ' name must be set in Shared Sub New by the derivative class.
    Friend pInterfaceName As String

    ' The friendly name of the device
    Friend pDisplayName As String

    ' The Device ID assigned by Windows used to open a handle to the device
    Friend pDeviceID As String

    ''' <summary>
    ''' Describes a device connection possible 
    ''' </summary>
    Public Structure DeviceConnection
        Dim DisplayName As String       ' The friendly name of the device assigned by Windows
        Dim DeviceId As String          ' The PnP device ID used to open a handle to the device
        Dim InterfaceName As String     ' The interface used to connect to the device (assigned by the derivative class)
    End Structure

    ''' <summary>
    ''' Returns the name of the interface type used to connect to the device.
    ''' </summary>
    ''' <returns>A string with the interface name connecting to the device</returns>
    Public ReadOnly Property InterfaceName As String
        Get
            Return pInterfaceName
        End Get
    End Property

    ''' <summary>
    ''' Returns a Collection of Dexcom Receiver devices connected to the system using interfaces derived from this class.
    ''' </summary>
    ''' <returns>A Collection of devices to connect to using an interface derived from this class</returns>
    Public MustOverride Async Function GetAvailableDevices() As Task(Of Collection(Of DeviceConnection))

    ''' <summary>
    ''' Returns a Collection of Dexcom Receiver devices connected to the system using interfaces derived from this class.
    ''' </summary>
    ''' <param name="AvailableDevices">A Collection of DeviceConnection structures to be supplemented</param>
    ''' <returns>A Collection of devices available for connection, including DeviceConnection passed to the function</returns>
    Public MustOverride Async Function GetAvailableDevices(ByVal AvailableDevices As Collection(Of DeviceConnection)) As Task(Of Collection(Of DeviceConnection))

    ''' <summary>
    ''' Attempts to connect to the passed device over the interface represented by the derivative class.
    ''' </summary>
    ''' <param name="AvailableConnection">An AvailableConnection structure representing the device to connect to</param>
    ''' <returns>True if the device is connected, False otherwise</returns>
    Friend MustOverride Async Function Connect(ByVal AvailableConnection As DeviceConnection) As Task(Of Boolean)

    ''' <summary>
    ''' Attempts to connect to a device using previously established connection data.
    ''' </summary>
    ''' <returns>True if the connection is reestablished, False otherwise</returns>
    Friend MustOverride Async Function Connect() As Task(Of Boolean)

    ''' <summary>
    ''' Disconnects the underlying device interface.
    ''' </summary>
    ''' <returns>True if the device is disconnected, False otherwise</returns>
    Friend MustOverride Async Function Disconnect() As Task(Of Boolean)

    ''' <summary>
    ''' Sends the provided array of bytes to the device.
    ''' </summary>
    ''' <param name="BytesToSend">An array of bytes to send to the device</param>
    ''' <returns>True if the data was successfully sent to the device, False otherwise</returns>
    Friend MustOverride Async Function SendPacketBytes(ByVal BytesToSend() As Byte) As Task(Of Boolean)

    ''' <summary>
    ''' Attempts to retrieve any available bytes from the device.
    ''' </summary>
    ''' <returns>An array of bytes retrieved from the device, Nothing otherwise</returns>
    Friend MustOverride Async Function ReceivePacketBytes() As Task(Of Byte())

    ''' <summary>
    ''' Returns the string provided by Windows that opens a handle to the device.
    ''' </summary>
    ''' <returns>A String Windows uses to open a connection to the device</returns>
    Public MustOverride ReadOnly Property DeviceID As String

    ''' <summary>
    ''' Returns the friendly name of the device provided by Windows.
    ''' </summary>
    ''' <returns>A string suitable for display in a user interface</returns>
    Public MustOverride ReadOnly Property DisplayName As String
End Class
