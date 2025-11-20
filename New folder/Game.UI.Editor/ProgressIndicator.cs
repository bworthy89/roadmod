using System;
using Colossal.UI.Binding;
using Game.UI.Widgets;
using UnityEngine;

namespace Game.UI.Editor;

public class ProgressIndicator : NamedWidgetWithTooltip
{
	public enum State
	{
		Loading,
		Success,
		Failure
	}

	private float m_Progress = 1f;

	private State m_State;

	public Func<float> progress { get; set; }

	public Func<State> state { get; set; }

	protected override WidgetChanges Update()
	{
		WidgetChanges widgetChanges = base.Update();
		if (this.state != null)
		{
			State state = this.state();
			if (state != m_State)
			{
				m_State = state;
				widgetChanges |= WidgetChanges.Properties;
			}
		}
		if (progress != null)
		{
			float a = progress();
			if (!Mathf.Approximately(a, m_Progress))
			{
				m_Progress = a;
				widgetChanges |= WidgetChanges.Properties;
			}
		}
		return widgetChanges;
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("state");
		writer.Write((int)m_State);
		writer.PropertyName("progress");
		writer.Write(m_Progress);
		writer.PropertyName("indeterminate");
		writer.Write(progress == null);
	}
}
