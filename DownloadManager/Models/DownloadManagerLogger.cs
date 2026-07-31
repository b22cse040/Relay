using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.Json;

namespace DownloadManager.Models
{
    internal class DownloadManagerLogger
    {
        private string _LogFilePath { get; set; }
        private string _MetaDataFilePath { get; set; }
        private bool _LogPretty { get; set; }
        private int _Visibility { get; set; }

        public DownloadManagerLogger(int visibility, string logFilePath = "", bool logPretty = false)
        {
            _Visibility = visibility;
            // If filePath is empty or null, the logFilePath will be set to {Root-Dir}/Logs
            if (string.IsNullOrEmpty(logFilePath))
            {
                string RootDir = Directory.GetParent(Directory.GetCurrentDirectory())!.Parent!.Parent!.FullName;
                string LogDir = Path.Combine(RootDir, "Logs");

                if (!Directory.Exists(LogDir))
                {
                    Directory.CreateDirectory(LogDir);
                    if (_Visibility >= 2)
                    {
                        Console.WriteLine($"Logging File Directory created at {LogDir}");
                    }

                }

                string currLogFile = Path.Combine(LogDir, "logs.jsonl");
                _LogFilePath = currLogFile;
                _LogPretty = logPretty;

                if (!File.Exists(_LogFilePath))
                {
                    using (File.Create(_LogFilePath)) { }

                    if (_Visibility >= 2)
                    {
                        Console.WriteLine($"Logging File Created at {_LogFilePath}");
                    }
                }

                _MetaDataFilePath = Path.Combine(LogDir, "DownloadManagerContextMetaData.json");

                if (!File.Exists(_MetaDataFilePath))
                {
                    using (File.Create(_MetaDataFilePath)) { }

                    if (_Visibility >= 2)
                    {
                        Console.WriteLine($"Metadata File Created at: {_MetaDataFilePath}");
                    }

                    DownloadManagerMetaData metadata = new();

                    UpdateMetaData(metadata);
                }
            }

            else
            {
                _LogPretty = logPretty;

                _LogFilePath = logFilePath;

                string? logDir = Path.GetDirectoryName(_LogFilePath);

                if (string.IsNullOrEmpty(logDir))
                {
                    logDir = Directory.GetCurrentDirectory();
                }

                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);

                    if (_Visibility >= 2)
                    {
                        Console.WriteLine($"Logging File Directory created at {logDir}");
                    }
                }

                if (!File.Exists(_LogFilePath))
                {
                    using (File.Create(_LogFilePath)) { }

                    if (_Visibility >= 2)
                    {
                        Console.WriteLine($"Logging File Created at {_LogFilePath}");
                    }
                }

                _MetaDataFilePath = Path.Combine(logDir, "DownloadManagerContextMetaData.json");

                if (!File.Exists(_MetaDataFilePath))
                {
                    using (File.Create(_MetaDataFilePath)) { }

                    if (_Visibility >= 2)
                    {
                        Console.WriteLine($"Metadata File Created at: {_MetaDataFilePath}");
                    }

                    DownloadManagerMetaData metadata = new();
                    UpdateMetaData(metadata);
                }
            }

        }

        /// <summary>
        /// Deletes the file, and not the downloads. 
        /// </summary>
        /// <param name="filePath"></param>
        private void DeleteFile(string filePath)
        {
            try
            {
                File.Delete(filePath);
                if (_Visibility >= 2)
                {
                    Console.WriteLine($"Log File at {_LogFilePath} deleted successfully");
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

            return;
        }

        public void ResetLogs()
        {
            DeleteFile(_LogFilePath);
            DeleteFile(_MetaDataFilePath);
        }

        public DownloadManagerMetaData GetMetaData()
        {
            string metadataJson = File.ReadAllText(_MetaDataFilePath);
            DownloadManagerMetaData metaData =
                JsonSerializer.Deserialize<DownloadManagerMetaData>(metadataJson);

            return metaData;
        }

        public void UpdateMetaData(DownloadManagerMetaData newMetadata)
        {
            string jsonMetaData = JsonSerializer.Serialize(
                    newMetadata,
                    new JsonSerializerOptions
                    {
                        WriteIndented = _LogPretty
                    }
                );

            File.WriteAllText(_MetaDataFilePath, jsonMetaData);
        }

        public void LogDownloadedItem(DownloadItem item, string mode, string error)
        {
            if (!File.Exists(_LogFilePath))
            {
                throw new FileNotFoundException("Log File does not exist!");
            }

            string jsonItem = JsonSerializer.Serialize(
                new
                {
                    Mode = mode,
                    Item = item,
                    Error = error
                },
                new JsonSerializerOptions
                {
                    WriteIndented = _LogPretty
                });

            File.AppendAllText(_LogFilePath, jsonItem + Environment.NewLine);
            if(_Visibility >= 2)
            {
                Console.WriteLine("Logged downloading action to Log File.");
            }
        }
    }
}
