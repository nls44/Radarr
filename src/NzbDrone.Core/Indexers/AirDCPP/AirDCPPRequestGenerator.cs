using System;
using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers.AirDCPP
{
    public class AirDCPPRequestGenerator : IIndexerRequestGenerator
    {
        public string BaseUrl { get; set; }
        public AirDCPPSettings Settings { get; set; }

        protected readonly IHttpClient _httpClient;
        protected readonly IAirDCPPProxy _airDCPPProxy;
        protected readonly Logger _logger;

        public AirDCPPRequestGenerator(IHttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _airDCPPProxy = new AirDCPPProxy(_httpClient, logger);
            _logger = logger;
        }

        public virtual IndexerPageableRequestChain GetRecentRequests()
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetRequest("Anna 1080p Bluray"));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetRequest(string.Format("{0}",
                    searchCriteria.Movie.Title)));

            return pageableRequests;
        }

        public class CustomInfo
        {
            public QueryInfo query { get; set; }
        }

        public class QueryInfo
        {
            public string pattern { get; set; }
        }

        private IEnumerable<IndexerRequest> GetRequest(string searchName)
        {
            var request = _airDCPPProxy.PerformSearch(Settings, searchName);
            yield return new IndexerRequest(request);
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }
}
