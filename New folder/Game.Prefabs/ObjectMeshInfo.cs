using System;
using Game.Objects;
using Game.UI.Widgets;
using Unity.Mathematics;

namespace Game.Prefabs;

[Serializable]
public class ObjectMeshInfo
{
	public RenderPrefabBase m_Mesh;

	[InputField]
	[RangeN(-10000f, 10000f, true)]
	public float3 m_Position;

	public quaternion m_Rotation = quaternion.identity;

	public ObjectState m_RequireState;
}
