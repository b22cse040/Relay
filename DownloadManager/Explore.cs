using System;
using System.Collections.Generic;
using System.Text;

namespace DownloadManager
{
    internal class Explore
    {
        static async Task Main()
        {
            var uri = new Uri("https://arxiv.org/pdf/2309.08532");

            using HttpClient client = new HttpClient();

            using HttpResponseMessage response =
                await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);

            Console.WriteLine("===== Status =====");
            Console.WriteLine($"Status Code : {(int)response.StatusCode} ({response.StatusCode})");
            Console.WriteLine($"HTTP Version: {response.Version}");
            Console.WriteLine();

            Console.WriteLine("===== Response Headers =====");
            foreach (var header in response.Headers)
            {
                Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }

            Console.WriteLine();

            Console.WriteLine("===== Content Headers =====");
            foreach (var header in response.Content.Headers)
            {
                Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }

            Console.WriteLine();

            Console.WriteLine("===== Useful Properties =====");
            Console.WriteLine($"Content-Type        : {response.Content.Headers.ContentType}");
            Console.WriteLine($"Content-Length      : {response.Content.Headers.ContentLength}");
            Console.WriteLine($"Content-Disposition : {response.Content.Headers.ContentDisposition}");
            Console.WriteLine($"Last-Modified       : {response.Content.Headers.LastModified}");
        }
    }
}
