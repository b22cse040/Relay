using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Globalization;
using System.Text.Json;
using System.Numerics;

namespace DownloadManager.Models
{
    internal class DownloadManagerContext
    {
        public int NextID { get; set; }
        private string _LogFilePath { get; set; }
        public int Visibility { get; set; }

        public DownloadManagerContext(string? LogFilePath, int nextID = 0, int visibility = 1)
        {
            NextID = nextID;
            Visibility = visibility;

            string RootDir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
            string LogDir = Path.Combine(RootDir, "Logs");
            if (!Directory.Exists(LogDir))
            {
                Directory.CreateDirectory(LogDir);
                if(Visibility >= 2)
                {
                    Console.WriteLine($"Logging File Directory created at: {LogDir}");
                }
            }


            String currFile = Path.Combine(LogDir, "logs.jsonl");
            _LogFilePath = LogFilePath ?? currFile;

            if (!File.Exists(_LogFilePath))
            {
                File.Create(_LogFilePath);
                if(Visibility >= 2)
                {
                    Console.WriteLine($"Logging File created at: {_LogFilePath}");
                }
            }
        }

        private DownloadItem CreateDownloadItem(string Uri, BigInteger Size, string FileDstPath)
        {
            return new DownloadItem(
                id: NextID++,
                uri: Uri,
                size: Size,
                filePath: FileDstPath
                );
        }

        public void DownloadTheItem(string Uri, BigInteger Size, string FileDstPath, string Mode = "Sync", bool logPretty = false)
        {
            DownloadItem item = CreateDownloadItem(Uri: Uri, Size: Size, FileDstPath: FileDstPath);
            if(Mode.ToLower() == "sync")
            {
                if(Visibility >= 2)
                    Console.WriteLine($"Starting download in {Mode} mode.");

                // Implement code to Download file from uri

                item.EndTime = DateTime.Now;
                if (Visibility >= 2)
                    Console.WriteLine($"Download complete in {(item.EndTime - item.StartTime)?.TotalSeconds:F2} seconds.");
            }

            LogDownloadedItem(item, Mode.ToLower(), logPretty);
        }

        private void LogDownloadedItem(DownloadItem item, string mode, bool logPretty = false)
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
                    WriteIndented = logPretty
                }
             );
            File.AppendAllText(_LogFilePath, jsonItem);
            if(Visibility >= 2)
            {
                Console.WriteLine($"Logged downloading action to Log File Path");
            }
        }
    }
}
