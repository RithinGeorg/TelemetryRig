using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TelemetryRig.Wpf.Behaviors;

/// <summary>
/// MVVM-friendly DataGrid routed-event behavior.
///
/// WPF event bubbling means an event can start on a child element, such as a TextBlock inside a cell,
/// then bubble upward through DataGridCell -> DataGridRow -> DataGrid.
///
/// This behavior listens at the DataGrid level, finds the row that was clicked,
/// and forwards the row item into a ViewModel command.
/// </summary>
public static class DataGridBubbleBehavior
{
    public static readonly DependencyProperty RowDoubleClickCommandProperty = DependencyProperty.RegisterAttached(
        "RowDoubleClickCommand",
        typeof(ICommand),
        typeof(DataGridBubbleBehavior),
        new PropertyMetadata(null, OnRowDoubleClickCommandChanged));

    public static void SetRowDoubleClickCommand(DependencyObject element, ICommand? value)
        => element.SetValue(RowDoubleClickCommandProperty, value);

    public static ICommand? GetRowDoubleClickCommand(DependencyObject element)
        => (ICommand?)element.GetValue(RowDoubleClickCommandProperty);

    private static void OnRowDoubleClickCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid dataGrid)
            return;

        if (e.OldValue is not null)
            dataGrid.RemoveHandler(Control.MouseDoubleClickEvent, new MouseButtonEventHandler(OnMouseDoubleClick));

        if (e.NewValue is not null)
        {
            // AddHandler lets us catch routed events at the DataGrid level.
            dataGrid.AddHandler(Control.MouseDoubleClickEvent, new MouseButtonEventHandler(OnMouseDoubleClick), handledEventsToo: true);
        }
    }

    private static void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid dataGrid)
            return;

        var row = FindVisualParent<DataGridRow>((DependencyObject)e.OriginalSource);
        if (row?.Item is null)
            return;

        var command = GetRowDoubleClickCommand(dataGrid);
        if (command?.CanExecute(row.Item) == true)
        {
            command.Execute(row.Item);
            e.Handled = true;
        }
    }

    private static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
    {
        var current = child;
        while (current is not null)
        {
            if (current is T correctlyTyped)
                return correctlyTyped;

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }
}
