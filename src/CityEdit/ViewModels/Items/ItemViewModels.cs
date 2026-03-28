using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using CityEdit.Models.GameData;
using CityEdit.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CityEdit.ViewModels.Items;

/// <summary>
/// ViewModel скина серфера.
/// Отображает информацию о скине и позволяет переключать разблокировку.
/// </summary>
public partial class SkinItemViewModel : ObservableObject
{
    /// <summary>
    /// Ссылка на сервис профиля для сохранения изменений.
    /// </summary>
    private readonly ProfileService _profileService;

    /// <summary>
    /// DataTag скина.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Отображаемое имя скина.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Признак, что этот скин — последний в нечётной группе (должен занимать 100% ширины).
    /// </summary>
    public bool IsLastInOddRow { get; set; }

    /// <summary>
    /// Признак разблокировки скина.
    /// </summary>
    [ObservableProperty]
    private bool _isUnlocked;

    /// <summary>
    /// Создаёт ViewModel скина.
    /// </summary>
    /// <param name="id">DataTag скина.</param>
    /// <param name="displayName">Имя скина.</param>
    /// <param name="isUnlocked">Текущее состояние разблокировки.</param>
    /// <param name="profileService">Сервис профиля.</param>
    public SkinItemViewModel(int id, string displayName, bool isUnlocked, ProfileService profileService)
    {
        Id = id;
        DisplayName = displayName;
        _isUnlocked = isUnlocked;
        _profileService = profileService;
    }

    /// <summary>
    /// Обработчик изменения состояния разблокировки.
    /// </summary>
    partial void OnIsUnlockedChanged(bool value)
    {
        _profileService.UpdateSkin(Id, value);
    }
}

/// <summary>
/// ViewModel серфера.
/// Содержит информацию о серфере, его уровне, рекорде и скинах.
/// </summary>
public partial class SurferItemViewModel : ObservableObject
{
    /// <summary>
    /// Ссылка на сервис профиля.
    /// </summary>
    private readonly ProfileService _profileService;

    /// <summary>
    /// Индекс серфера в массиве профиля.
    /// </summary>
    private readonly int _index;

    /// <summary>
    /// DataTag серфера.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Отображаемое имя серфера.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Признак разблокировки серфера.
    /// </summary>
    [ObservableProperty]
    private bool _isUnlocked;

    /// <summary>
    /// Уровень серфера (1-20).
    /// </summary>
    [ObservableProperty]
    private int _level;

    /// <summary>
    /// Рекорд серфера.
    /// </summary>
    [ObservableProperty]
    private int _highScore;

    /// <summary>
    /// Коллекция скинов серфера.
    /// </summary>
    public ObservableCollection<SkinItemViewModel> Skins { get; } = new();

    /// <summary>
    /// Создаёт ViewModel серфера.
    /// </summary>
    public SurferItemViewModel(int index, int id, string displayName, bool isUnlocked,
        int level, int highScore, ProfileService profileService)
    {
        _index = index;
        Id = id;
        DisplayName = displayName;
        _isUnlocked = isUnlocked;
        _level = level;
        _highScore = highScore;
        _profileService = profileService;
    }

    /// <summary>
    /// Обработчик изменения разблокировки.
    /// </summary>
    partial void OnIsUnlockedChanged(bool value)
    {
        _profileService.UpdateSurferProperty(_index, "isUnlocked", JsonSerializer.SerializeToElement(value));
        _profileService.UpdateSurferProperty(_index, "wasSeen", JsonSerializer.SerializeToElement(value));
    }

    /// <summary>
    /// Обработчик изменения уровня.
    /// </summary>
    partial void OnLevelChanged(int value)
    {
        int clamped = System.Math.Clamp(value, 1, 20);
        _profileService.UpdateSurferProperty(_index, "level", JsonSerializer.SerializeToElement(clamped));
    }

    /// <summary>
    /// Обработчик изменения рекорда.
    /// </summary>
    partial void OnHighScoreChanged(int value)
    {
        _profileService.UpdateSurferProperty(_index, "highScore", JsonSerializer.SerializeToElement(value));
    }
}

/// <summary>
/// ViewModel доски.
/// Содержит информацию о доске, её уровне и владельце.
/// </summary>
public partial class BoardItemViewModel : ObservableObject
{
    /// <summary>
    /// Ссылка на сервис профиля.
    /// </summary>
    private readonly ProfileService _profileService;

    /// <summary>
    /// Индекс доски в массиве профиля.
    /// </summary>
    private readonly int _index;

    /// <summary>
    /// DataTag доски.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Отображаемое имя доски.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Имя владельца доски (серфера).
    /// </summary>
    public string? OwnerName { get; }

    /// <summary>
    /// Полное отображаемое имя (имя доски + владелец).
    /// </summary>
    public string FullDisplayName =>
        OwnerName != null ? $"{DisplayName} ({OwnerName})" : DisplayName;

    /// <summary>
    /// Признак разблокировки доски.
    /// </summary>
    [ObservableProperty]
    private bool _isUnlocked;

