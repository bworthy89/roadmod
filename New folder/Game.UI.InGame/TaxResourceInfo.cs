using Colossal.UI.Binding;

namespace Game.UI.InGame;

public struct TaxResourceInfo : IJsonWritable
{
	public string m_ID;

	public string m_Icon;

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin("taxation.TaxResourceInfo");
		writer.PropertyName("id");
		writer.Write(m_ID);
		writer.PropertyName("icon");
		writer.Write(m_Icon);
		writer.TypeEnd();
	}
}
