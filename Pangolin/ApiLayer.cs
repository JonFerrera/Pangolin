using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Pangolin
{
    public class ApiLayer
    {
        private static ConcurrentDictionary<Uri, string> cachedGets = new ConcurrentDictionary<Uri, string>();

        private const string _jsonMediaType = "application/json";

        private static WebProxy _webProxy;
        private static HttpClientHandler _httpClientHandler;
        private static HttpClient _httpClient;
        private static CredentialCache _credentialCache;
        private static readonly MediaTypeWithQualityHeaderValue _jsonMediaTypeHeaderValue = new MediaTypeWithQualityHeaderValue(_jsonMediaType);

        private void SetCredentialCache(Uri baseAddress, string username, string password)
        {
            NetworkCredential networkCredential = new NetworkCredential(username, password);

            _credentialCache = new CredentialCache()
            {
                { baseAddress, "Basic", networkCredential },
                { baseAddress, "Digest", networkCredential },
                { baseAddress, "NTLM", networkCredential }
            };
        }

        public ApiLayer(Uri baseAddress)
        {
            if (baseAddress == null) { throw new ArgumentNullException(nameof(baseAddress)); }

            _httpClientHandler = new HttpClientHandler();
            _httpClient = new HttpClient(_httpClientHandler)
            {
                BaseAddress = baseAddress
            };
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(_jsonMediaTypeHeaderValue);
        }

        public ApiLayer(Uri baseAddress, string username, string password)
        {
            if (baseAddress == null) { throw new ArgumentNullException(nameof(baseAddress)); }
            if (username == null) { throw new ArgumentNullException(nameof(username)); }
            if (password == null) { throw new ArgumentNullException(nameof(password)); }

            SetCredentialCache(baseAddress, username, password);

            _httpClientHandler = new HttpClientHandler()
            {
                Credentials = _credentialCache
            };
            _httpClient = new HttpClient(_httpClientHandler)
            {
                BaseAddress = baseAddress
            };
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(_jsonMediaTypeHeaderValue);
        }

        public ApiLayer(Uri baseAddress, Uri proxyAddress)
        {
            if (baseAddress == null) { throw new ArgumentNullException(nameof(baseAddress)); }
            if (proxyAddress == null) { throw new ArgumentNullException(nameof(proxyAddress)); }

            _webProxy = new WebProxy(proxyAddress, true);
            _httpClientHandler = new HttpClientHandler()
            {
                Proxy = _webProxy,
                UseProxy = true
            };
            _httpClient = new HttpClient(_httpClientHandler)
            {
                BaseAddress = baseAddress
            };

            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(_jsonMediaTypeHeaderValue);
        }

        public ApiLayer(Uri baseAddress, Uri proxyAddress, string username, string password)
        {
            if (baseAddress == null) { throw new ArgumentNullException(nameof(baseAddress)); }
            if (proxyAddress == null) { throw new ArgumentNullException(nameof(proxyAddress)); }
            if (username == null) { throw new ArgumentNullException(nameof(username)); }
            if (password == null) { throw new ArgumentNullException(nameof(password)); }

            SetCredentialCache(baseAddress, username, password);

            _webProxy = new WebProxy(proxyAddress, true);
            _httpClientHandler = new HttpClientHandler()
            {
                Credentials = _credentialCache,
                Proxy = _webProxy,
                UseProxy = true
            };
            _httpClient = new HttpClient(_httpClientHandler)
            {
                BaseAddress = baseAddress
            };

            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(_jsonMediaTypeHeaderValue);
        }

        public async Task<HttpStatusCode> DeleteAsync(Uri apiUri, CancellationToken cancellationToken)
        {
            HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest;

            try
            {
                using (HttpResponseMessage httpResponseMessage = await _httpClient.DeleteAsync(apiUri, cancellationToken))
                {
                    httpStatusCode = httpResponseMessage.StatusCode;
                }
            }
            catch (InvalidOperationException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (HttpRequestException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }

            return httpStatusCode;
        }

        public async Task<string> GetAsync(Uri apiUri, CancellationToken cancellationToken)
        {
            string json = ConfigurationLayer.EmptyJson;

            if (cachedGets.TryGetValue(apiUri, out string cachedJson))
            {
                json = cachedJson;
            }
            else
            {
                try
                {
                    using (HttpResponseMessage httpResponseMessage = await _httpClient.GetAsync(apiUri, cancellationToken))
                    {
                        if (httpResponseMessage.IsSuccessStatusCode)
                        {
                            json = await httpResponseMessage.Content.ReadAsStringAsync();
                            cachedGets.TryAdd(apiUri, json);
                        }
                    }
                }
                catch (HttpRequestException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            }

            return json;
        }

        public async Task<HttpStatusCode> HeadAsync(Uri apiUri, CancellationToken cancellationToken)
        {
            HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest;

            using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Head, apiUri))
            {
                try
                {
                    using (HttpResponseMessage httpResponseMessage = await _httpClient.SendAsync(httpRequestMessage, cancellationToken))
                    {
                        httpStatusCode = httpResponseMessage.StatusCode;
                    }
                }
                catch (InvalidOperationException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
                catch (HttpRequestException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            }

            return httpStatusCode;
        }

        public async Task<string[]> OptionsAsync(Uri apiUri, CancellationToken cancellationToken)
        {
            string[] options = new string[] { };

            using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Options, apiUri))
            {
                try
                {
                    using (HttpResponseMessage httpResponseMessage = await _httpClient.SendAsync(httpRequestMessage, cancellationToken))
                    {
                        if (httpResponseMessage.IsSuccessStatusCode)
                        {
                            options = httpResponseMessage.Content.Headers.Allow.ToArray();
                        }
                    }
                }
                catch (ArgumentNullException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
                catch (InvalidOperationException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
                catch (HttpRequestException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            }

            return options;
        }

        public async Task<(HttpStatusCode statusCode, Uri location, string content)> PostAsync(Uri apiUri, CancellationToken cancellationToken)
        {
            using (HttpResponseMessage httpResponseMessage = await _httpClient.PostAsync(apiUri, null, cancellationToken))
            {
                try
                {
                    httpResponseMessage.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
                string returnData = await httpResponseMessage.Content.ReadAsStringAsync();
                return (httpResponseMessage.StatusCode, httpResponseMessage.Headers.Location, returnData);
            }
        }

        public async Task<(HttpStatusCode statusCode, Uri location, string content)> PostAsync(Uri apiUri, string data, CancellationToken cancellationToken)
        {
            StringContent stringContent = new StringContent(data, ConfigurationLayer.DefaultEncoding, _jsonMediaType);
            using (HttpResponseMessage httpResponseMessage = await _httpClient.PostAsync(apiUri, stringContent, cancellationToken))
            {
                try
                {
                    httpResponseMessage.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
                string returnData = await httpResponseMessage.Content.ReadAsStringAsync();
                return (httpResponseMessage.StatusCode, httpResponseMessage.Headers.Location, returnData);
            }
        }

        public async Task<(HttpStatusCode statusCode, Uri location)> PutAsync(Uri apiUri, string data, CancellationToken cancellationToken)
        {
            StringContent stringContent = new StringContent(data, ConfigurationLayer.DefaultEncoding, _jsonMediaType);
            using (HttpResponseMessage httpResponseMessage = await _httpClient.PutAsync(apiUri, stringContent, cancellationToken))
            {
                try
                {
                    httpResponseMessage.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }

                return (httpResponseMessage.StatusCode, httpResponseMessage.Headers.Location);
            }
        }
    }
}
