
using System;
using System.Collections.Generic;
using System.IO;

namespace Blocker
{
    public class BlocklistManager
    {
        private readonly HashSet<string> blockedDomains =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private readonly string blocklistPath;

        public BlocklistManager()
        {
            blocklistPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "blocklist.txt"
            );
        }

        public void Load()
        {
            blockedDomains.Clear();

            if (!File.Exists(blocklistPath))
            {
                File.WriteAllText(blocklistPath, "");
                return;
            }

            string[] lines = File.ReadAllLines(blocklistPath);

            foreach (string line in lines)
            {
                string domain = line.Trim().ToLowerInvariant();

                if (domain == "" || domain.StartsWith("#"))
                {
                    continue;
                }

                if (domain.StartsWith("*."))
                {
                    domain = domain.Substring(2);
                }

                domain = domain.TrimEnd('.');

                blockedDomains.Add(domain);
            }
        }

        public bool IsBlocked(string domain)
        {
            domain = domain.Trim().TrimEnd('.');

            foreach (string blockedDomain in blockedDomains)
            {
                if (domain.Equals(
                    blockedDomain,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (domain.EndsWith(
                    "." + blockedDomain,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public int GetDomainCount()
        {
            return blockedDomains.Count;
        }
    }
}
