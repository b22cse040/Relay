using DownloadManager.Models;

DownloadManagerService dmc = new DownloadManagerService(null, visibility: 1, logPretty: true);
//Uri uri = new Uri("https://cdn.prod.website-files.com/652ca14a705e04da4e297536/65f001ac3234467613d39950_New%20website%20blog%20images%20(3).jpg");
var uri = new Uri("https://arxiv.org/pdf/2309.08532");
string localPathDir = "C:\\Users\\Parth\\Downloads\\";
await dmc.DownloadTheItem(uri: uri, LocalPathDir: localPathDir, fileName: null);
// dmc.ResetLogs();

// 

//using HttpClient client = new HttpClient();

//using HttpResponseMessage response =
//    await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);

////response.EnsureSuccessStatusCode();
////var stream = await response.Content.ReadAsStreamAsync();

//Console.WriteLine("===== Status =====");
//Console.WriteLine($"Status Code : {(int)response.StatusCode} ({response.StatusCode})");
//Console.WriteLine($"HTTP Version: {response.Version}");
//Console.WriteLine();

//Console.WriteLine("===== Response Headers =====");
//foreach (var header in response.Headers)
//{
//    Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
//}

//Console.WriteLine();

//Console.WriteLine("===== Content Headers =====");
//foreach (var header in response.Content.Headers)
//{
//    Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
//}

//Console.WriteLine();

//Console.WriteLine("===== Useful Properties =====");
//Console.WriteLine($"Content-Type        : {response.Content.Headers.ContentType}");
//// Console.WriteLine($"Media-Type        : {response.Content.Headers.ContentType?.MediaType}");
//Console.WriteLine($"Content-Length      : {response.Content.Headers.ContentLength}");
//Console.WriteLine($"Content-Disposition : {response.Content.Headers.ContentDisposition}");
//Console.WriteLine($"Last-Modified       : {response.Content.Headers.LastModified}");