using AvaloniaManager.Data;
using AvaloniaManager.Models;
using AvaloniaManager.Services;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace AvaloniaManager.ViewModels
{
    public class EmployeesViewModel : ReactiveObject
    {
        private ObservableCollection<Employee> _employees = new();
        private Employee _selectedEmployee;
        private string _searchText;
        private int _currentPage = 1;
        private const int PageSize = 15;

        public ObservableCollection<Employee> Employees
        {
            get => _employees;
            set => this.RaiseAndSetIfChanged(ref _employees, value);
        }

        public Employee SelectedEmployee
        {
            get => _selectedEmployee;
            set => this.RaiseAndSetIfChanged(ref _selectedEmployee, value);
        }

        public string SearchText
        {
            get => _searchText;
            set => this.RaiseAndSetIfChanged(ref _searchText, value);
        }

        public int CurrentPage
        {
            get => _currentPage;
            set => this.RaiseAndSetIfChanged(ref _currentPage, value);
        }

        public bool HasNextPage => Employees.Count >= PageSize;
        public bool HasPreviousPage => CurrentPage > 1;

        private ObservableCollection<Employee> _newEmployees = new();
        public ObservableCollection<Employee> NewEmployees
        {
            get => _newEmployees;
            set => this.RaiseAndSetIfChanged(ref _newEmployees, value);
        }

        private bool _isAddingMode;
        public bool IsAddingMode
        {
            get => _isAddingMode;
            set => this.RaiseAndSetIfChanged(ref _isAddingMode, value);
        }

        public ReactiveCommand<Employee, Unit> EditCommand { get; }
        public ReactiveCommand<Employee, Unit> DeleteCommand { get; }
        public ReactiveCommand<Unit, Unit> RefreshCommand { get; }
        public ReactiveCommand<Unit, Unit> AddEmployeeCommand { get; }
        public ReactiveCommand<Unit, Unit> SaveEmployeesCommand { get; }
        public ReactiveCommand<Unit, Unit> ResetAddingCommand { get; }
        public ReactiveCommand<Unit, Unit> AddNewRowCommand { get; }
        public ReactiveCommand<Unit, Unit> NextPageCommand { get; }
        public ReactiveCommand<Unit, Unit> PreviousPageCommand { get; }

        public ReactiveCommand<Unit, Unit> ExitCommand { get; }

        public EmployeesViewModel()
        {
            // Инициализация команд
            RefreshCommand = ReactiveCommand.CreateFromTask(LoadEmployees);
            EditCommand = ReactiveCommand.Create<Employee>(EditEmployee);
            DeleteCommand = ReactiveCommand.CreateFromTask<Employee>(DeleteEmployee);
            NextPageCommand = ReactiveCommand.Create(NextPage);
            PreviousPageCommand = ReactiveCommand.Create(PreviousPage);

            AddEmployeeCommand = ReactiveCommand.Create(StartAdding);
            SaveEmployeesCommand = ReactiveCommand.CreateFromTask(SaveEmployees);
            ResetAddingCommand = ReactiveCommand.Create(ResetAdding);
            AddNewRowCommand = ReactiveCommand.Create(AddNewRow);
            ExitCommand = ReactiveCommand.Create(() =>
            {
                IsAddingMode = false;
                NewEmployees.Clear();
            });

            // Автоматическая загрузка при изменении параметров
            this.WhenAnyValue(x => x.CurrentPage, x => x.SearchText)
                .Throttle(TimeSpan.FromMilliseconds(300))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => LoadEmployees().ConfigureAwait(false));

            // Первоначальная загрузка
            LoadEmployees().ConfigureAwait(false);
        }

        private async Task LoadEmployees()
        {
            try
            {
                await using var db = new AppDbContext();
                var query = db.Employees.AsQueryable();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    query = query.Where(e =>
                        e.FullName.Contains(SearchText) ||
                        e.NickName.Contains(SearchText) ||
                        e.ContractNumber.ToString().Contains(SearchText));
                }

                var totalCount = await query.CountAsync();
                var employees = await query
                    .OrderBy(e => e.SurName)
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .AsNoTracking()
                    .ToListAsync();

                Employees = new ObservableCollection<Employee>(employees);
                this.RaisePropertyChanged(nameof(HasNextPage));
                this.RaisePropertyChanged(nameof(HasPreviousPage));
            }
            catch (Exception ex)
            {
                // Обработка ошибок
                Debug.WriteLine($"Ошибка загрузки: {ex.Message}");
            }
        }

        private void StartAdding()
        {
            NewEmployees.Clear();
            IsAddingMode = true;
        }

        private void AddNewRow()
        {
            NewEmployees.Add(new Employee
            {
                ContractStart = DateTime.Today,
                ContractEnd = DateTime.Today.AddYears(1),
                Shtatni = true,
                ContractName = "Авторский договор"
            });
        }

        private async Task SaveEmployees()
{
    // Проверка на пустые строки
    if (NewEmployees.Count == 0)
    {
        await ShowErrorDialog("Нет данных для сохранения. Добавьте хотя бы одного сотрудника.");
        return;
    }

    // Валидация всех записей
    var errors = new List<string>();
    foreach (var employee in NewEmployees)
    {
        // Проверка обязательных полей
        if (string.IsNullOrWhiteSpace(employee.SurName))
            errors.Add("Фамилия обязательна для заполнения");
        if (string.IsNullOrWhiteSpace(employee.Name))
            errors.Add("Имя обязательно для заполнения");
        if (string.IsNullOrWhiteSpace(employee.ContractName))
            errors.Add("Тип договора обязателен для заполнения");
        
        // Для числовых полей проверяем на значение по умолчанию
        if (employee.ContractNumber == 0) // или другое значение по умолчанию
            errors.Add("Номер договора обязателен для заполнения");

        // Дополнительная проверка дат
        if (employee.ContractStart >= employee.ContractEnd)
        {
            errors.Add("Дата окончания должна быть позже даты начала");
        }
    }

    if (errors.Any())
    {
        await ShowErrorDialog(string.Join("\n", errors.Distinct()));
        return;
    }

    try
    {
        await using var db = new AppDbContext();
        db.Employees.AddRange(NewEmployees);
        await db.SaveChangesAsync();

        await ShowSuccessDialog("Данные успешно сохранены");
        IsAddingMode = false;
        await LoadEmployees();
    }
    catch (Exception ex)
    {
        await ShowErrorDialog($"Ошибка сохранения: {ex.Message}");
    }
}

        private async Task ShowSuccessDialog(string message)
        {
            NotificationManagerService.ShowSuccess(message);
            await Task.CompletedTask;
        }

        private Task ShowErrorDialog(string message)
        {
            NotificationManagerService.ShowError(message);
            return Task.CompletedTask;
        }

        private void ResetAdding()
        {
            NewEmployees.Clear();
        }

        private void AddEmployee()
        {
            // Логика добавления сотрудника
        }

        private void EditEmployee(Employee employee)
        {
            // Логика редактирования сотрудника
        }

        private async Task DeleteEmployee(Employee employee)
        {
            // Логика удаления сотрудника
        }

        private void NextPage()
        {
            CurrentPage++;
            LoadEmployees().ConfigureAwait(false);
        }

        private void PreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                LoadEmployees().ConfigureAwait(false);
            }
        }
    }
}