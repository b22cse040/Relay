## Why `GetAsync` Instead of `GetStreamAsync`?

Although `GetStreamAsync()` is memory-efficient, it only returns the response body as a `Stream`, making HTTP headers such as `Content-Type`, `Content-Length`, and `Content-Disposition` inaccessible. A download manager needs these headers to accurately determine the file type, file size, and (optionally) the server-provided filename before downloading. Using `GetAsync(uri, HttpCompletionOption.ResponseHeadersRead)` returns as soon as the response headers are received without buffering the entire file into memory. We can inspect the headers, call `response.EnsureSuccessStatusCode()` to validate the response, and then obtain a streaming `Stream` via `response.Content.ReadAsStreamAsync()`, preserving the same memory efficiency as `GetStreamAsync()` while exposing all required metadata.

```text
                 GetStreamAsync()                          GetAsync(ResponseHeadersRead)
                 ----------------                          -----------------------------
        HTTP Request                               HTTP Request
              │                                          │
              ▼                                          ▼
       Receive Stream                           Receive Headers
              │                                          │
              ▼                                          ▼
      Download File                           Inspect Content-Type
                                              Inspect Content-Length
                                              EnsureSuccessStatusCode()
                                                       │
                                                       ▼
                                            ReadAsStreamAsync()
                                                       │
                                                       ▼
                                           Stream File to Disk
```

### Tracking size of the file downloaded
The Content-Length header is optional and may not always be present in an HTTP response. 
To accurately track download progress in all cases, a manual read/write loop using `NetworkStream.ReadAsync()` and `FileStream.WriteAsync()` is used. 
The number of bytes returned by each `ReadAsync()` call is accumulated to determine the total bytes downloaded.

```
long? contentLength = response.Content.Headers.ContentLength;

// contentLength may be null
const int BUFFER_SIZE = 81920;
byte[] buffer = new byte[BUFFER_SIZE]
long totalDownloadedBytes = 0;
int bytesRead;
while(bytesRead = (networkStream.ReadAsync(buffer, 0, buffer.Length)) > 0){
    fileStream.WriteAsync(buffer, 0, bytesRead);
    totalDownloadedBytes += bytesRead;
}

// In case the contentLength is null, it would fallback to totalDownloadedBytes.
item.Size = contentLength ?? totalDownloadedBytes;
```