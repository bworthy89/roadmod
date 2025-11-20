using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Annotations;
using Colossal.UI.Binding;

namespace Game.UI.Localization;

public readonly struct LocalizedString : ILocElement, IJsonWritable, IEquatable<LocalizedString>
{
	[CanBeNull]
	public string id { get; }

	[CanBeNull]
	public string value { get; }

	[CanBeNull]
	public IReadOnlyDictionary<string, ILocElement> args { get; }

	public bool isEmpty
	{
		get
		{
			if (id == null)
			{
				return value == null;
			}
			return false;
		}
	}

	public LocalizedString([CanBeNull] string id, [CanBeNull] string value, [CanBeNull] IReadOnlyDictionary<string, ILocElement> args)
	{
		this.id = id;
		this.value = value;
		this.args = args;
	}

	public static LocalizedString Id(string id)
	{
		return new LocalizedString(id, null, null);
	}

	public static LocalizedString Value(string value)
	{
		return new LocalizedString(null, value, null);
	}

	public static LocalizedString IdWithFallback(string id, string value)
	{
		return new LocalizedString(id, value, null);
	}

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin(GetType().FullName);
		writer.PropertyName("id");
		writer.Write(id);
		writer.PropertyName("value");
		writer.Write(value);
		writer.PropertyName("args");
		writer.Write(args);
		writer.TypeEnd();
	}

	public bool Equals(LocalizedString other)
	{
		if (id == other.id && value == other.value)
		{
			return ArgsEqual(args, other.args);
		}
		return false;
	}

	private static bool ArgsEqual([CanBeNull] IReadOnlyDictionary<string, ILocElement> a, [CanBeNull] IReadOnlyDictionary<string, ILocElement> b)
	{
		if (!object.Equals(a, b))
		{
			if (a != null && b != null)
			{
				return a.SequenceEqual(b);
			}
			return false;
		}
		return true;
	}

	public override bool Equals(object obj)
	{
		if (obj is LocalizedString other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(id, value, args);
	}

	public static implicit operator LocalizedString(string id)
	{
		return Id(id);
	}
}
