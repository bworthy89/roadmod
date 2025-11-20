using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;

namespace Game.Input;

public abstract class AnalogValueInputBindingComposite<T> : ValueInputBindingComposite<T> where T : struct
{
	protected class Vector2Comparer : IComparer<Vector2>
	{
		public static Vector2Comparer instance = new Vector2Comparer();

		public int Compare(Vector2 x, Vector2 y)
		{
			return x.magnitude.CompareTo(y.magnitude);
		}
	}

	public Mode m_Mode;

	protected override IEnumerable<NamedValue> GetParameters()
	{
		return base.GetParameters().Append(NamedValue.From("m_Mode", m_Mode));
	}
}
