using System;
using Colossal.UI.Binding;

namespace Game.UI.Tooltip;

public abstract class NumberTooltip<T> : LabelIconTooltip where T : IEquatable<T>
{
	private T m_Value;

	private string m_Unit = "integer";

	private bool m_Signed;

	private IWriter<T> m_ValueWriter;

	public T value
	{
		get
		{
			return m_Value;
		}
		set
		{
			if (!value.Equals(m_Value))
			{
				m_Value = value;
				SetPropertiesChanged();
			}
		}
	}

	public string unit
	{
		get
		{
			return m_Unit;
		}
		set
		{
			if (value != m_Unit)
			{
				m_Unit = value;
				SetPropertiesChanged();
			}
		}
	}

	public bool signed
	{
		get
		{
			return m_Signed;
		}
		set
		{
			if (value != m_Signed)
			{
				m_Signed = value;
				SetPropertiesChanged();
			}
		}
	}

	protected IWriter<T> valueWriter
	{
		get
		{
			if (m_ValueWriter == null)
			{
				m_ValueWriter = ValueWriters.Create<T>();
			}
			return m_ValueWriter;
		}
		set
		{
			m_ValueWriter = value;
		}
	}

	public override string propertiesTypeName => "Game.UI.Tooltip.NumberTooltip";

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("value");
		valueWriter.Write(writer, value);
		writer.PropertyName("unit");
		writer.Write(unit);
		writer.PropertyName("signed");
		writer.Write(signed);
	}
}
