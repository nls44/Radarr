using System.Collections.Generic;
using System.Net;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Download.Clients.Flood.Types;

namespace NzbDrone.Core.Download.Clients.Flood
{
    public interface IFloodProxy
    {
        void AuthVerify(FloodSettings settings);
        void AddTorrentByUrl(string url, FloodSettings settings);
        void AddTorrentByFile(string file, FloodSettings settings);
        void DeleteTorrent(string hash, bool deleteData, FloodSettings settings);
        Dictionary<string, FloodTorrent> GetTorrents(FloodSettings settings);
    }

    public class FloodProxy : IFloodProxy
    {
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;
        private readonly ICached<Dictionary<string, string>> _authCookieCache;

        public FloodProxy(IHttpClient httpClient, ICacheManager cacheManager, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _authCookieCache = cacheManager.GetCache<Dictionary<string, string>>(GetType(), "authCookies");
        }

        private string BuildCachedCookieKey(FloodSettings settings)
        {
            return $"{settings.Url}:{settings.Username}";
        }

        private HttpRequestBuilder BuildRequest(FloodSettings settings)
        {
            var requestBuilder = new HttpRequestBuilder(HttpUri.CombinePath(settings.Url, "/api"))
            {
                LogResponseContent = true,
                NetworkCredential = new NetworkCredential(settings.Username, settings.Password)
            };

            requestBuilder.Headers.ContentType = "application/json";
            requestBuilder.SetCookies(AuthAuthenticate(requestBuilder, settings));

            return requestBuilder;
        }

        private HttpResponse HandleRequest(HttpRequest request, FloodSettings settings)
        {
            try
            {
                return _httpClient.Execute(request);
            }
            catch (HttpException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.Forbidden ||
                    ex.Response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _authCookieCache.Remove(BuildCachedCookieKey(settings));
                    throw new DownloadClientAuthenticationException("Failed to authenticate with Flood.");
                }

                throw new DownloadClientException("Unable to connect to Flood, please check your settings");
            }
            catch
            {
                throw new DownloadClientException("Unable to connect to Flood, please check your settings");
            }
        }

        private Dictionary<string, string> AuthAuthenticate(HttpRequestBuilder requestBuilder, FloodSettings settings, bool force = false)
        {
            var cachedCookies = _authCookieCache.Find(BuildCachedCookieKey(settings));

            if (cachedCookies == null || force)
            {
                var authenticateRequest = requestBuilder.Resource("/auth/authenticate").Post().Build();

                var body = new Dictionary<string, object>
                {
                    { "username", settings.Username },
                    { "password", settings.Password }
                };
                authenticateRequest.SetContent(body.ToJson());

                var response = HandleRequest(authenticateRequest, settings);
                cachedCookies = response.GetCookies();
                _authCookieCache.Set(BuildCachedCookieKey(settings), cachedCookies);
            }

            return cachedCookies;
        }

        public void AuthVerify(FloodSettings settings)
        {
            var verifyRequest = BuildRequest(settings).Resource("/auth/verify").Build();

            verifyRequest.Method = HttpMethod.GET;

            HandleRequest(verifyRequest, settings);
        }

        public void AddTorrentByFile(string file, FloodSettings settings)
        {
            var addRequest = BuildRequest(settings).Resource("/torrents/add-files").Post().Build();

            var body = new Dictionary<string, object>
            {
                { "files", new List<string> { file } },
                { "destination", settings.Destination },
                { "tags", new List<string> { settings.Tag } },
                { "start", settings.StartOnAdd },
            };
            addRequest.SetContent(body.ToJson());

            HandleRequest(addRequest, settings);
        }

        public void AddTorrentByUrl(string url, FloodSettings settings)
        {
            var addRequest = BuildRequest(settings).Resource("/torrents/add-urls").Post().Build();

            var body = new Dictionary<string, object>
            {
                { "urls", new List<string> { url } },
                { "destination", settings.Destination },
                { "tags", new List<string> { settings.Tag } },
                { "start", settings.StartOnAdd }
            };
            addRequest.SetContent(body.ToJson());

            HandleRequest(addRequest, settings);
        }

        public void DeleteTorrent(string hash, bool deleteData, FloodSettings settings)
        {
            var deleteRequest = BuildRequest(settings).Resource("/torrents/delete").Post().Build();

            var body = new Dictionary<string, object>
            {
                { "hashes", new List<string> { hash } },
                { "deleteData", deleteData }
            };
            deleteRequest.SetContent(body.ToJson());

            HandleRequest(deleteRequest, settings);
        }

        public Dictionary<string, FloodTorrent> GetTorrents(FloodSettings settings)
        {
            var getTorrentsRequest = BuildRequest(settings).Resource("/torrents").Build();

            getTorrentsRequest.Method = HttpMethod.GET;

            return Json.Deserialize<FloodTorrentListSummary>(HandleRequest(getTorrentsRequest, settings).Content).Torrents;
        }
    }
}
