using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Globalization;
using System.Text.Json;
using System.Numerics;
using System.Net;

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
        private string _MetadataFilePath { get; set; } // Used to track NextID, to solve the persistence problem
        public int Visibility { get; set; }

        private bool _LogPretty { get; set; }

        public DownloadManagerContext(string? LogFilePath, int visibility = 1, bool logPretty = false)
        {
            Visibility = visibility;

            string RootDir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;

            string LogDir = Path.Combine(RootDir, "Logs");

            if (!Directory.Exists(LogDir))
            {
                Directory.CreateDirectory(LogDir);

                if (Visibility >= 2)
                {
                    Console.WriteLine($"Logging File Directory created at: {LogDir}");
                }
            }

            string currLogFile = Path.Combine(LogDir, "logs.jsonl");
            _LogFilePath = LogFilePath ?? currLogFile;
            _LogPretty = logPretty;

            if (!File.Exists(_LogFilePath))
            {
                /*
                File.Create() returns a FileStream and leaves the file open. If we don't use "using" then, file does not close and 
                no subsequent write can occur, so use using with File.Create();
                 */
                using (File.Create(_LogFilePath)) { }

                if (Visibility >= 2)
                {
                    Console.WriteLine($"Logging File created at: {_LogFilePath}");
                }
            }

            _MetadataFilePath = Path.Combine(LogDir, "DownloadManagerContextMetaData.json");

            if (!File.Exists(_MetadataFilePath))
            {
                using (File.Create(_MetadataFilePath)) { } ;

                if (Visibility >= 2)
                {
                    Console.WriteLine($"Logging File created at: {_MetadataFilePath}");
                }

                DownloadManagerMetadata metadata = new DownloadManagerMetadata();

                string DownloadManagerContextMetadataJSONString = JsonSerializer.Serialize(
                    metadata,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                File.WriteAllText(_MetadataFilePath, DownloadManagerContextMetadataJSONString);
            }
        }

        private DownloadItem CreateDownloadItem(Uri uri, string LocalPathDir, string? fileName)
        {
            string FileName = fileName ?? createHashedFileName(String.Concat(LocalPathDir, DateTime.Now.ToString()));
            string FileExtension = Path.GetExtension(uri.LocalPath);
            string FullFileName = String.Concat(FileName, FileExtension);
            string FilePath = Path.Combine(LocalPathDir, FullFileName);

            string metadataJson = File.ReadAllText(_MetadataFilePath);

            Console.WriteLine($"Metadata Path : {_MetadataFilePath}");
            Console.WriteLine($"MetaData Content : {metadataJson}");

            DownloadManagerMetadata metadata =
                JsonSerializer.Deserialize<DownloadManagerMetadata>(metadataJson)!;

            int nextID = metadata.ID;

            DownloadItem item = new DownloadItem(
                    id: nextID,
                    uri: uri,
                    filePath: FilePath
                );

            // Update the ID in the MetaData for the persistent new element
            metadata.ID++;

            string UpdatedJSONMetadata = JsonSerializer.Serialize(
                    metadata,

                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }
                );

            File.WriteAllText(_MetadataFilePath, UpdatedJSONMetadata);

            return item;
        }

        private string createHashedFileName(string Str)
        {
            var allowedSymbols = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();
            var hash = new char[6];

            for (int i = 0; i < Str.Length; i++)
            {
                hash[i % 6] = (char)(hash[i % 6] ^ Str[i]);
            }

            for (int i = 0; i < 6; i++)
            {
                hash[i] = allowedSymbols[hash[i] % allowedSymbols.Length];
            }

            return new string(hash);
        }

        public async Task DownloadTheItem(Uri uri, string LocalPathDir, string? fileName, string Mode = "ASync")
        {
            if (uri.Scheme != "http" && uri.Scheme != "https")
            {
                throw new Exception($"URI entered does not either HTTPS or HTTP based scheme, so try again!");
            }

            DownloadItem item = CreateDownloadItem(uri: uri, LocalPathDir: LocalPathDir, fileName: fileName);

            if (Mode.ToLower() == "async")
            {
                if (Visibility >= 2)
                    Console.WriteLine($"Starting download in {Mode} mode.");

                // Implement code to Download file from uri
                var httpClient = new HttpClient();

                using var downloadStream = await httpClient.GetStreamAsync(uri);
                using var fileStream = new FileStream(item.FilePath, FileMode.Create, FileAccess.Write);

                //await downloadStream.CopyToAsync(fileStream);
                byte[] buffer = new byte[8192];
                long downloadedBytes = 0;
                int bytesRead;

                while ((bytesRead = await downloadStream.ReadAsync(buffer)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                    downloadedBytes += bytesRead;
                }

                await fileStream.FlushAsync();

                item.Size = downloadedBytes;
                item.EndTime = DateTime.Now;

                if (Visibility >= 2)
                    Console.WriteLine($"Download complete in {(item.EndTime - item.StartTime)?.TotalSeconds:F2} seconds.");
            }

            LogDownloadedItem(item, Mode.ToLower());
        }

        private void LogDownloadedItem(DownloadItem item, string mode)
        {
            if (!File.Exists(_LogFilePath))
            {
                throw new FileNotFoundException("Log File does not exist!");
            }

            // Merge the JSON and then log it
            string jsonItem = JsonSerializer.Serialize(
                new
                {
                    Mode = mode,
                    Item = item,
                },

                new JsonSerializerOptions
                {
                    WriteIndented = _LogPretty
                }
             );

            File.AppendAllText(_LogFilePath, jsonItem + Environment.NewLine);

            if (Visibility >= 2)
            {
                Console.WriteLine($"Logged downloading action to Log File Path");
            }
        }
    }
}