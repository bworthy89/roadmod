using System;
using System.Collections.Generic;
using Colossal.Annotations;
using Colossal.UI.Binding;
using Game.Reflection;

namespace Game.UI.Widgets;

public abstract class ReadonlyField<T> : NamedWidgetWithTooltip, IDisableCallback
{
	protected const string kValue = "value";

	protected T m_Value;

	private IWriter<T> m_ValueWriter;

	private int m_ValueVersion = -1;

	public ITypedValueAccessor<T> accessor { get; set; }

	[CanBeNull]
	public Func<int> valueVersion { get; set; }

	protected IWriter<T> valueWriter
	{
		get
		{
			return m_ValueWriter ?? (m_ValueWriter = ValueWriters.Create<T>());
		}
		set
		{
			m_ValueWriter = value;
		}
	}

	public virtual T GetValue()
	{
		return accessor.GetTypedValue();
	}

	protected override WidgetChanges Update()
	{
		WidgetChanges widgetChanges = base.Update();
		if (valueVersion != null)
		{
			int num = valueVersion();
			if (num != m_ValueVersion)
			{
				widgetChanges |= WidgetChanges.Properties;
				m_ValueVersion = num;
				m_Value = GetValue();
			}
		}
		else
		{
			T value = GetValue();
			if (!ValueEquals(value, m_Value))
			{
				widgetChanges |= WidgetChanges.Properties;
				m_Value = value;
			}
		}
		return widgetChanges;
	}

	protected virtual bool ValueEquals(T newValue, T oldValue)
	{
		return EqualityComparer<T>.Default.Equals(newValue, oldValue);
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("value");
		valueWriter.Write(writer, m_Value);
	}
}
