$(document).ready(function () {
    $("#formPostCode").submit(function (e) {
        if ($("#Postcode").valid()) {
            //disable the submit button
            $("#btnSubmit").attr("disabled", true);
            $("#btnSubmit").val("Checking...");
        }
    });
});