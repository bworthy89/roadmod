using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem.Utilities;

namespace Game.Input;

public struct Usages : IEnumerable<int>, IEnumerable, IEquatable<Usages>
{
	public class Comparer : IEqualityComparer<Usages>
	{
		public static readonly Comparer defaultComparer = new Comparer();

		public bool Equals(Usages x, Usages y)
		{
			ulong[] array = x.m_Value ?? Array.Empty<ulong>();
			ulong[] array2 = y.m_Value ?? Array.Empty<ulong>();
			int num = Math.Max(array.Length, array2.Length);
			for (int i = 0; i < num; i++)
			{
				if (((i < array.Length) ? array[i] : 0) != ((i < array2.Length) ? array2[i] : 0))
				{
					return false;
				}
			}
			return true;
		}

		public int GetHashCode(Usages usages)
		{
			HashCode hashCode = default(HashCode);
			if (usages.m_Value != null)
			{
				for (int i = 0; i < usages.m_Value.Length; i++)
				{
					hashCode.Add(usages.m_Value[i]);
				}
			}
			return hashCode.ToHashCode();
		}
	}

	public const string kMenuUsage = "Menu";

	public const string kDefaultUsage = "DefaultTool";

	public const string kOverlayUsage = "Overlay";

	public const string kToolUsage = "Tool";

	public const string kCancelableToolUsage = "CancelableTool";

	public const string kDebugUsage = "Debug";

	public const string kEditorUsage = "Editor";

	public const string kPhotoModeUsage = "PhotoMode";

	public const string kOptionsUsage = "Options";

	public const string kTutorialUsage = "Tutorial";

	public const string kDiscardableToolUsage = "DiscardableTool";

	private ulong[] m_Value;

	private bool m_ReadOnly;

	internal static Dictionary<string, int> usagesMap { get; } = new Dictionary<string, int>();

	public static Usages defaultUsages { get; } = new Usages(BuiltInUsages.DefaultSet);

	public static Usages empty => new Usages(0, readOnly: true);

	public bool this[int index]
	{
		get
		{
			int num = index >> 6;
			if (m_Value == null || m_Value.Length < num)
			{
				return false;
			}
			return (m_Value[num] & (ulong)(1L << index)) != 0;
		}
		set
		{
			if (m_ReadOnly)
			{
				throw new InvalidOperationException("Value is readonly");
			}
			int num = index >> 6;
			if (value)
			{
				if (m_Value == null)
				{
					m_Value = new ulong[num + 1];
				}
				else if (m_Value.Length <= num)
				{
					Array.Resize(ref m_Value, num + 1);
				}
				m_Value[num] |= (ulong)(1L << index);
			}
			else if (m_Value != null && m_Value.Length > num)
			{
				m_Value[num] &= (ulong)(~(1L << index));
			}
		}
	}

	public bool this[string usage]
	{
		get
		{
			if (usagesMap.TryGetValue(usage, out var value))
			{
				return this[value];
			}
			return false;
		}
		set
		{
			this[AddOrGetUsage(usage)] = value;
		}
	}

	public bool isReadOnly => m_ReadOnly;

	public bool isNone
	{
		get
		{
			if (m_Value == null)
			{
				return true;
			}
			for (int i = 0; i < m_Value.Length; i++)
			{
				if (m_Value[i] != 0L)
				{
					return false;
				}
			}
			return true;
		}
	}

	public NameAndParameters parameters
	{
		get
		{
			return new NameAndParameters
			{
				name = "Usages",
				parameters = new ReadOnlyArray<NamedValue>(this.Select((int u) => NamedValue.From(u.ToString(), value: true)).ToArray())
			};
		}
		set
		{
			foreach (NamedValue parameter in value.parameters)
			{
				if (int.TryParse(parameter.name, out var result))
				{
					this[result] = parameter.value.ToBoolean();
				}
			}
		}
	}

	public Usages(int length = 0, bool readOnly = true)
	{
		m_Value = ((length == 0) ? Array.Empty<ulong>() : new ulong[length]);
		m_ReadOnly = readOnly;
	}

	public Usages(bool readOnly = true, params int[] values)
		: this(values.Max() >> 6, readOnly: false)
	{
		foreach (int index in values)
		{
			this[index] = true;
		}
		m_ReadOnly = readOnly;
	}

	public Usages(BuiltInUsages usages, bool readOnly = true)
		: this(0, readOnly: false)
	{
		this["Menu"] = (usages & BuiltInUsages.Menu) != 0;
		this["DefaultTool"] = (usages & BuiltInUsages.DefaultTool) != 0;
		this["Overlay"] = (usages & BuiltInUsages.Overlay) != 0;
		this["Tool"] = (usages & BuiltInUsages.Tool) != 0;
		this["CancelableTool"] = (usages & BuiltInUsages.CancelableTool) != 0;
		this["Debug"] = (usages & BuiltInUsages.Debug) != 0;
		this["Editor"] = (usages & BuiltInUsages.Editor) != 0;
		this["PhotoMode"] = (usages & BuiltInUsages.PhotoMode) != 0;
		this["Options"] = (usages & BuiltInUsages.Options) != 0;
		this["Tutorial"] = (usages & BuiltInUsages.Tutorial) != 0;
		this["DiscardableTool"] = (usages & BuiltInUsages.DiscardableTool) != 0;
		m_ReadOnly = readOnly;
	}

