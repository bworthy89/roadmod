using System;
using UnityEngine;

namespace Game.UI.Widgets;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class NumberUnitAttribute : PropertyAttribute
{
	public string Unit { get; set; }

	public NumberUnitAttribute(string unit)
	{
		Unit = unit;
	}
}
