using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;
using KitsuneMenu;
using KitsuneMenu.Core;
using KitsuneMenu.Core.Enums;
using KitsuneMenu.Core.MenuItems;
using RetakesAllocator;
using RetakesAllocatorCore;
using RetakesAllocatorCore.Config;
using RetakesAllocatorCore.Db;

namespace RetakesAllocator.AdvancedMenus;

public class AdvancedGunMenu
{
    private readonly ConcurrentDictionary<ulong, CsItem> _lastPreferredSnipers = new();

    public HookResult OnEventPlayerChat(EventPlayerChat @event, GameEventInfo info)
    {
        if (@event == null)
        {
            return HookResult.Continue;
        }

        var player = Utilities.GetPlayerFromUserid(@event.Userid);
        if (!Helpers.PlayerIsValid(player))
        {
            return HookResult.Continue;
        }

        var message = (@event.Text ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(message))
        {
            return HookResult.Continue;
        }

        var commands = Configs.GetConfigData().InGameGunMenuCenterCommands.Split(',');
        if (commands.Any(cmd => cmd.Equals(message, StringComparison.OrdinalIgnoreCase)))
        {
            _ = OpenMenuForPlayerAsync(player!);
        }

        return HookResult.Continue;
    }

    public void OnTick()
    {
        // Menu updates are handled by the Kitsune menu framework.
    }

    public HookResult OnEventPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        if (@event?.Userid == null)
        {
            return HookResult.Continue;
        }

        var player = @event.Userid;
        if (!Helpers.PlayerIsValid(player))
        {
            return HookResult.Continue;
        }

        var steamId = Helpers.GetSteamId(player);
        if (steamId != 0)
        {
            _lastPreferredSnipers.TryRemove(steamId, out _);
        }

