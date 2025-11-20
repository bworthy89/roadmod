using System;

namespace Game.Common;

[Flags]
public enum CollisionMask
{
	OnGround = 1,
	Overground = 2,
	Underground = 4,
	ExclusiveGround = 8
}
