# Architecture Decisions

## Why `GetAsync` Instead of `GetStreamAsync`?

Although `GetStreamAsync()` is memory-efficient, it only returns the response body as a `Stream`, making HTTP headers such as `Content-Type`, `Content-Length`, and `Content-Disposition` inaccessible. A download manager needs these headers to accurately determine the file type, file size, and (optionally) the server-provided filename before downloading.

Using

```csharp
HttpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead)
```

returns as soon as the response headers are available without buffering the entire response body into memory. This allows the download manager to:

- Validate the response using `EnsureSuccessStatusCode()`
- Determine the file extension from `Content-Type`
- Read the expected file size from `Content-Length`
- Stream the response directly to disk using `ReadAsStreamAsync()`

This preserves the same memory efficiency as `GetStreamAsync()` while exposing all required metadata.

```text
             GetStreamAsync()                     GetAsync(ResponseHeadersRead)
             ----------------                     -----------------------------
             HTTP Request                              HTTP Request
                   │                                        │
                   ▼                                        ▼
            Receive Stream                         Receive Headers
                   │                                        │
                   ▼                                        ▼
           Download File                      Inspect Content-Type
                                              Inspect Content-Length
                                              EnsureSuccessStatusCode()
                                                       │
                                                       ▼
                                            ReadAsStreamAsync()
                                                       │
                                                       ▼
                                               Stream File to Disk
```

---

## Tracking Download Size

The `Content-Length` header is optional and therefore cannot be relied upon to determine the size of every download.

Instead of assuming that the server provides this information, the download manager keeps track of the total number of bytes written to disk while streaming the response.

```csharp
long? contentLength = response.Content.Headers.ContentLength;

const int BUFFER_SIZE = 8192;
byte[] buffer = new byte[BUFFER_SIZE];

long totalDownloadedBytes = 0;
int bytesRead;

while ((bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
{
    await fileStream.WriteAsync(buffer, 0, bytesRead);
    totalDownloadedBytes += bytesRead;
}

item.Size = contentLength ?? totalDownloadedBytes;
```

This guarantees that the recorded download size is always accurate, even when the server omits the `Content-Length` header.

---

# Download Pipeline

The original implementation contained the entire download workflow inside a single method. As the project evolved to include retry policies, logging, metadata management and progress tracking, this became increasingly difficult to maintain.

The implementation was therefore refactored into a collection of smaller methods, each responsible for a single task.

```text
DownloadTheItem()
│
├── ValidateUri()
│
├── DownloadWithRetries()
│      │
│      ├── AttemptDownload()
│      │      │
│      │      ├── Send HTTP Request
│      │      ├── Read Response Headers
│      │      ├── Create DownloadItem
│      │      ├── DownloadToFile()
│      │      └── FinalizeDownload()
│      │
│      └── Calculate Retry Delay
│
└── Log Result
```

This separation follows the **Single Responsibility Principle (SRP)**. Each helper method owns one well-defined responsibility, making the implementation easier to understand, debug and extend.

---

## Retry Strategy

Network failures are often temporary. A request may fail because of:

- Temporary internet connectivity issues
- Server overload
- Timeouts
- Transient HTTP failures

Instead of immediately reporting these failures, the download manager retries the request using **exponential backoff**.

```text
delay = 2^attempt + random(0, jitter)
```

Example:

| Attempt | Delay (approx.) |
|---------:|----------------:|
| 1 | 2.1 s |
| 2 | 4.2 s |
| 3 | 8.0 s |
| 4 | 16.1 s |
| 5 | 32.2 s |

The exponential delay prevents repeatedly hammering a server that is already struggling, while the random jitter prevents multiple clients from retrying at exactly the same moment (commonly known as the **thundering herd problem**).

Only **transient failures** (such as HTTP or I/O exceptions) are retried. Validation failures, unsupported URI schemes and other permanent errors fail immediately.

---

## Why a Single `HttpClient`?

Rather than constructing a new `HttpClient` for every download, the service maintains a single shared instance.

```csharp
private static readonly HttpClient _httpClient = new();
```

`HttpClient` is designed to be reused throughout the application's lifetime. Reusing the same instance avoids repeatedly opening and closing sockets, reduces resource usage, and follows Microsoft's recommended best practices.

---

## Why Not Resume Downloads Using `Stream.Seek()`?

A common misconception is that an interrupted download can simply seek back to the previous position in the network stream.

```csharp
networkStream.Seek(...)
```

Unfortunately, HTTP response streams returned by `ReadAsStreamAsync()` are almost never seekable.

```csharp
networkStream.CanSeek == false
```

For this reason, retry attempts always establish a **new HTTP request** rather than attempting to rewind the existing stream.

True download resumption requires support for HTTP **Range Requests**, where the client:

1. Determines how many bytes have already been downloaded.
2. Sends a new request containing a `Range` header.
3. Appends the remaining bytes to the existing file.

Support for resumable downloads is intentionally left as future work.

---

## Why Stream Files Instead of Buffering Them?

Downloaded files are streamed directly from the network to disk using a fixed-size buffer.

```text
Network Stream
      │
      ▼
Read 8 KB
      │
      ▼
Write 8 KB
      │
      ▼
Repeat until EOF
```

Streaming avoids loading the entire file into memory, allowing the download manager to handle files ranging from a few kilobytes to several gigabytes while maintaining a small and predictable memory footprint.