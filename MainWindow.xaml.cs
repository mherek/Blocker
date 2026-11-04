
using System;
using System.Threading;
using System.Windows;

namespace Blocker
{
    public partial class MainWindow : Window
    {
        private readonly ConfigManager configManager;
        private readonly BlocklistManager blocklistManager;
        private readonly DnsFilterServer dnsServer;

        private readonly CancellationTokenSource cancellationTokenSource = new();

        private AppConfig appConfig;

        public MainWindow()
        {
            InitializeComponent();

            configManager = new ConfigManager();
            appConfig = configManager.Load();

            blocklistManager = new BlocklistManager();
            blocklistManager.Load();

            dnsServer = new DnsFilterServer(blocklistManager);

            UpdateInterface();

            StartDnsServer();
        }

        private async void StartDnsServer()
        {
            try
            {
                await dnsServer.StartAsync(
                    cancellationTokenSource.Token
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "DNS server error: " + ex.Message,
                    "Blocker"
                );
            }
        }

        private void UpdateInterface()
        {
            ProtectionStatus.Text = "DNS TEST MODE";
            BlocklistStatus.Text = "Loaded";

            DomainCount.Text =
                blocklistManager.GetDomainCount() + " domains loaded";
        }

        protected override void OnClosed(EventArgs e)
        {
            cancellationTokenSource.Cancel();
            dnsServer.Stop();

            base.OnClosed(e);
        }
    }
}
