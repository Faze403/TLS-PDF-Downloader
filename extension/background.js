const JOB_KEY_PREFIX = "job:";

chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  if (!message || !message.type) {
    return false;
  }

  if (message.type === "START_CONVERSION") {
    startConversion(message, sender)
      .then((response) => sendResponse(response))
      .catch((error) => sendResponse(errorResponse(error)));
    return true;
  }

  if (message.type === "CLOSE_CONVERTER_TAB") {
    closeSenderTab(sender)
      .then((response) => sendResponse(response))
      .catch((error) => sendResponse(errorResponse(error)));
    return true;
  }

  return false;
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

  await getJobStore().set({ [JOB_KEY_PREFIX + jobId]: job });

  const converterUrl = chrome.runtime.getURL(
    "converter.html#jobId=" + encodeURIComponent(jobId)
  );
  const tab = await chrome.tabs.create({
    url: converterUrl,
    active: false,
  });
  closeSourceTab(job.sourceTabId);

  return {
    ok: true,
    jobId,
    tabId: tab.id,
  };
}

async function closeSenderTab(sender) {
  if (!sender || !sender.tab || !sender.tab.id) {
    throw new Error("닫을 변환 탭을 찾을 수 없습니다.");
  }

  await chrome.tabs.remove(sender.tab.id);
  return { ok: true };
}

function closeSourceTab(tabId) {
  if (!tabId) {
    return;
  }

  chrome.tabs.remove(tabId).catch(() => {
    // The source ubdoc tab may already be closed.
  });
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

function errorResponse(error) {
  return {
    ok: false,
    error: error && error.message ? error.message : String(error),
  };
}
