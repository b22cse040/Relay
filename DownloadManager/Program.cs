using DownloadManager.Models;

DownloadManagerService dmc = new DownloadManagerService(null, visibility: 2, logPretty: true);
string uri = "https://cdn.prod.website-files.com/652ca14a705e04da4e297536/65f001ac3234467613d39950_New%20website%20blog%20images%20(3).jpg";
// var uri = "Placeholder"; // Invalid-URI Logged in log file
// var FalseURI = "False";
string localPathDir = "C:\\Users\\Parth\\Downloads\\";
await dmc.DownloadTheItem(uriStr: uri, localPathDir: localPathDir, fileName: "LLM-Handbook");
dmc.ResetLogs();