using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct InfoviewVehicleData : IComponentData, IQueryTypeParameter
{
	public VehicleType m_Type;

	public float4 m_Color;
}
