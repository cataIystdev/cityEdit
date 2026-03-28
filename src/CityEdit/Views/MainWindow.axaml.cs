using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace CityEdit.Views;

/// <summary>
/// Код-behind главного окна.
/// Управляет навигацией по вкладкам через боковую панель.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Массив панелей вкладок для переключения видимости.
    /// </summary>
    private ScrollViewer?[] _tabs = null!;

    /// <summary>
    /// Массив кнопок навигации для управления active-состоянием.
    /// </summary>
    private Button[] _navButtons = null!;

    /// <summary>
    /// Конструктор. Инициализирует компоненты и настраивает навигацию.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Вызывается после открытия окна.
    /// Инициализирует массивы вкладок и кнопок навигации.
    /// </summary>
    protected override void OnOpened(System.EventArgs e)
    {
        base.OnOpened(e);

        _tabs = new ScrollViewer?[]
        {
            this.FindControl<ScrollViewer>("SurfersTab"),
            this.FindControl<ScrollViewer>("BoardsTab"),
            this.FindControl<ScrollViewer>("StatsTab"),
            this.FindControl<ScrollViewer>("WalletTab"),
            this.FindControl<ScrollViewer>("PurchasesTab"),
            this.FindControl<ScrollViewer>("SeasonTab"),
            this.FindControl<ScrollViewer>("FlagsTab")
        };

        _navButtons = CollectNavButtons();
        SwitchToTab(0);
    }

    /// <summary>
    /// Обработчик клика по кнопке навигации.
    /// </summary>
    private void OnNavClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tagStr && int.TryParse(tagStr, out int index))
        {
            SwitchToTab(index);
        }
    }

    /// <summary>
    /// Переключает видимость вкладок и стиль кнопок навигации.
    /// </summary>
    private void SwitchToTab(int index)
    {
        if (_tabs == null) return;

        for (int i = 0; i < _tabs.Length; i++)
        {
            if (_tabs[i] != null)
            {
                _tabs[i]!.IsVisible = (i == index);
            }
        }

        if (_navButtons != null)
        {
            for (int i = 0; i < _navButtons.Length; i++)
            {
                var classes = _navButtons[i].Classes;
                if (i == index)
                {
                    if (!classes.Contains("active"))
                        classes.Add("active");
                }
                else
                {
                    classes.Remove("active");
                }
            }
        }
    }

    /// <summary>
    /// Собирает навигационные кнопки через визуальное дерево Avalonia.
    /// </summary>
    private Button[] CollectNavButtons()
    {
        var buttons = new List<Button>();
        foreach (var visual in this.GetVisualDescendants())
        {
            if (visual is Button btn && btn.Classes.Contains("nav"))
            {
                buttons.Add(btn);
            }
        }
        buttons.Sort((a, b) =>
        {
            int tagA = a.Tag is string sA && int.TryParse(sA, out int iA) ? iA : 0;
            int tagB = b.Tag is string sB && int.TryParse(sB, out int iB) ? iB : 0;
            return tagA.CompareTo(tagB);
        });
        return buttons.ToArray();
    }
}
