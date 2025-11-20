using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[InternalBufferCapacity(4)]
public struct WaterSourceColorElement : IBufferElementData
{
	public Color m_Outline;

	public Color m_Fill;

	public Color m_ProjectedOutline;

	public Color m_ProjectedFill;
}
