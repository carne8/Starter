using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;

namespace Starter.Desktop.Controls;

public class CustomSelectableTextBlock : SelectableTextBlock
{
    public static readonly StyledProperty<INotifyCollectionChanged?> InlinesSourceProperty =
        AvaloniaProperty.Register<CustomSelectableTextBlock, INotifyCollectionChanged?>(nameof(InlinesSource));

    public INotifyCollectionChanged? InlinesSource
    {
        get => GetValue(InlinesSourceProperty);
        set => SetValue(InlinesSourceProperty, value);
    }

    private INotifyCollectionChanged? previousBindedCollection;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == InlinesSourceProperty)
        {
            previousBindedCollection?.CollectionChanged -= OnCollectionChanged;
            Inlines?.Clear();

            if (InlinesSource is not null)
            {
                InlinesSource.CollectionChanged += OnCollectionChanged;
                previousBindedCollection = InlinesSource;
            }
        }

        base.OnPropertyChanged(change);
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems == null) break;
                foreach (var newItem in e.NewItems)
                {
                    if (newItem is not Inline newInline) continue;
                    Inlines?.Add(newInline);
                }
                break;

            case NotifyCollectionChangedAction.Move:
            case NotifyCollectionChangedAction.Replace:
                if (e.NewItems?[0] is not Inline inline) return;
                Inlines?.RemoveAt(e.OldStartingIndex);
                Inlines?.Insert(e.NewStartingIndex, inline);
                break;

            case NotifyCollectionChangedAction.Remove:
                Inlines?.RemoveAt(e.OldStartingIndex);
                break;
            case NotifyCollectionChangedAction.Reset:
                Inlines?.Clear();
                break;
        }
    }
}
