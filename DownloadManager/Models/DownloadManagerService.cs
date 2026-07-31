using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;

namespace DownloadManager.Models
{

    internal class DownloadManagerService
    {
        private DownloadManagerLogger _Logger {  get; set; }
        public int Visibility { get; set; }

        private readonly List<string> _extensions = new()
        {
            ".txt",
            ".pdf",
            ".png",
            ".jpg",
            ".jpeg"
        };

        public DownloadManagerService(string? LogFilePath, int visibility = 1, bool logPretty = false)
        {
            Visibility = visibility;
            string logFilePath = LogFilePath ?? string.Empty;
            _Logger = new DownloadManagerLogger(logFilePath: logFilePath, visibility: visibility, logPretty: logPretty); ; 
            
        }

        private DownloadItem CreateDownloadItem(
            Uri uri,
            string LocalPathDir,
            string? fileName,
            string contentType,
            string? error
            )
        {
            string FileName = fileName ?? createHashedFileName($"{LocalPathDir}{DateTime.Now}");

            string FileExtension = "." + contentType.Split('/').Last().ToLower();

            if (!_extensions.Contains(FileExtension))
            {
                throw new Exception(
                    $"Extension {FileExtension} not found or not yet implemented! Try a different item.");
            }

            string FullFileName = FileName + FileExtension;
            string FilePath = Path.Combine(LocalPathDir, FullFileName);

            DownloadManagerMetaData metadata = _Logger.GetMetaData();

            int nextID = (string.IsNullOrEmpty(error)) ? metadata.ID : -1;

            DownloadItem item = new DownloadItem(
                id: nextID,
                uri: uri,
                filePath: FilePath
            );

            // If error is empty or null then Item created successfully, so we can increase the next ID.
            if(string.IsNullOrEmpty(error))
            {
                metadata.ID++;
            }

            _Logger.UpdateMetaData(metadata);

            return item;
        }

        private string createHashedFileName(string str)
        {
            var allowedSymbols = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();
            var hash = new char[6];

            for (int i = 0; i < str.Length; i++)
            {
                hash[i % 6] = (char)(hash[i % 6] ^ str[i]);
            }

            for (int i = 0; i < 6; i++)
            {
                hash[i] = allowedSymbols[hash[i] % allowedSymbols.Length];
            }

            return new string(hash);
        }

        public void ResetLogs()
        {
            _Logger.ResetLogs();
        }

        public async Task DownloadTheItem(
            string uriStr,
            string LocalPathDir,
            string? fileName,
            string Mode = "Async")
        {
            if(!Uri.TryCreate(uriStr, UriKind.Absolute, out Uri? uri))
            {
                string InvalidURIErrorStr = "Invalid-URI";

                DownloadItem InvalidURIItem = new DownloadItem
                (
                    id : -1,
                    filePath : "",
                    uri : null
                );

                if(Visibility >= 2)
                {
                    Console.WriteLine("[ERROR] Invalid-URI");
                }

                _Logger.LogDownloadedItem(InvalidURIItem, Mode.ToLower(), InvalidURIErrorStr);
                return;
            }

            if(uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) // (uri.Scheme != "http" && uri.Scheme != "https")
            {
                string HTTPErrorStr = "URI entered does not use HTTP or HTTPS.";
                DownloadItem errorItem = CreateDownloadItem(
                    uri: uri, fileName: fileName,
                    error: HTTPErrorStr,
                    LocalPathDir: LocalPathDir, contentType: "Error-String"
                    );
                if(Visibility >= 2)
                {
                    Console.WriteLine("[ERROR] URI entered does not use HTTP or HTTPS.");
                }

                _Logger.LogDownloadedItem(item: errorItem, mode: Mode, error: HTTPErrorStr);
                throw new Exception(HTTPErrorStr);
            }

            DownloadItem? item = null;

            try
            {
                if (Mode.Equals("async", StringComparison.OrdinalIgnoreCase))
                {
                    if (Visibility >= 2)
                        Console.WriteLine($"Starting download in {Mode} mode.");

                    using HttpClient httpClient = new();

                    using HttpResponseMessage response = await httpClient.GetAsync(
                        uri,
                        HttpCompletionOption.ResponseHeadersRead);

                    response.EnsureSuccessStatusCode();

                    string contentType =
                        response.Content.Headers.ContentType?.MediaType
                        ?? "application/octet-stream";

                    long? contentLength =
                        response.Content.Headers.ContentLength;

                    if (Visibility >= 2)
                    {
                        Console.WriteLine($"Content-Type   : {contentType}");
                        Console.WriteLine($"Content-Length : {contentLength ?? -1} bytes");
                    }

                    using Stream networkStream =
                        await response.Content.ReadAsStreamAsync();

                    item = CreateDownloadItem(
                        uri: uri,
                        LocalPathDir: LocalPathDir,
                        fileName: fileName,
                        contentType: contentType,
                        error: String.Empty);

                    // Using buffer to track totalBytesDownloaded in case ContentLength is null 
                    // on the HTTP Header
                    const int BufferSize = 8192;
                    byte[] buffer = new byte[BufferSize];

                    long totalBytesDownloaded = 0;
                    int bytesRead = 0;

                    using FileStream fileStream = new(
                        item.FilePath,
                        FileMode.Create,
                        FileAccess.Write);

                    // await networkStream.CopyToAsync(fileStream);
                    while ((bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalBytesDownloaded += bytesRead;
                    }

                    item.EndTime = DateTime.Now;
                    item.Size = contentLength ?? totalBytesDownloaded;

                    Console.WriteLine(
                            $"Download for {Path.GetFileName(item.FilePath)} complete in {(item.EndTime - item.StartTime)?.TotalSeconds:F2} seconds.");

                    if(item != null)
                    {
                        _Logger.LogDownloadedItem(item, Mode.ToLower(), String.Empty);
                    }
                }
            }

            catch (Exception ex)
            {
                _Logger.LogDownloadedItem(item, Mode.ToLower(), Convert.ToString(ex));
                if(Visibility >= 2)
                {
                    Console.WriteLine($"[ERROR] {Convert.ToString(ex)}");
                }
                throw;
            }
        }
    }
}