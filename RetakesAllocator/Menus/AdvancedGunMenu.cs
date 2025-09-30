using System.Threading.Tasks;
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

        Server.NextFrame(() =>
        {
            if (!Helpers.PlayerIsValid(player))
            {
                return;
            }

            ShowMenu(player, data);
        });
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
        var pistolOptions = WeaponHelpers
            .GetPossibleWeaponsForAllocationType(WeaponAllocationType.PistolRound, team)
            .ToList();

        var currentPrimary = userSettings?.GetWeaponPreference(team, WeaponAllocationType.FullBuyPrimary) ??
                             GetDefaultWeapon(team, WeaponAllocationType.FullBuyPrimary, primaryOptions);
        var currentSecondary = userSettings?.GetWeaponPreference(team, WeaponAllocationType.Secondary) ??
                               GetDefaultWeapon(team, WeaponAllocationType.Secondary, secondaryOptions);
        var currentPistol = userSettings?.GetWeaponPreference(team, WeaponAllocationType.PistolRound) ??
                            GetDefaultWeapon(team, WeaponAllocationType.PistolRound, pistolOptions);

        var preferredSniper = userSettings?.GetWeaponPreference(team, WeaponAllocationType.Preferred);

        return new GunMenuData
        {
            SteamId = steamId,
            Team = team,
            PrimaryOptions = primaryOptions,
            SecondaryOptions = secondaryOptions,
            PistolOptions = pistolOptions,
            CurrentPrimary = currentPrimary,
            CurrentSecondary = currentSecondary,
            CurrentPistol = currentPistol,
            PreferredSniper = preferredSniper,
            ZeusEnabled = userSettings?.ZeusEnabled ?? false
        };
    }

    private void ShowMenu(CCSPlayerController player, GunMenuData data)
    {
        var teamDisplayName = GetTeamDisplayName(data.Team);
        var menuTitle = Translator.Instance["guns_menu.title", teamDisplayName];

        var config = Configs.GetConfigData();
        var isVip = Helpers.IsVip(player);
        var visibleItems = 3;
        if (isVip)
        {
            visibleItems++;
        }
        if (config.EnableZeusPreference)
        {
            visibleItems++;
        }
        var menuBuilder = KitsuneMenu.KitsuneMenu.Create(menuTitle)
            .MaxVisibleItems(System.Math.Max(visibleItems, 4));

        if (GlobalMenuManager.Config.FreezePlayer)
        {
            menuBuilder.ForceFreeze();
        }
        else
        {
            menuBuilder.NoFreeze();
        }

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

        var pistolNames = data.PistolOptions.Select(static weapon => weapon.GetName()).ToArray();
        if (pistolNames.Length > 0)
        {
            var defaultPistol = data.CurrentPistol?.GetName() ?? pistolNames[0];
            menuBuilder.AddChoice(Translator.Instance["weapon_type.pistol"], pistolNames, defaultPistol,
                (ply, choice) => HandlePistolChoice(ply, data, choice), MenuTextSize.Large);
        }
        else
        {
            menuBuilder.AddText($"{Translator.Instance["weapon_type.pistol"]}: {Translator.Instance["guns_menu.unavailable"]}",
                TextAlign.Left, MenuTextSize.Medium);
        }

        if (isVip)
        {
            var sniperLabel = Translator.Instance["guns_menu.sniper_label"];
            var sniperChoices = new[]
            {
                Translator.Instance["guns_menu.sniper_awp"],
                Translator.Instance["guns_menu.sniper_ssg"],
                Translator.Instance["guns_menu.sniper_random"],
                Translator.Instance["guns_menu.sniper_disabled"]
            };

            var defaultSniperChoice = sniperChoices[3];
            if (data.PreferredSniper is { } preferredSniper)
            {
                if (WeaponHelpers.IsRandomSniperPreference(preferredSniper))
                {
                    defaultSniperChoice = sniperChoices[2];
                }
                else
                {
                    defaultSniperChoice = preferredSniper switch
                    {
                        CsItem.Scout => sniperChoices[1],
                        _ => sniperChoices[0]
                    };
                }
            }

            menuBuilder.AddChoice(sniperLabel, sniperChoices, defaultSniperChoice,
                (ply, choice) => HandleSniperChoice(ply, data, choice, sniperChoices), MenuTextSize.Large);
        }
        if (config.EnableZeusPreference)
        {
            var zeusChoices = new[]
            {
                Translator.Instance["guns_menu.zeus_choice_disable"],
                Translator.Instance["guns_menu.zeus_choice_enable"]
            };

            var defaultZeusChoice = data.ZeusEnabled ? zeusChoices[1] : zeusChoices[0];

            menuBuilder.AddChoice(
                Translator.Instance["guns_menu.zeus_label"],
                zeusChoices,
                defaultZeusChoice,
                (ply, choice) => HandleZeusChoice(ply, data, choice, zeusChoices),
                MenuTextSize.Large);
        }
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

    private void HandlePistolChoice(CCSPlayerController player, GunMenuData data, string choice)
    {
        var weapon = FindWeaponByName(data.PistolOptions, choice);
        if (weapon == null)
        {
            return;
        }

        data.CurrentPistol = weapon;
        ApplyWeaponSelection(player, data.SteamId, data.Team, RoundType.Pistol, weapon.Value);
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

    private void HandleSniperChoice(CCSPlayerController player, GunMenuData data, string choice, IReadOnlyList<string> options)
    {
        if (choice == options[0])
        {
            ApplySniperPreference(player, data, CsItem.AWP);
        }
        else if (choice == options[1])
        {
            ApplySniperPreference(player, data, CsItem.Scout);
        }
        else if (choice == options[2])
        {
            ApplySniperPreference(player, data, WeaponHelpers.RandomSniperPreference);
        }
        else
        {
            ApplySniperPreference(player, data, null);
        }
    }
    private void HandleZeusChoice(CCSPlayerController player, GunMenuData data, string choice, IReadOnlyList<string> options)
    {
        var enabled = choice == options[1];
        if (data.ZeusEnabled == enabled)
        {
            return;
        }

        data.ZeusEnabled = enabled;
        Queries.SetZeusPreference(data.SteamId, enabled);

        var messageKey = enabled ? "guns_menu.zeus_enabled_message" : "guns_menu.zeus_disabled_message";
        Helpers.WriteNewlineDelimited(Translator.Instance[messageKey], player.PrintToChat);
    }
    private void ApplySniperPreference(CCSPlayerController player, GunMenuData data, CsItem? preference)
    {
        var steamId = data.SteamId;
        var previousPreference = data.PreferredSniper;
        data.PreferredSniper = preference;

        _ = Task.Run(async () =>
        {
            await Queries.SetAwpWeaponPreferenceAsync(steamId, preference);

            string message;
            if (preference.HasValue)
            {
                message = WeaponHelpers.IsRandomSniperPreference(preference.Value)
                    ? Translator.Instance["weapon_preference.set_preference_preferred_random"]
                    : Translator.Instance["weapon_preference.set_preference_preferred", preference.Value];
            }
            else
            {
                message = previousPreference.HasValue && WeaponHelpers.IsRandomSniperPreference(previousPreference.Value)
                    ? Translator.Instance["weapon_preference.unset_preference_preferred_random"]
                    : Translator.Instance["weapon_preference.unset_preference_preferred", previousPreference ?? CsItem.AWP];
            }

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
        public required List<CsItem> PistolOptions { get; init; }
        public CsItem? CurrentPrimary { get; set; }
        public CsItem? CurrentSecondary { get; set; }
        public CsItem? CurrentPistol { get; set; }
        public CsItem? PreferredSniper { get; set; }
        public bool ZeusEnabled { get; set; }
    }
}











