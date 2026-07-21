const API_BASE = 'http://localhost:9103';

chrome.runtime.onInstalled.addListener(() => {
  console.log('DayScribe extension installed successfully.');
});

chrome.tabs.onActivated.addListener(async (activeInfo) => {
  try {
    const tab = await chrome.tabs.get(activeInfo.tabId);
    if (tab.url && tab.url.startsWith('http')) {
      await fetch(`${API_BASE}/api/browser/activity`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          url: tab.url,
          title: tab.title || '',
          timeSpentSecs: 5,
          timestamp: new Date().toISOString()
        })
      });
    }
  } catch (err) {
    console.error('DayScribe background: error tracking tab activation', err);
  }
});

chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  if (message.type === 'checkConnection') {
    fetch(`${API_BASE}/api/browser/activity`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        url: sender.tab?.url || 'chrome-extension://ping',
        title: 'Connection Test',
        timeSpentSecs: 1,
        timestamp: new Date().toISOString()
      })
    })
    .then(r => sendResponse({ connected: r.ok }))
    .catch(() => sendResponse({ connected: false }));
    return true;
  }
});
