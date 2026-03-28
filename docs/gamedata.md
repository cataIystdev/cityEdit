# CityEdit — Игровые данные (Game Data Reference)

Справочник структуры данных Subway Surfers City, используемых в CityEdit.

## Формат файла профиля

Файл сохранения расположен по пути:
```
/data/data/com.sybogames.subwaycity/files/profile
```

### Бинарная структура

| Смещение | Размер | Описание |
|----------|--------|----------|
| 0x00 | 16 байт | IV (вектор инициализации) |
| 0x10 | 16 байт | KEY (ключ AES-128) |
| 0x20 | N байт | Зашифрованные данные (AES-128-CTR) |

### Шифрование

- **Алгоритм:** AES-128-CTR
- **Ключ:** 16 байт, хранится в файле
- **IV:** 16 байт, хранится в файле
- **Инкремент счётчика:** байты 15→8 (little-endian, carry propagation)

### JSON-структура (расшифрованная)

```json
{
  "version": 1,
  "lastUpdated": "...",
  "profile": "<JSON-строка с данными профиля>",
  "hash": "..."
}
```

Поле `profile` содержит вложенный JSON-объект в виде строки.

## Структура профиля

### Серферы (`surferProfiles`)

```json
{
  "surferProfiles": [
    {
      "dataTag": -1836944478,
      "isUnlocked": true,
      "isSelected": true,
      "level": 5,
      "highScore": 123456
    }
  ]
}
```

| Поле | Тип | Описание |
|------|-----|----------|
| `dataTag` | int | Уникальный ID серфера (DataTag) |
| `isUnlocked` | bool | Разблокирован ли серфер |
| `isSelected` | bool | Выбран ли серфер активным |
| `level` | int | Уровень (1–20) |
| `highScore` | int | Рекорд серфера |

### Скины

Каждый серфер может иметь массив `skins`:
```json
{
  "dataTag": -1836944478,
  "skins": [
    { "skinId": "abc123", "isUnlocked": true }
  ]
}
```

### Доски (`hoverboardProfiles`)

```json
{
  "hoverboardProfiles": [
    {
      "dataTag": 857306595,
      "isUnlocked": true,
      "isSelected": false,
      "level": 3
    }
  ]
}
```

### Кошелёк (`wallets`)

```json
{
  "wallets": [
    { "tag": "softCurrency", "amount": 999999 },
    { "tag": "premiumCurrency", "amount": 999999 },
    { "tag": "key", "amount": 999999 }
  ]
}
```

| Тег | Описание |
|-----|----------|
| `softCurrency` | Монеты |
| `premiumCurrency` | Премиум-валюта |
| `key` | Ключи |
| `seasonCurrency` | Сезонные токены |
| `campaignToken` | Токены кампании |
| `mysteryToken` | Токены загадок |

### Покупки (`purchaseHistory`)

```json
{
  "purchaseHistory": [
    "removeads.01",
    "districttrial.premiumladder.001",
    "jenny.buycharacter"
  ]
}
```

### Флаги (`flags`)

```json
{
  "flags": {
    "hasSeenIntro": true,
    "hasCompletedTutorial": true,
    "gdprConsent": true
  }
}
```

### Статистика

| Ключ | Тип | Описание |
|------|-----|----------|
| `level` | int | Уровень игрока |
| `xp` | int | Очки опыта |
| `totalCoinsCollected` | int | Всего собрано монет |
| `totalDistance` | int | Суммарная дистанция |
| `totalScore` | int | Суммарный счёт |
| `totalRuns` | int | Количество забегов |
| `tutorialStep` | int | Шаг тутора (5 = пройден) |

### Сезонный пропуск

| Ключ | Тип | Описание |
|------|-----|----------|
| `seasonPassPurchased` | bool | Куплен ли сезонный пропуск |
| `seasonPassPoints` | int | Очки сезонного пропуска |

## Справочник DataTag серферов

| DataTag | Имя |
|---------|-----|
| -1836944478 | Jake |
| 1900660162 | Tricky |
| 2129411796 | Fresh |
| 1936280213 | Prince K |
| 1614866432 | Miss Maia |
| 135046766 | Monique |
| 1663244716 | Yutani |
| 823378763 | Harini |
| 1804257387 | Ninja One |
| -502265868 | Noon |
| 1363767693 | Jenny |
| -518167090 | Wei |
| 849273384 | Spike |
| 1200047034 | Ella |
| 1120354844 | Jay |
| 581326566 | Billy |
| -1505268145 | Rosalita |
| 299562833 | Tasha |
| -1733051898 | Jaewoo |
| 1887684367 | Tagbot |
| 852717139 | Lucy |
| -2125407733 | Georgie |
| -2082009823 | Bueno |
| 966716028 | Ash |

## Категории покупок

| Категория | Примеры |
|-----------|---------|
| Characters | `jake.buycharacter`, `tricky.buycharacter` |
| Boards | `board.starboard.buy`, `board.lowrider.buy` |
| Skins | `jake.skin.dark.buy`, `fresh.skin.neon.buy` |
| Boost Items | `headstart.buy`, `scorebooster.buy` |
| Seasonal | `seasonpass.01.buy`, `event.ticket.buy` |
| Promotions | `starter.pack.buy`, `vip.bundle.01` |
| Infrastructure | `removeads.01`, `districttrial.premiumladder.*` |
| System | `tutorial.complete`, `daily.reward.claimed` |

## Бонусные трассы

16 бонусных трасс разблокируются через покупки:
```
districttrial.premiumladder.001
districttrial.premiumladder.002
...
districttrial.premiumladder.016
```
