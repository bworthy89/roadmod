using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Rendering;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct FrustumPlanes
{
	public enum IntersectResult
	{
		Out,
		In,
		Partial
	}

	public struct PlanePacket4
	{
		public float4 Xs;

		public float4 Ys;

		public float4 Zs;

		public float4 Distances;
	}

	public static int GetPacketCount(int cullingPlaneCount)
	{
		return cullingPlaneCount + 3 >> 2;
	}

	public static void BuildSOAPlanePackets(NativeArray<Plane> cullingPlanes, int cullingPlaneCount, NativeList<PlanePacket4> result)
	{
		int packetCount = GetPacketCount(cullingPlaneCount);
		result.ResizeUninitialized(packetCount);
		for (int i = 0; i < cullingPlaneCount; i++)
		{
			Plane plane = cullingPlanes[i];
			PlanePacket4 value = result[i >> 2];
			value.Xs[i & 3] = plane.normal.x;
			value.Ys[i & 3] = plane.normal.y;
			value.Zs[i & 3] = plane.normal.z;
			value.Distances[i & 3] = plane.distance;
			result[i >> 2] = value;
		}
		for (int j = cullingPlaneCount; j < 4 * packetCount; j++)
		{
			PlanePacket4 value2 = result[j >> 2];
			value2.Xs[j & 3] = 1f;
			value2.Ys[j & 3] = 0f;
			value2.Zs[j & 3] = 0f;
			value2.Distances[j & 3] = 1E+09f;
			result[j >> 2] = value2;
		}
	}

	private static float4 dot4(float4 xs, float4 ys, float4 zs, float4 mx, float4 my, float4 mz)
	{
		return xs * mx + ys * my + zs * mz;
	}

	public unsafe static IntersectResult CalculateIntersectResult(PlanePacket4* cullingPlanePackets, int length, float3 center, float3 extents)
	{
		float4 xxxx = center.xxxx;
		float4 yyyy = center.yyyy;
		float4 zzzz = center.zzzz;
		float4 xxxx2 = extents.xxxx;
		float4 yyyy2 = extents.yyyy;
		float4 zzzz2 = extents.zzzz;
		int4 x = 0;
		int4 x2 = 0;
		for (int i = 0; i < length; i++)
		{
			PlanePacket4 planePacket = cullingPlanePackets[i];
			float4 @float = dot4(planePacket.Xs, planePacket.Ys, planePacket.Zs, xxxx, yyyy, zzzz) + planePacket.Distances;
			float4 float2 = dot4(xxxx2, yyyy2, zzzz2, math.abs(planePacket.Xs), math.abs(planePacket.Ys), math.abs(planePacket.Zs));
			x += (int4)(@float + float2 < 0f);
			x2 += (int4)(@float >= float2);
		}
		int num = math.csum(x2);
		if (math.csum(x) != 0)
		{
			return IntersectResult.Out;
		}
		if (num != 4 * length)
		{
			return IntersectResult.Partial;
		}
		return IntersectResult.In;
	}

	public unsafe static void Intersect(PlanePacket4* cullingPlanePackets, int length, float3 center, float3 extents, out ulong inMask, out ulong outMask)
	{
		float4 xxxx = center.xxxx;
		float4 yyyy = center.yyyy;
		float4 zzzz = center.zzzz;
		float4 xxxx2 = extents.xxxx;
		float4 yyyy2 = extents.yyyy;
		float4 zzzz2 = extents.zzzz;
		uint4 x = 0u;
		uint4 x2 = 0u;
		uint4 x3 = 0u;
		uint4 x4 = 0u;
		uint4 trueValue = new uint4(1u, 2u, 4u, 8u);
		uint4 trueValue2 = new uint4(1u, 2u, 4u, 8u);
		int num = math.min(8, length);
		for (int i = 0; i < num; i++)
		{
			PlanePacket4 planePacket = cullingPlanePackets[i];
			float4 @float = dot4(planePacket.Xs, planePacket.Ys, planePacket.Zs, xxxx, yyyy, zzzz) + planePacket.Distances;
			float4 float2 = dot4(xxxx2, yyyy2, zzzz2, math.abs(planePacket.Xs), math.abs(planePacket.Ys), math.abs(planePacket.Zs));
			x2 += math.select(0u, trueValue, @float + float2 < 0f);
			x += math.select(0u, trueValue, @float >= float2);
			trueValue <<= 4;
		}
		for (int j = num; j < length; j++)
		{
			PlanePacket4 planePacket2 = cullingPlanePackets[j];
			float4 float3 = dot4(planePacket2.Xs, planePacket2.Ys, planePacket2.Zs, xxxx, yyyy, zzzz) + planePacket2.Distances;
			float4 float4 = dot4(xxxx2, yyyy2, zzzz2, math.abs(planePacket2.Xs), math.abs(planePacket2.Ys), math.abs(planePacket2.Zs));
			x4 += math.select(0u, trueValue2, float3 + float4 < 0f);
			x3 += math.select(0u, trueValue2, float3 >= float4);
			trueValue2 <<= 4;
		}
		inMask = math.csum(x) | ((ulong)math.csum(x3) << 32);
		outMask = math.csum(x2) | ((ulong)math.csum(x4) << 32);
	}

	public unsafe static bool Intersect(PlanePacket4* cullingPlanePackets, int length, float3 center, float3 extents)
	{
		float4 xxxx = center.xxxx;
		float4 yyyy = center.yyyy;
		float4 zzzz = center.zzzz;
		float4 xxxx2 = extents.xxxx;
		float4 yyyy2 = extents.yyyy;
		float4 zzzz2 = extents.zzzz;
		int4 x = 0;
		for (int i = 0; i < length; i++)
		{
			PlanePacket4 planePacket = cullingPlanePackets[i];
			float4 @float = dot4(planePacket.Xs, planePacket.Ys, planePacket.Zs, xxxx, yyyy, zzzz) + planePacket.Distances;
			float4 float2 = dot4(xxxx2, yyyy2, zzzz2, math.abs(planePacket.Xs), math.abs(planePacket.Ys), math.abs(planePacket.Zs));
			x += (int4)(@float + float2 < 0f);
		}
		return math.csum(x) == 0;
	}

	public unsafe static bool Intersect(PlanePacket4* cullingPlanePackets, int length, float3 center, float radius)
	{
		float4 xxxx = center.xxxx;
		float4 yyyy = center.yyyy;
		float4 zzzz = center.zzzz;
		float4 @float = new float4(radius);
		int4 x = 0;
		for (int i = 0; i < length; i++)
		{
			PlanePacket4 planePacket = cullingPlanePackets[i];
			float4 float2 = dot4(planePacket.Xs, planePacket.Ys, planePacket.Zs, xxxx, yyyy, zzzz) + planePacket.Distances;
			float4 float3 = dot4(@float, @float, @float, math.abs(planePacket.Xs), math.abs(planePacket.Ys), math.abs(planePacket.Zs));
			x += (int4)(float2 + float3 < 0f);
		}
		return math.csum(x) == 0;
	}

	public unsafe static void Intersect(PlanePacket4* cullingPlanePackets, int length, float3 center, float3 extents, out ulong outMask)
	{
		float4 xxxx = center.xxxx;
		float4 yyyy = center.yyyy;
		float4 zzzz = center.zzzz;
		float4 xxxx2 = extents.xxxx;
		float4 yyyy2 = extents.yyyy;
		float4 zzzz2 = extents.zzzz;
		uint4 x = 0u;
		uint4 x2 = 0u;
		uint4 trueValue = new uint4(1u, 2u, 4u, 8u);
		uint4 trueValue2 = new uint4(1u, 2u, 4u, 8u);
		for (int i = 0; i < 8; i++)
		{
			PlanePacket4 planePacket = cullingPlanePackets[i];
			float4 @float = dot4(planePacket.Xs, planePacket.Ys, planePacket.Zs, xxxx, yyyy, zzzz) + planePacket.Distances;
			float4 float2 = dot4(xxxx2, yyyy2, zzzz2, math.abs(planePacket.Xs), math.abs(planePacket.Ys), math.abs(planePacket.Zs));
			x += math.select(0u, trueValue, @float + float2 < 0f);
			trueValue <<= 4;
		}
		for (int j = 8; j < length; j++)
		{
			PlanePacket4 planePacket2 = cullingPlanePackets[j];
			float4 float3 = dot4(planePacket2.Xs, planePacket2.Ys, planePacket2.Zs, xxxx, yyyy, zzzz) + planePacket2.Distances;
			float4 float4 = dot4(xxxx2, yyyy2, zzzz2, math.abs(planePacket2.Xs), math.abs(planePacket2.Ys), math.abs(planePacket2.Zs));
			x2 += math.select(0u, trueValue2, float3 + float4 < 0f);
			trueValue2 <<= 4;
		}
		outMask = math.csum(x) | ((ulong)math.csum(x2) << 32);
	}

	public unsafe static void Intersect(PlanePacket4* cullingPlanePackets, int length, float3 center, float radius, out ulong outMask)
	{
		float4 xxxx = center.xxxx;
		float4 yyyy = center.yyyy;
		float4 zzzz = center.zzzz;
		float4 @float = new float4(radius);
		uint4 x = 0u;
		uint4 x2 = 0u;
		uint4 trueValue = new uint4(1u, 2u, 4u, 8u);
		uint4 trueValue2 = new uint4(1u, 2u, 4u, 8u);
		for (int i = 0; i < 8; i++)
		{
			PlanePacket4 planePacket = cullingPlanePackets[i];
			float4 float2 = dot4(planePacket.Xs, planePacket.Ys, planePacket.Zs, xxxx, yyyy, zzzz) + planePacket.Distances;
			float4 float3 = dot4(@float, @float, @float, math.abs(planePacket.Xs), math.abs(planePacket.Ys), math.abs(planePacket.Zs));
			x += math.select(0u, trueValue, float2 + float3 < 0f);
			trueValue <<= 4;
		}
		for (int j = 8; j < length; j++)
		{
			PlanePacket4 planePacket2 = cullingPlanePackets[j];
			float4 float4 = dot4(planePacket2.Xs, planePacket2.Ys, planePacket2.Zs, xxxx, yyyy, zzzz) + planePacket2.Distances;
			float4 float5 = dot4(@float, @float, @float, math.abs(planePacket2.Xs), math.abs(planePacket2.Ys), math.abs(planePacket2.Zs));
			x2 += math.select(0u, trueValue2, float4 + float5 < 0f);
			trueValue2 <<= 4;
		}
		outMask = math.csum(x) | ((ulong)math.csum(x2) << 32);
	}

	public unsafe static void Intersect(PlanePacket4* cullingPlanePackets, int length, float3 center, float3 extents, out uint outMask)
	{
		float4 xxxx = center.xxxx;
		float4 yyyy = center.yyyy;
		float4 zzzz = center.zzzz;
		float4 xxxx2 = extents.xxxx;
		float4 yyyy2 = extents.yyyy;
		float4 zzzz2 = extents.zzzz;
		uint4 x = 0u;
		uint4 trueValue = new uint4(1u, 2u, 4u, 8u);
		for (int i = 0; i < length; i++)
		{
			PlanePacket4 planePacket = cullingPlanePackets[i];
			float4 @float = dot4(planePacket.Xs, planePacket.Ys, planePacket.Zs, xxxx, yyyy, zzzz) + planePacket.Distances;
			float4 float2 = dot4(xxxx2, yyyy2, zzzz2, math.abs(planePacket.Xs), math.abs(planePacket.Ys), math.abs(planePacket.Zs));
			x += math.select(0u, trueValue, @float + float2 < 0f);
			trueValue <<= 4;
		}
		outMask = math.csum(x);
	}

	public unsafe static void Intersect(PlanePacket4* cullingPlanePackets, int length, float3 center, float radius, out uint outMask)
	{
		float4 xxxx = center.xxxx;
		float4 yyyy = center.yyyy;
		float4 zzzz = center.zzzz;
		float4 @float = new float4(radius);
		uint4 x = 0u;
		uint4 trueValue = new uint4(1u, 2u, 4u, 8u);
		for (int i = 0; i < length; i++)
		{
			PlanePacket4 planePacket = cullingPlanePackets[i];
			float4 float2 = dot4(planePacket.Xs, planePacket.Ys, planePacket.Zs, xxxx, yyyy, zzzz) + planePacket.Distances;
			float4 float3 = dot4(@float, @float, @float, math.abs(planePacket.Xs), math.abs(planePacket.Ys), math.abs(planePacket.Zs));
			x += math.select(0u, trueValue, float2 + float3 < 0f);
			trueValue <<= 4;
		}
		outMask = math.csum(x);
	}
}
