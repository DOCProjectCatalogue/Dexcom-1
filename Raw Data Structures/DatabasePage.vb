Imports System.Text.UTF8Encoding

''' <summary>
''' Author: Jay Lagorio
''' Date: May 15, 2016
''' Summary: Describes a raw database page retrieved from the device, parses headers and separates attached payloads.
''' </summary>

Public Class DatabasePage

    ''' <summary>
    ''' The type of record found on this database page. All data is in a proprietary format unless
    ''' otherwise noted.
    ''' </summary>
    Public Enum RecordType
        ManufacturingData = 0       ' Note: XML formatted text
        FirmwareParameterData = 1   ' Note: XML formatted text
        PCSoftwareParameterData = 2 ' Note: XML formatted text
        SensorData = 3              ' Data associated with a sensor record, can be combined with other recotds
        EGVData = 4                 ' Trend and glucose level data
        CalSetData = 5
        DeviationData = 6
        InsertionTime = 7           ' Insertion event data
        ReceiverLogData = 8
        ReceiverErrorData = 9
        MeterData = 10
        UserEventData = 11
        UserSettingData = 12
        MaxValue = 13               ' One greater than the highest value of database type defined
    End Enum

    ' The total length of the database page header. All bytes after this offset are page data.
    Private Const DatabasePageHeaderLength As Integer = 28

    ' In database records that consist of XML data the data starts after the first 8 bytes.
    Private Const XMLDataStartOffset As Integer = 8

    ' Unknown
    Private pIndex As UInteger

    ' The number of database records contained on this database page
    Private pNumberOfRecords As UInteger

    ' The type of database entry stored on this page. Must be a
    ' value from the RecordType Enum.
    Private pRecordType As Byte

    ' The data format revision number
    Private pRevision As Byte

    ' The page number out of the total number of database pages
    Private pPageNumber As UInteger

    ' Unknown
    Private pR1 As UInteger

    ' Unknown
    Private pR2 As UInteger

    ' Unknown
    Private pR3 As UInteger

    ' The CRC of the database page header
    Private pCRC As UShort

    ' The total bytes allocated for the database page content
    Private pPayload() As Byte

    ' Data offsets
    Private Const IndexOffset As Integer = 0
    Private Const NumberOfRecordsOffset As Integer = 4
    Private Const RecordTypeOffset As Integer = 8
    Private Const RevisionOffset As Integer = 9
    Private Const PageNumberOffset As Integer = 10
    Private Const R1Offset As Integer = 14
    Private Const R2Offset As Integer = 18
    Private Const R3Offset As Integer = 22
    Private Const CRCOffset As Integer = 26

    ''' <summary>
    ''' Creates a DatabasePage from the raw bytes representing the page as
    ''' received from the Dexcom Receiver. If PageBytes is Nothing or less
    ''' than the size of the DatabasePage header a FormatException is thrown.
    ''' </summary>
    ''' <param name="PageBytes">An array of bytes received from a Dexcom Receiver representing the DatabasePage</param>
    Sub New(ByRef PageBytes() As Byte)
        If PageBytes Is Nothing Then
            Throw New FormatException
        End If

        If PageBytes.Length < DatabasePageHeaderLength Then
            Throw New FormatException
        End If

        ' Populate the header fields, which consist of the first 28 bytes
        pIndex = BitConverter.ToUInt32(PageBytes, IndexOffset)
        pNumberOfRecords = BitConverter.ToUInt32(PageBytes, NumberOfRecordsOffset)
        pRecordType = PageBytes(RecordTypeOffset)
        pRevision = PageBytes(RevisionOffset)
        pPageNumber = BitConverter.ToUInt32(PageBytes, PageNumberOffset)
        pR1 = BitConverter.ToUInt32(PageBytes, R1Offset)
        pR2 = BitConverter.ToUInt32(PageBytes, R2Offset)
        pR3 = BitConverter.ToUInt32(PageBytes, R3Offset)
        pCRC = BitConverter.ToUInt16(PageBytes, CRCOffset)

        ' Check to see if there's data on the page after the header and
        ' copy the data to pPayload if so.
        If PageBytes.Length - DatabasePageHeaderLength > 0 Then
            ReDim pPayload(PageBytes.Length - DatabasePageHeaderLength - 1)
            Array.Copy(PageBytes, DatabasePageHeaderLength, pPayload, 0, pPayload.Length)
        End If
    End Sub

    ''' <summary>
    ''' Returns the type of content in the database page.
    ''' </summary>
    ''' <returns>A value from the RecordType Enum indicating the type of data stored in the page</returns>
    Public ReadOnly Property DataRecordType As RecordType
        Get
            Return pRecordType
        End Get
    End Property

    ''' <summary>
    ''' A byte array containing the raw page data after the headers.
    ''' </summary>
    ''' <returns>An array of bytes with raw page data</returns>
    Public ReadOnly Property Payload() As Byte()
        Get
            Return pPayload
        End Get
    End Property

    ''' <summary>
    ''' Returns the number of records contained in the database page.
    ''' </summary>
    ''' <returns>An Integer with the number of individual records in the page</returns>
    Public ReadOnly Property NumberOfRecords As Integer
        Get
            Return pNumberOfRecords
        End Get
    End Property

    ''' <summary>
    ''' Returns the contents of a database page as UTF-8 text. Among other database partitions 
    ''' this function is used for ManufacturingData information.
    ''' </summary>
    ''' <returns>A String containing XML data stored in the database page</returns>
    Public Function GetPageXMLContent() As String
        ' Manufacturing and Firmware XML text starts after 8 bytes. Find the NULL and
        ' cut the string to return to the caller.
        Dim NullPosition As Integer = Array.IndexOf(pPayload, CByte(0))
        Return UTF8.GetString(pPayload, XMLDataStartOffset, NullPosition - XMLDataStartOffset)
    End Function
End Class
