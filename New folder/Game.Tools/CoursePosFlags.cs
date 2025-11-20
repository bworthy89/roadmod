using System;

namespace Game.Tools;

[Flags]
public enum CoursePosFlags : uint
{
	IsFirst = 1u,
	IsLast = 2u,
	HalfAlign = 4u,
	IsParallel = 8u,
	IsRight = 0x10u,
	IsLeft = 0x20u,
	IsFixed = 0x40u,
	FreeHeight = 0x80u,
	LeftTransition = 0x100u,
	RightTransition = 0x200u,
	ForceElevatedNode = 0x400u,
	ForceElevatedEdge = 0x800u,
	DisableMerge = 0x1000u,
	IsGrid = 0x2000u,
	DontCreate = 0x4000u
}
