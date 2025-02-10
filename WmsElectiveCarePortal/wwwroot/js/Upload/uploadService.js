

$(document).ready(function () {
    $("#formCheck").submit(function (e) {
        //disable the submit button
        if ($("#FormFile").valid()) {
            $("#btnSubmit").attr("disabled", true);
            $("#btnSubmit").val("Please wait...");
        }
    });

});