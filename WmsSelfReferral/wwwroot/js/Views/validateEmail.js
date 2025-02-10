$(document).ready(function () {

    var dt = new Date().toString();
    $("#UserTimeZone").val(Intl.DateTimeFormat().resolvedOptions().timeZone);

    $("#formEmail").submit(function (e) {
        if ($("#EmailAddress").valid()) {
            //disable the submit button
            $("#btnSubmit").attr("disabled", true);
            $("#btnSubmit").val("Checking...");
        }
    });

});