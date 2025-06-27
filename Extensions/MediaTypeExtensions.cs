using AvaloniaManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AvaloniaManager.Extensions
{
    public static class MediaTypeExtensions
    {
        private static readonly Dictionary<string, MediaType> DescriptionToValueMap =
            Enum.GetValues(typeof(MediaType))
                .Cast<MediaType>()
                .ToDictionary(e => e.GetDescription(), e => e);

        public static string GetDescription(this MediaType value)
        {
            return EnumExtensions.GetDescription(value);
        }

        public static bool IsValidDescription(string description)
        {
            return DescriptionToValueMap.ContainsKey(description);
        }

        public static MediaType? FromDescription(string description)
        {
            return DescriptionToValueMap.TryGetValue(description, out var value)
                ? value
                : null;
        }
    }
}