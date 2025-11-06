using System;

namespace RetakesAllocatorCore.Config;

[Flags]
public enum EnemyStuffTeamPreference
{
    None = 0,
    Terrorist = 1 << 0,
    CounterTerrorist = 1 << 1,
    Both = Terrorist | CounterTerrorist,
}
