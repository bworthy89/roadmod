using System;
using Colossal.Mathematics;

namespace Game.UI.Widgets;

public class BezierFieldBuilders : IFieldBuilderFactory
{
	public FieldBuilder TryCreate(Type memberType, object[] attributes)
	{
		if (memberType == typeof(Bezier4x3))
		{
			return WidgetReflectionUtils.CreateFieldBuilder<Bezier4x3Field, Bezier4x3>();
		}
		return null;
	}
}
