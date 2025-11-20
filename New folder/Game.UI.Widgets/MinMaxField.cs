using System;
using System.Collections.Generic;
using Colossal.UI.Binding;
using Unity.Mathematics;

namespace Game.UI.Widgets;

public abstract class MinMaxField<T> : Field<T>
{
	protected abstract T defaultMin { get; }

	protected abstract T defaultMax { get; }

	public Func<T> dynamicMin { get; set; }

	public Func<T> dynamicMax { get; set; }

	public T min { get; set; }

	public T max { get; set; }

	protected MinMaxField()
	{
		min = defaultMin;
		max = defaultMax;
	}

	public virtual bool IsEqual(T x, T y)
	{
		return EqualityComparer<T>.Default.Equals(x, y);
	}

	public abstract T ToFieldType(double4 value);

	protected override WidgetChanges Update()
	{
		WidgetChanges widgetChanges = base.Update();
		if (dynamicMin != null)
		{
			T x = dynamicMin();
			if (!IsEqual(x, min))
			{
				widgetChanges |= WidgetChanges.Properties;
				min = x;
			}
		}
		if (dynamicMax != null)
		{
			T x2 = dynamicMax();
			if (!IsEqual(x2, max))
			{
				widgetChanges |= WidgetChanges.Properties;
				max = x2;
			}
		}
		return widgetChanges;
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("min");
		base.valueWriter.Write(writer, min);
		writer.PropertyName("max");
		base.valueWriter.Write(writer, max);
	}
}
