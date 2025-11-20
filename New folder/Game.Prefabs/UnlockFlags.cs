using System;

namespace Game.Prefabs;

[Flags]
public enum UnlockFlags : uint
{
	RequireAll = 1u,
	RequireAny = 2u
}
