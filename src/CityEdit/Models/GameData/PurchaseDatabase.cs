using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CityEdit.Models.GameData;

/// <summary>
/// Запись о покупке с идентификатором, категорией и описанием.
/// </summary>
public record PurchaseEntry(string ProductId, string Category, string Description);

/// <summary>
/// Справочник идентификаторов покупок Subway Surfers City.
/// Содержит все 250+ ProductID, сгруппированные по категориям.
/// </summary>
public static class PurchaseDatabase
{
    /// <summary>
    /// Шаблон ProductID для бонусных треков.
    /// Формат: districttrial.premiumladder.{номер:D3}
    /// </summary>
    public const string BonusTrackTemplate = "districttrial.premiumladder.{0:D3}";

    /// <summary>
    /// Количество бонусных треков в игре.
    /// </summary>
    public const int BonusTrackCount = 16;

    /// <summary>
    /// ProductID для удаления рекламы.
    /// </summary>
    public const string RemoveAdsId = "shop.currency.keypack6";

    /// <summary>
    /// ProductID для премиум-пропуска.
    /// </summary>
    public const string PremiumPassId = "premium_pass";

    /// <summary>
    /// Список всех категорий покупок.
    /// </summary>
    public static readonly IReadOnlyList<string> Categories = new[]
    {
        "Common",
        "Currency Packs",
        "Board Unlocks",
        "Surfer Token Bundles",
        "Board Token Bundles",
        "Home Offers",
        "Run Offers",
        "Chain Offers",
        "Bonus Tracks"
    };

    /// <summary>
    /// Полная база всех покупок с описаниями.
    /// </summary>
    public static readonly IReadOnlyList<PurchaseEntry> AllPurchases = BuildAllPurchases();

    /// <summary>
    /// Быстрый lookup по ProductId.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, PurchaseEntry> ByProductId =
        AllPurchases.ToDictionary(p => p.ProductId);

    /// <summary>
    /// Поиск покупок по тексту (ищет в ProductId и Description).
    /// </summary>
    public static IEnumerable<PurchaseEntry> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return AllPurchases;

