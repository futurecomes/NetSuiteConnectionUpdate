Imports NetsuiteLibrary

Public Class HomeController
    Inherits System.Web.Mvc.Controller

    Public Property oNetsuite As NetsuiteLibrary.Netsuite

    Function Index() As ActionResult
        oNetsuite = New Netsuite
        oNetsuite.accountId = "4082411_SB1"
        oNetsuite.Initialize()
        oNetsuite.customerKey = "1779cf7fb11de4a656b347aae0b268e600ef7538e6a536168636a080d01405bd"
        oNetsuite.customerSecret = "6b3fee1c7498c46e7efb2417f220d6be20d6cfc53e6364dab8188265ed63a8d2"
        oNetsuite.tokenId = "61e56e51a8667140efa06a69ff19bbd94101f0e016bd1ee088683481bbcb6fbe"
        oNetsuite.tokenSecret = "2dea68eb52e37d38986203ab4d88f92a077794fc0c43907c670b1a00189e1e91"
        oNetsuite.LoginWithToken()
        Dim internalId As String
        internalId = New String("")
        Dim location As String
        location = New String("")
        Dim lineuniquekey As String
        lineuniquekey = New String("")


        Dim productEstimateEndDate As String
        productEstimateEndDate = New String("")
        If Request.QueryString("internalId") IsNot Nothing Then
            internalId = Request.QueryString("internalId")
        End If
        If Request.QueryString("location") IsNot Nothing Then
            location = Request.QueryString("location")
        End If
        If Request.QueryString("lineuniquekey") IsNot Nothing Then
            lineuniquekey = Request.QueryString("lineuniquekey")
        End If
        If Request.QueryString("date") IsNot Nothing Then
            productEstimateEndDate = Request.QueryString("date")
        End If
        If Not String.IsNullOrEmpty(internalId) And Not String.IsNullOrEmpty(location) And Not String.IsNullOrEmpty(productEstimateEndDate) Then
            Dim result As String = NewMethod(internalId, location, lineuniquekey, productEstimateEndDate)
            ViewData("Message") = result
        End If
        'Dim polist As String() = {"86800"}
        'Dim itemlist As String() = {"320571"}
        'Dim inboundId = oNetsuite.CreateInboundShipment(polist, itemlist)
        'ViewData("inboundId") = inboundId
        'oNetsuite.CreateInboundShipmentFromVendor("Titus United States Co. Ltd.")
        Return View()

        Dim MemoFromVendor As String
        MemoFromVendor = New String("")
        If Request.QueryString("internalId") IsNot Nothing Then
            internalId = Request.QueryString("internalId")
        End If
        If Request.QueryString("location") IsNot Nothing Then
            location = Request.QueryString("location")
        End If
        If Request.QueryString("lineuniquekey") IsNot Nothing Then
            lineuniquekey = Request.QueryString("lineuniquekey")
        End If
        If Request.QueryString("data") IsNot Nothing Then
            MemoFromVendor = Request.QueryString("data")
        End If
        If Not String.IsNullOrEmpty(internalId) And Not String.IsNullOrEmpty(MemoFromVendor) Then
            Dim result As String = NewMethod1(internalId, location, lineuniquekey, MemoFromVendor)
            ViewData("Message") = result
        End If
        Return View()
    End Function

    Private Function NewMethod(internalId As String, location As String, lineuniquekey As String, productEstimateEndDate As String) As String
        Return oNetsuite.UpdateProductEstimateEndDate(internalId, location, lineuniquekey, productEstimateEndDate)
    End Function

    Private Function NewMethod1(internalId As String, location As String, lineuniquekey As String, MemoFromVendor As String) As String
        Return oNetsuite.UpdateMemoFromVendor(internalId, location, lineuniquekey, MemoFromVendor)
    End Function

    Function About() As ActionResult
        ViewData("Message") = "Your application description page."

        Return View()
    End Function

    Function Contact() As ActionResult
        ViewData("Message") = "Your contact page."

        Return View()
    End Function
End Class
