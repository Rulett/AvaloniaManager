using AvaloniaManager.Extensions;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace AvaloniaManager.Models
{
    public class Article
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string ArticleName { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        [Range(0, 99999999.99)]
        public decimal Summa { get; set; }

        [Range(0, 100)]
        public int? Bonus { get; set; }

        [Required]
        [Range(0, 99999999.99)]
        public decimal Itog { get; set; }

        [Required]
        [MaxLength(20)]
        public string SMI { get; set; }

        [Required]
        public bool Reklama { get; set; }

        public int? PublicationId { get; set; }

        public int? NewspaperLine { get; set; }

        [Required]
        public DateTime ReleaseDate { get; set; }

        [Required]
        [MaxLength(20)]
        public string ContentType { get; set; }

        // Навигационное свойство
        public Employee Employee { get; set; }

        // Вычисляемое свойство
        public decimal TotalWithBonus => Itog + (Bonus ?? 0);

        // Метод валидации SMI
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