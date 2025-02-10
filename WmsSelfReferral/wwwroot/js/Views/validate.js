$(document).ready(function () {
    $("#btnSubmit").val("Continue");
    $("#formEmail").submit(function (e) {
        //disable the submit button
        $("#btnSubmit").attr("disabled", true);
        $("#btnSubmit").val("Checking...");
    });
});