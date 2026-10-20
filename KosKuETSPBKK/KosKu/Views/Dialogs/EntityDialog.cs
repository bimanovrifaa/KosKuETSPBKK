using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace KosKu.Views.Dialogs;

public sealed class EntityDialog : Window
{
    private readonly List<FormField> _fields;
    private readonly Dictionary<string, Control> _controls = new();
    private readonly TextBlock _validationText;

    public Dictionary<string, string> Values { get; } = new(StringComparer.OrdinalIgnoreCase);

    public EntityDialog(string title, IEnumerable<FormField> fields, IReadOnlyDictionary<string, string>? initialValues = null)
    {
        _fields = fields.ToList();
        Title = title;
        Width = 500;
        MinWidth = 440;
        SizeToContent = SizeToContent.Height;
        MaxHeight = 760;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.CanResize;
        Background = (Brush)Application.Current.FindResource("BrushAppBg");
        FontFamily = new FontFamily("Segoe UI");

        var outer = new Grid { Margin = new Thickness(22) };
        outer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        outer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        outer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var titleBlock = new TextBlock
        {
            Text = title,
            FontSize = 21,
            FontWeight = FontWeights.Bold,
            Foreground = (Brush)Application.Current.FindResource("BrushTextPrimary"),
            Margin = new Thickness(0, 0, 0, 16)
        };
        outer.Children.Add(titleBlock);

        var card = new Border
        {
            Background = (Brush)Application.Current.FindResource("BrushCardBg"),
            BorderBrush = (Brush)Application.Current.FindResource("BrushBorder"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(18),
            Margin = new Thickness(0, 38, 0, 0)
        };
        Grid.SetRow(card, 0);
        var fieldsPanel = new StackPanel();
        foreach (var field in _fields)
        {
            var fieldPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 13) };
            fieldPanel.Children.Add(new TextBlock
            {
                Text = field.Required ? $"{field.Label} *" : field.Label,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)Application.Current.FindResource("BrushTextSecondary"),
                Margin = new Thickness(0, 0, 0, 6)
            });
            Control control = CreateControl(field, initialValues);
            _controls[field.Key] = control;
            fieldPanel.Children.Add(control);
            fieldsPanel.Children.Add(fieldPanel);
        }
        card.Child = fieldsPanel;
        outer.Children.Add(card);

        var footer = new StackPanel { Margin = new Thickness(0, 14, 0, 0) };
        Grid.SetRow(footer, 1);
        _validationText = new TextBlock
        {
            Foreground = (Brush)Application.Current.FindResource("BrushDanger"),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10)
        };
        footer.Children.Add(_validationText);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var cancel = new Button { Content = "Batal", Style = (Style)Application.Current.FindResource("BtnOutline"), Margin = new Thickness(0, 0, 8, 0), MinWidth = 90 };
        cancel.Click += (_, _) => { DialogResult = false; };
        var save = new Button { Content = "Simpan", Style = (Style)Application.Current.FindResource("BtnPrimary"), MinWidth = 100 };
        save.Click += Save_Click;
        buttons.Children.Add(cancel);
        buttons.Children.Add(save);
        footer.Children.Add(buttons);
        outer.Children.Add(footer);
        Content = outer;
    }

    private Control CreateControl(FormField field, IReadOnlyDictionary<string, string>? initialValues)
    {
        initialValues ??= new Dictionary<string, string>();
        initialValues.TryGetValue(field.Key, out var initial);
        initial ??= field.DefaultValue;
        Control control;
        switch (field.Type)
        {
            case FormFieldType.Password:
                var password = new PasswordBox
                {
                    Height = 38,
                    Padding = new Thickness(10, 7, 10, 7),
                    FontSize = 13,
                    Background = (Brush)Application.Current.FindResource("BrushInputBg"),
                    Foreground = (Brush)Application.Current.FindResource("BrushInputText"),
                    BorderBrush = (Brush)Application.Current.FindResource("BrushBorder")
                };
                password.Password = initial ?? string.Empty;
                control = password;
                break;
            case FormFieldType.Date:
                DateTime? selectedDate = null;
                if (!string.IsNullOrWhiteSpace(initial) && DateTime.TryParse(initial, out var parsed)) selectedDate = parsed;
                var datePicker = new DatePicker { Height = 38, SelectedDate = selectedDate ?? DateTime.Today, FontSize = 13, Background = (Brush)Application.Current.FindResource("BrushInputBg"), Foreground = (Brush)Application.Current.FindResource("BrushInputText") };
                control = datePicker;
                break;
            case FormFieldType.Multiline:
                var multiline = new TextBox
                {
                    Style = (Style)Application.Current.FindResource("ModernTextBox"),
                    Text = initial ?? string.Empty,
                    Height = 88,
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalContentAlignment = VerticalAlignment.Top,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Padding = new Thickness(10, 8, 10, 8)
                };
                control = multiline;
                break;
            case FormFieldType.Combo:
                var combo = new ComboBox
                {
                    Style = (Style)Application.Current.FindResource("ModernComboBox"),
                    ItemsSource = field.Options,
                    DisplayMemberPath = nameof(FormOption.Label),
                    SelectedValuePath = nameof(FormOption.Value)
                };
                if (!string.IsNullOrWhiteSpace(initial)) combo.SelectedValue = initial;
                if (combo.SelectedItem is null && combo.Items.Count > 0) combo.SelectedIndex = 0;
                control = combo;
                break;
            default:
                var textBox = new TextBox
                {
                    Style = (Style)Application.Current.FindResource("ModernTextBox"),
                    Text = initial ?? string.Empty
                };
                if (field.Type == FormFieldType.Number) textBox.ToolTip = "Masukkan angka saja";
                control = textBox;
                break;
        }
        return control;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        Values.Clear();
        foreach (var field in _fields)
        {
            string value = _controls[field.Key] switch
            {
                TextBox textBox => textBox.Text.Trim(),
                PasswordBox passwordBox => passwordBox.Password,
                DatePicker datePicker => datePicker.SelectedDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty,
                ComboBox combo => Convert.ToString(combo.SelectedValue, CultureInfo.InvariantCulture) ?? string.Empty,
                _ => string.Empty
            };
            if (field.Required && string.IsNullOrWhiteSpace(value))
            {
                _validationText.Text = $"Kolom {field.Label} wajib diisi.";
                return;
            }
            if (field.Type == FormFieldType.Number && !string.IsNullOrWhiteSpace(value))
            {
                var normalized = value.Replace(".", "").Replace(",", ".");
                if (!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var number) || number <= 0)
                {
                    _validationText.Text = $"Kolom {field.Label} harus berupa angka lebih besar dari nol.";
                    return;
                }
                value = number.ToString(CultureInfo.InvariantCulture);
            }
            Values[field.Key] = value;
        }
        DialogResult = true;
    }
}
