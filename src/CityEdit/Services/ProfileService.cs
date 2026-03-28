using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CityEdit.Core;
using CityEdit.Crypto;
using CityEdit.Models.GameData;

namespace CityEdit.Services;

/// <summary>
/// Сервис работы с профилем Subway Surfers City.
/// Отвечает за загрузку, модификацию и сохранение профиля.
/// Хранит текущее состояние загруженного профиля: IV, KEY, JSON-данные.
/// </summary>
public class ProfileService
{
    /// <summary>
    /// Вектор инициализации из текущего файла.
    /// </summary>
    private byte[]? _iv;

    /// <summary>
    /// Ключ шифрования из текущего файла.
    /// </summary>
    private byte[]? _key;

    /// <summary>
    /// Корневые JSON-данные (version, lastUpdated, profile, hash).
    /// </summary>
    private JsonElement _rootData;

    /// <summary>
    /// Внутренний профиль в виде изменяемого словаря.
    /// </summary>
    private Dictionary<string, JsonElement>? _profileDict;

    /// <summary>
    /// Путь к текущему файлу.
    /// </summary>
    public string? CurrentFilePath { get; private set; }

    /// <summary>
    /// Признак того, что профиль загружен.
    /// </summary>
    public bool IsLoaded => _profileDict != null;

    /// <summary>
    /// Признак наличия несохранённых изменений.
    /// </summary>
    public bool HasChanges { get; set; }

