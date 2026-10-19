using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Cryptology.Desktop.ViewModels;

namespace Cryptology.Desktop.Views;

public sealed partial class MainWindow : Window
{
    private readonly ShellViewModel? model;
    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }
    public MainWindow(ShellViewModel model) : this()
    {
        this.model = model;
        DataContext = model;
        model.PropertyChanged += Changed;
        UpdateTool();
        Closed += (_, _) => model.PropertyChanged -= Changed;
    }
    private void Changed(object? sender, PropertyChangedEventArgs e)
    {
        if (model is null) return;
        if (e.PropertyName == nameof(ShellViewModel.SelectedTool)) UpdateTool();
        if (e.PropertyName == nameof(ShellViewModel.DarkTheme))
            RequestedThemeVariant = model.DarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;
    }
    private void UpdateTool() { if (model is not null) this.FindControl<ContentControl>("ToolHost")!.Content = ToolViews.Create(model.SelectedTool); }
}
