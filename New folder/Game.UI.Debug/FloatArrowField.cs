using Game.UI.Widgets;
using Unity.Mathematics;

namespace Game.UI.Debug;

public class FloatArrowField : FloatField<double>
{
	protected override double defaultMin => double.MinValue;

	protected override double defaultMax => double.MaxValue;

	public override double ToFieldType(double4 value)
	{
		return value.x;
	}
}
