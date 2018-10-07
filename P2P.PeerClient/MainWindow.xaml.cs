using P2P.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.PeerToPeer;
using System.Net.Sockets;
using System.ServiceModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace P2P.PeerClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string PEER_CLASSIFIER = "P2PNetwork";
        private const int PEERLIST_REFRESH_DELAY_MS = 0;

        SynchronizationContext _syncContex;

        int _port;
        string _serviceUrl;
        IP2PService _localService;
        ServiceHost _host;
        Timer _refreshPeerTimer;

        PeerName _peerName;
        PeerNameRegistration _registration;
        PeerNameResolver _peerResolver;

        object _findPeersSyncObj = new object();
        object _peerFoundOrCompletedSyncObj = new object();

        List<IPeerEntry> _availablePeers = new List<IPeerEntry>();

        #region Ctor
        public MainWindow()
        {
            InitializeComponent();
        }

        #endregion

        #region Handlers
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            #region Adress init

            var addresses = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (IPAddress address in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    _port = GetAvailablePort();
                    if (_port == default(int))
                    {
                        MessageBox.Show(this, "Cannot find available port. Application will be shut down.", "Networking Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        Application.Current.Shutdown();
                    }

                    _serviceUrl = string.Format("net.tcp://{0}:{1}/P2PService", address, _port);
                    break;
                }
            }
            if (string.IsNullOrEmpty(_serviceUrl))
            {
                MessageBox.Show(this, "Invalid service address. Application will be shut down.", "Networking Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }

            #endregion

            try
            {
                StartWcfService();
            }
            catch (AddressAlreadyInUseException)
            {
                MessageBox.Show(this, "Host address is already in use. Application will be shut down.", "WCF Error",
                   MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }

            CreatePeer();
            InitPeerResolver();

            _syncContex = SynchronizationContext.Current.CreateCopy();
            _refreshPeerTimer = new Timer(TimerCallBack, new object(),
                                  TimeSpan.FromMilliseconds(PEERLIST_REFRESH_DELAY_MS), TimeSpan.FromMilliseconds(AppSettings.PeerListRefreshMs));
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _registration.Stop();
            _host.Close();
            _refreshPeerTimer = null;
        }

        private void OnPeerResolverFound(object sender, ResolveProgressChangedEventArgs e)
        {
            var peer = e.PeerNameRecord;
            foreach (var ep in peer.EndPointCollection)
            {
                if (ep.AddressFamily == AddressFamily.InterNetwork)
                {
                    var remoteUrl = string.Format("net.tcp://{0}:{1}/P2PService", ep.Address, ep.Port);
                    var binding = new NetTcpBinding();
                    binding.Security.Mode = SecurityMode.None;

                    try
                    {
                        IP2PService remoteService = ChannelFactory<IP2PService>.CreateChannel(
                            binding, new EndpointAddress(remoteUrl));
                        _availablePeers.Add(new PeerEntry(remoteService));
                    }
                    catch (Exception ex) when (ex is InvalidOperationException || ex is CommunicationException)
                    {
                        // log
                    }
                }
            }
        }

        private void OnPeerResolveComplete(object sender, ResolveCompletedEventArgs e)
        {
            var peerPanel = this.PeerListPanel;

            _syncContex.Send(state =>
            {
                peerPanel.Children.Clear();
                _availablePeers = _availablePeers.OrderBy(x => x.DisplayedName)
                                                 .ToList();
                foreach (var peer in _availablePeers)
                {
                    peerPanel.Children.Add(new Button()
                    {
                        Width = peerPanel.Width,
                        Height = 25,
                        Content = peer.DisplayedName
                    });
                }
            }, new object());
        }

        private void TimerCallBack(object state)
        { 
            FindPeers();
        }

        #endregion

        /// <summary>
        /// Starts WCF service
        /// </summary>
        /// <exception cref="AddressAlreadyInUseException"></exception>
        private void StartWcfService()
        {
            _localService = new P2PService(this, AppSettings.Name);
            _host = new ServiceHost(_localService, new Uri(_serviceUrl));

            NetTcpBinding binding = new NetTcpBinding();
            binding.Security.Mode = SecurityMode.None;

            _host.AddServiceEndpoint(typeof(IP2PService), binding, _serviceUrl);

            _host.Open();
        }

        private void CreatePeer()
        {
            _peerName = new PeerName(PEER_CLASSIFIER, PeerNameType.Unsecured);
            _registration = new PeerNameRegistration(_peerName, _port);
            _registration.Cloud = Cloud.AllLinkLocal;
        
            // connect to peer cloud
            _registration.Start();
        }

        private void FindPeers()
        {
            if (Monitor.TryEnter(_findPeersSyncObj))
            {
                try
                {
                    _availablePeers.Clear();
                    _peerResolver.ResolveAsync(_peerName, 1);
                }
                finally
                {
                    Monitor.Exit(_findPeersSyncObj);
                }
            }
        }

        private void FindPeersClickButton(object sender, RoutedEventArgs e)
        {
            FindPeers();
        }

        private void InitPeerResolver()
        {
            _peerResolver = new PeerNameResolver();
            _peerResolver.ResolveProgressChanged += OnPeerResolverFound;
            _peerResolver.ResolveCompleted += OnPeerResolveComplete;
        }

        /// <returns>the free port or 0 if it did not find a free port</returns>
        public static int GetAvailablePort()
        {
            var startingPort = 1000;
            IPEndPoint[] endPoints;
            List<int> portArray = new List<int>();

            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();

            //getting active connections
            TcpConnectionInformation[] connections = properties.GetActiveTcpConnections();
            portArray.AddRange(from n in connections
                               where n.LocalEndPoint.Port >= startingPort
                               select n.LocalEndPoint.Port);

            //getting active tcp listners - WCF service listening in tcp
            endPoints = properties.GetActiveTcpListeners();
            portArray.AddRange(from n in endPoints
                               where n.Port >= startingPort
                               select n.Port);

            //getting active udp listeners
            endPoints = properties.GetActiveUdpListeners();
            portArray.AddRange(from n in endPoints
                               where n.Port >= startingPort
                               select n.Port);

            portArray.Sort();

            for (int i = startingPort; i < UInt16.MaxValue; i++)
                if (!portArray.Contains(i))
                    return i;

            return default(int);
        }
    }
}