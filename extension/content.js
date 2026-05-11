(function () {
  "use strict";

  const PANEL_ID = "tls-pdf-downloader-panel";
  const BUTTON_ID = "tls-pdf-downloader-button";
  const STATUS_ID = "tls-pdf-downloader-status";
  const WORKER_PATH = "/local/ubdoc/worker.php";
  const QUERY_KEYS = ["id", "tp", "pg", "item", "fid"];

  if (document.getElementById(PANEL_ID)) {
    return;
  }

  if (!/\/local\/ubdoc\//i.test(window.location.pathname)) {
    return;
  }

  installPanel();

  function installPanel() {
    const panel = document.createElement("div");
    panel.id = PANEL_ID;

    const button = document.createElement("button");
    button.id = BUTTON_ID;
    button.type = "button";
    button.textContent = "PDF 저장";

    const status = document.createElement("div");
    status.id = STATUS_ID;
    status.setAttribute("role", "status");
    status.textContent = "";

    panel.appendChild(button);
    panel.appendChild(status);
    (document.body || document.documentElement).appendChild(panel);

    button.addEventListener("click", () => {
      handleClick(button, status);
    });
  }

  async function handleClick(button, status) {
    button.disabled = true;
    status.dataset.kind = "";
    status.textContent = "문서 확인 중";

    try {
      const state = await checkState(window.location.href);
      const response = await chrome.runtime.sendMessage({
        type: "START_CONVERSION",
        viewerUrl: window.location.href,
        state,
      });

      if (!response || !response.ok) {
        throw new Error(response && response.error ? response.error : "변환 탭을 열 수 없습니다.");
      }

      status.dataset.kind = "done";
      status.textContent = "변환 탭을 열었습니다";
    } catch (error) {
      status.dataset.kind = "error";
      status.textContent = error && error.message ? error.message : String(error);
      button.disabled = false;
    }
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
})();
