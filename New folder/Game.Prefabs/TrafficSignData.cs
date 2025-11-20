using Unity.Entities;

namespace Game.Prefabs;

public struct TrafficSignData : IComponentData, IQueryTypeParameter
{
	public uint m_TypeMask;

	public int m_SpeedLimit;

	public static uint GetTypeMask(TrafficSignType type)
	{
		if (type == TrafficSignType.None)
		{
			return 0u;
		}
		return (uint)(1 << (int)(20 - type));
	}
}
