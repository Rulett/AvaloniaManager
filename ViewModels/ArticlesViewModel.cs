using Avalonia.Controls;
using AvaloniaManager.Data;
using AvaloniaManager.Models;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

namespace AvaloniaManager.ViewModels
{
    public class ArticlesViewModel : ReactiveObject
    {
        private ObservableCollection<Article> _articles = new();
        private int _selectedMonth = DateTime.Now.Month;
        private int _selectedYear = DateTime.Now.Year;

        public ObservableCollection<Article> Articles
        {
            get => _articles;
            set => this.RaiseAndSetIfChanged(ref _articles, value);
        }

        public string MonthYearHeader =>
        $"Статьи за {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(SelectedMonth)} {SelectedYear}";

        public int SelectedMonth
        {
            get => _selectedMonth;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedMonth, value);
                this.RaisePropertyChanged(nameof(MonthYearHeader));
                LoadArticles();
            }
        }

        public int SelectedYear
        {
            get => _selectedYear;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedYear, value);
                this.RaisePropertyChanged(nameof(MonthYearHeader));
                LoadArticles();
            }
        }

        public DateTime SelectedDate => new DateTime(SelectedYear, SelectedMonth, 1);

        public ArticlesViewModel()
        {
            LoadArticles();
        }

        private async Task LoadArticles()
        {
            try
            {
                await using var db = new AppDbContext();

                var startDate = new DateTime(SelectedYear, SelectedMonth, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                var articles = await db.Articles
                    .Include(a => a.Employee)
                    .Where(a => a.ReleaseDate >= startDate && a.ReleaseDate <= endDate)
                    .OrderBy(a => a.ReleaseDate)
                    .ToListAsync();

                Articles = new ObservableCollection<Article>(articles);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки статей: {ex.Message}");
            }
        }
    }
}