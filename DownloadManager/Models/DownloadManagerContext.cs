using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;

namespace DownloadManager.Models
{
    internal class DownloadManagerMetadata
    {
        public int ID { get; set; }

        public DownloadManagerMetadata()
        {
            ID = 0;
        }
    }

    internal class DownloadManagerContext
    {
        private string _LogFilePath { get; set; }
        private string _MetadataFilePath { get; set; }
        public int Visibility { get; set; }

        private bool _LogPretty { get; set; }

        private readonly List<string> _extensions = new()
        {
            ".txt",
            ".pdf",
            ".png",
            ".jpg",
            ".jpeg"
        };

        public DownloadManagerContext(string? LogFilePath, int visibility = 1, bool logPretty = false)
        {
            Visibility = visibility;

            string RootDir = Directory.GetParent(Directory.GetCurrentDirectory())!.Parent!.Parent!.FullName;

            string LogDir = Path.Combine(RootDir, "Logs");

            if (!Directory.Exists(LogDir))
            {
                Directory.CreateDirectory(LogDir);

                if (Visibility >= 2)
                    Console.WriteLine($"Logging File Directory created at: {LogDir}");
            }

            string currLogFile = Path.Combine(LogDir, "logs.jsonl");
            _LogFilePath = LogFilePath ?? currLogFile;
            _LogPretty = logPretty;

            if (!File.Exists(_LogFilePath))
            {
                using (File.Create(_LogFilePath)) { }

                if (Visibility >= 2)
                    Console.WriteLine($"Logging File created at: {_LogFilePath}");
            }

            _MetadataFilePath = Path.Combine(LogDir, "DownloadManagerContextMetaData.json");

            if (!File.Exists(_MetadataFilePath))
            {
                using (File.Create(_MetadataFilePath)) { }

                if (Visibility >= 2)
                    Console.WriteLine($"Metadata File created at: {_MetadataFilePath}");

                DownloadManagerMetadata metadata = new();

                string json = JsonSerializer.Serialize(
                    metadata,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                File.WriteAllText(_MetadataFilePath, json);
            }
        }

        /// <summary>
        /// Destroys only the context and not the downloads lol xD
        /// </summary>
        public void ResetLogs()
        {
            // Try deleting log file
            try
            {
                File.Delete(_LogFilePath);
                if (Visibility >= 2)
                {
                    Console.WriteLine($"Log File at {_LogFilePath} deleted successfully.");
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"[ERROR]: File is in use or locked! {ex.Message}");
            }
            catch(UnauthorizedAccessException ex)
            {
                Console.WriteLine($"{ex.Message}");
            }

            // try deleting Metadata File
            try
            {
                File.Delete(_MetadataFilePath);
                if (Visibility >= 2)
                {
                    Console.WriteLine($"Log File at {_MetadataFilePath} deleted successfully.");
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"[ERROR]: File is in use or locked! {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
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

            string metadataJson = File.ReadAllText(_MetadataFilePath);

            DownloadManagerMetadata metadata =
                JsonSerializer.Deserialize<DownloadManagerMetadata>(metadataJson)!;

            int nextID = metadata.ID;

            DownloadItem item = new DownloadItem(
                id: nextID,
                uri: uri,
                filePath: FilePath
            );

            metadata.ID++;

            string updatedMetadata = JsonSerializer.Serialize(
                metadata,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            File.WriteAllText(_MetadataFilePath, updatedMetadata);

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
                LogDownloadedItem(item, Mode.ToLower());
            }
        }

        private void LogDownloadedItem(DownloadItem item, string mode)
        {
            if (!File.Exists(_LogFilePath))
            {
                throw new FileNotFoundException("Log File does not exist!");
            }

            string jsonItem = JsonSerializer.Serialize(
                new
                {
                    Mode = mode,
                    Item = item
                },
                new JsonSerializerOptions
                {
                    WriteIndented = _LogPretty
                });

            File.AppendAllText(_LogFilePath, jsonItem + Environment.NewLine);

            if (Visibility >= 2)
            {
                Console.WriteLine("Logged downloading action to Log File.");
            }
        }
    }
}