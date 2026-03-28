using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CityEdit.Models.GameData;

/// <summary>
/// Справочник досок Subway Surfers City.
/// Содержит маппинг числовых идентификаторов DataTag на имена досок,
/// а также привязку досок к владельцам (серферам).
/// </summary>
public static class BoardDatabase
{
    /// <summary>
    /// Словарь: DataTag доски -> имя доски.
    /// Все 27 игровых досок.
    /// </summary>
    public static readonly ReadOnlyDictionary<int, string> Names = new(new Dictionary<int, string>
    {
        { 857306595, "Electric Blue" },
        { -1398156391, "Home Runner" },
        { 821107692, "Trasher" },
        { 1016074756, "Peace Of Grind" },
        { 797961277, "Naughty & Nice" },
        { 1346993456, "Globetrotter" },
        { -2027086747, "Grandmaster" },
        { -2028224723, "Djinn's Fortune" },
        { -1569537423, "Honeycomb" },
        { -282672473, "Flame Tamer" },
        { -573684740, "Wakizashi" },
        { 463203314, "Knockout" },
        { -2089484430, "Spaced Invader" },
        { 1092080220, "Pawsome" },
        { -875752536, "Dog City" },
        { -1017845163, "Sub Surf Classic" },
        { -1886993925, "Vaunted" },
        { -425685755, "Zephyr Cruiser" },
        { -1101319815, "Eye Of The Viper" },
        { -1995581156, "Day Of The Shred" },
        { -487324792, "Sweet Street" },
        { -1946062440, "G-Tiger" },
        { -2014824727, "Cy-Board" },
        { 369661746, "Cobweb" },
        { 1389875337, "Super Hooper" },
        { -268124908, "H4X0R" },
        { -762458939, "Pouncer" },
        { -1702093280, "Aquiline" }
    });

    /// <summary>
    /// Словарь: DataTag доски -> DataTag серфера-владельца.
    /// Определяет, какой серфер является владельцем данной доски.
    /// </summary>
    public static readonly ReadOnlyDictionary<int, int> BoardOwners = new(new Dictionary<int, int>
    {
        { 821107692, -1836944478 },       // Trasher -> Jake
        { 797961277, 1900660162 },         // Naughty & Nice -> Tricky
        { -2027086747, 2129411796 },       // Grandmaster -> Fresh
        { -1569537423, 1614866432 },       // Honeycomb -> Miss Maia
        { -573684740, 1804257387 },        // Wakizashi -> Ninja One
        { -2089484430, 1663244716 },       // Spaced Invader -> Yutani
        { -875752536, 849273384 },         // Dog City -> Spike
        { -1886993925, 1200047034 },       // Vaunted -> Ella
        { -425685755, 1120354844 },        // Zephyr Cruiser -> Jay
        { -1101319815, 581326566 },        // Eye Of The Viper -> Billy
        { -1995581156, -1505268145 },      // Day Of The Shred -> Rosalita
        { -487324792, 299562833 },         // Sweet Street -> Tasha
        { -1946062440, -1733051898 },      // G-Tiger -> Jaewoo
        { -2014824727, 1887684367 },       // Cy-Board -> Tagbot
        { 369661746, 852717139 },          // Cobweb -> Lucy
        { 1389875337, -2125407733 },       // Super Hooper -> Georgie
        { -268124908, -1534276928 },       // H4X0R -> V3ctor
        { -762458939, -2075784936 },       // Pouncer -> Zara
        { -1702093280, -2116248615 },      // Aquiline -> Lilah
        { 1016074756, 1363767693 },        // Peace Of Grind -> Jenny
        { 1346993456, -518167090 },        // Globetrotter -> Wei
        { -2028224723, 1936280213 },       // Djinn's Fortune -> Prince K
        { -282672473, 135046766 },         // Flame Tamer -> Monique
        { 463203314, -502265868 },         // Knockout -> Noon
        { 1092080220, 823378763 },         // Pawsome -> Harini
        { -1398156391, 966716028 }           // Home Runner -> Ash
    });

    /// <summary>
    /// Возвращает имя доски по её DataTag.
    /// </summary>
    /// <param name="dataTag">Числовой идентификатор доски.</param>
    /// <returns>Имя доски или строка с ID.</returns>
    public static string GetName(int dataTag)
    {
        return Names.TryGetValue(dataTag, out var name) ? name : dataTag.ToString();
    }

    /// <summary>
    /// Возвращает имя владельца доски (серфера).
    /// </summary>
    /// <param name="boardDataTag">DataTag доски.</param>
    /// <returns>Имя серфера-владельца или null, если владелец не определён.</returns>
    public static string? GetOwnerName(int boardDataTag)
    {
        if (BoardOwners.TryGetValue(boardDataTag, out int ownerId))
        {
            return SurferDatabase.GetName(ownerId);
        }
        return null;
    }
}
