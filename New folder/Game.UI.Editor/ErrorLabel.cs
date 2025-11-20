using Colossal.UI.Binding;
using Game.UI.Widgets;

namespace Game.UI.Editor;

public class ErrorLabel : Label
{
	private bool m_Visible;

	public bool visible
	{
		get
		{
			return m_Visible;
		}
		set
		{
			if (m_Visible != value)
			{
				m_Visible = value;
				SetPropertiesChanged();
			}
		}
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("visible");
		writer.Write(m_Visible);
	}
}
