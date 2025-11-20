using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Common;

public static class CommonUtils
{
	public static Entity GetRandomEntity(ref Random random, NativeArray<ArchetypeChunk> chunks, EntityTypeHandle entityType)
	{
		int num = 0;
		for (int i = 0; i < chunks.Length; i++)
		{
			num += chunks[i].Count;
		}
		if (num == 0)
		{
			return Entity.Null;
		}
		num = random.NextInt(num);
		for (int j = 0; j < chunks.Length; j++)
		{
			ArchetypeChunk archetypeChunk = chunks[j];
			if (num < archetypeChunk.Count)
			{
				return archetypeChunk.GetNativeArray(entityType)[num];
			}
			num -= archetypeChunk.Count;
		}
		return Entity.Null;
	}

	public static Entity GetRandomEntity<T>(ref Random random, NativeArray<ArchetypeChunk> chunks, EntityTypeHandle entityType, ComponentTypeHandle<T> componentType, out T componentData) where T : unmanaged, IComponentData
	{
		componentData = default(T);
		int num = 0;
		for (int i = 0; i < chunks.Length; i++)
		{
			num += chunks[i].Count;
		}
		if (num == 0)
		{
			return Entity.Null;
		}
		num = random.NextInt(num);
		for (int j = 0; j < chunks.Length; j++)
		{
			ArchetypeChunk archetypeChunk = chunks[j];
			if (num < archetypeChunk.Count)
			{
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(entityType);
				componentData = archetypeChunk.GetNativeArray(ref componentType)[num];
				return nativeArray[num];
			}
			num -= archetypeChunk.Count;
		}
		return Entity.Null;
	}

	public static Entity GetRandomEntity<T1, T2>(ref Random random, NativeArray<ArchetypeChunk> chunks, EntityTypeHandle entityType, ComponentTypeHandle<T1> componentType1, ComponentTypeHandle<T2> componentType2, out T1 componentData1, out T2 componentData2) where T1 : unmanaged, IComponentData where T2 : unmanaged, IComponentData
	{
		componentData1 = default(T1);
		componentData2 = default(T2);
		int num = 0;
		for (int i = 0; i < chunks.Length; i++)
		{
			num += chunks[i].Count;
		}
		if (num == 0)
		{
			return Entity.Null;
		}
		num = random.NextInt(num);
		for (int j = 0; j < chunks.Length; j++)
		{
			ArchetypeChunk archetypeChunk = chunks[j];
			if (num < archetypeChunk.Count)
			{
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(entityType);
				NativeArray<T1> nativeArray2 = archetypeChunk.GetNativeArray(ref componentType1);
				NativeArray<T2> nativeArray3 = archetypeChunk.GetNativeArray(ref componentType2);
				componentData1 = nativeArray2[num];
				componentData2 = nativeArray3[num];
				return nativeArray[num];
			}
			num -= archetypeChunk.Count;
		}
		return Entity.Null;
	}

	public static void Swap<T>(ref T a, ref T b)
	{
		T val = a;
		a = b;
		b = val;
	}

	public static void SwapBits(ref uint bitMask, uint a, uint b)
	{
		uint2 @uint = math.select(0u, new uint2(b, a), (bitMask & new uint2(a, b)) != 0u);
		bitMask = (bitMask & ~(a | b)) | @uint.x | @uint.y;
	}

	public static void SwapBits(ref uint bitMask1, ref uint bitMask2, uint bits)
	{
		uint num = bitMask1;
		bitMask1 = (bitMask1 & ~bits) | (bitMask2 & bits);
		bitMask2 = (bitMask2 & ~bits) | (num & bits);
	}

	public static BoundsMask GetBoundsMask(MeshLayer meshLayers)
	{
		BoundsMask boundsMask = (BoundsMask)0;
		if ((meshLayers & (MeshLayer.Default | MeshLayer.Moving | MeshLayer.Tunnel | MeshLayer.Marker)) != 0)
		{
			boundsMask |= BoundsMask.NormalLayers;
		}
		if ((meshLayers & MeshLayer.Pipeline) != 0)
		{
			boundsMask |= BoundsMask.PipelineLayer;
		}
		if ((meshLayers & MeshLayer.SubPipeline) != 0)
		{
			boundsMask |= BoundsMask.SubPipelineLayer;
		}
		if ((meshLayers & MeshLayer.Waterway) != 0)
		{
			boundsMask |= BoundsMask.WaterwayLayer;
		}
		return boundsMask;
	}

	public static bool ExclusiveGroundCollision(CollisionMask mask1, CollisionMask mask2)
	{
		if ((mask1 & mask2 & CollisionMask.OnGround) != 0)
		{
			return ((mask1 | mask2) & CollisionMask.ExclusiveGround) != 0;
		}
		return false;
	}
}
