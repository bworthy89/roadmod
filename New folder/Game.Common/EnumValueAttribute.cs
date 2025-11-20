using System;
using UnityEngine;

namespace Game.Common;

public class EnumValueAttribute : PropertyAttribute
{
	public string[] names;

	public EnumValueAttribute(Type type)
	{
		names = Enum.GetNames(type);
	}
}
