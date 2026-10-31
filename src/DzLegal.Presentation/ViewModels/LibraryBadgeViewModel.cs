using CommunityToolkit.Mvvm.Input;

namespace DzLegal.Presentation.ViewModels;

/// <summary>
/// One of the two independent facts shown for a legal text. The tone selects a style class in
/// Library.axaml, so a new value can be added without changing the card layout.
/// </summary>
public sealed class LibraryBadgeViewModel(
    string text,
    string hint,
    string icon,
    string tone,
    IAsyncRelayCommand whatCommand,
    string whatLabel)
{
    public string Text { get; } = text;
    public string Hint { get; } = hint;
    public string Icon { get; } = icon;
    public string Tone { get; } = tone;
    public IAsyncRelayCommand WhatCommand { get; } = whatCommand;
    public string WhatLabel { get; } = whatLabel;

    public bool IsJournal => Tone == "journal";
    public bool IsPdf => Tone == "pdf";
    public bool IsImages => Tone == "images";
    public bool IsPending => Tone == "pending";
    public bool IsReviewed => Tone == "reviewed";
}
