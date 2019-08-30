Imports System.Net
Imports System.Security.Cryptography
Imports System.Security.Cryptography.X509Certificates
Imports System.Text
Imports NetsuiteLibrary.com.netsuite.webservices

Public Class Netsuite

    Private Property service As NetSuiteService
    Public Property accountId As String
    Public Property customerKey As String
    Public Property customerSecret As String
    Public Property tokenId As String
    Public Property tokenSecret As String
    Private Property PageSize As Int32
    Private Property IsAuthenticated As Boolean
    Private Property TRANSACTION_TYPE As String = "_purchaseOrder"
    Public Property NSUtility


    Public Sub Netsuite()
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 
        ServicePointManager.ServerCertificateValidationCallback =
          Function(se As Object,
          cert As X509Certificate,
          chain As X509Chain,
          sslerror As Security.SslPolicyErrors) True
        PageSize = 100
        IsAuthenticated = False
    End Sub
    Public Sub LoginWithToken()
        service.tokenPassport = CreateTokenPassport()
    End Sub
    Public Sub Initialize()
        service = New DataCenterAwareNetSuiteService(accountId, False)
        service.Timeout = 1000 * 60 * 60 * 2
        Dim myUri As Uri = New Uri("https://webservices.netsuite.com")
        service.CookieContainer = New CookieContainer()
    End Sub
    Public Sub SetAccount(ByVal accountId As String)
        CType(service, DataCenterAwareNetSuiteService).SetAccount(accountId)
    End Sub

    Public Sub SetPreferences()
        ' Set up request level preferences as a SOAP header
        Dim Prefs = New Preferences
        service.preferences = Prefs
        Dim SearchPreferences = New SearchPreferences
        service.searchPreferences = SearchPreferences
        ' Preference to ask NS to treat all warnings as errors
        Prefs.warningAsErrorSpecified = True
        Prefs.warningAsError = False
        Prefs.ignoreReadOnlyFieldsSpecified = True
        Prefs.ignoreReadOnlyFields = True
        SearchPreferences.pageSize = PageSize
        SearchPreferences.pageSizeSpecified = True
        ' Setting this bodyFieldsOnly to true for faster search times on Opportunities
        SearchPreferences.bodyFieldsOnly = True
        LoginWithToken()

    End Sub
    Function CreateTokenPassport()
        Dim tokenpassport As TokenPassport
        Dim nonce = ComputeNonce()
        Dim timestamp As Long
        timestamp = ComputeTimestamp()
        Dim signature As TokenPassportSignature
        signature = ComputeSignature(accountId, customerKey, customerSecret, tokenId, tokenSecret, nonce, timestamp)
        tokenpassport = New TokenPassport
        tokenpassport.account = accountId
        tokenpassport.consumerKey = customerKey
        tokenpassport.token = tokenId
        tokenpassport.nonce = nonce
        tokenpassport.timestamp = timestamp
        tokenpassport.signature = signature
        Return tokenpassport
    End Function
    Function ComputeNonce()
        Dim rng As RNGCryptoServiceProvider
        rng = New RNGCryptoServiceProvider
        Dim data(20) As Byte
        rng.GetBytes(data)
        Dim value = Math.Abs(BitConverter.ToInt32(data, 0))
        Return value.ToString()
    End Function
    Public Function GetStatusDetails(ByVal status As Status) As String
        Dim sb As System.Text.StringBuilder = New System.Text.StringBuilder
        Dim i As Integer = 0
        Do While (i < status.statusDetail.Length)
            sb.Append("[Code=" + status.statusDetail(i).code.ToString + "] " + status.statusDetail(i).message.ToString + ("" & vbLf))
            i = (i + 1)
        Loop

        Return sb.ToString
    End Function
    Function ComputeTimestamp()
        Return ((DateTime.UtcNow.Subtract(New DateTime(1970, 1, 1))).TotalSeconds)
    End Function
    Function UpdateDeliveryDate(internalId, newdate)
        Dim po As PurchaseOrder
        po = New PurchaseOrder
        po.internalId = internalId

        Dim shipdate As Date
        shipdate = Date.ParseExact(newdate, "yyyyMMdd", Globalization.CultureInfo.InvariantCulture)
        po.shipDate = shipdate
        po.shipDateSpecified = True
        Dim writeres As WriteResponse
        SetPreferences()
        writeres = service.update(po)
        If (writeres.status.isSuccess) Then
            Return "Successfully updated"
        Else
            Return GetStatusDetails(writeres.status)
        End If
    End Function
    Function UpdateProductEstimateEndDate(internalId, location, lineuniquekey, newdate)
        Dim po As PurchaseOrder
        po = New PurchaseOrder
        po.internalId = internalId

        Dim curPo As RecordRef
        curPo = New RecordRef
        curPo.internalId = internalId
        curPo.type = RecordType.purchaseOrder
        curPo.typeSpecified = True
        Dim custcolProdenddateValue As Date
        custcolProdenddateValue = Date.ParseExact(newdate, "yyyyMMdd", Globalization.CultureInfo.InvariantCulture)
        SetPreferences()
        Dim response = service.get(curPo)
        If (response.status.isSuccess) Then
            Dim curPuchaseOrder As PurchaseOrder = CType(response.record, PurchaseOrder)
            Dim itemlist As PurchaseOrderItem() = curPuchaseOrder.itemList.item
            Dim newitemlist As PurchaseOrderItem()
            newitemlist = New PurchaseOrderItem(itemlist.Length - 1) {}

            Dim locationRec As RecordRef
            locationRec = New RecordRef
            locationRec.internalId = curPuchaseOrder.location.internalId
            locationRec.type = curPuchaseOrder.location.type
            locationRec.typeSpecified = True

            Dim i = 0

            For Each item As PurchaseOrderItem In itemlist
                Dim flag As Boolean = False
                Dim customFieldlist = item.customFieldList
                For Each customfield As CustomFieldRef In customFieldlist
                    If String.Compare(customfield.scriptId, "custcol_line_unique_key") = 0 Then
                        Dim value As Long = New Long
                        value = CType(customfield, LongCustomFieldRef).value

                        If value = CType(lineuniquekey, Long) Then
                            flag = True
                            Exit For
                        End If
                    End If
                Next
                If flag Then
                    Dim iteminternalId = item.item.internalId
                    Dim qty = item.quantity
                    Dim custitem As RecordRef = New RecordRef()
                    custitem.type = RecordType.inventoryItem
                    custitem.typeSpecified = True
                    custitem.internalId = iteminternalId
                    Dim lineItem As PurchaseOrderItem = New PurchaseOrderItem
                    lineItem.item = custitem
                    lineItem.quantity = qty
                    lineItem.quantitySpecified = True

                    Dim cfRefs As CustomFieldRef()
                    cfRefs = New CustomFieldRef(1) {}
                    Dim datefRef As DateCustomFieldRef
                    datefRef = New DateCustomFieldRef
                    datefRef.scriptId = "custcol_prodenddate"
                    datefRef.value = custcolProdenddateValue
                    Dim uniquekey As LongCustomFieldRef = New LongCustomFieldRef
                    uniquekey.scriptId = "custcol_line_unique_key"
                    uniquekey.value = CType(lineuniquekey, Long)
                    cfRefs(0) = CType(datefRef, CustomFieldRef)
                    cfRefs(1) = CType(uniquekey, CustomFieldRef)
                    lineItem.customFieldList = cfRefs

                    lineItem.location = locationRec

                    newitemlist(i) = lineItem
                End If

                i += 1
            Next
            Dim newPOItemList As PurchaseOrderItemList
            newPOItemList = New PurchaseOrderItemList
            newPOItemList.replaceAll = False
            newPOItemList.item = newitemlist
            po.itemList = newPOItemList
            SetPreferences()
            Dim writeres = service.update(po)
            If (writeres.status.isSuccess) Then
                Return "Successfully updated"
            Else
                Return GetStatusDetails(writeres.status)
            End If
        Else
            Return GetStatusDetails(response.status)
        End If

    End Function

    Function UpdateMemoFromVendor(internalId, location, lineuniquekey, newdata)
        Dim po As PurchaseOrder
        po = New PurchaseOrder
        po.internalId = internalId

        Dim curPo As RecordRef
        curPo = New RecordRef
        curPo.internalId = internalId
        curPo.type = RecordType.purchaseOrder
        curPo.typeSpecified = True
        SetPreferences()
        Dim response = service.get(curPo)
        If (response.status.isSuccess) Then
            Dim curPuchaseOrder As PurchaseOrder = CType(response.record, PurchaseOrder)
            Dim itemlist As PurchaseOrderItem() = curPuchaseOrder.itemList.item
            Dim newitemlist As PurchaseOrderItem()
            newitemlist = New PurchaseOrderItem(itemlist.Length - 1) {}

            Dim locationRec As RecordRef
            locationRec = New RecordRef
            locationRec.internalId = curPuchaseOrder.location.internalId
            locationRec.type = curPuchaseOrder.location.type
            locationRec.typeSpecified = True

            Dim i = 0

            For Each item As PurchaseOrderItem In itemlist
                Dim flag As Boolean = False
                Dim customFieldlist = item.customFieldList
                For Each customfield As CustomFieldRef In customFieldlist
                    If String.Compare(customfield.scriptId, "custcol_line_unique_key") = 0 Then
                        Dim value As Long = New Long
                        value = CType(customfield, LongCustomFieldRef).value

                        If value = CType(lineuniquekey, Long) Then
                            flag = True
                            Exit For
                        End If
                    End If
                Next
                If flag Then
                    Dim iteminternalId = item.item.internalId
                    Dim qty = item.quantity
                    Dim custitem As RecordRef = New RecordRef()
                    custitem.type = RecordType.inventoryItem
                    custitem.typeSpecified = True
                    custitem.internalId = iteminternalId
                    Dim lineItem As PurchaseOrderItem = New PurchaseOrderItem
                    lineItem.item = custitem
                    lineItem.quantity = qty
                    lineItem.quantitySpecified = True

                    Dim cfRefs As CustomFieldRef()
                    cfRefs = New CustomFieldRef(0) {}
                    Dim datafRef As StringCustomFieldRef
                    datafRef = New StringCustomFieldRef
                    datafRef.scriptId = "custcol_vendor_memo"
                    datafRef.value = newdata
                    Dim uniquekey As LongCustomFieldRef = New LongCustomFieldRef
                    uniquekey.scriptId = "custcol_line_unique_key"
                    uniquekey.value = CType(lineuniquekey, Long)
                    cfRefs(0) = CType(datafRef, CustomFieldRef)
                    cfRefs(1) = CType(uniquekey, CustomFieldRef)
                    lineItem.customFieldList = cfRefs

                    lineItem.location = locationRec

                    newitemlist(i) = lineItem
                End If
                i += 1
            Next
            Dim newPOItemList As PurchaseOrderItemList
            newPOItemList = New PurchaseOrderItemList
            newPOItemList.replaceAll = False
            newPOItemList.item = newitemlist
            po.itemList = newPOItemList
            SetPreferences()
            Dim writeres = service.update(po)
            If (writeres.status.isSuccess) Then
                Return "Successfully updated"
            Else
                Return GetStatusDetails(writeres.status)
            End If
        Else
            Return GetStatusDetails(response.status)
        End If

    End Function

    'Function cureateinboundshipment(polist As String(), itemlist As String())
    'Dim inboundshipment As InboundShipment
    'inboundshipment = New InboundShipment
    'Dim inboundshipmentitems As InboundShipmentItems()
    'inboundshipmentitems = New InboundShipmentItems(polist.Length) {}
    'For i As Integer = 0 To UBound(polist)
    'inboundshipmentitems(i) = New InboundShipmentItems
    'Dim po As RecordRef
    'po = New RecordRef
    'po.type = RecordType.purchaseOrder
    'po.internalId = polist(i)
    'inboundshipmentitems(i).purchaseOrder = po
    'Dim item As RecordRef
    ' item = New RecordRef
    'item.internalId = itemlist(i)
    'item.type = RecordType.inventoryItem
    'inboundshipmentitems(i).shipmentItem = item
    'Next
    'Dim isil As InboundShipmentItemsList
    'isil = New InboundShipmentItemsList
    'isil.inboundShipmentItems = inboundshipmentitems
    'inboundshipment.itemsList = isil
    'inboundshipment.shipmentStatus = InboundShipmentShipmentStatus._toBeShipped
    'inboundshipment.shipmentStatusSpecified = True
    'Dim writeres As WriteResponse
    'SetPreferences()
    'writeres = service.add(inboundshipment)
    'If (writeres.status.isSuccess) Then
    'Return CType(writeres.baseRef, RecordRef).internalId
    'Else
    'Return GetStatusDetails(writeres.status)
    'End If
    'End Function

    'Private vendBasic As Object
    'Public Property NSUtility As Object
    'Public Property Client As Object
    'Public Property vend As Object
    'Function CreateInboundShipmentFromVendor(vendor As String)
    'Dim vendSearch As VendorSearch = New VendorSearch
    ' vendorEntityID As SearchStringField = New SearchStringField
    'vendorEntityID.operator = SearchStringFieldOperator.contains
    'vendorEntityID.operatorSpecified = True
    ' vendorEntityID.searchValue = vendor
    'Dim vendBasic As VendorSearchBasic = New VendorSearchBasic
    'vendBasic.entityId = vendorEntityID
    'vendSearch.basic = vendBasic
    'SetPreferences()

    'Dim res As SearchResult = service.search(vendSearch)
    'If res.status.isSuccess Then
    'If (res.totalRecords > 0) Then
    'Dim recordList() As Record
    'Dim vendorList As List(Of Vendor) = New List(Of Vendor)
    'Dim i As Integer = 0
    'Do While (i _
    ' <= (res.totalPages - 1))
    'recordList = res.recordList
    'Dim j As Integer = 0
    'Do While (j _
    '<= (recordList.Length - 1))
    'vendorList.Add(CType(recordList(j), Vendor))
    'j = (j + 1)
    'Loop

    'If (res.pageIndex < res.totalPages) Then
    ' Client.SetPreferences
    ' res = service.searchMoreWithId(res.searchId, (res.pageIndex + 1))
    'End If

    'i = (i + 1)
    'Loop

    'For Each vend As Vendor In vendorList
    'SetPreferences()
    ' SearchPOByEntityID(vend)
    'Next
    'End If
    'End If

    'End Function
    'Function SearchPOByEntityID(vendorRecord As Vendor)
    'Dim xactionSearch As TransactionSearch = New TransactionSearch
    'Dim entity As SearchMultiSelectField = New SearchMultiSelectField
    'entity.operator = SearchMultiSelectFieldOperator.anyOf
    'entity.operatorSpecified = True
    'Dim vendRecordRef As RecordRef = New RecordRef
    'vendRecordRef.internalId = vendorRecord.internalId
    'vendRecordRef.type = RecordType.vendor
    'vendRecordRef.typeSpecified = True
    'Dim vendRecordRefArray() As RecordRef = New RecordRef((1) - 1) {}
    'vendRecordRefArray(0) = vendRecordRef
    'entity.searchValue = vendRecordRefArray
    'Dim searchPurchaseOrderField As SearchEnumMultiSelectField = New SearchEnumMultiSelectField
    'searchPurchaseOrderField.operator = SearchEnumMultiSelectFieldOperator.anyOf
    'searchPurchaseOrderField.operatorSpecified = True
    'Dim soStringArray() As String = New String((1) - 1) {}
    'soStringArray(0) = TRANSACTION_TYPE
    'searchPurchaseOrderField.searchValue = soStringArray
    'Dim searchBillVariantStatus As SearchEnumMultiSelectField = New SearchEnumMultiSelectField
    'searchBillVariantStatus.operator = SearchEnumMultiSelectFieldOperator.anyOf
    'searchBillVariantStatus.operatorSpecified = True
    'Dim sbvs() As String = New String((1) - 1) {}
    'sbvs(0) = Nothing
    'searchBillVariantStatus.searchValue = sbvs
    'Dim xactionBasic As TransactionSearchBasic = New TransactionSearchBasic
    'xactionBasic.type = searchPurchaseOrderField
    'xactionBasic.entity = entity
    'xactionBasic.billVarianceStatus = searchBillVariantStatus
    'xactionSearch.basic = xactionBasic
    'Dim res As SearchResult = service.search(xactionSearch)
    'If res.status.isSuccess Then
    'Dim recordList() As Record
    'Dim i As Integer = 1
    'Do While (i <= res.totalPages)
    'If (i > 1) Then
    ' SetPreferences()
    'res = service.searchMoreWithId(res.searchId, i)

    'End If

    'recordList = res.recordList
    'Dim j As Integer = 0
    'Do While (j < recordList.Length)
    'If (TypeOf recordList(j) Is PurchaseOrder) Then
    'Dim po As PurchaseOrder = CType(recordList(j), PurchaseOrder)
    'End If

    'j = (j + 1)
    'Loop

    ' i = (i + 1)
    'Loop
    'End If
    'End Function

    Function ComputeSignature(compId, consumerKey, consumerSecret, tokenId, tokenSecret, nonce, timestamp)
        Dim baseString As String = compId & "&" & consumerKey & "&" & tokenId & "&" & nonce & "&" & timestamp
        Dim key As String = consumerSecret & "&" & tokenSecret
        Dim signature As String = ""
        Dim encoding As ASCIIEncoding
        encoding = New ASCIIEncoding
        Dim keyBytes() As Byte = encoding.GetBytes(key)
        Dim baseStringBytes() As Byte = encoding.GetBytes(baseString)
        Using hmacSha1 = New HMACSHA1(keyBytes)
            Dim hashBaseString() As Byte = hmacSha1.ComputeHash(baseStringBytes)
            signature = Convert.ToBase64String(hashBaseString)
        End Using
        Dim sign As TokenPassportSignature = New TokenPassportSignature
        sign.algorithm = "HMAC-SHA1"
        sign.Value = signature
        Return sign
    End Function
