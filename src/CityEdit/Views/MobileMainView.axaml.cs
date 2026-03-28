using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System.Linq;

namespace CityEdit.Views;

/// <summary>
/// Мобильный главный экран приложения CityEdit.
/// Используется на Android через ISingleViewApplicationLifetime.
/// Управляет горизонтальной навигацией по вкладкам.
/// </summary>
public partial class MobileMainView : UserControl
{
    /// <summary>
    /// Массив ScrollViewer для каждой вкладки.
    /// Индексы совпадают с Tab Tag.
    /// </summary>
    private ScrollViewer?[] _tabs = new ScrollViewer?[7];

    /// <summary>
    /// Список кнопок навигации для управления стилями.
    /// </summary>
    private List<Button>? _navButtons;

    /// <summary>
    /// Конструктор.
    /// </summary>
    public MobileMainView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Вызывается при загрузке View. Инициализирует вкладки и навигацию.
    /// </summary>
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _tabs[0] = this.FindControl<ScrollViewer>("QuickTab");
        _tabs[1] = this.FindControl<ScrollViewer>("SurfersTab");
        _tabs[2] = this.FindControl<ScrollViewer>("BoardsTab");
        _tabs[3] = this.FindControl<ScrollViewer>("StatsTab");
        _tabs[4] = this.FindControl<ScrollViewer>("WalletTab");
        _tabs[5] = this.FindControl<ScrollViewer>("PurchasesTab");
        _tabs[6] = this.FindControl<ScrollViewer>("SeasonTab");
        _tabs[7] = this.FindControl<ScrollViewer>("FlagsTab");

        CollectNavButtons();
        SwitchToTab(0);

        // Инициализируем мобильный режим (проверка Shizuku)
        if (DataContext is CityEdit.ViewModels.MainWindowViewModel vm)
        {
            vm.InitializeMobileCommand.Execute(null);
        }
    }

    /// <summary>
    /// Обработчик нажатия на кнопку навигации.
    /// Считывает Tag кнопки для определения целевой вкладки.
    /// </summary>
    private void OnNavClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tagStr && int.TryParse(tagStr, out int idx))
        {
            SwitchToTab(idx);
        }
    }

    /// <summary>
    /// Переключает видимость вкладок и обновляет стили кнопок навигации.
    /// </summary>
    /// <param name="tabIndex">Индекс целевой вкладки.</param>
    private void SwitchToTab(int tabIndex)
    {
        for (int i = 0; i < _tabs.Length; i++)
        {
            if (_tabs[i] != null)
            {
                _tabs[i]!.IsVisible = (i == tabIndex);
            }
        }

        if (_navButtons != null)
        {
            for (int i = 0; i < _navButtons.Count; i++)
            {
                UpdateNavButtonStyle(_navButtons[i], i == tabIndex);
            }
        }
    }

    /// <summary>
    /// Собирает кнопки навигации из визуального дерева TabStrip.
    /// </summary>
    private void CollectNavButtons()
    {
        var strip = this.FindControl<StackPanel>("TabStrip");
        if (strip != null)
        {
            _navButtons = strip.GetVisualDescendants()
                .OfType<Button>()
                .Where(b => b.Classes.Contains("nav"))
                .ToList();
        }
    }

    /// <summary>
    /// Обновляет визуальный стиль кнопки навигации.
    /// Активная кнопка получает яркий фон, неактивная -- прозрачный.
    /// </summary>
    /// <param name="btn">Кнопка.</param>
    /// <param name="isActive">Признак активности.</param>
    private static void UpdateNavButtonStyle(Button btn, bool isActive)
    {
        if (isActive)
        {
            btn.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF"));
            btn.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#0A0A0A"));
        }
        else
        {
            btn.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#00000000"));
            btn.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#808080"));
        }
    }
}
