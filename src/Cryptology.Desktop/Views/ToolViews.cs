using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Input;
using Avalonia.Media;
using Cryptology.Core.Cryptanalysis;
using Cryptology.Desktop.ViewModels;

namespace Cryptology.Desktop.Views;

/// <summary>Code-defined views keep presentation in Views; every action and value binds to a view model.</summary>
public static class ToolViews
{
    public static Control Create(ToolViewModel model)
    {
        var body = new StackPanel { Spacing = 0, DataContext = model, Name = model.Id + "Fields" };
        body.Bind(InputElement.IsEnabledProperty, new Binding("IsBusy") { Converter = new FuncValueConverter<bool, bool>(busy => !busy) });
        switch (model)
        {
            case FrequencyViewModel:
                body.Children.Add(Input());
                body.Children.Add(Card(Stack(
                    Row(Check("SensitiveCase", "CaseSensitive"), Check("Spaces", "Spaces"), Check("Punctuation", "Punctuation"), Check("Other", "Other"), Check("Alphabetical", "Alphabetical")),
                    Row(Action("Analyze", "analyze", true), Action("CSV", "csv")))));
                var chart = new FrequencyChart { Name = "FrequencyDistribution" }; chart.Bind(FrequencyChart.RowsProperty, new Binding("Rows"));
                body.Children.Add(Card(Stack(Label("ChartHelp", "caption"), chart, FrequencyTable())));
                break;
            case ClassicalViewModel:
                body.Children.Add(Input());
                body.Children.Add(Card(Stack(Check("UseSubstitution", "UseSubstitution"), Field("Shift", "Shift"),
                    Row(Action("Encrypt", "encrypt", true), Action("Decrypt", "decrypt"), Swap()),
                    Row(Action("GenerateKey", "generate"), Action("ImportKey", "import-key"), Action("ExportKey", "export-key")),
                    DuplicateNotice(), MappingTable())));
                break;
            case AnalysisViewModel analysis:
                body.Children.Add(Input());
                body.Children.Add(Card(Stack(Row(Action("Analyze", "analyze", true), Action("Baseline", "baseline"), Action("RankCaesar", "caesar-rank")),
                    Row(Action("ResetMap", "reset-map"), Action("ImportMap", "import-map"), Action("ExportMap", "export-map")), DuplicateNotice(), MappingTable())));
                var preview = new TextBlock { TextWrapping = TextWrapping.Wrap, MaxHeight = 240, ClipToBounds = true, Name = "LivePreview" };
                void UpdatePreview()
                {
                    preview.Inlines!.Clear(); int remaining = 12000;
                    foreach (var segment in analysis.PreviewSegments)
                    {
                        if (remaining <= 0) { preview.Inlines.Add(new Run("…")); break; }
                        var display = segment.Text[..Math.Min(remaining, segment.Text.Length)]; remaining -= display.Length;
                        var run = new Run(display);
                        if (segment.IsMapped) { run.Foreground = new SolidColorBrush(Color.Parse("#1FA88D")); run.FontWeight = FontWeight.SemiBold; }
                        preview.Inlines.Add(run);
                    }
                }
                analysis.PropertyChanged += OnPreview;
                void OnPreview(object? sender, System.ComponentModel.PropertyChangedEventArgs e) { if (e.PropertyName == nameof(AnalysisViewModel.PreviewSegments)) UpdatePreview(); }
                preview.DetachedFromVisualTree += (_, _) => analysis.PropertyChanged -= OnPreview;
                preview.AttachedToVisualTree += (_, _) => { analysis.PropertyChanged -= OnPreview; analysis.PropertyChanged += OnPreview; UpdatePreview(); };
                body.Children.Add(Card(Stack(Label("Preview", "section"), preview, BoundText("Statistics"), Label("NGrams", "section"), OutputBox("NGrams", 150))));
                break;
            case VigenereViewModel:
                body.Children.Add(Input());
                body.Children.Add(Card(Stack(Field("Key", "Key"), Row(Action("Encrypt", "encrypt", true), Action("Decrypt", "decrypt"), Swap()))));
                break;
            case OtpViewModel:
                body.Children.Add(Card(Label("OtpWarning", "caption")));
                body.Children.Add(Input(false));
                body.Children.Add(Card(Stack(Field("Base64Key", "Key", multiline: true),
                    Row(Action("GenerateKey", "generate"), Action("ImportKey", "import-key"), Action("ExportKey", "export-key")),
                    Row(Action("Encrypt", "encrypt", true), Action("Decrypt", "decrypt"), Swap()))));
                break;
            case MathViewModel:
                body.Children.Add(Card(Stack(Field("A", "A"), Field("B", "B"), Field("Exponent", "Exponent"),
                    Row(Action("Euclid", "euclid", true), Action("Power", "power")),
                    Row(Action("Random", "random"), Action("PrimeRange", "prime"), Action("Coprime", "coprime")))));
                break;
            case TheoryViewModel:
                body.Children.Add(Card(Stack(Field("JacobiA", "A"), Field("N", "N"), Field("Rounds", "Rounds"),
                    Row(Action("Jacobi", "jacobi", true), Action("MillerRabin", "miller-rabin"), Action("Solovay", "solovay")),
                    Field("PrimeBits", "Bits"), Row(Action("GeneratePrime", "generate")))));
                break;
            case RsaViewModel:
                body.Children.Add(Card(Check("ModernMode", "ModernMode")));
                body.Children.Add(ShowWhen(Card(Stack(Label("Standard", "section"), Label("RsaSession", "caption"),
                    Row(Action("GenerateKey", "generate", true), Action("PublicImport", "import-public"), Action("PublicExport", "export-public")),
                    Field("PublicKey", "PublicKey", multiline: true, readOnly: true),
                    new Expander { Name = "PrivateKeyExpander", Header = Label("PrivateExport", "caption"), HorizontalAlignment = HorizontalAlignment.Stretch,
                        Content = Stack(Field("Password", "Password", password: true), Check("PrivateConsent", "PrivateConsent"), Row(Action("PrivateImport", "import-private"), Action("PrivateExport", "export-private"))) })), "ModernMode"));
                body.Children.Add(ShowWhen(TextbookKey(), "ModernMode", true));
                body.Children.Add(Input(false));
                body.Children.Add(Card(Stack(Label("RsaInteger", "caption"), Row(Action("Encrypt", "encrypt", true), Action("Decrypt", "decrypt"), Swap()))));
                break;
            case SignatureViewModel:
                body.Children.Add(Card(Check("ModernMode", "ModernMode")));
                body.Children.Add(ShowWhen(Card(Stack(Label("Standard", "section"), Label("RsaSession", "caption"), Row(Action("GenerateKey", "generate", true)))), "ModernMode"));
                body.Children.Add(ShowWhen(TextbookKey(), "ModernMode", true));
                body.Children.Add(Card(Stack(Row(Action("ChooseFile", "choose-file"), Action("TextMode", "text-mode")), BoundText("FilePath"))));
                body.Children.Add(Input(false));
                body.Children.Add(Card(Stack(Field("SignatureValue", "Signature", multiline: true),
                    Row(Action("Sign", "sign", true), Action("Verify", "verify")),
                    Row(Action("ImportSignature", "import-signature"), Action("ExportSignature", "export-signature")))));
                break;
            case ShamirViewModel:
                body.Children.Add(Card(Stack(Label("ShamirWarning", "caption"), Field("Secret", "Secret"), Field("Threshold", "Threshold"),
                    Field("ShareCount", "ShareCount"), Field("FieldPrime", "Prime"), Row(Action("Split", "split", true)))));
                body.Children.Add(Card(Stack(Field("ShareJson", "ShareJson", multiline: true, height: 240),
                    Row(Action("Recover", "recover", true), Action("ImportShares", "import-shares"), Action("ExportShares", "export-shares")))));
                break;
        }
        var busy = new ProgressBar { IsIndeterminate = true, Height = 3, Margin = new Thickness(0, 0, 0, 10) };
        busy.Bind(Visual.IsVisibleProperty, new Binding("IsBusy"));
        var cancel = new Button { Name = "CancelButton", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 10) };
        cancel.Bind(ContentControl.ContentProperty, new Binding("L[Cancel]")); cancel.Bind(Button.CommandProperty, new Binding("CancelCommand")); cancel.Bind(Visual.IsVisibleProperty, new Binding("IsBusy"));
        var status = BoundText("Status"); status.Name = "StatusMessage"; status.Foreground = new SolidColorBrush(Color.Parse("#D68D42")); status.Margin = new Thickness(0, 0, 0, 10);
        var result = Stack(busy, cancel, status, Card(Stack(Label("Output", "section"), OutputBox("Output", 350),
            Row(BoundButton("Copy", "CopyCommand"), BoundButton("Export", "ExportCommand"), BoundButton("Clear", "ClearCommand")))));
        result.DataContext = model;
        var layout = new Grid { ColumnDefinitions = new ColumnDefinitions("*,350"), RowDefinitions = new RowDefinitions("Auto,Auto"), Name = model.Id + "View" };
        result.Margin = new Thickness(18, 0, 0, 0); Grid.SetColumn(result, 1);
        layout.Children.Add(body); layout.Children.Add(result);
        layout.SizeChanged += (_, e) =>
        {
            bool narrow = e.NewSize.Width < 860;
            layout.ColumnDefinitions[1].Width = narrow ? new GridLength(0) : new GridLength(350);
            Grid.SetRow(result, narrow ? 1 : 0); Grid.SetColumn(result, narrow ? 0 : 1);
            result.Margin = narrow ? new Thickness(0) : new Thickness(18, 0, 0, 0);
        };
        return layout;
    }

    private static Control Input(bool alphabet = true)
    {
        var panel = Stack(Label("Input", "section"));
        if (alphabet) panel.Children.Add(Stack(Label("Alphabet", "caption"), Check("UseUkrainian", "UkrainianAlphabet")));
        var editor = Editor("Text", 155); editor.Name = "InputText"; panel.Children.Add(editor);
        panel.Children.Add(Row(BoundButton("Import", "ImportCommand"), BoundButton("Paste", "PasteCommand")));
        return Card(panel);
    }
    private static Control TextbookKey() => Card(Stack(Label("Educational", "section"), Field("P", "P"), Field("Q", "Q"), Field("E", "E"), Row(Action("CalculateKey", "textbook-key"))));
    private static Control ShowWhen(Control control, string path, bool inverse = false)
    {
        control.Bind(Visual.IsVisibleProperty, new Binding(path) { Converter = new FuncValueConverter<bool, bool>(value => inverse ? !value : value) });
        return control;
    }
    private static Control DuplicateNotice()
    {
        var notice = Label("Duplicates", "caption"); notice.Foreground = Brushes.IndianRed;
        notice.Bind(Visual.IsVisibleProperty, new Binding("HasDuplicates")); return notice;
    }
    private static Control MappingTable()
    {
        var items = new ItemsControl { Name = "MappingTable" };
        items.Bind(ItemsControl.ItemsSourceProperty, new Binding("Mappings"));
        items.ItemsPanel = new FuncTemplate<Panel?>(() => new WrapPanel { Orientation = Orientation.Horizontal });
        items.ItemTemplate = new FuncDataTemplate<MappingRow>((row, _) =>
        {
            var source = new TextBlock { Text = row!.Source, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 3) };
            var target = new TextBox { Width = 43, MaxLength = 1, TextAlignment = TextAlignment.Center, Padding = new Thickness(3), Name = "Map" + row.Source };
            target.Bind(TextBox.TextProperty, new Binding("Target") { Mode = BindingMode.TwoWay });
            target.Bind(TemplatedControl.BorderBrushProperty, new Binding("Duplicate") { Converter = new FuncValueConverter<bool, IBrush>(duplicate => duplicate ? Brushes.IndianRed : Brushes.SlateGray) });
            return new StackPanel { Margin = new Thickness(0, 4, 7, 4), Children = { source, target } };
        });
        return Stack(Row(Label("Source", "caption"), Label("Target", "caption")), items);
    }
    private static Control FrequencyTable()
    {
        var items = new ItemsControl { Name = "FrequencyTable" }; items.Bind(ItemsControl.ItemsSourceProperty, new Binding("Rows"));
        items.ItemTemplate = new FuncDataTemplate<FrequencyRow>((row, _) =>
        {
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("90,100,130,130"), Margin = new Thickness(0, 3) };
            var values = new[] { row!.Symbol, row.Count.ToString(), row.Percentage.ToString("F3"), row.ReferencePercentage.ToString("F3") };
            for (int i = 0; i < values.Length; i++) { var text = new TextBlock { Text = values[i] }; Grid.SetColumn(text, i); grid.Children.Add(text); }
            return grid;
        });
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("90,100,130,130"), Margin = new Thickness(0, 10, 0, 6) };
        var keys = new[] { "Symbol", "Count", "Percent", "Reference" };
        for (int i = 0; i < keys.Length; i++) { var label = Label(keys[i], "caption"); Grid.SetColumn(label, i); header.Children.Add(label); }
        return Stack(header, new ScrollViewer { Content = items, MaxHeight = 260, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto });
    }
    private static TextBlock Label(string key, string? css = null)
    {
        var text = new TextBlock { TextWrapping = TextWrapping.Wrap }; text.Bind(TextBlock.TextProperty, new Binding("L[" + key + "]"));
        if (css is not null) text.Classes.Add(css); return text;
    }
    private static TextBlock BoundText(string path)
    {
        var text = new TextBlock { TextWrapping = TextWrapping.Wrap }; text.Bind(TextBlock.TextProperty, new Binding(path)); return text;
    }
    private static TextBox Editor(string path, double height, bool readOnly = false)
    {
        var box = new TextBox { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Height = height, IsReadOnly = readOnly,
            FontFamily = new FontFamily("Cascadia Mono,Consolas,monospace"), FontSize = 14 };
        box.Bind(TextBox.TextProperty, new Binding(path) { Mode = readOnly ? BindingMode.OneWay : BindingMode.TwoWay }); return box;
    }
    private static Control Field(string label, string path, bool multiline = false, bool readOnly = false, bool password = false, double height = 90)
    {
        TextBox box = multiline ? Editor(path, height, readOnly) : new TextBox { IsReadOnly = readOnly };
        if (!multiline) box.Bind(TextBox.TextProperty, new Binding(path) { Mode = readOnly ? BindingMode.OneWay : BindingMode.TwoWay });
        if (password) box.PasswordChar = '●';
        if (path is "A" or "B" or "N" or "P" or "Q" or "E" or "Shift" or "Exponent" or "Bits" or "Rounds" or "Secret" or "Threshold" or "ShareCount" or "Prime") box.MaxLength = 4096;
        box.Name = path + "Field";
        return Stack(Label(label, "caption"), box);
    }
    private static TextBox OutputBox(string path, double height) { var box = Editor(path, height, true); box.Name = path + "Text"; return box; }
    private static CheckBox Check(string label, string path)
    {
        var check = new CheckBox { Margin = new Thickness(0, 0, 14, 0), Name = path + "Toggle" };
        check.Bind(ContentControl.ContentProperty, new Binding("L[" + label + "]"));
        check.Bind(Avalonia.Controls.Primitives.ToggleButton.IsCheckedProperty, new Binding(path) { Mode = BindingMode.TwoWay });
        return check;
    }
    private static Button BoundButton(string label, string command)
    {
        var button = new Button { Margin = new Thickness(0, 5, 8, 0), Name = command + "Button" };
        button.Bind(ContentControl.ContentProperty, new Binding("L[" + label + "]")); button.Bind(Button.CommandProperty, new Binding(command)); return button;
    }
    private static Button Action(string label, string action, bool primary = false)
    {
        var button = BoundButton(label, "RunCommand"); button.CommandParameter = action; button.Name = action.Replace("-", "_") + "Button";
        if (primary) button.Classes.Add("primary"); return button;
    }
    private static Button Swap() => BoundButton("Swap", "SwapCommand");
    private static StackPanel Stack(params Control[] children)
    { var panel = new StackPanel { Spacing = 10 }; foreach (var child in children) panel.Children.Add(child); return panel; }
    private static WrapPanel Row(params Control[] children)
    { var panel = new WrapPanel { Orientation = Orientation.Horizontal }; foreach (var child in children) panel.Children.Add(child); return panel; }
    private static Border Card(Control content) { var border = new Border { Child = content }; border.Classes.Add("card"); return border; }
}
