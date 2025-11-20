using System;

namespace Game.Prefabs;

[Flags]
public enum ObjectRequirementType : ushort
{
	SelectOnly = 1,
	IgnoreExplicit = 2
}
