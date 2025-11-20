using System;

namespace Game.Prefabs;

[Flags]
public enum MeshLayer : ushort
{
	Default = 1,
	Moving = 2,
	Tunnel = 4,
	Pipeline = 8,
	SubPipeline = 0x10,
	Waterway = 0x20,
	Outline = 0x40,
	Marker = 0x80,
	First = 1,
	Last = 0x80
}
