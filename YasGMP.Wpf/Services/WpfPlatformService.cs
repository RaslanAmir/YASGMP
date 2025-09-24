using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using YasGMP.Services;

namespace YasGMP.Wpf.Services
{
    /// <summary>
    /// WPF desktop implementation of <see cref="IPlatformService"/> that surfaces
    /// basic device and network metadata for audit logging in the shared core.
    /// </summary>
    public sealed class WpfPlatformService : IPlatformService
    {
        /// <inheritdoc />
        public string GetLocalIpAddress()
        {
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up)
                    {
                        continue;
                    }

                    if (IsVirtual(ni))
                    {
                        continue;
                    }

                    var ipv4 = ni.GetIPProperties().UnicastAddresses
                        .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a.Address));
                    if (ipv4 != null)
                    {
                        return ipv4.Address.ToString();
                    }
                }
            }
            catch
            {
                // ignore and fall through to other strategies
            }

            try
            {
                var host = Dns.GetHostName();
                var entry = Dns.GetHostEntry(host);
                var address = entry.AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a));
                if (address != null)
                {
                    return address.ToString();
                }
            }
            catch
            {
                // ignore and fall through
            }

            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
                socket.Connect("8.8.8.8", 65530);
                if (socket.LocalEndPoint is IPEndPoint endPoint)
                {
                    return endPoint.Address.ToString();
                }
            }
            catch
            {
                // ignore final fallback
            }

            return string.Empty;
        }

        /// <inheritdoc />
        public string GetLocalIpv6Address()
        {
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up)
                    {
                        continue;
                    }

                    if (IsVirtual(ni))
                    {
                        continue;
                    }

                    var ipv6 = ni.GetIPProperties().UnicastAddresses
                        .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetworkV6 && !IPAddress.IsLoopback(a.Address));
                    if (ipv6 != null)
                    {
                        return ipv6.Address.ToString();
                    }
                }
            }
            catch
            {
                // ignore and return empty
            }

            return string.Empty;
        }

        /// <inheritdoc />
        public string GetOsVersion() => Environment.OSVersion.ToString();

        /// <inheritdoc />
        public string GetHostName() => Environment.MachineName;

        /// <inheritdoc />
        public string GetUserName() => Environment.UserName;

        private static bool IsVirtual(NetworkInterface ni)
        {
            if (ni.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
            {
                return true;
            }

            return ni.Description?.Contains("virtual", StringComparison.OrdinalIgnoreCase) == true
                || ni.Name?.Contains("virtual", StringComparison.OrdinalIgnoreCase) == true;
        }
    }
}
