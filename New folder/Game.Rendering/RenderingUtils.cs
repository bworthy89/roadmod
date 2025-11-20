using System;
using System.Linq;
using Colossal.Mathematics;
using Game.Prefabs;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Rendering;

public static class RenderingUtils
{
	public static Matrix4x4 ToMatrix4x4(float4x4 matrix)
	{
		Matrix4x4 result = default(Matrix4x4);
		result.SetColumn(0, matrix.c0);
		result.SetColumn(1, matrix.c1);
		result.SetColumn(2, matrix.c2);
		result.SetColumn(3, matrix.c3);
		return result;
	}

	public static Color ToColor(float4 vector)
	{
		return new Color(vector.x, vector.y, vector.z, vector.w);
	}

	public static float4 Lerp(float4 c0, float4 c0_5, float4 c1, float t)
	{
		if (t <= 0.5f)
		{
			return math.lerp(c0, c0_5, t * 2f);
		}
		return math.lerp(c0_5, c1, t * 2f - 1f);
	}

	public static Bounds ToBounds(Bounds3 bounds)
	{
		return new Bounds(MathUtils.Center(bounds), MathUtils.Size(bounds));
	}

	public static float4 CalculateLodParameters(float lodFactor, BatchCullingContext cullingContext)
	{
		return CalculateLodParameters(lodFactor, cullingContext.lodParameters);
	}

	public static float4 CalculateLodParameters(float lodFactor, LODParameters lodParameters)
	{
		float num = 1f / math.tan(math.radians(lodParameters.fieldOfView * 0.5f));
		lodFactor *= 540f * num;
		return new float4(lodFactor, 1f / (lodFactor * lodFactor), num + 1f, num);
	}

	public static float CalculateMinDistance(Bounds3 bounds, float3 cameraPosition, float3 cameraDirection, float4 lodParameters)
	{
		float3 x = bounds.min - cameraPosition;
		float3 @float = bounds.max - cameraPosition;
		float num = math.length(math.max(0f, math.max(x, -@float)));
		x *= cameraDirection;
		@float *= cameraDirection;
		return num * lodParameters.z - lodParameters.w * math.clamp(math.csum(math.max(x, @float)), 0f, num);
	}

	public static float CalculateMaxDistance(Bounds3 bounds, float3 cameraPosition, float3 cameraDirection, float4 lodParameters)
	{
		float3 @float = bounds.min - cameraPosition;
		float3 y = bounds.max - cameraPosition;
		float num = math.length(math.max(-@float, y));
		@float *= cameraDirection;
		y *= cameraDirection;
		return num * lodParameters.z - lodParameters.w * math.clamp(math.csum(math.min(@float, y)), 0f, num);
	}

	public static int CalculateLodLimit(float metersPerPixel, float bias)
	{
		metersPerPixel *= math.pow(2f, 0f - bias);
		return CalculateLodLimit(metersPerPixel);
	}

	public static int CalculateLodLimit(float metersPerPixel)
	{
		float num = metersPerPixel * metersPerPixel;
		return (255 - (math.asint(num * num * num) >> 23)) & 0xFF;
	}

	public static int CalculateLod(float distanceSq, float4 lodParameters)
	{
		distanceSq *= lodParameters.y;
		return (255 - (math.asint(distanceSq * distanceSq * distanceSq) >> 23)) & 0xFF;
	}

	public static float CalculateDistanceFactor(int lod)
	{
		return math.pow(2f, (float)(128 - lod) * (1f / 6f));
	}

	public static float CalculateDistance(int lod, float4 lodParameters)
	{
		return CalculateDistanceFactor(lod) * lodParameters.x;
	}

	public static Bounds3 SafeBounds(Bounds3 bounds)
	{
		float3 @float = math.min(0f, MathUtils.Size(bounds) - 0.01f);
		bounds.min += @float;
		bounds.max -= @float;
		return bounds;
	}

	public static float GetRenderingSize(float3 size)
	{
		return math.csum(size) * (1f / 3f);
	}

	public static float GetRenderingSize(float3 size, float indexCount)
	{
		return math.csum(size) * 0.57735026f * math.rsqrt(indexCount);
	}

	public static float GetRenderingSize(float2 size)
	{
		return GetRenderingSize(new float3(size, math.cmax(size * new float2(8f, 4f))));
	}

	public static float GetShadowRenderingSize(float2 size)
	{
		return GetRenderingSize(new float3(size, math.cmax(size * new float2(2f, 2f))));
	}

	public static float GetRenderingSize(float2 size, float indexFactor)
	{
		float num = math.cmax(size * new float2(8f, 4f));
		float indexCount = indexFactor * num;
		return GetRenderingSize(new float3(size, num), indexCount);
	}

	public static float GetRenderingSize(float3 boundsSize, StackDirection stackDirection)
	{
		return stackDirection switch
		{
			StackDirection.Right => GetRenderingSize(boundsSize.zy), 
			StackDirection.Up => GetRenderingSize(new float2(math.cmax(boundsSize.xz), math.cmin(boundsSize.xz))), 
			StackDirection.Forward => GetRenderingSize(boundsSize.xy), 
			_ => GetRenderingSize(boundsSize), 
		};
	}

