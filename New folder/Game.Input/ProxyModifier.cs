using System;
using System.Collections.Generic;
using Colossal.Json;

namespace Game.Input;

public struct ProxyModifier : IEquatable<ProxyModifier>
{
	public class Comparer : IEqualityComparer<ProxyModifier>, IComparer<ProxyModifier>
	{
		[Flags]
		public enum Options
		{
			Name = 1,
			Path = 2,
			Component = 4
		}

		private static readonly Dictionary<string, int> sOrder = new Dictionary<string, int>
		{
			{ "<Keyboard>/shift", 0 },
			{ "<Keyboard>/ctrl", 1 },
			{ "<Keyboard>/alt", 2 },
			{ "<Gamepad>/leftStickPress", 3 },
			{ "<Gamepad>/rightStickPress", 4 }
		};

		public static readonly Comparer defaultComparer = new Comparer();

		public readonly Options m_Options;

		public Comparer(Options options = Options.Path | Options.Component)
		{
			m_Options = options;
		}

		public bool Equals(ProxyModifier x, ProxyModifier y)
		{
			if ((m_Options & Options.Name) != 0 && x.m_Name != y.m_Name)
			{
				return false;
			}
			if ((m_Options & Options.Path) != 0 && x.m_Path != y.m_Path)
			{
				return false;
			}
			if ((m_Options & Options.Component) != 0 && x.m_Component != y.m_Component)
			{
				return false;
			}
			return true;
		}

		public int GetHashCode(ProxyModifier modifier)
		{
			HashCode hashCode = default(HashCode);
			if ((m_Options & Options.Name) != 0)
			{
				hashCode.Add(modifier.m_Name);
			}
			if ((m_Options & Options.Path) != 0)
			{
				hashCode.Add(modifier.m_Path);
			}
			if ((m_Options & Options.Component) != 0)
			{
				hashCode.Add(modifier.m_Component);
			}
			return hashCode.ToHashCode();
		}

		public int Compare(ProxyModifier x, ProxyModifier y)
		{
			int num = 0;
			if ((m_Options & Options.Name) != 0 && (num = string.Compare(x.m_Name, y.m_Name, StringComparison.Ordinal)) != 0)
			{
				return num;
			}
			if ((m_Options & Options.Path) != 0)
			{
				if (!sOrder.TryGetValue(x.m_Path, out var value))
				{
					value = int.MaxValue;
				}
				if (!sOrder.TryGetValue(y.m_Path, out var value2))
				{
					value2 = int.MaxValue;
				}
				num = ((value == int.MaxValue && value2 == int.MaxValue) ? string.Compare(x.m_Path, y.m_Path, StringComparison.Ordinal) : (value - value2));
				if (num != 0)
				{
					return num;
				}
			}
			if ((m_Options & Options.Component) != 0)
			{
				return x.m_Component - y.m_Component;
			}
			return num;
		}
	}

	public static readonly Comparer pathComparer = new Comparer(Comparer.Options.Path);

	[Exclude]
	public ActionComponent m_Component;

	public string m_Name;

	public string m_Path;

	public static Comparer defaultComparer => Comparer.defaultComparer;

	public override string ToString()
	{
		return m_Name + " - " + m_Path;
	}

	public bool Equals(ProxyModifier other)
	{
		return Comparer.defaultComparer.Equals(this, other);
	}

	public override int GetHashCode()
	{
		return Comparer.defaultComparer.GetHashCode(this);
	}

	public override bool Equals(object obj)
	{
		if (obj is ProxyModifier other)
		{
			return Equals(other);
		}
		return false;
	}

	public static bool operator ==(ProxyModifier left, ProxyModifier right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(ProxyModifier left, ProxyModifier right)
	{
		return !left.Equals(right);
	}
}
