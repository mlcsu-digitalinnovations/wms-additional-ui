$(document).ready(function () {
    $("#formEmail").submit(function (e) {
        if ($("#NHSNumber").valid()) {
            //disable the submit button
            $("#btnSubmit").attr("disabled", true);
            $("#btnSubmit").val("Checking...");
        }
    });

});