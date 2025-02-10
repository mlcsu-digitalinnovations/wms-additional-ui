
//delete users
$(document).ready(function () {
    $(document).on('click', '.deleteUser', function (e) {

        var userid = $(this).attr('data-userid');

        $(".deleteUser").attr("disabled", true);
        $(".deleteUser").val("Please wait...");

        $.ajax({
            type: 'GET',
            url: '/Manage/DeleteUser/' + userid,

            success: function (data) {
                if (data == "Success") {
                    $("#nhsuk-notification").removeAttr("style")
                    $('#nhsuk-notification').addClass('nhsuk-notification-banner--success');
                    $('#nhsuk-notification').removeClass('nhsuk-notification-banner--danger');
                    $('#nhsuk-notification').removeClass('nhsuk-u-visually-hidden');
                    $('#MessageTitle').append('Success');
                    $('#MessageText').append('<p class="nhsuk-body-s">' + data + '</p>');
                    $(".deleteUser").val("Delete User");
                    $("#fsDelete").attr("style", "display:none;");
                }
                else {
                    $("#nhsuk-notification").removeAttr("style")
                    $('#nhsuk-notification').addClass('nhsuk-notification-banner--danger');
                    $('#nhsuk-notification').removeClass('nhsuk-notification-banner--success');
                    $('#nhsuk-notification').removeClass('nhsuk-u-visually-hidden');
                    $('#MessageTitle').append('Failure');
                    $('#MessageText').append('<p class="nhsuk-body-s">' + data + '</p>');
                    $(".deleteUser").attr("disabled", false);
                    $(".deleteUser").val("Delete User");
                }

            },
            contentType: "application/json",
            dataType: 'json'
        });
    });

    if ($('#UserInviteExpiry').length) {
        $(function () {
            $("#UserInviteExpiry").datepicker({ dateFormat: 'dd/mm/yy' });
        });
    }


});