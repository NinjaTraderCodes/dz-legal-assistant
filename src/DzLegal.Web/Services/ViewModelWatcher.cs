using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using DzLegal.Presentation.ViewModels;

namespace DzLegal.Web.Services;

/// <summary>
/// Calls <c>changed</c> whenever anything the page shows changes in the view model: a property, an item of a list, a list
/// itself. The page then re-renders. This is what XAML bindings do on the desktop; Blazor needs to be told.
/// </summary>
public sealed class ViewModelWatcher : IDisposable
{
    private readonly Action _changed;
    private readonly HashSet<INotifyPropertyChanged> _objects = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<INotifyCollectionChanged> _lists = new(ReferenceEqualityComparer.Instance);

    public ViewModelWatcher(MainWindowViewModel vm, Action changed)
    {
        ArgumentNullException.ThrowIfNull(vm);
        _changed = changed;
        Watch(vm);
        Watch(vm.L);
        foreach (var activity in new[] { vm.AnalysisActivity, vm.GenerationActivity, vm.AssistantActivity })
        {
            Watch(activity);
            WatchList(activity.Steps);
        }

        foreach (var group in vm.FieldGroups)
        {
            Watch(group);
            foreach (var field in group.Fields)
            {
                Watch(field);
            }
        }

        WatchList(vm.Sections);
        WatchList(vm.Findings);
        WatchList(vm.IssueFindings);
        WatchList(vm.PresentFindings);
        WatchList(vm.AnalysedArticles);
        WatchList(vm.ArticleResults);
        WatchList(vm.SelectedGrounds);
        WatchList(vm.RequestChips);
        WatchList(vm.DocumentChips);
        WatchList(vm.AssistantThread);
        WatchList(vm.ProposalDiff);
    }

    public void Dispose()
    {
        foreach (var o in _objects)
        {
            o.PropertyChanged -= OnPropertyChanged;
        }

        foreach (var l in _lists)
        {
            l.CollectionChanged -= OnCollectionChanged;
        }

        _objects.Clear();
        _lists.Clear();
    }

    private void Watch(INotifyPropertyChanged item)
    {
        if (_objects.Add(item))
        {
            item.PropertyChanged += OnPropertyChanged;
        }
    }

    private void WatchList(INotifyCollectionChanged list)
    {
        if (_lists.Add(list))
        {
            list.CollectionChanged += OnCollectionChanged;
        }

        WatchItems(list as IEnumerable);
    }

    private void WatchItems(IEnumerable? items)
    {
        foreach (var item in items?.OfType<INotifyPropertyChanged>() ?? [])
        {
            Watch(item);
        }
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e) => _changed();

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // New rows (sections, articles…) are watched too; removed ones simply stop mattering.
        WatchItems(e.NewItems);
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            WatchItems(sender as IEnumerable);
        }

        _changed();
    }
}
