using AvaloniaManager.Data;
using AvaloniaManager.Models;
using AvaloniaManager.Services;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private Dictionary<Employee, Employee> _originalValues = new();
        private Employee _selectedEmployee;
        private string _searchText;
        private int _currentPage = 1;
        private int _pageSize = 10;
        
        public int PageSize
        {
            get => _pageSize;
            set => this.RaiseAndSetIfChanged(ref _pageSize, value);
        }

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

        public bool HasNextPage
        {
            get
            {
                try
                {
                    using var db = new AppDbContext();
                    var totalCount = db.Employees.Count();
                    return totalCount > CurrentPage * PageSize;
                }
                catch
                {
                    return false;
                }
            }
        }
        public bool HasPreviousPage => CurrentPage > 1;

        private ObservableCollection<Employee> _newEmployees = new();
        public ObservableCollection<Employee> NewEmployees
        {
            get => _newEmployees;
            set => this.RaiseAndSetIfChanged(ref _newEmployees, value);
        }

        private bool _hasChanges;
        public bool HasChanges
        {
            get => _hasChanges;
            set => this.RaiseAndSetIfChanged(ref _hasChanges, value);
        }
        public bool HasUnsavedChanges => HasChanges;

        private bool _isAddingMode;
        public bool IsAddingMode
        {
            get => _isAddingMode;
            set => this.RaiseAndSetIfChanged(ref _isAddingMode, value);
        }
        
        public ReactiveCommand<Employee, Unit> DeleteCommand { get; }
        public ReactiveCommand<Unit, Unit> RefreshCommand { get; }
        public ReactiveCommand<Unit, Unit> AddEmployeeCommand { get; }
        public ReactiveCommand<Unit, Unit> SaveEmployeesCommand { get; }
        public ReactiveCommand<Unit, Unit> ResetAddingCommand { get; }
        public ReactiveCommand<Unit, Unit> AddNewRowCommand { get; }
        public ReactiveCommand<Unit, Unit> NextPageCommand { get; }
        public ReactiveCommand<Unit, Unit> PreviousPageCommand { get; }
        public ReactiveCommand<Unit, Unit> SaveChangesCommand { get; }
        public ReactiveCommand<Unit, Unit> ExitCommand { get; }

        public EmployeesViewModel()
        {
            // Инициализация команд
            RefreshCommand = ReactiveCommand.CreateFromTask(LoadEmployees);
            DeleteCommand = ReactiveCommand.CreateFromTask<Employee>(DeleteEmployee);
            NextPageCommand = ReactiveCommand.CreateFromTask(NextPage);
            PreviousPageCommand = ReactiveCommand.CreateFromTask(PreviousPage);

            AddEmployeeCommand = ReactiveCommand.Create(StartAdding);
            SaveEmployeesCommand = ReactiveCommand.CreateFromTask(SaveEmployees);
            ResetAddingCommand = ReactiveCommand.Create(ResetAdding);
            AddNewRowCommand = ReactiveCommand.Create(AddNewRow);
            ExitCommand = ReactiveCommand.Create(() =>
            {
                IsAddingMode = false;
                NewEmployees.Clear();
            });

            SaveChangesCommand = ReactiveCommand.CreateFromTask(SaveChanges);

            // Автоматическая загрузка при изменении параметров
            this.WhenAnyValue(x => x.CurrentPage, x => x.SearchText, x=> x.PageSize)
                .Throttle(TimeSpan.FromMilliseconds(300))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => LoadEmployees().ConfigureAwait(false));
            
            // Отслеживание изменений в таблице
            this.WhenAnyValue(x => x.Employees)
        .Subscribe(employees =>
        {
            _originalValues.Clear();
            foreach (var employee in employees)
            {
                _originalValues[employee] = employee.Clone(); 

                employee.PropertyChanged += (s, e) =>
                {
                    HasChanges = true;
                    Debug.WriteLine($"Изменение свойства {e.PropertyName}, HasChanges = {HasChanges}");
                };
            }
            TrackAllChanges();
        });

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

                var employees = await query
                    .OrderBy(e => e.SurName)
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .AsNoTracking()
                    .ToListAsync();

                // Сохраняем текущее состояние изменений перед обновлением
                bool hadChanges = HasChanges;

                Employees = new ObservableCollection<Employee>(employees);

                // Восстанавливаем состояние изменений
                HasChanges = hadChanges;

                this.RaisePropertyChanged(nameof(HasNextPage));
                this.RaisePropertyChanged(nameof(HasPreviousPage));
            }
            catch (Exception ex)
            {
                await DialogService.ShowErrorNotification($"Ошибка загрузки: {ex.Message}");
            }
        }

        private async void StartAdding()
        {
            if (await ConfirmNavigation())
            {
                NewEmployees.Clear();
                IsAddingMode = true;
            }
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
                await DialogService.ShowErrorNotification("Нет данных для сохранения. Добавьте хотя бы одного сотрудника.");
                return;
            }

            // Валидация 
            var errors = new List<string>();
            for (int i = 0; i < NewEmployees.Count; i++)
            {
                var employee = NewEmployees[i];
                int rowNumber = i + 1; 

                // Проверка строковых полей
                if (string.IsNullOrWhiteSpace(employee.SurName))
                    errors.Add($"Строка {rowNumber}: Фамилия обязательна для заполнения");
                if (string.IsNullOrWhiteSpace(employee.Name))
                    errors.Add($"Строка {rowNumber}: Имя обязательно для заполнения");
                if (string.IsNullOrWhiteSpace(employee.ContractName))
                    errors.Add($"Строка {rowNumber}: Тип договора обязателен для заполнения");

                // Проверка числовых полей
                if (employee.ContractNumber <= 0)
                    errors.Add($"Строка {rowNumber}: Номер договора должен быть положительным числом");

                // Проверка дат
                if (employee.ContractStart >= employee.ContractEnd)
                {
                    errors.Add($"Строка {rowNumber}: Дата окончания должна быть позже даты начала");
                }
            }

            if (errors.Any())
            {
                await DialogService.ShowErrorNotification(string.Join("\n", errors.Distinct()));
                return;
            }

            try
            {
                await using var db = new AppDbContext();
                db.Employees.AddRange(NewEmployees);
                await db.SaveChangesAsync();

                await DialogService.ShowSuccessNotification("Данные успешно добавлены");
                IsAddingMode = false;
                await LoadEmployees();
            }
            catch (Exception ex)
            {
                await DialogService.ShowErrorNotification($"Ошибка сохранения: {ex.Message}");
            }
        }

        private void ResetAdding()
        {
            NewEmployees.Clear();
        }

        private async Task DeleteEmployee(Employee employee)
        {
            try
            {
                var confirm = await DialogService.ShowConfirmationDialog(
                    "Удаление сотрудника",
                    $"Вы уверены, что хотите удалить сотрудника {employee.FullName}?\n");

                if (!confirm) return;

                await using var db = new AppDbContext();

                var employeeToDelete = await db.Employees
                    .Include(e => e.Articles) 
                    .FirstOrDefaultAsync(e => e.Id == employee.Id);

                if (employeeToDelete == null)
                {
                    await DialogService.ShowErrorNotification("Сотрудник уже был удален");
                    Employees.Remove(employee);
                    return;
                }

                // Проверка на связанные статьи с подтверждением
                if (employeeToDelete.Articles?.Any() == true)
                {
                    var articlesConfirm = await DialogService.ShowConfirmationDialog(
                        "Предупреждение",
                        $"У этого сотрудника есть {employeeToDelete.Articles.Count} статей. " +
                        "Все они будут удалены. Продолжить?");

                    if (!articlesConfirm) return;
                }

                db.Employees.Remove(employeeToDelete);
                await db.SaveChangesAsync();
                Employees.Remove(employee);
                await DialogService.ShowSuccessNotification($"Сотрудник {employee.FullName} успешно удален");

                await LoadEmployees();
            }
            catch (Exception ex)
            {
                await DialogService.ShowErrorNotification($"Ошибка при удалении: {ex.Message}");
            }
        }

        private async Task SaveChanges()
        {
            try
            {
                await using var db = new AppDbContext();
                foreach (var (employee, original) in _originalValues)
                {
                    // Проверяем изменения
                    if (employee.SurName != original.SurName ||
                        employee.Name != original.Name ||
                        employee.FatherName != original.FatherName ||
                        employee.ContractName != original.ContractName ||
                        employee.ContractNumber != original.ContractNumber ||
                        employee.ContractStart != original.ContractStart ||
                        employee.ContractEnd != original.ContractEnd ||
                        employee.NickName != original.NickName ||
                        employee.Shtatni != original.Shtatni)
                    {
                        db.Entry(employee).State = EntityState.Modified;
                    }
                }

                await db.SaveChangesAsync();
                _originalValues.Clear();

                // Обновляем ориджинал значения
                foreach (var employee in Employees)
                {
                    _originalValues[employee] = new Employee
                    {
                        SurName = employee.SurName,
                        Name = employee.Name,
                        FatherName = employee.FatherName,
                        ContractName = employee.ContractName,
                        ContractNumber = employee.ContractNumber,
                        ContractStart = employee.ContractStart,
                        ContractEnd = employee.ContractEnd,
                        NickName = employee.NickName,
                        Shtatni = employee.Shtatni
                    };
                }

                HasChanges = false;
                await DialogService.ShowSuccessNotification("Изменения успешно сохранены");
            }
            catch (Exception ex)
            {
                await DialogService.ShowErrorNotification($"Ошибка сохранения: {ex.Message}");
            }
        }

        // Подтверждение перехода при наличии изменений
        public async Task<bool> ConfirmNavigation()
        {
            // проверка изменений перед подтверждением
            TrackAllChanges();

            if (!HasChanges) return true;

            var result = await DialogService.ShowConfirmationDialog(
                "Несохраненные изменения",
                "У вас есть несохраненные изменения. Хотите сохранить перед переходом?"
                );

            if (result) // Сохранить
            {
                try
                {
                    await SaveChanges();
                    return true;
                }
                catch (Exception ex)
                {
                    await DialogService.ShowErrorNotification($"Ошибка сохранения: {ex.Message}");
                    return false;
                }
            }
            else // Отменить изменения
            {
                await DiscardChanges();
                return true;
            }
        }

        private void TrackAllChanges()
        {
            bool anyChanges = false;
            foreach (var employee in Employees)
            {
                if (!_originalValues.TryGetValue(employee, out var original))
                {
                    Debug.WriteLine($"Обнаружен новый сотрудник без оригинальных значений");
                    anyChanges = true;
                    continue;
                }

                if (employee.SurName != original.SurName ||
                    employee.Name != original.Name ||
                    employee.FatherName != original.FatherName ||
                    employee.ContractName != original.ContractName ||
                    employee.ContractNumber != original.ContractNumber ||
                    employee.ContractStart != original.ContractStart ||
                    employee.ContractEnd != original.ContractEnd ||
                    employee.NickName != original.NickName ||
                    employee.Shtatni != original.Shtatni)
                {
                    Debug.WriteLine($"Обнаружены изменения у сотрудника {employee.FullName}");
                    anyChanges = true;
                }
            }
            HasChanges = anyChanges;
            Debug.WriteLine($"TrackAllChanges: HasChanges = {HasChanges}");
        }

        private async Task DiscardChanges()
        {
            // Создаем временную копию оригинальных значений
            var originalCopies = _originalValues.ToDictionary(kv => kv.Key, kv => kv.Value.Clone());

            foreach (var (employee, original) in originalCopies)
            {
                employee.SurName = original.SurName;
                employee.Name = original.Name;
                employee.FatherName = original.FatherName;
                employee.ContractName = original.ContractName;
                employee.ContractNumber = original.ContractNumber;
                employee.ContractStart = original.ContractStart;
                employee.ContractEnd = original.ContractEnd;
                employee.NickName = original.NickName;
                employee.Shtatni = original.Shtatni;
            }

            // релоад оригинальных значений
            _originalValues.Clear();
            foreach (var employee in Employees)
            {
                _originalValues[employee] = employee.Clone();
            }

            HasChanges = false;
            Debug.WriteLine($"DiscardChanges: Все изменения отменены, HasChanges = {HasChanges}");

            await DialogService.ShowSuccessNotification("Изменения отменены");
        }

        private async Task NextPage()
        {   
            if (!HasNextPage)
            {
                return;
            }

            if (HasChanges && !await ConfirmNavigation())
            {
                return;
            }

            CurrentPage++;
            await LoadEmployees();
        }

        private async Task PreviousPage()
        {
            if (!HasPreviousPage)
            {
                return;
            }

            if (HasChanges && !await ConfirmNavigation())
            {
                return;
            }

            CurrentPage--;
            await LoadEmployees();
        }
    }
}