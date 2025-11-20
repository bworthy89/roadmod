using System;
using UnityEngine;

namespace Game.UI.Widgets;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class NumberStepAttribute : PropertyAttribute
{
	public float Step { get; set; }

	public NumberStepAttribute(float step)
	{
		Step = step;
	}
}
