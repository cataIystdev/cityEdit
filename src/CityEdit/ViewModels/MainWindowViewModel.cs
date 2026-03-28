using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CityEdit.Core;
using CityEdit.Crypto;
using CityEdit.Models.GameData;
using CityEdit.Services;
using CityEdit.ViewModels.Items;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CityEdit.ViewModels;

/// <summary>
/// Главный ViewModel приложения CityEdit.
/// Управляет навигацией по вкладкам, командами файловых операций,
/// быстрыми действиями, интеграцией с Shizuku (Android) и содержит все дочерние ViewModel.
/// Поддерживает два режима работы: десктопный и мобильный.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    /// <summary>
    /// Сервис работы с профилем.
    /// </summary>
    private readonly ProfileService _profileService = new();

    /// <summary>
    /// Сервис доступа к файлам. Платформозависимый.
    /// </summary>
    private IFileAccessService? _fileAccessService;

    /// <summary>
    /// Признак мобильного режима.
    /// </summary>
    public bool IsMobile { get; }

    // ---- Свойства состояния ----

    /// <summary>
    /// Индекс текущей выбранной вкладки.
    /// </summary>
    [ObservableProperty]
    private int _selectedTabIndex;

    /// <summary>
    /// Признак того, что профиль загружен.
    /// </summary>
    [ObservableProperty]
    private bool _isProfileLoaded;

    /// <summary>
    /// Имя загруженного файла для отображения в статус-баре.
    /// </summary>
    [ObservableProperty]
    private string _loadedFileName = "";

    /// <summary>
    /// Сообщение статуса для отображения пользователю.
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = "Ready";

    /// <summary>
    /// Признак наличия несохранённых изменений.
    /// </summary>
    [ObservableProperty]
    private bool _hasChanges;

    // ---- Shizuku (Android) ----

    /// <summary>
    /// Признак доступности Shizuku (актуально на Android).
    /// </summary>
    [ObservableProperty]
    private bool _isShizukuAvailable;

    /// <summary>
    /// Признак наличия разрешения Shizuku.
    /// </summary>
    [ObservableProperty]
    private bool _hasShizukuPermission;

    /// <summary>
    /// Текст статуса Shizuku для отображения пользователю.
    /// </summary>
    [ObservableProperty]
    private string _shizukuStatusText = "";

    /// <summary>
    /// Признак того, что показывается экран Shizuku (профиль не загружен, Shizuku недоступен).
    /// </summary>
    [ObservableProperty]
    private bool _showShizukuScreen;

    /// <summary>
    /// Признак загрузки (операция в процессе).
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    // ---- Коллекции для вкладок ----

    /// <summary>
    /// Список серферов.
    /// </summary>
    public ObservableCollection<SurferItemViewModel> Surfers { get; } = new();

    /// <summary>
    /// Список досок.
    /// </summary>
    public ObservableCollection<BoardItemViewModel> Boards { get; } = new();

    /// <summary>
    /// Список элементов статистики.
    /// </summary>
    public ObservableCollection<StatItemViewModel> Stats { get; } = new();

    /// <summary>
    /// Список элементов кошелька.
    /// </summary>
    public ObservableCollection<WalletItemViewModel> WalletItems { get; } = new();

    /// <summary>
    /// Список покупок.
    /// </summary>
    public ObservableCollection<PurchaseItemViewModel> Purchases { get; } = new();

    /// <summary>
    /// Список доступных покупок для добавления (отфильтрованный).
    /// </summary>
    public ObservableCollection<PurchaseEntry> FilteredPurchaseEntries { get; } = new();

    /// <summary>
    /// Выбранная покупка для добавления.
    /// </summary>
    [ObservableProperty]
    private PurchaseEntry? _selectedPurchaseEntry;

    /// <summary>
    /// Текст поиска покупок.
    /// </summary>
    [ObservableProperty]
    private string _purchaseSearchText = "";

    /// <summary>
    /// Выбранная категория покупок.
    /// </summary>
    [ObservableProperty]
    private string _selectedPurchaseCategory = "All";

    /// <summary>
    /// Список категорий покупок.
    /// </summary>
    public ObservableCollection<string> PurchaseCategories { get; } = new();

    /// <summary>
    /// Выбранная покупка для удаления.
    /// </summary>
    [ObservableProperty]
    private PurchaseItemViewModel? _selectedPurchase;

    // ---- Сезонный пропуск ----

    /// <summary>
    /// Куплен ли сезонный пропуск.
    /// </summary>
    [ObservableProperty]
    private bool _seasonPassPurchased;

    /// <summary>
    /// Очки сезонного пропуска.
    /// </summary>
    [ObservableProperty]
    private int _seasonPassPoints;

    // ---- Флаги ----

    /// <summary>
    /// Список флагов профиля.
    /// </summary>
    public ObservableCollection<FlagItemViewModel> Flags { get; } = new();

    // ---- Активный серфер/доска ----

    /// <summary>
    /// Имя текущего выбранного серфера.
    /// </summary>
    [ObservableProperty]
    private string _selectedSurferName = "—";

    /// <summary>
    /// Имя текущей выбранной доски.
    /// </summary>
    [ObservableProperty]
    private string _selectedBoardName = "—";

    /// <summary>
    /// Список имён серферов для выбора.
    /// </summary>
    public ObservableCollection<string> SurferNames { get; } = new();

    /// <summary>
    /// Список имён досок для выбора.
    /// </summary>
    public ObservableCollection<string> BoardNames { get; } = new();

    // ---- Названия вкладок ----

    /// <summary>
    /// Список имён вкладок для навигации.
    /// На мобильном добавляется вкладка "QUICK" в начало.
    /// </summary>
    public string[] TabNames { get; }

    /// <summary>
    /// Конструктор. Поддерживает десктопный и мобильный режимы.
    /// </summary>
    /// <param name="isMobile">true для мобильного (Android) режима.</param>
    public MainWindowViewModel(bool isMobile = false)
    {
        IsMobile = isMobile;

        TabNames = isMobile
            ? new[] { "QUICK", "SURFERS", "BOARDS", "STATS", "WALLET", "PURCHASES", "SEASON", "FLAGS" }
            : new[] { "SURFERS", "BOARDS", "STATS", "WALLET", "PURCHASES", "SEASON", "FLAGS" };

        // Инициализируем списки имён серферов и досок
        foreach (var (_, name) in SurferDatabase.Names)
            SurferNames.Add(name);
        foreach (var (_, name) in BoardDatabase.Names)
            BoardNames.Add(name);

        PurchaseCategories.Add("All");
        foreach (var cat in PurchaseDatabase.Categories)
            PurchaseCategories.Add(cat);
        ApplyPurchaseFilter();
    }

    /// <summary>
    /// Таймер для периодической проверки Shizuku после запроса разрешения.
    /// </summary>
    private Timer? _shizukuPollTimer;

    /// <summary>
    /// Устанавливает платформозависимый сервис доступа к файлам.
    /// Вызывается из Android-кода после инициализации.
    /// </summary>
    /// <param name="service">Экземпляр IFileAccessService.</param>
    public void SetFileAccessService(IFileAccessService service)
    {
        _fileAccessService = service;
    }

    /// <summary>
    /// Вызывается из Android при получении результата запроса разрешения.
    /// Запускает CheckShizukuAndLoad на UI-потоке.
    /// </summary>
    public void OnShizukuPermissionGranted(bool granted)
    {
        Dispatcher.UIThread.Post(() =>
        {
            StopShizukuPolling();
            if (granted)
            {
                StatusMessage = "Разрешение Shizuku получено!";
                CheckShizukuAndLoad();
            }
            else
            {
                ShizukuStatusText = "Разрешение Shizuku отклонено.\nНажмите кнопку для повторного запроса.";
            }
        });
    }

    /// <summary>
    /// Инициализирует мобильный режим: проверяет Shizuku и загружает профиль.
    /// Если Shizuku доступен, но разрешение не выдано — автоматически запрашивает.
    /// </summary>
    [RelayCommand]
    private void InitializeMobile()
    {
        if (!IsMobile) return;

        // Останавливаем игру при запуске редактора
        try { _fileAccessService?.KillGameProcess(); } catch { }

        CheckShizukuAndLoad();
    }

    /// <summary>
    /// Запускает игру.
    /// Последовательность:
    /// 1. Force-stop — убиваем игру, чтобы она не могла перезаписать профиль.
    /// 2. Перезаписываем профиль — гарантируем, что файл содержит наши изменения
    ///    (игра могла перезаписать его своим автосохранением пока работала в фоне).
    /// 3. Запускаем игру — она прочитает актуальный профиль.
    /// </summary>
    [RelayCommand]
    private void LaunchGame()
    {
        try
        {
            // 1. Убиваем игру, чтобы она не смогла перезаписать профиль при завершении
            _fileAccessService?.ForceStopGame();

            // Ждём завершения процесса игры
            System.Threading.Thread.Sleep(500);

            // 2. Перезаписываем профиль актуальными данными из редактора
            if (_profileService.IsLoaded && _fileAccessService != null)
            {
                _profileService.SetSeasonPass(SeasonPassPurchased, SeasonPassPoints);
                var data = _profileService.SaveToBytes();
                _fileAccessService.WriteGameProfile(data);
            }

            // 3. Запускаем игру — она прочитает свежий профиль
            _fileAccessService?.LaunchGame();
            HasChanges = false;
            StatusMessage = "Игра запущена с актуальным профилем";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка запуска: {ex.Message}";
        }
    }

    /// <summary>
    /// Проверяет состояние Shizuku и пытается загрузить профиль.
    /// Если Shizuku доступен и разрешение есть — загружает.
    /// Если нет разрешения — автоматически запрашивает и начинает polling.
    /// </summary>
    [RelayCommand]
    private void CheckShizukuAndLoad()
    {
        if (_fileAccessService == null)
        {
            ShowShizukuScreen = true;
            ShizukuStatusText = "Сервис файлового доступа не инициализирован";
            return;
        }

        IsShizukuAvailable = _fileAccessService.IsShizukuAvailable;
        HasShizukuPermission = _fileAccessService.HasShizukuPermission;

        if (!IsShizukuAvailable)
        {
            ShowShizukuScreen = true;
            ShizukuStatusText = "Shizuku не запущен.\nЗапустите Shizuku и нажмите кнопку ниже.";
            return;
        }

        if (!HasShizukuPermission)
        {
            ShowShizukuScreen = true;
            ShizukuStatusText = "Разрешение Shizuku не выдано.\nЗапрашиваю автоматически...";
            // Автоматически запрашиваем разрешение
            _fileAccessService.RequestShizukuPermission();
            // Запускаем polling как fallback (на случай если listener не сработает)
            StartShizukuPolling();
            return;
        }

        // Shizuku доступен и разрешение есть — загружаем профиль
        StopShizukuPolling();
        ShowShizukuScreen = false;
        LoadProfileFromShizuku();
    }

    /// <summary>
    /// Запрашивает разрешение Shizuku (кнопка).
    /// </summary>
    [RelayCommand]
    private void RequestShizukuPermission()
    {
        _fileAccessService?.RequestShizukuPermission();
        StartShizukuPolling();
    }

    /// <summary>
    /// Запускает периодическую проверку разрешения Shizuku.
    /// Polling каждые 2 секунды — fallback на случай если listener не сработает.
    /// </summary>
    private void StartShizukuPolling()
    {
        StopShizukuPolling();
        _shizukuPollTimer = new Timer(_ =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (_fileAccessService == null) return;
                if (_fileAccessService.HasShizukuPermission)
                {
                    StopShizukuPolling();
                    CheckShizukuAndLoad();
                }
            });
        }, null, 2000, 2000);
    }

    /// <summary>
    /// Останавливает polling Shizuku.
    /// </summary>
    private void StopShizukuPolling()
    {
        _shizukuPollTimer?.Dispose();
        _shizukuPollTimer = null;
    }

    /// <summary>
    /// Загружает профиль через Shizuku из известного пути.
    /// </summary>
    private void LoadProfileFromShizuku()
    {
        if (_fileAccessService == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Загрузка профиля...";

            if (!_fileAccessService.CanAccessGameProfile())
            {
                StatusMessage = "Файл профиля не найден. Убедитесь, что игра установлена.";
                return;
            }

            var data = _fileAccessService.ReadGameProfile();
            _profileService.LoadFromBytes(data);

            LoadedFileName = "profile (Shizuku)";
            IsProfileLoaded = true;
            HasChanges = false;
            StatusMessage = "Профиль загружен";

            RefreshAllTabs();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ========== Файловые команды ==========

    /// <summary>
    /// Открывает диалог выбора файла и загружает профиль (десктоп).
    /// </summary>
    [RelayCommand]
    private async Task OpenFileAsync()
    {
        try
        {
            var topLevel = GetTopLevel();
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open profile",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Profile files") { Patterns = new[] { "profile", "*" } }
                }
            });

            if (files.Count == 0) return;

            var filePath = files[0].Path.LocalPath;
            _profileService.LoadFromFile(filePath);

            LoadedFileName = System.IO.Path.GetFileName(filePath);
            IsProfileLoaded = true;
            HasChanges = false;
            StatusMessage = "Profile loaded";

            RefreshAllTabs();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Сохраняет профиль. На Android -- через Shizuku обратно в игру.
    /// На десктопе -- в текущий файл.
    /// При мобильном сохранении сначала убивает игру, чтобы она не перезаписала профиль.
    /// </summary>
    [RelayCommand]
    private async Task SaveFileAsync()
    {
        if (!_profileService.IsLoaded) return;

        try
        {
            _profileService.SetSeasonPass(SeasonPassPurchased, SeasonPassPoints);

            if (IsMobile && _fileAccessService != null)
            {
                // Сначала убиваем игру, чтобы она не перезаписала профиль
                _fileAccessService.ForceStopGame();
                // Мобильный режим: сохраняем через Shizuku
                var data = _profileService.SaveToBytes();
                _fileAccessService.WriteGameProfile(data);
                StatusMessage = "Профиль сохранён в игру";
            }
            else
            {
                _profileService.SaveToFile();
                StatusMessage = "Profile saved";
            }
            HasChanges = false;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка: {ex.Message}";
        }
    }

    /// <summary>
    /// Сохраняет профиль в новый файл.
    /// На Android -- в Download/CityEdit/.
    /// На десктопе -- через диалог выбора файла.
    /// </summary>
    [RelayCommand]
    private async Task SaveFileAsAsync()
    {
        if (!_profileService.IsLoaded) return;

        try
        {
            _profileService.SetSeasonPass(SeasonPassPurchased, SeasonPassPoints);

            if (IsMobile && _fileAccessService != null)
            {
                // Мобильный режим: сохраняем в Download/CityEdit
                string? defaultPath = _fileAccessService.GetDefaultSavePath();
                if (defaultPath == null)
                {
                    StatusMessage = "Путь для сохранения недоступен";
                    return;
                }

                string fileName = $"profile_{DateTime.Now:yyyyMMdd_HHmmss}";
                string fullPath = System.IO.Path.Combine(defaultPath, fileName);

                var data = _profileService.SaveToBytes();
                _fileAccessService.SaveToPath(fullPath, data);

                LoadedFileName = fileName;
                HasChanges = false;
                StatusMessage = $"Сохранено: {fullPath}";
            }
            else
            {
                var topLevel = GetTopLevel();
                if (topLevel == null) return;

                var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Save profile as",
                    SuggestedFileName = "profile"
                });

                if (file == null) return;

                _profileService.SaveToFile(file.Path.LocalPath);
                LoadedFileName = System.IO.Path.GetFileName(file.Path.LocalPath);
                HasChanges = false;
                StatusMessage = "Profile saved";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка: {ex.Message}";
        }
    }

    // ========== Быстрые действия ==========

    /// <summary>
    /// Разблокирует всех серферов.
    /// </summary>
    [RelayCommand]
    private void UnlockAllSurfers()
    {
        _profileService.SetAllSurfers(true);
        RefreshSurfers();
        MarkChanged("All surfers unlocked");
    }

    /// <summary>
    /// Блокирует всех серферов.
    /// </summary>
    [RelayCommand]
    private void LockAllSurfers()
    {
        _profileService.SetAllSurfers(false);
        RefreshSurfers();
        MarkChanged("All surfers locked");
    }

    /// <summary>
    /// Разблокирует все скины.
    /// </summary>
    [RelayCommand]
    private void UnlockAllSkins()
    {
        _profileService.SetAllSkins(true);
        RefreshSurfers();
        MarkChanged("All skins unlocked");
    }

    /// <summary>
    /// Блокирует все скины.
    /// </summary>
    [RelayCommand]
    private void LockAllSkins()
    {
        _profileService.SetAllSkins(false);
        RefreshSurfers();
        MarkChanged("All skins locked");
    }

    /// <summary>
    /// Разблокирует все доски.
    /// </summary>
    [RelayCommand]
    private void UnlockAllBoards()
    {
        _profileService.SetAllBoards(true);
        RefreshBoards();
        MarkChanged("All boards unlocked");
    }

    /// <summary>
    /// Блокирует все доски.
    /// </summary>
    [RelayCommand]
    private void LockAllBoards()
    {
        _profileService.SetAllBoards(false);
        RefreshBoards();
        MarkChanged("All boards locked");
    }

    /// <summary>
    /// Устанавливает максимальный уровень всем серферам.
    /// </summary>
    [RelayCommand]
    private void MaxLevelAllSurfers()
    {
        _profileService.SetAllSurfers(true, Constants.MaxLevel);
        RefreshSurfers();
        MarkChanged("All surfers set to max level");
    }

    /// <summary>
    /// Устанавливает максимальный уровень всем доскам.
    /// </summary>
    [RelayCommand]
    private void MaxLevelAllBoards()
    {
        _profileService.SetAllBoards(true, Constants.MaxLevel);
        RefreshBoards();
        MarkChanged("All boards set to max level");
    }

    /// <summary>
    /// Устанавливает максимум всех валют.
    /// </summary>
    [RelayCommand]
    private void MaxAllCurrency()
    {
        _profileService.SetMaxCurrency(Constants.MaxCurrencyValue);
        RefreshWallet();
        MarkChanged("All currencies set to max");
    }

    /// <summary>
    /// Активирует сезонный пропуск с максимальными очками.
    /// </summary>
    [RelayCommand]
    private void ActivateSeasonPass()
    {
        _profileService.SetSeasonPass(true, Constants.MaxSeasonPoints);
        SeasonPassPurchased = true;
        SeasonPassPoints = Constants.MaxSeasonPoints;
        MarkChanged("Season pass activated");
    }

    /// <summary>
    /// Добавляет покупку удаления рекламы.
    /// </summary>
    [RelayCommand]
    private void RemoveAds()
    {
        _profileService.AddPurchase(PurchaseDatabase.RemoveAdsId);
        RefreshPurchases();
        MarkChanged("Ads removed");
    }

    /// <summary>
    /// Применяет все разблокировки: серферы, скины, доски, валюта, сезонный пропуск.
    /// </summary>
    [RelayCommand]
    private void UnlockEverything()
    {
        _profileService.SetAllSurfers(true, Constants.MaxLevel);
        _profileService.SetAllSkins(true);
        _profileService.SetAllBoards(true, Constants.MaxLevel);
        _profileService.SetMaxCurrency(Constants.MaxCurrencyValue);
        _profileService.SetSeasonPass(true, Constants.MaxSeasonPoints);
        _profileService.AddPurchase(PurchaseDatabase.RemoveAdsId);
        for (int i = 1; i <= PurchaseDatabase.BonusTrackCount; i++)
            _profileService.AddPurchase(string.Format(PurchaseDatabase.BonusTrackTemplate, i));
        _profileService.SetTutorialStep(5);
        RefreshAllTabs();
        MarkChanged("Everything unlocked!");
    }

    /// <summary>
    /// Пропускает туториал (устанавливает tutorialStep = 5).
    /// </summary>
    [RelayCommand]
    private void SkipTutorial()
    {
        _profileService.SetTutorialStep(5);
        MarkChanged("Tutorial skipped");
    }

    /// <summary>
    /// Устанавливает активного серфера по имени.
    /// </summary>
    [RelayCommand]
    private void SetActiveSurfer(string surferName)
    {
        if (string.IsNullOrEmpty(surferName)) return;
        var entry = SurferDatabase.Names.FirstOrDefault(x => x.Value == surferName);
        if (entry.Value != null)
        {
            _profileService.SetSelectedSurfer(entry.Key);
            SelectedSurferName = surferName;
            MarkChanged($"Active surfer: {surferName}");
        }
    }

    /// <summary>
    /// Устанавливает активную доску по имени.
    /// </summary>
    [RelayCommand]
    private void SetActiveBoard(string boardName)
    {
        if (string.IsNullOrEmpty(boardName)) return;
        var entry = BoardDatabase.Names.FirstOrDefault(x => x.Value == boardName);
        if (entry.Value != null)
        {
            _profileService.SetSelectedBoard(entry.Key);
            SelectedBoardName = boardName;
            MarkChanged($"Active board: {boardName}");
        }
    }

    // ========== Действия покупок ==========

    /// <summary>
    /// Добавляет выбранную покупку.
    /// </summary>
    [RelayCommand]
    private void AddPurchase()
    {
        if (SelectedPurchaseEntry == null) return;
        _profileService.AddPurchase(SelectedPurchaseEntry.ProductId);
        RefreshPurchases();
        MarkChanged($"Purchase added: {SelectedPurchaseEntry.ProductId}");
    }

    /// <summary>
    /// Разблокирует все бонусные трассы (1-16).
    /// </summary>
    [RelayCommand]
    private void UnlockBonusTracks()
    {
        for (int i = 1; i <= PurchaseDatabase.BonusTrackCount; i++)
        {
            var id = string.Format(PurchaseDatabase.BonusTrackTemplate, i);
            _profileService.AddPurchase(id);
        }
        RefreshPurchases();
        MarkChanged($"All {PurchaseDatabase.BonusTrackCount} bonus tracks unlocked");
    }

    /// <summary>
    /// Удаляет выбранную покупку.
    /// </summary>
    [RelayCommand]
    private void RemoveSelectedPurchase()
    {
        if (SelectedPurchase == null) return;
        var removedId = SelectedPurchase.ProductId;
        _profileService.RemovePurchase(SelectedPurchase.ProductId, SelectedPurchase.DateIndex);
        RefreshPurchases();
        MarkChanged($"Purchase removed: {removedId}");
    }

    /// <summary>
    /// Применяет фильтр покупок по категории и поисковому тексту.
    /// </summary>
    private void ApplyPurchaseFilter()
    {
        FilteredPurchaseEntries.Clear();
        var results = PurchaseDatabase.GetByCategory(SelectedPurchaseCategory);
        if (!string.IsNullOrWhiteSpace(PurchaseSearchText))
        {
            var q = PurchaseSearchText.Trim();
            results = results.Where(p =>
                p.ProductId.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(q, StringComparison.OrdinalIgnoreCase));
        }
        foreach (var entry in results)
            FilteredPurchaseEntries.Add(entry);
        if (FilteredPurchaseEntries.Count > 0)
            SelectedPurchaseEntry = FilteredPurchaseEntries[0];
    }

    partial void OnPurchaseSearchTextChanged(string value) => ApplyPurchaseFilter();
    partial void OnSelectedPurchaseCategoryChanged(string value) => ApplyPurchaseFilter();

    partial void OnSelectedSurferNameChanged(string value)
    {
        if (string.IsNullOrEmpty(value) || value == "—" || !_profileService.IsLoaded) return;
        var entry = SurferDatabase.Names.FirstOrDefault(x => x.Value == value);
        if (entry.Value != null)
        {
            _profileService.SetSelectedSurfer(entry.Key);
            MarkChanged($"Active surfer: {value}");
        }
    }

    partial void OnSelectedBoardNameChanged(string value)
    {
        if (string.IsNullOrEmpty(value) || value == "—" || !_profileService.IsLoaded) return;
        var entry = BoardDatabase.Names.FirstOrDefault(x => x.Value == value);
        if (entry.Value != null)
        {
            _profileService.SetSelectedBoard(entry.Key);
            MarkChanged($"Active board: {value}");
        }
    }

    // ========== Обработчики изменения свойств ==========

    partial void OnSeasonPassPurchasedChanged(bool value)
    {
        if (_profileService.IsLoaded)
        {
            _profileService.SetSeasonPass(value, SeasonPassPoints);
            MarkChanged("Season pass updated");
        }
    }

    partial void OnSeasonPassPointsChanged(int value)
    {
        if (_profileService.IsLoaded)
        {
            _profileService.SetSeasonPass(SeasonPassPurchased, value);
            MarkChanged("Season points updated");
        }
    }

    // ========== Обновление данных вкладок ==========

    private void RefreshAllTabs()
    {
        RefreshSurfers();
        RefreshBoards();
        RefreshStats();
        RefreshWallet();
        RefreshPurchases();
        RefreshSeasonPass();
        RefreshFlags();
        RefreshActiveSelection();
    }

    private void RefreshFlags()
    {
        Flags.Clear();
        var flags = _profileService.GetFlags();
        foreach (var (name, value) in flags.OrderBy(x => x.Key))
        {
            Flags.Add(new FlagItemViewModel(name, value, _profileService));
        }
    }

    private void RefreshActiveSelection()
    {
        int surferId = _profileService.GetSelectedSurferId();
        SelectedSurferName = SurferDatabase.GetName(surferId);

        int boardId = _profileService.GetSelectedBoardId();
        SelectedBoardName = BoardDatabase.GetName(boardId);
    }

    private void RefreshSurfers()
    {
        Surfers.Clear();
        var profiles = _profileService.GetSurferProfiles();
        var skinProfiles = _profileService.GetSkinProfiles();
        var skinUnlockMap = skinProfiles.ToDictionary(
            s => s.ContainsKey("id") ? s["id"].GetInt32() : 0,
            s => s.ContainsKey("isUnlocked") && s["isUnlocked"].GetBoolean()
        );

        for (int i = 0; i < profiles.Count; i++)
        {
            var p = profiles[i];
            int id = p.ContainsKey("id") ? p["id"].GetInt32() : 0;
            string name = SurferDatabase.GetName(id);
            bool unlocked = p.ContainsKey("isUnlocked") && p["isUnlocked"].GetBoolean();
            int level = p.ContainsKey("level") ? p["level"].GetInt32() : 1;
            int highScore = p.ContainsKey("highScore") ? p["highScore"].GetInt32() : 0;

            var vm = new SurferItemViewModel(i, id, name, unlocked, level, highScore, _profileService);

            var surferSkins = SkinDatabase.GetSkinsForSurfer(id);
            foreach (var (skinId, skinInfo) in surferSkins)
            {
                bool skinUnlocked = skinUnlockMap.TryGetValue(skinId, out var su) && su;
                vm.Skins.Add(new SkinItemViewModel(skinId, skinInfo.DisplayName, skinUnlocked, _profileService));
            }

            // Помечаем последний скин если их нечётное количество — чтобы UI растянул его на 100%
            if (vm.Skins.Count % 2 == 1 && vm.Skins.Count > 0)
            {
                vm.Skins[^1].IsLastInOddRow = true;
            }

            Surfers.Add(vm);
        }
    }

    private void RefreshBoards()
    {
        Boards.Clear();
        var profiles = _profileService.GetBoardProfiles();

        for (int i = 0; i < profiles.Count; i++)
        {
            var p = profiles[i];
            int id = p.ContainsKey("id") ? p["id"].GetInt32() : 0;
            string name = BoardDatabase.GetName(id);
            string? owner = BoardDatabase.GetOwnerName(id);
            bool unlocked = p.ContainsKey("isUnlocked") && p["isUnlocked"].GetBoolean();
            int level = p.ContainsKey("level") ? p["level"].GetInt32() : 1;

            Boards.Add(new BoardItemViewModel(i, id, name, owner, unlocked, level, _profileService));
        }
    }

    private void RefreshStats()
    {
        Stats.Clear();
        var stats = _profileService.GetStats();
        var labels = new (string Key, string Label)[]
        {
            ("runs", "Total Runs"),
            ("campaignRuns", "Campaign Runs"),
            ("trialRuns", "Trial Runs"),
            ("stompedTimes", "Stomped Times"),
            ("tarpBouncedTimes", "Tarp Bounced"),
            ("bubbleBouncedTimes", "Bubble Bounced"),
            ("boardActivatedTimes", "Board Activations"),
            ("level", "Player Level"),
            ("xp", "XP")
        };

        foreach (var (key, label) in labels)
        {
            int value = stats.TryGetValue(key, out var v) ? v : 0;
            Stats.Add(new StatItemViewModel(key, label, value, _profileService));
        }
    }

    private void RefreshWallet()
    {
        WalletItems.Clear();
        var wallet = _profileService.GetWallet();

        foreach (var tag in WalletTags.PrimaryTags)
        {
            int count = wallet.TryGetValue(tag, out var c) ? c : 0;
            string name = WalletTags.GetName(tag);
            WalletItems.Add(new WalletItemViewModel(tag, name, count, _profileService));
        }

        foreach (var (tag, count) in wallet)
        {
            if (!WalletTags.PrimaryTags.Contains(tag))
            {
                string name = WalletTags.GetName(tag);
                WalletItems.Add(new WalletItemViewModel(tag, name, count, _profileService));
            }
        }
    }

    private void RefreshPurchases()
    {
        Purchases.Clear();
        var history = _profileService.GetPurchaseHistory();

        var allItems = history
            .SelectMany(kv => kv.Value.Dates
                .Select((date, idx) => new PurchaseItemViewModel(kv.Key, date, idx)))
            .OrderByDescending(p => p.PurchaseDate)
            .ToList();

        foreach (var item in allItems)
        {
            Purchases.Add(item);
        }
    }

    private void RefreshSeasonPass()
    {
        var (purchased, points) = _profileService.GetSeasonPass();
        SeasonPassPurchased = purchased;
        SeasonPassPoints = points;
    }

    /// <summary>
    /// Помечает профиль как изменённый и обновляет статус.
    /// </summary>
    private void MarkChanged(string message)
    {
        HasChanges = true;
        StatusMessage = message;
    }

    /// <summary>
    /// Получает TopLevel окно/view для работы с диалогами.
    /// </summary>
    private static TopLevel? GetTopLevel()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        return null;
    }
}
