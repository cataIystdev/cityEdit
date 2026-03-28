namespace CityEdit.Core;

/// <summary>
/// Константы приложения CityEdit.
/// Содержит версию, имя разработчика и параметры шифрования.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Название приложения.
    /// </summary>
    public const string AppName = "CityEdit";

    /// <summary>
    /// Подзаголовок приложения.
    /// </summary>
    public const string AppSubtitle = "PROFILE EDITOR";

    /// <summary>
    /// Текущая версия приложения.
    /// </summary>
    public const string AppVersion = "v1.1.0";

    /// <summary>
    /// Имя разработчика.
    /// </summary>
    public const string DeveloperName = "Catalyst";

    /// <summary>
    /// Размер блока шифрования AES в байтах.
    /// </summary>
    public const int AesBlockSize = 16;

    /// <summary>
    /// Размер вектора инициализации (IV) в байтах.
    /// </summary>
    public const int IvSize = 16;

    /// <summary>
    /// Размер ключа шифрования в байтах.
    /// </summary>
    public const int KeySize = 16;

    /// <summary>
    /// Минимальный допустимый размер файла профиля (IV + KEY).
    /// </summary>
    public const int MinProfileFileSize = IvSize + KeySize;

    /// <summary>
    /// Максимальный уровень серфера или доски.
    /// </summary>
    public const int MaxLevel = 20;

    /// <summary>
    /// Минимальный уровень серфера или доски.
    /// </summary>
    public const int MinLevel = 1;

    /// <summary>
    /// Максимальное значение валюты для быстрого действия "Установить максимум".
    /// </summary>
    public const int MaxCurrencyValue = 999999;

    /// <summary>
    /// Максимальное количество очков сезонного пропуска для быстрого действия.
    /// </summary>
    public const int MaxSeasonPoints = 99999;
}
