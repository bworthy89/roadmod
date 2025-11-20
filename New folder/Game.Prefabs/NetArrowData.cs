using Colossal.Serialization.Entities;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

public struct NetArrowData : IComponentData, IQueryTypeParameter, IEmptySerializable
{
	public Color32 m_RoadColor;

	public Color32 m_TrackColor;

	public int m_MaterialIndex;
}
