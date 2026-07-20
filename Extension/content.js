let startTime = Date.now();
let accumulatedTime = 0;
let lastUrl = window.location.href;
let lastTitle = document.title;
let trackingInterval = null;

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

    // Reset accumulated time
    accumulatedTime = 0;
    startTime = Date.now();

    fetch('http://localhost:9103/api/browser/activity', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
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

// Listen for tab focus/blur
document.addEventListener('visibilitychange', () => {
  if (document.visibilityState === 'visible') {
    lastUrl = window.location.href;
    lastTitle = document.title;
    startTracking();
  } else {
    stopTracking();
  }
});

// Send pending activity when page is unloaded/navigated away
window.addEventListener('beforeunload', () => {
  sendActivity();
});

// Periodic Title updates (for Single Page Apps like YouTube/GitHub where pages change dynamically without page loads)
const observer = new MutationObserver(() => {
  if (document.title && document.title !== lastTitle) {
    lastTitle = document.title;
  }
});
observer.observe(document.querySelector('title') || document.documentElement, {
  subtree: true,
  characterData: true,
  childList: true
});

// Start tracking immediately if the page loads and is already active
if (document.visibilityState === 'visible') {
  startTracking();
}
