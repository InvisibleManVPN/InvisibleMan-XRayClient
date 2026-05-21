using System;
using System.Security.Cryptography;

namespace InvisibleGorillaXRay.Models
{
    public sealed class LocalProxyCredentials
    {
        public static LocalProxyCredentials None { get; } = new(string.Empty, string.Empty);

        public string Username { get; }
        public string Password { get; }

        public bool HasValue =>
            !string.IsNullOrWhiteSpace(Username) &&
            !string.IsNullOrWhiteSpace(Password);

        public LocalProxyCredentials(string username, string password)
        {
            Username = username?.Trim() ?? string.Empty;
            Password = password?.Trim() ?? string.Empty;
        }

        public static LocalProxyCredentials CreateSessionScoped()
        {
            return new LocalProxyCredentials(
                username: CreateToken(8),
                password: CreateToken(16));
        }

        public string BuildSocks5Uri(string host, int port)
        {
            return $"socks5://{Uri.EscapeDataString(Username)}:{Uri.EscapeDataString(Password)}@{host}:{port}";
        }

        private static string CreateToken(int byteCount)
        {
            return Convert.ToHexString(RandomNumberGenerator.GetBytes(byteCount)).ToLowerInvariant();
        }
    }
}
