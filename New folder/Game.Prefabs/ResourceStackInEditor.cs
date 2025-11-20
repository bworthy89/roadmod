using System;
using Game.Economy;

namespace Game.Prefabs;

[Serializable]
public struct ResourceStackInEditor
{
	public ResourceInEditor m_Resource;

	public int m_Amount;
}