        var q = query.Trim();
        return AllPurchases.Where(p =>
            p.ProductId.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            p.Description.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            p.Category.Contains(q, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Фильтрация по категории.
    /// </summary>
    public static IEnumerable<PurchaseEntry> GetByCategory(string category)
    {
        if (string.IsNullOrEmpty(category) || category == "All")
            return AllPurchases;
        return AllPurchases.Where(p => p.Category == category);
    }

    /// <summary>
    /// Получить описание покупки, если она есть в базе.
    /// </summary>
    public static string GetDescription(string productId)
    {
        return ByProductId.TryGetValue(productId, out var entry)
            ? entry.Description
            : productId;
    }

    private static List<PurchaseEntry> BuildAllPurchases()
    {
        var list = new List<PurchaseEntry>();

        // === Common ===
        list.AddRange(new[]
        {
            new PurchaseEntry("shop.currency.keypackfree", "Common", "Free key pack"),
            new PurchaseEntry("shop.box.ad", "Common", "Ad box reward"),
            new PurchaseEntry("shop.box.super", "Common", "Super box"),
            new PurchaseEntry("shop.box.admeter", "Common", "Ad meter box reward"),
            new PurchaseEntry("shop.box.super.free", "Common", "Free super box"),
            new PurchaseEntry("premium_pass", "Common", "Premium season pass"),
            new PurchaseEntry("home.offer.jakestarterpack", "Common", "Jake starter pack"),
            new PurchaseEntry("home.offer.missmaiaintermediatepack", "Common", "Miss Maia intermediate pack"),
            new PurchaseEntry("home.offer.freshexpertpack", "Common", "Fresh expert pack"),
        });

        // === Surfer Unlock Offers ===
        var surferUnlocks = new (string id, string name)[]
        {
            ("shop.offer.billyunlock001", "Billy"),
            ("shop.offer.ellaunlock001", "Ella"),
            ("shop.offer.tagbotunlock001", "Tagbot"),
            ("shop.offer.rosalitaunlock001", "Rosalita"),
            ("shop.offer.yutaniunlock001", "Yutani"),
            ("shop.offer.tashaunlock001", "Tasha"),
            ("shop.offer.lilahunlock001", "Lilah"),
            ("shop.offer.zaraunlock001", "Zara"),
            ("shop.offer.jaewoounlock001", "Jaewoo"),
            ("shop.offer.ashunlock001", "Ash"),
            ("shop.offer.jennyunlock001", "Jenny"),
            ("shop.offer.weiunlock001", "Wei"),
            ("shop.offer.princekunlock001", "Prince K"),
            ("shop.offer.moniqueunlock001", "Monique"),
            ("shop.offer.noonunlock001", "Noon"),
            ("shop.offer.lucyunlock001", "Lucy"),
            ("shop.offer.georgieunlock001", "Georgie"),
            ("shop.offer.v3ctorunlock001", "V3ctor"),
        };
        foreach (var (id, name) in surferUnlocks)
            list.Add(new PurchaseEntry(id, "Common", $"Unlock surfer: {name}"));

        // === Currency Packs ===
        var currencyPacks = new (string id, string desc)[]
        {
            ("shop.currency.keypack1", "Key pack (small)"),
            ("shop.currency.keypack2", "Key pack (medium)"),
            ("shop.currency.keypack3", "Key pack (large)"),
            ("shop.currency.keypack4", "Key pack (XL)"),
            ("shop.currency.keypack6", "Key pack (mega)"),
            ("shop.currency.revivepack1", "Revive pack (small)"),
            ("shop.currency.revivepack2", "Revive pack (medium)"),
            ("shop.currency.revivepack3", "Revive pack (large)"),
            ("shop.currency.coinpack1", "Coin pack (500)"),
            ("shop.currency.coinpack2", "Coin pack (2,500)"),
            ("shop.currency.coinpack3", "Coin pack (10,000)"),
            ("shop.currency.coinpack4", "Coin pack (50,000)"),
            ("shop.currency.coinpack5", "Coin pack (150,000)"),
            ("shop.currency.coinpack6", "Coin pack (500,000)"),
            ("shop.currency.ticketpack1", "Ticket pack (small)"),
            ("shop.currency.ticketpack2", "Ticket pack (medium)"),
            ("shop.currency.ticketpack3", "Ticket pack (large)"),
            ("shop.currency.coinfromkeyspack1", "Coins from keys (1)"),
            ("shop.currency.coinfromkeyspack2", "Coins from keys (2)"),
            ("shop.currency.coinfromkeyspack3", "Coins from keys (3)"),
            ("shop.currency.coinfromkeyspack4", "Coins from keys (4)"),
            ("shop.currency.coinfromkeyspack5", "Coins from keys (5)"),
            ("shop.currency.coinfromkeyspack6", "Coins from keys (6)"),
            ("shop.currency.coinfromadpack", "Coins from ad"),
            ("shop.currency.keysfromadpack", "Keys from ad"),
        };
        foreach (var (id, desc) in currencyPacks)
            list.Add(new PurchaseEntry(id, "Currency Packs", desc));

        // === Board Unlocks ===
        var boardUnlocks = new (string id, string surfer)[]
        {
            ("shop.offer.billyboardunlock001", "Billy"),
            ("shop.offer.rosalitaboardunlock001", "Rosalita"),
            ("shop.offer.yutaniboardunlock001", "Yutani"),
            ("shop.offer.tagbotboardunlock001", "Tagbot"),
            ("shop.offer.ellaboardunlock001", "Ella"),
            ("shop.offer.ninjaoneboardunlock001", "Ninja One"),
            ("shop.offer.tashaboardunlock001", "Tasha"),
            ("shop.offer.lilahboardunlock001", "Lilah"),
            ("shop.offer.zaraboardunlock001", "Zara"),
            ("shop.offer.jaewooboardunlock001", "Jaewoo"),
            ("shop.offer.ashboardunlock001", "Ash"),
            ("shop.offer.jennyboardunlock001", "Jenny"),
            ("shop.offer.weiboardunlock001", "Wei"),
            ("shop.offer.spikeboardunlock001", "Spike"),
            ("shop.offer.nickyboardunlock001", "Nicky"),
            ("shop.offer.princekboardunlock001", "Prince K"),
            ("shop.offer.moniqueboardunlock001", "Monique"),
            ("shop.offer.noonboardunlock001", "Noon"),
            ("shop.offer.missmaiaboardunlock001", "Miss Maia"),
            ("shop.offer.lucy_board_unlock001", "Lucy"),
            ("shop.offer.georgie_board_unlock001", "Georgie"),
            ("shop.offer.v3ctor_board_unlock001", "V3ctor"),
        };
        foreach (var (id, surfer) in boardUnlocks)
            list.Add(new PurchaseEntry(id, "Board Unlocks", $"Unlock {surfer}'s board"));

        // === Surfer Token Bundles ===
        var surferTokens = new (string id, string desc)[]
        {
            ("shop.offer.revive_bundle001", "Revive bundle"),
            ("shop.offer.jaketokenbundle001", "Jake tokens (small)"),
            ("shop.offer.jaketokenbundle002", "Jake tokens (large)"),
            ("shop.offer.trickytokenbundle001", "Tricky tokens (small)"),
            ("shop.offer.trickytokenbundle002", "Tricky tokens (large)"),
            ("shop.offer.freshtokenbundle001", "Fresh tokens (small)"),
            ("shop.offer.freshtokenbundle002", "Fresh tokens (large)"),
            ("shop.offer.yutanitokenbundle001", "Yutani tokens (small)"),
            ("shop.offer.yutanitokenbundle002", "Yutani tokens (large)"),
            ("shop.offer.billytokenbundle001", "Billy tokens (small)"),
            ("shop.offer.billytokenbundle002", "Billy tokens (large)"),
            ("shop.offer.nickytokenbundle001", "Nicky tokens (small)"),
            ("shop.offer.nickytokenbundle002", "Nicky tokens (large)"),
            ("shop.offer.rosalitatokenbundle001", "Rosalita tokens (small)"),
            ("shop.offer.rosalitatokenbundle002", "Rosalita tokens (large)"),
            ("shop.offer.missmaiatokenbundle001", "Miss Maia tokens (small)"),
            ("shop.offer.missmaiatokenbundle002", "Miss Maia tokens (large)"),
            ("shop.offer.tagbottokenbundle001", "Tagbot tokens (small)"),
            ("shop.offer.tagbottokenbundle002", "Tagbot tokens (large)"),
            ("shop.offer.spiketokenbundle001", "Spike tokens (small)"),
            ("shop.offer.spiketokenbundle002", "Spike tokens (large)"),
            ("shop.offer.ellatokenbundle001", "Ella tokens (small)"),
            ("shop.offer.ellatokenbundle002", "Ella tokens (large)"),
            ("shop.offer.tashatokenbundle001", "Tasha tokens (small)"),
            ("shop.offer.tashatokenbundle002", "Tasha tokens (large)"),
            ("shop.offer.lilahtokenbundle002", "Lilah tokens"),
            ("shop.offer.ninjatokenbundle002", "Ninja One tokens"),
            ("shop.offer.zaratokenbundle002", "Zara tokens"),
            ("shop.offer.jaewootokenbundle002", "Jaewoo tokens"),
            ("shop.offer.ashtokenbundle002", "Ash tokens"),
            ("shop.offer.jennytokenbundle002", "Jenny tokens"),
            ("shop.offer.weitokenbundle002", "Wei tokens"),
            ("shop.offer.princektokenbundle002", "Prince K tokens"),
            ("shop.offer.moniquetokenbundle002", "Monique tokens"),
            ("shop.offer.noontokenbundle002", "Noon tokens"),
            ("shop.offer.lucytokenbundle002", "Lucy tokens"),
            ("shop.offer.georgietokenbundle002", "Georgie tokens"),
            ("shop.offer.v3ctortokenbundle002", "V3ctor tokens"),
        };
        foreach (var (id, desc) in surferTokens)
            list.Add(new PurchaseEntry(id, "Surfer Token Bundles", desc));

        // === Board Token Bundles ===
        var boardTokens = new (string id, string desc)[]
        {
            ("shop.offer.jakeboardtokenbundle001", "Jake board tokens (small)"),
            ("shop.offer.jakeboardtokenbundle002", "Jake board tokens (large)"),
            ("shop.offer.trickyboardtokenbundle001", "Tricky board tokens (small)"),
            ("shop.offer.trickyboardtokenbundle002", "Tricky board tokens (large)"),
            ("shop.offer.freshboardtokenbundle001", "Fresh board tokens (small)"),
            ("shop.offer.freshboardtokenbundle002", "Fresh board tokens (large)"),
            ("shop.offer.billyboardtokenbundle001", "Billy board tokens (small)"),
            ("shop.offer.billyboardtokenbundle002", "Billy board tokens (large)"),
            ("shop.offer.yutaniboardtokenbundle001", "Yutani board tokens (small)"),
            ("shop.offer.yutaniboardtokenbundle002", "Yutani board tokens (large)"),
            ("shop.offer.rosalitaboardtokenbundle001", "Rosalita board tokens (small)"),
            ("shop.offer.rosalitaboardtokenbundle002", "Rosalita board tokens (large)"),
            ("shop.offer.tagbotboardtokenbundle001", "Tagbot board tokens (small)"),
            ("shop.offer.tagbotboardtokenbundle002", "Tagbot board tokens (large)"),
            ("shop.offer.ellaboardtokenbundle001", "Ella board tokens (small)"),
            ("shop.offer.ellaboardtokenbundle002", "Ella board tokens (large)"),
            ("shop.offer.ninjaoneboardtokenbundle001", "Ninja One board tokens (small)"),
            ("shop.offer.ninjaoneboardtokenbundle002", "Ninja One board tokens (large)"),
            ("shop.offer.tashaboardtokenbundle001", "Tasha board tokens (small)"),
            ("shop.offer.tashaboardtokenbundle002", "Tasha board tokens (large)"),
            ("shop.offer.lilahboardtokenbundle002", "Lilah board tokens"),
            ("shop.offer.zaraboardtokenbundle002", "Zara board tokens"),
            ("shop.offer.jaewooboardtokenbundle002", "Jaewoo board tokens"),
            ("shop.offer.ashboardtokenbundle002", "Ash board tokens"),
            ("shop.offer.jennyboardtokenbundle002", "Jenny board tokens"),
            ("shop.offer.weiboardtokenbundle002", "Wei board tokens"),
            ("shop.offer.spikeboardtokenbundle002", "Spike board tokens"),
            ("shop.offer.nickyboardtokenbundle002", "Nicky board tokens"),
            ("shop.offer.princekboardtokenbundle002", "Prince K board tokens"),
            ("shop.offer.moniqueboardtokenbundle002", "Monique board tokens"),
            ("shop.offer.noonboardtokenbundle002", "Noon board tokens"),
            ("shop.offer.missmaiaboardtokenbundle002", "Miss Maia board tokens"),
            ("shop.offer.lucyboardtokenbundle002", "Lucy board tokens"),
            ("shop.offer.georgieboardtokenbundle002", "Georgie board tokens"),
            ("shop.offer.v3ctorboardtokenbundle002", "V3ctor board tokens"),
        };
        foreach (var (id, desc) in boardTokens)
            list.Add(new PurchaseEntry(id, "Board Token Bundles", desc));

        // === Home Offers ===
        var homeOffers = new (string id, string desc)[]
        {
            ("home.offer.megabundle001", "Mega bundle"),
            ("home.offer.ashunlock", "Ash unlock offer"),
            ("home.offer.billyunlock", "Billy unlock offer"),
            ("home.offer.ellaunlock", "Ella unlock offer"),
            ("home.offer.jaewoounlock", "Jaewoo unlock offer"),
            ("home.offer.nickyunlock", "Nicky unlock offer"),
            ("home.offer.jennyunlock", "Jenny unlock offer"),
            ("home.offer.lilahunlock", "Lilah unlock offer"),
            ("home.offer.rosalitaunlock", "Rosalita unlock offer"),
            ("home.offer.spikeunlock", "Spike unlock offer"),
            ("home.offer.tagbotunlock", "Tagbot unlock offer"),
            ("home.offer.tashaunlock", "Tasha unlock offer"),
            ("home.offer.weiunlock", "Wei unlock offer"),
            ("home.offer.yutaniunlock", "Yutani unlock offer"),
            ("home.offer.zaraunlock", "Zara unlock offer"),
            ("home.offer.noonunlock", "Noon unlock offer"),
            ("home.offer.lucyunlock", "Lucy unlock offer"),
            ("home.offer.georgieunlock", "Georgie unlock offer"),
            ("home.offer.v3ctorunlock", "V3ctor unlock offer"),
            ("home.offer.ashtokenbundle001", "Ash token bundle offer"),
            ("home.offer.billytokenbundle001", "Billy token bundle offer"),
            ("home.offer.ellatokenbundle001", "Ella token bundle offer"),
            ("home.offer.freshtokenbundle001", "Fresh token bundle offer"),
            ("home.offer.georgietokenbundle001", "Georgie token bundle offer"),
            ("home.offer.jaewootokenbundle001", "Jaewoo token bundle offer"),
            ("home.offer.jaketokenbundle001", "Jake token bundle offer"),
            ("home.offer.jaytokenbundle001", "Jay token bundle offer"),
            ("home.offer.jennytokenbundle001", "Jenny token bundle offer"),
            ("home.offer.lilahtokenbundle001", "Lilah token bundle offer"),
            ("home.offer.lucytokenbundle001", "Lucy token bundle offer"),
            ("home.offer.nickytokenbundle001", "Nicky token bundle offer"),
            ("home.offer.noontokenbundle001", "Noon token bundle offer"),
            ("home.offer.rosalitatokenbundle001", "Rosalita token bundle offer"),
            ("home.offer.spiketokenbundle001", "Spike token bundle offer"),
            ("home.offer.tagbottokenbundle001", "Tagbot token bundle offer"),
            ("home.offer.tashatokenbundle001", "Tasha token bundle offer"),
            ("home.offer.trickytokenbundle001", "Tricky token bundle offer"),
            ("home.offer.yutanitokenbundle001", "Yutani token bundle offer"),
            ("home.offer.weibundle001", "Wei bundle offer"),
            ("home.offer.zaratokenbundle001", "Zara token bundle offer"),
        };
        foreach (var (id, desc) in homeOffers)
            list.Add(new PurchaseEntry(id, "Home Offers", desc));

        // === Run Offers ===
        var runOffers = new (string id, string desc)[]
        {
            ("runoffer.revivepack.001", "In-run revive pack #1"),
            ("runoffer.revivepack.002", "In-run revive pack #2"),
            ("runoffer.revivepack.003", "In-run revive pack #3"),
            ("runoffer.campaignticket.001", "In-run campaign ticket #1"),
            ("runoffer.campaignticket.002", "In-run campaign ticket #2"),
            ("runoffer.campaignticket.003", "In-run campaign ticket #3"),
        };
        foreach (var (id, desc) in runOffers)
            list.Add(new PurchaseEntry(id, "Run Offers", desc));

        // === Chain Offers ===
        var chainOffers = new (string id, string desc)[]
        {
            ("chainoffer.coins.100.free", "Chain: 100 coins (free)"),
            ("chainoffer.coins.120.free", "Chain: 120 coins (free)"),
            ("chainoffer.coins.200.free", "Chain: 200 coins (free)"),
            ("chainoffer.coins.230.free", "Chain: 230 coins (free)"),
            ("chainoffer.coins.250.free", "Chain: 250 coins (free)"),
            ("chainoffer.coins.500.free", "Chain: 500 coins (free)"),
            ("chainoffer.coins.1000.free", "Chain: 1,000 coins (free)"),
            ("chainoffer.coins.3000.free", "Chain: 3,000 coins (free)"),
            ("chainoffer.coins.3300.free", "Chain: 3,300 coins (free)"),
            ("chainoffer.coins.3500.free", "Chain: 3,500 coins (free)"),
            ("chainoffer.coins.5000.free", "Chain: 5,000 coins (free)"),
            ("chainoffer.coins.7000.free", "Chain: 7,000 coins (free)"),
            ("chainoffer.coins.7500.free", "Chain: 7,500 coins (free)"),
            ("chainoffer.keys.2.ad", "Chain: 2 keys (ad)"),
            ("chainoffer.keys.3.ad", "Chain: 3 keys (ad)"),
            ("chainoffer.keys.5.ad", "Chain: 5 keys (ad)"),
            ("chainoffer.keys.10.free", "Chain: 10 keys (free)"),
            ("chainoffer.keys.20.free", "Chain: 20 keys (free)"),
            ("chainoffer.keys.50.iap", "Chain: 50 keys (IAP)"),
            ("chainoffer.keys.70.free", "Chain: 70 keys (free)"),
            ("chainoffer.keys.125.iap", "Chain: 125 keys (IAP)"),
            ("chainoffer.revives.1.free", "Chain: 1 revive (free)"),
            ("chainoffer.revives.3.ad", "Chain: 3 revives (ad)"),
            ("chainoffer.revives.5.free", "Chain: 5 revives (free)"),
            ("chainoffer.revives.10.free", "Chain: 10 revives (free)"),
            ("chainoffer.revives.12.free", "Chain: 12 revives (free)"),
            ("chainoffer.revives.15.iap", "Chain: 15 revives (IAP)"),
            ("chainoffer.revives.30.iap", "Chain: 30 revives (IAP)"),
            ("chainoffer.revives.75.iap", "Chain: 75 revives (IAP)"),
            ("chainoffer.jaketokens.8.free", "Chain: 8 Jake tokens (free)"),
            ("chainoffer.jaketokens.200.free", "Chain: 200 Jake tokens (free)"),
            ("chainoffer.jaketokens.300.iap", "Chain: 300 Jake tokens (IAP)"),
            ("chainoffer.jaketokens.500.iap", "Chain: 500 Jake tokens (IAP)"),
            ("chainoffer.jakeboardtokens.7.free", "Chain: 7 Jake board tokens"),
            ("chainoffer.jakeboardtokens.15.free", "Chain: 15 Jake board tokens"),
            ("chainoffer.jakeboardtokens.20.free", "Chain: 20 Jake board tokens"),
            ("chainoffer.tagbot.unlock.iap", "Chain: Tagbot unlock (IAP)"),
            ("chainoffer.tagbottokens.150.free", "Chain: 150 Tagbot tokens"),
            ("chainoffer.tagbottokens.200.free", "Chain: 200 Tagbot tokens"),
            ("chainoffer.tagbottokens.350.iap", "Chain: 350 Tagbot tokens (IAP)"),
            ("chainoffer.tagbotboardtokens.350.free", "Chain: 350 Tagbot board tokens"),
            ("chainoffer.zaratokens.275.free", "Chain: 275 Zara tokens"),
            ("chainoffer.zaratokens.385.iap", "Chain: 385 Zara tokens (IAP)"),
            ("chainoffer.zaratokens.535.iap", "Chain: 535 Zara tokens (IAP)"),
            ("chainoffer.jaewoo.unlock.iap", "Chain: Jaewoo unlock (IAP)"),
            ("chainoffer.jaewootokens.350.iap", "Chain: 350 Jaewoo tokens (IAP)"),
            ("chainoffer.jaewootokens.200.free", "Chain: 200 Jaewoo tokens"),
            ("chainoffer.jaewootokens.150.free", "Chain: 150 Jaewoo tokens"),
        };
        foreach (var (id, desc) in chainOffers)
            list.Add(new PurchaseEntry(id, "Chain Offers", desc));

        // Chain offer series (rvav1, rvav2, iapstepup, iapstepup2, iapconv)
        for (int i = 1; i <= 8; i++)
        {
            list.Add(new PurchaseEntry($"chainoffer.rvav1.{i}", "Chain Offers", $"Rewarded video chain 1 (step {i})"));
            list.Add(new PurchaseEntry($"chainoffer.rvav2.{i}", "Chain Offers", $"Rewarded video chain 2 (step {i})"));
        }
        for (int i = 1; i <= 13; i++)
        {
            list.Add(new PurchaseEntry($"chainoffer.iapstepup.{i}", "Chain Offers", $"IAP step-up chain 1 (step {i})"));
            list.Add(new PurchaseEntry($"chainoffer.iapstepup2.{i}", "Chain Offers", $"IAP step-up chain 2 (step {i})"));
        }
        for (int i = 1; i <= 20; i++)
        {
            list.Add(new PurchaseEntry($"chainoffer.iapconv.{i}", "Chain Offers", $"IAP conversion chain (step {i})"));
        }

        // === Bonus Tracks ===
        for (int i = 1; i <= BonusTrackCount; i++)
        {
            list.Add(new PurchaseEntry(
                string.Format(BonusTrackTemplate, i),
                "Bonus Tracks",
                $"Premium bonus track #{i}"));
        }

        return list;
    }
}
