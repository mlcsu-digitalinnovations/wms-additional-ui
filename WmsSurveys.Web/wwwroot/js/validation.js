$(document).ready(function () {
    $(function () {
        var settngs = $.data($('form')[0], 'validator').settings;
        settngs.onkeyup = false;
        settngs.onfocusout = false;
    });
});