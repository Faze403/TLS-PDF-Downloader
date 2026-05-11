const JOB_KEY_PREFIX = "job:";
const OFFSCREEN_DOCUMENT_PATH = "offscreen.html";

let creatingOffscreenDocument = null;

chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  if (!message || (message.target && message.target !== "background")) {
    return false;
  }

  if (message.type === "START_CONVERSION") {
    startConversion(message, sender)
      .then((response) => sendResponse(response))
      .catch((error) => sendResponse(errorResponse(error)));
    return true;
  }

  if (message.type === "OFFSCREEN_PROGRESS") {
    relayToSourceTab(message.jobId, {
      type: "CONVERSION_PROGRESS",
      jobId: message.jobId,
      status: message.status,
      current: message.current,
      total: message.total,
      filename: message.filename,
    });
    sendResponse({ ok: true });
    return false;
  }

  if (message.type === "OFFSCREEN_COMPLETE") {
    completeConversion(message)
      .then((response) => sendResponse(response))
      .catch(async (error) => {
        await failConversion(message.jobId, formatError(error));
        sendResponse(errorResponse(error));
      });
    return true;
  }

  if (message.type === "OFFSCREEN_ERROR") {
    failConversion(message.jobId, message.error || "PDF 저장에 실패했습니다.");
    sendResponse({ ok: true });
    return false;
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
  await setupOffscreenDocument();
  await chrome.runtime.sendMessage({
    target: "offscreen",
    type: "CONVERT_JOB",
    job,
  });

  return {
    ok: true,
    jobId,
  };
}

async function completeConversion(message) {
  if (!message.jobId || !message.url || !message.filename) {
    throw new Error("다운로드 정보를 찾을 수 없습니다.");
  }

  const downloadId = await chrome.downloads.download({
    url: message.url,
    filename: message.filename,
    conflictAction: "uniquify",
    saveAs: false,
  });

  await relayToSourceTab(message.jobId, {
    type: "CONVERSION_COMPLETE",
    jobId: message.jobId,
    filename: message.filename,
    downloadId,
  });
  await removeJob(message.jobId);

  return {
    ok: true,
    downloadId,
  };
}

async function failConversion(jobId, error) {
  await relayToSourceTab(jobId, {
    type: "CONVERSION_ERROR",
    jobId,
    error,
  });
  await removeJob(jobId);
}

async function relayToSourceTab(jobId, payload) {
  const job = await getJob(jobId);
  if (!job || !job.sourceTabId) {
    return;
  }

  try {
    await chrome.tabs.sendMessage(job.sourceTabId, payload);
  } catch (error) {
    // The source tab may have been closed or reloaded.
  }
}

async function getJob(jobId) {
  if (!jobId) {
    return null;
  }
  const key = JOB_KEY_PREFIX + jobId;
  const data = await getJobStore().get(key);
  return data[key] || null;
}

async function removeJob(jobId) {
  if (!jobId) {
    return;
  }
  await getJobStore().remove(JOB_KEY_PREFIX + jobId);
}

async function setupOffscreenDocument() {
  const offscreenUrl = chrome.runtime.getURL(OFFSCREEN_DOCUMENT_PATH);

  if ("getContexts" in chrome.runtime) {
    const contexts = await chrome.runtime.getContexts({
      contextTypes: ["OFFSCREEN_DOCUMENT"],
      documentUrls: [offscreenUrl],
    });
    if (contexts.length > 0) {
      return;
    }
  } else {
    const matchedClients = await clients.matchAll();
    if (matchedClients.some((client) => client.url === offscreenUrl)) {
      return;
    }
  }

  if (!creatingOffscreenDocument) {
    creatingOffscreenDocument = chrome.offscreen.createDocument({
      url: OFFSCREEN_DOCUMENT_PATH,
      reasons: ["BLOBS", "DOM_PARSER"],
      justification: "Create a PDF from ubdoc page images without opening a visible tab.",
    });
  }

  try {
    await creatingOffscreenDocument;
  } finally {
    creatingOffscreenDocument = null;
  }
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
    error: formatError(error),
  };
}

function formatError(error) {
  return error && error.message ? error.message : String(error);
}
