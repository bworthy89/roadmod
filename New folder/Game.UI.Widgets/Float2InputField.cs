using Unity.Mathematics;

namespace Game.UI.Widgets;

public class Float2InputField : FloatField<float2>
{
	protected override float2 defaultMin => new float2(float.MinValue);

	protected override float2 defaultMax => new float2(float.MaxValue);

	public override float2 ToFieldType(double4 value)
	{
		return new float2(value.xy);
	}
}
