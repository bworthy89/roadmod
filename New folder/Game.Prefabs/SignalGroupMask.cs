using System;

namespace Game.Prefabs;

[Flags]
public enum SignalGroupMask : ushort
{
	SignalGroup1 = 1,
	SignalGroup2 = 2,
	SignalGroup3 = 4,
	SignalGroup4 = 8,
	SignalGroup5 = 0x10,
	SignalGroup6 = 0x20,
	SignalGroup7 = 0x40,
	SignalGroup8 = 0x80,
	SignalGroup9 = 0x100,
	SignalGroup10 = 0x200,
	SignalGroup11 = 0x400
}
