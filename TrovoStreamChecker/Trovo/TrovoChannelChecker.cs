using Flurl.Http;
using System;

namespace TrovoStreamChecker.Trovo
{
    public static class TrovoChannelChecker
    {
        public static ChannelInfo GetInfo(string ChannelName)
        {
            var result = "https://open-api.trovo.live/openplatform/channels/id"
                .WithHeader("Accept", "application/json")
                .WithHeader("Client-ID", "8FMjuk785AX4FMyrwPTU3B8vYvgHWN33")
                .PostJsonAsync(new { username = ChannelName })
                .ReceiveJson<ChannelInfo>();

            try
            {
                return result.Result;
            }
            catch (Exception ex)
            {
                return new ChannelInfo
                {
                    username = ChannelName,
                    category_name = "Канал не найден"
                };
            }
        }
    }
}
