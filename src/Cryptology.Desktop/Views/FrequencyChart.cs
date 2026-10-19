using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Cryptology.Core.Cryptanalysis;

namespace Cryptology.Desktop.Views;

/// <summary>Small, dependency-free chart; exact values remain available in the table and CSV.</summary>
public sealed class FrequencyChart : Control
{
    public static readonly StyledProperty<IReadOnlyList<FrequencyRow>> RowsProperty =
        AvaloniaProperty.Register<FrequencyChart, IReadOnlyList<FrequencyRow>>(nameof(Rows), []);
    public IReadOnlyList<FrequencyRow> Rows { get => GetValue(RowsProperty); set => SetValue(RowsProperty, value); }
    static FrequencyChart() => AffectsRender<FrequencyChart>(RowsProperty);
    public FrequencyChart() { Height = 230; ClipToBounds = true; }
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var rows = Rows.Where(r => r.Symbol.Length == 1 && char.IsLetter(r.Symbol[0])).Take(40).ToArray();
        if (rows.Length == 0) return;
        double max = Math.Max(1, rows.Max(r => Math.Max(r.Percentage, r.ReferencePercentage)));
        double width = Math.Max(1, (Bounds.Width - 36) / rows.Length); double height = Bounds.Height - 40;
        var observed = new SolidColorBrush(Color.Parse("#29BFA1"));
        var reference = new SolidColorBrush(Color.Parse("#598DDD"));
        for (var i = 0; i < rows.Length; i++)
        {
            double x = 32 + width * i;
            context.DrawRectangle(observed, null, new Rect(x, height * (1 - rows[i].Percentage / max), width * .38, height * rows[i].Percentage / max));
            context.DrawRectangle(reference, null, new Rect(x + width * .40, height * (1 - rows[i].ReferencePercentage / max), width * .30, height * rows[i].ReferencePercentage / max));
            var label = new FormattedText(rows[i].Symbol, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 10, reference);
            context.DrawText(label, new Point(x, height + 6));
        }
        var scale = new FormattedText($"{max:F1}%", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 10, reference);
        context.DrawText(scale, new Point(0, 0));
    }
}
