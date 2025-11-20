using System;
using System.Collections.Generic;
using Colossal.Annotations;
using Colossal.UI.Binding;

namespace Game.UI.Localization;

public readonly struct LocalizedBounds<T> : ILocElement, IJsonWritable, IEquatable<LocalizedBounds<T>>
{
	private readonly IWriter<T> m_ValueWriter;

	public T min { get; }

	public T max { get; }

	[CanBeNull]
	public string unit { get; }

	public LocalizedBounds(T min, T max, [CanBeNull] string unit = null, IWriter<T> valueWriter = null)
	{
		this.min = min;
		this.max = max;
		this.unit = unit;
		m_ValueWriter = valueWriter ?? ValueWriters.Create<T>();
	}

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin("Game.UI.Localization.LocalizedBounds");
		writer.PropertyName("min");
		m_ValueWriter.Write(writer, min);
		writer.PropertyName("max");
		m_ValueWriter.Write(writer, max);
		writer.PropertyName("unit");
		writer.Write(unit);
		writer.TypeEnd();
	}

	public bool Equals(LocalizedBounds<T> other)
	{
		if (EqualityComparer<T>.Default.Equals(min, other.min) && EqualityComparer<T>.Default.Equals(max, other.max))
		{
			return unit == other.unit;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is LocalizedBounds<T> other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(min, max, unit);
	}
}
