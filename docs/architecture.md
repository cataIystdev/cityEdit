# CityEdit — Архитектура

## Обзор

CityEdit — кроссплатформенный редактор сохранений игры **Subway Surfers City**, написанный на C# с использованием фреймворка [Avalonia UI](https://avaloniaui.net/).

Поддерживаемые платформы:
- **Windows** (x64) — десктопное приложение
- **Linux** (x64, ARM64) — десктопное приложение
- **Android** (24+) — мобильное приложение с поддержкой Shizuku

## Структура проекта

```
CityEdit/
├── CityEdit.slnx                  # Solution file
├── src/
│   ├── CityEdit/                   # Основной проект (shared)
│   │   ├── Core/                   # Константы, утилиты
│   │   ├── Crypto/                 # AES-CTR шифрование профиля
│   │   ├── Converters/             # Avalonia value converters
│   │   ├── Models/
│   │   │   └── GameData/           # Статические справочники (surfers, boards, skins, purchases)
│   │   ├── Services/               # Бизнес-логика (ProfileService, IconService, IFileAccessService)
│   │   ├── ViewModels/             # MVVM ViewModel'ы
│   │   │   └── Items/              # ItemViewModel'ы для списков
│   │   ├── Views/                  # AXAML разметка + code-behind
│   │   └── Styles/                 # Тема приложения (AppTheme.axaml)
│   └── CityEdit.Android/          # Android-специфичный код
│       ├── Java/                   # Shizuku bridge (Java interop)
│       └── Services/               # AndroidFileAccessService
├── tests/
│   └── CityEdit.Tests/            # xUnit тесты
├── tools/
│   └── extract_icons.py           # Скрипт извлечения иконок из APK
└── docs/                          # Документация
```

## Архитектурные слои

### 1. Crypto Layer (`CityEdit.Crypto`)

Занимается шифрованием/дешифрованием файлов профиля.

| Класс | Назначение |
|-------|-----------|
| `AesCtrCipher` | Реализация AES-128 в режиме CTR (Counter Mode) с кастомным инкрементом счётчика (байты 15→8) |
| `ProfileCrypto` | Высокоуровневые операции: `Decrypt(byte[])`, `Encrypt(iv, key, json)`, файловые обёртки |

**Формат файла профиля:**
```
[IV: 16 bytes][KEY: 16 bytes][AES-CTR encrypted JSON data]
```

### 2. Models Layer (`CityEdit.Models.GameData`)

Статические справочники игровых данных, извлечённые из APK:

| Класс | Записей | Назначение |
|-------|---------|-----------|
| `SurferDatabase` | 27 | Маппинг DataTag → имя серфера |
| `BoardDatabase` | 77 | Маппинг DataTag → имя доски |
| `SkinDatabase` | ~140 | Маппинг skinId → (имя скина, DataTag владельца) |
| `PurchaseDatabase` | 250+ | Каталог покупок с категориями и описаниями |
| `WalletTags` | 6 | Маппинг тегов кошелька (монеты, ключи, токены) |

### 3. Services Layer (`CityEdit.Services`)

| Класс | Назначение |
|-------|-----------|
| `ProfileService` | Центральный сервис: загрузка/сохранение профиля, CRUD операции над серферами, досками, скинами, валютой, покупками, флагами, настройками |
| `IconService` | Загрузка и кэширование иконок из файловой системы |
| `IFileAccessService` | Абстракция доступа к файлам (десктоп vs Android) |
| `DesktopFileAccessService` | Реализация для десктопа (диалоги файлов) |
| `AndroidFileAccessService` | Реализация для Android (Shizuku, прямой доступ к `/data/data/`) |

### 4. ViewModels Layer (`CityEdit.ViewModels`)

Использует `CommunityToolkit.Mvvm` для source-generated свойств и команд.

| Класс | Назначение |
|-------|-----------|
| `ViewModelBase` | Базовый класс с `ObservableObject` |
| `MainWindowViewModel` | Главный ViewModel: все вкладки, команды, навигация |
| Item ViewModels | `SurferItemViewModel`, `BoardItemViewModel`, `SkinItemViewModel`, `StatItemViewModel`, `WalletItemViewModel`, `PurchaseItemViewModel`, `FlagItemViewModel` |

### 5. Views Layer (`CityEdit.Views`)

| View | Платформа | Назначение |
|------|-----------|-----------|
| `MainWindow.axaml` | Desktop | Двухколоночный layout с sidebar навигацией |
| `MobileMainView.axaml` | Android | Горизонтальные табы с прокруткой |
| `QuickActionsView` | Оба | Быстрые действия: Unlock All, Max Currency, и т.д. |
| `SurfersView` | Оба | Список серферов с unlock/level/highscore |
| `BoardsView` | Оба | Список досок с unlock/level |
| `StatsView` | Оба | Статистика профиля |
| `WalletView` | Оба | Валюта: монеты, ключи |
| `PurchasesView` | Оба | Покупки с категориями и поиском |
| `SeasonPassView` | Оба | Сезонный пропуск |
| `FlagsView` | Оба | Флаги профиля (toggle switches) |
| `ShizukuStatusView` | Android | Статус Shizuku (overlay) |

## Потоки данных

### Загрузка профиля
```
File → ProfileCrypto.Decrypt → JSON → ProfileService.LoadFromFile → ViewModel.RefreshAllTabs → UI
```

### Редактирование
```
UI Action → ViewModel Command → ProfileService.SetXxx → Mark HasChanges
```

### Сохранение
```
ProfileService.SerializeProfile → JSON → ProfileCrypto.Encrypt → File
```

## Android: Shizuku Integration

На Android CityEdit использует **Shizuku** для доступа к файлам профиля в `/data/data/com.sybogames.subwaycity/`:

1. Проверка: Shizuku работает? Есть разрешение?
2. Если нет → overlay экран с инструкцией
3. Если да → копирование профиля через `cat` из Shizuku shell → редактирование → запись обратно

## Зависимости

| Пакет | Версия | Назначение |
|-------|--------|-----------|
| Avalonia | 11.3.12 | UI фреймворк |
| Avalonia.Themes.Fluent | 11.3.12 | Тема оформления |
| Avalonia.Fonts.Inter | 11.3.12 | Шрифт Inter |
| Avalonia.Desktop | 11.3.12 | Desktop integration |
| Avalonia.Android | 11.3.12 | Android integration |
| CommunityToolkit.Mvvm | 8.2.1 | MVVM source generators |
| Shizuku (binding) | — | Android privilege escalation |
