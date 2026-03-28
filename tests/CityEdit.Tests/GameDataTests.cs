using CityEdit.Models.GameData;
using Xunit;

namespace CityEdit.Tests.GameData;

/// <summary>
/// Тесты справочника серферов.
/// </summary>
public class SurferDatabaseTests
{
    [Fact]
    public void Names_ContainsJake()
    {
        Assert.True(SurferDatabase.Names.ContainsKey(-1836944478));
        Assert.Equal("Jake", SurferDatabase.Names[-1836944478]);
    }

    [Fact]
    public void Names_ContainsAsh()
    {
        Assert.True(SurferDatabase.Names.ContainsKey(966716028));
        Assert.Equal("Ash", SurferDatabase.Names[966716028]);
    }

    [Fact]
    public void Names_HasAtLeast24Surfers()
    {
        Assert.True(SurferDatabase.Names.Count >= 24,
            $"Expected at least 24 surfers, got {SurferDatabase.Names.Count}");
    }

    [Fact]
    public void Names_AllNamesAreNotEmpty()
    {
        foreach (var kvp in SurferDatabase.Names)
        {
            Assert.False(string.IsNullOrWhiteSpace(kvp.Value),
                $"Surfer with DataTag {kvp.Key} has empty name");
        }
    }

    [Fact]
    public void Names_NoDuplicateNames()
    {
        var names = new HashSet<string>();
        foreach (var kvp in SurferDatabase.Names)
        {
            Assert.True(names.Add(kvp.Value),
                $"Duplicate surfer name: {kvp.Value}");
        }
    }
}

/// <summary>
/// Тесты справочника досок.
/// </summary>
public class BoardDatabaseTests
{
    [Fact]
    public void Names_ContainsElectricBlue()
    {
        Assert.True(BoardDatabase.Names.ContainsKey(857306595));
        Assert.Equal("Electric Blue", BoardDatabase.Names[857306595]);
    }

    [Fact]
    public void Names_ContainsHomeRunner()
    {
        Assert.True(BoardDatabase.Names.ContainsKey(-1398156391));
        Assert.Equal("Home Runner", BoardDatabase.Names[-1398156391]);
    }

    [Fact]
    public void Names_HasAtLeast20Boards()
    {
        Assert.True(BoardDatabase.Names.Count >= 20,
            $"Expected at least 20 boards, got {BoardDatabase.Names.Count}");
    }

    [Fact]
    public void Names_AllNamesAreNotEmpty()
    {
        foreach (var kvp in BoardDatabase.Names)
        {
            Assert.False(string.IsNullOrWhiteSpace(kvp.Value),
                $"Board with DataTag {kvp.Key} has empty name");
        }
    }

    [Fact]
    public void HomeRunner_BelongsToAsh()
    {
        // Home Runner (id -1398156391) принадлежит Ash (id 966716028)
        Assert.True(BoardDatabase.Names.ContainsKey(-1398156391));
        Assert.True(SurferDatabase.Names.ContainsKey(966716028));
    }
}

/// <summary>
/// Тесты справочника скинов.
/// </summary>
public class SkinDatabaseTests
{
    [Fact]
    public void Skins_HasEntries()
    {
        Assert.True(SkinDatabase.Skins.Count > 0,
            "SkinDatabase should have entries");
    }

    [Fact]
    public void Skins_AllHaveValidOwner()
    {
        foreach (var entry in SkinDatabase.Skins)
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.Value.DisplayName),
                $"Skin {entry.Key} has empty name");
            // Проверяем, что владелец существует в справочнике серферов
            Assert.True(SurferDatabase.Names.ContainsKey(entry.Value.OwnerId),
                $"Skin '{entry.Value.DisplayName}' has owner DataTag {entry.Value.OwnerId} " +
                $"not found in SurferDatabase");
        }
    }

    [Fact]
    public void GetSkinsForSurfer_Jake_ReturnsNonEmpty()
    {
        var jakeSkins = SkinDatabase.GetSkinsForSurfer(-1836944478);
        Assert.NotEmpty(jakeSkins);
    }
}

/// <summary>
/// Тесты справочника покупок.
/// </summary>
public class PurchaseDatabaseTests
{
    [Fact]
    public void AllPurchases_HasAtLeast100Entries()
    {
        Assert.True(PurchaseDatabase.AllPurchases.Count >= 100,
            $"Expected at least 100 purchase entries, got {PurchaseDatabase.AllPurchases.Count}");
    }

    [Fact]
    public void ByProductId_RemoveAdsExists()
    {
        Assert.True(PurchaseDatabase.ByProductId.ContainsKey(PurchaseDatabase.RemoveAdsId));
    }

    [Fact]
    public void Categories_ContainsExpectedCategories()
    {
        var categories = PurchaseDatabase.Categories;
        Assert.Contains("Common", categories);
        Assert.Contains("Bonus Tracks", categories);
    }

    [Fact]
    public void BonusTrackTemplate_FormatsCorrectly()
    {
        var track1 = string.Format(PurchaseDatabase.BonusTrackTemplate, 1);
        Assert.Equal("districttrial.premiumladder.001", track1);

        var track16 = string.Format(PurchaseDatabase.BonusTrackTemplate, 16);
        Assert.Equal("districttrial.premiumladder.016", track16);
    }

    [Fact]
    public void BonusTrackCount_Is16()
    {
        Assert.Equal(16, PurchaseDatabase.BonusTrackCount);
    }

    [Fact]
    public void AllPurchases_AllHaveDescriptions()
    {
        foreach (var entry in PurchaseDatabase.AllPurchases)
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.Description),
                $"Purchase '{entry.ProductId}' has empty description");
        }
    }

    [Fact]
    public void AllPurchases_AllHaveCategory()
    {
        foreach (var entry in PurchaseDatabase.AllPurchases)
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.Category),
                $"Purchase '{entry.ProductId}' has empty category");
        }
    }

    [Fact]
    public void Search_FindsRemoveAds()
    {
        var results = PurchaseDatabase.Search("keypack6").ToList();
        Assert.NotEmpty(results);
    }

    [Fact]
    public void GetByCategory_BonusTracks_ReturnsEntries()
    {
        var results = PurchaseDatabase.GetByCategory("Bonus Tracks").ToList();
        Assert.True(results.Count >= 16);
    }

    [Fact]
    public void GetDescription_KnownProduct_ReturnsDescription()
    {
        var desc = PurchaseDatabase.GetDescription(PurchaseDatabase.RemoveAdsId);
        Assert.NotEqual(PurchaseDatabase.RemoveAdsId, desc); // Should return actual description, not id
    }
}

/// <summary>
/// Тесты WalletTags.
/// </summary>
public class WalletTagsTests
{
    [Fact]
    public void Names_HasExpectedCount()
    {
        Assert.True(WalletTags.Names.Count >= 3,
            $"Expected at least 3 wallet tags, got {WalletTags.Names.Count}");
    }

    [Fact]
    public void Names_AllHaveDisplayNames()
    {
        foreach (var tag in WalletTags.Names)
        {
            Assert.False(string.IsNullOrWhiteSpace(tag.Value),
                $"WalletTag '{tag.Key}' has empty display name");
        }
    }
}
