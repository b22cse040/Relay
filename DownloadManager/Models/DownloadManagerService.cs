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
            string contentType)
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

            int nextID = metadata.ID;

            DownloadItem item = new DownloadItem(
                id: nextID,
                uri: uri,
                filePath: FilePath
            );

            metadata.ID++;

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
            Uri uri,
            string LocalPathDir,
            string? fileName,
            string Mode = "Async")
        {
            if (uri.Scheme != "http" && uri.Scheme != "https")
            {
                throw new Exception("URI entered does not use HTTP or HTTPS.");
            }

            DownloadItem? item = null;

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
                    contentType: contentType);

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
                while((bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalBytesDownloaded += bytesRead;
                }

                item.EndTime = DateTime.Now;
                item.Size = contentLength ?? totalBytesDownloaded;

                Console.WriteLine(
                        $"Download for {Path.GetFileName(item.FilePath)} complete in {(item.EndTime - item.StartTime)?.TotalSeconds:F2} seconds.");
            }

            if (item != null)
            {
                _Logger.LogDownloadedItem(item, Mode.ToLower());
            }
        }
    }
}