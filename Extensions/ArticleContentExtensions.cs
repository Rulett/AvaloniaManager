using AvaloniaManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AvaloniaManager.Extensions
{
    public static class ArticleContentTypeExtensions
    {
        private static readonly Dictionary<string, ArticleContentType> DescriptionToValueMap =
            Enum.GetValues(typeof(ArticleContentType))
                .Cast<ArticleContentType>()
                .ToDictionary(e => e.GetDescription(), e => e);

        public static string GetDescription(this ArticleContentType value)
        {
            return EnumExtensions.GetDescription(value);
        }

        public static bool IsValidDescription(string description)
        {
            return DescriptionToValueMap.ContainsKey(description);
        }

        public static ArticleContentType? FromDescription(string description)
        {
            return DescriptionToValueMap.TryGetValue(description, out var value)
                ? value
                : null;
        }
    }
}