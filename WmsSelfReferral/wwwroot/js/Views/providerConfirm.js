$(document).ready(function () {

    $("#formProvider").submit(function (e) {
        //disable the submit button
        $("#btnSubmit").attr("disabled", true);
        $("#btnSubmit").val("Please wait...");
    });

});