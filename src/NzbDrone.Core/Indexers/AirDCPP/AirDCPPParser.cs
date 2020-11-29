using System;
using System.Collections.Generic;
using System.Net;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Indexers.AirDCPP.Responses;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.AirDCPP
{
    public class AirDCPPParser : IParseIndexerResponse
    {
        private readonly AirDCPPSettings _settings;

        public AirDCPPParser(AirDCPPSettings settings)
        {
            _settings = settings;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var releases = new List<ReleaseInfo>();

            // here we receive the response from the search we defined in the request generator
            if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new IndexerException(indexerResponse,
                    "Unexpected response status {0} code from API request",
                    indexerResponse.HttpResponse.StatusCode);
            }

            var splitUrl = indexerResponse.HttpRequest.Url.Path.Split('/');
            var searchInstanceId = splitUrl[4];

            var searchResults = Json.Deserialize<List<SearchResult>>(indexerResponse.Content);

            foreach (var searchResult in searchResults)
            {
                releases.Add(new ReleaseInfo()
                {
                    Guid = string.Format("AirDCPP-{0}", searchResult.id),
                    Title = searchResult.name,
                    Size = searchResult.size,
                    DownloadUrl = $"{searchInstanceId}:{searchResult.id}",
                    PublishDate = searchResult.time.HasValue ? FromUnixTime(searchResult.time.Value) : DateTime.Now,
                    Source = "airdcpp",
                    DownloadProtocol = DownloadProtocol.DirectConnect
                });
            }

            return releases.ToArray();
        }

        private static readonly DateTime _epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static DateTime FromUnixTime(long unixTime)
        {
            return _epoch.AddSeconds(unixTime);
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }
}
