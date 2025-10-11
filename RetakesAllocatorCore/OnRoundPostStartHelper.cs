using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore.Config;
using RetakesAllocatorCore.Db;
using RetakesAllocatorCore.Managers;
using System;

namespace RetakesAllocatorCore;

public class OnRoundPostStartHelper
{
    public static void Handle<T>(
        ICollection<T> allPlayers,
        Func<T?, ulong> getSteamId,
        Func<T, CsTeam> getTeam,
        Action<T> giveDefuseKit,
        Action<T, ICollection<CsItem>, string?> allocateItemsForPlayer,
        Func<T, bool> isVip,
        Func<T, bool> hasEnemyStuffPermission,
        out RoundType currentRoundType
    ) where T : notnull
    {
        var roundType = RoundTypeManager.Instance.GetNextRoundType();
        currentRoundType = roundType;

        var tPlayers = new List<T>();
        var ctPlayers = new List<T>();
        var playerIds = new List<ulong>();
        foreach (var player in allPlayers)
        {
            var steamId = getSteamId(player);
            if (steamId != 0)
            {
                playerIds.Add(steamId);
            }

            var playerTeam = getTeam(player);
            if (playerTeam == CsTeam.Terrorist)
            {
                tPlayers.Add(player);
            }
            else if (playerTeam == CsTeam.CounterTerrorist)
            {
                ctPlayers.Add(player);
            }
        }

        Log.Debug($"#T Players: {string.Join(",", tPlayers.Select(getSteamId))}");
        Log.Debug($"#CT Players: {string.Join(",", ctPlayers.Select(getSteamId))}");

        var userSettingsByPlayerId = Queries.GetUsersSettings(playerIds);

        var defusingPlayer = Utils.Choice(ctPlayers);

        HashSet<T> FilterPreferredPlayers(IEnumerable<T> ps, Func<CsItem, bool> preferenceFilter) =>
            ps.Where(p =>
                    userSettingsByPlayerId.TryGetValue(getSteamId(p), out var userSetting) &&
                    userSetting.GetWeaponPreference(getTeam(p), WeaponAllocationType.Preferred) is { } preferredWeapon &&
                    preferenceFilter(preferredWeapon))
                .ToHashSet();

        var tPreferredWeapons = new Dictionary<T, CsItem>();
        var ctPreferredWeapons = new Dictionary<T, CsItem>();

        void AssignPreferredWeapons(
            Dictionary<T, CsItem> preferredWeapons,
            IEnumerable<T> eligiblePlayers,
            Func<IEnumerable<T>, Func<T, bool>, CsTeam, IList<T>> selectPlayers,
            CsTeam team,
            Func<CsItem> randomWeaponFactory
        )
        {
            var selectedPlayers = selectPlayers(eligiblePlayers, isVip, team);
            foreach (var selectedPlayer in selectedPlayers)
            {
                if (preferredWeapons.ContainsKey(selectedPlayer))
                {
                    continue;
                }

                var steamId = getSteamId(selectedPlayer);
                if (!userSettingsByPlayerId.TryGetValue(steamId, out var userSetting))
                {
                    continue;
                }

                var preference =
                    userSetting.GetWeaponPreference(team, WeaponAllocationType.Preferred);
                if (preference is null)
                {
                    continue;
                }

                var weapon = preference.Value;
                if (WeaponHelpers.IsRandomSniperPreference(weapon))
                {
                    weapon = randomWeaponFactory();
                }
                else if (!WeaponHelpers.IsUsableWeapon(weapon))
                {
                    continue;
                }

                preferredWeapons[selectedPlayer] = weapon;
            }
        }

        var config = Configs.GetConfigData();
        var enemyStuffGrantedPerTeam = new Dictionary<CsTeam, int>
        {
            {CsTeam.Terrorist, 0},
            {CsTeam.CounterTerrorist, 0},
        };

        if (roundType == RoundType.FullBuy)
        {
            var random = new Random();

            if (random.NextDouble() * 100 <= config.ChanceForAwpWeapon)
            {
                var tAwpEligible = FilterPreferredPlayers(tPlayers, WeaponHelpers.IsAwpOrAutoSniperPreference);
                var ctAwpEligible = FilterPreferredPlayers(ctPlayers, WeaponHelpers.IsAwpOrAutoSniperPreference);

                AssignPreferredWeapons(
                    tPreferredWeapons,
                    tAwpEligible,
                    WeaponHelpers.SelectPreferredPlayers,
                    CsTeam.Terrorist,
                    () => CsItem.AWP
                );
                AssignPreferredWeapons(
                    ctPreferredWeapons,
                    ctAwpEligible,
                    WeaponHelpers.SelectPreferredPlayers,
                    CsTeam.CounterTerrorist,
                    () => CsItem.AWP
                );
            }

            if (random.NextDouble() * 100 <= config.ChanceForSsgWeapon)
            {
                var tSsgEligible = FilterPreferredPlayers(tPlayers, WeaponHelpers.IsSsgPreference)
                    .Where(player => !tPreferredWeapons.ContainsKey(player));
                var ctSsgEligible = FilterPreferredPlayers(ctPlayers, WeaponHelpers.IsSsgPreference)
                    .Where(player => !ctPreferredWeapons.ContainsKey(player));

                AssignPreferredWeapons(
                    tPreferredWeapons,
                    tSsgEligible,
                    WeaponHelpers.SelectPreferredSsgPlayers,
                    CsTeam.Terrorist,
                    () => CsItem.Scout
                );
                AssignPreferredWeapons(
                    ctPreferredWeapons,
                    ctSsgEligible,
                    WeaponHelpers.SelectPreferredSsgPlayers,
                    CsTeam.CounterTerrorist,
                    () => CsItem.Scout
                );
            }
        }

        var nadesByPlayer = new Dictionary<T, ICollection<CsItem>>();
        NadeHelpers.AllocateNadesToPlayers(
            NadeHelpers.GetUtilForTeam(
                RoundTypeManager.Instance.Map,
                roundType,
                CsTeam.Terrorist,
                tPlayers.Count
            ),
            tPlayers,
            nadesByPlayer
        );
        NadeHelpers.AllocateNadesToPlayers(
            NadeHelpers.GetUtilForTeam(
                RoundTypeManager.Instance.Map,
                roundType,
                CsTeam.CounterTerrorist,
                tPlayers.Count
            ),
            ctPlayers,
            nadesByPlayer
        );

        foreach (var player in allPlayers)
        {
            var team = getTeam(player);
            var playerSteamId = getSteamId(player);
            userSettingsByPlayerId.TryGetValue(playerSteamId, out var userSetting);
            var items = new List<CsItem>
            {
                RoundTypeHelpers.GetArmorForRoundType(roundType),
                team == CsTeam.Terrorist ? CsItem.DefaultKnifeT : CsItem.DefaultKnifeCT,
            };

            CsItem? preferredOverride = team switch
            {
                CsTeam.Terrorist => tPreferredWeapons.TryGetValue(player, out var weapon)
                    ? weapon
                    : (CsItem?)null,
                CsTeam.CounterTerrorist => ctPreferredWeapons.TryGetValue(player, out var weapon)
                    ? weapon
                    : (CsItem?)null,
                _ => null,
            };
            var givePreferred = preferredOverride.HasValue;

            var enemyStuffQuotaAvailable =
                config.EnableEnemyStuffPreference &&
                hasEnemyStuffPermission(player) &&
                userSetting?.EnemyStuffEnabled == true &&
                team is CsTeam.Terrorist or CsTeam.CounterTerrorist &&
                (config.MaxEnemyStuffPerTeam < 0 ||
                 enemyStuffGrantedPerTeam[team] < config.MaxEnemyStuffPerTeam);

            var weaponSelection = WeaponHelpers.GetWeaponsForRoundType(
                roundType,
                team,
                userSetting,
                givePreferred,
                enemyStuffQuotaAvailable,
                preferredOverride
            );
            items.AddRange(weaponSelection.Weapons);

            if (weaponSelection.EnemyStuffGranted && team is CsTeam.Terrorist or CsTeam.CounterTerrorist)
            {
                enemyStuffGrantedPerTeam[team]++;
            }

            if (nadesByPlayer.TryGetValue(player, out var playerNades))
            {
                items.AddRange(playerNades);
            }

            if (team == CsTeam.CounterTerrorist)
            {
                // On non-pistol rounds, everyone gets defuse kit and util
                if (roundType != RoundType.Pistol)
                {
                    giveDefuseKit(player);
                }
                else if (getSteamId(defusingPlayer) == getSteamId(player))
                {
                    // On pistol rounds, only one person gets a defuse kit
                    giveDefuseKit(player);
                }
            }

            if (config.EnableZeusPreference && userSetting?.ZeusEnabled == true)
            {
                items.Add(CsItem.Zeus);
            }

            allocateItemsForPlayer(player, items, team == CsTeam.Terrorist ? "slot5" : "slot1");
        }
    }
}
