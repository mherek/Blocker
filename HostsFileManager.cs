using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;

namespace Blocker
{
    /// <summary>
    /// Applies Blocker's domain list to the Windows hosts file.
    /// Only the section between Blocker's markers is ever replaced or removed.
    /// </summary>
    public class HostsFileManager
    {
        private const string BeginMarker = "# BEGIN BLOCKER";
        private const string EndMarker = "# END BLOCKER";
        private const string RedirectAddress = "127.0.0.1";

        private readonly string hostsFilePath;

        public HostsFileManager()
        {
            hostsFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                @"drivers\etc\hosts"
            );
        }

        // Internal overload makes it possible to test against a temporary file.
        internal HostsFileManager(string hostsFilePath)
        {
            this.hostsFilePath = hostsFilePath;
        }

        public void Apply(IEnumerable<string> domains)
        {
            ArgumentNullException.ThrowIfNull(domains);
            EnsureAdministrator();

            if (!File.Exists(hostsFilePath))
            {
                throw new FileNotFoundException(
                    "The Windows hosts file could not be found.",
                    hostsFilePath
                );
            }

            string originalContent = File.ReadAllText(hostsFilePath);
            string newline = originalContent.Contains("\r\n")
                ? "\r\n"
                : "\n";

            string contentWithoutOldBlock = RemoveManagedBlock(originalContent);

            List<string> validDomains = domains
                .Select(NormalizeDomain)
                .Where(domain => domain != null)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(domain => domain, StringComparer.OrdinalIgnoreCase)
                .ToList()!;

            if (validDomains.Count == 0)
            {
                WriteSafely(contentWithoutOldBlock);
                return;
            }

            var blockLines = new List<string> { BeginMarker };
            blockLines.AddRange(validDomains.Select(
                domain => $"{RedirectAddress} {domain}"
            ));
            blockLines.Add(EndMarker);

            string separator = contentWithoutOldBlock.Length == 0 ||
                               contentWithoutOldBlock.EndsWith("\n") ||
                               contentWithoutOldBlock.EndsWith("\r")
                ? string.Empty
                : newline;

            string updatedContent = contentWithoutOldBlock
                + separator
                + string.Join(newline, blockLines)
                + newline;

            WriteSafely(updatedContent);
        }

        public void Remove()
        {
            EnsureAdministrator();

            if (!File.Exists(hostsFilePath))
            {
                throw new FileNotFoundException(
                    "The Windows hosts file could not be found.",
                    hostsFilePath
                );
            }

            string originalContent = File.ReadAllText(hostsFilePath);
            string updatedContent = RemoveManagedBlock(originalContent);

            if (!string.Equals(originalContent, updatedContent, StringComparison.Ordinal))
            {
                WriteSafely(updatedContent);
            }
        }

        public bool IsApplied()
        {
            if (!File.Exists(hostsFilePath))
                return false;

            string content = File.ReadAllText(hostsFilePath);
            return content.Contains(BeginMarker, StringComparison.Ordinal)
                && content.Contains(EndMarker, StringComparison.Ordinal);
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

        private static string RemoveManagedBlock(string content)
        {
            int beginIndex = content.IndexOf(BeginMarker, StringComparison.Ordinal);
            int endIndex = content.IndexOf(EndMarker, StringComparison.Ordinal);

            if (beginIndex == -1 && endIndex == -1)
                return content;

            if (beginIndex == -1 || endIndex == -1 || endIndex < beginIndex)
            {
                throw new InvalidDataException(
                    "The hosts file contains an incomplete or malformed Blocker section. "
                    + "No changes were made."
                );
            }

            int endAfterMarker = endIndex + EndMarker.Length;

            // Remove the line break after our end marker as well, if present.
            if (endAfterMarker < content.Length && content[endAfterMarker] == '\r')
                endAfterMarker++;

            if (endAfterMarker < content.Length && content[endAfterMarker] == '\n')
                endAfterMarker++;

            int startIndex = beginIndex;

            // Also remove the line break immediately before our begin marker.
            if (startIndex > 0 && content[startIndex - 1] == '\n')
            {
                startIndex--;
                if (startIndex > 0 && content[startIndex - 1] == '\r')
                    startIndex--;
            }

            return content.Remove(startIndex, endAfterMarker - startIndex);
        }

        private void WriteSafely(string content)
        {
            string? directory = Path.GetDirectoryName(hostsFilePath);
            if (string.IsNullOrEmpty(directory))
                throw new InvalidOperationException("The hosts file directory is invalid.");

            string temporaryPath = Path.Combine(
                directory,
                $".hosts.blocker.{Guid.NewGuid():N}.tmp"
            );

            try
            {
                File.WriteAllText(temporaryPath, content, new UTF8Encoding(false));
                File.Move(temporaryPath, hostsFilePath, true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        private static void EnsureAdministrator()
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);

            if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                throw new UnauthorizedAccessException(
                    "Blocker needs to be run as administrator to modify the Windows hosts file."
                );
            }
        }
    }
}
