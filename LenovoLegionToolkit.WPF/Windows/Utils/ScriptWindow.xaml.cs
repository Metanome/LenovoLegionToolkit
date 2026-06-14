using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Scripting;
using LenovoLegionToolkit.WPF.Resources;
using LenovoLegionToolkit.WPF.Utils;

namespace LenovoLegionToolkit.WPF.Windows.Utils;

public partial class ScriptWindow
{
    private static ScriptWindow? _instance;

    private readonly ScriptEngine _engine = IoCContainer.Resolve<ScriptEngine>();
    private readonly ThemeManager _themeManager = IoCContainer.Resolve<ThemeManager>();

    private OutputColorizingTransformer? _outputTransformer;

    private class OutputColorizingTransformer : DocumentColorizingTransformer
    {
        public Brush HeaderBrush { get; set; } = Brushes.Transparent;
        public Brush ErrorBrush { get; set; } = Brushes.Transparent;
        public Brush ElapsedBrush { get; set; } = Brushes.Transparent;

        private readonly string _elapsedPrefix;

        public OutputColorizingTransformer()
        {
            var elapsedStr = Resource.ScriptConsole_Section_Elapsed;
            var idx = elapsedStr.IndexOf("{0}");
            _elapsedPrefix = idx >= 0 ? elapsedStr.Substring(0, idx) : elapsedStr;
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            int lineStartOffset = line.Offset;
            string text = CurrentContext.Document.GetText(line);

            if (text.StartsWith("--- ") && text.EndsWith(" ---"))
            {
                ChangeLinePart(lineStartOffset, lineStartOffset + text.Length, element =>
                {
                    element.TextRunProperties.SetForegroundBrush(HeaderBrush);
                    var tf = element.TextRunProperties.Typeface;
                    element.TextRunProperties.SetTypeface(new Typeface(tf.FontFamily, tf.Style, FontWeights.SemiBold, tf.Stretch));
                });
            }
            else if (text.Contains("error CS") || text.Contains("Exception:") || text.Contains("Exception "))
            {
                ChangeLinePart(lineStartOffset, lineStartOffset + text.Length, element => element.TextRunProperties.SetForegroundBrush(ErrorBrush));
            }
            else if (!string.IsNullOrEmpty(_elapsedPrefix) && text.StartsWith(_elapsedPrefix))
            {
                ChangeLinePart(lineStartOffset, lineStartOffset + text.Length, element => element.TextRunProperties.SetForegroundBrush(ElapsedBrush));
            }
        }
    }

    private void OnThemeApplied(object? sender, EventArgs e) => UpdateSyntaxColors();

    private ScriptWindow()
    {
        InitializeComponent();

        _codeInput.TextArea.IndentationStrategy = new ICSharpCode.AvalonEdit.Indentation.CSharp.CSharpIndentationStrategy(_codeInput.Options);

        Loaded += (_, _) =>
        {
            _codeInput.Focus();
            UpdateSyntaxColors();
        };
        _codeInput.PreviewKeyDown += CodeInput_PreviewKeyDown;
        _themeManager.ThemeApplied += OnThemeApplied;
    }

    protected override void OnClosed(EventArgs e)
    {
        _themeManager.ThemeApplied -= OnThemeApplied;
        base.OnClosed(e);
    }

    public static void ShowInstance()
    {
        if (_instance is null)
        {
            _instance = new ScriptWindow();
            _instance.Closed += (_, _) => _instance = null;
            _instance.Show();
        }
        else
        {
            if (_instance.WindowState == WindowState.Minimized)
                _instance.WindowState = WindowState.Normal;
            _instance.Activate();
        }
    }

