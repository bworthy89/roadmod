using System;
using UnityEngine;

namespace Game.UI;

[Serializable]
public class BudgetItem<T> where T : struct, Enum
{
	public string m_ID;

	public Color m_Color = Color.gray;

	public string m_Icon;

	public T[] m_Sources;
}
