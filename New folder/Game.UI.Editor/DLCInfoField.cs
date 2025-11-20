using Colossal.UI.Binding;
using Game.UI.Localization;
using Game.UI.Widgets;

namespace Game.UI.Editor;

public class DLCInfoField : Widget
{
	private LocalizedString m_DisplayName;

	private LocalizedString m_Type;

	private string m_Image;

	public LocalizedString displayName
	{
		get
		{
			return m_DisplayName;
		}
		set
		{
			m_DisplayName = value;
			SetPropertiesChanged();
		}
	}

	public LocalizedString type
	{
		get
		{
			return m_Type;
		}
		set
		{
			m_Type = value;
			SetPropertiesChanged();
		}
	}

	public string image
	{
		get
		{
			return m_Image;
		}
		set
		{
			m_Image = value;
			SetPropertiesChanged();
		}
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("displayName");
		writer.Write(displayName);
		writer.PropertyName("type");
		writer.Write(type);
		writer.PropertyName("image");
		writer.Write(image);
	}
}
