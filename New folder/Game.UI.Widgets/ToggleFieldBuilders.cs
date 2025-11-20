using System;

namespace Game.UI.Widgets;

public class ToggleFieldBuilders : IFieldBuilderFactory
{
	public FieldBuilder TryCreate(Type memberType, object[] attributes)
	{
		if (memberType == typeof(bool))
		{
			return WidgetReflectionUtils.CreateFieldBuilder<ToggleField, bool>();
		}
		return null;
	}
}
