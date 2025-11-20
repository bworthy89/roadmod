using Colossal.UI.Binding;

namespace Game.UI.Widgets;

public class UIntField : Field<uint>
{
	public uint min { get; set; }

	public uint max { get; set; } = uint.MaxValue;

	public uint step { get; set; } = 1u;

	public uint stepMultiplier { get; set; } = 10u;

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("min");
		writer.Write(min);
		writer.PropertyName("max");
		writer.Write(max);
		writer.PropertyName("step");
		writer.Write(step);
		writer.PropertyName("stepMultiplier");
		writer.Write(step);
	}
}
