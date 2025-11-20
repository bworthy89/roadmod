using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Rendering;
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
public class ObjectInitializeSystem : GameSystemBase
{
	[BurstCompile]
	private struct FixPlaceholdersJob : IJobChunk
	{
		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		public BufferTypeHandle<PlaceholderObjectElement> m_PlaceholderObjectElementType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			BufferAccessor<PlaceholderObjectElement> bufferAccessor = chunk.GetBufferAccessor(ref m_PlaceholderObjectElementType);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				DynamicBuffer<PlaceholderObjectElement> dynamicBuffer = bufferAccessor[i];
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					if (m_DeletedData.HasComponent(dynamicBuffer[j].m_Object))
					{
						dynamicBuffer.RemoveAtSwapBack(j--);
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct InitializeSubNetsJob : IJobParallelFor
	{
		[ReadOnly]
		public NativeArray<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		public ComponentTypeHandle<PlaceableObjectData> m_PlaceableObjectDataType;

		public BufferTypeHandle<SubNet> m_SubNetType;

		[ReadOnly]
		public ComponentLookup<NetData> m_NetData;

		public void Execute(int index)
		{
			ArchetypeChunk archetypeChunk = m_Chunks[index];
			if (archetypeChunk.Has(ref m_DeletedType))
			{
				return;
			}
			NativeArray<PlaceableObjectData> nativeArray = archetypeChunk.GetNativeArray(ref m_PlaceableObjectDataType);
			BufferAccessor<SubNet> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_SubNetType);
			NativeList<int> nativeList = default(NativeList<int>);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				DynamicBuffer<SubNet> dynamicBuffer = bufferAccessor[i];
				Game.Objects.PlacementFlags placementFlags = Game.Objects.PlacementFlags.None;
				if (dynamicBuffer.Length != 0)
				{
					if (!nativeList.IsCreated)
					{
						nativeList = new NativeList<int>(dynamicBuffer.Length * 2, Allocator.Temp);
					}
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						ref SubNet reference = ref dynamicBuffer.ElementAt(j);
						if (reference.m_NodeIndex.x >= 0)
						{
							while (nativeList.Length <= reference.m_NodeIndex.x)
							{
								nativeList.Add(0);
							}
							nativeList[reference.m_NodeIndex.x]++;
						}
						if (reference.m_NodeIndex.y >= 0 && reference.m_NodeIndex.y != reference.m_NodeIndex.x)
						{
							while (nativeList.Length <= reference.m_NodeIndex.y)
							{
								nativeList.Add(0);
							}
							nativeList[reference.m_NodeIndex.y]++;
						}
					}
					for (int k = 0; k < dynamicBuffer.Length; k++)
					{
						ref SubNet reference2 = ref dynamicBuffer.ElementAt(k);
						if (reference2.m_NodeIndex.x >= 0 && nativeList[reference2.m_NodeIndex.x] == 1)
						{
							reference2.m_Snapping.x = GetEnableSnapping(reference2.m_Prefab);
						}
						else
						{
							reference2.m_Snapping.x = false;
						}
						if (reference2.m_NodeIndex.y >= 0 && reference2.m_NodeIndex.y != reference2.m_NodeIndex.x && nativeList[reference2.m_NodeIndex.y] == 1)
						{
							reference2.m_Snapping.y = GetEnableSnapping(reference2.m_Prefab);
						}
						else
						{
							reference2.m_Snapping.y = false;
						}
						if (math.any(reference2.m_Snapping))
						{
							placementFlags |= Game.Objects.PlacementFlags.SubNetSnap;
						}
					}
					nativeList.Clear();
				}
				if (nativeArray.Length != 0)
				{
					nativeArray.ElementAt(i).m_Flags |= placementFlags;
				}
			}
			if (nativeList.IsCreated)
			{
				nativeList.Dispose();
			}
		}

