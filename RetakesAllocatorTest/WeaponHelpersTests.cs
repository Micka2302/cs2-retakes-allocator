using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore;
using RetakesAllocatorCore.Config;

namespace RetakesAllocatorTest;

public class WeaponHelpersTests : BaseTestFixture
{
    [Test]
    [TestCase(true, true, true)]
    [TestCase(true, false, true)]
    [TestCase(false, true, true)]
    [TestCase(false, false, false)]
    public void TestIsWeaponAllocationAllowed(bool allowAfterFreezeTime, bool isFreezeTime, bool expected)
    {
        Configs.OverrideConfigDataForTests(new ConfigData() {AllowAllocationAfterFreezeTime = allowAfterFreezeTime});

        var canAllocate = WeaponHelpers.IsWeaponAllocationAllowed(isFreezeTime);

        Assert.That(canAllocate, Is.EqualTo(expected));
    }

    [Test]
    public void EnableAllWeaponsConfigAllowsCrossTeamOptions()
    {
        Configs.GetConfigData().EnableAllWeaponsForEveryone = true;

        var team = Utils.ParseTeam("CT");
        var weapons = WeaponHelpers.GetPossibleWeaponsForAllocationType(
            WeaponAllocationType.FullBuyPrimary, team);

        var ak47 = WeaponHelpers.FindValidWeaponsByName("ak47").First();
        var m4a1s = WeaponHelpers.FindValidWeaponsByName("m4a1s").First();

        Assert.That(weapons, Does.Contain(ak47));
        Assert.That(weapons, Does.Contain(m4a1s));
    }
}
