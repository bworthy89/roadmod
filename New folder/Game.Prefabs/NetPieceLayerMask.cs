using System;

namespace Game.Prefabs;

[Flags]
public enum NetPieceLayerMask
{
	Surface = 1,
	Bottom = 2,
	Top = 4,
	Side = 8
}
