using Colossal.Serialization.Entities;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Game.Simulation;

public struct Wind : IStrideSerializable, ISerializable
{
	public float2 m_Wind;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Wind);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Wind);
	}

	public int GetStride(Context context)
	{
		return UnsafeUtility.SizeOf<float2>();
	}

	public static float2 SampleWind(CellMapData<Wind> wind, float3 position)
	{
		float2 @float = position.xz / wind.m_CellSize + (float2)wind.m_TextureSize * 0.5f - 0.5f;
		int4 xyxy = ((int2)math.floor(@float)).xyxy;
		xyxy.zw += 1;
		xyxy = math.clamp(xyxy, 0, wind.m_TextureSize.xyxy - 1);
		int4 @int = xyxy.xzxz + wind.m_TextureSize.x * xyxy.yyww;
		float4 start = new float4(wind.m_Buffer[@int.x].m_Wind, wind.m_Buffer[@int.z].m_Wind);
		float4 end = new float4(wind.m_Buffer[@int.y].m_Wind, wind.m_Buffer[@int.w].m_Wind);
		float2 float2 = math.saturate(@float - xyxy.xy);
		float4 float3 = math.lerp(start, end, float2.x);
		return math.lerp(float3.xy, float3.zw, float2.y);
	}
}
