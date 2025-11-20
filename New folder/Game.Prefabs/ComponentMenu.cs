using System;

namespace Game.Prefabs;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ComponentMenu : Attribute
{
	public readonly string menu;

	public readonly Type[] requiredPrefab;

	public ComponentMenu(params Type[] requiredPrefab)
	{
		this.requiredPrefab = requiredPrefab;
	}

	public ComponentMenu(string menu, params Type[] requiredPrefab)
		: this(requiredPrefab)
	{
		this.menu = menu;
	}
}
