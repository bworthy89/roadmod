using System;

namespace Game.Common;

[Flags]
public enum BoundsMask : ushort
{
	Debug = 1,
	NormalLayers = 2,
	PipelineLayer = 4,
	SubPipelineLayer = 8,
	WaterwayLayer = 0x10,
	IsTree = 0x20,
	OccupyZone = 0x40,
	NotOverridden = 0x80,
	NotWalkThrough = 0x100,
	HasLot = 0x200,
	AllLayers = 0x1E
}
