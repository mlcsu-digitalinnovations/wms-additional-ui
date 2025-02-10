$(document).ready(function () {
    $("#formEmail").submit(function (e) {
        if ($("#Email").valid()) {
            //disable the submit button
            $("#btnSubmit").attr("disabled", true);
            $("#btnSubmit").val("Checking...");
        }
    });

});