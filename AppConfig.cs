namespace Pr0nBlocker
{
    public class AppConfig
    {
        public string AppName { get; set; } = "Pr0n Blocker";

        public string Version { get; set; } = "0.1.0";

        public bool StartWithWindows { get; set; } = true;

        public bool ProtectionEnabled { get; set; } = false;

        public BlocklistConfig Blocklist { get; set; } = new();
    }

    public class BlocklistConfig
    {
        public string Source { get; set; } = "";

        public DateTime? LastUpdated { get; set; }

        public List<string> BlockedDomains { get; set; } = new();
    }
}