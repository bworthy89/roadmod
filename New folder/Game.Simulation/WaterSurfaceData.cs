using Unity.Collections;
using Unity.Mathematics;

namespace Game.Simulation;

public struct WaterSurfaceData<T> where T : struct
{
	public NativeArray<T> depths { get; private set; }

	public int3 resolution { get; private set; }

	public float3 scale { get; private set; }

	public float3 offset { get; private set; }

	public bool isCreated => depths.IsCreated;

	public bool hasDepths { get; private set; }

	public WaterSurfaceData(NativeArray<T> _depths, int3 _resolution, float3 _scale, float3 _offset, bool _hasDepths)
	{
		depths = _depths;
		scale = _scale;
		offset = _offset;
		resolution = _resolution;
		hasDepths = _hasDepths;
	}
}