	public static float GetShadowRenderingSize(float3 boundsSize, StackDirection stackDirection)
	{
		return stackDirection switch
		{
			StackDirection.Right => GetShadowRenderingSize(boundsSize.zy), 
			StackDirection.Up => GetShadowRenderingSize(new float2(math.cmax(boundsSize.xz), math.cmin(boundsSize.xz))), 
			StackDirection.Forward => GetShadowRenderingSize(boundsSize.xy), 
			_ => GetRenderingSize(boundsSize), 
		};
	}

	public static float GetRenderingSize(float3 boundsSize, float3 meshSize, float indexCount, StackDirection stackDirection)
	{
		switch (stackDirection)
		{
		case StackDirection.Right:
		{
			float indexFactor3 = indexCount / math.max(1f, meshSize.x);
			return GetRenderingSize(boundsSize.zy, indexFactor3);
		}
		case StackDirection.Up:
		{
			float indexFactor2 = indexCount / math.max(1f, meshSize.y);
			return GetRenderingSize(new float2(math.cmax(boundsSize.xz), math.cmin(boundsSize.xz)), indexFactor2);
		}
		case StackDirection.Forward:
		{
			float indexFactor = indexCount / math.max(1f, meshSize.z);
			return GetRenderingSize(boundsSize.xy, indexFactor);
		}
		default:
			return GetRenderingSize(boundsSize, indexCount);
		}
	}

	public static int2 FindBoneIndex(Entity prefab, ref float3 position, ref quaternion rotation, int boneID, ref BufferLookup<SubMesh> subMeshBuffers, ref BufferLookup<ProceduralBone> proceduralBoneBuffers)
	{
		if (boneID > 0 && subMeshBuffers.TryGetBuffer(prefab, out var bufferData) && boneID >= bufferData.Length)
		{
			int num = 0;
			int2 result = default(int2);
			for (int i = 0; i < bufferData.Length; i++)
			{
				SubMesh subMesh = bufferData[i];
				if (proceduralBoneBuffers.TryGetBuffer(subMesh.m_SubMesh, out var bufferData2))
				{
					for (int j = 0; j < bufferData2.Length; j++)
					{
						ProceduralBone proceduralBone = bufferData2[j];
						if (proceduralBone.m_ConnectionID == boneID)
						{
							if ((subMesh.m_Flags & SubMeshFlags.HasTransform) != 0)
							{
								proceduralBone.m_ObjectPosition = subMesh.m_Position + math.rotate(subMesh.m_Rotation, proceduralBone.m_ObjectPosition);
								proceduralBone.m_ObjectRotation = math.mul(subMesh.m_Rotation, proceduralBone.m_ObjectRotation);
							}
							float4x4 a = float4x4.TRS(proceduralBone.m_ObjectPosition, proceduralBone.m_ObjectRotation, 1f);
							a = math.inverse(math.mul(a, proceduralBone.m_BindPose));
							position = math.transform(a, position);
							float3 forward = math.rotate(a, math.forward(rotation));
							float3 up = math.rotate(a, math.mul(rotation, math.up()));
							rotation = quaternion.LookRotation(forward, up);
							result.x = num + j;
							result.y = math.select(-1, i, (subMesh.m_Flags & SubMeshFlags.HasTransform) != 0);
							return result;
						}
					}
					num += bufferData2.Length;
				}
				boneID++;
			}
		}
		return -1;
	}

	public static BlendWeight GetBlendWeight(CharacterGroup.IndexWeight indexWeight)
	{
		return new BlendWeight
		{
			m_Index = indexWeight.index,
			m_Weight = indexWeight.weight
		};
	}

	public static BlendWeights GetBlendWeights(CharacterGroup.IndexWeight8 indexWeight8)
	{
		return new BlendWeights
		{
			m_Weight0 = GetBlendWeight(indexWeight8.w0),
			m_Weight1 = GetBlendWeight(indexWeight8.w1),
			m_Weight2 = GetBlendWeight(indexWeight8.w2),
			m_Weight3 = GetBlendWeight(indexWeight8.w3),
			m_Weight4 = GetBlendWeight(indexWeight8.w4),
			m_Weight5 = GetBlendWeight(indexWeight8.w5),
			m_Weight6 = GetBlendWeight(indexWeight8.w6),
			m_Weight7 = GetBlendWeight(indexWeight8.w7)
		};
	}

	public static OverlayAtlasElement[] GetOverlayAtlasElements(RenderPrefab renderPrefab)
	{
		return (renderPrefab.GetComponent<CharacterProperties>()?.m_Overlays)?.Select((CharacterOverlay o) => new OverlayAtlasElement(o.m_sourceRegion, o.m_targetRegion)).ToArray();
	}

	public static OverlayAtlasElement[] GetOverlayAtlasElements(RenderPrefab[] renderPrefabs)
	{
		OverlayAtlasElement[] array = Array.Empty<OverlayAtlasElement>();
		foreach (RenderPrefab renderPrefab in renderPrefabs)
		{
			array = array.Concat(GetOverlayAtlasElements(renderPrefab) ?? new OverlayAtlasElement[0]).ToArray();
		}
		return array;
	}
}
