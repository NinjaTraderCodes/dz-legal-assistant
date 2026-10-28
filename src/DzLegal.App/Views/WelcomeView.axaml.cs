using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using DzLegal.App.Services;

namespace DzLegal.App.Views;

public partial class WelcomeView : UserControl
{
    public WelcomeView() : this(WelcomeMotion.IsEnabled()) { }

    public WelcomeView(bool animate)
    {
        InitializeComponent();
        // Resource names are case-sensitive, even on Windows. Accept the approved asset's
        // existing casing without renaming, copying, or rewriting the supplied files.
        var image = AssetLoader.GetAssets(new Uri("avares://DzLegalAssistant/"), null)
            .Single(uri => uri.AbsolutePath.Equals("/Assets/Images/welcome-bg.jpg", StringComparison.OrdinalIgnoreCase));
        using (var stream = AssetLoader.Open(image))
        {
            WelcomeBackground.Source = new Bitmap(stream);
        }

        if (animate)
        {
            Loaded += async (_, _) => await new Animation
            {
                Duration = TimeSpan.FromMilliseconds(120),
                Children =
                {
                    new KeyFrame { Cue = new Cue(0), Setters = { new Setter(OpacityProperty, 0d) } },
                    new KeyFrame { Cue = new Cue(1), Setters = { new Setter(OpacityProperty, 1d) } },
                },
            }.RunAsync(this);
        }
    }
}
