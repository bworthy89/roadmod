using System;

namespace Game.Objects;

[Flags]
public enum TransformFlags : uint
{
	MainLights = 1u,
	ExtraLights = 2u,
	TurningLeft = 4u,
	TurningRight = 8u,
	Braking = 0x10u,
	RearLights = 0x20u,
	BoardingLeft = 0x40u,
	BoardingRight = 0x80u,
	InteriorLights = 0x100u,
	Pantograph = 0x200u,
	WarningLights = 0x400u,
	Reversing = 0x800u,
	WorkLights = 0x1000u,
	SignalAnimation1 = 0x2000u,
	SignalAnimation2 = 0x4000u,
	TakingOff = 0x8000u,
	Landing = 0x10000u,
	Flying = 0x20000u
}
