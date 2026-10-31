using CommunityToolkit.Mvvm.Input;

namespace DzLegal.Presentation.ViewModels;

public sealed class TourOfferViewModel(Loc localization, Action yes, Action notNow)
{
    public Loc L { get; } = localization;
    public bool IsRightToLeft => L.IsRightToLeft;
    public IRelayCommand YesCommand { get; } = new RelayCommand(yes);
    public IRelayCommand NotNowCommand { get; } = new RelayCommand(notNow);
}
