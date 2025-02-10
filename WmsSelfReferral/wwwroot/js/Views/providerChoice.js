$(document).ready(function () {
        $.validator.defaults.focusInvalid = false;
        $("#formProviderChoice").submit(function (e) {
            if ($("input[name=ProviderId]").valid()) {
                //disable the submit button
                $("#btnSubmit").attr("disabled", true);
                $("#btnSubmit").val("Checking...");
            } else {
                //form not valid, set focus                
                $("#provider-error-message").attr("tabindex",-1).focus();
            }
        });

        $('.serviceuser-provider-detail-box input[type="radio"]').each(function () {
            if ($(this).is(':checked')) {
                findServiceName($(this));
            }
        });

        if ($('#serviceuser-provider-selected-display').text() != "") {
            $('#serviceuser-provider-selected-wrapper').removeClass('hidden');
        }


        $('.serviceuser-provider-detail-box .nhsuk-radios__input').click(function () {
            findServiceName($(this));
        });

        function findServiceName(selected) {
            var selectedService = $(selected).data('provider-name');
            $('#serviceuser-provider-selected-display').text(selectedService);
            $('#serviceuser-provider-selected-wrapper').removeClass('hidden');
        }
    });