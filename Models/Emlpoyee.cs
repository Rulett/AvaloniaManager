using System.Collections.Generic;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using AvaloniaManager.Extensions;
using System.ComponentModel.DataAnnotations.Schema;

namespace AvaloniaManager.Models
{
    public class Employee
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string SurName { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; }

        [Required]
        [MaxLength(255)]
        public string FatherName { get; set; }

        [Required]
        [MaxLength(80)]
        public string ContractName { get; set; } 

        [Required]
        public int ContractNumber { get; set; }

        [Required]
        public DateTime ContractStart { get; set; }

        [Required]
        public DateTime ContractEnd { get; set; }

        [MaxLength(255)]
        public string? NickName { get; set; } 

        [Required]
        public bool Shtatni { get; set; }

        // Навигационное свойство
        public List<Article> Articles { get; set; } = new List<Article>();

        // Вычисляемые свойства
        [NotMapped]
        public string FullName => $"{SurName} {Name} {FatherName}";

        [NotMapped]
        public string ContractStatus => ContractEnd >= DateTime.Today ? "Действующий" : "Истекший";

        [NotMapped]
        public int DisplayNumber { get; set; }

        // Метод для валидации ContractName
        public bool IsValidContractName()
        {
            return Enum.GetNames(typeof(ContractType))
                     .Select(e => ContractTypeExtensions.GetDescription((ContractType)Enum.Parse(typeof(ContractType), e)))
                     .Contains(ContractName);
        }
    }

    // Enum для внутреннего использования в приложении
    public enum ContractType
    {
        [Description("Договор на создание и использование служебного произведения")]
        ServiceWork,

        [Description("Авторский договор")]
        AuthorContract,

        [Description("Договор на создание и использование объекта авторского права")]
        CopyrightObject,

        [Description("Договор уступок исключительного права")]
        ExclusiveRightsTransfer
    }

}