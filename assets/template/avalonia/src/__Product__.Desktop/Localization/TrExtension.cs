using Avalonia.Markup.Xaml;

namespace __Product__.Desktop.Localization;

// XAML text from the shared resources: Text="{l:Tr Key=App_Name}". Resolved once; the language
// changes with a restart.
public sealed class TrExtension : MarkupExtension
{
    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider) => Tr.Get(Key);
}
