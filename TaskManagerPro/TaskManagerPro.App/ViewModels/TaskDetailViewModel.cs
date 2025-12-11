using System;
using CommunityToolkit.Mvvm.ComponentModel;
using TaskManagerPro.Data.Entities;

namespace TaskManagerPro.App.ViewModels;

public partial class TaskDetailViewModel : ObservableObject
{
    [ObservableProperty]
    private int? _id;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private DateTime? _dueDate;

    [ObservableProperty]
    private decimal _effortHours = 1.0m;

    [ObservableProperty]
    private int _impactScore = 5;

    [ObservableProperty]
    private int _urgencyScore = 5;

    [ObservableProperty]
    private Data.Entities.TaskStatus _status = Data.Entities.TaskStatus.Todo;

    public TaskItem ToEntity()
    {
        return new TaskItem
        {
            Title = Title,
            Description = Description,
            DueDate = DueDate,
            EffortHours = EffortHours,
            ImpactScore = ImpactScore,
            UrgencyScore = UrgencyScore,
            Status = Status
        };
    }

    public bool IsNew => Id == null;
    public string FormTitle => IsNew ? "New Task" : "Edit Task";

    public void LoadFromEntity(TaskItem task)
    {
        Id = task.Id;
        Title = task.Title;
        Description = task.Description;
        DueDate = task.DueDate;
        EffortHours = task.EffortHours;
        ImpactScore = task.ImpactScore;
        UrgencyScore = task.UrgencyScore;
        Status = task.Status;
        OnPropertyChanged(nameof(IsNew));
        OnPropertyChanged(nameof(FormTitle));
    }
}
