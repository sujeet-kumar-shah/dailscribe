let startTime = Date.now();
let accumulatedTime = 0;
let lastUrl = window.location.href;
let lastTitle = document.title;
let trackingInterval = null;
let titleObserver = null;

const API_BASE = 'http://localhost:9103';

function getActiveTime() {
  if (document.visibilityState === 'visible') {
    let elapsed = Math.floor((Date.now() - startTime) / 1000);
    startTime = Date.now();
    accumulatedTime += elapsed;
  }
}

function sendActivity() {
  getActiveTime();
  if (accumulatedTime > 0) {
    const payload = {
      url: lastUrl,
      title: lastTitle || document.title || lastUrl,
      timeSpentSecs: accumulatedTime,
      timestamp: new Date().toISOString()
    };

    accumulatedTime = 0;
    startTime = Date.now();

    fetch(`${API_BASE}/api/browser/activity`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    })
    .then(response => {
      if (!response.ok) {
        console.warn('DayScribe: local API returned status', response.status);
      }
    })
    .catch(err => {
      console.error('DayScribe: failed to connect to local tracker API', err);
    });
  }
}

function startTracking() {
  startTime = Date.now();
  if (!trackingInterval) {
    trackingInterval = setInterval(() => {
      if (document.visibilityState === 'visible') {
        getActiveTime();
      }
    }, 5000);
  }
}

function stopTracking() {
  if (trackingInterval) {
    clearInterval(trackingInterval);
    trackingInterval = null;
  }
  sendActivity();
}

function updateUrlAndTitle() {
  lastUrl = window.location.href;
  lastTitle = document.title;
}

// ---- SPA Navigation Tracking ----
// Intercept history.pushState and replaceState for SPA frameworks
const originalPushState = history.pushState;
const originalReplaceState = history.replaceState;

history.pushState = function (...args) {
  originalPushState.apply(this, args);
  updateUrlAndTitle();
};

history.replaceState = function (...args) {
  originalReplaceState.apply(this, args);
  updateUrlAndTitle();
};

window.addEventListener('popstate', updateUrlAndTitle);
window.addEventListener('hashchange', updateUrlAndTitle);

// ---- Title Observer ----
function setupTitleObserver() {
  const titleEl = document.querySelector('title');
  if (titleEl) {
    titleObserver = new MutationObserver(() => {
      if (document.title !== lastTitle) {
        lastTitle = document.title;
      }
    });
    titleObserver.observe(titleEl, { subtree: true, characterData: true, childList: true });
  }
}

// ---- Tab visibility tracking ----
document.addEventListener('visibilitychange', () => {
  if (document.visibilityState === 'visible') {
    updateUrlAndTitle();
    startTracking();
  } else {
    stopTracking();
  }
});

window.addEventListener('beforeunload', () => {
  sendActivity();
});

// Initialize
setupTitleObserver();
if (document.visibilityState === 'visible') {
  startTracking();
}
