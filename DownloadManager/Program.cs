using DownloadManager.Models;

DownloadManagerContext dmc = new DownloadManagerContext(null, visibility: 2, logPretty: true);
Uri uri = new Uri("https://cdn.prod.website-files.com/652ca14a705e04da4e297536/65f001ac3234467613d39950_New%20website%20blog%20images%20(3).jpg");
string localPathDir = "C:\\Users\\Parth\\Downloads\\";
await dmc.DownloadTheItem(uri: uri, LocalPathDir: localPathDir, fileName: null);