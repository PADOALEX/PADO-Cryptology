using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Cryptology.Desktop.Resources;
using Cryptology.Desktop.Services;
using Cryptology.Desktop.ViewModels;
using Cryptology.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Cryptology.Desktop;

public sealed partial class App : Application
{
    private ServiceProvider? services;
    public override void Initialize() => AvaloniaXamlLoader.Load(this);
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            services = new ServiceCollection().AddSingleton<Strings>().AddSingleton<FileService>()
                .AddSingleton<IFileService>(s => s.GetRequiredService<FileService>()).AddSingleton<RsaSession>()
                .AddSingleton<ShellViewModel>().BuildServiceProvider();
            var window = new MainWindow(services.GetRequiredService<ShellViewModel>());
            services.GetRequiredService<FileService>().Owner = window;
            desktop.MainWindow = window;
            desktop.Exit += (_, _) => services.Dispose();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
