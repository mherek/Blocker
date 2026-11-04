
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Blocker
{
    public class DnsFilterServer
    {
        private readonly BlocklistManager blocklistManager;
        private UdpClient? udpServer;

        public DnsFilterServer(BlocklistManager manager)
        {
            blocklistManager = manager;
        }

        public async Task StartAsync(
            CancellationToken cancellationToken = default)
        {
            udpServer = new UdpClient(
                new IPEndPoint(IPAddress.Loopback, 5353)
            );

            Console.WriteLine(
                "DNS listener started on 127.0.0.1:5353"
            );

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    UdpReceiveResult result =
                        await udpServer.ReceiveAsync(cancellationToken);

                    string? domain = GetDomainName(result.Buffer);

                    if (domain == null)
                    {
                        Console.WriteLine("Invalid DNS request received.");
                        continue;
                    }

                    bool blocked = blocklistManager.IsBlocked(domain);

                    Console.WriteLine(
                        blocked ? $"BLOCKED: {domain}" : $"ALLOWED: {domain}"
                    );
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("DNS listener stopped.");
            }
        }

        private string? GetDomainName(byte[] packet)
        {
            if (packet.Length < 17)
                return null;

            if ((packet[2] & 0x80) != 0)
                return null;

            int questionCount = (packet[4] << 8) | packet[5];

            if (questionCount < 1)
                return null;

            StringBuilder domain = new StringBuilder();
            int position = 12;

            while (position < packet.Length)
            {
                int length = packet[position++];

                if (length == 0)
                    break;

                if ((length & 0xC0) != 0)
                    return null;

                if (length > 63 || position + length > packet.Length)
                    return null;

                if (domain.Length > 0)
                    domain.Append('.');

                domain.Append(
                    Encoding.ASCII.GetString(packet, position, length)
                );

                position += length;
            }

            if (domain.Length == 0)
                return null;

            return domain.ToString();
        }

        public void Stop()
        {
            udpServer?.Close();
            udpServer?.Dispose();
            udpServer = null;
        }
    }
}
