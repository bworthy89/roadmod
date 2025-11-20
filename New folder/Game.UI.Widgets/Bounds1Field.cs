using Colossal.Mathematics;
using Colossal.UI.Binding;

namespace Game.UI.Widgets;

public abstract class Bounds1Field : Field<Bounds1>
{
	public float min { get; set; } = float.MinValue;

	public float max { get; set; } = float.MaxValue;

	public int fractionDigits { get; set; } = 3;

	public float step { get; set; } = 0.1f;

	public bool allowMinGreaterMax { get; set; }

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("min");
		writer.Write(min);
		writer.PropertyName("max");
		writer.Write(max);
		writer.PropertyName("fractionDigits");
		writer.Write(fractionDigits);
		writer.PropertyName("step");
		writer.Write(step);
		writer.PropertyName("allowMinGreaterMax");
		writer.Write(allowMinGreaterMax);
	}
}
