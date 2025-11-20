using System;
using Colossal.UI.Binding;
using Unity.Mathematics;

namespace Game.UI.InGame;

public readonly struct IndicatorValue : IEquatable<IndicatorValue>, IJsonWritable
{
	public float min { get; }

	public float max { get; }

	public float current { get; }

	public IndicatorValue(float min, float max, float current)
	{
		this.min = min;
		this.max = max;
		this.current = math.clamp(current, min, max);
	}

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin(GetType().Name);
		writer.PropertyName("min");
		writer.Write(min);
		writer.PropertyName("max");
		writer.Write(max);
		writer.PropertyName("current");
		writer.Write(current);
		writer.TypeEnd();
	}

	public static IndicatorValue Calculate(float supply, float demand, float minRangeFactor = -1f, float maxRangeFactor = 1f)
	{
		float num = ((supply > float.Epsilon) ? math.clamp((supply - demand) / supply, minRangeFactor, maxRangeFactor) : minRangeFactor);
		return new IndicatorValue(minRangeFactor, maxRangeFactor, num);
	}

	public bool Equals(IndicatorValue other)
	{
		float num = min;
		float num2 = max;
		float num3 = current;
		float num4 = other.min;
		float num5 = other.max;
		float num6 = other.current;
		if (num == num4 && num2 == num5)
		{
			return num3 == num6;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is IndicatorValue indicatorValue)
		{
			return indicatorValue.Equals(this);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (min, max, current).GetHashCode();
	}
}
