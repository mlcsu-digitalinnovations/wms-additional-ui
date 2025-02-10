$(document).ready(function () {
    $("#btnSubmit").val("Accept and submit referral");
    $("#formCheck").submit(function (e) {
        //disable the submit button
        $("#btnSubmit").attr("disabled", true);
        $("#btnSubmit").val("Please wait...");
    });
});