using System;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskManagerPro.App.ViewModels;
using TaskManagerPro.Data.Context;
using TaskManagerPro.Data.Repositories;

namespace TaskManagerPro.App;

public partial class App : Application
{
    public new static App Current => (App)Application.Current;
    public IServiceProvider Services { get; }

    public App()
    {
        Services = ConfigureServices();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Database
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        var dbPath = System.IO.Path.Join(path, "TaskManagerPro", "tasks.db");
        services.AddDbContext<AppDbContext>(
            options => options.UseSqlite($"Data Source={dbPath}"),
            ServiceLifetime.Transient);

        // Repositories
        services.AddTransient<ITaskRepository, SqliteTaskRepository>();

        // ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<TaskDetailViewModel>();

        // Views
        services.AddTransient<MainWindow>();

        return services.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Ensure DB created
        using (var scope = Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // DEV MODE: Reset DB for schema changes
            // context.Database.EnsureDeleted(); // Commented out to persist data
            context.Database.EnsureCreated();
        }

        // Show Main Window
        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
}
