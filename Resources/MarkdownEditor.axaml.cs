using Avalonia;
using Avalonia.Controls;

namespace VSuiteLab.Resources;

public partial class MarkdownEditor : UserControl
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<MarkdownEditor, string?>(nameof(Text));

    public static readonly StyledProperty<bool> IsPreviewModeProperty =
        AvaloniaProperty.Register<MarkdownEditor, bool>(nameof(IsPreviewMode));

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public bool IsPreviewMode
    {
        get => GetValue(IsPreviewModeProperty);
        set => SetValue(IsPreviewModeProperty, value);
    }

    public MarkdownEditor()
    {
        InitializeComponent();
    }
}