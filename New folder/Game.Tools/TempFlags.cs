using System;

namespace Game.Tools;

[Flags]
public enum TempFlags : uint
{
	Create = 1u,
	Delete = 2u,
	IsLast = 4u,
	Essential = 8u,
	Dragging = 0x10u,
	Select = 0x20u,
	Modify = 0x40u,
	Regenerate = 0x80u,
	Replace = 0x100u,
	Upgrade = 0x200u,
	Hidden = 0x400u,
	Parent = 0x800u,
	Combine = 0x1000u,
	RemoveCost = 0x2000u,
	Optional = 0x4000u,
	Cancel = 0x8000u,
	SubDetail = 0x10000u,
	Duplicate = 0x20000u
}
