using Colossal.Serialization.Entities;
using Unity.Mathematics;

namespace Game.Simulation;

public struct TelecomCoverage : IStrideSerializable, ISerializable
{
	public byte m_SignalStrength;

	public byte m_NetworkLoad;

	public int networkQuality => m_SignalStrength * 510 / (255 + (m_NetworkLoad << 1));

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		byte signalStrength = m_SignalStrength;
		writer.Write(signalStrength);
		byte networkLoad = m_NetworkLoad;
		writer.Write(networkLoad);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref byte signalStrength = ref m_SignalStrength;
		reader.Read(out signalStrength);
		ref byte networkLoad = ref m_NetworkLoad;
		reader.Read(out networkLoad);
	}

	public int GetStride(Context context)
	{
		return 2;
	}

	public static float SampleNetworkQuality(CellMapData<TelecomCoverage> coverage, float3 position)
	{
		float2 @float = position.xz / coverage.m_CellSize + (float2)coverage.m_TextureSize * 0.5f - 0.5f;
		int4 xyxy = ((int2)math.floor(@float)).xyxy;
		xyxy.zw += 1;
		xyxy = math.clamp(xyxy, 0, coverage.m_TextureSize.xyxy - 1);
		int4 @int = xyxy.xzxz + coverage.m_TextureSize.x * xyxy.yyww;
		TelecomCoverage telecomCoverage = coverage.m_Buffer[@int.x];
		TelecomCoverage telecomCoverage2 = coverage.m_Buffer[@int.y];
		TelecomCoverage telecomCoverage3 = coverage.m_Buffer[@int.z];
		TelecomCoverage telecomCoverage4 = coverage.m_Buffer[@int.w];
		float4 float2 = new float4((int)telecomCoverage.m_SignalStrength, (int)telecomCoverage2.m_SignalStrength, (int)telecomCoverage3.m_SignalStrength, (int)telecomCoverage4.m_SignalStrength);
		float4 float3 = math.min(y: float2 / (127.5f + new float4((int)telecomCoverage.m_NetworkLoad, (int)telecomCoverage2.m_NetworkLoad, (int)telecomCoverage3.m_NetworkLoad, (int)telecomCoverage4.m_NetworkLoad)), x: 1f);
		float2 float4 = math.saturate(@float - xyxy.xy);
		float2 float5 = math.lerp(float3.xz, float3.yw, float4.x);
		return math.lerp(float5.x, float5.y, float4.y);
	}
}
