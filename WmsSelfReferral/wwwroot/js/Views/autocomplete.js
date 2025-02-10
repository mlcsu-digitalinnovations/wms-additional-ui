$(document).ready(function () {
    accessibleAutocomplete.enhanceSelectElement({
        displayMenu: 'overlay',
        defaultValue: '',
        selectElement: document.querySelector('#SelectedMskHub')
    });

    var form = $("#mskHub")
        .removeData("validator") /* added by the raw jquery.validate plugin */
        .removeData("unobtrusiveValidation");  /* added by the jquery unobtrusive plugin*/

    $("#SelectedMskHub").attr("data-val", "true");
    $("#SelectedMskHub").attr("data-val-required", "MSK required");

    $.validator.unobtrusive.parse(form);

    $("#mskHub").submit(function (e) {
        $("#btnSubmit").attr("disabled", true);
        $("#btnSubmit").val("Checking...");
        $('#mskHub').validate();

        if ($("#mskHub").valid()) {

        } else {
            $("#SelectedMSKHub_Error").text("Please enter your MSK site");
            $('#SelectedMSKHub_Error').removeClass('field-validation-valid').addClass('field-validation-error');

            $("#btnSubmit").removeAttr("disabled");
            $("#btnSubmit").val("Continue");
        }
    });
});