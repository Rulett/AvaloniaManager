using AvaloniaManager.Data;
using AvaloniaManager.Models;
using AvaloniaManager.Services;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

namespace AvaloniaManager.ViewModels
{
    public class ReportsViewModel : ReactiveObject
    {
        private ReportType _selectedReportType;

        public ReportType SelectedReportType
        {
            get => _selectedReportType;
            set => this.RaiseAndSetIfChanged(ref _selectedReportType, value);
        }

        public List<ReportType> ReportTypes { get; } = new List<ReportType>
        {
            new ReportType { Id = 1, DisplayName = "Отчет по статьям за месяц" },
            new ReportType { Id = 2, DisplayName = "Отчет по статьям за все время" }
        };

        public ReactiveCommand<Unit, Unit> GenerateReportCommand { get; }

        public ReportsViewModel()
        {
            GenerateReportCommand = ReactiveCommand.CreateFromTask(GenerateReport);
        }

        private async Task GenerateReport()
        {
            try
            {
                if (SelectedReportType == null)
                {
                    await DialogService.ShowErrorNotification("Пожалуйста, выберите тип отчета");
                    return;
                }

                using var dbContext = new AppDbContext();
                var reportService = new ReportService();

                var now = DateTime.Now;
                var currentMonthStart = new DateTime(now.Year, now.Month, 1);
                var nextMonthStart = currentMonthStart.AddMonths(1);

                switch (SelectedReportType.Id)
                {
                    case 1: // За текущий месяц
                        var monthlyArticles = await dbContext.Articles
                            .Include(a => a.Employee)
                            .Where(a => a.ReleaseDate >= currentMonthStart && a.ReleaseDate < nextMonthStart)
                            .ToListAsync();

                        reportService.GenerateArticlesReport(monthlyArticles, $"Отчет по статьям за {currentMonthStart:MMMM yyyy}");
                        await DialogService.ShowSuccessNotification($"Отчет по статьям за {currentMonthStart:MMMM yyyy} сгенерирован");
                        break;

                    case 2: // За все время
                        var allArticles = await dbContext.Articles
                            .Include(a => a.Employee)
                            .ToListAsync();

                        reportService.GenerateArticlesReport(allArticles, "Отчет по статьям за все время");
                        await DialogService.ShowSuccessNotification("Отчет по статьям за все время сгенерирован");
                        break;
                }
            }
            catch (Exception ex)
            {
                await DialogService.ShowErrorNotification($"Ошибка генерации отчета: {ex.Message}");
                Debug.WriteLine(ex);
            }
        }
    }

    public class ReportType
    {
        public int Id { get; set; }
        public string DisplayName { get; set; }
    }
}