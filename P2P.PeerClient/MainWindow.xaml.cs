using Newtonsoft.Json;
using NLog;
using P2P.Core;
using P2P.PeerClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.PeerToPeer;
using System.Net.Sockets;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace P2P.PeerClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly SolidColorBrush PEER_BUTTON_MESSAGE_RECEIVED_COLOR = Brushes.LightYellow;
        private readonly SolidColorBrush PEER_BUTTON_NO_MESSAGES_COLOR = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFDDDDDD"));
        private readonly SolidColorBrush PEER_BUTTON_CURRENT_COLOR = Brushes.LightGreen;

        private const string PEER_CLASSIFIER = "P2PNetwork";
        private const int PEERLIST_REFRESH_DELAY_MS = 0;

        private static ILogger _logger = NLog.LogManager.GetCurrentClassLogger();

        SynchronizationContext _uiSyncContex;

        int _port;
        string _serviceUrl;
        IP2PService _localService;
        ServiceHost _host;
        Timer _refreshPeerTimer;
        string _name;

        PeerName _peerName;
        PeerNameRegistration _registration;
        PeerNameResolver _peerResolver;
        IPeerEntry _user;
        
        volatile bool _canUpdatePeers = true;

        List<IPeerEntry> _foundPeers = new List<IPeerEntry>();

        IReadOnlyDictionary<Button, PeerContextData> _peersByButtons;
        internal IReadOnlyList<PeerContextData> AvailablePeers { get; private set; }
        internal PeerContextData CurrentPeer { get; private set; }

        #region Ctor

        public MainWindow()
        {
            _peersByButtons = new Dictionary<Button, PeerContextData>();
            AvailablePeers = new List<PeerContextData>();
            InitializeComponent();
        }

        #endregion

        #region Handlers

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InputNameDialog nameDialog = new InputNameDialog();
            if (nameDialog.ShowDialog() == true)
            {
                _name = nameDialog.ResponseText;
                Title = _name;
            }
            else
            {
                Environment.Exit(-1);
            }

            #region Adress init

            var addresses = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (IPAddress address in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    _port = GetAvailablePort();
                    if (_port == default(int))
                    {
                        var msg = "Cannot find available port. Application will be shut down.";
                        _logger.Fatal(msg);
                        MessageBox.Show(this, msg, "Networking Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        Environment.Exit(-1);
                    }

                    _serviceUrl = string.Format("net.tcp://{0}:{1}/P2PService", address, _port);
                    _logger.Info($"Service URL: {_serviceUrl}");
                    break;
                }
            }
            if (string.IsNullOrEmpty(_serviceUrl))
            {
                var msg = "Invalid service address. Application will be shut down.";
                _logger.Fatal(msg);
                MessageBox.Show(this, msg, "Networking Error", MessageBoxButton.OK, MessageBoxImage.Error);

                Environment.Exit(-1);
            }

            #endregion

            try
            {
                StartWcfService();
            }
            catch (AddressAlreadyInUseException ex)
            {
                var msg = "Host address is already in use. Application will be shut down.";
                _logger.Fatal(ex, ex.Message);
                MessageBox.Show(this, msg, "WCF Error", MessageBoxButton.OK, MessageBoxImage.Error);

                Environment.Exit(-1);
            }

            CreatePeer();
            InitPeerResolver();

            _user = new PeerEntry(_localService, _serviceUrl);

            _uiSyncContex = SynchronizationContext.Current.CreateCopy();
            _refreshPeerTimer = new Timer(TimerCallBack, new object(),
                                  TimeSpan.FromMilliseconds(PEERLIST_REFRESH_DELAY_MS), TimeSpan.FromMilliseconds(AppSettings.PeerListRefreshMs));
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _logger.Debug("Closing window...");

            _registration.Stop();
            foreach (var peer in AvailablePeers)
            {
                peer.PeerEntry.ServiceProxy.ExcludeFromList(_user.DisplayedName);
            }

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
                        var peerContext = AvailablePeers.FirstOrDefault(x => x.PeerEntry.Address == remoteUrl);
                        if (peerContext == null)
                        {
                            IP2PService remoteService = ChannelFactory<IP2PService>.CreateChannel(
                                binding, new EndpointAddress(remoteUrl));
                            var peerEntry = new PeerEntry(remoteService, remoteUrl);
                            _foundPeers.Add(peerEntry);

                            _logger.Debug($"New peer found: {peerEntry}");
                        }
                        else
                        {
                            _foundPeers.Add(peerContext.PeerEntry);
                        }
                    }
                    catch (Exception ex) when (ex is InvalidOperationException || ex is CommunicationException)
                    {
                        _logger.Error(ex, "Cannot process new peer: {0}", ex.Message);
                    }
                }
            }
        }

        private void OnPeerResolveComplete(object sender, ResolveCompletedEventArgs e)
        {
            var peerPanel = this.PeerListPanel;

            _uiSyncContex.Send(state =>
            {
                try
                {
                    _logger.Debug("Updating peer list...");

                    var existedEntries = _peersByButtons.Values.ToList();
                    var newPeersByButtons = new Dictionary<Button, PeerContextData>();

                    var newEntries = new List<KeyValuePair<Button, PeerContextData>>();
                    foreach (var foundPeer in _foundPeers)
                    {
                        var entry = existedEntries.FirstOrDefault(x => x.PeerEntry.DisplayedName == foundPeer.DisplayedName);
                        if (entry == null) // new peer
                        {
                            var button = new Button()
                            {
                                Width = peerPanel.Width,
                                Height = 25,
                                Content = foundPeer.DisplayedName,
                                Background = PEER_BUTTON_NO_MESSAGES_COLOR
                            };
                            button.Click += PeerButtonClick;

                            var entryContex = new PeerContextData(foundPeer, button);
                            newEntries.Add(new KeyValuePair<Button, PeerContextData>(button, entryContex));
                        }
                        else
                        {
                            newPeersByButtons.Add(_peersByButtons.First(x => x.Value == entry).Key, entry);
                        }
                    }
                    foreach (var newEntry in newEntries)
                    {
                        newPeersByButtons.Add(newEntry.Key, newEntry.Value);
                    }
                    var oldKeys = _peersByButtons.Keys.ToList();
                    if (_peersByButtons != null && _peersByButtons.Count > 0)
                    {
                        newPeersByButtons = newPeersByButtons.OrderBy(x => oldKeys.IndexOf(x.Key))
                                                             .ToDictionary(x => x.Key, y => y.Value);
                    }

                    _peersByButtons = newPeersByButtons;
                    AvailablePeers = _peersByButtons.Values.ToList();

                    if ((AvailablePeers.Any() && CurrentPeer == null) || 
                        (AvailablePeers.Any() && !AvailablePeers.Contains(CurrentPeer)))
                    {
                        CurrentPeer = AvailablePeers.First();
                        CurrentPeer.PeerButton.Background = PEER_BUTTON_CURRENT_COLOR;
                    }

                    RefreshPeerPanel();

                    _canUpdatePeers = true;
                    _logger.Debug($"Peer list update is completed. Number of peers: {_peersByButtons.Count}");
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Cannot update peer list: {ex.Message}");
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
            _localService = new P2PService(this, _name);
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
            // obj unlocks in OnPeerResolveComplete
            if (_canUpdatePeers)
            {
                _canUpdatePeers = false;
                try
                {
                    _logger.Debug("Searching peers...");

                    _foundPeers.Clear();
                    _peerResolver.ResolveAsync(_peerName, 1);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Find peers error: {ex.Message}");
                }
            }
        }

        private void InitPeerResolver()
        {
            _peerResolver = new PeerNameResolver();
            _peerResolver.ResolveProgressChanged += OnPeerResolverFound;
            _peerResolver.ResolveCompleted += OnPeerResolveComplete;

            _logger.Debug("Peer resolver is inited");
        }

        private void PeerButtonClick(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var peerContextData = _peersByButtons[button];

            CurrentPeer.PeerButton.Background = PEER_BUTTON_NO_MESSAGES_COLOR;

            CurrentPeer = peerContextData;
            button.Background = PEER_BUTTON_CURRENT_COLOR;

            OuputPeerMessages(CurrentPeer);
        }

        internal void OuputPeerMessages(PeerContextData peer)
        {
            if (peer == CurrentPeer)
            {
                var messages = new StringBuilder();
                foreach (var text in CurrentPeer.Messages)
                {
                    messages.Append(text.Content + Environment.NewLine);
                }

                MessagesHistoryTextBlock.Text = messages.ToString();
            }
            else
            {
                var button = peer.PeerButton;
                button.Background = PEER_BUTTON_MESSAGE_RECEIVED_COLOR;
            }
        }

        private void SendMessageClick(object sender, RoutedEventArgs e)
        {
            var messageToSend = MessageTextBox.Text;
            if (string.IsNullOrEmpty(messageToSend))
                return;

            if (CurrentPeer != null)
            {
                try
                {
                    var internalMessage = new Message(_user, messageToSend);

                    var msgOverNet = new MessageOverNet(_user, messageToSend);
                    var msgStr = JsonConvert.SerializeObject(msgOverNet);

                    CurrentPeer.PeerEntry.ServiceProxy.SendMessage(msgStr);
                    CurrentPeer.Messages.Add(internalMessage);

                    MessagesHistoryTextBlock.Text += messageToSend + Environment.NewLine;
                }
                catch (CommunicationException ex)
                {
                    _logger.Error($"Connection aborted. Host: {CurrentPeer.PeerEntry.Address}");
                    MessageBox.Show($"User {CurrentPeer.PeerEntry.DisplayedName} disconected");

                    _peersByButtons = _peersByButtons.Where(x => x.Key != CurrentPeer.PeerButton).ToDictionary(x => x.Key, x => x.Value);
                    AvailablePeers = _peersByButtons.Select(x => x.Value).ToList();
                    CurrentPeer = AvailablePeers.FirstOrDefault();
                    RefreshPeerPanel();
                }
            }
            MessageTextBox.Text = string.Empty;
        }

        private void RefreshPeerPanel()
        {
            _uiSyncContex.Send(state =>
            {
                var peerPanel = this.PeerListPanel;
                peerPanel.Children.Clear();
                foreach (var buttonKey in _peersByButtons.Keys)
                {
                    peerPanel.Children.Add(buttonKey);
                }
            }, new object());
        }

        /// <returns>the free port or 0 if it did not find a free port</returns>
        private static int GetAvailablePort()
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

        #region TESTING
        private void FindPeersClickButton(object sender, RoutedEventArgs e)
        {
            FindPeers();
        }
        #endregion

        public void ExcludePeer(string displayedName)
        {
            if (CurrentPeer.PeerEntry.DisplayedName == displayedName)
            {
                var nextPeer = AvailablePeers.FirstOrDefault(x => x.PeerEntry.DisplayedName != displayedName);
                if (nextPeer == null)
                {
                    MessagesHistoryTextBlock.Text = string.Empty;
                }
                else
                {
                    OuputPeerMessages(nextPeer);
                    CurrentPeer = nextPeer;
                }
            }

            _peersByButtons = _peersByButtons.Where(x => x.Value.PeerEntry.DisplayedName != displayedName)
                                             .ToDictionary(x => x.Key, x => x.Value);
            AvailablePeers = _peersByButtons.Values.ToList();

            RefreshPeerPanel();
        }
    }
}