    /// <summary>
    /// Уровень доски (1-20).
    /// </summary>
    [ObservableProperty]
    private int _level;

    /// <summary>
    /// Создаёт ViewModel доски.
    /// </summary>
    public BoardItemViewModel(int index, int id, string displayName, string? ownerName,
        bool isUnlocked, int level, ProfileService profileService)
    {
        _index = index;
        Id = id;
        DisplayName = displayName;
        OwnerName = ownerName;
        _isUnlocked = isUnlocked;
        _level = level;
        _profileService = profileService;
    }

    /// <summary>
    /// Обработчик изменения разблокировки.
    /// </summary>
    partial void OnIsUnlockedChanged(bool value)
    {
        _profileService.UpdateBoardProperty(_index, "isUnlocked", JsonSerializer.SerializeToElement(value));
        _profileService.UpdateBoardProperty(_index, "wasSeen", JsonSerializer.SerializeToElement(value));
    }

    /// <summary>
    /// Обработчик изменения уровня.
    /// </summary>
    partial void OnLevelChanged(int value)
    {
        int clamped = System.Math.Clamp(value, 1, 20);
        _profileService.UpdateBoardProperty(_index, "level", JsonSerializer.SerializeToElement(clamped));
    }
}

/// <summary>
/// ViewModel элемента кошелька.
/// Отображает тип валюты и позволяет редактировать количество.
/// </summary>
public partial class WalletItemViewModel : ObservableObject
{
    /// <summary>
    /// Ссылка на сервис профиля.
    /// </summary>
    private readonly ProfileService _profileService;

    /// <summary>
    /// DataTag валюты.
    /// </summary>
    public int DataTag { get; }

    /// <summary>
    /// Отображаемое имя валюты.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Количество валюты.
    /// </summary>
    [ObservableProperty]
    private int _count;

    /// <summary>
    /// Создаёт ViewModel элемента кошелька.
    /// </summary>
    public WalletItemViewModel(int dataTag, string displayName, int count, ProfileService profileService)
    {
        DataTag = dataTag;
        DisplayName = displayName;
        _count = count;
        _profileService = profileService;
    }

    /// <summary>
    /// Обработчик изменения количества.
    /// </summary>
    partial void OnCountChanged(int value)
    {
        _profileService.SetWalletItem(DataTag, value);
    }
}

/// <summary>
/// ViewModel элемента статистики.
/// Отображает ключ статистики и позволяет редактировать значение.
/// </summary>
public partial class StatItemViewModel : ObservableObject
{
    /// <summary>
    /// Ссылка на сервис профиля.
    /// </summary>
    private readonly ProfileService _profileService;

    /// <summary>
    /// Ключ статистики в JSON профиля.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Отображаемое имя статистики.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Значение статистики.
    /// </summary>
    [ObservableProperty]
    private int _value;

    /// <summary>
    /// Создаёт ViewModel элемента статистики.
    /// </summary>
    public StatItemViewModel(string key, string displayName, int value, ProfileService profileService)
    {
        Key = key;
        DisplayName = displayName;
        _value = value;
        _profileService = profileService;
    }

    /// <summary>
    /// Обработчик изменения значения.
    /// </summary>
    partial void OnValueChanged(int value)
    {
        _profileService.SetStat(Key, value);
    }
}

/// <summary>
/// ViewModel записи о покупке.
/// </summary>
public class PurchaseItemViewModel
{
    /// <summary>
    /// Идентификатор продукта.
    /// </summary>
    public string ProductId { get; }

    /// <summary>
    /// Дата покупки.
    /// </summary>
    public string PurchaseDate { get; }

    /// <summary>
    /// Индекс даты в массиве PurchaseDates (для удаления).
    /// </summary>
    public int DateIndex { get; }

    /// <summary>
    /// Описание покупки из базы данных.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Категория покупки из базы данных.
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// Отображаемая строка покупки.
    /// </summary>
    public string Display => $"{ProductId} - {PurchaseDate}";

    /// <summary>
    /// Создаёт ViewModel записи о покупке.
    /// </summary>
    public PurchaseItemViewModel(string productId, string purchaseDate, int dateIndex)
    {
        ProductId = productId;
        PurchaseDate = purchaseDate;
        DateIndex = dateIndex;
        if (PurchaseDatabase.ByProductId.TryGetValue(productId, out var entry))
        {
            Description = entry.Description;
            Category = entry.Category;
        }
        else
        {
            Description = productId;
            Category = "Unknown";
        }
    }
}

/// <summary>
/// ViewModel записи флага профиля.
/// </summary>
public partial class FlagItemViewModel : ObservableObject
{
    private readonly ProfileService _profileService;

    /// <summary>
    /// Имя флага.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Значение флага.
    /// </summary>
    [ObservableProperty]
    private bool _value;

    public FlagItemViewModel(string name, bool value, ProfileService profileService)
    {
        Name = name;
        _value = value;
        _profileService = profileService;
    }

    partial void OnValueChanged(bool value)
    {
        _profileService.SetFlag(Name, value);
    }
}
