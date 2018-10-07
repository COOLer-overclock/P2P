using System;
using System.Configuration;

namespace P2P.PeerClient
{
    static class AppSettings
    {
        [Obsolete("Think about to add dynamic port pick out")]
        public static int? Port { get; }

        public static ulong PeerListRefreshMs { get; set; }

        public static string Name { get; set; }

        static AppSettings()
        {
            #region Port

            var portStr = ConfigurationManager.AppSettings.Get("Port");
            if (!string.IsNullOrEmpty(portStr))
            {
                int port;
                if (int.TryParse(portStr, out port))
                {
                    Port = port;
                }
                else
                {
                    throw new ConfigurationErrorsException("Cannot parse port value");
                }
            }

            #endregion

            #region Peer List refresh

            var refreshPeriod = ConfigurationManager.AppSettings.Get("PeerListRefreshMs");
            ulong refreshInterval;
            if (!ulong.TryParse(refreshPeriod, out refreshInterval))
            {
                refreshInterval = 500;
            }

            PeerListRefreshMs = refreshInterval;

            #endregion

            Name = ConfigurationManager.AppSettings.Get("Name");
        }
    }
}
