import { getStatistics, setStatistics, setConsented  } from "./main.js";

var statistics = document.getElementById("statistics");

statistics.checked = window.NHSCookieConsent.getStatistics();

function updateSettings() {
  window.NHSCookieConsent.setStatistics(statistics.checked);
  // If the user has hit save, then they have consented.
  window.NHSCookieConsent.setConsented(true);
  // reload
  location = location;
}

document.getElementById('cookie-update-button').addEventListener('click', (e) => {
  e.preventDefault();
  updateSettings();
});