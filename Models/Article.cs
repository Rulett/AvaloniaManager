using AvaloniaManager.Extensions;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reactive;
using ReactiveUI;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace AvaloniaManager.Models
{
    public class Article : ReactiveObject
    {
        private int _id;
        private string _articleName = string.Empty;
        private int _employeeId;
        private decimal _summa;
        private decimal? _bonus;
        private decimal _itog;
        private string _smi = string.Empty;
        private bool _reklama;
        private int? _pubicationId;
        private int? _newspaperLine;
        private DateTime _releaseDate;
        private string _contentType = string.Empty;
        private Employee _employee;

        public int Id
        {
            get => _id;
            set => this.RaiseAndSetIfChanged(ref _id, value);
        }

        [Required(ErrorMessage = "Название статьи обязательно")]
        [MaxLength(255, ErrorMessage = "Максимальная длина 255 символов")]
        public string ArticleName
        {
            get => _articleName;
            set => this.RaiseAndSetIfChanged(ref _articleName, value);
        }

        [Required(ErrorMessage = "ID сотрудника обязательно")]
        public int EmployeeId
        {
            get => _employeeId;
            set => this.RaiseAndSetIfChanged(ref _employeeId, value);
        }

        [Required(ErrorMessage = "Сумма обязательна")]
        [Range(0, 99999999.99, ErrorMessage = "Сумма должна быть от 0 до 99,999,999.99")]
        public decimal Summa
        {
            get => _summa;
            set
            {
                this.RaiseAndSetIfChanged(ref _summa, value);
                this.RaisePropertyChanged(nameof(TotalWithBonus));
            }
        }

        [Range(0, 100, ErrorMessage = "Бонус должен быть от 0 до 100")]
        public decimal? Bonus
        {
            get => _bonus;
            set
            {
                this.RaiseAndSetIfChanged(ref _bonus, value);
                this.RaisePropertyChanged(nameof(TotalWithBonus));
            }
        }

        [Required(ErrorMessage = "Итоговая сумма обязательна")]
        [Range(0, 99999999.99, ErrorMessage = "Итог должен быть от 0 до 99,999,999.99")]
        public decimal Itog
        {
            get => _itog;
            set
            {
                this.RaiseAndSetIfChanged(ref _itog, value);
                this.RaisePropertyChanged(nameof(TotalWithBonus));
            }
        }

        [Required(ErrorMessage = "СМИ обязательно")]
        [MaxLength(20, ErrorMessage = "Максимальная длина 20 символов")]
        public string SMI
        {
            get => _smi;
            set => this.RaiseAndSetIfChanged(ref _smi, value);
        }

        [Required(ErrorMessage = "Укажите рекламный статус")]
        public bool Reklama
        {
            get => _reklama;
            set => this.RaiseAndSetIfChanged(ref _reklama, value);
        }

        public int? PubicationId
        {
            get => _pubicationId;
            set => this.RaiseAndSetIfChanged(ref _pubicationId, value);
        }

        public int? NewspaperLine
        {
            get => _newspaperLine;
            set => this.RaiseAndSetIfChanged(ref _newspaperLine, value);
        }

        [Required(ErrorMessage = "Дата выпуска обязательна")]
        public DateTime ReleaseDate
        {
            get => _releaseDate;
            set => this.RaiseAndSetIfChanged(ref _releaseDate, value);
        }

        [Required(ErrorMessage = "Тип контента обязателен")]
        [MaxLength(20, ErrorMessage = "Максимальная длина 20 символов")]
        public string ContentType
        {
            get => _contentType;
            set => this.RaiseAndSetIfChanged(ref _contentType, value);
        }

        // Навигационное свойство
        public Employee Employee
        {
            get => _employee;
            set => this.RaiseAndSetIfChanged(ref _employee, value);
        }

        // Вычисляемое свойство
        [NotMapped]
        public decimal TotalWithBonus => Itog + (Bonus ?? 0);

        public Article Clone()
        {
            return new Article
            {
                Id = this.Id,
                ArticleName = this.ArticleName,
                EmployeeId = this.EmployeeId,
                Summa = this.Summa,
                Bonus = this.Bonus,
                Itog = this.Itog,
                SMI = this.SMI,
                Reklama = this.Reklama,
                PubicationId = this.PubicationId,
                NewspaperLine = this.NewspaperLine,
                ReleaseDate = this.ReleaseDate,
                ContentType = this.ContentType,
                Employee = this.Employee?.Clone()
            };
        }

        public bool IsModified(Article original)
        {
            return ArticleName != original.ArticleName ||
                   EmployeeId != original.EmployeeId ||
                   Summa != original.Summa ||
                   Bonus != original.Bonus ||
                   Itog != original.Itog ||
                   SMI != original.SMI ||
                   Reklama != original.Reklama ||
                   PubicationId != original.PubicationId ||
                   NewspaperLine != original.NewspaperLine ||
                   ReleaseDate != original.ReleaseDate ||
                   ContentType != original.ContentType;
        }

        public bool IsValidSMI()
        {
            return Enum.GetNames(typeof(MediaType))
                     .Select(e => MediaTypeExtensions.GetDescription((MediaType)Enum.Parse(typeof(MediaType), e)))
                     .Contains(SMI);
        }

        // Метод валидации ContentType
        public bool IsValidContentType()
        {
            return Enum.GetNames(typeof(ArticleContentType))
                     .Select(e => ArticleContentTypeExtensions.GetDescription((ArticleContentType)Enum.Parse(typeof(ArticleContentType), e)))
                     .Contains(ContentType);
        }
    }


    public enum MediaType
        {
            [Description("ВМ")]
            VM,

            [Description("МК")]
            MK,

            [Description("Радио-Минск")]
            RadioMinsk,

            [Description("Качели")]
            Kachel,

            [Description("minsknews.by")]
            MinskNews
        }

        public enum ArticleContentType
        {
            [Description("Текстовый материал")]
            Text,

            [Description("Видеоматериал")]
            Video,

            [Description("Фотоматериал")]
            Photo,

            [Description("Аудиоматериал")]
            Audio
        }
    }


