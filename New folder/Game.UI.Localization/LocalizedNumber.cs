using System;
using System.Collections.Generic;
using Colossal.Annotations;
using Colossal.UI.Binding;

namespace Game.UI.Localization;

public readonly struct LocalizedNumber<T> : ILocElement, IJsonWritable, IEquatable<LocalizedNumber<T>>
{
	private readonly IWriter<T> m_ValueWriter;

	public T value { get; }

	[CanBeNull]
	public string unit { get; }

	public bool signed { get; }

	public LocalizedNumber(T value, [CanBeNull] string unit = null, bool signed = false, IWriter<T> valueWriter = null)
	{
		this.value = value;
		this.unit = unit;
		this.signed = signed;
		m_ValueWriter = valueWriter ?? ValueWriters.Create<T>();
	}

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin("Game.UI.Localization.LocalizedNumber");
		writer.PropertyName("value");
		m_ValueWriter.Write(writer, value);
		writer.PropertyName("unit");
		writer.Write(unit);
		writer.PropertyName("signed");
		writer.Write(signed);
		writer.TypeEnd();
	}

	public bool Equals(LocalizedNumber<T> other)
	{
		if (EqualityComparer<T>.Default.Equals(value, other.value) && unit == other.unit)
		{
			return signed == other.signed;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is LocalizedNumber<T> other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(value, unit, signed);
	}
}
