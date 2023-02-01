using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace TrovoStreamChecker
{
    public static class TrovoChannelChecker
    {
        public class ChannelInfo
        {
            public string username { get; set; }
            public string category_name { get; set; }
            public bool is_live { get; set; }
            public long current_viewers { get; set; }
        }

        private static readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();

        public static ChannelInfo GetInfo(string ChannelName)
        {
            try
            {
                using (var webClient = new WebClient())
                {
                    webClient.Encoding = Encoding.UTF8;
                    webClient.Headers["Accept"] = "application/json";
                    webClient.Headers["Client-ID"] = "8FMjuk785AX4FMyrwPTU3B8vYvgHWN33";
                    var json = _serializer.Serialize(new { username = ChannelName });
                    var result = webClient.UploadString("https://open-api.trovo.live/openplatform/channels/id", json);
                    return _serializer.Deserialize(result, typeof(ChannelInfo)) as ChannelInfo;
                }
            }
            catch
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