	internal Usages(bool readOnly = true, params string[] customUsages)
		: this(0, readOnly: false)
	{
		if (customUsages != null)
		{
			foreach (string usage in customUsages)
			{
				this[usage] = true;
			}
		}
		m_ReadOnly = readOnly;
	}

	public Usages Copy(bool readOnly = true)
	{
		if (m_ReadOnly && readOnly)
		{
			return this;
		}
		Usages result = new Usages(m_Value.Length, readOnly);
		Array.Copy(m_Value, result.m_Value, m_Value.Length);
		return result;
	}

	public void SetFrom(Usages source)
	{
		if (m_ReadOnly)
		{
			throw new InvalidOperationException("Value is readonly");
		}
		if (m_Value == null)
		{
			if (source.m_Value == null || source.m_Value.Length == 0)
			{
				m_Value = Array.Empty<ulong>();
				return;
			}
			Array.Resize(ref m_Value, source.m_Value.Length);
		}
		Array.Copy(source.m_Value, m_Value, m_Value.Length);
	}

	internal void MakeReadOnly()
	{
		m_ReadOnly = true;
	}

	internal void MakeEditable()
	{
		m_ReadOnly = false;
	}

	public static bool TestAny(Usages usages1, Usages usages2)
	{
		ulong[] array = usages1.m_Value ?? Array.Empty<ulong>();
		ulong[] array2 = usages2.m_Value ?? Array.Empty<ulong>();
		int num = Math.Max(array.Length, array2.Length);
		for (int i = 0; i < num; i++)
		{
			if ((((i < array.Length) ? array[i] : 0) & ((i < array2.Length) ? array2[i] : 0)) != 0L)
			{
				return true;
			}
		}
		return false;
	}

	public static bool TestAll(Usages usages1, Usages usages2)
	{
		ulong[] array = usages1.m_Value ?? Array.Empty<ulong>();
		ulong[] array2 = usages2.m_Value ?? Array.Empty<ulong>();
		int num = Math.Max(array.Length, array2.Length);
		for (int i = 0; i < num; i++)
		{
			if (((i < array.Length) ? array[i] : 0) != ((i < array2.Length) ? array2[i] : 0))
			{
				return false;
			}
		}
		return true;
	}

	public static Usages Intersect(Usages usages1, Usages usages2, bool readOnly = true)
	{
		ulong[] array = usages1.m_Value ?? Array.Empty<ulong>();
		ulong[] array2 = usages2.m_Value ?? Array.Empty<ulong>();
		int num = Math.Max(array.Length, array2.Length);
		Usages result = new Usages(num, readOnly);
		for (int i = 0; i < num; i++)
		{
			result.m_Value[i] = ((i < array.Length) ? array[i] : 0) & ((i < array2.Length) ? array2[i] : 0);
		}
		return result;
	}

	public static Usages Combine(Usages usages1, Usages usages2, bool readOnly = true)
	{
		ulong[] array = usages1.m_Value ?? Array.Empty<ulong>();
		ulong[] array2 = usages2.m_Value ?? Array.Empty<ulong>();
		int num = Math.Max(array.Length, array2.Length);
		Usages result = new Usages(num, readOnly);
		for (int i = 0; i < num; i++)
		{
			result.m_Value[i] = ((i < array.Length) ? array[i] : 0) | ((i < array2.Length) ? array2[i] : 0);
		}
		return result;
	}

	internal static int AddOrGetUsage(string usageName)
	{
		if (!usagesMap.TryGetValue(usageName, out var value))
		{
			value = usagesMap.Count;
			usagesMap[usageName] = value;
		}
		return value;
	}

	IEnumerator<int> IEnumerable<int>.GetEnumerator()
	{
		return Enumerate().GetEnumerator();
	}

	public IEnumerator GetEnumerator()
	{
		return Enumerate().GetEnumerator();
	}

	private IEnumerable<int> Enumerate()
	{
		if (m_Value == null)
		{
			yield break;
		}
		for (int i = 0; i < m_Value.Length; i++)
		{
			for (int j = 0; j < 64; j++)
			{
				if ((m_Value[i] & (ulong)(1L << j)) != 0L)
				{
					yield return (i << 6) + j;
				}
			}
		}
	}

	public bool Equals(Usages other)
	{
		return Comparer.defaultComparer.Equals(this, other);
	}

	public override string ToString()
	{
		if (m_Value != null)
		{
			return string.Join('|', this);
		}
		return "Empty";
	}
}
