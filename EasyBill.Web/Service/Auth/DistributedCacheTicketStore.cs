using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Cryptography;

namespace EasyBill.UI.Service.Auth
{
    /// <summary>
    /// Stores authentication tickets in server-side distributed cache.
    /// This keeps browser cookie size small even when principal has many claims.
    /// </summary>
    public sealed class DistributedCacheTicketStore : ITicketStore
    {
        private const string KeyPrefix = "EasyBill:AuthTicket:";
        private static readonly TimeSpan DefaultSlidingExpiration = TimeSpan.FromDays(2);

        private readonly IDistributedCache _cache;

        public DistributedCacheTicketStore(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<string> StoreAsync(AuthenticationTicket ticket)
        {
            var key = KeyPrefix + Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            await RenewAsync(key, ticket);
            return key;
        }

        public async Task RenewAsync(string key, AuthenticationTicket ticket)
        {
            var ticketBytes = TicketSerializer.Default.Serialize(ticket);
            var cacheOptions = new DistributedCacheEntryOptions();

            if (ticket.Properties.ExpiresUtc.HasValue)
            {
                cacheOptions.SetAbsoluteExpiration(ticket.Properties.ExpiresUtc.Value);
            }
            else
            {
                cacheOptions.SetSlidingExpiration(DefaultSlidingExpiration);
            }

            await _cache.SetAsync(key, ticketBytes, cacheOptions);
        }

        public async Task<AuthenticationTicket?> RetrieveAsync(string key)
        {
            var ticketBytes = await _cache.GetAsync(key);
            if (ticketBytes == null)
            {
                return null;
            }

            // Keep sliding-expiration tickets alive while user is active.
            await _cache.RefreshAsync(key);

            return TicketSerializer.Default.Deserialize(ticketBytes);
        }

        public Task RemoveAsync(string key)
        {
            return _cache.RemoveAsync(key);
        }
    }
}
