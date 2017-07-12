using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using jsreport.Types;
using jsreport.Shared;
using Newtonsoft.Json.Converters;

namespace jsreport.Client
{
    /// <summary>
    /// jsreport API .net Wrapper
    /// </summary>
    public class ReportingService : IReportingService
    {
        /// <summary>
        /// Credentials for jsreport having authentication enabled
        /// </summary>
        public string Username { get; set; }
       
        /// <summary>
        /// Boolean to indicate if compression should be enabled or not
        /// </summary>
        public bool Compression { get; set; }

        /// <summary>
        /// Credentials for jsreport having authentication enabled
        /// </summary>
        public string Password { get; set; }
        public Uri ServiceUri { get; set; }

        /// <summary>
        /// Timeout for http client requests
        /// </summary>
        public TimeSpan? HttpClientTimeout { get; set; }
        
        public ReportingService(string serviceUri, string username, string password) : this(serviceUri)
        {
            Username = username;
            Password = password;
            Compression = false;
        }

        public ReportingService(string serviceUri)
        {
            ServiceUri = new Uri(serviceUri);
            Compression = false;
        }

        protected virtual HttpClient CreateClient()
        {
            var client = new HttpClient() { BaseAddress = ServiceUri };

            if (Username != null)
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", System.Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(String.Format("{0}:{1}",Username,Password))));
            }
            if (HttpClientTimeout != null)
                client.Timeout = HttpClientTimeout.Value;

            return client;
        }

        /// <summary>
        /// Specify comnpletely the rendering requests, see http://jsreport.net/learn/api for details
        /// </summary>
        /// <param name="request">ram name="request">Description of rendering process</param>
        /// <exception cref="JsReportException"></exception>
        /// <returns>Report result promise</returns>
        public Task<Report> RenderAsync(RenderRequest request, CancellationToken ct = default(CancellationToken))
        {
            return RenderAsync(SerializerHelper.SerializeRenderRequest(request), ct);
        }

        // <summary>
        /// The simpliest rendering using template shortid and input data
        /// </summary>
        /// <param name="templateShortid">template shortid can be taken from jsreport studio or from filename in jsreport embedded</param>
        /// <param name="jsonData">any json string</param>
        /// <exception cref="JsReportException"></exception>
        /// <returns>Report result promise</returns>
        public Task<Report> RenderAsync(string templateShortid, object data, CancellationToken ct = default(CancellationToken))
        {
            return RenderAsync(SerializerHelper.SerializeRenderRequest(templateShortid, data), ct);
        }

        /// <summary>
        /// The simpliest rendering using template shortid and input data
        /// </summary>
        /// <param name="templateShortid">template shortid can be taken from jsreport studio or from filename in jsreport embedded</param>
        /// <param name="data">any json serializable object</param>
        /// <exception cref="JsReportException"></exception>
        /// <returns>Report result promise</returns>
        public Task<Report> RenderAsync(string templateShortid, string jsonData, CancellationToken ct = default(CancellationToken))
        {
            return RenderAsync(SerializerHelper.SerializeRenderRequest(templateShortid, jsonData), ct);
        }

        /// <summary>
        /// Overload for more sophisticated rendering.
        /// </summary>
        /// <param name="request">ram name="request">Description of rendering process <see cref="RenderRequest"/></param>
        /// <exception cref="JsReportException"></exception>
        /// <returns>Report result promise</returns>
        public Task<Report> RenderAsync(object request, CancellationToken ct = default(CancellationToken))
        {
            return RenderAsync(SerializerHelper.SerializeRenderRequest(request), ct);
        }

        /// <summary>
        /// The simpliest rendering using template name and input data
        /// </summary>
        /// <param name="templateName">template shortid can be taken from jsreport studio or from filename in jsreport embedded</param>
        /// <param name="jsonData">any json string</param>
        /// <exception cref="JsReportException"></exception>
        /// <returns>Report result promise</returns>
        public Task<Report> RenderByNameAsync(string templateName, string jsonData, CancellationToken ct = default(CancellationToken))
        {
            return RenderAsync(SerializerHelper.SerializeRenderRequestForName(templateName, jsonData), ct);
        }

        /// <summary>
        /// The simpliest rendering using template name and input data
        /// </summary>
        /// <param name="templateName">template name</param>
        /// <param name="data">any json serializable object</param>
        /// <exception cref="JsReportException"></exception>
        /// <returns>Report result promise</returns>
        public Task<Report> RenderByNameAsync(string templateName, object data, CancellationToken ct = default(CancellationToken))
        {
            return RenderAsync(SerializerHelper.SerializeRenderRequestForName(templateName, data), ct);
        }
      
        private async Task<Report> RenderAsync(string request, CancellationToken ct = default(CancellationToken))
        {
            var client = CreateClient();
            HttpContent content;
            if (Compression)
            {
                byte[] jsonBytes = Encoding.UTF8.GetBytes(request);
                var ms = new MemoryStream();
                using (var gzip = new GZipStream(ms, CompressionMode.Compress, true))
                {
                    gzip.Write(jsonBytes, 0, jsonBytes.Length);
                }
                ms.Position = 0;

                content = new StreamContent(ms);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                content.Headers.ContentEncoding.Add("gzip");
            }
            else
            {
                content = new StringContent(request, Encoding.UTF8,
                    "application/json");
            }

           var response =
                    await
                        client.PostAsync("api/report", content, ct).ConfigureAwait(false);


            if (response.StatusCode != HttpStatusCode.OK)
                throw JsReportException.Create("Unable to render template. ", response);

            response.EnsureSuccessStatusCode();

            return await ReportFromResponse(response).ConfigureAwait(false);
        }
        /// <summary>
        /// Request list of recipes registered in jsreport server
        /// </summary>
        /// <returns>list of recipes names</returns>
        public async Task<IEnumerable<string>> GetRecipesAsync()
        {
            var client = CreateClient();

            var response = await client.GetAsync("/api/recipe").ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            
            return JsonConvert.DeserializeObject<IEnumerable<string>>(content);
        }

        /// <summary>
        /// Request list of engines registered in jsreport server
        /// </summary>
        /// <returns>list of recipes names</returns>
        public async Task<IEnumerable<string>> GetEnginesAsync()
        {
            var client = CreateClient();

            var response = await client.GetAsync("/api/engine").ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            return JsonConvert.DeserializeObject<IEnumerable<string>>(content);
        }

        /// <summary>
        /// Request jsreport package version
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetServerVersionAsync()
        {
            var client = CreateClient();

            return await client.GetStringAsync("/api/version").ConfigureAwait(false);
        }

        private static async Task<Report> ReportFromResponse(HttpResponseMessage response)
        {
            var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            IDictionary<string, string> meta = new Dictionary<string, string>();
            response.Headers.ToList().ForEach(h => meta[h.Key] = h.Value.FirstOrDefault());
            response.Content.Headers.ToList().ForEach(h => meta[h.Key] = h.Value.FirstOrDefault());
            return new ReportHttp()
            {
                Content = stream,
                Meta = SerializerHelper.ParseReportMeta(meta)
            };
        }
    }
}