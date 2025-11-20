using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Common;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Prefabs;

[CompilerGenerated]
public class LotInitializeSystem : GameSystemBase
{
	[BurstCompile]
	private struct InitializeLotPrefabsJob : IJobChunk
	{
		[ReadOnly]
		public BufferTypeHandle<SubObject> m_SubObjectType;

		public ComponentTypeHandle<AreaGeometryData> m_AreaGeometryType;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_ObjectGeometryData;

		[ReadOnly]
		public BufferLookup<PlaceholderObjectElement> m_PlaceholderObjectElements;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<AreaGeometryData> nativeArray = chunk.GetNativeArray(ref m_AreaGeometryType);
			BufferAccessor<SubObject> bufferAccessor = chunk.GetBufferAccessor(ref m_SubObjectType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				AreaGeometryData value = nativeArray[i];
				value.m_MaxHeight = 0f;
				if (CollectionUtils.TryGet(bufferAccessor, i, out var value2))
				{
					for (int j = 0; j < value2.Length; j++)
					{
						SubObject subObject = value2[j];
						if ((subObject.m_Flags & SubObjectFlags.EdgePlacement) != 0)
						{
							continue;
						}
						ObjectGeometryData componentData2;
						if (m_PlaceholderObjectElements.TryGetBuffer(subObject.m_Prefab, out var bufferData))
						{
							for (int k = 0; k < bufferData.Length; k++)
							{
								PlaceholderObjectElement placeholderObjectElement = bufferData[k];
								if (m_ObjectGeometryData.TryGetComponent(placeholderObjectElement.m_Object, out var componentData))
								{
									value.m_MaxHeight = math.max(value.m_MaxHeight, componentData.m_Bounds.max.y);
								}
							}
						}
						else if (m_ObjectGeometryData.TryGetComponent(subObject.m_Prefab, out componentData2))
						{
							value.m_MaxHeight = math.max(value.m_MaxHeight, componentData2.m_Bounds.max.y);
						}
					}
				}
				nativeArray[i] = value;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public BufferTypeHandle<SubObject> __Game_Prefabs_SubObject_RO_BufferTypeHandle;

		public ComponentTypeHandle<AreaGeometryData> __Game_Prefabs_AreaGeometryData_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<PlaceholderObjectElement> __Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_SubObject_RO_BufferTypeHandle = state.GetBufferTypeHandle<SubObject>(isReadOnly: true);
			__Game_Prefabs_AreaGeometryData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<AreaGeometryData>();
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup = state.GetBufferLookup<PlaceholderObjectElement>(isReadOnly: true);
		}
	}

	private PrefabSystem m_PrefabSystem;

	private EntityQuery m_PrefabQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_PrefabQuery = GetEntityQuery(ComponentType.ReadOnly<Created>(), ComponentType.ReadOnly<PrefabData>(), ComponentType.ReadOnly<LotData>(), ComponentType.ReadWrite<AreaGeometryData>());
		RequireAnyForUpdate(m_PrefabQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependency = JobChunkExtensions.ScheduleParallel(new InitializeLotPrefabsJob
		{
			m_SubObjectType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_SubObject_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_AreaGeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_AreaGeometryData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PlaceholderObjectElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup, ref base.CheckedStateRef)
		}, m_PrefabQuery, base.Dependency);
		base.Dependency = dependency;
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
	public LotInitializeSystem()
	{
	}
}
