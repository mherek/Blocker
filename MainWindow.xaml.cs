using System;
using System.Windows;
using System.Windows.Input;

namespace Blocker
{
    public partial class MainWindow : Window
    {
        private readonly ConfigManager configManager;
        private readonly BlocklistManager blocklistManager;
        private readonly HostsFileManager hostsFileManager;

        private AppConfig appConfig;

        public MainWindow()
        {
            InitializeComponent();

            configManager = new ConfigManager();
            appConfig = configManager.Load();

            blocklistManager = new BlocklistManager();
            blocklistManager.Load();

            hostsFileManager = new HostsFileManager();

            // The text file is the source of truth for the domain list.
            RefreshDomainList();

            if (appConfig.ProtectionEnabled && blocklistManager.GetDomainCount() > 0)
            {
                try
                {
                    hostsFileManager.Apply(blocklistManager.GetBlockedDomains());
                    UpdateProtectionInterface(true);
                }
                catch (Exception ex)
                {
                    appConfig.ProtectionEnabled = false;
                    configManager.Save(appConfig);
                    UpdateProtectionInterface(false);

                    MessageBox.Show(
                        "Blocker could not restore protection.\n\n" + ex.Message,
                        "Blocker",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                }
            }
            else
            {
                appConfig.ProtectionEnabled = false;
                configManager.Save(appConfig);
                UpdateProtectionInterface(false);
            }
        }

        private void ProtectionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (appConfig.ProtectionEnabled)
                {
                    hostsFileManager.Remove();
                    appConfig.ProtectionEnabled = false;
                    configManager.Save(appConfig);
                    UpdateProtectionInterface(false);
                }
                else
                {
                    if (blocklistManager.GetDomainCount() == 0)
                    {
                        MessageBox.Show(
                            "Add at least one domain before enabling protection.",
                            "Blocker",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information
                        );
                        return;
                    }

                    hostsFileManager.Apply(blocklistManager.GetBlockedDomains());
                    appConfig.ProtectionEnabled = true;
                    configManager.Save(appConfig);
                    UpdateProtectionInterface(true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Could not change protection state.\n\n" + ex.Message,
                    "Blocker",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void AddDomainButton_Click(object sender, RoutedEventArgs e)
        {
            AddDomain();
        }

        private void DomainInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                AddDomain();
        }

        private void AddDomain()
        {
            string domain = DomainInput.Text.Trim();

            if (!blocklistManager.AddDomain(domain))
            {
                MessageBox.Show(
                    "That domain is invalid or already in the blocklist.",
                    "Blocker",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                return;
            }

            try
            {
                if (appConfig.ProtectionEnabled)
                    hostsFileManager.Apply(blocklistManager.GetBlockedDomains());

                DomainInput.Clear();
                RefreshDomainList();
            }
            catch (Exception ex)
            {
                // Keep the saved domain, but make it clear that active rules may be stale.
                MessageBox.Show(
                    "The domain was saved, but Windows blocking could not be updated.\n\n"
                    + ex.Message,
                    "Blocker",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
        }

        private void RemoveDomainButton_Click(object sender, RoutedEventArgs e)
        {
            if (DomainList.SelectedItem is not string selectedDomain)
            {
                MessageBox.Show(
                    "Select a domain to remove first.",
                    "Blocker",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                return;
            }

            if (!blocklistManager.RemoveDomain(selectedDomain))
                return;

            try
            {
                if (appConfig.ProtectionEnabled)
                {
                    if (blocklistManager.GetDomainCount() == 0)
                    {
                        hostsFileManager.Remove();
                        appConfig.ProtectionEnabled = false;
                        configManager.Save(appConfig);
                        UpdateProtectionInterface(false);
                    }
                    else
                    {
                        hostsFileManager.Apply(blocklistManager.GetBlockedDomains());
                    }
                }

                RefreshDomainList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "The domain was removed from the saved list, but Windows blocking "
                    + "could not be updated.\n\n" + ex.Message,
                    "Blocker",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
        }

        private void RefreshDomainList()
        {
            DomainList.ItemsSource = null;
            DomainList.ItemsSource = blocklistManager.GetBlockedDomains();
            DomainCount.Text = blocklistManager.GetDomainCount() == 1
                ? "1 domain"
                : blocklistManager.GetDomainCount() + " domains";
        }

        private void UpdateProtectionInterface(bool enabled)
        {
            appConfig.ProtectionEnabled = enabled;

            if (enabled)
            {
                ProtectionStatus.Text = "PROTECTION ON";
                ProtectionStatus.Foreground =
                    new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(74, 222, 128));
                ProtectionDescription.Text =
                    "Domains in your list are redirected to localhost by the Windows hosts file.";
                ProtectionButton.Content = "Disable protection";
                ProtectionButton.Background =
                    new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(185, 28, 28));
            }
            else
            {
                ProtectionStatus.Text = "PROTECTION OFF";
                ProtectionStatus.Foreground =
                    new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(248, 113, 113));
                ProtectionDescription.Text =
                    "Your blocklist is saved, but websites are not being blocked.";
                ProtectionButton.Content = "Enable protection";
                ProtectionButton.Background =
                    new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(22, 163, 74));
            }
        }
    }
}
