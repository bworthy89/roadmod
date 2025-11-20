using System;
using Colossal.Annotations;
using Colossal.UI.Binding;
using Unity.Mathematics;

namespace Game.UI.Widgets;

public abstract class FloatSliderField<T> : FloatField<T>
{
	[CanBeNull]
	public string unit { get; set; }

	public bool signed { get; set; }

	public bool separateThousands { get; set; }

	public double maxValueWithFraction { get; set; }

	public bool scaleDragVolume { get; set; }

	public bool updateOnDragEnd { get; set; }

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("unit");
		writer.Write(unit);
		writer.PropertyName("signed");
		writer.Write(signed);
		writer.PropertyName("separateThousands");
		writer.Write(separateThousands);
		writer.PropertyName("maxValueWithFraction");
		writer.Write(maxValueWithFraction);
		writer.PropertyName("scaleDragVolume");
		writer.Write(scaleDragVolume);
		writer.PropertyName("updateOnDragEnd");
		writer.Write(updateOnDragEnd);
	}
}
public class FloatSliderField : FloatSliderField<double>, IWarning
{
	private bool m_Warning;

	[CanBeNull]
	public Func<bool> warningAction { get; set; }

	protected override double defaultMin => double.MinValue;

	protected override double defaultMax => double.MaxValue;

	public bool warning
	{
		get
		{
			return m_Warning;
		}
		set
		{
			warningAction = null;
			m_Warning = value;
		}
	}

	public override double ToFieldType(double4 value)
	{
		return value.x;
	}

	protected override WidgetChanges Update()
	{
		WidgetChanges widgetChanges = base.Update();
		if (warningAction != null)
		{
			bool flag = warningAction();
			if (flag != m_Warning)
			{
				m_Warning = flag;
				widgetChanges |= WidgetChanges.Properties;
			}
		}
		return widgetChanges;
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("warning");
		writer.Write(warning);
	}
}
