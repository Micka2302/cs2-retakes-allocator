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

        var tPreferredPlayers = new HashSet<T>();
        var ctPreferredPlayers = new HashSet<T>();

        if (roundType == RoundType.FullBuy)
        {
            var config = Configs.GetConfigData();
            var random = new Random();

            if (random.NextDouble() * 100 <= config.ChanceForAwpWeapon)
            {
                var tAwpEligible = FilterPreferredPlayers(tPlayers, WeaponHelpers.IsAwpOrAutoSniperPreference);
                var ctAwpEligible = FilterPreferredPlayers(ctPlayers, WeaponHelpers.IsAwpOrAutoSniperPreference);

                tPreferredPlayers.UnionWith(
                    WeaponHelpers.SelectPreferredPlayers(tAwpEligible, isVip, CsTeam.Terrorist)
                );
                ctPreferredPlayers.UnionWith(
                    WeaponHelpers.SelectPreferredPlayers(ctAwpEligible, isVip, CsTeam.CounterTerrorist)
                );
            }

            if (random.NextDouble() * 100 <= config.ChanceForSsgWeapon)
            {
                var tSsgEligible = FilterPreferredPlayers(tPlayers, WeaponHelpers.IsSsgPreference);
                var ctSsgEligible = FilterPreferredPlayers(ctPlayers, WeaponHelpers.IsSsgPreference);

                tPreferredPlayers.UnionWith(
                    WeaponHelpers.SelectPreferredSsgPlayers(tSsgEligible, isVip, CsTeam.Terrorist)
                );
                ctPreferredPlayers.UnionWith(
                    WeaponHelpers.SelectPreferredSsgPlayers(ctSsgEligible, isVip, CsTeam.CounterTerrorist)
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

            var givePreferred = team switch
            {
                CsTeam.Terrorist => tPreferredPlayers.Contains(player),
                CsTeam.CounterTerrorist => ctPreferredPlayers.Contains(player),
                _ => false,
            };

            items.AddRange(
                WeaponHelpers.GetWeaponsForRoundType(
                    roundType,
                    team,
                    userSetting,
                    givePreferred
                )
            );

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

            if (Configs.GetConfigData().EnableZeusPreference && userSetting?.ZeusEnabled == true)
            {
                items.Add(CsItem.Zeus);
            }

            allocateItemsForPlayer(player, items, team == CsTeam.Terrorist ? "slot5" : "slot1");
        }
    }
}
