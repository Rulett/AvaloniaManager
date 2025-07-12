using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaManager.Data;
using AvaloniaManager.Models;
using AvaloniaManager.Services;
using DialogHostAvalonia;
using Material.Icons.Avalonia;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Command;

namespace AvaloniaManager.ViewModels
{
    public class ArticlesViewModel : ReactiveObject
    {
        private ObservableCollection<Article> _articles = new();
        private ObservableCollection<Employee> _employees = new();
        private int _selectedMonth = DateTime.Now.Month;
        private int _selectedYear = DateTime.Now.Year;
        private int _pageSize = 10;
        private int _currentPage = 1;
        private Article _selectedArticle;

        public ObservableCollection<Article> Articles
        {
            get => _articles;
            set => this.RaiseAndSetIfChanged(ref _articles, value);
        }

        public ObservableCollection<Employee> Employees
        {
            get => _employees;
            set => this.RaiseAndSetIfChanged(ref _employees, value);
        }

        public Article SelectedArticle
        {
            get => _selectedArticle;
            set => this.RaiseAndSetIfChanged(ref _selectedArticle, value);
        }

        public int PageSize
        {
            get => _pageSize;
            set => this.RaiseAndSetIfChanged(ref _pageSize, value);
        }

        public int CurrentPage
        {
            get => _currentPage;
            set => this.RaiseAndSetIfChanged(ref _currentPage, value);
        }

        public bool HasNextPage
        {
            get
            {
                try
                {
                    using var db = new AppDbContext();
                    var startDate = new DateTime(SelectedYear, SelectedMonth, 1);
                    var endDate = startDate.AddMonths(1).AddDays(-1);
                    var totalCount = db.Articles.Count(a => a.ReleaseDate >= startDate && a.ReleaseDate <= endDate);
                    return totalCount > CurrentPage * PageSize;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool HasPreviousPage => CurrentPage > 1;

        public string MonthYearHeader =>
            $"Статьи за {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(SelectedMonth)} {SelectedYear}";

        public int SelectedMonth
        {
            get => _selectedMonth;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedMonth, value);
                this.RaisePropertyChanged(nameof(MonthYearHeader));
                CurrentPage = 1;
                LoadArticles();
                LoadEmployees();
            }
        }

        public int SelectedYear
        {
            get => _selectedYear;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedYear, value);
                this.RaisePropertyChanged(nameof(MonthYearHeader));
                CurrentPage = 1;
                LoadArticles();
                LoadEmployees();
            }
        }

        public ReactiveCommand<Unit, Unit> NextPageCommand { get; }
        public ReactiveCommand<Unit, Unit> PreviousPageCommand { get; }
        public ReactiveCommand<Article, Unit> DeleteCommand { get; }

        public ArticlesViewModel()
        {
            NextPageCommand = ReactiveCommand.CreateFromTask(NextPage);
            PreviousPageCommand = ReactiveCommand.CreateFromTask(PreviousPage);
            DeleteCommand = ReactiveCommand.CreateFromTask<Article>(DeleteArticle);

            this.WhenAnyValue(
                x => x.CurrentPage,
                x => x.PageSize)
                .Throttle(TimeSpan.FromMilliseconds(300))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => LoadArticles().ConfigureAwait(false));

            LoadEmployees();
            LoadArticles();
        }

        private async Task LoadEmployees()
        {
            try
            {
                await using var db = new AppDbContext();
                var employees = await db.Employees.ToListAsync();
                Employees = new ObservableCollection<Employee>(employees);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки сотрудников: {ex.Message}");
            }
        }

        private async Task NextPage()
        {
            if (HasNextPage)
            {
                CurrentPage++;
                await LoadArticles();
            }
        }

        private async Task PreviousPage()
        {
            if (HasPreviousPage)
            {
                CurrentPage--;
                await LoadArticles();
            }
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
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .AsNoTracking()
                    .ToListAsync();

                foreach (var article in articles)
                {
                    article.Itog = (decimal)(article.Summa + (article.Summa * article.Bonus / 100));

                    Debug.WriteLine($"Article: {article.ArticleName}, Employee: {article.Employee?.FullName ?? "null"}");
                }

                Articles = new ObservableCollection<Article>(articles);
                this.RaisePropertyChanged(nameof(HasNextPage));
                this.RaisePropertyChanged(nameof(HasPreviousPage));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки статей: {ex.Message}");
            }
        }

        private async Task DeleteArticle(Article article)
        {
            try
            {
                var confirm = await DialogService.ShowConfirmationDialog(
                    "Удаление статьи",
                    $"Вы уверены, что хотите удалить статью {article.ArticleName}?");

                if (!confirm) return;

                await using var db = new AppDbContext();

                var articleToDelete = await db.Articles.FindAsync(article.Id);
                if (articleToDelete == null)
                {
                    await DialogService.ShowErrorNotification("Статья не найдена в базе данных");
                    return;
                }

                db.Articles.Remove(articleToDelete);
                await db.SaveChangesAsync();
                Articles.Remove(article);
                await DialogService.ShowSuccessNotification($"Статья {article.ArticleName} успешно удалена");


                await LoadArticles();
            }
            catch (Exception ex)
            {
                await DialogService.ShowErrorNotification($"Ошибка при удалении: {ex.Message}");
            }
        }
    }
}