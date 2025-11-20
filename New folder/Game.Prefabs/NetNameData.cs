using Colossal.Serialization.Entities;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

public struct NetNameData : IComponentData, IQueryTypeParameter, IEmptySerializable
{
	public Color32 m_Color;

	public Color32 m_SelectedColor;

	public int m_MaterialIndex;
}
