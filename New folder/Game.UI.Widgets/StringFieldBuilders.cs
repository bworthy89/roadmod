using System;

namespace Game.UI.Widgets;

public class StringFieldBuilders : IFieldBuilderFactory
{
	public FieldBuilder TryCreate(Type memberType, object[] attributes)
	{
		if (memberType == typeof(string))
		{
			return WidgetReflectionUtils.CreateFieldBuilder<StringInputField, string>();
		}
		return null;
	}
}
