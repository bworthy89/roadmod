using Colossal.UI.Binding;

namespace Game.UI.InGame;

public struct StringProperty : IJsonWritable
{
	public string labelId;

	public string valueId;

	public string icon;

	public string valueIcon;

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin("Game.UI.Common.StringProperty");
		writer.PropertyName("labelId");
		writer.Write(labelId);
		writer.PropertyName("valueId");
		writer.Write(valueId);
		writer.PropertyName("icon");
		writer.Write(icon);
		writer.PropertyName("valueIcon");
		writer.Write(valueIcon);
		writer.TypeEnd();
	}
}
