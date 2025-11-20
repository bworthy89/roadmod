using System;

namespace Game.Areas;

[Flags]
public enum AreaTypeMask
{
	None = 0,
	Lots = 1,
	Districts = 2,
	MapTiles = 4,
	Spaces = 8,
	Surfaces = 0x10
}
