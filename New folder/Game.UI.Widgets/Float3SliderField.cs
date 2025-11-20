using Unity.Mathematics;

namespace Game.UI.Widgets;

public class Float3SliderField : FloatSliderField<float3>
{
	protected override float3 defaultMin => new float3(float.MinValue);

	protected override float3 defaultMax => new float3(float.MaxValue);

	public override float3 ToFieldType(double4 value)
	{
		return new float3(value.xyz);
	}
}
