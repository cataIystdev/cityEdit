using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using CityEdit.Core;
using CityEdit.Crypto;
using CityEdit.Models.GameData;
using CityEdit.Services;
using Xunit;

namespace CityEdit.Tests.Services;

/// <summary>
/// Тесты ProfileService.
/// Используют реальный файл профиля из корня проекта.
/// </summary>
public class ProfileServiceTests
{
    private static readonly string TestProfilePath = Path.Combine(
        Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "profile");

    private ProfileService? CreateLoadedService()
    {
        if (!File.Exists(TestProfilePath)) return null;
        var svc = new ProfileService();
        svc.LoadFromFile(TestProfilePath);
        return svc;
    }

    // ==================== Загрузка ====================

    [Fact]
    public void LoadFromFile_ValidProfile_SetsIsLoadedTrue()
    {
        var svc = CreateLoadedService();
        if (svc == null) return;
        Assert.True(svc.IsLoaded);
    }

    [Fact]
    public void LoadFromFile_ValidProfile_SetsCurrentFilePath()
    {
        var svc = CreateLoadedService();
        if (svc == null) return;
        Assert.NotNull(svc.CurrentFilePath);
    }

    [Fact]
    public void LoadFromFile_NonExistentFile_ThrowsFileNotFoundException()
    {
        var svc = new ProfileService();
        Assert.Throws<FileNotFoundException>(() =>
            svc.LoadFromFile("/tmp/nonexistent_profile_12345"));
    }

    [Fact]
    public void LoadFromFile_ValidProfile_HasNoChanges()
    {
        var svc = CreateLoadedService();
        if (svc == null) return;
        Assert.False(svc.HasChanges);
    }

    // ==================== Серферы ====================

    [Fact]
    public void GetSurferProfiles_ReturnsNonEmptyList()
    {
        var svc = CreateLoadedService();
        if (svc == null) return;
        var surfers = svc.GetSurferProfiles();
        Assert.NotEmpty(surfers);
    }

    [Fact]
    public void GetSurferProfiles_ContainsJake()
    {
        var svc = CreateLoadedService();
        if (svc == null) return;
        var surfers = svc.GetSurferProfiles();
        Assert.Contains(surfers, s =>
            s.ContainsKey("id") && s["id"].GetInt32() == -1836944478);
    }

    [Fact]
    public void SetAllSurfers_UnlocksAll()
    {
        var svc = CreateLoadedService();
        if (svc == null) return;

        svc.SetAllSurfers(true, Constants.MaxLevel);
        var surfers = svc.GetSurferProfiles();
        Assert.All(surfers, s =>
        {
            Assert.True(s["isUnlocked"].GetBoolean());
            Assert.Equal(Constants.MaxLevel, s["level"].GetInt32());
        });
        Assert.True(svc.HasChanges);
    }

    // ==================== Доски ====================

    [Fact]
    public void GetBoardProfiles_ReturnsNonEmptyList()
    {
        var svc = CreateLoadedService();
        if (svc == null) return;
        var boards = svc.GetBoardProfiles();
        Assert.NotEmpty(boards);
    }

    [Fact]
    public void SetAllBoards_SetsMaxLevel()
    {
        var svc = CreateLoadedService();
        if (svc == null) return;

        svc.SetAllBoards(true, Constants.MaxLevel);
        var boards = svc.GetBoardProfiles();
        Assert.All(boards, b =>
        {
            Assert.True(b["isUnlocked"].GetBoolean());
            Assert.Equal(Constants.MaxLevel, b["level"].GetInt32());
        });
    }

    // ==================== Валюта ====================

    [Fact]
    public void GetWallet_ReturnsNonEmptyDictionary()
    {
        var svc = CreateLoadedService();
        if (svc == null) return;
        var wallet = svc.GetWallet();
        Assert.NotEmpty(wallet);
    }

    [Fact]
    public void SetWalletItem_ChangesValue()
    {
        var svc = CreateLoadedService();
        if (svc == null) return;

        var wallet = svc.GetWallet();
        var firstTag = wallet.Keys.First();
        svc.SetWalletItem(firstTag, 123456);
        var updated = svc.GetWallet();
        Assert.Equal(123456, updated[firstTag]);
    }

    [Fact]
    public void SetMaxCurrency_SetsAllToMax()
    {
        var svc = CreateLoadedService();
        if (svc == null) return;

        svc.SetMaxCurrency(Constants.MaxCurrencyValue);
        var wallet = svc.GetWallet();
        // SetMaxCurrency only affects primary wallet tags
        foreach (var tag in WalletTags.PrimaryTags)
        {
            Assert.True(wallet.ContainsKey(tag));
            Assert.Equal(Constants.MaxCurrencyValue, wallet[tag]);
        }
    }

