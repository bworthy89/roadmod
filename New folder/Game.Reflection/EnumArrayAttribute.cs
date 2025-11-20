using System;

namespace Game.Reflection;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field)]
public class EnumArrayAttribute : Attribute
{
	public Type type { get; set; }

	public EnumArrayAttribute(Type type)
	{
		this.type = type;
	}
}
