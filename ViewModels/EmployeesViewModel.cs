using AvaloniaManager.Data;
using AvaloniaManager.Models;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using System;
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

        public ReactiveCommand<Employee, Unit> EditCommand { get; }
        public ReactiveCommand<Employee, Unit> DeleteCommand { get; }
        public ReactiveCommand<Unit, Unit> RefreshCommand { get; }
        public ReactiveCommand<Unit, Unit> AddEmployeeCommand { get; }
        public ReactiveCommand<Unit, Unit> NextPageCommand { get; }
        public ReactiveCommand<Unit, Unit> PreviousPageCommand { get; }

        public EmployeesViewModel()
        {
            // Инициализация команд
            RefreshCommand = ReactiveCommand.CreateFromTask(LoadEmployees);
            AddEmployeeCommand = ReactiveCommand.Create(AddEmployee);
            EditCommand = ReactiveCommand.Create<Employee>(EditEmployee);
            DeleteCommand = ReactiveCommand.CreateFromTask<Employee>(DeleteEmployee);
            NextPageCommand = ReactiveCommand.Create(NextPage);
            PreviousPageCommand = ReactiveCommand.Create(PreviousPage);

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
            LoadEmployees().ConfigureAwait(false); // Добавьте загрузку данных
        }

        private void PreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                LoadEmployees().ConfigureAwait(false); // Добавьте загрузку данных
            }
        }
    }
}