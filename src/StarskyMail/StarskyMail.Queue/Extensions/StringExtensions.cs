using Newtonsoft.Json;

namespace StarskyMail.Queue.Extensions
{
    public static class StringExtensions
    {
        public static bool TryDeserializeJson<T>(this string @this, out T result)
        {
            var success = true;
            var settings = new JsonSerializerSettings
            {
                Error = (sender, args) => { success = false; args.ErrorContext.Handled = true; },
                MissingMemberHandling = MissingMemberHandling.Error
            };
            result = JsonConvert.DeserializeObject<T>(@this, settings);
            return success;
        }
    }
}