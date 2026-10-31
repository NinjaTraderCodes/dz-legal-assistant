namespace DzLegal.Presentation.ViewModels;

/// <summary>Optional desktop navigation; shared screen logic does not require guide windows.</summary>
public sealed record HomeGuideActions(Func<Task> Sample, Func<Task> Tour, Func<Task> Help, Func<Task>? Library = null, Func<Task>? About = null);
