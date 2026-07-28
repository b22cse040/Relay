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

### Why not using 