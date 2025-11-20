using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Creatures;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Rendering;

[CompilerGenerated]
public class MeshGroupSystem : GameSystemBase
{
	[BurstCompile]
	private struct SetMeshGroupsJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<PseudoRandomSeed> m_PseudoRandomSeedType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<Human> m_HumanType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentVehicle> m_CurrentVehicleType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<TransformFrame> m_TransformFrameType;

		public BufferTypeHandle<MeshGroup> m_MeshGroupType;

		public BufferTypeHandle<MeshBatch> m_MeshBatchType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<Human> m_HumanData;

		[ReadOnly]
		public ComponentLookup<CurrentVehicle> m_CurrentVehicleData;

		[ReadOnly]
		public BufferLookup<SubMeshGroup> m_SubMeshGroups;

		[ReadOnly]
		public BufferLookup<SubMesh> m_SubMeshes;

		[ReadOnly]
		public BufferLookup<OverlayElement> m_OverlayElements;

		[ReadOnly]
		public BufferLookup<ActivityLocationElement> m_ActivityLocations;

		[ReadOnly]
		public BufferLookup<TransformFrame> m_TransformFrames;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<PseudoRandomSeed> nativeArray = chunk.GetNativeArray(ref m_PseudoRandomSeedType);
			NativeArray<Temp> nativeArray2 = chunk.GetNativeArray(ref m_TempType);
			NativeArray<Human> nativeArray3 = chunk.GetNativeArray(ref m_HumanType);
			NativeArray<CurrentVehicle> nativeArray4 = chunk.GetNativeArray(ref m_CurrentVehicleType);
			NativeArray<PrefabRef> nativeArray5 = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<MeshGroup> bufferAccessor = chunk.GetBufferAccessor(ref m_MeshGroupType);
			BufferAccessor<MeshBatch> bufferAccessor2 = chunk.GetBufferAccessor(ref m_MeshBatchType);
			BufferAccessor<TransformFrame> bufferAccessor3 = chunk.GetBufferAccessor(ref m_TransformFrameType);
			Unity.Mathematics.Random random = ((nativeArray.Length != 0) ? default(Unity.Mathematics.Random) : m_RandomSeed.GetRandom(unfilteredChunkIndex));
			NativeList<MeshGroup> nativeList = default(NativeList<MeshGroup>);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				PrefabRef prefabRef = nativeArray5[i];
				DynamicBuffer<MeshGroup> newGroups = bufferAccessor[i];
				DynamicBuffer<MeshBatch> batches = bufferAccessor2[i];
				MeshGroup oldGroup = default(MeshGroup);
				int length = newGroups.Length;
				if (length > 1)
				{
					if (!nativeList.IsCreated)
					{
						nativeList = new NativeList<MeshGroup>(length, Allocator.Temp);
					}
					nativeList.AddRange(newGroups.AsNativeArray());
				}
				else if (length == 1)
				{
					oldGroup = newGroups[0];
				}
				newGroups.Clear();
				if (m_SubMeshGroups.TryGetBuffer(prefabRef.m_Prefab, out var bufferData))
				{
					DynamicBuffer<SubMesh> dynamicBuffer = m_SubMeshes[prefabRef.m_Prefab];
					int num = 0;
					int num2 = 0;
					int num3 = 0;
					if (CollectionUtils.TryGet(nativeArray, i, out var value))
					{
						random = value.GetRandom(PseudoRandomSeed.kMeshGroup);
					}
					MeshGroupFlags meshGroupFlags = (MeshGroupFlags)0u;
					if (CollectionUtils.TryGet(nativeArray2, i, out var value2) && value2.m_Original != Entity.Null)
					{
						if (m_HumanData.TryGetComponent(value2.m_Original, out var componentData))
						{
							meshGroupFlags |= GetHumanFlags(componentData);
						}
						if (m_CurrentVehicleData.TryGetComponent(value2.m_Original, out var componentData2))
						{
							meshGroupFlags |= GetCurrentVehicleFlags(componentData2);
						}
						else
						{
							meshGroupFlags |= MeshGroupFlags.ForbidMotorcycle;
							meshGroupFlags |= MeshGroupFlags.ForbidBicycle;
						}
						meshGroupFlags = ((!m_TransformFrames.TryGetBuffer(value2.m_Original, out var bufferData2)) ? (meshGroupFlags | MeshGroupFlags.ForbidFishing) : (meshGroupFlags | GetCurrentPropActivityFlags(bufferData2)));
					}
					else
					{
						if (CollectionUtils.TryGet(nativeArray3, i, out var value3))
						{
							meshGroupFlags |= GetHumanFlags(value3);
						}
						if (CollectionUtils.TryGet(nativeArray4, i, out var value4))
						{
							meshGroupFlags |= GetCurrentVehicleFlags(value4);
						}
						else
						{
							meshGroupFlags |= MeshGroupFlags.ForbidMotorcycle;
							meshGroupFlags |= MeshGroupFlags.ForbidBicycle;
						}
						meshGroupFlags = ((!CollectionUtils.TryGet(bufferAccessor3, i, out var value5)) ? (meshGroupFlags | MeshGroupFlags.ForbidFishing) : (meshGroupFlags | GetCurrentPropActivityFlags(value5)));
					}
					while (num < bufferData.Length)
					{
						SubMeshGroup subMeshGroup = bufferData[num];
						int num4 = num;
						num += subMeshGroup.m_SubGroupCount;
						if (subMeshGroup.m_SubGroupCount <= 0 || num + subMeshGroup.m_SubGroupCount >= 65536)
						{
							throw new Exception("Invalid m_SubGroupCount!");
						}
						if ((subMeshGroup.m_Flags & meshGroupFlags) != subMeshGroup.m_Flags)
						{
							continue;
						}
						num4 += random.NextInt(subMeshGroup.m_SubGroupCount);
						subMeshGroup = bufferData[num4];
						newGroups.Add(new MeshGroup
						{
							m_SubMeshGroup = (ushort)num4,
							m_MeshOffset = (byte)num2,
							m_ColorOffset = (byte)num3
						});
						num2 += subMeshGroup.m_SubMeshRange.y - subMeshGroup.m_SubMeshRange.x;
						num3 += subMeshGroup.m_SubMeshRange.y - subMeshGroup.m_SubMeshRange.x;
						for (int j = subMeshGroup.m_SubMeshRange.x; j < subMeshGroup.m_SubMeshRange.y; j++)
						{
							SubMesh subMesh = dynamicBuffer[j];
							if (m_OverlayElements.HasBuffer(subMesh.m_SubMesh))
							{
								num3 += 8;
								break;
							}
						}
					}
				}
				else
				{
					newGroups.Add(new MeshGroup
					{
						m_SubMeshGroup = ushort.MaxValue,
						m_MeshOffset = 0
					});
				}
				if (length > 1)
				{
					for (int k = 0; k < nativeList.Length; k++)
					{
						TryRemoveBatches(nativeList[k], k, newGroups, batches);
					}
					nativeList.Clear();
				}
				else if (length == 1)
				{
					TryRemoveBatches(oldGroup, 0, newGroups, batches);
				}
			}
			if (nativeList.IsCreated)
			{
				nativeList.Dispose();
			}
		}

		private void TryRemoveBatches(MeshGroup oldGroup, int groupIndex, DynamicBuffer<MeshGroup> newGroups, DynamicBuffer<MeshBatch> batches)
		{
			for (int i = 0; i < newGroups.Length; i++)
			{
				if (newGroups[i].m_SubMeshGroup == oldGroup.m_SubMeshGroup)
				{
					return;
				}
			}
			for (int j = 0; j < batches.Length; j++)
			{
				MeshBatch value = batches[j];
				if (value.m_MeshGroup == groupIndex)
				{
					value.m_MeshGroup = byte.MaxValue;
					value.m_MeshIndex = byte.MaxValue;
					value.m_TileIndex = byte.MaxValue;
					batches[j] = value;
				}
			}
		}

		public static MeshGroupFlags GetHumanFlags(Human human)
		{
			MeshGroupFlags meshGroupFlags = (MeshGroupFlags)0u;
			meshGroupFlags = (((human.m_Flags & HumanFlags.Cold) == 0) ? (meshGroupFlags | MeshGroupFlags.RequireWarm) : (meshGroupFlags | MeshGroupFlags.RequireCold));
			if ((human.m_Flags & HumanFlags.Homeless) != 0)
			{
				return meshGroupFlags | MeshGroupFlags.RequireHomeless;
			}
			return meshGroupFlags | MeshGroupFlags.RequireHome;
		}

		private MeshGroupFlags GetCurrentVehicleFlags(CurrentVehicle currentVehicle)
		{
			MeshGroupFlags meshGroupFlags = MeshGroupFlags.ForbidMotorcycle | MeshGroupFlags.ForbidBicycle;
			if (m_PrefabRefData.TryGetComponent(currentVehicle.m_Vehicle, out var componentData) && m_ActivityLocations.TryGetBuffer(componentData.m_Prefab, out var bufferData))
			{
				ActivityMask activityMask = new ActivityMask(ActivityType.Driving);
				ActivityMask activityMask2 = new ActivityMask(ActivityType.Biking);
				for (int i = 0; i < bufferData.Length; i++)
				{
					ActivityLocationElement activityLocationElement = bufferData[i];
					if ((activityLocationElement.m_ActivityMask.m_Mask & activityMask.m_Mask) != 0)
					{
						meshGroupFlags = (MeshGroupFlags)((uint)meshGroupFlags & 0xFFFFFFDFu);
						meshGroupFlags |= MeshGroupFlags.RequireMotorcycle;
					}
					if ((activityLocationElement.m_ActivityMask.m_Mask & activityMask2.m_Mask) != 0)
					{
						meshGroupFlags = (MeshGroupFlags)((uint)meshGroupFlags & 0xFFFFFDFFu);
						meshGroupFlags |= MeshGroupFlags.RequireBicycle;
					}
				}
			}
			return meshGroupFlags;
		}

		private MeshGroupFlags GetCurrentPropActivityFlags(DynamicBuffer<TransformFrame> transformFrames)
		{
			MeshGroupFlags meshGroupFlags = MeshGroupFlags.ForbidFishing;
			for (int i = 0; i < transformFrames.Length; i++)
			{
				if (transformFrames[i].m_Activity == 24)
				{
					meshGroupFlags = (MeshGroupFlags)((uint)meshGroupFlags & 0xFFFFFF7Fu);
					meshGroupFlags |= MeshGroupFlags.RequireFishing;
				}
			}
			return meshGroupFlags;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<PseudoRandomSeed> __Game_Common_PseudoRandomSeed_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Human> __Game_Creatures_Human_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CurrentVehicle> __Game_Creatures_CurrentVehicle_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<TransformFrame> __Game_Objects_TransformFrame_RO_BufferTypeHandle;

		public BufferTypeHandle<MeshGroup> __Game_Rendering_MeshGroup_RW_BufferTypeHandle;

		public BufferTypeHandle<MeshBatch> __Game_Rendering_MeshBatch_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Human> __Game_Creatures_Human_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentVehicle> __Game_Creatures_CurrentVehicle_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<SubMeshGroup> __Game_Prefabs_SubMeshGroup_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubMesh> __Game_Prefabs_SubMesh_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<OverlayElement> __Game_Prefabs_OverlayElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ActivityLocationElement> __Game_Prefabs_ActivityLocationElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<TransformFrame> __Game_Objects_TransformFrame_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Common_PseudoRandomSeed_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PseudoRandomSeed>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Creatures_Human_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Human>(isReadOnly: true);
			__Game_Creatures_CurrentVehicle_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentVehicle>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Objects_TransformFrame_RO_BufferTypeHandle = state.GetBufferTypeHandle<TransformFrame>(isReadOnly: true);
			__Game_Rendering_MeshGroup_RW_BufferTypeHandle = state.GetBufferTypeHandle<MeshGroup>();
			__Game_Rendering_MeshBatch_RW_BufferTypeHandle = state.GetBufferTypeHandle<MeshBatch>();
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Creatures_Human_RO_ComponentLookup = state.GetComponentLookup<Human>(isReadOnly: true);
			__Game_Creatures_CurrentVehicle_RO_ComponentLookup = state.GetComponentLookup<CurrentVehicle>(isReadOnly: true);
			__Game_Prefabs_SubMeshGroup_RO_BufferLookup = state.GetBufferLookup<SubMeshGroup>(isReadOnly: true);
			__Game_Prefabs_SubMesh_RO_BufferLookup = state.GetBufferLookup<SubMesh>(isReadOnly: true);
			__Game_Prefabs_OverlayElement_RO_BufferLookup = state.GetBufferLookup<OverlayElement>(isReadOnly: true);
			__Game_Prefabs_ActivityLocationElement_RO_BufferLookup = state.GetBufferLookup<ActivityLocationElement>(isReadOnly: true);
			__Game_Objects_TransformFrame_RO_BufferLookup = state.GetBufferLookup<TransformFrame>(isReadOnly: true);
		}
	}

	private EntityQuery m_UpdateQuery;

	private EntityQuery m_AllQuery;

	private bool m_Loaded;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_UpdateQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<MeshGroup>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<BatchesUpdated>()
			}
		});
		m_AllQuery = GetEntityQuery(ComponentType.ReadOnly<MeshGroup>());
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		m_Loaded = true;
	}

	private bool GetLoaded()
	{
		if (m_Loaded)
		{
			m_Loaded = false;
			return true;
		}
		return false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		EntityQuery query = (GetLoaded() ? m_AllQuery : m_UpdateQuery);
		if (!query.IsEmptyIgnoreFilter)
		{
			JobHandle dependency = JobChunkExtensions.ScheduleParallel(new SetMeshGroupsJob
			{
				m_PseudoRandomSeedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_PseudoRandomSeed_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_HumanType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_Human_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CurrentVehicleType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_CurrentVehicle_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TransformFrameType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Objects_TransformFrame_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_MeshGroupType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Rendering_MeshGroup_RW_BufferTypeHandle, ref base.CheckedStateRef),
				m_MeshBatchType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Rendering_MeshBatch_RW_BufferTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HumanData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_Human_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CurrentVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_CurrentVehicle_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SubMeshGroups = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMeshGroup_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubMeshes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMesh_RO_BufferLookup, ref base.CheckedStateRef),
				m_OverlayElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_OverlayElement_RO_BufferLookup, ref base.CheckedStateRef),
				m_ActivityLocations = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ActivityLocationElement_RO_BufferLookup, ref base.CheckedStateRef),
				m_TransformFrames = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_TransformFrame_RO_BufferLookup, ref base.CheckedStateRef),
				m_RandomSeed = RandomSeed.Next()
			}, query, base.Dependency);
			base.Dependency = dependency;
		}
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
	public MeshGroupSystem()
	{
	}
}
