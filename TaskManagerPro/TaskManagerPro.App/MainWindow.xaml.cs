using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System;
using TaskManagerPro.App.ViewModels;
using TaskManagerPro.Data.Entities;

namespace TaskManagerPro.App;

public partial class MainWindow : Window
{
    private Point _startPoint;

    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        // Auto-load data on startup
        Loaded += async (s, e) => 
        {
            try
            {
                await viewModel.LoadCommand.ExecuteAsync(null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Load Error: {ex.Message}", "Error");
            }
        };
    }

    private void TaskCard_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _startPoint = e.GetPosition(null);
    }

    private void TaskCard_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            Point mousePos = e.GetPosition(null);
            Vector diff = _startPoint - mousePos;

            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                var border = sender as Border;
                var task = border?.DataContext as TaskItem;
                
                if (task != null && border != null)
                {
                    DragDrop.DoDragDrop(border, new DataObject("TaskItem", task), DragDropEffects.Move);
                }
            }
        }
    }

    private void Column_DragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("TaskItem"))
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }
    }

    private async void Column_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent("TaskItem"))
        {
            var task = e.Data.GetData("TaskItem") as TaskItem;
            var border = sender as FrameworkElement;
            var statusStr = border?.Tag?.ToString();

            if (task != null && statusStr != null && Enum.TryParse(typeof(Data.Entities.TaskStatus), statusStr, out var newStatusObj))
            {
                var newStatus = (Data.Entities.TaskStatus)newStatusObj;
                var vm = DataContext as MainWindowViewModel;
                if (vm != null)
                {
                    await vm.UpdateTaskStatusAsync(task, newStatus);
                }
            }
        }
    }
}