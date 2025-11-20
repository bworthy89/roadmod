using System;
using Colossal.Mathematics;
using Colossal.UI.Binding;
using Game.Prefabs;

namespace Game.UI.InGame;

public readonly struct UIPolicySlider : IEquatable<UIPolicySlider>, IJsonWritable
{
	private readonly float m_Value;

	private readonly float m_Default;

	private readonly float m_Step;

	private readonly PolicySliderUnit m_Unit;

	public Bounds1 range { get; }

	public UIPolicySlider(float value, PolicySliderData sliderData)
	{
		m_Value = value;
		range = sliderData.m_Range;
		m_Default = sliderData.m_Default;
		m_Step = sliderData.m_Step;
		m_Unit = (PolicySliderUnit)sliderData.m_Unit;
	}

	public UIPolicySlider(PolicySliderData sliderData)
	{
		m_Value = sliderData.m_Default;
		range = sliderData.m_Range;
		m_Default = sliderData.m_Default;
		m_Step = sliderData.m_Step;
		m_Unit = (PolicySliderUnit)sliderData.m_Unit;
	}

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin(TypeNames.kPolicySlider);
		writer.PropertyName("range");
		writer.Write(range);
		writer.PropertyName("value");
		writer.Write(m_Value);
		writer.PropertyName("default");
		writer.Write(m_Default);
		writer.PropertyName("step");
		writer.Write(m_Step);
		writer.PropertyName("unit");
		writer.Write(Enum.GetName(typeof(PolicySliderUnit), m_Unit));
		writer.TypeEnd();
	}

	public override int GetHashCode()
	{
		return (range, m_Value, m_Default, m_Step, m_Unit).GetHashCode();
	}

	public override bool Equals(object obj)
	{
		if (obj is UIPolicySlider other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(UIPolicySlider other)
	{
		Bounds1 bounds = range;
		float value = m_Value;
		float num = m_Default;
		float step = m_Step;
		PolicySliderUnit unit = m_Unit;
		Bounds1 bounds2 = other.range;
		float value2 = other.m_Value;
		float num2 = other.m_Default;
		float step2 = m_Step;
		PolicySliderUnit unit2 = m_Unit;
		if (bounds == bounds2 && value == value2 && num == num2 && step == step2)
		{
			return unit == unit2;
		}
		return false;
	}

	public static bool operator ==(UIPolicySlider left, UIPolicySlider right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(UIPolicySlider left, UIPolicySlider right)
	{
		return !left.Equals(right);
	}
}
