using Colossal.UI.Binding;

namespace Game.UI.Tooltip;

public class ProgressTooltip : LabelIconTooltip
{
	public const float kCapacityWarningThreshold = 0.75f;

	private float m_Value;

	private float m_Max;

	private string m_Unit = "integer";

	private bool m_OmitMax;

	public float value
	{
		get
		{
			return m_Value;
		}
		set
		{
			if (value != m_Value)
			{
				m_Value = value;
				SetPropertiesChanged();
			}
		}
	}

	public float max
	{
		get
		{
			return m_Max;
		}
		set
		{
			if (!object.Equals(value, m_Max))
			{
				m_Max = value;
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

	public bool omitMax
	{
		get
		{
			return m_OmitMax;
		}
		set
		{
			if (value != m_OmitMax)
			{
				m_OmitMax = value;
				SetPropertiesChanged();
			}
		}
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("value");
		writer.Write(value);
		writer.PropertyName("max");
		writer.Write(max);
		writer.PropertyName("unit");
		writer.Write(unit);
		writer.PropertyName("omitMax");
		writer.Write(omitMax);
	}

	public static void SetCapacityColor(ProgressTooltip tooltip)
	{
		if (tooltip.value >= tooltip.max * 0.75f)
		{
			tooltip.color = TooltipColor.Info;
		}
		else if (tooltip.value > 0f)
		{
			tooltip.color = TooltipColor.Warning;
		}
		else
		{
			tooltip.color = TooltipColor.Error;
		}
	}
}
