using Colossal.Annotations;
using Colossal.UI.Binding;
using Game.UI.Widgets;
using Unity.Entities;

namespace Game.UI.Tooltip;

public class NameTooltip : Widget
{
	[CanBeNull]
	private string m_Icon;

	public Entity m_Entity;

	[CanBeNull]
	public string icon
	{
		get
		{
			return m_Icon;
		}
		set
		{
			if (value != m_Icon)
			{
				m_Icon = value;
				SetPropertiesChanged();
			}
		}
	}

	public Entity entity
	{
		get
		{
			return m_Entity;
		}
		set
		{
			if (value != m_Entity)
			{
				m_Entity = value;
				SetPropertiesChanged();
			}
		}
	}

	public NameSystem nameBinder { get; set; }

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("icon");
		writer.Write(icon);
		writer.PropertyName("name");
		nameBinder.BindName(writer, entity);
	}
}
