using Colossal.UI.Binding;

namespace Game.UI.InGame;

public struct IntRangeProperty : IJsonWritable
{
	public string labelId;

	public int minValue;

	public int maxValue;

	public string unit;

	public bool signed;

	public string icon;

	public string valueIcon;

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin("Game.UI.Common.NumberRangeProperty");
		writer.PropertyName("labelId");
		writer.Write(labelId);
		writer.PropertyName("minValue");
		writer.Write(minValue);
		writer.PropertyName("maxValue");
		writer.Write(maxValue);
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
