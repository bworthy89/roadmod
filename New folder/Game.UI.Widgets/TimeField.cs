using Colossal.UI.Binding;

namespace Game.UI.Widgets;

public abstract class TimeField<T> : Field<T>
{
	public float min { get; set; }

	public float max { get; set; } = 1f;

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("min");
		writer.Write(min);
		writer.PropertyName("max");
		writer.Write(max);
	}
}
