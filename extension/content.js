(function () {
  "use strict";

  const PANEL_ID = "tls-pdf-downloader-panel";
  const BUTTON_ID = "tls-pdf-downloader-button";
  const STATUS_ID = "tls-pdf-downloader-status";
  const WORKER_PATH = "/local/ubdoc/worker.php";
  const QUERY_KEYS = ["id", "tp", "pg", "item", "fid"];

  let currentJobId = null;
  let runButton = null;
  let statusBox = null;

  if (document.getElementById(PANEL_ID)) {
    return;
  }

  if (!/\/local\/ubdoc\//i.test(window.location.pathname)) {
    return;
  }

  installPanel();
  chrome.runtime.onMessage.addListener(handleRuntimeMessage);

  function installPanel() {
    const panel = document.createElement("div");
    panel.id = PANEL_ID;

    runButton = document.createElement("button");
    runButton.id = BUTTON_ID;
    runButton.type = "button";
    runButton.textContent = "PDF 저장";

    statusBox = document.createElement("div");
    statusBox.id = STATUS_ID;
    statusBox.setAttribute("role", "status");
    statusBox.textContent = "";

    panel.appendChild(runButton);
    panel.appendChild(statusBox);
    (document.body || document.documentElement).appendChild(panel);

    runButton.addEventListener("click", handleClick);
  }

  async function handleClick() {
    runButton.disabled = true;
    setStatus("문서 확인 중", "");

    try {
      const state = await checkState(window.location.href);
      const response = await chrome.runtime.sendMessage({
        type: "START_CONVERSION",
        viewerUrl: window.location.href,
        state,
      });

      if (!response || !response.ok) {
        throw new Error(response && response.error ? response.error : "PDF 저장을 시작할 수 없습니다.");
      }

      currentJobId = response.jobId;
      setStatus("다운로드 준비 중", "");
    } catch (error) {
      currentJobId = null;
      setStatus(formatError(error), "error");
      runButton.disabled = false;
    }
  }

  function handleRuntimeMessage(message) {
    if (!message || !currentJobId || message.jobId !== currentJobId) {
      return false;
    }

    if (message.type === "CONVERSION_PROGRESS") {
      const progress = formatProgress(message.current, message.total);
      setStatus((message.status || "처리 중") + progress, "");
      return false;
    }

    if (message.type === "CONVERSION_COMPLETE") {
      setStatus("다운로드가 시작되었습니다", "done");
      currentJobId = null;
      runButton.disabled = false;
      return false;
    }

    if (message.type === "CONVERSION_ERROR") {
      setStatus(message.error || "PDF 저장에 실패했습니다.", "error");
      currentJobId = null;
      runButton.disabled = false;
      return false;
    }

    return false;
  }

  async function checkState(viewerUrl) {
    const viewer = new URL(viewerUrl);
    const body = new URLSearchParams();
    body.set("job", "checkState");

    for (const key of QUERY_KEYS) {
      body.set(key, viewer.searchParams.get(key) || "");
    }

    const workerUrl = new URL(WORKER_PATH, viewer.origin).toString();
    const response = await fetch(workerUrl, {
      method: "POST",
      headers: {
        "Content-Type": "application/x-www-form-urlencoded;charset=UTF-8",
      },
      body: body.toString(),
      credentials: "include",
      referrer: viewerUrl,
    });

    const text = await response.text();
    if (!response.ok) {
      throw new Error("worker.php 요청 실패: HTTP " + response.status);
    }

    let state;
    try {
      state = JSON.parse(text);
    } catch (error) {
      throw new Error("worker.php 응답을 해석할 수 없습니다.");
    }

    if (!state || typeof state !== "object") {
      throw new Error("worker.php 응답을 해석할 수 없습니다.");
    }

    if (String(state.state_code || "") !== "100") {
      throw new Error("문서가 아직 준비되지 않았습니다.");
    }

    return state;
  }

  function setStatus(text, kind) {
    statusBox.dataset.kind = kind || "";
    statusBox.textContent = text;
  }

  function formatProgress(current, total) {
    if (!Number.isFinite(current) || !Number.isFinite(total) || total <= 0) {
      return "";
    }
    return " (" + current + "/" + total + ")";
  }

  function formatError(error) {
    return error && error.message ? error.message : String(error);
  }
})();
