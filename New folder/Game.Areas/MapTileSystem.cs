using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Prefabs;
using Game.Serialization;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Areas;

[CompilerGenerated]
public class MapTileSystem : GameSystemBase, IDefaultSerializable, ISerializable, IPostDeserialize
{
	[BurstCompile]
	private struct GenerateMapTilesJob : IJobParallelFor
	{
		[DeallocateOnJobCompletion]
		[ReadOnly]
		public NativeArray<Entity> m_Entities;

		[ReadOnly]
		public Entity m_Prefab;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Area> m_AreaData;

		[NativeDisableParallelForRestriction]
		public BufferLookup<Node> m_NodeData;

		public void Execute(int index)
		{
			Entity entity = m_Entities[index];
			m_PrefabRefData[entity] = new PrefabRef(m_Prefab);
			m_AreaData[entity] = new Area(AreaFlags.Complete);
			DynamicBuffer<Node> dynamicBuffer = m_NodeData[entity];
			int2 @int = new int2(index % 23, index / 23);
			float2 @float = new float2(23f, 23f) * 311.65216f;
			Bounds2 bounds = default(Bounds2);
			bounds.min = (float2)@int * 623.3043f - @float;
			bounds.max = (float2)(@int + 1) * 623.3043f - @float;
			dynamicBuffer.ResizeUninitialized(4);
			dynamicBuffer[0] = new Node(new float3(bounds.min.x, 0f, bounds.min.y), float.MinValue);
			dynamicBuffer[1] = new Node(new float3(bounds.min.x, 0f, bounds.max.y), float.MinValue);
			dynamicBuffer[2] = new Node(new float3(bounds.max.x, 0f, bounds.max.y), float.MinValue);
			dynamicBuffer[3] = new Node(new float3(bounds.max.x, 0f, bounds.min.y), float.MinValue);
		}
	}

	private struct TypeHandle
	{
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RW_ComponentLookup;

		public ComponentLookup<Area> __Game_Areas_Area_RW_ComponentLookup;

		public BufferLookup<Node> __Game_Areas_Node_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabRef_RW_ComponentLookup = state.GetComponentLookup<PrefabRef>();
			__Game_Areas_Area_RW_ComponentLookup = state.GetComponentLookup<Area>();
			__Game_Areas_Node_RW_BufferLookup = state.GetBufferLookup<Node>();
		}
	}

	private const int LEGACY_GRID_WIDTH = 23;

	private const int LEGACY_GRID_LENGTH = 23;

	private const float LEGACY_CELL_SIZE = 623.3043f;

	private EntityQuery m_PrefabQuery;

	private EntityQuery m_MapTileQuery;

	private EntityQuery m_DeletedMapTileQuery;

	private NativeList<Entity> m_StartTiles;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabQuery = GetEntityQuery(ComponentType.ReadOnly<MapTileData>(), ComponentType.ReadOnly<AreaData>(), ComponentType.ReadOnly<PrefabData>(), ComponentType.Exclude<Locked>());
		m_MapTileQuery = GetEntityQuery(ComponentType.ReadOnly<MapTile>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_DeletedMapTileQuery = GetEntityQuery(ComponentType.ReadOnly<MapTile>(), ComponentType.ReadOnly<Deleted>(), ComponentType.Exclude<Temp>());
		m_StartTiles = new NativeList<Entity>(Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_StartTiles.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_DeletedMapTileQuery.IsEmptyIgnoreFilter)
		{
			return;
		}
		base.Dependency.Complete();
		foreach (Entity item in m_DeletedMapTileQuery.ToEntityArray(Allocator.Temp))
		{
			int num = m_StartTiles.IndexOf(item);
			if (num >= 0)
			{
				m_StartTiles.RemoveAtSwapBack(num);
			}
		}
	}

	public void PostDeserialize(Context context)
	{
		if (context.purpose == Purpose.NewGame)
		{
			if (context.version >= Version.editorMapTiles)
			{
				for (int i = 0; i < m_StartTiles.Length; i++)
				{
					if (m_StartTiles[i] == Entity.Null)
					{
						m_StartTiles.RemoveAtSwapBack(i);
					}
				}
				if (m_StartTiles.Length != 0)
				{
					base.EntityManager.RemoveComponent<Native>(m_StartTiles.AsArray());
				}
			}
			else
			{
				LegacyGenerateMapTiles(editorMode: false);
			}
		}
		else if (context.purpose == Purpose.NewMap)
		{
			LegacyGenerateMapTiles(editorMode: true);
		}
	}

	public NativeList<Entity> GetStartTiles()
	{
		return m_StartTiles;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int length = m_StartTiles.Length;
		writer.Write(length);
		for (int i = 0; i < m_StartTiles.Length; i++)
		{
			Entity value = m_StartTiles[i];
			writer.Write(value);
		}
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out int value);
		m_StartTiles.ResizeUninitialized(value);
		for (int i = 0; i < value; i++)
		{
			reader.Read(out Entity value2);
			m_StartTiles[i] = value2;
		}
	}

	public void SetDefaults(Context context)
	{
		m_StartTiles.Clear();
	}

	private void LegacyGenerateMapTiles(bool editorMode)
	{
		if (!m_MapTileQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.DestroyEntity(m_MapTileQuery);
		}
		m_StartTiles.Clear();
		NativeArray<Entity> nativeArray = m_PrefabQuery.ToEntityArray(Allocator.TempJob);
		try
		{
			Entity entity = nativeArray[0];
			AreaData componentData = base.EntityManager.GetComponentData<AreaData>(entity);
			int entityCount = 529;
			NativeArray<Entity> entities = base.EntityManager.CreateEntity(componentData.m_Archetype, entityCount, Allocator.TempJob);
			if (!editorMode)
			{
				base.EntityManager.AddComponent<Native>(entities);
			}
			AddOwner(new int2(10, 10), entities);
			AddOwner(new int2(11, 10), entities);
			AddOwner(new int2(12, 10), entities);
			AddOwner(new int2(10, 11), entities);
			AddOwner(new int2(11, 11), entities);
			AddOwner(new int2(12, 11), entities);
			AddOwner(new int2(10, 12), entities);
			AddOwner(new int2(11, 12), entities);
			AddOwner(new int2(12, 12), entities);
			IJobParallelForExtensions.Schedule(new GenerateMapTilesJob
			{
				m_Entities = entities,
				m_Prefab = entity,
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RW_ComponentLookup, ref base.CheckedStateRef),
				m_AreaData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Area_RW_ComponentLookup, ref base.CheckedStateRef),
				m_NodeData = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RW_BufferLookup, ref base.CheckedStateRef)
			}, entities.Length, 4).Complete();
		}
		finally
		{
			nativeArray.Dispose();
		}
	}

	private void AddOwner(int2 tile, NativeArray<Entity> entities)
	{
		int index = tile.y * 23 + tile.x;
		base.EntityManager.RemoveComponent<Native>(entities[index]);
		m_StartTiles.Add(entities[index]);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		new EntityQueryBuilder(Allocator.Temp).Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public MapTileSystem()
	{
	}
}
