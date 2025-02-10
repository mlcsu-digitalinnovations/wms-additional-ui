$(document).ready(function () {
    $("#formEmail").submit(function (e) {
        if ($("#Token").valid()) {
            //disable the submit button
            $("#btnSubmit").attr("disabled", true);
            $("#btnSubmit").val("Checking...");
        }
    });
});