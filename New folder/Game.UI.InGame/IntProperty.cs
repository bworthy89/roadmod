using Colossal.UI.Binding;

namespace Game.UI.InGame;

public struct IntProperty : IJsonWritable
{
	public string labelId;

	public int value;

	public string unit;

	public bool signed;

	public string icon;

	public string valueIcon;

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin("Game.UI.Common.NumberProperty");
		writer.PropertyName("labelId");
		writer.Write(labelId);
		writer.PropertyName("value");
		writer.Write(value);
		writer.PropertyName("unit");
		writer.Write(unit);
		writer.PropertyName("signed");
		writer.Write(signed);
		writer.PropertyName("icon");
		writer.Write(icon);
		writer.PropertyName("valueIcon");
		writer.Write(valueIcon);
		writer.TypeEnd();
	}
}