        KitsuneMenu.KitsuneMenu.CloseMenu(player);
        return HookResult.Continue;
    }

    private async Task OpenMenuForPlayerAsync(CCSPlayerController player)
    {
        if (!Helpers.PlayerIsValid(player))
        {
            return;
        }

        if (!Configs.GetConfigData().CanPlayersSelectWeapons())
        {
            Helpers.WriteNewlineDelimited(Translator.Instance["weapon_preference.cannot_choose"], player.PrintToChat);
            return;
        }

        var team = Helpers.GetTeam(player);
        if (team is not CsTeam.Terrorist and not CsTeam.CounterTerrorist)
        {
            Helpers.WriteNewlineDelimited(Translator.Instance["weapon_preference.join_team"], player.PrintToChat);
            return;
        }

        var steamId = Helpers.GetSteamId(player);
        if (steamId == 0)
        {
            Helpers.WriteNewlineDelimited(Translator.Instance["guns_menu.invalid_steam_id"], player.PrintToChat);
            return;
        }

        var data = await BuildMenuDataAsync(team, steamId);
        if (data == null)
        {
            Helpers.WriteNewlineDelimited(Translator.Instance["weapon_preference.not_saved"], player.PrintToChat);
            return;
        }

        ShowMenu(player, data);
    }

    private async Task<GunMenuData?> BuildMenuDataAsync(CsTeam team, ulong steamId)
    {
        var userSettings = await Queries.GetUserSettings(steamId);
        var primaryOptions = WeaponHelpers
            .GetPossibleWeaponsForAllocationType(WeaponAllocationType.FullBuyPrimary, team)
            .ToList();
        var secondaryOptions = WeaponHelpers
            .GetPossibleWeaponsForAllocationType(WeaponAllocationType.Secondary, team)
            .ToList();

        var currentPrimary = userSettings?.GetWeaponPreference(team, WeaponAllocationType.FullBuyPrimary) ??
                             GetDefaultWeapon(team, WeaponAllocationType.FullBuyPrimary, primaryOptions);
        var currentSecondary = userSettings?.GetWeaponPreference(team, WeaponAllocationType.Secondary) ??
                               GetDefaultWeapon(team, WeaponAllocationType.Secondary, secondaryOptions);

        var preferredSniper = userSettings?.GetWeaponPreference(team, WeaponAllocationType.Preferred);
        if (preferredSniper.HasValue)
        {
            _lastPreferredSnipers[steamId] = preferredSniper.Value;
        }

        return new GunMenuData
        {
            SteamId = steamId,
            Team = team,
            PrimaryOptions = primaryOptions,
            SecondaryOptions = secondaryOptions,
            CurrentPrimary = currentPrimary,
            CurrentSecondary = currentSecondary,
            PreferredSniper = preferredSniper
        };
    }

    private void ShowMenu(CCSPlayerController player, GunMenuData data)
    {
        var teamDisplayName = GetTeamDisplayName(data.Team);
        var menuTitle = Translator.Instance["guns_menu.title", teamDisplayName];

        var menuBuilder = KitsuneMenu.KitsuneMenu.Create(menuTitle)
            .MaxVisibleItems(4)
            .NoFreeze();

        var primaryNames = data.PrimaryOptions.Select(static weapon => weapon.GetName()).ToArray();
        if (primaryNames.Length > 0)
        {
            var defaultPrimary = data.CurrentPrimary?.GetName() ?? primaryNames[0];
            menuBuilder.AddChoice(Translator.Instance["weapon_type.primary"], primaryNames, defaultPrimary,
                (ply, choice) => HandlePrimaryChoice(ply, data, choice), MenuTextSize.Large);
        }
        else
        {
            menuBuilder.AddText($"{Translator.Instance["weapon_type.primary"]}: {Translator.Instance["guns_menu.unavailable"]}",
                TextAlign.Left, MenuTextSize.Medium);
        }

        var secondaryNames = data.SecondaryOptions.Select(static weapon => weapon.GetName()).ToArray();
        if (secondaryNames.Length > 0)
        {
            var defaultSecondary = data.CurrentSecondary?.GetName() ?? secondaryNames[0];
            menuBuilder.AddChoice(Translator.Instance["weapon_type.secondary"], secondaryNames, defaultSecondary,
                (ply, choice) => HandleSecondaryChoice(ply, data, choice), MenuTextSize.Large);
        }
        else
        {
            menuBuilder.AddText($"{Translator.Instance["weapon_type.secondary"]}: {Translator.Instance["guns_menu.unavailable"]}",
                TextAlign.Left, MenuTextSize.Medium);
        }

        var awpLabel = Translator.Instance["guns_menu.awp_label"];
        var awpEnabled = data.PreferredSniper.HasValue;
        var awpChoices = new[]
        {
            Translator.Instance["guns_menu.awp_enabled"],
            Translator.Instance["guns_menu.awp_disabled"]
        };
        var defaultAwpChoice = awpEnabled ? awpChoices[0] : awpChoices[1];
        menuBuilder.AddChoice(awpLabel, awpChoices, defaultAwpChoice,
            (ply, choice) => HandleAwpChoice(ply, data, choice == awpChoices[0]), MenuTextSize.Large);

        menuBuilder.AddSeparator();
        menuBuilder.AddButton(Translator.Instance["menu.exit"], ply => KitsuneMenu.KitsuneMenu.CloseMenu(ply));
        var menu = menuBuilder.Build();
        menu.Show(player);
    }

    private void HandlePrimaryChoice(CCSPlayerController player, GunMenuData data, string choice)
    {
        var weapon = FindWeaponByName(data.PrimaryOptions, choice);
        if (weapon == null)
        {
            return;
        }

        data.CurrentPrimary = weapon;
        ApplyWeaponSelection(player, data.SteamId, data.Team, RoundType.FullBuy, weapon.Value);
    }

    private void HandleSecondaryChoice(CCSPlayerController player, GunMenuData data, string choice)
    {
        var weapon = FindWeaponByName(data.SecondaryOptions, choice);
        if (weapon == null)
        {
            return;
        }

        data.CurrentSecondary = weapon;
        ApplyWeaponSelection(player, data.SteamId, data.Team, RoundType.FullBuy, weapon.Value);
    }

    private void HandleAwpChoice(CCSPlayerController player, GunMenuData data, bool enabled)
    {
        ApplyAwpPreference(player, data, enabled);
    }

    private void ApplyWeaponSelection(CCSPlayerController player, ulong steamId, CsTeam team,
        RoundType roundType, CsItem weapon)
    {
        var weaponName = weapon.GetName();
        _ = Task.Run(async () =>
        {
            var result = await OnWeaponCommandHelper.HandleAsync(new[] { weaponName }, steamId, roundType, team, false);
            if (string.IsNullOrWhiteSpace(result.Item1))
            {
                return;
            }

            Server.NextFrame(() =>
            {
                if (!Helpers.PlayerIsValid(player))
                {
                    return;
                }

                Helpers.WriteNewlineDelimited(result.Item1, player.PrintToChat);
            });
        });
    }

    private void ApplyAwpPreference(CCSPlayerController player, GunMenuData data, bool enabled)
    {
        var steamId = data.SteamId;
        _ = Task.Run(async () =>
        {
            CsItem? itemToSet = null;
            CsItem selectedReference;

            if (enabled)
            {
                if (!_lastPreferredSnipers.TryGetValue(steamId, out var stored))
                {
                    stored = data.PreferredSniper ?? CsItem.AWP;
                }

                itemToSet = stored;
                data.PreferredSniper = stored;
                _lastPreferredSnipers[steamId] = stored;
                selectedReference = stored;
            }
            else
            {
                selectedReference = data.PreferredSniper ?? CsItem.AWP;
                if (data.PreferredSniper.HasValue)
                {
                    _lastPreferredSnipers[steamId] = data.PreferredSniper.Value;
                }

                data.PreferredSniper = null;
            }

            await Queries.SetAwpWeaponPreferenceAsync(steamId, itemToSet);

            var message = enabled
                ? Translator.Instance["weapon_preference.set_preference_preferred", selectedReference]
                : Translator.Instance["weapon_preference.unset_preference_preferred", selectedReference.GetName()];

            Server.NextFrame(() =>
            {
                if (!Helpers.PlayerIsValid(player))
                {
                    return;
                }

                Helpers.WriteNewlineDelimited(message, player.PrintToChat);
            });
        });
    }

    private static CsItem? GetDefaultWeapon(CsTeam team, WeaponAllocationType type, IReadOnlyList<CsItem> fallback)
    {
        if (Configs.GetConfigData().DefaultWeapons.TryGetValue(team, out var defaults) &&
            defaults.TryGetValue(type, out var configured))
        {
            return configured;
        }

        return fallback.Count > 0 ? fallback[0] : null;
    }

    private static CsItem? FindWeaponByName(IEnumerable<CsItem> items, string choice)
    {
        return items.FirstOrDefault(item => item.GetName().Equals(choice, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetTeamDisplayName(CsTeam team)
    {
        return team == CsTeam.Terrorist
            ? Translator.Instance["teams.terrorist"]
            : Translator.Instance["teams.counter_terrorist"];
    }

    private sealed class GunMenuData
    {
        public required ulong SteamId { get; init; }
        public required CsTeam Team { get; init; }
        public required List<CsItem> PrimaryOptions { get; init; }
        public required List<CsItem> SecondaryOptions { get; init; }
        public CsItem? CurrentPrimary { get; set; }
        public CsItem? CurrentSecondary { get; set; }
        public CsItem? PreferredSniper { get; set; }
    }
}







