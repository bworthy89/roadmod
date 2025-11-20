using System;
using System.Collections.Generic;
using Colossal.Annotations;
using Colossal.UI.Binding;

namespace Game.UI.Localization;

public readonly struct LocalizedFraction<T> : ILocElement, IJsonWritable, IEquatable<LocalizedFraction<T>>
{
	private readonly IWriter<T> m_ValueWriter;

	public T value { get; }

	public T total { get; }

	[CanBeNull]
	public string unit { get; }

	public LocalizedFraction(T value, T total, [CanBeNull] string unit = null, IWriter<T> valueWriter = null)
	{
		this.value = value;
		this.total = total;
		this.unit = unit;
		m_ValueWriter = valueWriter ?? ValueWriters.Create<T>();
	}

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin("Game.UI.Localization.LocalizedFraction");
		writer.PropertyName("value");
		m_ValueWriter.Write(writer, value);
		writer.PropertyName("total");
		m_ValueWriter.Write(writer, total);
		writer.PropertyName("unit");
		writer.Write(unit);
		writer.TypeEnd();
	}

	public bool Equals(LocalizedFraction<T> other)
	{
		if (EqualityComparer<T>.Default.Equals(value, other.value) && EqualityComparer<T>.Default.Equals(total, other.total))
		{
			return unit == other.unit;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is LocalizedFraction<T> other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(value, total, unit);
	}
}
