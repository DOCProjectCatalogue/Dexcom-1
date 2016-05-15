''' <summary>
''' Author: Jay Lagorio
''' Date: May 15, 2016
''' Summary: Maps database partition names and ID numbers.
''' </summary>

Public Class DatabasePartitions

    ''' <summary>
    ''' Each of these constants represents a database name in the Dexcom Receiver. These are used to get the ID
    ''' numbers for each database to query.
    ''' </summary>
    Public Const ManufacturingData As String = "ManufacturingData"
    Public Const FirmwareParameterData As String = "FirmwareParameterData"
    Public Const PCSoftwareParameter As String = "PCSoftwareParameter"
    Public Const SensorData As String = "SensorData"
    Public Const EGVData As String = "EGVData"
    Public Const CalSet As String = "CalSet"
    Public Const Aberration As String = "Aberration"
    Public Const InsertionTime As String = "InsertionTime"
    Public Const ReceiverLogData As String = "ReceiverLogData"
    Public Const ReceiverErrorData As String = "ReceiverErrorData"
    Public Const MeterData As String = "MeterData"
    Public Const UserEventData As String = "UserEventData"
    Public Const UserSettingData As String = "UserSettingData"

    ' Simple name and ID pair
    Private Structure DatabasePartitionData
        Dim ID As Integer
        Dim Name As String

        Sub New(ByVal ID As Integer, ByVal Name As String)
            Me.ID = ID
            Me.Name = Name
        End Sub
    End Structure

    Private Shared pPartitions As New Collection(Of DatabasePartitionData)

    ''' <summary>
    ''' Sets up the name/ID pairs in the pPartitions() collection
    ''' </summary>
    Shared Sub New()
        ' Populate the partition index
        Call pPartitions.Add(New DatabasePartitionData(0, "ManufacturingData"))
        Call pPartitions.Add(New DatabasePartitionData(1, "FirmwareParameterData"))
        Call pPartitions.Add(New DatabasePartitionData(2, "PCSoftwareParameter"))
        Call pPartitions.Add(New DatabasePartitionData(3, "SensorData"))
        Call pPartitions.Add(New DatabasePartitionData(4, "EGVData"))
        Call pPartitions.Add(New DatabasePartitionData(5, "CalSet"))
        Call pPartitions.Add(New DatabasePartitionData(6, "Aberration"))
        Call pPartitions.Add(New DatabasePartitionData(7, "InsertionTime"))
        Call pPartitions.Add(New DatabasePartitionData(8, "ReceiverLogData"))
        Call pPartitions.Add(New DatabasePartitionData(9, "ReceiverErrorData"))
        Call pPartitions.Add(New DatabasePartitionData(10, "MeterData"))
        Call pPartitions.Add(New DatabasePartitionData(11, "UserEventData"))
        Call pPartitions.Add(New DatabasePartitionData(12, "UserSettingData"))
    End Sub

    ''' <summary>
    ''' When provided a database name this function returns the ID number in the device.
    ''' </summary>
    ''' <param name="PartitionName">The name of the database to find</param>
    ''' <returns>Returns the ID number of the database or -1 if the database isn't found</returns>
    Public Shared Function GetID(ByVal PartitionName As String) As Integer
        For i = 0 To pPartitions.Count - 1
            If pPartitions(i).Name = PartitionName Then
                Return pPartitions(i).ID
            End If
        Next

        Return -1
    End Function

    ''' <summary>
    ''' When provided the ID number of a database it returns the database name.
    ''' </summary>
    ''' <param name="PartitionID">The ID of the database to search for</param>
    ''' <returns>Returns the name of the database or an empty String</returns>
    Public Shared Function GetName(ByVal PartitionID As Integer) As String
        For i = 0 To pPartitions.Count - 1
            If pPartitions(i).ID = PartitionID Then
                Return pPartitions(i).Name
            End If
        Next

        Return ""
    End Function
End Class
