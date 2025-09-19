using CounterStrikeSharp.API.Modules.Entities.Constants;
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
    [TestCase("weapon_ssg08")]
    [TestCase("ssg08")]
    public void FindValidWeaponsByName_FindsScoutAliases(string alias)
    {
        var results = WeaponHelpers.FindValidWeaponsByName(alias);

        Assert.That(results, Is.EquivalentTo(new[] {CsItem.Scout}));
    }
}
