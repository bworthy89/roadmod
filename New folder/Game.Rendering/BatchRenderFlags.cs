using System;

namespace Game.Rendering;

[Flags]
public enum BatchRenderFlags : byte
{
	MotionVectors = 1,
	ReceiveShadows = 2,
	CastShadows = 4,
	IsEnabled = 8,
	All = byte.MaxValue
}