    // ==================== Покупки ====================

    [Fact]
    public void GetPurchaseHistory_ReturnsNonNullDictionary()
    {
        var svc = CreateLoadedService();
        if (svc == null) return;
        var purchases = svc.GetPurchaseHistory();
        Assert.NotNull(purchases);
    }

    [Fact]
    public void AddPurchase_AddsToPurchaseHistory()
    {
        var svc = CreateLoadedService();
        if (svc == null) return;

        var testId = "test.purchase.unittest";
        svc.AddPurchase(testId);
        var purchases = svc.GetPurchaseHistory();
        Assert.True(purchases.ContainsKey(testId));
    }

    [Fact]
    public void RemovePurchase_RemovesFromPurchaseHistory()
    {
        var svc = CreateLoadedService();
        if (svc == null) return;

        var testId = "test.purchase.remove";
        svc.AddPurchase(testId);
        Assert.True(svc.GetPurchaseHistory().ContainsKey(testId));

        svc.RemovePurchase(testId);
        Assert.False(svc.GetPurchaseHistory().ContainsKey(testId));
    }

    // ==================== Сезонный пропуск ====================

    [Fact]
    public void SetSeasonPass_SetsValuesCorrectly()
    {
        var svc = CreateLoadedService();
        if (svc == null) return;

        svc.SetSeasonPass(true, 50000);
        var (purchased, points) = svc.GetSeasonPass();
        Assert.True(purchased);
        Assert.Equal(50000, points);
    }

    // ==================== Флаги ====================

    [Fact]
    public void GetFlags_ReturnsNonNullDictionary()
    {
        var svc = CreateLoadedService();
        if (svc == null) return;
        var flags = svc.GetFlags();
        Assert.NotNull(flags);
    }

    [Fact]
    public void SetFlag_ChangesValue()
    {
        var svc = CreateLoadedService();
        if (svc == null) return;

        svc.SetFlag("testFlag", true);
        var flags = svc.GetFlags();
        Assert.True(flags.ContainsKey("testFlag"));
        Assert.True(flags["testFlag"]);
    }

    // ==================== Туториал ====================

    [Fact]
    public void SetTutorialStep_SetsValueCorrectly()
    {
        var svc = CreateLoadedService();
        if (svc == null) return;

        svc.SetTutorialStep(5);
        var step = svc.GetTutorialStep();
        Assert.Equal(5, step);
    }

    // ==================== Статистика ====================

    [Fact]
    public void GetStats_ReturnsNonEmptyDictionary()
    {
        var svc = CreateLoadedService();
        if (svc == null) return;
        var stats = svc.GetStats();
        Assert.NotEmpty(stats);
    }

    // ==================== Активный серфер/доска ====================

    [Fact]
    public void SetSelectedSurfer_ChangesSelection()
    {
        var svc = CreateLoadedService();
        if (svc == null) return;

        svc.SetSelectedSurfer(1900660162); // Tricky
        var selectedId = svc.GetSelectedSurferId();
        Assert.Equal(1900660162, selectedId);
    }

    [Fact]
    public void SetSelectedBoard_ChangesSelection()
    {
        var svc = CreateLoadedService();
        if (svc == null) return;

        svc.SetSelectedBoard(857306595); // Electric Blue
        var selectedId = svc.GetSelectedBoardId();
        Assert.Equal(857306595, selectedId);
    }

    // ==================== Сохранение ====================

    [Fact]
    public void SaveToFile_RoundTrip_PreservesData()
    {
        var svc = CreateLoadedService();
        if (svc == null) return;

        var tmpPath = Path.Combine(Path.GetTempPath(), "cityedit_test_profile_" + Guid.NewGuid());
        try
        {
            svc.SaveToFile(tmpPath);
            Assert.True(File.Exists(tmpPath));

            var svc2 = new ProfileService();
            svc2.LoadFromFile(tmpPath);
            Assert.True(svc2.IsLoaded);

            var surfers1 = svc.GetSurferProfiles();
            var surfers2 = svc2.GetSurferProfiles();
            Assert.Equal(surfers1.Count, surfers2.Count);
        }
        finally
        {
            if (File.Exists(tmpPath)) File.Delete(tmpPath);
        }
    }

    [Fact]
    public void SaveToFile_NotLoaded_ThrowsInvalidOperationException()
    {
        var svc = new ProfileService();
        Assert.Throws<InvalidOperationException>(() => svc.SaveToFile("/tmp/test"));
    }
}