End Class
Public Class DataCenterAwareNetSuiteService
    Inherits NetSuiteService
    Private OriginalUri As Uri

    Public Sub New(ByVal account As String, ByVal doNotSetUrl As Boolean)
        MyBase.New
        Me.OriginalUri = New System.Uri(Me.Url)
        If ((account Is Nothing) _
                OrElse (account.Length = 0)) Then
            account = "empty"
        End If

        If Not doNotSetUrl Then
            Dim urls As DataCenterUrls = Me.getDataCenterUrls(account).dataCenterUrls
            Dim dataCenterUri As Uri = New Uri((urls.webservicesDomain + Me.OriginalUri.PathAndQuery))
            Me.Url = dataCenterUri.ToString
        End If

    End Sub

    Public Sub SetAccount(ByVal account As String)
        If ((account Is Nothing) _
                    OrElse (account.Length = 0)) Then
            account = "empty"
        End If

        Me.Url = Me.OriginalUri.AbsoluteUri
        Dim urls As DataCenterUrls = getDataCenterUrls(account).dataCenterUrls
        Dim dataCenterUri As Uri = New Uri((urls.webservicesDomain + Me.OriginalUri.PathAndQuery))
        Me.Url = dataCenterUri.ToString
    End Sub
End Class

Class OverrideCertificatePolicy
    Implements ICertificatePolicy

    Public Function CheckValidationResult(ByVal srvPoint As ServicePoint, ByVal certificate As System.Security.Cryptography.X509Certificates.X509Certificate, ByVal request As WebRequest, ByVal certificateProblem As Integer) As Boolean
        Return True
    End Function

    Private Function ICertificatePolicy_CheckValidationResult(srvPoint As ServicePoint, certificate As X509Certificate, request As WebRequest, certificateProblem As Integer) As Boolean Implements ICertificatePolicy.CheckValidationResult
        Throw New NotImplementedException()
    End Function
End Class