(function () {
  "use strict";

  const JOB_KEY_PREFIX = "job:";
  const DOWNLOAD_DIR = "TLS PDF Downloader";
  const JPEG_QUALITY = 0.95;

  const summaryEl = document.getElementById("summary");
  const statusEl = document.getElementById("status");
  const countEl = document.getElementById("count");
  const progressEl = document.getElementById("progress");
  const logEl = document.getElementById("log");
  const closeButton = document.getElementById("closeButton");

  closeButton.addEventListener("click", () => window.close());
  document.addEventListener("DOMContentLoaded", run);

  async function run() {
    try {
      if (!window.PDFLib || !window.PDFLib.PDFDocument) {
        throw new Error("pdf-lib을 불러올 수 없습니다.");
      }

      const jobId = getJobId();
      if (!jobId) {
        throw new Error("변환 작업 정보를 찾을 수 없습니다.");
      }

      const jobKey = JOB_KEY_PREFIX + jobId;
      const store = getJobStore();
      const data = await store.get(jobKey);
      const job = data[jobKey];
      if (!job || !job.state) {
        throw new Error("변환 작업이 만료되었거나 존재하지 않습니다.");
      }

      await convertJob(job);
      await store.remove(jobKey);
    } catch (error) {
      setStatus("실패", 0, 1);
      appendLog("error: " + formatError(error));
      summaryEl.textContent = formatError(error);
      closeButton.hidden = false;
    }
  }

  async function convertJob(job) {
    const pageSet = await loadPageSet(job.state);
    const outputName = safePdfName(
      getString(job.state, "file_realname") ||
      getString(job.state, "file_name") ||
      "ubdoc.pdf"
    );

    summaryEl.textContent = outputName + " / " + pageSet.pages.length + "페이지";
    progressEl.max = pageSet.pages.length + 1;
    setStatus("이미지 다운로드 중", 0, progressEl.max);
    appendLog("found " + pageSet.pages.length + " pages");

    const pdfDoc = await window.PDFLib.PDFDocument.create();

    for (let index = 0; index < pageSet.pages.length; index += 1) {
      const page = pageSet.pages[index];
      const imageUrl = new URL(page.pathHtml, pageSet.baseUrl).toString();
      const step = index + 1;

      setStatus("이미지 다운로드 중", step, progressEl.max);
      appendLog("download " + step + "/" + pageSet.pages.length + ": " + imageUrl);

      const imageBytes = await requestBytes(imageUrl, pageSet.baseUrl);
      const image = await convertImageToJpeg(imageBytes);
      const embedded = await pdfDoc.embedJpg(image.jpegBytes);
      const pdfPage = pdfDoc.addPage([image.width, image.height]);
      pdfPage.drawImage(embedded, {
        x: 0,
        y: 0,
        width: image.width,
        height: image.height,
      });
    }

    setStatus("PDF 생성 중", pageSet.pages.length, progressEl.max);
    appendLog("building PDF");
    const pdfBytes = await pdfDoc.save();

    const blob = new Blob([pdfBytes], { type: "application/pdf" });
    const objectUrl = URL.createObjectURL(blob);
    const filename = DOWNLOAD_DIR + "/" + outputName;

    try {
      const downloadId = await chrome.downloads.download({
        url: objectUrl,
        filename,
        conflictAction: "uniquify",
        saveAs: false,
      });

      setStatus("완료", progressEl.max, progressEl.max);
      appendLog("saved PDF: " + filename);
      appendLog("download id: " + downloadId);
      summaryEl.textContent = "다운로드가 시작되었습니다: " + filename;
      closeButton.hidden = false;
    } finally {
      setTimeout(() => URL.revokeObjectURL(objectUrl), 30000);
    }
  }

  async function loadPageSet(state) {
    const fileId = getString(state, "file_id");
    const organization = getString(state, "organization_code");
    const owner = getString(state, "owner_id") || getString(state, "fileuser");

    if (!fileId || !organization || !owner) {
      throw new Error("문서 이미지 경로 정보를 찾을 수 없습니다.");
    }

    const baseUrl =
      "https://doc.coursemos.co.kr/" +
      encodePathPart(organization) + "/" +
      encodePathPart(owner) + "/" +
      encodePathPart(fileId) + "/";
    const xmlUrl = baseUrl + encodePathPart(fileId) + ".xml";

    appendLog("load XML: " + xmlUrl);
    const xmlBytes = await requestBytes(xmlUrl, null);
    const xmlText = new TextDecoder("utf-8").decode(xmlBytes);
    const documentXml = new DOMParser().parseFromString(xmlText, "application/xml");

    const parserError = documentXml.querySelector("parsererror");
    if (parserError) {
      throw new Error("XML 메타데이터를 해석할 수 없습니다.");
    }

    const pages = Array.from(documentXml.querySelectorAll("pdf"))
      .map((node, index) => ({
        id: parseInteger(readElementText(node, "id"), index + 1),
        pathHtml: readElementText(node, "path_html"),
        width: parseInteger(readElementText(node, "w"), 0),
        height: parseInteger(readElementText(node, "h"), 0),
      }))
      .filter((page) => page.pathHtml);

    if (pages.length === 0) {
      throw new Error("XML 메타데이터에서 페이지 이미지를 찾을 수 없습니다.");
    }

    return {
      baseUrl,
      pages,
    };
  }

  async function requestBytes(url, referer) {
    const request = {
      method: "GET",
      credentials: "include",
    };

    if (referer) {
      request.referrer = referer;
    }

    const response = await fetch(url, request);

    if (!response.ok) {
      throw new Error("요청 실패: HTTP " + response.status + " / " + url);
    }

    return new Uint8Array(await response.arrayBuffer());
  }

  async function convertImageToJpeg(imageBytes) {
    const blob = new Blob([imageBytes], { type: "image/png" });
    const bitmap = await loadBitmap(blob);
    const canvas = document.createElement("canvas");
    canvas.width = bitmap.width;
    canvas.height = bitmap.height;

    const context = canvas.getContext("2d");
    context.fillStyle = "#ffffff";
    context.fillRect(0, 0, canvas.width, canvas.height);
    context.drawImage(bitmap, 0, 0);

    if (typeof bitmap.close === "function") {
      bitmap.close();
    }

    const jpegBlob = await new Promise((resolve) => {
      canvas.toBlob(resolve, "image/jpeg", JPEG_QUALITY);
    });

    if (!jpegBlob) {
      throw new Error("이미지를 JPEG로 변환할 수 없습니다.");
    }

    return {
      width: canvas.width,
      height: canvas.height,
      jpegBytes: new Uint8Array(await jpegBlob.arrayBuffer()),
    };
  }

  async function loadBitmap(blob) {
    if (typeof createImageBitmap === "function") {
      return createImageBitmap(blob);
    }

    const url = URL.createObjectURL(blob);
    try {
      const image = await new Promise((resolve, reject) => {
        const img = new Image();
        img.onload = () => resolve(img);
        img.onerror = () => reject(new Error("이미지를 불러올 수 없습니다."));
        img.src = url;
      });

      return image;
    } finally {
      URL.revokeObjectURL(url);
    }
  }

  function getJobId() {
    const hash = window.location.hash.startsWith("#")
      ? window.location.hash.slice(1)
      : window.location.hash;
    return new URLSearchParams(hash).get("jobId");
  }

  function getJobStore() {
    return chrome.storage.session || chrome.storage.local;
  }

  function getString(data, key) {
    if (!data || data[key] === undefined || data[key] === null) {
      return "";
    }
    return String(data[key]);
  }

  function readElementText(node, tagName) {
    const element = node.getElementsByTagName(tagName)[0];
    return element && element.textContent ? element.textContent.trim() : "";
  }

  function parseInteger(value, fallback) {
    const parsed = Number.parseInt(value, 10);
    return Number.isFinite(parsed) ? parsed : fallback;
  }

  function encodePathPart(value) {
    return String(value)
      .split("/")
      .map((part) => encodeURIComponent(part))
      .join("/");
  }

  function safePdfName(name) {
    let safe = String(name || "ubdoc.pdf")
      .replace(/[<>:"/\\|?*\x00-\x1F]/g, "_")
      .replace(/\s+/g, " ")
      .trim();

    safe = safe.replace(/^\.+/, "").replace(/[. ]+$/, "");
    if (!safe) {
      safe = "ubdoc";
    }
    if (!/\.pdf$/i.test(safe)) {
      safe += ".pdf";
    }
    return safe;
  }

  function setStatus(text, value, max) {
    statusEl.textContent = text;
    progressEl.value = value;
    progressEl.max = max || 1;
    countEl.textContent = value + " / " + progressEl.max;
  }

  function appendLog(text) {
    const stamp = new Date().toLocaleTimeString("ko-KR", { hour12: false });
    logEl.textContent += "[" + stamp + "] " + text + "\n";
    logEl.scrollTop = logEl.scrollHeight;
  }

  function formatError(error) {
    return error && error.message ? error.message : String(error);
  }
})();
