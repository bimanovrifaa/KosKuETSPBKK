namespace KosKu.Views.Dialogs;

public enum FormFieldType
{
    Text,
    Multiline,
    Number,
    Password,
    Date,
    Combo
}

public sealed class FormOption
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public override string ToString() => Label;
}

public sealed class FormField
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public FormFieldType Type { get; set; } = FormFieldType.Text;
    public bool Required { get; set; } = true;
    public string? DefaultValue { get; set; }
    public List<FormOption> Options { get; set; } = new();
}
