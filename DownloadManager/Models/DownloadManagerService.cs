using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace DownloadManager.Models
{
    internal class DownloadManagerService
    {
        private readonly DownloadManagerLogger _Logger;
        private static readonly HttpClient _httpClient = new();

        public int Visibility { get; set; }
        private List<string> _extensions = new List<string>
        {
            ".txt",".pdf",".png",".jpg",".jpeg"
        };

        public DownloadManagerService(
                string? LogFilePath,
                int visibility = 1,
                bool logPretty = false
            )
        {
            Visibility = visibility;
            _Logger = new DownloadManagerLogger(
                    logFilePath: LogFilePath ?? string.Empty,
                    visibility: visibility,
                    logPretty: logPretty
                );
        }

        public void ResetLogs() => _Logger.ResetLogs();

        private Uri ValidateUri(string uriStr, string localPathDir, string? fileName, string Mode)
        {
            if(!Uri.TryCreate(uriStr, UriKind.Absolute, out Uri? uri))
            {
                const string err = "Invalid-URI";
                var item = new DownloadItem(-1, null, "");
                if (Visibility >= 2)
                    Console.WriteLine($"[ERROR] err");

                _Logger.LogDownloadedItem(item, Mode.ToLower(), err);
                throw new ArgumentException(err);
            }

            if(uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                const string err = "URI entered does not use HTTP or HTTPS";
                var item = CreateDownloadItem(uri, localPathDir, fileName, "application/octet-stream", err);
                _Logger.LogDownloadedItem(item, Mode.ToLower(), err);
                throw new ArgumentException(err);
            }

            return uri;
        }

        private DownloadItem CreateDownloadItem(Uri uri, string localPathDir, string? fileName, string contentType, string? error)
        {
            string FileName = fileName ?? CreateHashedFileName($"{localPathDir}{DateTime.Now}");
            string Extension = "." + contentType.Split('/').Last().ToLowerInvariant();

            if (!_extensions.Contains(Extension))
            {
                throw new NotSupportedException($"Extension {Extension} is not supported");
            }

            string FullPath = Path.Combine(localPathDir, FileName + Extension);

            DownloadManagerMetaData metadata = _Logger.GetMetaData();
            int id = string.IsNullOrEmpty(error) ? metadata.ID : -1;

            DownloadItem item = new(id, uri, FullPath);

            if (string.IsNullOrEmpty(error))
                metadata.ID++;

            _Logger.UpdateMetaData(metadata);
            return item;
        }

        public async Task DownloadTheItem(
                string uriStr,
                string localPathDir,
                string? fileName,
                string mode = "Async",
                double jitter = 0.25,
                int retries = 5
            )
        {
            Uri uri = ValidateUri(uriStr, localPathDir, fileName, mode);

            if (!mode.Equals("async", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException("Only async mode is currently supported.");

            await DownloadWithRetries(uri, localPathDir, fileName, mode, retries, jitter);
        }

        private async Task DownloadWithRetries(
                Uri uri,
                string localPathDir, 
                string? fileName,
                string mode,
                int retries,
                double jitter
            )
        {
            Exception? last = null;

            for(int attempt = 0; attempt < retries; attempt++)
            {
                try
                {
                    await AttemptDownload(uri, localPathDir, fileName, mode);
                    return;
                }
                catch (HttpRequestException ex)
                {
                    last = ex;
                }
                catch (IOException ex)
                {
                    last = ex;
                }

                if (attempt == retries - 1)
                    break;

                double wait = GetNextRetryDelay(attempt + 1, jitter);

                if(Visibility >= 2)
                    Console.WriteLine($"[ERROR] {last!.Message} | Retrying in {wait:F2} seconds");

                await Task.Delay(TimeSpan.FromSeconds(wait));
            }

            throw last!;
        }

        private async Task AttemptDownload(
                Uri uri,
                string localPathDir,
                string? fileName,
                string mode
            )
        {
            if(Visibility >= 2)
                Console.WriteLine($"Starting download in {mode} mode.");

            using HttpResponseMessage response =
                await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode();

            string contentType = response.Content.Headers.ContentType?.MediaType
                                 ?? "application/octet-stream";

            long? contentLength = response.Content.Headers.ContentLength;

            if (Visibility >= 2)
            {
                Console.WriteLine($"Content-Type   : {contentType}");
                Console.WriteLine($"Content-Length : {contentLength ?? -1} bytes");
            }

            using Stream networkStream = await response.Content.ReadAsStreamAsync();

            if (Visibility >= 2)
            {
                Console.WriteLine($"Network Stream Seek Enabled : {networkStream.CanSeek}");

                if (networkStream.CanSeek)
                {
                    networkStream.Seek(0, SeekOrigin.Begin);
                    Console.WriteLine("Resuming download.");
                }
                else
                {
                    Console.WriteLine("Network stream is not seekable. Starting from beginning.");
                }
            }

            DownloadItem item = CreateDownloadItem(
                uri,
                localPathDir,
                fileName,
                contentType,
                string.Empty);

            long bytes = await DownloadToFile(networkStream, item.FilePath);

            FinalizeDownload(item, bytes, contentLength);

            _Logger.LogDownloadedItem(item, mode.ToLower(), string.Empty);
        }

        private async Task<long> DownloadToFile(Stream networkStream, string filePath)
        {
            const int BufferSize = 8192;
            byte[] buffer = new byte[BufferSize];
            long total = 0;
            int read;

            using FileStream fs = new(filePath, FileMode.Create, FileAccess.Write);

            while ((read = await networkStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fs.WriteAsync(buffer, 0, read);
                total += read;
            }

            return total;
        }

        private void FinalizeDownload(DownloadItem item, long bytes, long? contentLength)
        {
            item.EndTime = DateTime.Now;
            item.Size = contentLength ?? bytes;

            if (Visibility >= 2)
            {
                Console.WriteLine(
                    $"Download for {Path.GetFileName(item.FilePath)} complete in {(item.EndTime - item.StartTime)?.TotalSeconds:F2} seconds.");
            }
        }

        private double GetNextRetryDelay(int attempt, double jitter)
            => Math.Pow(2, attempt) + Random.Shared.NextDouble() * jitter;

        private string CreateHashedFileName(string fileName)
        {
            var chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();
            var hash = new char[6];

            for (int i = 0; i < fileName.Length; i++)
                hash[i % 6] = (char)(hash[i % 6] ^ fileName[i]);

            for (int i = 0; i < 6; i++)
                hash[i] = chars[hash[i] % chars.Length];

            return new string(hash);
        }
    }
}