    private void CodeInput_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
        {
            e.Handled = true;
            _ = ExecuteAsync();
            return;
        }
    }

    private async void Execute_Click(object sender, RoutedEventArgs e) => await ExecuteAsync();

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        _outputBox.Text = string.Empty;
        _engine.Reset();
        SetStatus(null, StatusColor.Default);
        _codeInput.Focus();
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        _outputBox.Text = string.Empty;
    }

    private async Task ExecuteAsync()
    {
        var code = _codeInput.Text;
        if (string.IsNullOrWhiteSpace(code))
            return;

        _executeButton.IsEnabled = false;
        SetStatus(Resource.ScriptConsole_Status_Running, StatusColor.Default);
        _outputBox.Text = "";

        try
        {
            var result = await _engine.ExecuteAsync(code);

            _outputBox.Text = FormatResult(result);

            if (result.Error is not null)
                SetStatus(string.Format(Resource.ScriptConsole_Status_Error, $"{result.Elapsed.TotalMilliseconds:F0}"), StatusColor.Error);
            else
                SetStatus(string.Format(Resource.ScriptConsole_Status_Ok, $"{result.Elapsed.TotalMilliseconds:F0}"), StatusColor.Success);
        }
        catch (Exception ex)
        {
            _outputBox.Text = string.Format(Resource.ScriptConsole_UnexpectedError_Detail, ex.Message);
            SetStatus(Resource.ScriptConsole_Status_UnexpectedError, StatusColor.Error);
        }
        finally
        {
            _executeButton.IsEnabled = true;
        }
    }

    private enum StatusColor { Default, Success, Error }

    private void SetStatus(string? text, StatusColor color)
    {
        if (string.IsNullOrEmpty(text))
        {
            _statusBorder.Visibility = Visibility.Collapsed;
            return;
        }

        _statusLabel.Text = text;
        _statusBorder.Visibility = Visibility.Visible;

        if (color == StatusColor.Default)
        {
            _statusBorder.Background = Brushes.Transparent;
            _statusLabel.Foreground = (Brush)FindResource("TextFillColorSecondaryBrush");
            _statusIcon.Visibility = Visibility.Collapsed;
        }
        else
        {
            _statusBorder.Background = color switch
            {
                StatusColor.Success => (Brush)FindResource("SystemFillColorSuccessBackgroundBrush"),
                StatusColor.Error => (Brush)FindResource("SystemFillColorCriticalBackgroundBrush"),
                _ => Brushes.Transparent
            };

            _statusIcon.Foreground = color switch
            {
                StatusColor.Success => (Brush)FindResource("SystemFillColorSuccessBrush"),
                StatusColor.Error => (Brush)FindResource("SystemFillColorCriticalBrush"),
                _ => Brushes.Transparent
            };
            
            _statusIcon.Symbol = color == StatusColor.Success ? Wpf.Ui.Common.SymbolRegular.Checkmark24 : Wpf.Ui.Common.SymbolRegular.ErrorCircle24;
            _statusLabel.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            _statusIcon.Visibility = Visibility.Visible;
        }
    }

    private void UpdateSyntaxColors()
    {
        if (_codeInput.SyntaxHighlighting == null) return;

        Color GetColor(string resourceKey)
        {
            var res = Application.Current.TryFindResource(resourceKey);
            if (res is SolidColorBrush brush)
                return brush.Color;
            if (res is Color color)
                return color;
            return Colors.Gray;
        }

        var commentColor = GetColor("PaletteGreenColor");
        var stringColor = GetColor("PaletteOrangeColor");
        var keywordColor = GetColor("PaletteLightBlueColor");
        var textColor = GetColor("TextFillColorPrimaryBrush");
        var numberColor = GetColor("PaletteTealColor");
        var preprocessorColor = GetColor("TextFillColorSecondaryBrush");
        var methodColor = GetColor("PaletteYellowColor");

        void SetColor(string name, Color color)
        {
            var rule = _codeInput.SyntaxHighlighting.GetNamedColor(name);
            if (rule != null)
                rule.Foreground = new ICSharpCode.AvalonEdit.Highlighting.SimpleHighlightingBrush(color);
        }

        SetColor("Comment", commentColor);
        SetColor("String", stringColor);
        SetColor("StringInterpolation", textColor);
        SetColor("Char", stringColor);
        SetColor("Preprocessor", preprocessorColor);
        SetColor("MethodCall", methodColor);
        SetColor("SemanticKeywords", keywordColor);
        SetColor("ValueTypeKeywords", keywordColor);
        SetColor("ReferenceTypeKeywords", keywordColor);
        SetColor("NumberLiteral", numberColor);
        SetColor("ThisOrBaseReference", keywordColor);
        SetColor("NullOrValueKeywords", keywordColor);
        SetColor("Keywords", keywordColor);
        SetColor("GotoKeywords", keywordColor);
        SetColor("ContextKeywords", keywordColor);
        SetColor("ExceptionKeywords", keywordColor);
        SetColor("CheckedKeyword", keywordColor);
        SetColor("UnsafeKeywords", keywordColor);
        SetColor("OperatorKeywords", keywordColor);
        SetColor("ParameterModifiers", keywordColor);
        SetColor("Modifiers", keywordColor);
        SetColor("Visibility", keywordColor);
        SetColor("NamespaceKeywords", keywordColor);
        SetColor("GetSetAddRemove", keywordColor);
        SetColor("TrueFalse", keywordColor);
        SetColor("TypeKeywords", keywordColor);

        if (_outputTransformer == null && _outputBox.TextArea?.TextView != null)
        {
            _outputTransformer = new OutputColorizingTransformer();
            _outputBox.TextArea.TextView.LineTransformers.Add(_outputTransformer);
        }

        if (_outputTransformer != null)
        {
            _outputTransformer.HeaderBrush = new SolidColorBrush(GetColor("PaletteLightBlueColor"));
            _outputTransformer.ErrorBrush = new SolidColorBrush(GetColor("PaletteRedColor"));
            _outputTransformer.ElapsedBrush = new SolidColorBrush(GetColor("TextFillColorSecondaryBrush"));
        }

        _codeInput.TextArea?.TextView?.Redraw();
        _outputBox.TextArea?.TextView?.Redraw();
    }

    private static string FormatResult(ScriptResult result)
    {
        var sb = new System.Text.StringBuilder();

        void AppendSection(string title, string? content)
        {
            if (string.IsNullOrEmpty(content)) return;
            sb.Append("--- ").Append(title).AppendLine(" ---");
            sb.AppendLine(content.TrimEnd());
            sb.AppendLine();
        }

        AppendSection(Resource.ScriptConsole_Output_Title, result.Output);
        AppendSection(Resource.ScriptConsole_Section_ReturnValue, result.ReturnValue?.ToString());
        AppendSection(Resource.ScriptConsole_Section_Error, result.Error);

        sb.Append(string.Format(Resource.ScriptConsole_Section_Elapsed, $"{result.Elapsed.TotalMilliseconds:F0}"));
        return sb.ToString().TrimEnd();
    }
}
