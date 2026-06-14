using FileCraft.Models;
using FileCraft.ViewModels.Functional;
using Fonts;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Binding = System.Windows.Data.Binding;
using Control = System.Windows.Controls.Control;
using DataGrid = System.Windows.Controls.DataGrid;

namespace FileCraft.Views
{
    public partial class CsvViewerView : UserControl
    {
        public CsvViewerView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            CsvDataGrid.Sorting += CsvDataGrid_Sorting;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is CsvViewerViewModel oldViewModel)
            {
                oldViewModel.Columns.CollectionChanged -= Columns_CollectionChanged;
            }

            if (e.NewValue is CsvViewerViewModel newViewModel)
            {
                newViewModel.Columns.CollectionChanged += Columns_CollectionChanged;
                RebuildColumns(newViewModel);
            }
        }

        private void Columns_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (DataContext is CsvViewerViewModel viewModel)
            {
                RebuildColumns(viewModel);
            }
        }

        private void RebuildColumns(CsvViewerViewModel viewModel)
        {
            CsvDataGrid.Columns.Clear();

            CsvDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "#",
                Binding = new Binding(nameof(CsvRow.RowNumber)),
                SortMemberPath = nameof(CsvRow.RowNumber),
                Width = new DataGridLength(56),
                MinWidth = 48,
                IsReadOnly = true
            });

            foreach (var column in viewModel.Columns)
            {
                CsvDataGrid.Columns.Add(CreateCellColumn(column));
            }
        }

        private DataGridTemplateColumn CreateCellColumn(CsvColumn column)
        {
            var template = new DataTemplate();

            var panel = new FrameworkElementFactory(typeof(DockPanel));
            panel.SetValue(FrameworkElement.MarginProperty, new Thickness(0));
            panel.SetValue(DockPanel.LastChildFillProperty, true);
            panel.SetBinding(FrameworkElement.TagProperty, new Binding($"Cells[{column.Index}]"));
            panel.AddHandler(MouseLeftButtonDownEvent, new MouseButtonEventHandler(CellPanel_MouseLeftButtonDown), true);

            var button = new FrameworkElementFactory(typeof(Button));
            button.SetValue(DockPanel.DockProperty, Dock.Right);
            button.SetValue(ContentControl.ContentProperty, MaterialIcons.content_copy);
            button.SetResourceReference(Control.FontFamilyProperty, "MaterialIcons");
            button.SetResourceReference(Control.ForegroundProperty, "TextBrush");
            button.SetResourceReference(FrameworkElement.StyleProperty, "CsvCellCopyButtonStyle");
            button.SetBinding(Button.CommandProperty, new Binding("DataContext.CopyCellCommand")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGrid), 1)
            });
            button.SetBinding(Button.CommandParameterProperty, new Binding($"Cells[{column.Index}]"));
            panel.AppendChild(button);

            var textBlock = new FrameworkElementFactory(typeof(TextBlock));
            textBlock.SetValue(FrameworkElement.MarginProperty, new Thickness(8, 0, 4, 0));
            textBlock.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            textBlock.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
            textBlock.SetValue(TextBlock.TextWrappingProperty, TextWrapping.NoWrap);
            textBlock.SetBinding(TextBlock.TextProperty, new Binding($"Cells[{column.Index}].DisplayValue"));
            textBlock.SetBinding(FrameworkElement.ToolTipProperty, new Binding($"Cells[{column.Index}].ShortPreview"));
            panel.AppendChild(textBlock);

            template.VisualTree = panel;

            return new DataGridTemplateColumn
            {
                Header = column.DisplayName,
                CellTemplate = template,
                SortMemberPath = $"Cells[{column.Index}].Value",
                Width = new DataGridLength(column.Width),
                MinWidth = 90,
                CanUserResize = true
            };
        }

        private void CellPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2 || sender is not FrameworkElement element || element.Tag is not CsvCell cell)
            {
                return;
            }

            if (DataContext is CsvViewerViewModel viewModel && viewModel.ViewCellCommand.CanExecute(cell))
            {
                viewModel.ViewCellCommand.Execute(cell);
            }
        }

        private void CsvDataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = true;

            var direction = e.Column.SortDirection != ListSortDirection.Ascending
                ? ListSortDirection.Ascending
                : ListSortDirection.Descending;

            foreach (var column in CsvDataGrid.Columns)
            {
                column.SortDirection = null;
            }

            e.Column.SortDirection = direction;

            var view = CollectionViewSource.GetDefaultView(CsvDataGrid.ItemsSource);
            if (view is ListCollectionView listView)
            {
                listView.CustomSort = CreateComparer(e.Column.SortMemberPath, direction);
            }
        }

        private static IComparer CreateComparer(string sortMemberPath, ListSortDirection direction)
        {
            if (sortMemberPath == nameof(CsvRow.RowNumber))
            {
                return new CsvRowComparer(row => row.RowNumber, direction);
            }

            int columnIndex = GetColumnIndex(sortMemberPath);
            return new CsvRowComparer(row =>
            {
                if (columnIndex < 0 || columnIndex >= row.Cells.Count)
                {
                    return string.Empty;
                }

                return row.Cells[columnIndex].Value;
            }, direction);
        }

        private static int GetColumnIndex(string sortMemberPath)
        {
            const string prefix = "Cells[";
            const string suffix = "].Value";

            if (!sortMemberPath.StartsWith(prefix, StringComparison.Ordinal) ||
                !sortMemberPath.EndsWith(suffix, StringComparison.Ordinal))
            {
                return -1;
            }

            string indexText = sortMemberPath.Substring(prefix.Length, sortMemberPath.Length - prefix.Length - suffix.Length);
            return int.TryParse(indexText, out int index) ? index : -1;
        }

        private class CsvRowComparer : IComparer, IComparer<CsvRow>
        {
            private readonly Func<CsvRow, object> _valueSelector;
            private readonly ListSortDirection _direction;

            public CsvRowComparer(Func<CsvRow, object> valueSelector, ListSortDirection direction)
            {
                _valueSelector = valueSelector;
                _direction = direction;
            }

            public int Compare(object? x, object? y)
            {
                return Compare((CsvRow?)x, (CsvRow?)y);
            }

            public int Compare(CsvRow? x, CsvRow? y)
            {
                int result = CompareValues(x == null ? null : _valueSelector(x), y == null ? null : _valueSelector(y));
                return _direction == ListSortDirection.Ascending ? result : -result;
            }

            private static int CompareValues(object? x, object? y)
            {
                if (ReferenceEquals(x, y))
                {
                    return 0;
                }

                if (x == null)
                {
                    return -1;
                }

                if (y == null)
                {
                    return 1;
                }

                if (x is int leftInt && y is int rightInt)
                {
                    return leftInt.CompareTo(rightInt);
                }

                return StringComparer.CurrentCultureIgnoreCase.Compare(x.ToString(), y.ToString());
            }
        }

    }
}
