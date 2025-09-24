using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.Maui.Devices;

namespace YasGMP.Services.Platform
{
    /// <summary>
    /// Default MAUI implementation of <see cref="IPlatformService"/> that surfaces
    /// device and network metadata used by the shared core logic for audit logging.
    /// </summary>
    public sealed class MauiPlatformService : IPlatformService
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

                    var ipProps = ni.GetIPProperties();
                    var ipv4 = ipProps.UnicastAddresses
                        .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a.Address));
                    if (ipv4 != null)
                    {
                        return ipv4.Address.ToString();
                    }
                }
            }
            catch
            {
                // swallow – fall back to other strategies
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
                // ignore and continue to socket fallback
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
                // ignore and fall through
            }

            return string.Empty;
        }

        /// <inheritdoc />
        public string GetOsVersion()
        {
            try
            {
                var platform = DeviceInfo.Current?.Platform.ToString();
                var version = DeviceInfo.Current?.VersionString;
                if (!string.IsNullOrWhiteSpace(platform) && !string.IsNullOrWhiteSpace(version))
                {
                    return $"{platform} {version}";
                }

                if (!string.IsNullOrWhiteSpace(platform))
                {
                    return platform!;
                }
            }
            catch
            {
                // ignore and fall through
            }

            return Environment.OSVersion.ToString();
        }

        /// <inheritdoc />
        public string GetHostName()
        {
            try
            {
                return DeviceInfo.Current?.Name ?? Environment.MachineName;
            }
            catch
            {
                return Environment.MachineName;
            }
        }

        /// <inheritdoc />
        public string GetUserName()
        {
            try
            {
                return Environment.UserName;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <inheritdoc />
        public string GetManufacturer()
        {
            try
            {
                return DeviceInfo.Current?.Manufacturer ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <inheritdoc />
        public string GetModel()
        {
            try
            {
                return DeviceInfo.Current?.Model ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

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
