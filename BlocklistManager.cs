using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Blocker
{
    public class BlocklistManager
    {
        private readonly HashSet<string> blockedDomains =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly string blocklistPath;

        public BlocklistManager()
            : this(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "blocklist.txt"))
        {
        }

        // Allows tests to use a temporary file instead of the real blocklist.
        internal BlocklistManager(string blocklistPath)
        {
            this.blocklistPath = blocklistPath;
        }

        public void Load()
        {
            blockedDomains.Clear();

            if (!File.Exists(blocklistPath))
            {
                Save();
                return;
            }

            foreach (string line in File.ReadAllLines(blocklistPath))
            {
                string value = line.Trim();

                if (value.Length == 0 || value.StartsWith("#"))
                    continue;

                if (value.StartsWith("*.", StringComparison.Ordinal))
                    value = value.Substring(2);

                string? domain = NormalizeDomain(value);

                if (domain != null)
                    blockedDomains.Add(domain);
            }
        }

        public bool AddDomain(string value)
        {
            string? domain = NormalizeDomain(value);

            if (domain == null || !blockedDomains.Add(domain))
                return false;

            Save();
            return true;
        }

        public bool RemoveDomain(string value)
        {
            string? domain = NormalizeDomain(value);

            if (domain == null || !blockedDomains.Remove(domain))
                return false;

            Save();
            return true;
        }

        public IReadOnlyList<string> GetBlockedDomains()
        {
            return blockedDomains
                .OrderBy(domain => domain, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public bool IsBlocked(string domain)
        {
            string? normalizedDomain = NormalizeDomain(domain);

            if (normalizedDomain == null)
                return false;

            foreach (string blockedDomain in blockedDomains)
            {
                if (normalizedDomain.Equals(
                    blockedDomain,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (normalizedDomain.EndsWith(
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

        private void Save()
        {
            string? directory = Path.GetDirectoryName(blocklistPath);

            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var lines = new List<string>
            {
                "# Blocker blocklist"
            };

            lines.AddRange(
                blockedDomains.OrderBy(
                    domain => domain,
                    StringComparer.OrdinalIgnoreCase));

            File.WriteAllLines(blocklistPath, lines);
        }

        private static string? NormalizeDomain(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            string domain = value.Trim().TrimEnd('.').ToLowerInvariant();

            if (domain.Length == 0 || domain.Length > 253)
                return null;

            string[] labels = domain.Split('.');

            foreach (string label in labels)
            {
                if (label.Length is < 1 or > 63)
                    return null;

                if (label[0] == '-' || label[^1] == '-')
                    return null;

                if (label.Any(character =>
                    !(character is >= 'a' and <= 'z')
                    && !(character is >= '0' and <= '9')
                    && character != '-'))
                {
                    return null;
                }
            }

            return domain;
        }
    }
}
