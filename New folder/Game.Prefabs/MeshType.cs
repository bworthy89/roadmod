using System;

namespace Game.Prefabs;

[Flags]
public enum MeshType : ushort
{
	Object = 1,
	Net = 2,
	Lane = 4,
	Zone = 8,
	First = 1,
	Last = 8
}
