using System;

namespace Game.Tools;

[Flags]
public enum CreationFlags : uint
{
	Permanent = 1u,
	Select = 2u,
	Delete = 4u,
	Attach = 8u,
	Upgrade = 0x10u,
	Relocate = 0x20u,
	Invert = 0x40u,
	Align = 0x80u,
	Hidden = 0x100u,
	Parent = 0x200u,
	Dragging = 0x400u,
	Recreate = 0x800u,
	Optional = 0x1000u,
	Lowered = 0x2000u,
	Native = 0x4000u,
	Construction = 0x8000u,
	SubElevation = 0x10000u,
	Duplicate = 0x20000u,
	Repair = 0x40000u,
	Stamping = 0x80000u
}
