using System;
using Unity.Burst;
using Unity.Mathematics;

namespace Game.Rendering;

[BurstCompile]
public struct WaterHeightRequest : IComparable<WaterHeightRequest>
{
	public int entityId;

	public int queryId;

	public float3 position;

	public float distance;

	public WaterHeightRequest(int _entityId, int _query, float3 _position)
	{
		entityId = _entityId;
		queryId = _query;
		position = _position;
		distance = 0f;
	}

	public int CompareTo(WaterHeightRequest other)
	{
		return distance.CompareTo(other.distance);
	}
}
