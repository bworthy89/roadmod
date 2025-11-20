using System;

namespace Game.Prefabs;

[Flags]
public enum EffectConditionFlags
{
	None = 0,
	Emergency = 1,
	Parked = 2,
	Operational = 4,
	OnFire = 8,
	Extinguishing = 0x10,
	TakingOff = 0x20,
	Landing = 0x40,
	Flying = 0x80,
	Stopped = 0x100,
	Processing = 0x200,
	Boarding = 0x400,
	Disaster = 0x800,
	Occurring = 0x1000,
	Night = 0x2000,
	Cold = 0x4000,
	LightsOff = 0x8000,
	MainLights = 0x10000,
	ExtraLights = 0x20000,
	WarningLights = 0x40000,
	WorkLights = 0x80000,
	Spillway = 0x100000,
	Collapsing = 0x200000,
	MoveableBridgeWorking = 0x400000,
	Last = 0x400000
}
