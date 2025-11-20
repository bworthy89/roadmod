using System;
using Colossal.Annotations;
using UnityEngine;

namespace Game.UI.Widgets;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class ElementCustomFieldAttribute : PropertyAttribute
{
	[NotNull]
	public Type Factory { get; set; }

	public ElementCustomFieldAttribute([NotNull] Type factory)
	{
		Factory = factory ?? throw new ArgumentNullException("factory");
	}
}
