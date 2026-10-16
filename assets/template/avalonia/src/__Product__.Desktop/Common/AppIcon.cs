using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using __Product__.Presentation.Resources;

namespace __Product__.Desktop.Common;

// A Fluent outline icon by name: <common:AppIcon Kind="Settings" />. Directional icons are
// mirrored by the caller in RTL (localization.md).
public sealed class AppIcon : PathIcon
{
    public static readonly StyledProperty<IconKind> KindProperty =
        AvaloniaProperty.Register<AppIcon, IconKind>(nameof(Kind));

    static AppIcon() =>
        KindProperty.Changed.AddClassHandler<AppIcon>((icon, _) => icon.Data = StreamGeometry.Parse(IconData.Get(icon.Kind)));

    public IconKind Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(PathIcon);
}
