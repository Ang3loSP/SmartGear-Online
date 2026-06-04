using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace SmartGear_Online.Extensions
{
    /// <summary>
    /// Extension methods for storing complex objects in session
    /// Used for shopping cart storage
    /// </summary>
    public static class SessionExtensions
    {
        public static void SetObjectAsJson(this ISession session, string key, object value)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
            session.SetString(key, JsonSerializer.Serialize(value, options));
        }

        public static T? GetObjectFromJson<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            if (string.IsNullOrEmpty(value))
                return default;

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            return JsonSerializer.Deserialize<T>(value, options);
        }

        public static void RemoveObject(this ISession session, string key)
        {
            session.Remove(key);
        }

        public static bool ContainsKey(this ISession session, string key)
        {
            return session.Keys.Contains(key);
        }
    }
}