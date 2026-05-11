const JOB_KEY_PREFIX = "job:";

chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  if (!message || message.type !== "START_CONVERSION") {
    return false;
  }

  startConversion(message, sender)
    .then((response) => sendResponse(response))
    .catch((error) => {
      sendResponse({
        ok: false,
        error: error && error.message ? error.message : String(error),
      });
    });

  return true;
});

async function startConversion(message, sender) {
  if (!message.viewerUrl) {
    throw new Error("ubdoc URL을 찾을 수 없습니다.");
  }
  if (!message.state || typeof message.state !== "object") {
    throw new Error("문서 상태 정보를 찾을 수 없습니다.");
  }

  const jobId = createJobId();
  const job = {
    jobId,
    viewerUrl: message.viewerUrl,
    state: message.state,
    sourceTabId: sender && sender.tab ? sender.tab.id : null,
    createdAt: new Date().toISOString(),
  };

  const store = getJobStore();
  await store.set({ [JOB_KEY_PREFIX + jobId]: job });

  const converterUrl = chrome.runtime.getURL(
    "converter.html#jobId=" + encodeURIComponent(jobId)
  );
  const tab = await chrome.tabs.create({ url: converterUrl });

  return {
    ok: true,
    jobId,
    tabId: tab.id,
  };
}

function createJobId() {
  const cryptoApi = globalThis.crypto;
  if (cryptoApi && typeof cryptoApi.randomUUID === "function") {
    return cryptoApi.randomUUID();
  }

  const bytes = new Uint8Array(16);
  cryptoApi.getRandomValues(bytes);
  return Array.from(bytes, (value) => value.toString(16).padStart(2, "0")).join("");
}

function getJobStore() {
  return chrome.storage.session || chrome.storage.local;
}
