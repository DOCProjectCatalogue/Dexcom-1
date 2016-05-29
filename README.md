This library is used to pull data from Dexcom G4 Receivers on the Universal Windows Platform. Dexcom G5 Receivers are unsupported only because I don't have an exemplar device to experiment with. The library can be compiled for inclusion in any UWP app for any platform, including apps written in C#. Once the device is connected to the PC via USB the following code snippet will open the device and query data:

```
Private Sub ConnectToDevice()
    Dim USBInterface As New USBInterface
    Dim DevicesFound As Collection(Of DeviceInterface.DeviceConnection) = Await USBInterface.GetAvailableDevices()

    ' Connect to the device
    If DevicesFound.Count > 1 Then
        Dim Receiver As New Dexcom.Receiver(USBInterface)
        If Await Receiver.ConnectToReceiver(DevicesFound(0)) Then
            ' Get the Transmitter ID
            Dim TransmitterID As String = Receiver.TransmitterID

            ' Get EGV records from the device database
            Dim DeviceEGVRecords As Collection(Of DatabaseRecord) = Await Receiver.GetDatabaseContents(DatabasePartitions.EGVData)


            ' Disconnect from the device
            Await Receiver.Disconnect()       
        Else
            ' Fail out if an error occurs
            Return
        End If
    End If

    Return
End Sub
```

Apps that use the library will need to include the `bluetooth` and/or `serialcommunication` capabilities to successfully connect to the device using either interface:

```
<DeviceCapability Name="bluetooth" />
<DeviceCapability Name="serialcommunication">
  <Device Id="any">
    <Function Type="name:serialPort" />
  </Device>
</DeviceCapability>
```