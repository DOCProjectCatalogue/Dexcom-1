Imports System.Xml.Serialization

''' <summary>
''' Author: Jay Lagorio
''' Date: June 12, 2016
''' Summary: Helper class to deserializes XML records from the FirmwareHeader database partition.
''' </summary>

<DataContract> Public Class FirmwareHeader
    ''' <summary>
    ''' The version of the database schema the device uses.
    ''' </summary>
    ''' <returns>A string representing the SchemaVersion property from the device</returns>
    <DataMember, XmlAttribute> Public Property SchemaVersion As String

    ''' <summary>
    ''' The version of the API exposed by the device.
    ''' </summary>
    ''' <returns>A string representing the ApiVersion property from the device</returns>
    <DataMember, XmlAttribute> Public Property ApiVersion As String

    ''' <summary>
    ''' The API version of the test API.
    ''' </summary>
    ''' <returns>A string representing the TestApiVersion property from the device</returns>
    <DataMember, XmlAttribute> Public Property TestApiVersion As String

    ''' <summary>
    ''' A short string identifying the receiver product type
    ''' </summary>
    ''' <returns></returns>
    <DataMember, XmlAttribute> Public Property ProductId As String

    ''' <summary>
    ''' A user-friendly string describing the receiver product type
    ''' </summary>
    ''' <returns></returns>
    <DataMember, XmlAttribute> Public Property ProductName As String

    ''' <summary>
    ''' The number of the software published on the device.
    ''' </summary>
    ''' <returns>Returns a product code identifying the loaded software</returns>
    <DataMember, XmlAttribute> Public Property SoftwareNumber As String

    ''' <summary>
    ''' The overall version of Firmware loaded onto the device.
    ''' </summary>
    ''' <returns>A version string representing the overall firmware version</returns>
    <DataMember, XmlAttribute> Public Property FirmwareVersion As String

    ''' <summary>
    ''' The version of the Port software in this firmware version.
    ''' </summary>
    ''' <returns>A version string representing the port version</returns>
    <DataMember, XmlAttribute> Public Property PortVersion As String

    ''' <summary>
    ''' The version of the RF software in this firmware version.
    ''' </summary>
    ''' <returns>A version string representing the RF version</returns>
    <DataMember, XmlAttribute> Public Property RFVersion As String

    ''' <summary>
    ''' The revision number of the DexBoot software in this firmware version.
    ''' </summary>
    ''' <returns>An String containing an integer representing the revision of DexBoot</returns>
    <DataMember, XmlAttribute> Public Property DexBootVersion As String

    ''' <summary>
    ''' The version of the BLE software in this firmware version.
    ''' </summary>
    ''' <returns>A version string representing the BLE version</returns>
    <DataMember, XmlAttribute> Public Property BLEVersion As String

    ''' <summary>
    ''' The version of the BLE Soft Device software in this firmware version.
    ''' </summary>
    ''' <returns>A version string representing the BLE Soft Device version</returns>
    <DataMember, XmlAttribute> Public Property BLESoftDeviceVersion As String
End Class
