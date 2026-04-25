using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System.Xml.Linq;

internal sealed class UBDocGuiForm : Form
{
    private readonly string appDir;
    private readonly string defaultOutput;
    private readonly string logDir;
    private readonly string lastRunLog;
    private readonly string guiErrorLog;
    private readonly string settingsPath;

    private TextBox urlBox;
    private TextBox outputBox;
    private TextBox logBox;
    private Button browseButton;
    private Button runButton;
    private CheckBox keepImagesBox;

    private readonly StringBuilder stdoutBuffer = new StringBuilder();
    private readonly StringBuilder stderrBuffer = new StringBuilder();
    private readonly object bufferLock = new object();

    public UBDocGuiForm()
    {
        appDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        defaultOutput = Path.Combine(appDir, "PDF");
        logDir = Path.Combine(appDir, "Logs");
        lastRunLog = Path.Combine(logDir, "last_run.log");
        guiErrorLog = Path.Combine(logDir, "gui_error.log");
        settingsPath = Path.Combine(appDir, "settings.ini");

        ServicePointManager.SecurityProtocol =
            (SecurityProtocolType)768 | (SecurityProtocolType)3072 | SecurityProtocolType.Tls;

        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "KKU ubdoc PDF 저장";
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(720, 430);
        MinimumSize = new Size(640, 390);
        Font = new Font("Malgun Gothic", 9F);

        Label urlLabel = new Label();
        urlLabel.Text = "ubdoc URL";
        urlLabel.AutoSize = true;
        urlLabel.Location = new Point(16, 18);

        urlBox = new TextBox();
        urlBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        urlBox.Location = new Point(16, 42);
        urlBox.Size = new Size(670, 25);
        urlBox.Text = "";

        Label outputLabel = new Label();
        outputLabel.Text = "PDF 저장 폴더";
        outputLabel.AutoSize = true;
        outputLabel.Location = new Point(16, 82);

        outputBox = new TextBox();
        outputBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        outputBox.Location = new Point(16, 106);
        outputBox.Size = new Size(560, 25);
        outputBox.Text = LoadLastOutputDir();

        browseButton = new Button();
        browseButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        browseButton.Location = new Point(590, 104);
        browseButton.Size = new Size(96, 29);
        browseButton.Text = "찾기";
        browseButton.Click += BrowseButton_Click;

        keepImagesBox = new CheckBox();
        keepImagesBox.Location = new Point(16, 146);
        keepImagesBox.Size = new Size(220, 24);
        keepImagesBox.Text = "PNG 이미지 남기기";
        keepImagesBox.Checked = false;

        runButton = new Button();
        runButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        runButton.Location = new Point(556, 142);
        runButton.Size = new Size(130, 32);
        runButton.Text = "PDF 저장";
        runButton.Click += RunButton_Click;

        logBox = new TextBox();
        logBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        logBox.Location = new Point(16, 188);
        logBox.Size = new Size(670, 185);
        logBox.Multiline = true;
        logBox.ScrollBars = ScrollBars.Vertical;
        logBox.ReadOnly = true;

        Controls.Add(urlLabel);
        Controls.Add(urlBox);
        Controls.Add(outputLabel);
        Controls.Add(outputBox);
        Controls.Add(browseButton);
        Controls.Add(keepImagesBox);
        Controls.Add(runButton);
        Controls.Add(logBox);
    }

