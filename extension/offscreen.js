(function () {
  "use strict";

  const DOWNLOAD_DIR = "TLS PDF Downloader";
  const JPEG_QUALITY = 0.95;
  const IMAGE_DOWNLOAD_CONCURRENCY = 4;
  const OBJECT_URL_TTL_MS = 60000;

  chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
    if (!message || message.target !== "offscreen" || message.type !== "CONVERT_JOB") {
      return false;
    }

    convertJob(message.job)
      .catch((error) => {
        notifyError(message.job && message.job.jobId, formatError(error));
      });

    sendResponse({ ok: true });
    return false;
  });

  async function convertJob(job) {
    if (!window.PDFLib || !window.PDFLib.PDFDocument) {
      throw new Error("pdf-lib을 불러올 수 없습니다.");
    }
    if (!job || !job.jobId || !job.state) {
      throw new Error("변환 작업 정보를 찾을 수 없습니다.");
    }

    notifyProgress(job, "문서 정보 읽는 중", 0, 1);
    const pageSet = await loadPageSet(job.state);
    const outputName = safePdfName(
      getString(job.state, "file_realname") ||
      getString(job.state, "file_name") ||
      "ubdoc.pdf"
    );

    notifyProgress(job, "이미지 다운로드 중", 0, pageSet.pages.length, outputName);
    const images = await downloadImages(job, pageSet, outputName);

    notifyProgress(job, "PDF 생성 중", 0, pageSet.pages.length, outputName);
    const pdfDoc = await window.PDFLib.PDFDocument.create();
    for (let index = 0; index < pageSet.pages.length; index += 1) {
      const image = images[index];
      const embedded = await pdfDoc.embedJpg(image.jpegBytes);
      const pdfPage = pdfDoc.addPage([image.width, image.height]);
      pdfPage.drawImage(embedded, {
        x: 0,
        y: 0,
        width: image.width,
        height: image.height,
      });
      notifyProgress(job, "PDF 생성 중", index + 1, pageSet.pages.length, outputName);
    }

    notifyProgress(job, "PDF 생성 중", pageSet.pages.length, pageSet.pages.length, outputName);
    const pdfBytes = await pdfDoc.save();
    const blob = new Blob([pdfBytes], { type: "application/pdf" });
    const objectUrl = URL.createObjectURL(blob);
    const filename = DOWNLOAD_DIR + "/" + outputName;

    setTimeout(() => URL.revokeObjectURL(objectUrl), OBJECT_URL_TTL_MS);

    await chrome.runtime.sendMessage({
      target: "background",
      type: "OFFSCREEN_COMPLETE",
      jobId: job.jobId,
      url: objectUrl,
      filename,
    });
  }

  async function downloadImages(job, pageSet, outputName) {
    const images = new Array(pageSet.pages.length);
    let nextIndex = 0;
    let completed = 0;
    const workerCount = Math.min(IMAGE_DOWNLOAD_CONCURRENCY, pageSet.pages.length);

    async function worker() {
      while (nextIndex < pageSet.pages.length) {
        const index = nextIndex;
        nextIndex += 1;

        const page = pageSet.pages[index];
        const imageUrl = new URL(page.pathHtml, pageSet.baseUrl).toString();
        const imageBytes = await requestBytes(imageUrl, pageSet.baseUrl);
        images[index] = await convertImageToJpeg(imageBytes);

        completed += 1;
        notifyProgress(job, "이미지 다운로드 중", completed, pageSet.pages.length, outputName);
      }
    }

    await Promise.all(Array.from({ length: workerCount }, () => worker()));
    return images;
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
      return await new Promise((resolve, reject) => {
        const image = new Image();
        image.onload = () => resolve(image);
        image.onerror = () => reject(new Error("이미지를 불러올 수 없습니다."));
        image.src = url;
      });
    } finally {
      URL.revokeObjectURL(url);
    }
  }

  function notifyProgress(job, status, current, total, filename) {
    chrome.runtime.sendMessage({
      target: "background",
      type: "OFFSCREEN_PROGRESS",
      jobId: job.jobId,
      status,
      current,
      total,
      filename,
    });
  }

  function notifyError(jobId, error) {
    chrome.runtime.sendMessage({
      target: "background",
      type: "OFFSCREEN_ERROR",
      jobId,
      error,
    });
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

  function formatError(error) {
    return error && error.message ? error.message : String(error);
  }
})();
