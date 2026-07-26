using DownloadManager.Models;

DownloadManagerContext dmc = new DownloadManagerContext(null, visibility: 2);
dmc.DownloadTheItem(Uri: "Placeholder", Size: 1024 * 1024 * 5, FileDstPath: "Placeholder");