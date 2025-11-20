using System;

namespace Game.Creatures;

[Flags]
public enum HumanFlags : uint
{
	Run = 1u,
	Selfies = 2u,
	Emergency = 4u,
	Dead = 8u,
	Carried = 0x10u,
	Cold = 0x20u,
	Homeless = 0x40u,
	Waiting = 0x80u,
	Sad = 0x100u,
	Happy = 0x200u,
	Angry = 0x400u,
	Collapsed = 0x800u
}
