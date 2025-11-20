using System;
using Game.Reflection;
using UnityEngine;

namespace Game.UI.Widgets;

public class AnimationCurveFieldBuilders : IFieldBuilderFactory
{
	public FieldBuilder TryCreate(Type memberType, object[] attributes)
	{
		if (memberType == typeof(AnimationCurve))
		{
			return delegate(IValueAccessor accessor)
			{
				if (accessor.GetValue() == null)
				{
					accessor.SetValue(new AnimationCurve());
				}
				return new AnimationCurveField
				{
					accessor = new CastAccessor<AnimationCurve>(accessor)
				};
			};
		}
		return null;
	}
}
