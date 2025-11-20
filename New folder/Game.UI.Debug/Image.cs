using Colossal.UI.Binding;
using Game.UI.Widgets;

namespace Game.UI.Debug;

public class Image : NamedWidget
{
	private string m_Uri;

	public string uri
	{
		get
		{
			return m_Uri;
		}
		set
		{
			if (m_Uri != value)
			{
				m_Uri = value;
				SetPropertiesChanged();
			}
		}
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("uri");
		writer.Write(uri);
	}
}
