using System;
using System.Configuration;

namespace P2P.PeerClient
{
    static class AppSettings
    {

        public static ulong PeerListRefreshMs { get; set; }

        static AppSettings()
        {
            #region Peer List refresh

            var refreshPeriod = ConfigurationManager.AppSettings.Get("PeerListRefreshMs");
            ulong refreshInterval;
            if (!ulong.TryParse(refreshPeriod, out refreshInterval))
            {
                refreshInterval = 500;
            }

            PeerListRefreshMs = refreshInterval;

            #endregion
        }
    }
}