    /// <summary>
    /// Загружает профиль из файла на диске.
    /// </summary>
    /// <param name="filePath">Путь к файлу профиля.</param>
    /// <exception cref="FileNotFoundException">Файл не найден.</exception>
    /// <exception cref="InvalidOperationException">Ошибка формата файла.</exception>
    public void LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Файл профиля не найден.", filePath);
        }

        var decrypted = ProfileCrypto.DecryptFromFile(filePath);
        _iv = decrypted.Iv;
        _key = decrypted.Key;
        _rootData = decrypted.JsonData.RootElement.Clone();
        CurrentFilePath = filePath;

        // Извлекаем вложенный профиль из строки
        if (_rootData.TryGetProperty("profile", out var profileElement))
        {
            string profileJson = profileElement.GetString()
                ?? throw new InvalidOperationException("Поле 'profile' пустое.");
            var profileDoc = JsonDocument.Parse(profileJson);
            _profileDict = new Dictionary<string, JsonElement>();
            foreach (var prop in profileDoc.RootElement.EnumerateObject())
            {
                _profileDict[prop.Name] = prop.Value.Clone();
            }
        }
        else
        {
            throw new InvalidOperationException("Поле 'profile' не найдено в JSON.");
        }

        // Обогащаем профиль недостающими серферами и досками из справочника
        EnsureAllSurfers();
        EnsureAllBoards();

        HasChanges = false;
        decrypted.JsonData.Dispose();
    }

    /// <summary>
    /// Сохраняет текущий профиль в файл.
    /// </summary>
    /// <param name="filePath">Путь для сохранения. Если null, используется текущий путь.</param>
    /// <exception cref="InvalidOperationException">Профиль не загружен.</exception>
    public void SaveToFile(string? filePath = null)
    {
        if (!IsLoaded || _iv == null || _key == null || _profileDict == null)
        {
            throw new InvalidOperationException("Профиль не загружен.");
        }

        string targetPath = filePath ?? CurrentFilePath
            ?? throw new InvalidOperationException("Путь для сохранения не указан.");

        // Сериализуем внутренний профиль обратно в JSON-строку
        string profileJson = SerializeProfile();

        // Собираем корневой объект
        var rootDict = new Dictionary<string, object>();
        foreach (var prop in _rootData.EnumerateObject())
        {
            if (prop.Name == "profile")
            {
                rootDict["profile"] = profileJson;
            }
            else if (prop.Name == "lastUpdated")
            {
                // Формат ISO 8601 с 7 знаками дробной части, как в оригинале
                rootDict["lastUpdated"] = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
            }
            else
            {
                rootDict[prop.Name] = prop.Value;
            }
        }

        string rootJson = JsonSerializer.Serialize(rootDict, new JsonSerializerOptions
        {
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        var rootElement = JsonDocument.Parse(rootJson).RootElement;
        ProfileCrypto.EncryptToFile(_iv, _key, rootElement, targetPath);

        CurrentFilePath = targetPath;
        HasChanges = false;
    }

    /// <summary>
    /// Загружает профиль из массива байтов (для Android/Shizuku).
    /// </summary>
    /// <param name="data">Зашифрованные данные профиля.</param>
    /// <exception cref="InvalidOperationException">Ошибка формата данных.</exception>
    public void LoadFromBytes(byte[] data)
    {
        var decrypted = ProfileCrypto.Decrypt(data);
        _iv = decrypted.Iv;
        _key = decrypted.Key;
        _rootData = decrypted.JsonData.RootElement.Clone();
        CurrentFilePath = null;

        if (_rootData.TryGetProperty("profile", out var profileElement))
        {
            string profileJson = profileElement.GetString()
                ?? throw new InvalidOperationException("Поле 'profile' пустое.");
            var profileDoc = JsonDocument.Parse(profileJson);
            _profileDict = new Dictionary<string, JsonElement>();
            foreach (var prop in profileDoc.RootElement.EnumerateObject())
            {
                _profileDict[prop.Name] = prop.Value.Clone();
            }
        }
        else
        {
            throw new InvalidOperationException("Поле 'profile' не найдено в JSON.");
        }

        EnsureAllSurfers();
        EnsureAllBoards();

        HasChanges = false;
        decrypted.JsonData.Dispose();
    }

    /// <summary>
    /// Сериализует и шифрует профиль в массив байтов (для Android/Shizuku).
    /// </summary>
    /// <returns>Зашифрованные данные профиля.</returns>
    /// <exception cref="InvalidOperationException">Профиль не загружен.</exception>
    public byte[] SaveToBytes()
    {
        if (!IsLoaded || _iv == null || _key == null || _profileDict == null)
        {
            throw new InvalidOperationException("Профиль не загружен.");
        }

        string profileJson = SerializeProfile();

        var rootDict = new Dictionary<string, object>();
        foreach (var prop in _rootData.EnumerateObject())
        {
            if (prop.Name == "profile")
            {
                rootDict["profile"] = profileJson;
            }
            else if (prop.Name == "lastUpdated")
            {
                // Формат ISO 8601 с 7 знаками дробной части, как в оригинале
                rootDict["lastUpdated"] = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
            }
            else
            {
                rootDict[prop.Name] = prop.Value;
            }
        }

        string rootJson = JsonSerializer.Serialize(rootDict, new JsonSerializerOptions
        {
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        var rootElement = JsonDocument.Parse(rootJson).RootElement;
        return ProfileCrypto.Encrypt(_iv, _key, rootElement);
    }

    /// <summary>
    /// Возвращает список профилей серферов.
    /// </summary>
    public List<Dictionary<string, JsonElement>> GetSurferProfiles()
    {
        return GetArrayOfDicts("surferProfiles");
    }

    /// <summary>
    /// Возвращает список профилей скинов.
    /// </summary>
    public List<Dictionary<string, JsonElement>> GetSkinProfiles()
    {
        return GetArrayOfDicts("surferSkinProfiles");
    }

    /// <summary>
    /// Возвращает список профилей досок.
    /// </summary>
    public List<Dictionary<string, JsonElement>> GetBoardProfiles()
    {
        return GetArrayOfDicts("boardProfiles");
    }

    /// <summary>
    /// Возвращает словарь кошелька: DataTag -> количество.
    /// </summary>
    public Dictionary<int, int> GetWallet()
    {
        var result = new Dictionary<int, int>();
        if (_profileDict == null) return result;

        if (_profileDict.TryGetValue("wallet", out var walletElement))
        {
            foreach (var item in walletElement.EnumerateArray())
            {
                int tag = item.GetProperty("dataTag").GetInt32();
                int count = item.GetProperty("count").GetInt32();
                result[tag] = count;
            }
        }
        return result;
    }

    /// <summary>
    /// Устанавливает значение валюты в кошельке.
    /// </summary>
    /// <param name="dataTag">DataTag валюты.</param>
    /// <param name="count">Новое количество.</param>
    public void SetWalletItem(int dataTag, int count)
    {
        if (_profileDict == null) return;

        var wallet = GetWallet();
        wallet[dataTag] = count;

        // Пересобираем массив wallet
        var walletArray = wallet.Select(kv =>
            JsonSerializer.SerializeToElement(new { dataTag = kv.Key, count = kv.Value })
        ).ToList();

        _profileDict["wallet"] = JsonSerializer.SerializeToElement(walletArray);
        HasChanges = true;
    }

    /// <summary>
    /// Возвращает статистику профиля.
    /// </summary>
    public Dictionary<string, int> GetStats()
    {
        var stats = new Dictionary<string, int>();
        if (_profileDict == null) return stats;

        string[] statKeys = {
            "runs", "campaignRuns", "trialRuns", "stompedTimes",
            "tarpBouncedTimes", "bubbleBouncedTimes", "boardActivatedTimes",
            "level", "xp"
        };

        foreach (var key in statKeys)
        {
            if (_profileDict.TryGetValue(key, out var value))
            {
                stats[key] = value.GetInt32();
            }
            else
            {
                stats[key] = 0;
            }
        }
        return stats;
    }

    /// <summary>
    /// Устанавливает значение статистики.
    /// </summary>
    /// <param name="key">Ключ статистики.</param>
    /// <param name="value">Новое значение.</param>
    public void SetStat(string key, int value)
    {
        if (_profileDict == null) return;
        _profileDict[key] = JsonSerializer.SerializeToElement(value);
        HasChanges = true;
    }

    /// <summary>
    /// Возвращает историю покупок: ProductID -> (PurchaseCount, PurchaseDates).
    /// </summary>
    public Dictionary<string, (int Count, List<string> Dates)> GetPurchaseHistory()
    {
        var result = new Dictionary<string, (int Count, List<string> Dates)>();
        if (_profileDict == null) return result;

        if (_profileDict.TryGetValue("purchaseHistory", out var histElement))
        {
            foreach (var prop in histElement.EnumerateObject())
            {
                int count = prop.Value.GetProperty("PurchaseCount").GetInt32();
                var dates = new List<string>();
                foreach (var date in prop.Value.GetProperty("PurchaseDates").EnumerateArray())
                {
                    dates.Add(date.GetString() ?? "");
                }
                result[prop.Name] = (count, dates);
            }
        }
        return result;
    }

    /// <summary>
    /// Добавляет покупку в историю.
    /// </summary>
    /// <param name="productId">Идентификатор продукта.</param>
    /// <param name="purchaseDate">Дата покупки. Если null, используется текущее UTC время.</param>
    public void AddPurchase(string productId, string? purchaseDate = null)
    {
        if (_profileDict == null) return;

        purchaseDate ??= DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffff") + "Z";

        var history = GetPurchaseHistory();
        if (history.TryGetValue(productId, out var existing))
        {
            existing.Dates.Add(purchaseDate);
            history[productId] = (existing.Count + 1, existing.Dates);
        }
        else
        {
            history[productId] = (1, new List<string> { purchaseDate });
        }

        // Сериализуем обратно
        var histDict = history.ToDictionary(
            kv => kv.Key,
            kv => new { PurchaseCount = kv.Value.Count, PurchaseDates = kv.Value.Dates }
        );
        _profileDict["purchaseHistory"] = JsonSerializer.SerializeToElement(histDict);
        HasChanges = true;
    }

    /// <summary>
    /// Удаляет покупку из истории.
    /// </summary>
    /// <param name="productId">Идентификатор продукта.</param>
    /// <param name="dateIndex">Индекс даты для удаления. Если null, удаляется вся запись.</param>
    public void RemovePurchase(string productId, int? dateIndex = null)
    {
        if (_profileDict == null) return;

        var history = GetPurchaseHistory();
        if (!history.ContainsKey(productId)) return;

        if (dateIndex == null)
        {
            history.Remove(productId);
        }
        else
        {
            var (count, dates) = history[productId];
            if (dateIndex >= 0 && dateIndex < dates.Count)
            {
                dates.RemoveAt(dateIndex.Value);
                if (dates.Count == 0)
                {
                    history.Remove(productId);
                }
                else
                {
                    history[productId] = (count - 1, dates);
                }
            }
        }

        var histDict = history.ToDictionary(
            kv => kv.Key,
            kv => new { PurchaseCount = kv.Value.Count, PurchaseDates = kv.Value.Dates }
        );
        _profileDict["purchaseHistory"] = JsonSerializer.SerializeToElement(histDict);
        HasChanges = true;
    }

    /// <summary>
    /// Возвращает данные сезонного пропуска.
    /// </summary>
    public (bool Purchased, int Points) GetSeasonPass()
    {
        if (_profileDict == null) return (false, 0);

        bool purchased = _profileDict.TryGetValue("seasonPassPurchased", out var p) && p.GetBoolean();
        int points = _profileDict.TryGetValue("seasonPassPoints", out var pts) ? pts.GetInt32() : 0;
        return (purchased, points);
    }

    /// <summary>
    /// Устанавливает состояние сезонного пропуска.
    /// </summary>
    /// <param name="purchased">Куплен ли пропуск.</param>
    /// <param name="points">Количество очков.</param>
    public void SetSeasonPass(bool purchased, int points)
    {
        if (_profileDict == null) return;
        _profileDict["seasonPassPurchased"] = JsonSerializer.SerializeToElement(purchased);
        _profileDict["seasonPassPoints"] = JsonSerializer.SerializeToElement(points);
        HasChanges = true;
    }

    /// <summary>
    /// Обновляет свойство серфера по индексу.
    /// </summary>
    /// <param name="index">Индекс серфера в массиве.</param>
    /// <param name="propertyName">Имя свойства JSON.</param>
    /// <param name="value">Новое значение.</param>
    public void UpdateSurferProperty(int index, string propertyName, JsonElement value)
    {
        UpdateArrayItem("surferProfiles", index, propertyName, value);
    }

    /// <summary>
    /// Обновляет свойство доски по индексу.
    /// </summary>
    /// <param name="index">Индекс доски в массиве.</param>
    /// <param name="propertyName">Имя свойства JSON.</param>
    /// <param name="value">Новое значение.</param>
    public void UpdateBoardProperty(int index, string propertyName, JsonElement value)
    {
        UpdateArrayItem("boardProfiles", index, propertyName, value);
    }

    /// <summary>
    /// Обновляет скин по DataTag.
    /// </summary>
    /// <param name="skinId">DataTag скина.</param>
    /// <param name="isUnlocked">Разблокирован ли скин.</param>
    public void UpdateSkin(int skinId, bool isUnlocked)
    {
        if (_profileDict == null) return;

        var skins = GetSkinProfiles();
        bool found = false;
        for (int i = 0; i < skins.Count; i++)
        {
            if (skins[i].TryGetValue("id", out var idEl) && idEl.GetInt32() == skinId)
            {
                skins[i]["isUnlocked"] = JsonSerializer.SerializeToElement(isUnlocked);
                skins[i]["wasSeen"] = JsonSerializer.SerializeToElement(isUnlocked);
                found = true;
                break;
            }
        }

        if (!found)
        {
            skins.Add(new Dictionary<string, JsonElement>
            {
                ["id"] = JsonSerializer.SerializeToElement(skinId),
                ["isUnlocked"] = JsonSerializer.SerializeToElement(isUnlocked),
                ["wasSeen"] = JsonSerializer.SerializeToElement(isUnlocked)
            });
        }

        // Сериализуем обратно
        _profileDict["surferSkinProfiles"] = SerializeListOfDicts(skins);
        HasChanges = true;
    }

    /// <summary>
    /// Массовое обновление всех серферов.
    /// </summary>
    /// <param name="isUnlocked">Новое состояние разблокировки.</param>
    /// <param name="level">Новый уровень (null = не менять).</param>
    public void SetAllSurfers(bool isUnlocked, int? level = null)
    {
        if (_profileDict == null) return;

        var surfers = GetSurferProfiles();
        for (int i = 0; i < surfers.Count; i++)
        {
            surfers[i]["isUnlocked"] = JsonSerializer.SerializeToElement(isUnlocked);
            surfers[i]["wasSeen"] = JsonSerializer.SerializeToElement(isUnlocked);
            if (level.HasValue)
            {
                surfers[i]["level"] = JsonSerializer.SerializeToElement(
                    Math.Clamp(level.Value, Constants.MinLevel, Constants.MaxLevel));
            }
        }
        _profileDict["surferProfiles"] = SerializeListOfDicts(surfers);
        HasChanges = true;
    }

    /// <summary>
    /// Массовое обновление всех скинов.
    /// </summary>
    /// <param name="isUnlocked">Новое состояние разблокировки.</param>
    public void SetAllSkins(bool isUnlocked)
    {
        if (_profileDict == null) return;

        foreach (var skinId in SkinDatabase.Skins.Keys)
        {
            UpdateSkin(skinId, isUnlocked);
        }
    }

    /// <summary>
    /// Массовое обновление всех досок.
    /// </summary>
    /// <param name="isUnlocked">Новое состояние разблокировки.</param>
    /// <param name="level">Новый уровень (null = не менять).</param>
    public void SetAllBoards(bool isUnlocked, int? level = null)
    {
        if (_profileDict == null) return;

        var boards = GetBoardProfiles();
        for (int i = 0; i < boards.Count; i++)
        {
            boards[i]["isUnlocked"] = JsonSerializer.SerializeToElement(isUnlocked);
            boards[i]["wasSeen"] = JsonSerializer.SerializeToElement(isUnlocked);
            if (level.HasValue)
            {
                boards[i]["level"] = JsonSerializer.SerializeToElement(
                    Math.Clamp(level.Value, Constants.MinLevel, Constants.MaxLevel));
            }
        }
        _profileDict["boardProfiles"] = SerializeListOfDicts(boards);
        HasChanges = true;
    }

    /// <summary>
    /// Устанавливает максимальные значения всех валют.
    /// </summary>
    /// <param name="amount">Количество для установки.</param>
    public void SetMaxCurrency(int amount = 999999)
    {
        foreach (var tag in WalletTags.PrimaryTags)
        {
            SetWalletItem(tag, amount);
        }
    }

    // ---- Вспомогательные методы ----

    /// <summary>
    /// Добавляет недостающих серферов из справочника в профиль.
    /// </summary>
    private void EnsureAllSurfers()
    {
        if (_profileDict == null) return;

        var surfers = GetSurferProfiles();
        var existingIds = surfers
            .Where(s => s.ContainsKey("id"))
            .Select(s => s["id"].GetInt32())
            .ToHashSet();

        bool added = false;
        foreach (var (surferId, _) in SurferDatabase.Names)
        {
            if (!existingIds.Contains(surferId))
            {
                surfers.Add(new Dictionary<string, JsonElement>
                {
                    ["id"] = JsonSerializer.SerializeToElement(surferId),
                    ["level"] = JsonSerializer.SerializeToElement(1),
                    ["isUnlocked"] = JsonSerializer.SerializeToElement(false),
                    ["wasSeen"] = JsonSerializer.SerializeToElement(false),
                    ["highScore"] = JsonSerializer.SerializeToElement(0),
                    ["selectedSkin"] = JsonSerializer.SerializeToElement(0)
                });
                added = true;
            }
        }

        if (added)
        {
            _profileDict["surferProfiles"] = SerializeListOfDicts(surfers);
        }
    }

    /// <summary>
    /// Добавляет недостающие доски из справочника в профиль.
    /// </summary>
    private void EnsureAllBoards()
    {
        if (_profileDict == null) return;

        var boards = GetBoardProfiles();
        var existingIds = boards
            .Where(b => b.ContainsKey("id"))
            .Select(b => b["id"].GetInt32())
            .ToHashSet();

        bool added = false;
        foreach (var (boardId, _) in BoardDatabase.Names)
        {
            if (!existingIds.Contains(boardId))
            {
                boards.Add(new Dictionary<string, JsonElement>
                {
                    ["id"] = JsonSerializer.SerializeToElement(boardId),
                    ["level"] = JsonSerializer.SerializeToElement(1),
                    ["isUnlocked"] = JsonSerializer.SerializeToElement(false),
                    ["wasSeen"] = JsonSerializer.SerializeToElement(false)
                });
                added = true;
            }
        }

        if (added)
        {
            // Сортируем доски по порядку из справочника
            var allBoardIds = BoardDatabase.Names.Keys.ToList();
            boards.Sort((a, b) =>
            {
                int idA = a.ContainsKey("id") ? a["id"].GetInt32() : 0;
                int idB = b.ContainsKey("id") ? b["id"].GetInt32() : 0;
                int indexA = allBoardIds.IndexOf(idA);
                int indexB = allBoardIds.IndexOf(idB);
                if (indexA < 0) indexA = int.MaxValue;
                if (indexB < 0) indexB = int.MaxValue;
                return indexA.CompareTo(indexB);
            });

            _profileDict["boardProfiles"] = SerializeListOfDicts(boards);
        }
    }

    /// <summary>
    /// Извлекает массив JSON-объектов как список словарей.
    /// </summary>
    private List<Dictionary<string, JsonElement>> GetArrayOfDicts(string key)
    {
        var result = new List<Dictionary<string, JsonElement>>();
        if (_profileDict == null) return result;

        if (_profileDict.TryGetValue(key, out var element))
        {
            foreach (var item in element.EnumerateArray())
            {
                var dict = new Dictionary<string, JsonElement>();
                foreach (var prop in item.EnumerateObject())
                {
                    dict[prop.Name] = prop.Value.Clone();
                }
                result.Add(dict);
            }
        }
        return result;
    }

    /// <summary>
    /// Обновляет свойство элемента массива по индексу.
    /// </summary>
    private void UpdateArrayItem(string arrayKey, int index, string propName, JsonElement value)
    {
        if (_profileDict == null) return;

        var items = GetArrayOfDicts(arrayKey);
        if (index >= 0 && index < items.Count)
        {
            items[index][propName] = value;
            _profileDict[arrayKey] = SerializeListOfDicts(items);
            HasChanges = true;
        }
    }

    /// <summary>
    /// Сериализует список словарей в JsonElement.
    /// </summary>
    private static JsonElement SerializeListOfDicts(List<Dictionary<string, JsonElement>> list)
    {
        return JsonSerializer.SerializeToElement(list);
    }

    /// <summary>
    /// Сериализует внутренний профиль в JSON-строку.
    /// </summary>
    private string SerializeProfile()
    {
        if (_profileDict == null) return "{}";

        return JsonSerializer.Serialize(_profileDict, new JsonSerializerOptions
        {
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }

    // ========== Флаги ==========

    /// <summary>
    /// Возвращает словарь всех флагов профиля.
    /// </summary>
    public Dictionary<string, bool> GetFlags()
    {
        var flags = new Dictionary<string, bool>();
        if (_profileDict == null) return flags;

        if (_profileDict.TryGetValue("flags", out var flagsEl))
        {
            foreach (var prop in flagsEl.EnumerateObject())
            {
                flags[prop.Name] = prop.Value.ValueKind == JsonValueKind.True;
            }
        }
        return flags;
    }

    /// <summary>
    /// Устанавливает значение флага.
    /// </summary>
    public void SetFlag(string flagName, bool value)
    {
        if (_profileDict == null) return;

        var flags = GetFlags();
        flags[flagName] = value;
        _profileDict["flags"] = JsonSerializer.SerializeToElement(flags);
        HasChanges = true;
    }

    // ========== Выбор активного серфера/доски ==========

    /// <summary>
    /// Возвращает DataTag текущей выбранной доски.
    /// </summary>
    public int GetSelectedBoardId()
    {
        if (_profileDict == null) return 0;
        return _profileDict.TryGetValue("selectedBoard", out var el) ? el.GetInt32() : 0;
    }

    /// <summary>
    /// Устанавливает выбранную доску.
    /// </summary>
    public void SetSelectedBoard(int boardDataTag)
    {
        if (_profileDict == null) return;
        _profileDict["selectedBoard"] = JsonSerializer.SerializeToElement(boardDataTag);
        HasChanges = true;
    }

    /// <summary>
    /// Возвращает DataTag текущего выбранного серфера (по isSelected в surferProfiles).
    /// </summary>
    public int GetSelectedSurferId()
    {
        if (_profileDict == null) return 0;
        var surfers = GetSurferProfiles();
        foreach (var s in surfers)
        {
            if (s.TryGetValue("isSelected", out var sel) && sel.ValueKind == JsonValueKind.True)
            {
                return s.TryGetValue("id", out var id) ? id.GetInt32() : 0;
            }
        }
        return 0;
    }

    /// <summary>
    /// Устанавливает выбранного серфера (isSelected = true для указанного, false для остальных).
    /// </summary>
    public void SetSelectedSurfer(int surferDataTag)
    {
        if (_profileDict == null) return;

        var surfers = GetSurferProfiles();
        for (int i = 0; i < surfers.Count; i++)
        {
            bool isTarget = surfers[i].TryGetValue("id", out var id) && id.GetInt32() == surferDataTag;
            surfers[i]["isSelected"] = JsonSerializer.SerializeToElement(isTarget);
        }
        _profileDict["surferProfiles"] = SerializeListOfDicts(surfers);
        HasChanges = true;
    }

    // ========== Туториал ==========

    /// <summary>
    /// Возвращает текущий шаг туториала.
    /// </summary>
    public int GetTutorialStep()
    {
        if (_profileDict == null) return 0;
        return _profileDict.TryGetValue("tutorialStep", out var el) ? el.GetInt32() : 0;
    }

    /// <summary>
    /// Устанавливает шаг туториала (5 = завершён).
    /// </summary>
    public void SetTutorialStep(int step)
    {
        if (_profileDict == null) return;
        _profileDict["tutorialStep"] = JsonSerializer.SerializeToElement(step);
        HasChanges = true;
    }

    /// <summary>
    /// Получает произвольное целое значение из профиля.
    /// </summary>
    public int GetIntValue(string key, int defaultValue = 0)
    {
        if (_profileDict == null) return defaultValue;
        return _profileDict.TryGetValue(key, out var el) ? el.GetInt32() : defaultValue;
    }

    /// <summary>
    /// Устанавливает произвольное целое значение в профиль.
    /// </summary>
    public void SetIntValue(string key, int value)
    {
        if (_profileDict == null) return;
        _profileDict[key] = JsonSerializer.SerializeToElement(value);
        HasChanges = true;
    }
}
