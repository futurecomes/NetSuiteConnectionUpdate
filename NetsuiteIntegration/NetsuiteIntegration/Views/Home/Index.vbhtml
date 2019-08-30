@Code
    If Not ViewData.ContainsKey("Message") Then
        ViewData("Message") = "You can update product estimate end date in here."
        'ViewData("Message") = "You can update Memo From Vendor in here."
    End If
    ViewData("Title") = "Home Page"
End Code

<div class="row">
    <div class="col-md-12">
        <h3>@ViewData("Message")</h3>
    </div>
</div>


