using Colossal.UI.Binding;

namespace Game.UI.Widgets;

public abstract class FloatField<T> : MinMaxField<T>
{
	public int fractionDigits { get; set; } = 3;

	public double step { get; set; } = 0.1;

	public double stepMultiplier { get; set; } = 10.0;

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("fractionDigits");
		writer.Write(fractionDigits);
		writer.PropertyName("step");
		writer.Write(step);
		writer.PropertyName("stepMultiplier");
		writer.Write(stepMultiplier);
	}
}
