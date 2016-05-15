Imports System.Xml.Serialization

''' <summary>
''' Author: Jay Lagorio
''' Date: May 15, 2016
''' Summary: Helper class to deserializes XML records from the ManufacturingData database partition.
''' </summary>

<DataContract> Public Class ManufacturingParameters
    ''' <summary>
    ''' The device serial number.
    ''' </summary>
    ''' <returns>String representing the SerialNumber field (e.g. SM123456789)</returns>
    <DataMember, XmlAttribute> Public Property SerialNumber As String

    ''' <summary>
    ''' The device hardware part number.
    ''' </summary>
    ''' <returns>String representing the HardwarePartNumber field</returns>
    <DataMember, XmlAttribute> Public Property HardwarePartNumber As String

    ''' <summary>
    ''' The device's hardware revision number.
    ''' </summary>
    ''' <returns>A string representing the hardware revision (e.g. 10, 11, etc)</returns>
    <DataMember, XmlAttribute> Public Property HardwareRevision As String

    ''' <summary>
    ''' The date and time the device was created at the factory.
    ''' </summary>
    ''' <returns>A date in string format.</returns>
    <DataMember, XmlAttribute> Public Property DateTimeCreated As String

    ''' <summary>
    ''' The unique hardware ID of the device.
    ''' </summary>
    ''' <returns>A string containing a GUID uniquely representing the device.</returns>
    <DataMember, XmlAttribute> Public Property HardwareId As String
End Class
