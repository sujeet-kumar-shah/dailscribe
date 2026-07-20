document.addEventListener('DOMContentLoaded', function () {
  var billingToggle = document.getElementById('billingToggle');
  var monthlyPrices = document.querySelectorAll('.price-monthly');
  var annualPrices = document.querySelectorAll('.price-annual');
  var billingLabel = document.getElementById('billingLabel');

  if (billingToggle) {
    billingToggle.addEventListener('change', function () {
      var isAnnual = this.checked;
      monthlyPrices.forEach(function (el) { el.classList.toggle('d-none', isAnnual); });
      annualPrices.forEach(function (el) { el.classList.toggle('d-none', !isAnnual); });
      if (billingLabel) {
        billingLabel.textContent = isAnnual ? 'Annual (save 20%)' : 'Monthly';
      }
    });
  }

  document.querySelectorAll('a[href^="#"]').forEach(function (anchor) {
    anchor.addEventListener('click', function (e) {
      var target = document.querySelector(this.getAttribute('href'));
      if (target) {
        e.preventDefault();
        target.scrollIntoView({ behavior: 'smooth' });
      }
    });
  });
});
