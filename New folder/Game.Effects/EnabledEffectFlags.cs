using System;

namespace Game.Effects;

[Flags]
public enum EnabledEffectFlags : uint
{
	IsEnabled = 1u,
	EnabledUpdated = 2u,
	Deleted = 4u,
	IsLight = 8u,
	IsVFX = 0x10u,
	IsAudio = 0x20u,
	AudioDisabled = 0x40u,
	EditorContainer = 0x80u,
	RandomTransform = 0x100u,
	TempOwner = 0x200u,
	DynamicTransform = 0x400u,
	RandomColor = 0x800u,
	OwnerUpdated = 0x1000u,
	OwnerCollapsed = 0x2000u,
	WrongPrefab = 0x4000u
}
