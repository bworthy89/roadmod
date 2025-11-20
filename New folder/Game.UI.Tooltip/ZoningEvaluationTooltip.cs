using System;
using Colossal.UI.Binding;
using Game.Simulation;
using Game.UI.Widgets;

namespace Game.UI.Tooltip;

public class ZoningEvaluationTooltip : Widget
{
	private ZoneEvaluationUtils.ZoningEvaluationFactor m_Factor;

	private float m_Score;

	public ZoneEvaluationUtils.ZoningEvaluationFactor factor
	{
		get
		{
			return m_Factor;
		}
		set
		{
			if (value != m_Factor)
			{
				m_Factor = value;
				SetPropertiesChanged();
			}
		}
	}

	public float score
	{
		get
		{
			return m_Score;
		}
		set
		{
			if (value != m_Score)
			{
				m_Score = value;
				SetPropertiesChanged();
			}
		}
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("factor");
		writer.Write(Enum.GetName(typeof(ZoneEvaluationUtils.ZoningEvaluationFactor), factor));
		writer.PropertyName("score");
		writer.Write(score);
	}
}
