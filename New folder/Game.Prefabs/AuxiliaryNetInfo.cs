using System;
using Unity.Mathematics;

namespace Game.Prefabs;

[Serializable]
public class AuxiliaryNetInfo
{
	public NetPrefab m_Prefab;

	public float3 m_Position;

	public NetInvertMode m_InvertWhen;
}
