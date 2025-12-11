using System.Windows;
using TaskManagerPro.App.ViewModels;

namespace TaskManagerPro.App.Views;

public partial class TaskDialog : Window
{
    public TaskDialog(TaskDetailViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
