using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace DownloadManager.Models
{
    internal class DownloadItem
    {
        public int ID {  get; set; }
        public string URI { get; set; }
        public BigInteger Size { get; set; } // In Bytes
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string FilePath { get; set; }

        public DownloadItem(int id, string uri, BigInteger size, string filePath)
        {
            ID = id;
            URI = uri;
            Size = size;
            
            DateTime startTime = DateTime.Now;
            StartTime = startTime;
            FilePath = filePath;
        }
    }
}