    private void BrowseButton_Click(object sender, EventArgs e)
    {
        string selectedPath = null;
        string initialPath = Directory.Exists(outputBox.Text) ? outputBox.Text : defaultOutput;

        try
        {
            selectedPath = ModernFolderPicker.Show(this.Handle, "PDF 저장 폴더 선택", initialPath);
        }
        catch
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "PDF 저장 폴더 선택";
                dialog.SelectedPath = initialPath;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    selectedPath = dialog.SelectedPath;
                }
            }
        }

        if (!string.IsNullOrEmpty(selectedPath))
        {
            outputBox.Text = selectedPath;
            SaveLastOutputDir(selectedPath);
        }
    }

    private void RunButton_Click(object sender, EventArgs e)
    {
        string url = urlBox.Text.Trim();
        string outputDir = outputBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(url))
        {
            MessageBox.Show(this, "ubdoc URL을 입력하세요.", "확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (string.IsNullOrWhiteSpace(outputDir))
        {
            MessageBox.Show(this, "PDF 저장 폴더를 입력하세요.", "확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        SaveLastOutputDir(outputDir);

        stdoutBuffer.Length = 0;
        stderrBuffer.Length = 0;
        logBox.Clear();
        AppendLog("변환 중입니다. 완료될 때까지 기다리세요.");
        SetInputsEnabled(false);

        RunConfig config = new RunConfig();
        config.ViewerUrl = url;
        config.OutputDir = outputDir;
        config.KeepImages = keepImagesBox.Checked;

        BackgroundWorker worker = new BackgroundWorker();
        worker.WorkerReportsProgress = true;
        worker.DoWork += Worker_DoWork;
        worker.ProgressChanged += Worker_ProgressChanged;
        worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
        worker.RunWorkerAsync(config);
    }

    private void Worker_DoWork(object sender, DoWorkEventArgs e)
    {
        BackgroundWorker worker = (BackgroundWorker)sender;
        RunConfig config = (RunConfig)e.Argument;
        e.Result = ConvertDocument(config, worker);
    }

    private ConversionResult ConvertDocument(RunConfig config, BackgroundWorker worker)
    {
        Dictionary<string, object> state = CheckState(config.ViewerUrl);
        string originalName = GetString(state, "file_realname");
        if (string.IsNullOrEmpty(originalName))
        {
            originalName = GetString(state, "file_name");
        }
        if (string.IsNullOrEmpty(originalName))
        {
            originalName = "ubdoc.pdf";
        }

        Directory.CreateDirectory(config.OutputDir);
        string outputPdf = AvailableOutputPath(Path.Combine(config.OutputDir, SafeFileName(originalName, "ubdoc.pdf")));
        if (!outputPdf.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            outputPdf = outputPdf + ".pdf";
        }

        string imageDir = Path.Combine(appDir, Path.GetFileNameWithoutExtension(outputPdf) + "_images");
        PageSet pageSet = LoadPages(state);
        Report(worker, "found " + pageSet.Pages.Count.ToString(CultureInfo.InvariantCulture) + " pages");

        List<string> images = DownloadImages(pageSet, imageDir, worker);
        CreatePdfFromImages(images, outputPdf, worker);
        Report(worker, "saved PDF: " + Path.GetFullPath(outputPdf));

        if (!config.KeepImages)
        {
            bool removed = CleanupImages(images, imageDir);
            if (removed)
            {
                Report(worker, "deleted images and folder: " + Path.GetFullPath(imageDir));
            }
            else
            {
                Report(worker, "deleted images; folder kept because it is not empty: " + Path.GetFullPath(imageDir));
            }
        }

        ConversionResult result = new ConversionResult();
        result.OutputPdf = outputPdf;
        return result;
    }

    private Dictionary<string, object> CheckState(string viewerUrl)
    {
        Uri uri = new Uri(viewerUrl);
        Dictionary<string, string> query = ParseQuery(uri.Query);
        string postData =
            "job=checkState" +
            "&id=" + Escape(query.ContainsKey("id") ? query["id"] : "") +
            "&tp=" + Escape(query.ContainsKey("tp") ? query["tp"] : "") +
            "&pg=" + Escape(query.ContainsKey("pg") ? query["pg"] : "") +
            "&item=" + Escape(query.ContainsKey("item") ? query["item"] : "") +
            "&fid=" + Escape(query.ContainsKey("fid") ? query["fid"] : "");

        Uri workerUri = new Uri(uri, "/local/ubdoc/worker.php");
        string json = Encoding.UTF8.GetString(RequestBytes(workerUri.ToString(), "POST", postData, viewerUrl));
        JavaScriptSerializer serializer = new JavaScriptSerializer();
        Dictionary<string, object> result = serializer.DeserializeObject(json) as Dictionary<string, object>;
        if (result == null)
        {
            throw new InvalidOperationException("worker.php 응답을 해석할 수 없습니다.");
        }

        string stateCode = GetString(result, "state_code");
        if (stateCode != "100")
        {
            throw new InvalidOperationException("문서가 아직 준비되지 않았습니다: " + json);
        }
        return result;
    }

    private PageSet LoadPages(Dictionary<string, object> state)
    {
        string fileId = GetString(state, "file_id");
        string organization = GetString(state, "organization_code");
        string owner = GetString(state, "owner_id");
        if (string.IsNullOrEmpty(owner))
        {
            owner = GetString(state, "fileuser");
        }

        if (string.IsNullOrEmpty(fileId) || string.IsNullOrEmpty(organization) || string.IsNullOrEmpty(owner))
        {
            throw new InvalidOperationException("문서 이미지 경로 정보를 찾을 수 없습니다.");
        }

        string baseUrl = "https://doc.coursemos.co.kr/" + organization + "/" + owner + "/" + fileId + "/";
        string xmlUrl = baseUrl + fileId + ".xml";
        string xmlText = Encoding.UTF8.GetString(RequestBytes(xmlUrl, "GET", null, null));
        XDocument document = XDocument.Parse(xmlText);

        List<PageInfo> pages = new List<PageInfo>();
        foreach (XElement page in document.Descendants("pdf"))
        {
            string path = ElementText(page, "path_html");
            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            PageInfo info = new PageInfo();
            info.Id = ParseInt(ElementText(page, "id"), pages.Count + 1);
            info.PathHtml = path;
            info.Width = ParseInt(ElementText(page, "w"), 0);
            info.Height = ParseInt(ElementText(page, "h"), 0);
            pages.Add(info);
        }

        if (pages.Count == 0)
        {
            throw new InvalidOperationException("XML 메타데이터에서 페이지 이미지를 찾을 수 없습니다.");
        }

        PageSet pageSet = new PageSet();
        pageSet.BaseUrl = baseUrl;
        pageSet.Pages = pages;
        return pageSet;
    }

    private List<string> DownloadImages(PageSet pageSet, string imageDir, BackgroundWorker worker)
    {
        Directory.CreateDirectory(imageDir);
        List<string> imagePaths = new List<string>();
        int digits = pageSet.Pages.Count.ToString(CultureInfo.InvariantCulture).Length;

        for (int i = 0; i < pageSet.Pages.Count; i++)
        {
            PageInfo page = pageSet.Pages[i];
            Uri imageUri = new Uri(new Uri(pageSet.BaseUrl), page.PathHtml);
            string fileName = (i + 1).ToString(new string('0', digits), CultureInfo.InvariantCulture) + ".png";
            string outputPath = Path.Combine(imageDir, fileName);

            if (!File.Exists(outputPath) || new FileInfo(outputPath).Length == 0)
            {
                byte[] imageBytes = RequestBytes(imageUri.ToString(), "GET", null, pageSet.BaseUrl);
                File.WriteAllBytes(outputPath, imageBytes);
            }

            imagePaths.Add(outputPath);
            Report(worker, "downloaded " + (i + 1).ToString(CultureInfo.InvariantCulture) + "/" +
                pageSet.Pages.Count.ToString(CultureInfo.InvariantCulture) + ": " + fileName);
        }

        return imagePaths;
    }

    private void CreatePdfFromImages(List<string> imagePaths, string outputPdf, BackgroundWorker worker)
    {
        List<PdfImage> pdfImages = new List<PdfImage>();
        try
        {
            for (int i = 0; i < imagePaths.Count; i++)
            {
                Report(worker, "building PDF page " + (i + 1).ToString(CultureInfo.InvariantCulture) + "/" +
                    imagePaths.Count.ToString(CultureInfo.InvariantCulture));
                pdfImages.Add(LoadPdfImage(imagePaths[i]));
            }

            SimplePdfWriter.Write(outputPdf, pdfImages);
        }
        finally
        {
            foreach (PdfImage image in pdfImages)
            {
                image.Dispose();
            }
        }
    }

    private PdfImage LoadPdfImage(string path)
    {
        using (Image original = Image.FromFile(path))
        {
            using (Bitmap bitmap = new Bitmap(original.Width, original.Height, PixelFormat.Format24bppRgb))
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    graphics.Clear(Color.White);
                    graphics.DrawImage(original, 0, 0, original.Width, original.Height);
                }

                using (MemoryStream stream = new MemoryStream())
                {
                    ImageCodecInfo codec = GetJpegCodec();
                    EncoderParameters parameters = new EncoderParameters(1);
                    parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 95L);
                    bitmap.Save(stream, codec, parameters);

                    PdfImage image = new PdfImage();
                    image.Width = original.Width;
                    image.Height = original.Height;
                    image.JpegBytes = stream.ToArray();
                    return image;
                }
            }
        }
    }

    private static ImageCodecInfo GetJpegCodec()
    {
        ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
        foreach (ImageCodecInfo codec in codecs)
        {
            if (codec.FormatID == ImageFormat.Jpeg.Guid)
            {
                return codec;
            }
        }
        throw new InvalidOperationException("JPEG encoder를 찾을 수 없습니다.");
    }

    private bool CleanupImages(List<string> imagePaths, string imageDir)
    {
        foreach (string imagePath in imagePaths)
        {
            try
            {
                File.Delete(imagePath);
            }
            catch
            {
            }
        }

        try
        {
            Directory.Delete(imageDir, false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static byte[] RequestBytes(string url, string method, string body, string referer)
    {
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = method;
        request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36";
        request.Timeout = 30000;
        request.ReadWriteTimeout = 30000;
        if (!string.IsNullOrEmpty(referer))
        {
            request.Referer = referer;
        }

        if (method == "POST")
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(body ?? "");
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = bodyBytes.Length;
            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(bodyBytes, 0, bodyBytes.Length);
            }
        }

        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
        using (Stream stream = response.GetResponseStream())
        using (MemoryStream memory = new MemoryStream())
        {
            stream.CopyTo(memory);
            return memory.ToArray();
        }
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (query.StartsWith("?"))
        {
            query = query.Substring(1);
        }

        string[] parts = query.Split('&');
        foreach (string part in parts)
        {
            if (part.Length == 0)
            {
                continue;
            }

            string[] pair = part.Split(new char[] { '=' }, 2);
            string key = Uri.UnescapeDataString(pair[0].Replace("+", " "));
            string value = pair.Length > 1 ? Uri.UnescapeDataString(pair[1].Replace("+", " ")) : "";
            result[key] = value;
        }
        return result;
    }

    private static string Escape(string value)
    {
        return Uri.EscapeDataString(value ?? "");
    }

    private static string GetString(Dictionary<string, object> data, string key)
    {
        if (!data.ContainsKey(key) || data[key] == null)
        {
            return "";
        }
        return Convert.ToString(data[key], CultureInfo.InvariantCulture);
    }

    private static string ElementText(XElement parent, string name)
    {
        XElement element = parent.Element(name);
        return element == null ? "" : element.Value;
    }

    private static int ParseInt(string text, int fallback)
    {
        int value;
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) ? value : fallback;
    }

    private static string SafeFileName(string name, string fallback)
    {
        string cleaned = Regex.Replace(name ?? "", "[<>:\"/\\\\|?*\\x00-\\x1f]", "_").Trim().TrimEnd('.');
        return string.IsNullOrEmpty(cleaned) ? fallback : cleaned;
    }

    private static string AvailableOutputPath(string path)
    {
        if (!File.Exists(path))
        {
            return path;
        }

        string directory = Path.GetDirectoryName(path);
        string stem = Path.GetFileNameWithoutExtension(path);
        string extension = Path.GetExtension(path);
        for (int i = 1; ; i++)
        {
            string candidate = Path.Combine(directory, stem + "_" + i.ToString(CultureInfo.InvariantCulture) + extension);
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }
    }

    private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
    {
        AppendLog((string)e.UserState);
    }

    private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
        SetInputsEnabled(true);

        string stdout;
        string stderr;
        lock (bufferLock)
        {
            stdout = stdoutBuffer.ToString();
            stderr = stderrBuffer.ToString();
        }
        WriteTextLog(lastRunLog, stdout);
        WriteTextLog(guiErrorLog, stderr);

        if (e.Error != null)
        {
            AppendLog(e.Error.ToString());
            WriteTextLog(guiErrorLog, e.Error.ToString());
            MessageBox.Show(this, e.Error.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        SaveLastOutputDir(outputBox.Text.Trim());
        AppendLog("Done.");
        MessageBox.Show(this, "PDF 저장이 완료되었습니다.", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void Report(BackgroundWorker worker, string message)
    {
        lock (bufferLock)
        {
            stdoutBuffer.AppendLine(message);
        }
        if (worker != null)
        {
            worker.ReportProgress(0, message);
        }
    }

    private void AppendLog(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }
        logBox.AppendText(message + Environment.NewLine);
    }

    private void SetInputsEnabled(bool enabled)
    {
        runButton.Enabled = enabled;
        browseButton.Enabled = enabled;
        urlBox.Enabled = enabled;
        outputBox.Enabled = enabled;
        keepImagesBox.Enabled = enabled;
    }

    private static void WriteTextLog(string path, string text)
    {
        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(path, text ?? "", new UTF8Encoding(true));
    }

    private string LoadLastOutputDir()
    {
        try
        {
            if (File.Exists(settingsPath))
            {
                string value = File.ReadAllText(settingsPath, Encoding.UTF8).Trim();
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }
        }
        catch
        {
        }
        return defaultOutput;
    }

    private void SaveLastOutputDir(string outputDir)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(outputDir))
            {
                File.WriteAllText(settingsPath, outputDir.Trim(), new UTF8Encoding(true));
            }
        }
        catch
        {
        }
    }

    private sealed class RunConfig
    {
        public string ViewerUrl;
        public string OutputDir;
        public bool KeepImages;
    }

    public sealed class ConversionResult
    {
        public string OutputPdf;
    }

    private sealed class PageSet
    {
        public string BaseUrl;
        public List<PageInfo> Pages;
    }

    private sealed class PageInfo
    {
        public int Id;
        public string PathHtml;
        public int Width;
        public int Height;
    }

    private sealed class PdfImage : IDisposable
    {
        public int Width;
        public int Height;
        public byte[] JpegBytes;

        public void Dispose()
        {
            JpegBytes = null;
        }
    }

    private static class SimplePdfWriter
    {
        public static void Write(string outputPath, List<PdfImage> images)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            using (FileStream stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            {
                int objectCount = 2 + images.Count * 3;
                long[] offsets = new long[objectCount + 1];
                WriteAscii(stream, "%PDF-1.4\n%\u00e2\u00e3\u00cf\u00d3\n");

                offsets[1] = stream.Position;
                WriteObject(stream, 1, "<< /Type /Catalog /Pages 2 0 R >>");

                StringBuilder kids = new StringBuilder();
                for (int i = 0; i < images.Count; i++)
                {
                    int pageObj = 3 + i * 3;
                    kids.Append(pageObj.ToString(CultureInfo.InvariantCulture)).Append(" 0 R ");
                }

                offsets[2] = stream.Position;
                WriteObject(stream, 2, "<< /Type /Pages /Kids [" + kids.ToString() + "] /Count " +
                    images.Count.ToString(CultureInfo.InvariantCulture) + " >>");

                for (int i = 0; i < images.Count; i++)
                {
                    PdfImage image = images[i];
                    int pageObj = 3 + i * 3;
                    int imageObj = pageObj + 1;
                    int contentObj = pageObj + 2;
                    string imageName = "Im" + (i + 1).ToString(CultureInfo.InvariantCulture);

                    offsets[pageObj] = stream.Position;
                    string page = "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 " +
                        image.Width.ToString(CultureInfo.InvariantCulture) + " " +
                        image.Height.ToString(CultureInfo.InvariantCulture) +
                        "] /Resources << /XObject << /" + imageName + " " +
                        imageObj.ToString(CultureInfo.InvariantCulture) +
                        " 0 R >> >> /Contents " + contentObj.ToString(CultureInfo.InvariantCulture) + " 0 R >>";
                    WriteObject(stream, pageObj, page);

                    offsets[imageObj] = stream.Position;
                    WriteAscii(stream, imageObj.ToString(CultureInfo.InvariantCulture) + " 0 obj\n");
                    WriteAscii(stream, "<< /Type /XObject /Subtype /Image /Width " +
                        image.Width.ToString(CultureInfo.InvariantCulture) + " /Height " +
                        image.Height.ToString(CultureInfo.InvariantCulture) +
                        " /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length " +
                        image.JpegBytes.Length.ToString(CultureInfo.InvariantCulture) + " >>\nstream\n");
                    stream.Write(image.JpegBytes, 0, image.JpegBytes.Length);
                    WriteAscii(stream, "\nendstream\nendobj\n");

                    string content = "q\n" +
                        image.Width.ToString(CultureInfo.InvariantCulture) + " 0 0 " +
                        image.Height.ToString(CultureInfo.InvariantCulture) + " 0 0 cm\n/" +
                        imageName + " Do\nQ\n";
                    byte[] contentBytes = Encoding.ASCII.GetBytes(content);

                    offsets[contentObj] = stream.Position;
                    WriteAscii(stream, contentObj.ToString(CultureInfo.InvariantCulture) + " 0 obj\n");
                    WriteAscii(stream, "<< /Length " + contentBytes.Length.ToString(CultureInfo.InvariantCulture) + " >>\nstream\n");
                    stream.Write(contentBytes, 0, contentBytes.Length);
                    WriteAscii(stream, "endstream\nendobj\n");
                }

                long xref = stream.Position;
                WriteAscii(stream, "xref\n0 " + (objectCount + 1).ToString(CultureInfo.InvariantCulture) + "\n");
                WriteAscii(stream, "0000000000 65535 f \n");
                for (int i = 1; i <= objectCount; i++)
                {
                    WriteAscii(stream, offsets[i].ToString("0000000000", CultureInfo.InvariantCulture) + " 00000 n \n");
                }
                WriteAscii(stream, "trailer\n<< /Size " + (objectCount + 1).ToString(CultureInfo.InvariantCulture) +
                    " /Root 1 0 R >>\nstartxref\n" + xref.ToString(CultureInfo.InvariantCulture) + "\n%%EOF\n");
            }
        }

        private static void WriteObject(FileStream stream, int number, string body)
        {
            WriteAscii(stream, number.ToString(CultureInfo.InvariantCulture) + " 0 obj\n" + body + "\nendobj\n");
        }

        private static void WriteAscii(FileStream stream, string text)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            stream.Write(bytes, 0, bytes.Length);
        }
    }

    private static class ModernFolderPicker
    {
        private const uint FOS_PICKFOLDERS = 0x00000020;
        private const uint FOS_FORCEFILESYSTEM = 0x00000040;
        private const uint FOS_PATHMUSTEXIST = 0x00000800;
        private const uint FOS_NOCHANGEDIR = 0x00000008;
        private const int ERROR_CANCELLED = unchecked((int)0x800704C7);

        public static string Show(IntPtr owner, string title, string initialPath)
        {
            IFileOpenDialog dialog = (IFileOpenDialog)new FileOpenDialogRCW();
            try
            {
                uint options;
                dialog.GetOptions(out options);
                dialog.SetOptions(options | FOS_PICKFOLDERS | FOS_FORCEFILESYSTEM | FOS_PATHMUSTEXIST | FOS_NOCHANGEDIR);
                dialog.SetTitle(title);

                if (!string.IsNullOrEmpty(initialPath) && Directory.Exists(initialPath))
                {
                    IShellItem folder;
                    Guid shellItemGuid = typeof(IShellItem).GUID;
                    SHCreateItemFromParsingName(initialPath, IntPtr.Zero, ref shellItemGuid, out folder);
                    dialog.SetFolder(folder);
                }

                int result = dialog.Show(owner);
                if (result == ERROR_CANCELLED)
                {
                    return null;
                }
                if (result != 0)
                {
                    Marshal.ThrowExceptionForHR(result);
                }

                IShellItem item;
                dialog.GetResult(out item);
                IntPtr pathPtr;
                item.GetDisplayName(SIGDN_FILESYSPATH, out pathPtr);
                try
                {
                    return Marshal.PtrToStringUni(pathPtr);
                }
                finally
                {
                    Marshal.FreeCoTaskMem(pathPtr);
                }
            }
            finally
            {
                Marshal.FinalReleaseComObject(dialog);
            }
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void SHCreateItemFromParsingName(
            [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            IntPtr pbc,
            ref Guid riid,
            [MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);

        private const uint SIGDN_FILESYSPATH = 0x80058000;

        [ComImport]
        [Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
        private class FileOpenDialogRCW
        {
        }

        [ComImport]
        [Guid("42f85136-db7e-439c-85f1-e4075d135fc8")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileDialog
        {
            [PreserveSig]
            int Show(IntPtr parent);
            void SetFileTypes(uint cFileTypes, IntPtr rgFilterSpec);
            void SetFileTypeIndex(uint iFileType);
            void GetFileTypeIndex(out uint piFileType);
            void Advise(IntPtr pfde, out uint pdwCookie);
            void Unadvise(uint dwCookie);
            void SetOptions(uint fos);
            void GetOptions(out uint pfos);
            void SetDefaultFolder(IShellItem psi);
            void SetFolder(IShellItem psi);
            void GetFolder(out IShellItem ppsi);
            void GetCurrentSelection(out IShellItem ppsi);
            void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
            void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
            void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
            void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
            void GetResult(out IShellItem ppsi);
            void AddPlace(IShellItem psi, int fdap);
            void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
            void Close(int hr);
            void SetClientGuid(ref Guid guid);
            void ClearClientData();
            void SetFilter(IntPtr pFilter);
        }

        [ComImport]
        [Guid("d57c7288-d4ad-4768-be02-9d969532d960")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileOpenDialog : IFileDialog
        {
            void GetResults(IntPtr ppenum);
            void GetSelectedItems(IntPtr ppsai);
        }

        [ComImport]
        [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
            void GetParent(out IShellItem ppsi);
            void GetDisplayName(uint sigdnName, out IntPtr ppszName);
            void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
            void Compare(IShellItem psi, uint hint, out int piOrder);
        }
    }
}

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--self-test")
        {
            return 0;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new UBDocGuiForm());
        return 0;
    }
}
