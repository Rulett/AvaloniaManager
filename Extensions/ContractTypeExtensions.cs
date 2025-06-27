using AvaloniaManager.Models;
using System.Collections.Generic;
using System;
using System.Linq;

namespace AvaloniaManager.Extensions
{
    public static class ContractTypeExtensions
    {
        private static readonly Dictionary<string, ContractType> DescriptionToValueMap =
            Enum.GetValues(typeof(ContractType))
                .Cast<ContractType>()
                .ToDictionary(e => e.GetDescription(), e => e);

        public static string GetDescription(this ContractType value)
        {
            return EnumExtensions.GetDescription(value);
        }

        public static bool IsValidDescription(string description)
        {
            return DescriptionToValueMap.ContainsKey(description);
        }

        public static ContractType? FromDescription(string description)
        {
            return DescriptionToValueMap.TryGetValue(description, out var value)
                ? value
                : null;
        }
    }
}
