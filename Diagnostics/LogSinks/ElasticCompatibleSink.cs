using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Storage;

namespace YasGMP.Diagnostics.LogSinks
{
    internal sealed class ElasticCompatibleSink : ILogSink, IDisposable
    {
        private readonly string? _url;
        private readonly string? _index;
        private readonly HttpClient _http = new();
        private readonly object _sync = new();
        private readonly string _bufferPath;
        private readonly string? _authHeader;
        private readonly string? _authScheme;
        private readonly Dictionary<string, string> _extraHeaders = new();
        private readonly System.Threading.Timer _flushTimer;
        /// <summary>
        /// Initializes a new instance of the ElasticCompatibleSink class.
        /// </summary>

        public ElasticCompatibleSink(IConfiguration cfg)
        {
            _url = cfg[DiagnosticsConstants.KeyElasticUrl];
            _index = cfg[DiagnosticsConstants.KeyElasticIndex];
            var dir = Path.Combine(FileSystem.AppDataDirectory, "logs");
            Directory.CreateDirectory(dir);
            _bufferPath = Path.Combine(dir, "elastic_buffer.ndjson");
            _authHeader = cfg["Diagnostics:Elastic:Authorization"];
            _authScheme = cfg["Diagnostics:Elastic:AuthScheme"]; // e.g., Bearer or ApiKey
            var apiKey = cfg["Diagnostics:Elastic:ApiKey"];
            var bearer = cfg["Diagnostics:Elastic:BearerToken"];
            if (!string.IsNullOrWhiteSpace(apiKey)) { _authHeader = apiKey; _authScheme = "ApiKey"; }
            if (!string.IsNullOrWhiteSpace(bearer)) { _authHeader = bearer; _authScheme = "Bearer"; }
            var headerPrefix = "Diagnostics:Elastic:Headers:";
            foreach (var kv in cfg.AsEnumerable())
            {
                if (kv.Key.StartsWith(headerPrefix, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(kv.Value))
                {
                    var name = kv.Key.Substring(headerPrefix.Length);
                    _extraHeaders[name] = kv.Value!;
                }
            }
            _flushTimer = new System.Threading.Timer(_ => TryFlushBuffer(), null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));
        }
        /// <summary>
        /// Gets or sets the name.
        /// </summary>

        public string Name => "elastic";
        /// <summary>
        /// Executes the write batch operation.
        /// </summary>

        public void WriteBatch(IReadOnlyList<DiagnosticEvent> batch)
        {
            if (batch == null || batch.Count == 0) return;
            var ndjson = new StringBuilder();
            foreach (var e in batch) ndjson.AppendLine(e.ToJson());

            if (string.IsNullOrWhiteSpace(_url))
            {
                lock (_sync) { File.AppendAllText(_bufferPath, ndjson.ToString()); }
                return;
            }

            // Fire and forget; buffer on failure
            _ = PostAsync(ndjson.ToString());
        }

        private async Task PostAsync(string ndjson)
        {
            try
            {
                using var content = new StringContent(ndjson, Encoding.UTF8, "application/x-ndjson");
                var uri = string.IsNullOrWhiteSpace(_index) ? _url! : _url!.TrimEnd('/') + "/" + _index!.Trim('/');
                var req = new HttpRequestMessage(HttpMethod.Post, uri) { Content = content };
                if (!string.IsNullOrWhiteSpace(_authHeader) && !string.IsNullOrWhiteSpace(_authScheme))
                {
                    req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(_authScheme, _authHeader);
                }
                foreach (var kv in _extraHeaders) req.Headers.TryAddWithoutValidation(kv.Key, kv.Value);

                var backoff = TimeSpan.FromSeconds(1);
                HttpResponseMessage? resp = null;
                for (int attempt = 0; attempt < 4; attempt++)
                {
                    try
                    {
                        resp = await _http.SendAsync(req).ConfigureAwait(false);
                        if (resp.IsSuccessStatusCode) return;
                    }
                    catch { }
                    await Task.Delay(backoff).ConfigureAwait(false);
                    backoff = TimeSpan.FromSeconds(Math.Min(backoff.TotalSeconds * 2, 30));
                }
                if (resp is null || !resp.IsSuccessStatusCode)
                {
                    lock (_sync) { File.AppendAllText(_bufferPath, ndjson); }
                }
            }
            catch { lock (_sync) { File.AppendAllText(_bufferPath, ndjson); } }
        }

        internal void TryFlushBuffer()
        {
            try
            {
                if (!File.Exists(_bufferPath)) return;
                string all;
                lock (_sync) { all = File.ReadAllText(_bufferPath); File.WriteAllText(_bufferPath, string.Empty); }
                if (string.IsNullOrWhiteSpace(all)) return;
                _ = PostAsync(all);
            }
            catch { }
        }

        // Exposed for DiagnosticsHub button
        /// <summary>
        /// Executes the flush buffer now operation.
        /// </summary>
        public void FlushBufferNow()
        {
            TryFlushBuffer();
        }
        /// <summary>
        /// Executes the dispose operation.
        /// </summary>

        public void Dispose()
        {
            try { _flushTimer?.Dispose(); } catch { }
            _http.Dispose();
        }
    }
}
