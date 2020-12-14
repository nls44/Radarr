using System;
using System.Collections.Generic;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles.TorrentInfo;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RemotePathMappings;

namespace NzbDrone.Core.Download.Clients.Flood
{
    public class Flood : TorrentClientBase<FloodSettings>
    {
        private readonly IFloodProxy _proxy;

        public Flood(IFloodProxy proxy,
                        ITorrentFileInfoReader torrentFileInfoReader,
                        IHttpClient httpClient,
                        IConfigService configService,
                        INamingConfigService namingConfigService,
                        IDiskProvider diskProvider,
                        IRemotePathMappingService remotePathMappingService,
                        Logger logger)
            : base(torrentFileInfoReader, httpClient, configService, namingConfigService, diskProvider, remotePathMappingService, logger)
        {
            _proxy = proxy;
        }

        public override string Name => "Flood";

        protected override string AddFromTorrentFile(RemoteMovie remoteEpisode, string hash, string filename, byte[] fileContent)
        {
            _proxy.AddTorrentByFile(Convert.ToBase64String(fileContent), Settings);

            return hash;
        }

        protected override string AddFromMagnetLink(RemoteMovie remoteEpisode, string hash, string magnetLink)
        {
            _proxy.AddTorrentByUrl(magnetLink, Settings);

            return hash;
        }

        public override IEnumerable<DownloadClientItem> GetItems()
        {
            var items = new List<DownloadClientItem>();

            var list = _proxy.GetTorrents(Settings);

            foreach (var torrent in list)
            {
                var infoHash = torrent.Key.ToLower();
                var properties = torrent.Value;
                var item = new DownloadClientItem
                {
                    DownloadClientInfo = DownloadClientItemClientInfo.FromDownloadClient(this),
                    DownloadId = infoHash,
                    Title = properties.Name,
                    OutputPath = new OsPath(properties.Directory),
                    Category = properties.Tags.Count > 0 ? properties.Tags[0] : null,
                    RemainingSize = properties.SizeBytes - properties.BytesDone,
                    RemainingTime = TimeSpan.FromSeconds(properties.Eta),
                    TotalSize = properties.SizeBytes,
                    SeedRatio = properties.Ratio,
                    Message = properties.Message,
                    CanBeRemoved = true,
                    CanMoveFiles = true,
                };

                if (properties.Status.Contains("error"))
                {
                    item.Status = DownloadItemStatus.Warning;
                }
                else if (properties.Status.Contains("seeding") || properties.Status.Contains("complete"))
                {
                    item.Status = DownloadItemStatus.Completed;
                }
                else if (properties.Status.Contains("downloading"))
                {
                    item.Status = DownloadItemStatus.Downloading;
                }
                else if (properties.Status.Contains("stopped"))
                {
                    item.Status = DownloadItemStatus.Paused;
                }

                items.Add(item);
            }

            return items;
        }

        public override void RemoveItem(string downloadId, bool deleteData)
        {
            _proxy.DeleteTorrent(downloadId, deleteData, Settings);
        }

        public override DownloadClientInfo GetStatus()
        {
            return new DownloadClientInfo
            {
                IsLocalhost = true,
            };
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            try
            {
                _proxy.AuthVerify(Settings);
            }
            catch (DownloadClientAuthenticationException ex)
            {
                failures.Add(new ValidationFailure("Password", ex.Message));
            }
            catch (Exception ex)
            {
                failures.Add(new ValidationFailure("URL", ex.Message));
            }
        }
    }
}
