using AvaloniaManager.Data;
using AvaloniaManager.Models;
using AvaloniaManager.Services;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

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
                if (!IsAddingMode) 
                {
                    LoadArticles();
                    LoadEmployees();
                }
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
                if (!IsAddingMode)
                {
                    LoadArticles();
                    LoadEmployees();
                }
            }
        }

        private ObservableCollection<Article> _newArticles = new();
        public ObservableCollection<Article> NewArticles
        {
            get => _newArticles;
            set => this.RaiseAndSetIfChanged(ref _newArticles, value);
        }

        private bool _isAddingMode;
        public bool IsAddingMode
        {
            get => _isAddingMode;
            set => this.RaiseAndSetIfChanged(ref _isAddingMode, value);
        }

        private bool _hasChanges;
        public bool HasChanges
        {
            get => _hasChanges;
            set => this.RaiseAndSetIfChanged(ref _hasChanges, value);
        }

        private Dictionary<Article, Article> _originalValues = new();

        public ReactiveCommand<Unit, Unit> NextPageCommand { get; }
        public ReactiveCommand<Unit, Unit> PreviousPageCommand { get; }
        public ReactiveCommand<Article, Unit> DeleteCommand { get; }
        public ReactiveCommand<Unit, Unit> AddArticlesCommand { get; }
        public ReactiveCommand<Unit, Unit> AddNewRowCommand { get; }
        public ReactiveCommand<Unit, Unit> ResetAddingCommand { get; }
        public ReactiveCommand<Unit, Unit> SaveArticlesCommand { get; }
        public ReactiveCommand<Unit, Unit> ExitCommand { get; }
        public ReactiveCommand<Unit, Unit> SaveChangesCommand { get; }

        public ArticlesViewModel()
        {
            NextPageCommand = ReactiveCommand.CreateFromTask(NextPage);
            PreviousPageCommand = ReactiveCommand.CreateFromTask(PreviousPage);
            DeleteCommand = ReactiveCommand.CreateFromTask<Article>(DeleteArticle);
            AddArticlesCommand = ReactiveCommand.Create(StartAdding);
            AddNewRowCommand = ReactiveCommand.Create(AddNewRow);
            ResetAddingCommand = ReactiveCommand.Create(ResetAdding);
            ExitCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                IsAddingMode = false;
                NewArticles.Clear();
                await LoadEmployees();
                await LoadArticles().ConfigureAwait(false);
            });

            SaveArticlesCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await SaveArticles();
                LoadArticles().ConfigureAwait(false); 
            });
            SaveChangesCommand = ReactiveCommand.CreateFromTask(SaveChanges);

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
                    .OrderBy(a => a.ArticleName) 
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

        private async Task SaveArticles()
        {
            foreach (var article in NewArticles)
            {
                article.CalculateItog();
            }

            // Валидация
            var errors = new List<string>();
            for (int i = 0; i < NewArticles.Count; i++)
            {
                var article = NewArticles[i];

                if (string.IsNullOrWhiteSpace(article.ArticleName))
                    errors.Add($"Строка {i + 1}: Название статьи обязательно");
                if (article.Employee == null)
                    errors.Add($"Строка {i + 1}: Не выбран сотрудник");
                if (string.IsNullOrWhiteSpace(article.SMI))
                    errors.Add($"Строка {i + 1}: Не указано СМИ");
                if (article.ReleaseDate == default)
                    errors.Add($"Строка {i + 1}: Не указана дата публикации");
                if (string.IsNullOrWhiteSpace(article.ContentType))
                    errors.Add($"Строка {i + 1}: Не указан тип контента");
                
                if (article.Summa <= 0)
                    errors.Add($"Строка {i + 1}: Сумма должна быть больше 0");
            }

            if (errors.Any())
            {
                await DialogService.ShowErrorNotification(string.Join("\n", errors));
                return;
            }

            try
            {
                await using var db = new AppDbContext();

                foreach (var article in NewArticles)
                {
                    if (article.Employee != null)
                    {
                        db.Entry(article.Employee).State = EntityState.Unchanged;
                        article.EmployeeId = article.Employee.Id;
                    }
                }

                db.Articles.AddRange(NewArticles);
                await db.SaveChangesAsync();

                await DialogService.ShowSuccessNotification("Статьи успешно добавлены");
                IsAddingMode = false;
                NewArticles.Clear();
                await LoadArticles();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception: {ex.Message}");
                await DialogService.ShowErrorNotification($"Ошибка: {ex.Message}");
            }
        }

        private async Task SaveChanges()
        {
            try
            {
                await using var db = new AppDbContext();
                foreach (var (article, original) in _originalValues)
                {
                    if (article.ArticleName != original.ArticleName ||
                        article.Employee != original.Employee ||
                        article.SMI != original.SMI ||
                        article.ReleaseDate != original.ReleaseDate ||
                        article.PubicationId != original.PubicationId ||
                        article.NewspaperLine != original.NewspaperLine ||
                        article.Summa != original.Summa ||
                        article.Bonus != original.Bonus ||
                        article.Reklama != original.Reklama ||
                        article.ContentType != original.ContentType)
                    {
                        db.Entry(article).State = EntityState.Modified;
                    }
                }

                await db.SaveChangesAsync();
                _originalValues.Clear();

                // Обновление значений
                foreach (var article in Articles)
                {
                    _originalValues[article] = new Article
                    {
                        ArticleName = article.ArticleName,
                        Employee = article.Employee,
                        SMI = article.SMI,
                        ReleaseDate = article.ReleaseDate,
                        PubicationId = article.PubicationId,
                        NewspaperLine = article.NewspaperLine,
                        Summa = article.Summa,
                        Bonus = article.Bonus,
                        Reklama = article.Reklama,
                        ContentType = article.ContentType
                    };
                }

                HasChanges = false;
                await DialogService.ShowSuccessNotification("Изменения сохранены");
            }
            catch (Exception ex)
            {
                await DialogService.ShowErrorNotification($"Ошибка сохранения: {ex.Message}");
            }
        }

        private async Task<bool> ConfirmNavigation()
        {
            if (!HasChanges) return true;

            var result = await DialogService.ShowConfirmationDialog(
                "Несохраненные изменения",
                "У вас есть несохраненные изменения. Хотите сохранить перед переходом?");

            if (result)
            {
                await SaveChanges();
            }
            else
            {
                // Отмена изменения
                foreach (var (article, original) in _originalValues)
                {
                    article.ArticleName = original.ArticleName;
                    article.Employee = original.Employee;
                    article.SMI = original.SMI;
                    article.ReleaseDate = original.ReleaseDate;
                    article.PubicationId = original.PubicationId;
                    article.NewspaperLine = original.NewspaperLine;
                    article.Summa = original.Summa;
                    article.Bonus = original.Bonus;
                    article.Reklama = original.Reklama;
                    article.ContentType = original.ContentType;
                }
                HasChanges = false;
            }

            return true;
        }

        private async void StartAdding()
        {
            if (ConfirmNavigation().GetAwaiter().GetResult())
            {
                NewArticles.Clear();
                await LoadEmployees(); 
                IsAddingMode = true;
            }
        }

        private void AddNewRow()
        {
            var newArticle = new Article
            {
                ReleaseDate = DateTime.Today,
                Bonus = 0,
                Summa = 0,
                Reklama = false,
                ContentType = "Текстовый материал", 
                SMI = "МК" 
            };
            newArticle.CalculateItog();
            NewArticles.Add(newArticle);
        }

        private void ResetAdding()
        {
            NewArticles.Clear();
        }
    }
}