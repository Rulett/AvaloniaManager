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

        [Required(ErrorMessage = "Фамилия обязательна")]
        [MaxLength(255, ErrorMessage = "Максимальная длина 255 символов")]
        public string SurName { get; set; }

        [Required(ErrorMessage = "Имя обязательно")]
        [MaxLength(255, ErrorMessage = "Максимальная длина 255 символов")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Отчество обязательно")]
        [MaxLength(255, ErrorMessage = "Максимальная длина 255 символов")]
        public string FatherName { get; set; }

        [Required(ErrorMessage = "Тип договора обязателен")]
        public string ContractName { get; set; }

        [Required(ErrorMessage = "Номер договора обязателен")]
        [Range(1, int.MaxValue, ErrorMessage = "Номер должен быть положительным числом")]
        public int ContractNumber { get; set; }

        [Required(ErrorMessage = "Дата начала обязательна")]
        public DateTime ContractStart { get; set; }

        [Required(ErrorMessage = "Дата окончания обязательна")]
        public DateTime ContractEnd { get; set; }

        [MaxLength(255, ErrorMessage = "Максимальная длина 255 символов")]
        public string? NickName { get; set; }

        [Required(ErrorMessage = "Укажите штатный статус")]
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