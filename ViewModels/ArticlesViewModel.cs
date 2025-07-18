﻿using AvaloniaManager.Data;
using AvaloniaManager.Models;
using AvaloniaManager.Services;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        private HashSet<Article> _trackedArticles = new();
        private int _selectedMonth = DateTime.Now.Month;
        private int _selectedYear = DateTime.Now.Year;
        private int _pageSize = 10;
        private int _currentPage = 1;
        private Article _selectedArticle;

        public bool HasUnsavedChanges => HasChanges;

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
                    HandleMonthYearChange().ConfigureAwait(false);
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
                    HandleMonthYearChange().ConfigureAwait(false);
                }
            }
        }

        private async Task HandleMonthYearChange()
        {
            if (await ConfirmNavigation())
            {
                await LoadArticles();
                await LoadEmployees();
            }
            else
            {
                this.RaisePropertyChanged(nameof(SelectedMonth));
                this.RaisePropertyChanged(nameof(SelectedYear));
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
                await LoadArticles();
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

            this.WhenAnyValue(x => x.Articles)
                 .Where(_ => !IsAddingMode) 
                 .Subscribe(articles =>
                    {
                         foreach (var article in _trackedArticles)
                             {
                              article.PropertyChanged -= Article_PropertyChanged;
                             }
                          _trackedArticles.Clear();
                          _originalValues.Clear();

                         foreach (var article in articles)
                             {
                                 _originalValues[article] = article.Clone();
                                 article.PropertyChanged += Article_PropertyChanged;
                                 _trackedArticles.Add(article);
                             }
                         TrackAllChanges();
                     });

            LoadEmployees();
            LoadArticles();
        }

        private void Article_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (IsAddingMode) return; 

            if (e.PropertyName != nameof(Article.Employee))
            {
                HasChanges = true;
                Debug.WriteLine($"Изменение свойства {e.PropertyName}, HasChanges = {HasChanges}");
            }
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
            if (!HasNextPage) return;

            if (HasChanges && !await ConfirmNavigation())
            {
                return;
            }

            CurrentPage++;
            await LoadArticles();
        }

        private async Task PreviousPage()
        {
            if (!HasPreviousPage) return;

            if (HasChanges && !await ConfirmNavigation())
            {
                return;
            }

            CurrentPage--;
            await LoadArticles();
        }

        private async Task LoadArticles()
        {
            try
            {
                await using var db = new AppDbContext();

                var startDate = new DateTime(SelectedYear, SelectedMonth, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                var existingEmployees = Employees.ToDictionary(e => e.Id);

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

                    if (article.Employee != null && existingEmployees.TryGetValue(article.Employee.Id, out var existingEmployee))
                    {
                        article.Employee = existingEmployee;
                    }
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

        public async Task<bool> ConfirmNavigation()
        {
            if (IsAddingMode) return true;

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
                await DiscardChanges();
            }

            return true;
        }

        private async void StartAdding()
        {
            if (await ConfirmNavigation())
            {
                NewArticles.Clear();
                await LoadEmployees();
                IsAddingMode = true;
                HasChanges = false;
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

        private void TrackAllChanges()
        {
            if (IsAddingMode) 
            {
                HasChanges = false;
                return;
            }

            bool anyChanges = false;
            foreach (var article in Articles)
            {
                if (!_originalValues.TryGetValue(article, out var original))
                {
                    Debug.WriteLine($"Обнаружена новая статья без оригинальных значений");
                    anyChanges = true;
                    continue;
                }

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
                    Debug.WriteLine($"Обнаружены изменения в статье {article.ArticleName}");
                    anyChanges = true;
                }
            }
            HasChanges = anyChanges;
            Debug.WriteLine($"TrackAllChanges: HasChanges = {HasChanges}");
        }

        private async Task DiscardChanges()
        {
            var originalCopies = _originalValues.ToDictionary(kv => kv.Key, kv => kv.Value.Clone());

            foreach (var (article, original) in originalCopies)
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

            _originalValues.Clear();
            foreach (var article in Articles)
            {
                _originalValues[article] = article.Clone();
            }

            HasChanges = false;
            Debug.WriteLine($"DiscardChanges: Все изменения отменены, HasChanges = {HasChanges}");

            await DialogService.ShowErrorNotification("Изменения отменены");
        }

        public void Cleanup()
        {
            foreach (var article in _trackedArticles)
            {
                article.PropertyChanged -= Article_PropertyChanged;
            }
            _trackedArticles.Clear();
            _originalValues.Clear();
        }
    }
}