using System;
using System.Windows;
using System.Linq;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskManagerPro.Data.Entities;
using TaskManagerPro.Data.Repositories;
using TaskManagerPro.Data.Services;

namespace TaskManagerPro.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly ITaskRepository _repository;



    public MainWindowViewModel(ITaskRepository repository)
    {
        _repository = repository;
    }

    // Kanban Columns
    [ObservableProperty]
    private ObservableCollection<TaskItem> _todoTasks = new();

    [ObservableProperty]
    private ObservableCollection<TaskItem> _inProgressTasks = new();

    [ObservableProperty]
    private ObservableCollection<TaskItem> _doneTasks = new();

    [ObservableProperty]
    private TaskItem? _selectedTask;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    // Inline Editing Properties
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTodoEditing))]
    [NotifyPropertyChangedFor(nameof(IsInProgressEditing))]
    [NotifyPropertyChangedFor(nameof(IsDoneEditing))]
    private bool _isEditing;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTodoEditing))]
    [NotifyPropertyChangedFor(nameof(IsInProgressEditing))]
    [NotifyPropertyChangedFor(nameof(IsDoneEditing))]
    private Data.Entities.TaskStatus _editingStatus = Data.Entities.TaskStatus.Todo;

    [ObservableProperty]
    private TaskDetailViewModel _editingTaskVM = new();

    public bool IsTodoEditing => IsEditing && EditingStatus == Data.Entities.TaskStatus.Todo;
    public bool IsInProgressEditing => IsEditing && EditingStatus == Data.Entities.TaskStatus.InProgress;
    public bool IsDoneEditing => IsEditing && EditingStatus == Data.Entities.TaskStatus.Done;

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading tasks...";
            var allItems = await _repository.GetAllAsync();
            
            Application.Current.Dispatcher.Invoke(() => 
            {
                // Update ToDo
                TodoTasks.Clear();
                foreach (var item in allItems.Where(t => t.Status == Data.Entities.TaskStatus.Todo))
                {
                    TodoTasks.Add(item);
                }

                // Update InProgress
                InProgressTasks.Clear();
                foreach (var item in allItems.Where(t => t.Status == Data.Entities.TaskStatus.InProgress))
                {
                    InProgressTasks.Add(item);
                }

                // Update Done
                DoneTasks.Clear();
                foreach (var item in allItems.Where(t => t.Status == Data.Entities.TaskStatus.Done))
                {
                    DoneTasks.Add(item);
                }
            });
            
            StatusMessage = $"Loaded {allItems.Count()} tasks.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            MessageBox.Show($"Error loading tasks: {ex.Message}\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // View Mode
    public enum ViewMode { Board, List }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBoardView))]
    [NotifyPropertyChangedFor(nameof(IsListView))]
    private ViewMode _currentViewMode = ViewMode.Board;

    public bool IsBoardView => CurrentViewMode == ViewMode.Board;
    public bool IsListView => CurrentViewMode == ViewMode.List;

    [RelayCommand]
    public void CycleViewMode()
    {
        CurrentViewMode = CurrentViewMode == ViewMode.Board ? ViewMode.List : ViewMode.Board;
    }

    [RelayCommand]
    public void StartAddTask(string? statusStr)
    {
        Data.Entities.TaskStatus initialStatus = Data.Entities.TaskStatus.Todo;
        if (!string.IsNullOrEmpty(statusStr) && Enum.TryParse(typeof(Data.Entities.TaskStatus), statusStr, out var result))
        {
             initialStatus = (Data.Entities.TaskStatus)result;
        }

        EditingStatus = initialStatus;
        EditingTaskVM = new TaskDetailViewModel { Status = initialStatus };
        IsEditing = true;
        StatusMessage = "Editing new task...";
    }

    [RelayCommand]
    public void CancelEdit()
    {
        IsEditing = false;
        EditingTaskVM = new TaskDetailViewModel();
        StatusMessage = "Edit cancelled.";
    }

    [RelayCommand]
    public async Task SaveTaskAsync()
    {
        try 
        {
            var vm = EditingTaskVM;
            TaskItem taskToSave;

            if (vm.Id.HasValue)
            {
                // Update
                taskToSave = await _repository.GetByIdAsync(vm.Id.Value);
                if (taskToSave == null) 
                {
                    StatusMessage = "Error: Task not found.";
                    return;
                }
                
                taskToSave.Title = vm.Title;
                taskToSave.Description = vm.Description;
                taskToSave.EffortHours = vm.EffortHours;
                taskToSave.DueDate = vm.DueDate;
                taskToSave.ImpactScore = vm.ImpactScore;
                taskToSave.UrgencyScore = vm.UrgencyScore;
                taskToSave.Status = vm.Status; // Update status if changed in parsed vm? Or keep EditingStatus?
                // Actually VM status might not change if UI doesn't allow it, but let's assume it stays in the column for now.
                // If user changes 'EditingStatus' column, we should reflect that? The inline form doesn't expose status selector usually.
                // We use EditingStatus to determine which column.
                // But wait, the form is in a specific column. So Status is implicit.
                taskToSave.Status = EditingStatus;
                
                taskToSave.PriorityScore = TaskPriorityService.Calculate(taskToSave);
                taskToSave.UpdatedAt = DateTime.Now;

                await _repository.UpdateAsync(taskToSave);
                StatusMessage = $"Updated: {taskToSave.Title}";
            }
            else
            {
                // Create
                taskToSave = vm.ToEntity();
                taskToSave.CreatedAt = DateTime.Now;
                taskToSave.UpdatedAt = DateTime.Now;
                taskToSave.Status = EditingStatus; // New tasks created in the column where '+' was clicked
                
                taskToSave.PriorityScore = TaskPriorityService.Calculate(taskToSave);
                taskToSave.CategoryId = 1;

                await _repository.AddAsync(taskToSave);
                StatusMessage = "Task added.";
            }

            IsEditing = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving: {ex.Message}";
            MessageBox.Show($"Error saving task: {ex.Message}\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    public async Task MoveToNextAsync(TaskItem? task)
    {
        if (task == null) return;
        
        if (task.Status == Data.Entities.TaskStatus.Todo)
            task.Status = Data.Entities.TaskStatus.InProgress;
        else if (task.Status == Data.Entities.TaskStatus.InProgress)
            task.Status = Data.Entities.TaskStatus.Done;
        else
            return;

        await _repository.UpdateAsync(task);
        await LoadAsync();
    }
    
    [RelayCommand]
    public async Task MoveToPreviousAsync(TaskItem? task)
    {
        if (task == null) return;

        if (task.Status == Data.Entities.TaskStatus.Done)
            task.Status = Data.Entities.TaskStatus.InProgress;
        else if (task.Status == Data.Entities.TaskStatus.InProgress)
            task.Status = Data.Entities.TaskStatus.Todo;
        else
            return;

        await _repository.UpdateAsync(task);
        await LoadAsync();
    }

    [RelayCommand]
    public void StartEditTask(TaskItem? task)
    {
        if (task == null) return;

        var vm = new TaskDetailViewModel();
        vm.LoadFromEntity(task);
        
        EditingStatus = task.Status; // Show form in the task's current column
        EditingTaskVM = vm;
        IsEditing = true;
        StatusMessage = $"Editing: {task.Title}";
    }

    [RelayCommand]
    public async Task DeleteTaskAsync(TaskItem? task)
    {
         if (task == null) return;
         
         await _repository.DeleteAsync(task.Id);
         await LoadAsync();
         StatusMessage = $"Deleted: {task.Title}";
    }

    public async Task UpdateTaskStatusAsync(TaskItem task, Data.Entities.TaskStatus newStatus)
    {
        if (task == null) return;
        if (task.Status == newStatus) return;

        var oldStatus = task.Status;
        task.Status = newStatus;
        task.UpdatedAt = DateTime.Now;
        
        // Priority recalculation might be needed if rules depend on status (not currently, but logic is in Service)
        // task.PriorityScore = TaskPriorityService.Calculate(task); 

        try 
        {
            await _repository.UpdateAsync(task);
            
            // Optimistic UI update or full reload?
            // Full reload is safer for now to sync lists
            await LoadAsync();
            StatusMessage = $"Moved to {newStatus}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error moving task: {ex.Message}";
            MessageBox.Show($"Error moving task: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
