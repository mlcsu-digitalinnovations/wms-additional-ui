import bannerHtml from './bannerhtml.js';
function hideCookieBanner() {
  document.getElementById('cookiebanner').classList.add('cookie-display-none');
}

function showCookieConfirmation() {
  document.getElementById('nhsuk-cookie-confirmation-banner').classList.remove('cookie-display-none');
}

function addFocusCookieConfirmation() {
  const cookieConfirmationMessage = document.getElementById('nhsuk-success-banner__message');
  cookieConfirmationMessage.setAttribute('tabIndex', '-1');
  cookieConfirmationMessage.focus();
}

function removeFocusCookieConfirmation() {
  const cookieConfirmationMessage = document.getElementById('nhsuk-success-banner__message');
  cookieConfirmationMessage.addEventListener('blur', () => {
    cookieConfirmationMessage.removeAttribute('tabIndex');
  });
}

/**
 * Call common methods on link click as well as consent type callback
 * @param {function} consentCallback callback to be called based on which link has been clicked.
 */
function handleLinkClick(consentCallback, value) {
  hideCookieBanner();
  consentCallback();
  showCookieConfirmation();
  addFocusCookieConfirmation();
  removeFocusCookieConfirmation();
}

/**
 * Insert the cookie banner at the top of a page.
 * @param {function} onAccept callback that is called when necessary consent is accepted.
 * @param {function} onAnalyticsAccept callback that is called analytics consent is accepted.
 */
export default function insertCookieBanner(onAccept, onAnalyticsAccept) {
  // add a css block to the inserted html
  const div = document.createElement('div');
  div.innerHTML = bannerHtml;
  document.body.insertBefore(div, document.body.firstChild);

  document.getElementById('nhsuk-cookie-banner__link_accept').addEventListener('click', (e) => {
    e.preventDefault();
    handleLinkClick(onAccept);
  });

  document.getElementById('nhsuk-cookie-banner__link_accept_analytics').addEventListener('click', (e) => {
    e.preventDefault();
    handleLinkClick(onAnalyticsAccept);
  });
}