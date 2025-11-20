using System;

namespace Game.Prefabs;

[Flags]
public enum ContentFlags : uint
{
	RequireDlc = 1u,
	RequirePdxLogin = 2u
}