		private bool GetEnableSnapping(Entity prefab)
		{
			if (m_NetData.TryGetComponent(prefab, out var componentData))
			{
				return (componentData.m_RequiredLayers & (Layer.MarkerPathway | Layer.MarkerTaxiway)) == 0;
			}
			return false;
		}
	}

	[BurstCompile]
	private struct FindPlaceholderRequirementsJob : IJobParallelFor
	{
		[ReadOnly]
		public NativeArray<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		[ReadOnly]
		public BufferTypeHandle<PlaceholderObjectElement> m_PlaceholderObjectElementType;

		public ComponentTypeHandle<PlaceholderObjectData> m_PlaceholderObjectDataType;

		[ReadOnly]
		public BufferLookup<ObjectRequirementElement> m_ObjectRequirementElements;

		public void Execute(int index)
		{
			ArchetypeChunk archetypeChunk = m_Chunks[index];
			if (archetypeChunk.Has(ref m_DeletedType))
			{
				return;
			}
			NativeArray<PlaceholderObjectData> nativeArray = archetypeChunk.GetNativeArray(ref m_PlaceholderObjectDataType);
			BufferAccessor<PlaceholderObjectElement> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_PlaceholderObjectElementType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				ref PlaceholderObjectData reference = ref nativeArray.ElementAt(i);
				DynamicBuffer<PlaceholderObjectElement> dynamicBuffer = bufferAccessor[i];
				ObjectRequirementFlags objectRequirementFlags = (ObjectRequirementFlags)0;
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					PlaceholderObjectElement placeholderObjectElement = dynamicBuffer[j];
					if (m_ObjectRequirementElements.TryGetBuffer(placeholderObjectElement.m_Object, out var bufferData))
					{
						for (int k = 0; k < bufferData.Length; k++)
						{
							ObjectRequirementElement objectRequirementElement = bufferData[k];
							objectRequirementFlags |= objectRequirementElement.m_RequireFlags;
							objectRequirementFlags |= objectRequirementElement.m_ForbidFlags;
						}
					}
				}
				reference.m_RequirementMask = objectRequirementFlags;
			}
		}
	}

	[BurstCompile]
	private struct FindSubObjectRequirementsJob : IJobParallelFor
	{
		[ReadOnly]
		public NativeArray<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		[ReadOnly]
		public BufferTypeHandle<SubObject> m_SubObjectType;

		public ComponentTypeHandle<ObjectGeometryData> m_ObjectGeometryDataType;

		[ReadOnly]
		public ComponentLookup<PlaceholderObjectData> m_PlaceholderObjectData;

		[ReadOnly]
		public BufferLookup<SubObject> m_SubObjects;

		public void Execute(int index)
		{
			ArchetypeChunk archetypeChunk = m_Chunks[index];
			if (!archetypeChunk.Has(ref m_DeletedType))
			{
				NativeArray<ObjectGeometryData> nativeArray = archetypeChunk.GetNativeArray(ref m_ObjectGeometryDataType);
				BufferAccessor<SubObject> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_SubObjectType);
				for (int i = 0; i < bufferAccessor.Length; i++)
				{
					ref ObjectGeometryData reference = ref nativeArray.ElementAt(i);
					DynamicBuffer<SubObject> subObjects = bufferAccessor[i];
					reference.m_SubObjectMask = GetRequirementMask(subObjects);
				}
			}
		}

		private ObjectRequirementFlags GetRequirementMask(DynamicBuffer<SubObject> subObjects)
		{
			ObjectRequirementFlags objectRequirementFlags = (ObjectRequirementFlags)0;
			for (int i = 0; i < subObjects.Length; i++)
			{
				SubObject subObject = subObjects[i];
				DynamicBuffer<SubObject> bufferData;
				if (m_PlaceholderObjectData.TryGetComponent(subObject.m_Prefab, out var componentData))
				{
					objectRequirementFlags |= componentData.m_RequirementMask;
				}
				else if (m_SubObjects.TryGetBuffer(subObject.m_Prefab, out bufferData))
				{
					objectRequirementFlags |= GetRequirementMask(bufferData);
				}
			}
			return objectRequirementFlags;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> __Game_Common_Deleted_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<UtilityObjectData> __Game_Prefabs_UtilityObjectData_RO_ComponentTypeHandle;

		public ComponentTypeHandle<AnimalData> __Game_Prefabs_AnimalData_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PillarData> __Game_Prefabs_PillarData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<MoveableBridgeData> __Game_Prefabs_MoveableBridgeData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<NetObjectData> __Game_Prefabs_NetObjectData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PlantData> __Game_Prefabs_PlantData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<HumanData> __Game_Prefabs_HumanData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<VehicleData> __Game_Prefabs_VehicleData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ActivityPropData> __Game_Prefabs_ActivityPropData_RO_ComponentTypeHandle;

		public ComponentTypeHandle<BuildingExtensionData> __Game_Prefabs_BuildingExtensionData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<PlaceableObjectData> __Game_Prefabs_PlaceableObjectData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<SpawnableObjectData> __Game_Prefabs_SpawnableObjectData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<AssetStampData> __Game_Prefabs_AssetStampData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<GrowthScaleData> __Game_Prefabs_GrowthScaleData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<StackData> __Game_Prefabs_StackData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<QuantityObjectData> __Game_Prefabs_QuantityObjectData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<CreatureData> __Game_Prefabs_CreatureData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<BuildingTerraformData> __Game_Prefabs_BuildingTerraformData_RW_ComponentTypeHandle;

		public BufferTypeHandle<SubMesh> __Game_Prefabs_SubMesh_RW_BufferTypeHandle;

		public BufferTypeHandle<SubMeshGroup> __Game_Prefabs_SubMeshGroup_RW_BufferTypeHandle;

		public BufferTypeHandle<CharacterElement> __Game_Prefabs_CharacterElement_RW_BufferTypeHandle;

		public BufferTypeHandle<SubObject> __Game_Prefabs_SubObject_RW_BufferTypeHandle;

		public BufferTypeHandle<SubNet> __Game_Prefabs_SubNet_RW_BufferTypeHandle;

		public BufferTypeHandle<SubLane> __Game_Prefabs_SubLane_RW_BufferTypeHandle;

		public BufferTypeHandle<SubArea> __Game_Prefabs_SubArea_RW_BufferTypeHandle;

		public BufferTypeHandle<SubAreaNode> __Game_Prefabs_SubAreaNode_RW_BufferTypeHandle;

		[ReadOnly]
		public BufferLookup<AnimationClip> __Game_Prefabs_AnimationClip_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<AnimationMotion> __Game_Prefabs_AnimationMotion_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		public BufferTypeHandle<PlaceholderObjectElement> __Game_Prefabs_PlaceholderObjectElement_RW_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<PlaceholderObjectElement> __Game_Prefabs_PlaceholderObjectElement_RO_BufferTypeHandle;

		public ComponentTypeHandle<PlaceholderObjectData> __Game_Prefabs_PlaceholderObjectData_RW_ComponentTypeHandle;

		[ReadOnly]
		public BufferLookup<ObjectRequirementElement> __Game_Prefabs_ObjectRequirementElement_RO_BufferLookup;

		[ReadOnly]
		public BufferTypeHandle<SubObject> __Game_Prefabs_SubObject_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<PlaceholderObjectData> __Game_Prefabs_PlaceholderObjectData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<SubObject> __Game_Prefabs_SubObject_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<NetData> __Game_Prefabs_NetData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
			__Game_Prefabs_PrefabData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabData>(isReadOnly: true);
			__Game_Prefabs_UtilityObjectData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<UtilityObjectData>(isReadOnly: true);
			__Game_Prefabs_AnimalData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<AnimalData>();
			__Game_Prefabs_PillarData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PillarData>(isReadOnly: true);
			__Game_Prefabs_MoveableBridgeData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<MoveableBridgeData>(isReadOnly: true);
			__Game_Prefabs_NetObjectData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<NetObjectData>(isReadOnly: true);
			__Game_Prefabs_PlantData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PlantData>(isReadOnly: true);
			__Game_Prefabs_HumanData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HumanData>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingData>(isReadOnly: true);
			__Game_Prefabs_VehicleData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<VehicleData>(isReadOnly: true);
			__Game_Prefabs_ActivityPropData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ActivityPropData>(isReadOnly: true);
			__Game_Prefabs_BuildingExtensionData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingExtensionData>();
			__Game_Prefabs_ObjectGeometryData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ObjectGeometryData>();
			__Game_Prefabs_PlaceableObjectData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PlaceableObjectData>();
			__Game_Prefabs_SpawnableObjectData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<SpawnableObjectData>();
			__Game_Prefabs_AssetStampData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<AssetStampData>();
			__Game_Prefabs_GrowthScaleData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<GrowthScaleData>();
			__Game_Prefabs_StackData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<StackData>();
			__Game_Prefabs_QuantityObjectData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<QuantityObjectData>();
			__Game_Prefabs_CreatureData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<CreatureData>();
			__Game_Prefabs_BuildingTerraformData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingTerraformData>();
			__Game_Prefabs_SubMesh_RW_BufferTypeHandle = state.GetBufferTypeHandle<SubMesh>();
			__Game_Prefabs_SubMeshGroup_RW_BufferTypeHandle = state.GetBufferTypeHandle<SubMeshGroup>();
			__Game_Prefabs_CharacterElement_RW_BufferTypeHandle = state.GetBufferTypeHandle<CharacterElement>();
			__Game_Prefabs_SubObject_RW_BufferTypeHandle = state.GetBufferTypeHandle<SubObject>();
			__Game_Prefabs_SubNet_RW_BufferTypeHandle = state.GetBufferTypeHandle<SubNet>();
			__Game_Prefabs_SubLane_RW_BufferTypeHandle = state.GetBufferTypeHandle<SubLane>();
			__Game_Prefabs_SubArea_RW_BufferTypeHandle = state.GetBufferTypeHandle<SubArea>();
			__Game_Prefabs_SubAreaNode_RW_BufferTypeHandle = state.GetBufferTypeHandle<SubAreaNode>();
			__Game_Prefabs_AnimationClip_RO_BufferLookup = state.GetBufferLookup<AnimationClip>(isReadOnly: true);
			__Game_Prefabs_AnimationMotion_RO_BufferLookup = state.GetBufferLookup<AnimationMotion>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Prefabs_PlaceholderObjectElement_RW_BufferTypeHandle = state.GetBufferTypeHandle<PlaceholderObjectElement>();
			__Game_Prefabs_PlaceholderObjectElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<PlaceholderObjectElement>(isReadOnly: true);
			__Game_Prefabs_PlaceholderObjectData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PlaceholderObjectData>();
			__Game_Prefabs_ObjectRequirementElement_RO_BufferLookup = state.GetBufferLookup<ObjectRequirementElement>(isReadOnly: true);
			__Game_Prefabs_SubObject_RO_BufferTypeHandle = state.GetBufferTypeHandle<SubObject>(isReadOnly: true);
			__Game_Prefabs_PlaceholderObjectData_RO_ComponentLookup = state.GetComponentLookup<PlaceholderObjectData>(isReadOnly: true);
			__Game_Prefabs_SubObject_RO_BufferLookup = state.GetBufferLookup<SubObject>(isReadOnly: true);
			__Game_Prefabs_NetData_RO_ComponentLookup = state.GetComponentLookup<NetData>(isReadOnly: true);
		}
	}

	private EntityQuery m_PrefabQuery;

	private EntityQuery m_PlaceholderQuery;

	private PrefabSystem m_PrefabSystem;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_PrefabQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<PrefabData>(),
				ComponentType.ReadOnly<ObjectData>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_PlaceholderQuery = GetEntityQuery(ComponentType.ReadOnly<ObjectData>(), ComponentType.ReadOnly<PlaceholderObjectElement>(), ComponentType.Exclude<Deleted>());
		RequireForUpdate(m_PrefabQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeArray<ArchetypeChunk> chunks = m_PrefabQuery.ToArchetypeChunkArray(Allocator.TempJob);
		bool flag = false;
		try
		{
			EntityTypeHandle entityTypeHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<Deleted> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<PrefabData> typeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<UtilityObjectData> typeHandle3 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_UtilityObjectData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<AnimalData> typeHandle4 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_AnimalData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<PillarData> typeHandle5 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PillarData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<MoveableBridgeData> typeHandle6 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_MoveableBridgeData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<NetObjectData> typeHandle7 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_NetObjectData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<PlantData> typeHandle8 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PlantData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<HumanData> typeHandle9 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_HumanData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<BuildingData> typeHandle10 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<VehicleData> typeHandle11 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_VehicleData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<ActivityPropData> typeHandle12 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ActivityPropData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<BuildingExtensionData> typeHandle13 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_BuildingExtensionData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<ObjectGeometryData> typeHandle14 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<PlaceableObjectData> typeHandle15 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PlaceableObjectData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<SpawnableObjectData> typeHandle16 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_SpawnableObjectData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<AssetStampData> typeHandle17 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_AssetStampData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<GrowthScaleData> typeHandle18 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_GrowthScaleData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<StackData> typeHandle19 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_StackData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<QuantityObjectData> typeHandle20 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_QuantityObjectData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<CreatureData> typeHandle21 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_CreatureData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<BuildingTerraformData> typeHandle22 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_BuildingTerraformData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			BufferTypeHandle<SubMesh> bufferTypeHandle = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_SubMesh_RW_BufferTypeHandle, ref base.CheckedStateRef);
			BufferTypeHandle<SubMeshGroup> bufferTypeHandle2 = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_SubMeshGroup_RW_BufferTypeHandle, ref base.CheckedStateRef);
			BufferTypeHandle<CharacterElement> bufferTypeHandle3 = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_CharacterElement_RW_BufferTypeHandle, ref base.CheckedStateRef);
			BufferTypeHandle<SubObject> bufferTypeHandle4 = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_SubObject_RW_BufferTypeHandle, ref base.CheckedStateRef);
			BufferTypeHandle<SubNet> bufferTypeHandle5 = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_SubNet_RW_BufferTypeHandle, ref base.CheckedStateRef);
			BufferTypeHandle<SubLane> bufferTypeHandle6 = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_SubLane_RW_BufferTypeHandle, ref base.CheckedStateRef);
			BufferTypeHandle<SubArea> bufferTypeHandle7 = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_SubArea_RW_BufferTypeHandle, ref base.CheckedStateRef);
			BufferTypeHandle<SubAreaNode> bufferTypeHandle8 = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_SubAreaNode_RW_BufferTypeHandle, ref base.CheckedStateRef);
			BufferLookup<AnimationClip> bufferLookup = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_AnimationClip_RO_BufferLookup, ref base.CheckedStateRef);
			BufferLookup<AnimationMotion> bufferLookup2 = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_AnimationMotion_RO_BufferLookup, ref base.CheckedStateRef);
			CompleteDependency();
			SubArea elem3 = default(SubArea);
			for (int i = 0; i < chunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = chunks[i];
				if (archetypeChunk.Has(ref typeHandle))
				{
					flag |= archetypeChunk.Has(ref typeHandle16);
					continue;
				}
				NativeArray<PrefabData> nativeArray = archetypeChunk.GetNativeArray(ref typeHandle2);
				NativeArray<ObjectGeometryData> nativeArray2 = archetypeChunk.GetNativeArray(ref typeHandle14);
				NativeArray<PlaceableObjectData> nativeArray3 = archetypeChunk.GetNativeArray(ref typeHandle15);
				NativeArray<SpawnableObjectData> nativeArray4 = archetypeChunk.GetNativeArray(ref typeHandle16);
				NativeArray<AssetStampData> nativeArray5 = archetypeChunk.GetNativeArray(ref typeHandle17);
				NativeArray<GrowthScaleData> nativeArray6 = archetypeChunk.GetNativeArray(ref typeHandle18);
				NativeArray<StackData> nativeArray7 = archetypeChunk.GetNativeArray(ref typeHandle19);
				NativeArray<QuantityObjectData> nativeArray8 = archetypeChunk.GetNativeArray(ref typeHandle20);
				NativeArray<CreatureData> nativeArray9 = archetypeChunk.GetNativeArray(ref typeHandle21);
				NativeArray<PillarData> nativeArray10 = archetypeChunk.GetNativeArray(ref typeHandle5);
				NativeArray<BuildingTerraformData> nativeArray11 = archetypeChunk.GetNativeArray(ref typeHandle22);
				NativeArray<BuildingExtensionData> nativeArray12 = archetypeChunk.GetNativeArray(ref typeHandle13);
				NativeArray<AnimalData> nativeArray13 = archetypeChunk.GetNativeArray(ref typeHandle4);
				BufferAccessor<SubObject> bufferAccessor = archetypeChunk.GetBufferAccessor(ref bufferTypeHandle4);
				BufferAccessor<SubArea> bufferAccessor2 = archetypeChunk.GetBufferAccessor(ref bufferTypeHandle7);
				BufferAccessor<SubMesh> bufferAccessor3 = archetypeChunk.GetBufferAccessor(ref bufferTypeHandle);
				BufferAccessor<SubMeshGroup> bufferAccessor4 = archetypeChunk.GetBufferAccessor(ref bufferTypeHandle2);
				BufferAccessor<CharacterElement> bufferAccessor5 = archetypeChunk.GetBufferAccessor(ref bufferTypeHandle3);
				BufferAccessor<SubNet> bufferAccessor6 = archetypeChunk.GetBufferAccessor(ref bufferTypeHandle5);
				BufferAccessor<SubLane> bufferAccessor7 = archetypeChunk.GetBufferAccessor(ref bufferTypeHandle6);
				bool flag2 = archetypeChunk.Has(ref typeHandle3);
				bool flag3 = archetypeChunk.Has(ref typeHandle6);
				bool flag4 = archetypeChunk.Has(ref typeHandle7);
				bool isPlantObject = archetypeChunk.Has(ref typeHandle8);
				bool flag5 = archetypeChunk.Has(ref typeHandle9);
				bool flag6 = archetypeChunk.Has(ref typeHandle10) || nativeArray12.Length != 0;
				bool isVehicleObject = archetypeChunk.Has(ref typeHandle11);
				bool isCreatureObject = nativeArray9.Length != 0;
				bool isActivityPropObject = archetypeChunk.Has(ref typeHandle12);
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					ObjectPrefab prefab = m_PrefabSystem.GetPrefab<ObjectPrefab>(nativeArray[j]);
					ObjectGeometryData objectGeometryData = nativeArray2[j];
					objectGeometryData.m_MinLod = 255;
					objectGeometryData.m_Layers = (MeshLayer)0;
					PlaceableObjectData placeableObjectData = default(PlaceableObjectData);
					if (nativeArray3.Length != 0)
					{
						placeableObjectData = nativeArray3[j];
					}
					GrowthScaleData growthScaleData = default(GrowthScaleData);
					if (nativeArray6.Length != 0)
					{
						growthScaleData = nativeArray6[j];
					}
					StackData stackData = default(StackData);
					if (nativeArray7.Length != 0)
					{
						stackData = nativeArray7[j];
						stackData.m_FirstBounds = new Bounds1(float.MaxValue, float.MinValue);
						stackData.m_MiddleBounds = new Bounds1(float.MaxValue, float.MinValue);
						stackData.m_LastBounds = new Bounds1(float.MaxValue, float.MinValue);
					}
					QuantityObjectData quantityObjectData = default(QuantityObjectData);
					if (nativeArray8.Length != 0)
					{
						quantityObjectData = nativeArray8[j];
					}
					CreatureData creatureData = default(CreatureData);
					if (nativeArray9.Length != 0)
					{
						creatureData = nativeArray9[j];
						CreaturePrefab creaturePrefab = prefab as CreaturePrefab;
						creatureData.m_Gender = creaturePrefab.m_Gender;
						if (!flag5)
						{
							objectGeometryData.m_Flags |= Game.Objects.GeometryFlags.LowCollisionPriority;
						}
					}
					if (prefab is AssetStampPrefab)
					{
						AssetStampData assetStampData = nativeArray5[j];
						InitializePrefab(prefab as AssetStampPrefab, ref assetStampData, ref placeableObjectData, ref objectGeometryData);
						nativeArray5[j] = assetStampData;
						objectGeometryData.m_Flags |= Game.Objects.GeometryFlags.ExclusiveGround | Game.Objects.GeometryFlags.WalkThrough | Game.Objects.GeometryFlags.OccupyZone | Game.Objects.GeometryFlags.Stampable | Game.Objects.GeometryFlags.HasLot;
					}
					else if (prefab is ObjectGeometryPrefab)
					{
						CollectionUtils.TryGet(bufferAccessor4, j, out var value);
						CollectionUtils.TryGet(bufferAccessor5, j, out var value2);
						InitializePrefab(prefab as ObjectGeometryPrefab, placeableObjectData, ref objectGeometryData, ref growthScaleData, ref stackData, ref quantityObjectData, ref creatureData, bufferAccessor3[j], value, value2, isPlantObject, flag5, flag6, isVehicleObject, isCreatureObject, isActivityPropObject);
						if (nativeArray10.Length != 0)
						{
							if (nativeArray10[j].m_Type == PillarType.Horizontal)
							{
								objectGeometryData.m_Flags |= Game.Objects.GeometryFlags.IgnoreBottomCollision;
							}
							else
							{
								objectGeometryData.m_Flags |= Game.Objects.GeometryFlags.BaseCollision;
							}
							if (flag3)
							{
								objectGeometryData.m_Flags |= Game.Objects.GeometryFlags.WalkThrough;
							}
							objectGeometryData.m_Flags |= Game.Objects.GeometryFlags.OccupyZone | Game.Objects.GeometryFlags.CanSubmerge | Game.Objects.GeometryFlags.OptionalAttach;
						}
						else if (flag2)
						{
							objectGeometryData.m_Flags |= Game.Objects.GeometryFlags.IgnoreSecondaryCollision;
						}
						else if (flag4)
						{
							objectGeometryData.m_Flags |= Game.Objects.GeometryFlags.OccupyZone;
						}
						else
						{
							objectGeometryData.m_Flags |= Game.Objects.GeometryFlags.Overridable | Game.Objects.GeometryFlags.Brushable;
						}
						if (archetypeChunk.Has(ref typeHandle4))
						{
							AnimalData value3 = nativeArray13[j];
							if (value3.m_FlySpeed > 0f && !value2.IsEmpty)
							{
								CharacterElement characterElement = value2.ElementAt(0);
								DynamicBuffer<AnimationMotion> motions = bufferLookup2[characterElement.m_Style];
								bufferLookup.TryGetBuffer(characterElement.m_Style, out var bufferData);
								for (int k = 0; k < bufferData.Length; k++)
								{
									AnimationClip animationClip = bufferData[k];
									if (animationClip.m_Activity == ActivityType.Flying && animationClip.m_Type == AnimationType.End && animationClip.m_MotionRange.x != animationClip.m_MotionRange.y)
									{
										ObjectUtils.GetRootMotion(motions, animationClip.m_MotionRange, default(BlendWeights), 0f, out var rootOffset, out var rootVelocity, out var rootRotation);
										ObjectUtils.GetRootMotion(motions, animationClip.m_MotionRange, default(BlendWeights), 1f, out var rootOffset2, out rootVelocity, out rootRotation);
										value3.m_LandingOffset = rootOffset - rootOffset2;
										nativeArray13[j] = value3;
									}
								}
							}
						}
					}
					else if (prefab is MarkerObjectPrefab)
					{
						InitializePrefab(prefab as MarkerObjectPrefab, placeableObjectData, ref objectGeometryData, bufferAccessor3[j]);
						placeableObjectData.m_Flags |= Game.Objects.PlacementFlags.CanOverlap;
						objectGeometryData.m_Flags |= Game.Objects.GeometryFlags.WalkThrough;
					}
					if (!flag6 && nativeArray11.Length != 0)
					{
						BuildingTerraformOverride component = prefab.GetComponent<BuildingTerraformOverride>();
						Bounds2 bounds;
						if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Standing) != Game.Objects.GeometryFlags.None)
						{
							float2 xz = objectGeometryData.m_Pivot.xz;
							float2 @float = objectGeometryData.m_LegSize.xz * 0.5f + objectGeometryData.m_LegOffset;
							bounds = new Bounds2(xz - @float, xz + @float);
						}
						else
						{
							bounds = objectGeometryData.m_Bounds.xz;
						}
						BuildingTerraformData buildingTerraformData = nativeArray11[j];
						BuildingInitializeSystem.InitializeTerraformData(component, ref buildingTerraformData, bounds, bounds);
						nativeArray11[j] = buildingTerraformData;
					}
					nativeArray2[j] = objectGeometryData;
					if (nativeArray3.Length != 0)
					{
						nativeArray3[j] = placeableObjectData;
					}
					if (nativeArray6.Length != 0)
					{
						nativeArray6[j] = growthScaleData;
					}
					if (nativeArray7.Length != 0)
					{
						if (stackData.m_FirstBounds.min > stackData.m_FirstBounds.max)
						{
							stackData.m_FirstBounds = default(Bounds1);
						}
						if (stackData.m_MiddleBounds.min > stackData.m_MiddleBounds.max)
						{
							stackData.m_MiddleBounds = default(Bounds1);
						}
						if (stackData.m_LastBounds.min > stackData.m_LastBounds.max)
						{
							stackData.m_LastBounds = default(Bounds1);
						}
						nativeArray7[j] = stackData;
					}
					if (nativeArray8.Length != 0)
					{
						nativeArray8[j] = quantityObjectData;
					}
					if (nativeArray9.Length != 0)
					{
						nativeArray9[j] = creatureData;
					}
				}
				for (int l = 0; l < bufferAccessor.Length; l++)
				{
					ObjectSubObjects component2 = m_PrefabSystem.GetPrefab<ObjectPrefab>(nativeArray[l]).GetComponent<ObjectSubObjects>();
					if (component2.m_SubObjects == null)
					{
						continue;
					}
					bool flag7 = !flag6 && flag4 && nativeArray3.Length != 0 && (nativeArray3[l].m_Flags & Game.Objects.PlacementFlags.RoadEdge) != 0;
					DynamicBuffer<SubObject> dynamicBuffer = bufferAccessor[l];
					for (int m = 0; m < component2.m_SubObjects.Length; m++)
					{
						ObjectSubObjectInfo objectSubObjectInfo = component2.m_SubObjects[m];
						ObjectPrefab objectPrefab = objectSubObjectInfo.m_Object;
						if (objectPrefab == null || !m_PrefabSystem.TryGetEntity(objectPrefab, out var entity))
						{
							continue;
						}
						SubObject elem = new SubObject
						{
							m_Prefab = entity,
							m_Position = objectSubObjectInfo.m_Position,
							m_Rotation = objectSubObjectInfo.m_Rotation,
							m_ParentIndex = objectSubObjectInfo.m_ParentMesh,
							m_GroupIndex = objectSubObjectInfo.m_GroupIndex,
							m_Probability = math.select(objectSubObjectInfo.m_Probability, 100, objectSubObjectInfo.m_Probability == 0)
						};
						if (objectSubObjectInfo.m_ParentMesh == -1)
						{
							if (flag7)
							{
								elem.m_Flags |= SubObjectFlags.OnAttachedParent;
							}
							else
							{
								elem.m_Flags |= SubObjectFlags.OnGround;
							}
						}
						dynamicBuffer.Add(elem);
					}
				}
				for (int n = 0; n < bufferAccessor6.Length; n++)
				{
					ObjectPrefab prefab2 = m_PrefabSystem.GetPrefab<ObjectPrefab>(nativeArray[n]);
					ObjectSubNets component3 = prefab2.GetComponent<ObjectSubNets>();
					if (component3.m_SubNets == null)
					{
						continue;
					}
					bool flag8 = false;
					DynamicBuffer<SubNet> dynamicBuffer2 = bufferAccessor6[n];
					for (int num = 0; num < component3.m_SubNets.Length; num++)
					{
						ObjectSubNetInfo objectSubNetInfo = component3.m_SubNets[num];
						NetPrefab netPrefab = objectSubNetInfo.m_NetPrefab;
						if (!(netPrefab == null) && m_PrefabSystem.TryGetEntity(netPrefab, out var entity2))
						{
							SubNet elem2 = new SubNet
							{
								m_Prefab = entity2,
								m_Curve = objectSubNetInfo.m_BezierCurve,
								m_NodeIndex = objectSubNetInfo.m_NodeIndex,
								m_InvertMode = component3.m_InvertWhen,
								m_ParentMesh = objectSubNetInfo.m_ParentMesh
							};
							if (MathUtils.Min(objectSubNetInfo.m_BezierCurve).y <= -2f)
							{
								flag8 = true;
							}
							NetCompositionHelpers.GetRequirementFlags(objectSubNetInfo.m_Upgrades, out elem2.m_Upgrades, out var sectionFlags);
							if (sectionFlags != 0)
							{
								COSystemBase.baseLog.ErrorFormat(prefab2, "ObjectSubNets ({0}[{1}]) cannot upgrade section flags: {2}", prefab2.name, num, sectionFlags);
							}
							dynamicBuffer2.Add(elem2);
						}
					}
					if (flag8)
					{
						if (nativeArray3.Length != 0)
						{
							PlaceableObjectData value4 = nativeArray3[n];
							value4.m_Flags |= Game.Objects.PlacementFlags.HasUndergroundElements;
							nativeArray3[n] = value4;
						}
						if (nativeArray12.Length != 0)
						{
							BuildingExtensionData value5 = nativeArray12[n];
							value5.m_HasUndergroundElements = true;
							nativeArray12[n] = value5;
						}
					}
				}
				for (int num2 = 0; num2 < bufferAccessor7.Length; num2++)
				{
					ObjectSubLanes component4 = m_PrefabSystem.GetPrefab<ObjectPrefab>(nativeArray[num2]).GetComponent<ObjectSubLanes>();
					if (component4.m_SubLanes == null)
					{
						continue;
					}
					DynamicBuffer<SubLane> dynamicBuffer3 = bufferAccessor7[num2];
					for (int num3 = 0; num3 < component4.m_SubLanes.Length; num3++)
					{
						ObjectSubLaneInfo objectSubLaneInfo = component4.m_SubLanes[num3];
						NetLanePrefab lanePrefab = objectSubLaneInfo.m_LanePrefab;
						if (lanePrefab == null || !m_PrefabSystem.TryGetEntity(lanePrefab, out var entity3))
						{
							continue;
						}
						dynamicBuffer3.Add(new SubLane
						{
							m_Prefab = entity3,
							m_Curve = objectSubLaneInfo.m_BezierCurve,
							m_NodeIndex = objectSubLaneInfo.m_NodeIndex,
							m_ParentMesh = objectSubLaneInfo.m_ParentMesh
						});
						if (!flag6 && nativeArray2.Length != 0 && objectSubLaneInfo.m_NodeIndex.x != objectSubLaneInfo.m_NodeIndex.y)
						{
							ObjectGeometryData value6 = nativeArray2[num2];
							if ((value6.m_Flags & Game.Objects.GeometryFlags.Overridable) != Game.Objects.GeometryFlags.None)
							{
								value6.m_Flags &= ~Game.Objects.GeometryFlags.Overridable;
								value6.m_Flags |= Game.Objects.GeometryFlags.OccupyZone;
								nativeArray2[num2] = value6;
							}
						}
					}
				}
				if (bufferAccessor2.Length != 0)
				{
					BufferAccessor<SubAreaNode> bufferAccessor8 = archetypeChunk.GetBufferAccessor(ref bufferTypeHandle8);
					for (int num4 = 0; num4 < bufferAccessor2.Length; num4++)
					{
						ObjectSubAreas component5 = m_PrefabSystem.GetPrefab<ObjectPrefab>(nativeArray[num4]).GetComponent<ObjectSubAreas>();
						if (component5.m_SubAreas == null)
						{
							continue;
						}
						int num5 = 0;
						for (int num6 = 0; num6 < component5.m_SubAreas.Length; num6++)
						{
							ObjectSubAreaInfo objectSubAreaInfo = component5.m_SubAreas[num6];
							if (!(objectSubAreaInfo.m_AreaPrefab == null) && m_PrefabSystem.TryGetEntity(objectSubAreaInfo.m_AreaPrefab, out var _))
							{
								num5 += objectSubAreaInfo.m_NodePositions.Length;
							}
						}
						DynamicBuffer<SubArea> dynamicBuffer4 = bufferAccessor2[num4];
						DynamicBuffer<SubAreaNode> dynamicBuffer5 = bufferAccessor8[num4];
						dynamicBuffer4.EnsureCapacity(component5.m_SubAreas.Length);
						dynamicBuffer5.ResizeUninitialized(num5);
						num5 = 0;
						for (int num7 = 0; num7 < component5.m_SubAreas.Length; num7++)
						{
							ObjectSubAreaInfo objectSubAreaInfo2 = component5.m_SubAreas[num7];
							if (objectSubAreaInfo2.m_AreaPrefab == null || !m_PrefabSystem.TryGetEntity(objectSubAreaInfo2.m_AreaPrefab, out var entity5))
							{
								continue;
							}
							elem3.m_Prefab = entity5;
							elem3.m_NodeRange.x = num5;
							if (objectSubAreaInfo2.m_ParentMeshes != null && objectSubAreaInfo2.m_ParentMeshes.Length != 0)
							{
								for (int num8 = 0; num8 < objectSubAreaInfo2.m_NodePositions.Length; num8++)
								{
									float3 position = objectSubAreaInfo2.m_NodePositions[num8];
									int parentMesh = objectSubAreaInfo2.m_ParentMeshes[num8];
									dynamicBuffer5[num5++] = new SubAreaNode(position, parentMesh);
								}
							}
							else
							{
								for (int num9 = 0; num9 < objectSubAreaInfo2.m_NodePositions.Length; num9++)
								{
									float3 position2 = objectSubAreaInfo2.m_NodePositions[num9];
									int parentMesh2 = -1;
									dynamicBuffer5[num5++] = new SubAreaNode(position2, parentMesh2);
								}
							}
							elem3.m_NodeRange.y = num5;
							dynamicBuffer4.Add(elem3);
						}
					}
				}
				if (nativeArray4.Length == 0)
				{
					continue;
				}
				NativeArray<Entity> nativeArray14 = archetypeChunk.GetNativeArray(entityTypeHandle);
				for (int num10 = 0; num10 < nativeArray4.Length; num10++)
				{
					Entity obj = nativeArray14[num10];
					SpawnableObjectData value7 = nativeArray4[num10];
					SpawnableObject component6 = m_PrefabSystem.GetPrefab<ObjectPrefab>(nativeArray[num10]).GetComponent<SpawnableObject>();
					if (component6.m_Placeholders != null)
					{
						for (int num11 = 0; num11 < component6.m_Placeholders.Length; num11++)
						{
							ObjectPrefab objectPrefab2 = component6.m_Placeholders[num11];
							if (!(objectPrefab2 == null) && m_PrefabSystem.TryGetEntity(objectPrefab2, out var entity6))
							{
								base.EntityManager.GetBuffer<PlaceholderObjectElement>(entity6).Add(new PlaceholderObjectElement(obj));
							}
						}
					}
					if (component6.m_RandomizationGroup != null)
					{
						value7.m_RandomizationGroup = m_PrefabSystem.GetEntity(component6.m_RandomizationGroup);
					}
					value7.m_Probability = component6.m_Probability;
					nativeArray4[num10] = value7;
				}
			}
			JobHandle dependsOn = default(JobHandle);
			if (flag)
			{
				dependsOn = JobChunkExtensions.ScheduleParallel(new FixPlaceholdersJob
				{
					m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PlaceholderObjectElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_PlaceholderObjectElement_RW_BufferTypeHandle, ref base.CheckedStateRef)
				}, m_PlaceholderQuery, base.Dependency);
			}
			FindPlaceholderRequirementsJob jobData = new FindPlaceholderRequirementsJob
			{
				m_Chunks = chunks,
				m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PlaceholderObjectElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_PlaceholderObjectElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_PlaceholderObjectDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PlaceholderObjectData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ObjectRequirementElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ObjectRequirementElement_RO_BufferLookup, ref base.CheckedStateRef)
			};
			FindSubObjectRequirementsJob jobData2 = new FindSubObjectRequirementsJob
			{
				m_Chunks = chunks,
				m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_SubObjectType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_SubObject_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_ObjectGeometryDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PlaceholderObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceholderObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubObject_RO_BufferLookup, ref base.CheckedStateRef)
			};
			InitializeSubNetsJob jobData3 = new InitializeSubNetsJob
			{
				m_Chunks = chunks,
				m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PlaceableObjectDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PlaceableObjectData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_SubNetType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_SubNet_RW_BufferTypeHandle, ref base.CheckedStateRef),
				m_NetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetData_RO_ComponentLookup, ref base.CheckedStateRef)
			};
			JobHandle job = IJobParallelForExtensions.Schedule(dependsOn: IJobParallelForExtensions.Schedule(jobData, chunks.Length, 1, dependsOn), jobData: jobData2, arrayLength: chunks.Length, innerloopBatchCount: 1);
			JobHandle job2 = IJobParallelForExtensions.Schedule(jobData3, chunks.Length, 1);
			base.Dependency = JobHandle.CombineDependencies(job, job2);
		}
		finally
		{
			chunks.Dispose(base.Dependency);
		}
	}

	private void InitializePrefab(AssetStampPrefab stampPrefab, ref AssetStampData assetStampData, ref PlaceableObjectData placeableObjectData, ref ObjectGeometryData objectGeometryData)
	{
		assetStampData.m_Size = new int2(stampPrefab.m_Width, stampPrefab.m_Depth);
		placeableObjectData.m_ConstructionCost = stampPrefab.m_ConstructionCost;
		assetStampData.m_UpKeepCost = stampPrefab.m_UpKeepCost;
		float2 @float = assetStampData.m_Size;
		@float *= 8f;
		objectGeometryData.m_MinLod = math.min(objectGeometryData.m_MinLod, RenderingUtils.CalculateLodLimit(RenderingUtils.GetRenderingSize(new float3(@float.x, 0f, @float.y))));
		objectGeometryData.m_Layers = MeshLayer.Default;
		@float -= 0.4f;
		objectGeometryData.m_Size.xz = @float;
		objectGeometryData.m_Size.y = math.max(objectGeometryData.m_Size.y, 5f);
		objectGeometryData.m_Bounds.min.xz = @float * -0.5f;
		objectGeometryData.m_Bounds.min.y = math.min(objectGeometryData.m_Bounds.min.y, 0f);
		objectGeometryData.m_Bounds.max.xz = @float * 0.5f;
		objectGeometryData.m_Bounds.max.y = math.max(objectGeometryData.m_Bounds.max.y, 5f);
	}

	private static MeshGroupFlags GetMeshGroupFlag(ObjectState state, bool inverse)
	{
		if (inverse)
		{
			switch (state)
			{
			case ObjectState.Cold:
				return MeshGroupFlags.RequireWarm;
			case ObjectState.Warm:
				return MeshGroupFlags.RequireCold;
			case ObjectState.Home:
				return MeshGroupFlags.RequireHomeless;
			case ObjectState.Homeless:
				return MeshGroupFlags.RequireHome;
			case ObjectState.Motorcycle:
				return MeshGroupFlags.ForbidMotorcycle;
			case ObjectState.Fishing:
				return MeshGroupFlags.ForbidFishing;
			case ObjectState.Bicycle:
				return MeshGroupFlags.ForbidBicycle;
			}
		}
		else
		{
			switch (state)
			{
			case ObjectState.Cold:
				return MeshGroupFlags.RequireCold;
			case ObjectState.Warm:
				return MeshGroupFlags.RequireWarm;
			case ObjectState.Home:
				return MeshGroupFlags.RequireHome;
			case ObjectState.Homeless:
				return MeshGroupFlags.RequireHomeless;
			case ObjectState.Motorcycle:
				return MeshGroupFlags.RequireMotorcycle;
			case ObjectState.Fishing:
				return MeshGroupFlags.RequireFishing;
			case ObjectState.Bicycle:
				return MeshGroupFlags.RequireBicycle;
			}
		}
		return (MeshGroupFlags)0u;
	}

	private void InitializePrefab(ObjectGeometryPrefab objectPrefab, PlaceableObjectData placeableObjectData, ref ObjectGeometryData objectGeometryData, ref GrowthScaleData growthScaleData, ref StackData stackData, ref QuantityObjectData quantityObjectData, ref CreatureData creatureData, DynamicBuffer<SubMesh> meshes, DynamicBuffer<SubMeshGroup> meshGroups, DynamicBuffer<CharacterElement> characterElements, bool isPlantObject, bool isHumanObject, bool isBuildingObject, bool isVehicleObject, bool isCreatureObject, bool isActivityPropObject)
	{
		Bounds3 bounds = new Bounds3(float.MaxValue, float.MinValue);
		bool flag = false;
		Bounds3 bounds2 = default(Bounds3);
		Bounds3 bounds3 = default(Bounds3);
		Bounds3 bounds4 = default(Bounds3);
		Bounds3 bounds5 = default(Bounds3);
		Bounds3 bounds6 = default(Bounds3);
		if (objectPrefab.m_Meshes != null)
		{
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			int num5 = objectPrefab.m_Meshes.Length;
			int num6 = 0;
			int num7 = 0;
			int num8 = 0;
			int num9 = 0;
			ObjectMeshInfo objectMeshInfo = null;
			CharacterGroup.Character[] array = null;
			CharacterGroup.OverrideInfo[] array2 = null;
			CharacterGroup.OverrideInfo overrideInfo = null;
			RenderPrefab[] array3 = null;
			List<RenderPrefab> list = null;
			MeshGroupFlags meshGroupFlags = (MeshGroupFlags)0u;
			MeshGroupFlags meshGroupFlags2 = (MeshGroupFlags)0u;
			Entity entity = Entity.Null;
			while (true)
			{
				RenderPrefab renderPrefab = null;
				while (true)
				{
					if (num4 < num9)
					{
						renderPrefab = ((array3 == null) ? list[num4++] : array3[num4++]);
						break;
					}
					if (num2 < num6)
					{
						CharacterGroup.Character character = array[num2++];
						if ((character.m_Style.m_Gender & creatureData.m_Gender) != creatureData.m_Gender)
						{
							continue;
						}
						entity = m_PrefabSystem.GetEntity(character.m_Style);
						num4 = 0;
						CharacterElement elem = new CharacterElement
						{
							m_Style = entity,
							m_ShapeWeights = RenderingUtils.GetBlendWeights(character.m_Meta.shapeWeights),
							m_TextureWeights = RenderingUtils.GetBlendWeights(character.m_Meta.textureWeights),
							m_OverlayWeights = RenderingUtils.GetBlendWeights(character.m_Meta.overlayWeights),
							m_MaskWeights = RenderingUtils.GetBlendWeights(character.m_Meta.maskWeights),
							m_RestPoseClipIndex = base.EntityManager.GetComponentData<CharacterStyleData>(entity).m_RestPoseClipIndex,
							m_CorrectiveClipIndex = -1
						};
						if (overrideInfo != null)
						{
							CharacterGroup.Character character2 = overrideInfo.m_Group.m_Characters[num2 - 1];
							if (overrideInfo.m_OverrideShapeWeights)
							{
								elem.m_ShapeWeights = RenderingUtils.GetBlendWeights(character2.m_Meta.shapeWeights);
							}
							if (overrideInfo.m_overrideMaskWeights)
							{
								elem.m_MaskWeights = RenderingUtils.GetBlendWeights(character2.m_Meta.maskWeights);
							}
							if (list == null)
							{
								list = new List<RenderPrefab>();
							}
							else
							{
								list.Clear();
							}
							for (int i = 0; i < character.m_MeshPrefabs.Length; i++)
							{
								RenderPrefab renderPrefab2 = character.m_MeshPrefabs[i];
								if (!renderPrefab2.TryGet<CharacterProperties>(out var component) || (component.m_BodyParts & overrideInfo.m_OverrideBodyParts) == 0)
								{
									list.Add(renderPrefab2);
								}
							}
							for (int j = 0; j < character2.m_MeshPrefabs.Length; j++)
							{
								RenderPrefab renderPrefab3 = character2.m_MeshPrefabs[j];
								if (!renderPrefab3.TryGet<CharacterProperties>(out var component2) || (component2.m_BodyParts & overrideInfo.m_OverrideBodyParts) != 0)
								{
									list.Add(renderPrefab3);
								}
							}
							array3 = null;
							num9 = list.Count;
						}
						else
						{
							array3 = character.m_MeshPrefabs;
							num9 = array3.Length;
						}
						meshGroups.Add(new SubMeshGroup
						{
							m_SubGroupCount = num8,
							m_SubMeshRange = new int2(meshes.Length, meshes.Length + num9),
							m_Flags = (meshGroupFlags2 | GetMeshGroupFlag(objectMeshInfo.m_RequireState, inverse: false))
						});
						characterElements.Add(elem);
						continue;
					}
					if (num3 < num7)
					{
						overrideInfo = array2[num3++];
						num2 = 0;
						meshGroupFlags2 = (meshGroupFlags & ~GetMeshGroupFlag(overrideInfo.m_RequireState, inverse: true)) | GetMeshGroupFlag(overrideInfo.m_RequireState, inverse: false);
						continue;
					}
					if (num >= num5)
					{
						break;
					}
					objectMeshInfo = objectPrefab.m_Meshes[num++];
					array = null;
					if (objectMeshInfo.m_Mesh is RenderPrefab renderPrefab4)
					{
						renderPrefab = renderPrefab4;
						entity = Entity.Null;
						break;
					}
					if (!(objectMeshInfo.m_Mesh is CharacterGroup characterGroup))
					{
						continue;
					}
					array = characterGroup.m_Characters;
					array2 = characterGroup.m_Overrides;
					overrideInfo = null;
					num2 = 0;
					num6 = array.Length;
					num3 = 0;
					num7 = ((array2 != null) ? array2.Length : 0);
					num8 = 0;
					CharacterGroup.Character[] array4 = array;
					for (int k = 0; k < array4.Length; k++)
					{
						if ((array4[k].m_Style.m_Gender & creatureData.m_Gender) == creatureData.m_Gender)
						{
							num8++;
						}
					}
					meshGroupFlags = (MeshGroupFlags)0u;
					CharacterGroup.OverrideInfo[] array5 = array2;
					foreach (CharacterGroup.OverrideInfo overrideInfo2 in array5)
					{
						meshGroupFlags |= GetMeshGroupFlag(overrideInfo2.m_RequireState, inverse: true);
					}
					meshGroupFlags2 = meshGroupFlags;
				}
				if (renderPrefab == null)
				{
					break;
				}
				Entity entity2 = m_PrefabSystem.GetEntity(renderPrefab);
				MeshData componentData = base.EntityManager.GetComponentData<MeshData>(entity2);
				Bounds3 meshBounds = renderPrefab.bounds;
				float3 @float = MathUtils.Size(meshBounds);
				if (objectPrefab.m_Circular || objectMeshInfo.m_Rotation.Equals(quaternion.identity))
				{
					meshBounds += objectMeshInfo.m_Position;
				}
				else
				{
					meshBounds = MathUtils.Bounds(MathUtils.Box(meshBounds, objectMeshInfo.m_Rotation, objectMeshInfo.m_Position));
				}
				SubMeshFlags subMeshFlags = (SubMeshFlags)0u;
				switch (objectMeshInfo.m_RequireState)
				{
				case ObjectState.Child:
					subMeshFlags |= SubMeshFlags.RequireChild;
					bounds2 |= meshBounds;
					break;
				case ObjectState.Teen:
					subMeshFlags |= SubMeshFlags.RequireTeen;
					bounds3 |= meshBounds;
					break;
				case ObjectState.Adult:
					subMeshFlags |= SubMeshFlags.RequireAdult;
					bounds4 |= meshBounds;
					break;
				case ObjectState.Elderly:
					subMeshFlags |= SubMeshFlags.RequireElderly;
					bounds5 |= meshBounds;
					break;
				case ObjectState.Dead:
					subMeshFlags |= SubMeshFlags.RequireDead;
					bounds6 |= meshBounds;
					break;
				case ObjectState.Stump:
					subMeshFlags |= SubMeshFlags.RequireStump;
					break;
				case ObjectState.Empty:
					subMeshFlags |= SubMeshFlags.RequireEmpty;
					quantityObjectData.m_StepMask |= 1u;
					break;
				case ObjectState.Full:
					subMeshFlags |= SubMeshFlags.RequireFull;
					quantityObjectData.m_StepMask |= 8u;
					break;
				case ObjectState.Clear:
					subMeshFlags |= SubMeshFlags.RequireClear;
					break;
				case ObjectState.Track:
					subMeshFlags |= SubMeshFlags.RequireTrack;
					break;
				case ObjectState.Partial1:
					subMeshFlags |= SubMeshFlags.RequirePartial1;
					quantityObjectData.m_StepMask |= 2u;
					break;
				case ObjectState.Partial2:
					subMeshFlags |= SubMeshFlags.RequirePartial2;
					quantityObjectData.m_StepMask |= 4u;
					break;
				case ObjectState.LefthandTraffic:
					subMeshFlags |= SubMeshFlags.RequireLeftHandTraffic;
					break;
				case ObjectState.RighthandTraffic:
					subMeshFlags |= SubMeshFlags.RequireRightHandTraffic;
					break;
				case ObjectState.Forward:
					subMeshFlags |= SubMeshFlags.RequireForward;
					break;
				case ObjectState.Backward:
					subMeshFlags |= SubMeshFlags.RequireBackward;
					break;
				case ObjectState.Outline:
					subMeshFlags |= SubMeshFlags.OutlineOnly;
					break;
				}
				float renderingSize;
				float metersPerPixel;
				if ((componentData.m_State & (MeshFlags.StackX | MeshFlags.StackY | MeshFlags.StackZ)) != 0)
				{
					StackProperties component3 = renderPrefab.GetComponent<StackProperties>();
					renderingSize = RenderingUtils.GetRenderingSize(@float, component3.m_Direction);
					metersPerPixel = RenderingUtils.GetShadowRenderingSize(@float, component3.m_Direction);
					if ((stackData.m_Direction != StackDirection.None && stackData.m_Direction != component3.m_Direction) || component3.m_Direction == StackDirection.None)
					{
						COSystemBase.baseLog.WarnFormat(objectPrefab, "{0}: Stack direction mismatch ({1})", objectPrefab.name, renderPrefab.name);
					}
					else
					{
						stackData.m_Direction = component3.m_Direction;
					}
					switch (component3.m_Order)
					{
					case StackOrder.First:
						subMeshFlags |= SubMeshFlags.IsStackStart;
						stackData.m_DontScale.x |= component3.m_ForbidScaling;
						UpdateStackBounds(ref stackData.m_FirstBounds, ref meshBounds, component3);
						break;
					case StackOrder.Middle:
						subMeshFlags |= SubMeshFlags.IsStackMiddle;
						stackData.m_DontScale.y |= component3.m_ForbidScaling;
						UpdateStackBounds(ref stackData.m_MiddleBounds, ref meshBounds, component3);
						break;
					case StackOrder.Last:
						subMeshFlags |= SubMeshFlags.IsStackEnd;
						stackData.m_DontScale.z |= component3.m_ForbidScaling;
						if (stackData.m_Direction == StackDirection.Up && (objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Standing) != Game.Objects.GeometryFlags.None)
						{
							meshBounds.max.y = math.max(meshBounds.max.y, objectGeometryData.m_LegSize.y + 0.1f);
						}
						UpdateStackBounds(ref stackData.m_LastBounds, ref meshBounds, component3);
						break;
					}
				}
				else
				{
					if (renderPrefab.surfaceArea <= 0f)
					{
						if (isHumanObject)
						{
							@float.x /= 3f;
						}
						else if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Standing) != Game.Objects.GeometryFlags.None)
						{
							@float.xz = math.lerp(@float.xz, math.min(@float.xz, objectGeometryData.m_LegSize.xz + objectGeometryData.m_LegOffset * 2f), math.saturate(objectGeometryData.m_LegSize.y / math.max(0.001f, @float.y)));
						}
					}
					@float = math.min(@float, math.max(math.min(@float.yzx, @float.zxy) * 8f, math.max(@float.yzx, @float.zxy) * 4f));
					renderingSize = RenderingUtils.GetRenderingSize(@float);
					metersPerPixel = renderingSize;
				}
				if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Standing) != Game.Objects.GeometryFlags.None)
				{
					meshBounds.max.y = math.max(meshBounds.max.y, objectGeometryData.m_LegSize.y + 0.1f);
				}
				componentData.m_MinLod = (byte)RenderingUtils.CalculateLodLimit(renderingSize, componentData.m_LodBias);
				componentData.m_ShadowLod = (byte)RenderingUtils.CalculateLodLimit(metersPerPixel, componentData.m_ShadowBias);
				MeshLayer meshLayer = ((componentData.m_DefaultLayers == (MeshLayer)0) ? MeshLayer.Default : componentData.m_DefaultLayers);
				objectGeometryData.m_Layers |= meshLayer;
				if (!objectMeshInfo.m_Position.Equals(default(float3)) || !objectMeshInfo.m_Rotation.Equals(quaternion.identity))
				{
					subMeshFlags |= SubMeshFlags.HasTransform;
				}
				ushort randomSeed = (ushort)num4;
				if (array != null)
				{
					randomSeed = GetRandomSeed(renderPrefab.name);
				}
				meshes.Add(new SubMesh(entity2, objectMeshInfo.m_Position, objectMeshInfo.m_Rotation, subMeshFlags, randomSeed));
				flag |= (componentData.m_State & MeshFlags.Decal) == 0;
				if (array == null || num4 == 1)
				{
					bounds |= meshBounds;
					objectGeometryData.m_MinLod = math.min(objectGeometryData.m_MinLod, componentData.m_MinLod);
				}
				else
				{
					componentData.m_MinLod = (byte)math.max(componentData.m_MinLod, objectGeometryData.m_MinLod);
				}
				CharacterStyleData component4;
				if (base.EntityManager.TryGetBuffer(entity2, isReadOnly: false, out DynamicBuffer<AnimationClip> buffer))
				{
					float num10 = float.MaxValue;
					float num11 = 0f;
					for (int l = 0; l < buffer.Length; l++)
					{
						AnimationClip animationClip = buffer[l];
						if (animationClip.m_Type == AnimationType.Move)
						{
							switch (animationClip.m_Activity)
							{
							case ActivityType.Walking:
								num10 = animationClip.m_MovementSpeed;
								break;
							case ActivityType.Running:
								num11 = animationClip.m_MovementSpeed;
								break;
							}
						}
						creatureData.m_SupportedActivities.m_Mask |= new ActivityMask(animationClip.m_Activity).m_Mask;
					}
					for (int m = 0; m < buffer.Length; m++)
					{
						AnimationClip value = buffer[m];
						if (value.m_Type == AnimationType.Move)
						{
							value.m_SpeedRange = new Bounds1(0f, float.MaxValue);
							switch (value.m_Activity)
							{
							case ActivityType.Walking:
								value.m_SpeedRange.max = math.select((num10 + num11) * 0.5f, float.MaxValue, num11 <= num10);
								break;
							case ActivityType.Running:
								value.m_SpeedRange.min = math.select((num10 + num11) * 0.5f, 0f, num10 >= num11);
								break;
							}
							buffer[m] = value;
						}
					}
				}
				else if (base.EntityManager.TryGetComponent<CharacterStyleData>(entity, out component4))
				{
					creatureData.m_SupportedActivities.m_Mask |= component4.m_ActivityMask.m_Mask;
					if (renderPrefab.TryGet<CharacterProperties>(out var component5) && !string.IsNullOrEmpty(component5.m_CorrectiveAnimationName))
					{
						CharacterStyle prefab = m_PrefabSystem.GetPrefab<CharacterStyle>(entity);
						ref CharacterElement reference = ref characterElements.ElementAt(characterElements.Length - 1);
						for (int n = 0; n < prefab.m_Animations.Length; n++)
						{
							if (prefab.m_Animations[n].name == component5.m_CorrectiveAnimationName)
							{
								reference.m_CorrectiveClipIndex = n;
								break;
							}
						}
					}
				}
				if (isBuildingObject)
				{
					componentData.m_DecalLayer |= DecalLayers.Buildings;
				}
				if (isVehicleObject)
				{
					componentData.m_DecalLayer |= DecalLayers.Vehicles;
				}
				if (isCreatureObject)
				{
					componentData.m_DecalLayer |= DecalLayers.Creatures;
				}
				bool flag2 = renderPrefab.TryGet<BaseProperties>(out var component6) && component6.m_BaseType != null;
				flag2 |= isBuildingObject && component6 == null;
				if (flag2 && (componentData.m_State & (MeshFlags.Decal | MeshFlags.Impostor)) == 0)
				{
					componentData.m_State |= MeshFlags.Base;
				}
				if ((componentData.m_State & MeshFlags.Base) != 0)
				{
					objectGeometryData.m_Flags |= Game.Objects.GeometryFlags.HasBase;
				}
				if (array != null)
				{
					componentData.m_State |= MeshFlags.Character;
				}
				if (isActivityPropObject)
				{
					componentData.m_State |= MeshFlags.Prop;
				}
				base.EntityManager.SetComponentData(entity2, componentData);
				if (!(isBuildingObject || isVehicleObject || isCreatureObject || flag2 || isActivityPropObject) || !renderPrefab.TryGet<LodProperties>(out var component7) || component7.m_LodMeshes == null)
				{
					continue;
				}
				for (int num12 = 0; num12 < component7.m_LodMeshes.Length; num12++)
				{
					if (!(component7.m_LodMeshes[num12] == null))
					{
						Entity entity3 = m_PrefabSystem.GetEntity(component7.m_LodMeshes[num12]);
						MeshData componentData2 = base.EntityManager.GetComponentData<MeshData>(entity3);
						if (isBuildingObject)
						{
							componentData2.m_DecalLayer |= DecalLayers.Buildings;
						}
						if (isVehicleObject)
						{
							componentData2.m_DecalLayer |= DecalLayers.Vehicles;
						}
						if (isCreatureObject)
						{
							componentData2.m_DecalLayer |= DecalLayers.Creatures;
						}
						if (flag2 && (componentData2.m_State & (MeshFlags.Decal | MeshFlags.Impostor)) == 0)
						{
							componentData2.m_State |= componentData.m_State & (MeshFlags.Base | MeshFlags.MinBounds);
						}
						if (array != null)
						{
							componentData2.m_State |= MeshFlags.Character;
						}
						if (isActivityPropObject)
						{
							componentData2.m_State |= MeshFlags.Prop;
						}
						base.EntityManager.SetComponentData(entity3, componentData2);
					}
				}
			}
		}
		if (bounds.min.x > bounds.max.x)
		{
			bounds = default(Bounds3);
		}
		objectGeometryData.m_Bounds = bounds;
		objectGeometryData.m_Size = ObjectUtils.GetSize(bounds);
		if (isPlantObject)
		{
			float num13 = 0.5f;
			if (objectPrefab.TryGet<PlantObject>(out var component8))
			{
				num13 = math.min(num13, 1f - component8.m_PotCoverage);
			}
			float3 float2 = default(float3);
			float2.xz = objectGeometryData.m_Size.xz * num13;
			float2.y = math.min(objectGeometryData.m_Size.y * num13, math.cmin(float2.xz) * 0.25f);
			objectGeometryData.m_Size -= float2;
			objectGeometryData.m_Bounds.min.xz = math.max(objectGeometryData.m_Bounds.min.xz, objectGeometryData.m_Size.xz * -0.5f);
			objectGeometryData.m_Bounds.max.xz = math.min(objectGeometryData.m_Bounds.max.xz, objectGeometryData.m_Size.xz * 0.5f);
			objectGeometryData.m_Bounds.max.y = objectGeometryData.m_Size.y;
		}
		else if (isHumanObject)
		{
			objectGeometryData.m_Size.x = math.max(objectGeometryData.m_Size.z, objectGeometryData.m_Size.x / 3f);
			objectGeometryData.m_Bounds.min.x = objectGeometryData.m_Size.x * -0.5f;
			objectGeometryData.m_Bounds.max.x = objectGeometryData.m_Size.x * 0.5f;
		}
		if ((placeableObjectData.m_Flags & Game.Objects.PlacementFlags.Wall) != Game.Objects.PlacementFlags.None)
		{
			objectGeometryData.m_Pivot = default(float3);
		}
		else if ((placeableObjectData.m_Flags & Game.Objects.PlacementFlags.Hanging) != Game.Objects.PlacementFlags.None)
		{
			objectGeometryData.m_Pivot = new float3(0f, math.lerp(bounds.min.y, bounds.max.y, 0.9f), 0f);
		}
		else
		{
			objectGeometryData.m_Pivot = new float3(0f, math.lerp(bounds.min.y, bounds.max.y, 0.25f), 0f);
		}
		if (objectPrefab.m_Circular)
		{
			objectGeometryData.m_Flags |= Game.Objects.GeometryFlags.Circular;
			objectGeometryData.m_Size.xz = math.max(objectGeometryData.m_Size.x, objectGeometryData.m_Size.z);
		}
		if (flag)
		{
			objectGeometryData.m_Flags |= Game.Objects.GeometryFlags.Physical;
		}
		else
		{
			objectGeometryData.m_Flags |= Game.Objects.GeometryFlags.WalkThrough;
		}
		growthScaleData.m_ChildSize = ObjectUtils.GetSize(bounds2);
		growthScaleData.m_TeenSize = ObjectUtils.GetSize(bounds3);
		growthScaleData.m_AdultSize = ObjectUtils.GetSize(bounds4);
		growthScaleData.m_ElderlySize = ObjectUtils.GetSize(bounds5);
		growthScaleData.m_DeadSize = ObjectUtils.GetSize(bounds6);
		if (!meshGroups.IsCreated)
		{
			return;
		}
		MeshGroupFlags meshGroupFlags3 = (MeshGroupFlags)0u;
		for (int num14 = 0; num14 < meshGroups.Length; num14++)
		{
			meshGroupFlags3 |= meshGroups[num14].m_Flags;
		}
		if ((meshGroupFlags3 & (MeshGroupFlags.RequireHome | MeshGroupFlags.RequireHomeless)) != MeshGroupFlags.RequireHomeless)
		{
			return;
		}
		for (int num15 = 0; num15 < meshGroups.Length; num15++)
		{
			ref SubMeshGroup reference2 = ref meshGroups.ElementAt(num15);
			if ((reference2.m_Flags & (MeshGroupFlags.RequireCold | MeshGroupFlags.RequireWarm)) != 0)
			{
				reference2.m_Flags |= MeshGroupFlags.RequireHome;
			}
		}
	}

	private ushort GetRandomSeed(string name)
	{
		uint num = 0u;
		for (int i = 0; i < name.Length; i++)
		{
			num = (num << 1) ^ name[i];
		}
		return (ushort)num;
	}

	private void UpdateStackBounds(ref Bounds1 stackBounds, ref Bounds3 meshBounds, StackProperties properties)
	{
		switch (properties.m_Direction)
		{
		case StackDirection.Right:
			stackBounds |= new Bounds1(meshBounds.min.x + properties.m_StartOverlap, meshBounds.max.x - properties.m_EndOverlap);
			meshBounds.min.x = ((properties.m_Order == StackOrder.First) ? math.min(meshBounds.min.x, 0f) : 0f);
			meshBounds.max.x = ((properties.m_Order == StackOrder.Last) ? math.max(meshBounds.max.x, 0f) : 0f);
			break;
		case StackDirection.Up:
			stackBounds |= new Bounds1(meshBounds.min.y + properties.m_StartOverlap, meshBounds.max.y - properties.m_EndOverlap);
			meshBounds.min.y = ((properties.m_Order == StackOrder.First) ? math.min(meshBounds.min.y, 0f) : 0f);
			meshBounds.max.y = ((properties.m_Order == StackOrder.Last) ? math.max(meshBounds.max.y, 0f) : 0f);
			break;
		case StackDirection.Forward:
			stackBounds |= new Bounds1(meshBounds.min.z + properties.m_StartOverlap, meshBounds.max.z - properties.m_EndOverlap);
			meshBounds.min.z = ((properties.m_Order == StackOrder.First) ? math.min(meshBounds.min.z, 0f) : 0f);
			meshBounds.max.z = ((properties.m_Order == StackOrder.Last) ? math.max(meshBounds.max.z, 0f) : 0f);
			break;
		}
	}

	private void InitializePrefab(MarkerObjectPrefab objectPrefab, PlaceableObjectData placeableObjectData, ref ObjectGeometryData objectGeometryData, DynamicBuffer<SubMesh> meshes)
	{
		Bounds3 bounds = default(Bounds3);
		if (objectPrefab.m_Mesh != null)
		{
			Entity entity = m_PrefabSystem.GetEntity(objectPrefab.m_Mesh);
			MeshData componentData = base.EntityManager.GetComponentData<MeshData>(entity);
			Bounds3 bounds2 = objectPrefab.m_Mesh.bounds;
			float renderingSize = RenderingUtils.GetRenderingSize(MathUtils.Size(bounds2));
			componentData.m_MinLod = (byte)RenderingUtils.CalculateLodLimit(renderingSize, componentData.m_LodBias);
			componentData.m_ShadowLod = (byte)RenderingUtils.CalculateLodLimit(renderingSize, componentData.m_ShadowBias);
			objectGeometryData.m_MinLod = math.min(objectGeometryData.m_MinLod, componentData.m_MinLod);
			objectGeometryData.m_Layers = ((componentData.m_DefaultLayers == (MeshLayer)0) ? MeshLayer.Default : componentData.m_DefaultLayers);
			bounds |= bounds2;
			base.EntityManager.SetComponentData(entity, componentData);
			meshes.Add(new SubMesh(entity, (SubMeshFlags)0u, 0));
		}
		objectGeometryData.m_Bounds = bounds;
		objectGeometryData.m_Size = ObjectUtils.GetSize(bounds);
		if ((placeableObjectData.m_Flags & Game.Objects.PlacementFlags.Wall) != Game.Objects.PlacementFlags.None)
		{
			objectGeometryData.m_Pivot = default(float3);
		}
		else if ((placeableObjectData.m_Flags & Game.Objects.PlacementFlags.Hanging) != Game.Objects.PlacementFlags.None)
		{
			objectGeometryData.m_Pivot = new float3(0f, math.lerp(bounds.min.y, bounds.max.y, 0.9f), 0f);
		}
		else
		{
			objectGeometryData.m_Pivot = new float3(0f, math.lerp(bounds.min.y, bounds.max.y, 0.25f), 0f);
		}
		if (objectPrefab.m_Circular)
		{
			objectGeometryData.m_Flags |= Game.Objects.GeometryFlags.Circular;
			objectGeometryData.m_Size.xz = math.max(objectGeometryData.m_Size.x, objectGeometryData.m_Size.z);
		}
		objectGeometryData.m_Flags |= Game.Objects.GeometryFlags.Marker;
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
	public ObjectInitializeSystem()
	{
	}
}
