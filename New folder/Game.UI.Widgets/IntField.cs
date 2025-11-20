using System;
using Colossal.UI.Binding;

namespace Game.UI.Widgets;

public abstract class IntField<T> : Field<T>
{
	public int min { get; set; } = int.MinValue;

	public int max { get; set; } = int.MaxValue;

	public int step { get; set; } = 1;

	public int stepMultiplier { get; set; } = 10;

	public Func<int> dynamicMin { get; set; }

	public Func<int> dynamicMax { get; set; }

	protected override WidgetChanges Update()
	{
		WidgetChanges widgetChanges = base.Update();
		if (dynamicMin != null)
		{
			int num = dynamicMin();
			if (num != min)
			{
				widgetChanges |= WidgetChanges.Properties;
				min = num;
			}
		}
		if (dynamicMax != null)
		{
			int num2 = dynamicMax();
			if (num2 != max)
			{
				widgetChanges |= WidgetChanges.Properties;
				max = num2;
			}
		}
		return widgetChanges;
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("min");
		writer.Write(min);
		writer.PropertyName("max");
		writer.Write(max);
		writer.PropertyName("step");
		writer.Write(step);
		writer.PropertyName("stepMultiplier");
		writer.Write(stepMultiplier);
	}
}
