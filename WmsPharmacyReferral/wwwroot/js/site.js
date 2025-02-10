// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Handle cookie banner
$(document).ready(function () {
    $(document).on('click', '#nhsuk-cookie-banner__link_accept_analytics', function (e) {
        handleCloseCookies();
    });
});

function handleCloseCookies() {
    document.getElementById('cookiebanner').style.display = 'none';
    var cookieValue = encodeURIComponent("True");
    document.cookie = "CookieConsent=" + cookieValue + "; path=/; SameSite=Lax; Secure";
}



