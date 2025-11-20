using System;
using Colossal.UI.Binding;
using Unity.Mathematics;

namespace Game.UI.Widgets;

public class RangedSliderField : FloatSliderField<float>, IIconProvider
{
	protected override float defaultMin => float.MinValue;

	protected override float defaultMax => float.MaxValue;

	public float[] ranges { get; set; }

	public Func<string> iconSrc { get; set; }

	public override float ToFieldType(double4 value)
	{
		return (float)value.x;
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("ranges");
		int num = ((ranges != null) ? ranges.Length : 0);
		writer.ArrayBegin(num);
		for (int i = 0; i < num; i++)
		{
			writer.Write(ranges[i]);
		}
		writer.ArrayEnd();
		writer.PropertyName("iconSrc");
		writer.Write(iconSrc());
	}
}
