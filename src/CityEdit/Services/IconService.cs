using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace CityEdit.Services;

/// <summary>
/// Сервис загрузки и кэширования иконок игровых предметов.
/// Использует embedded ресурсы или файлы из папки icons/.
/// Иконки извлекаются скриптом tools/extract_icons.py из APK игры.
/// </summary>
public class IconService
{
    /// <summary>
    /// Кэш загруженных иконок: ключ -> путь к файлу.
    /// </summary>
    private readonly Dictionary<string, string> _cache = new();

    /// <summary>
    /// Базовая папка с иконками.
    /// </summary>
    private readonly string? _iconsDir;

    /// <summary>
    /// Признак доступности иконок.
    /// </summary>
    public bool IsAvailable { get; }

    /// <summary>
    /// Создаёт экземпляр IconService.
    /// Ищет папку icons/ рядом с приложением или в Assets.
    /// </summary>
    public IconService()
    {
        // Пробуем найти папку icons рядом с исполняемым файлом
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(appDir, "icons"),
            Path.Combine(appDir, "Assets", "icons"),
            Path.Combine(appDir, "..", "icons"),
        };

        foreach (var dir in candidates)
        {
            if (Directory.Exists(dir))
            {
                _iconsDir = Path.GetFullPath(dir);
                IsAvailable = true;
                IndexIcons();
                break;
            }
        }
    }

    /// <summary>
    /// Индексирует все найденные иконки.
    /// </summary>
    private void IndexIcons()
    {
        if (_iconsDir == null) return;

        foreach (var category in new[] { "surfers", "boards", "skins" })
        {
            var catDir = Path.Combine(_iconsDir, category);
            if (!Directory.Exists(catDir)) continue;

            foreach (var file in Directory.GetFiles(catDir, "*.png"))
            {
                var name = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                var key = $"{category}/{name}";
                _cache[key] = file;
            }
        }
    }

    /// <summary>
    /// Получает путь к иконке серфера по имени.
    /// </summary>
    /// <param name="surferName">Имя серфера (например, "Jake").</param>
    /// <returns>Путь к файлу иконки или null.</returns>
    public string? GetSurferIcon(string surferName)
    {
        var key = $"surfers/{surferName.ToLowerInvariant().Replace(" ", "_")}";
        return _cache.TryGetValue(key, out var path) ? path : null;
    }

    /// <summary>
    /// Получает путь к иконке доски по имени.
    /// </summary>
    /// <param name="boardName">Имя доски (например, "Electric Blue").</param>
    /// <returns>Путь к файлу иконки или null.</returns>
    public string? GetBoardIcon(string boardName)
    {
        var key = $"boards/{boardName.ToLowerInvariant().Replace(" ", "_")}";
        return _cache.TryGetValue(key, out var path) ? path : null;
    }

    /// <summary>
    /// Получает путь к иконке скина по имени.
    /// </summary>
    /// <param name="skinName">Имя скина.</param>
    /// <returns>Путь к файлу иконки или null.</returns>
    public string? GetSkinIcon(string skinName)
    {
        var key = $"skins/{skinName.ToLowerInvariant().Replace(" ", "_")}";
        return _cache.TryGetValue(key, out var path) ? path : null;
    }

    /// <summary>
    /// Получает общее количество загруженных иконок.
    /// </summary>
    public int Count => _cache.Count;

    /// <summary>
    /// Получает количество иконок в категории.
    /// </summary>
    public int GetCategoryCount(string category)
    {
        int count = 0;
        var prefix = $"{category}/";
        foreach (var key in _cache.Keys)
        {
            if (key.StartsWith(prefix)) count++;
        }
        return count;
    }
}
