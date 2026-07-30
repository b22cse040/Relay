using System;
using System.Collections.Generic;
using System.Text;

namespace DownloadManager.Models
{
    internal class DownloadManagerMetaData
    {
        // Sole-purpose is to build the JSON formatted metadata to log in the file.
        // _MetadataFilePath is present in DownloadManagerLogger class.
        public int ID { get; set; }
        public DownloadManagerMetaData()
        {
            ID = 0;
        }
    }
}
