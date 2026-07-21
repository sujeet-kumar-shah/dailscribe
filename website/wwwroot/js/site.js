document.addEventListener('DOMContentLoaded', function () {
  // === Scroll Animations with Intersection Observer ===
  const observerOptions = { threshold: 0.1, rootMargin: '0px 0px -50px 0px' };

  const revealObserver = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
      if (entry.isIntersecting) {
        entry.target.classList.add('visible');
        revealObserver.unobserve(entry.target);
      }
    });
  }, observerOptions);

  document.querySelectorAll('.reveal, .reveal-left, .reveal-right, .reveal-scale, .stagger-children')
    .forEach(el => revealObserver.observe(el));

  // === Navbar scroll effect ===
  const navbar = document.querySelector('.navbar');
  let lastScroll = 0;

  window.addEventListener('scroll', () => {
    const currentScroll = window.pageYOffset;

    if (currentScroll > 50) {
      navbar.classList.add('scrolled');
    } else {
      navbar.classList.remove('scrolled');
    }

    // Hide/show navbar on scroll direction
    if (currentScroll > lastScroll && currentScroll > 200) {
      navbar.style.transform = 'translateY(-100%)';
    } else {
      navbar.style.transform = 'translateY(0)';
    }

    lastScroll = currentScroll;
  }, { passive: true });

  // === Back to top button ===
  const backToTop = document.createElement('button');
  backToTop.className = 'back-to-top';
  backToTop.innerHTML = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M18 15l-6-6-6 6"/></svg>';
  backToTop.setAttribute('aria-label', 'Back to top');
  document.body.appendChild(backToTop);

  window.addEventListener('scroll', () => {
    backToTop.classList.toggle('visible', window.pageYOffset > 400);
  }, { passive: true });

  backToTop.addEventListener('click', () => {
    window.scrollTo({ top: 0, behavior: 'smooth' });
  });

  // === Pricing billing toggle ===
  const billingToggle = document.getElementById('billingToggle');
  const monthlyPrices = document.querySelectorAll('.price-monthly');
  const annualPrices = document.querySelectorAll('.price-annual');
  const billingLabel = document.getElementById('billingLabel');
  const billingPeriods = document.querySelectorAll('.billing-period');

  if (billingToggle) {
    billingToggle.addEventListener('change', function () {
      const isAnnual = this.checked;
      monthlyPrices.forEach(el => el.classList.toggle('d-none', isAnnual));
      annualPrices.forEach(el => el.classList.toggle('d-none', !isAnnual));
      billingPeriods.forEach(el => el.classList.toggle('d-none', false));

      if (billingLabel) {
        billingLabel.innerHTML = isAnnual
          ? 'Annual <span class="badge bg-success ms-1">Save 20%</span>'
          : 'Monthly';
      }

      // Update pricing card featured state if needed
      if (isAnnual) {
        document.querySelectorAll('.price-amount').forEach(el => {
          const yearlyEl = el.querySelector('.price-annual');
          if (yearlyEl && yearlyEl.textContent.trim() !== '--') {
            el.style.setProperty('--amount', yearlyEl.textContent.trim());
          }
        });
      }
    });
  }

  // === Smooth scroll for anchor links ===
  document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function (e) {
      const targetId = this.getAttribute('href');
      if (targetId === '#') return;
      const target = document.querySelector(targetId);
      if (target) {
        e.preventDefault();
        const offset = 80;
        const top = target.getBoundingClientRect().top + window.pageYOffset - offset;
        window.scrollTo({ top, behavior: 'smooth' });
      }
    });
  });

  // === Contact form character counter ===
  const messageField = document.getElementById('Contact_Message');
  const charCount = document.getElementById('charCount');
  if (messageField && charCount) {
    const maxLength = messageField.getAttribute('maxlength') || 5000;
    const updateCount = () => {
      const remaining = maxLength - messageField.value.length;
      charCount.textContent = `${remaining} characters remaining`;
      charCount.style.color = remaining < 100 ? '#ef4444' : remaining < 500 ? '#f59e0b' : '#64748b';
    };
    messageField.addEventListener('input', updateCount);
    updateCount();
  }

  // === Platform detection for download page ===
  const downloadCards = document.querySelectorAll('.download-card');
  if (downloadCards.length > 0) {
    const platform = navigator.platform.toLowerCase();
    let detected = '';

    if (platform.includes('win')) detected = 'Windows';
    else if (platform.includes('mac')) detected = 'macOS';
    else if (platform.includes('linux')) detected = 'Linux';

    if (detected) {
      downloadCards.forEach(card => {
        const title = card.querySelector('h4')?.textContent;
        if (title === detected) {
          card.classList.add('border-primary', 'bg-primary', 'bg-opacity-10');
          const btn = card.querySelector('.btn');
          if (btn) {
            btn.textContent = 'Download for ' + detected;
            btn.classList.remove('btn-primary');
            btn.classList.add('btn-success');
          }
        }
      });
    }
  }

  // === Animated counter for stats ===
  function animateCounter(el) {
    const target = parseInt(el.getAttribute('data-target')) || 0;
    const duration = 2000;
    const step = target / (duration / 16);
    let current = 0;

    const update = () => {
      current += step;
      if (current < target) {
        el.textContent = Math.floor(current);
        requestAnimationFrame(update);
      } else {
        el.textContent = target;
      }
    };

    const counterObserver = new IntersectionObserver((entries) => {
      entries.forEach(entry => {
        if (entry.isIntersecting) {
          update();
          counterObserver.unobserve(entry.target);
        }
      });
    }, { threshold: 0.5 });

    counterObserver.observe(el);
  }

  document.querySelectorAll('.counter').forEach(animateCounter);

  // === Console greeting ===
  console.log('%c DayScribe %c Local-first, Privacy-first Activity Tracker ',
    'background:#4f46e5;color:white;padding:4px 8px;border-radius:4px 0 0 4px;font-weight:bold;',
    'background:#1e293b;color:#94a3b8;padding:4px 8px;border-radius:0 4px 4px 0;');
});
