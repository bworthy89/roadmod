using System;

namespace Game.Rendering;

[Flags]
public enum BatchFlags
{
	MotionVectors = 1,
	Animated = 2,
	Bones = 4,
	Emissive = 8,
	ColorMask = 0x10,
	Outline = 0x20,
	InfoviewColor = 0x40,
	Lod = 0x80,
	Node = 0x100,
	Roundabout = 0x200,
	Extended1 = 0x400,
	Extended2 = 0x800,
	Extended3 = 0x1000,
	LodFade = 0x2000,
	InfoviewFlow = 0x4000,
	Hanging = 0x8000,
	BlendWeights = 0x10000,
	SurfaceState = 0x20000,
	Base = 0x40000,
	CullVertices = 0x80000,
	Overlay = 0x100000
}
