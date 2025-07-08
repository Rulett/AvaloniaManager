using System.Collections.Generic;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ReactiveUI;

namespace AvaloniaManager.Models
{
    public class Employee : ReactiveObject
    {
        private int _id;
        private string _surName = string.Empty;
        private string _name = string.Empty;
        private string _fatherName = string.Empty;
        private string _contractName = string.Empty;
        private int _contractNumber;
        private DateTime _contractStart;
        private DateTime _contractEnd;
        private string? _nickName;
        private bool _shtatni;

        public int Id
        {
            get => _id;
            set => this.RaiseAndSetIfChanged(ref _id, value);
        }

        [Required(ErrorMessage = "Фамилия обязательна")]
        [MaxLength(255, ErrorMessage = "Максимальная длина 255 символов")]
        public string SurName
        {
            get => _surName;
            set => this.RaiseAndSetIfChanged(ref _surName, value);
        }

        [Required(ErrorMessage = "Имя обязательно")]
        [MaxLength(255, ErrorMessage = "Максимальная длина 255 символов")]
        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        [Required(ErrorMessage = "Отчество обязательно")]
        [MaxLength(255, ErrorMessage = "Максимальная длина 255 символов")]
        public string FatherName
        {
            get => _fatherName;
            set => this.RaiseAndSetIfChanged(ref _fatherName, value);
        }

        [Required(ErrorMessage = "Тип договора обязателен")]
        public string ContractName
        {
            get => _contractName;
            set => this.RaiseAndSetIfChanged(ref _contractName, value);
        }

        [Required(ErrorMessage = "Номер договора обязателен")]
        [Range(1, int.MaxValue, ErrorMessage = "Номер должен быть положительным числом")]
        public int ContractNumber
        {
            get => _contractNumber;
            set => this.RaiseAndSetIfChanged(ref _contractNumber, value);
        }

        [Required(ErrorMessage = "Дата начала обязательна")]
        public DateTime ContractStart
        {
            get => _contractStart;
            set => this.RaiseAndSetIfChanged(ref _contractStart, value);
        }

        [Required(ErrorMessage = "Дата окончания обязательна")]
        public DateTime ContractEnd
        {
            get => _contractEnd;
            set => this.RaiseAndSetIfChanged(ref _contractEnd, value);
        }

        [MaxLength(255, ErrorMessage = "Максимальная длина 255 символов")]
        public string? NickName
        {
            get => _nickName;
            set => this.RaiseAndSetIfChanged(ref _nickName, value);
        }

        [Required(ErrorMessage = "Укажите штатный статус")]
        public bool Shtatni
        {
            get => _shtatni;
            set => this.RaiseAndSetIfChanged(ref _shtatni, value);
        }

        // Навигационное свойство
        public List<Article> Articles { get; set; } = new List<Article>();

        // Вычисляемые свойства
        [NotMapped]
        public string FullName => $"{SurName} {Name} {FatherName}";

        [NotMapped]
        public string ContractStatus => ContractEnd >= DateTime.Today ? "Действующий" : "Истекший";

        [NotMapped]
        public int DisplayNumber { get; set; }

        public Employee Clone()
        {
            return new Employee
            {
                Id = this.Id,
                SurName = this.SurName,
                Name = this.Name,
                FatherName = this.FatherName,
                ContractName = this.ContractName,
                ContractNumber = this.ContractNumber,
                ContractStart = this.ContractStart,
                ContractEnd = this.ContractEnd,
                NickName = this.NickName,
                Shtatni = this.Shtatni
            };
        }

        public bool IsModified(Employee original)
        {
            return SurName != original.SurName ||
                   Name != original.Name ||
                   FatherName != original.FatherName ||
                   ContractName != original.ContractName ||
                   ContractNumber != original.ContractNumber ||
                   ContractStart != original.ContractStart ||
                   ContractEnd != original.ContractEnd ||
                   NickName != original.NickName ||
                   Shtatni != original.Shtatni;
        }
    }

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