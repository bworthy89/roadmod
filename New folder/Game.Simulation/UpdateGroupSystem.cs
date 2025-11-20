using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Creatures;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class UpdateGroupSystem : GameSystemBase
{
	public struct UpdateGroupTypes
	{
		public ComponentTypeHandle<Moving> m_MovingType;

		public ComponentTypeHandle<Stopped> m_StoppedType;

		public ComponentTypeHandle<Plant> m_PlantType;

		public ComponentTypeHandle<Building> m_BuildingType;

		public ComponentTypeHandle<Extension> m_ExtensionType;

		public ComponentTypeHandle<Node> m_NodeType;

		public ComponentTypeHandle<Edge> m_EdgeType;

		public ComponentTypeHandle<Lane> m_LaneType;

		public ComponentTypeHandle<CompanyData> m_CompanyType;

		public ComponentTypeHandle<Household> m_HouseholdType;

		public ComponentTypeHandle<Citizen> m_CitizenType;

		public ComponentTypeHandle<HouseholdPet> m_HouseholdPetType;

		public ComponentTypeHandle<CurrentVehicle> m_CurrentVehicleType;

		public UpdateGroupTypes(SystemBase system)
		{
			m_MovingType = system.GetComponentTypeHandle<Moving>(isReadOnly: true);
			m_StoppedType = system.GetComponentTypeHandle<Stopped>(isReadOnly: true);
			m_PlantType = system.GetComponentTypeHandle<Plant>(isReadOnly: true);
			m_BuildingType = system.GetComponentTypeHandle<Building>(isReadOnly: true);
			m_ExtensionType = system.GetComponentTypeHandle<Extension>(isReadOnly: true);
			m_NodeType = system.GetComponentTypeHandle<Node>(isReadOnly: true);
			m_EdgeType = system.GetComponentTypeHandle<Edge>(isReadOnly: true);
			m_LaneType = system.GetComponentTypeHandle<Lane>(isReadOnly: true);
			m_CompanyType = system.GetComponentTypeHandle<CompanyData>(isReadOnly: true);
			m_HouseholdType = system.GetComponentTypeHandle<Household>(isReadOnly: true);
			m_CitizenType = system.GetComponentTypeHandle<Citizen>(isReadOnly: true);
			m_HouseholdPetType = system.GetComponentTypeHandle<HouseholdPet>(isReadOnly: true);
			m_CurrentVehicleType = system.GetComponentTypeHandle<CurrentVehicle>(isReadOnly: true);
		}

		public void Update(SystemBase system)
		{
			m_MovingType.Update(system);
			m_StoppedType.Update(system);
			m_PlantType.Update(system);
			m_BuildingType.Update(system);
			m_ExtensionType.Update(system);
			m_NodeType.Update(system);
			m_EdgeType.Update(system);
			m_LaneType.Update(system);
			m_CompanyType.Update(system);
			m_HouseholdType.Update(system);
			m_CitizenType.Update(system);
			m_HouseholdPetType.Update(system);
			m_CurrentVehicleType.Update(system);
		}
	}

	public struct UpdateGroupSizes
	{
		private NativeArray<int> m_MovingObjectUpdateGroupSizes;

		private NativeArray<int> m_TreeUpdateGroupSizes;

		private NativeArray<int> m_BuildingUpdateGroupSizes;

		private NativeArray<int> m_NetUpdateGroupSizes;

		private NativeArray<int> m_LaneUpdateGroupSizes;

		private NativeArray<int> m_CompanyUpdateGroupSizes;

		private NativeArray<int> m_HouseholdUpdateGroupSizes;

		private NativeArray<int> m_CitizenUpdateGroupSizes;

		private NativeArray<int> m_HouseholdPetUpdateGroupSizes;

		public UpdateGroupSizes(Allocator allocator)
		{
			m_MovingObjectUpdateGroupSizes = new NativeArray<int>(16, allocator);
			m_TreeUpdateGroupSizes = new NativeArray<int>(16, allocator);
			m_BuildingUpdateGroupSizes = new NativeArray<int>(16, allocator);
			m_NetUpdateGroupSizes = new NativeArray<int>(16, allocator);
			m_LaneUpdateGroupSizes = new NativeArray<int>(16, allocator);
			m_CompanyUpdateGroupSizes = new NativeArray<int>(16, allocator);
			m_HouseholdUpdateGroupSizes = new NativeArray<int>(16, allocator);
			m_CitizenUpdateGroupSizes = new NativeArray<int>(16, allocator);
			m_HouseholdPetUpdateGroupSizes = new NativeArray<int>(16, allocator);
		}

		public void Clear()
		{
			m_MovingObjectUpdateGroupSizes.Fill(0);
			m_TreeUpdateGroupSizes.Fill(0);
			m_BuildingUpdateGroupSizes.Fill(0);
			m_NetUpdateGroupSizes.Fill(0);
			m_LaneUpdateGroupSizes.Fill(0);
			m_CompanyUpdateGroupSizes.Fill(0);
			m_HouseholdUpdateGroupSizes.Fill(0);
			m_CitizenUpdateGroupSizes.Fill(0);
			m_HouseholdPetUpdateGroupSizes.Fill(0);
		}

		public void Dispose()
		{
			m_MovingObjectUpdateGroupSizes.Dispose();
			m_TreeUpdateGroupSizes.Dispose();
			m_BuildingUpdateGroupSizes.Dispose();
			m_NetUpdateGroupSizes.Dispose();
			m_LaneUpdateGroupSizes.Dispose();
			m_CompanyUpdateGroupSizes.Dispose();
			m_HouseholdUpdateGroupSizes.Dispose();
			m_CitizenUpdateGroupSizes.Dispose();
			m_HouseholdPetUpdateGroupSizes.Dispose();
		}

		public NativeArray<int> Get(ArchetypeChunk chunk, UpdateGroupTypes types)
		{
			if (chunk.Has(ref types.m_MovingType) || chunk.Has(ref types.m_StoppedType) || chunk.Has(ref types.m_CurrentVehicleType))
			{
				return m_MovingObjectUpdateGroupSizes;
			}
			if (chunk.Has(ref types.m_PlantType))
			{
				return m_TreeUpdateGroupSizes;
			}
			if (chunk.Has(ref types.m_BuildingType) || chunk.Has(ref types.m_ExtensionType))
			{
				return m_BuildingUpdateGroupSizes;
			}
			if (chunk.Has(ref types.m_NodeType) || chunk.Has(ref types.m_EdgeType))
			{
				return m_NetUpdateGroupSizes;
			}
			if (chunk.Has(ref types.m_LaneType))
			{
				return m_LaneUpdateGroupSizes;
			}
			if (chunk.Has(ref types.m_CompanyType))
			{
				return m_CompanyUpdateGroupSizes;
			}
			if (chunk.Has(ref types.m_HouseholdType))
			{
				return m_HouseholdUpdateGroupSizes;
			}
			if (chunk.Has(ref types.m_CitizenType))
			{
				return m_CitizenUpdateGroupSizes;
			}
			if (chunk.Has(ref types.m_HouseholdPetType))
			{
				return m_HouseholdPetUpdateGroupSizes;
			}
			return default(NativeArray<int>);
		}
	}

	[BurstCompile]
	private struct UpdateGroupJob : IJob
	{
		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Applied> m_AppliedType;

		[ReadOnly]
		public ComponentTypeHandle<Created> m_CreatedType;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<Controller> m_ControllerType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<LayoutElement> m_LayoutElementType;

		public ComponentTypeHandle<InterpolatedTransform> m_InterpolatedTransformType;

		[ReadOnly]
		public EntityStorageInfoLookup m_EntityLookup;

		[ReadOnly]
		public ComponentLookup<Created> m_CreatedData;

		[ReadOnly]
		public ComponentLookup<UpdateFrameData> m_PrefabUpdateFrameData;

		public BufferLookup<TransformFrame> m_TransformFrameData;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public UpdateGroupTypes m_UpdateGroupTypes;

		public EntityCommandBuffer m_CommandBuffer;

		public UpdateGroupSizes m_UpdateGroupSizes;

		public void Execute()
		{
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				ArchetypeChunk chunk = m_Chunks[i];
				if (chunk.Has(ref m_DeletedType) && !chunk.Has(ref m_CreatedType))
				{
					CheckDeleted(chunk);
				}
			}
			for (int j = 0; j < m_Chunks.Length; j++)
			{
				ArchetypeChunk chunk2 = m_Chunks[j];
				if (chunk2.Has(ref m_CreatedType) && !chunk2.Has(ref m_DeletedType))
				{
					CheckCreated(chunk2);
				}
			}
		}

		private NativeArray<int> GetGroupSizeArray(ArchetypeChunk chunk)
		{
			NativeArray<int> result = m_UpdateGroupSizes.Get(chunk, m_UpdateGroupTypes);
			if (!result.IsCreated)
			{
				NativeArray<ComponentType> componentTypes = chunk.Archetype.GetComponentTypes();
				UnityEngine.Debug.Log("UpdateFrame added to unsupported type");
				for (int i = 0; i < componentTypes.Length; i++)
				{
					UnityEngine.Debug.Log($"Component: {componentTypes[i]}");
				}
				componentTypes.Dispose();
			}
			return result;
		}

		private void CheckDeleted(ArchetypeChunk chunk)
		{
			if (!chunk.Has(ref m_TempType))
			{
				uint index = chunk.GetSharedComponent(m_UpdateFrameType).m_Index;
				NativeArray<int> groupSizeArray = GetGroupSizeArray(chunk);
				if (index < groupSizeArray.Length)
				{
					groupSizeArray[(int)index] -= chunk.Count;
				}
			}
		}

		private int GetUpdateFrame(Entity entity)
		{
			if (m_CreatedData.HasComponent(entity))
			{
				return -1;
			}
			if (!m_EntityLookup.Exists(entity))
			{
				return -1;
			}
			ArchetypeChunk chunk = m_EntityLookup[entity].Chunk;
			if (!chunk.Has(m_UpdateFrameType))
			{
				return -1;
			}
			return (int)chunk.GetSharedComponent(m_UpdateFrameType).m_Index;
		}

		private void CheckCreated(ArchetypeChunk chunk)
		{
			NativeArray<int> groupSizeArray = GetGroupSizeArray(chunk);
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Temp> nativeArray2 = chunk.GetNativeArray(ref m_TempType);
			NativeArray<InterpolatedTransform> nativeArray3 = chunk.GetNativeArray(ref m_InterpolatedTransformType);
			NativeArray<Controller> nativeArray4 = chunk.GetNativeArray(ref m_ControllerType);
			NativeArray<PrefabRef> nativeArray5 = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<LayoutElement> bufferAccessor = chunk.GetBufferAccessor(ref m_LayoutElementType);
			if (nativeArray2.Length != 0)
			{
				for (int i = 0; i < nativeArray.Length; i++)
				{
					Entity entity = nativeArray[i];
					int num = -1;
					if (nativeArray4.Length != 0)
					{
						Controller controller = nativeArray4[i];
						if (controller.m_Controller != Entity.Null && controller.m_Controller != entity)
						{
							num = GetUpdateFrame(controller.m_Controller);
							if (num == -1)
							{
								continue;
							}
						}
					}
					Temp temp = nativeArray2[i];
					if (temp.m_Original != Entity.Null)
					{
						num = GetUpdateFrame(temp.m_Original);
					}
					uint index;
					if (nativeArray5.Length != 0)
					{
						PrefabRef prefabRef = nativeArray5[i];
						index = FindUpdateIndex(num, groupSizeArray, prefabRef);
					}
					else
					{
						index = FindUpdateIndex(num, groupSizeArray);
					}
					m_CommandBuffer.SetSharedComponent(entity, new UpdateFrame(index));
					if (bufferAccessor.Length == 0)
					{
						continue;
					}
					DynamicBuffer<LayoutElement> dynamicBuffer = bufferAccessor[i];
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						Entity vehicle = dynamicBuffer[j].m_Vehicle;
						if (vehicle != entity)
						{
							m_CommandBuffer.SetSharedComponent(vehicle, new UpdateFrame(index));
						}
					}
				}
				if (nativeArray3.Length == 0)
				{
					return;
				}
				NativeArray<Game.Objects.Transform> nativeArray6 = chunk.GetNativeArray(ref m_TransformType);
				for (int k = 0; k < nativeArray.Length; k++)
				{
					Entity entity2 = nativeArray[k];
					Temp temp2 = nativeArray2[k];
					Game.Objects.Transform transform = nativeArray6[k];
					if (m_TransformFrameData.HasBuffer(entity2))
					{
						DynamicBuffer<TransformFrame> dynamicBuffer2 = m_TransformFrameData[entity2];
						if (m_TransformFrameData.HasBuffer(temp2.m_Original))
						{
							DynamicBuffer<TransformFrame> dynamicBuffer3 = m_TransformFrameData[temp2.m_Original];
							dynamicBuffer2.ResizeUninitialized(dynamicBuffer3.Length);
							for (int l = 0; l < dynamicBuffer2.Length; l++)
							{
								dynamicBuffer2[l] = dynamicBuffer3[l];
							}
						}
						else
						{
							dynamicBuffer2.ResizeUninitialized(4);
							for (int m = 0; m < dynamicBuffer2.Length; m++)
							{
								dynamicBuffer2[m] = new TransformFrame(transform);
							}
						}
					}
					nativeArray3[k] = new InterpolatedTransform(transform);
				}
				return;
			}
			uint index2 = chunk.GetSharedComponent(m_UpdateFrameType).m_Index;
			bool flag = chunk.Has(ref m_AppliedType);
			for (int n = 0; n < nativeArray.Length; n++)
			{
				Entity entity3 = nativeArray[n];
				uint num2;
				if (flag)
				{
					num2 = index2;
				}
				else
				{
					int num3 = -1;
					if (nativeArray4.Length != 0 && !flag)
					{
						Controller controller2 = nativeArray4[n];
						if (controller2.m_Controller != Entity.Null && controller2.m_Controller != entity3)
						{
							num3 = GetUpdateFrame(controller2.m_Controller);
							if (num3 == -1)
							{
								continue;
							}
						}
					}
					if (nativeArray5.Length != 0)
					{
						PrefabRef prefabRef2 = nativeArray5[n];
						num2 = FindUpdateIndex(num3, groupSizeArray, prefabRef2);
					}
					else
					{
						num2 = FindUpdateIndex(num3, groupSizeArray);
					}
				}
				int num4 = 1;
				if (!flag)
				{
					m_CommandBuffer.SetSharedComponent(entity3, new UpdateFrame(num2));
					if (bufferAccessor.Length != 0)
					{
						DynamicBuffer<LayoutElement> dynamicBuffer4 = bufferAccessor[n];
						for (int num5 = 0; num5 < dynamicBuffer4.Length; num5++)
						{
							Entity vehicle2 = dynamicBuffer4[num5].m_Vehicle;
							if (vehicle2 != entity3)
							{
								m_CommandBuffer.SetSharedComponent(vehicle2, new UpdateFrame(num2));
								num4++;
							}
						}
					}
				}
				if (num2 < groupSizeArray.Length)
				{
					groupSizeArray[(int)num2] += num4;
				}
			}
			if (nativeArray3.Length == 0)
			{
				return;
			}
			NativeArray<Game.Objects.Transform> nativeArray7 = chunk.GetNativeArray(ref m_TransformType);
			for (int num6 = 0; num6 < nativeArray.Length; num6++)
			{
				Entity entity4 = nativeArray[num6];
				Game.Objects.Transform transform2 = nativeArray7[num6];
				if (m_TransformFrameData.HasBuffer(entity4))
				{
					DynamicBuffer<TransformFrame> dynamicBuffer5 = m_TransformFrameData[entity4];
					dynamicBuffer5.ResizeUninitialized(4);
					for (int num7 = 0; num7 < dynamicBuffer5.Length; num7++)
					{
						dynamicBuffer5[num7] = new TransformFrame(transform2);
					}
				}
				nativeArray3[num6] = new InterpolatedTransform(transform2);
			}
		}

		private uint FindUpdateIndex(NativeArray<int> groupSizes, PrefabRef prefabRef)
		{
			if (m_PrefabUpdateFrameData.HasComponent(prefabRef.m_Prefab))
			{
				return (uint)m_PrefabUpdateFrameData[prefabRef.m_Prefab].m_UpdateGroupIndex;
			}
			return FindUpdateIndex(groupSizes);
		}

		private uint FindUpdateIndex(int originalIndex, NativeArray<int> groupSizes, PrefabRef prefabRef)
		{
			if (originalIndex != -1)
			{
				return (uint)originalIndex;
			}
			return FindUpdateIndex(groupSizes, prefabRef);
		}

		private uint FindUpdateIndex(NativeArray<int> groupSizes)
		{
			uint result = uint.MaxValue;
			int num = int.MaxValue;
			for (int i = 0; i < groupSizes.Length; i++)
			{
				int num2 = groupSizes[i];
				if (num2 < num)
				{
					num = num2;
					result = (uint)i;
				}
			}
			return result;
		}

		private uint FindUpdateIndex(int originalIndex, NativeArray<int> groupSizes)
		{
			if (originalIndex != -1)
			{
				return (uint)originalIndex;
			}
			return FindUpdateIndex(groupSizes);
		}
	}

	[BurstCompile]
	private struct MovingObjectsUpdatedJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<HumanNavigation> m_HumanNavigationType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		public ComponentTypeHandle<InterpolatedTransform> m_InterpolatedTransformType;

		[NativeDisableParallelForRestriction]
		public BufferLookup<TransformFrame> m_TransformFrameData;

		[ReadOnly]
		public uint m_SimulationFrame;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Game.Objects.Transform> nativeArray2 = chunk.GetNativeArray(ref m_TransformType);
			NativeArray<HumanNavigation> nativeArray3 = chunk.GetNativeArray(ref m_HumanNavigationType);
			NativeArray<InterpolatedTransform> nativeArray4 = chunk.GetNativeArray(ref m_InterpolatedTransformType);
			NativeArray<Temp> nativeArray5 = chunk.GetNativeArray(ref m_TempType);
			uint num = 0u;
			if (chunk.Has(m_UpdateFrameType))
			{
				uint num2 = m_SimulationFrame % 16;
				uint index = chunk.GetSharedComponent(m_UpdateFrameType).m_Index;
				num = (m_SimulationFrame + num2 - index) / 16 % 4;
			}
			for (int i = 0; i < nativeArray4.Length; i++)
			{
				Entity entity = nativeArray[i];
				Game.Objects.Transform transform = nativeArray2[i];
				if (m_TransformFrameData.TryGetBuffer(entity, out var bufferData))
				{
					CollectionUtils.TryGet(nativeArray5, i, out var value);
					if (m_TransformFrameData.TryGetBuffer(value.m_Original, out var bufferData2))
					{
						bufferData.ResizeUninitialized(bufferData2.Length);
						for (int j = 0; j < bufferData.Length; j++)
						{
							bufferData[j] = bufferData2[j];
						}
					}
					else
					{
						bufferData.ResizeUninitialized(4);
						TransformFrame transformFrame = new TransformFrame(transform);
						if (CollectionUtils.TryGet(nativeArray3, i, out var value2))
						{
							transformFrame.m_Activity = value2.m_LastActivity;
							transformFrame.m_State = value2.m_TransformState;
						}
						for (int k = 0; k < bufferData.Length; k++)
						{
							TransformFrame value3 = transformFrame;
							value3.m_StateTimer = (ushort)k;
							int num3 = k - (int)num - 1;
							num3 = math.select(num3, num3 + bufferData.Length, num3 < 0);
							bufferData[num3] = value3;
						}
					}
				}
				nativeArray4[i] = new InterpolatedTransform(transform);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Created> __Game_Common_Created_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> __Game_Common_Deleted_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Applied> __Game_Common_Applied_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Controller> __Game_Vehicles_Controller_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferTypeHandle;

		public ComponentTypeHandle<InterpolatedTransform> __Game_Rendering_InterpolatedTransform_RW_ComponentTypeHandle;

		[ReadOnly]
		public EntityStorageInfoLookup __EntityStorageInfoLookup;

		[ReadOnly]
		public ComponentLookup<Created> __Game_Common_Created_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<UpdateFrameData> __Game_Prefabs_UpdateFrameData_RO_ComponentLookup;

		public BufferLookup<TransformFrame> __Game_Objects_TransformFrame_RW_BufferLookup;

		[ReadOnly]
		public ComponentTypeHandle<HumanNavigation> __Game_Creatures_HumanNavigation_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Created_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Created>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
			__Game_Common_Applied_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Applied>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true);
			__Game_Vehicles_Controller_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Controller>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<LayoutElement>(isReadOnly: true);
			__Game_Rendering_InterpolatedTransform_RW_ComponentTypeHandle = state.GetComponentTypeHandle<InterpolatedTransform>();
			__EntityStorageInfoLookup = state.GetEntityStorageInfoLookup();
			__Game_Common_Created_RO_ComponentLookup = state.GetComponentLookup<Created>(isReadOnly: true);
			__Game_Prefabs_UpdateFrameData_RO_ComponentLookup = state.GetComponentLookup<UpdateFrameData>(isReadOnly: true);
			__Game_Objects_TransformFrame_RW_BufferLookup = state.GetBufferLookup<TransformFrame>();
			__Game_Creatures_HumanNavigation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HumanNavigation>(isReadOnly: true);
		}
	}

	private SimulationSystem m_SimulationSystem;

	private ModificationBarrier5 m_ModificationBarrier;

	private EntityQuery m_CreatedQuery;

	private EntityQuery m_UpdatedQuery;

	private UpdateGroupTypes m_UpdateGroupTypes;

	private UpdateGroupSizes m_UpdateGroupSizes;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier5>();
		m_CreatedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<UpdateFrame>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_UpdatedQuery = GetEntityQuery(ComponentType.ReadOnly<Updated>(), ComponentType.ReadOnly<InterpolatedTransform>(), ComponentType.Exclude<Created>());
		m_UpdateGroupTypes = new UpdateGroupTypes(this);
		m_UpdateGroupSizes = new UpdateGroupSizes(Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_UpdateGroupSizes.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_CreatedQuery.IsEmptyIgnoreFilter)
		{
			JobHandle outJobHandle;
			NativeList<ArchetypeChunk> chunks = m_CreatedQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
			m_UpdateGroupTypes.Update(this);
			UpdateGroupJob jobData = new UpdateGroupJob
			{
				m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_CreatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Created_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_AppliedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Applied_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ControllerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Controller_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_LayoutElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_InterpolatedTransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Rendering_InterpolatedTransform_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_EntityLookup = InternalCompilerInterface.GetEntityStorageInfoLookup(ref __TypeHandle.__EntityStorageInfoLookup, ref base.CheckedStateRef),
				m_CreatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Created_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabUpdateFrameData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_UpdateFrameData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TransformFrameData = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_TransformFrame_RW_BufferLookup, ref base.CheckedStateRef),
				m_Chunks = chunks,
				m_UpdateGroupTypes = m_UpdateGroupTypes,
				m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer(),
				m_UpdateGroupSizes = m_UpdateGroupSizes
			};
			base.Dependency = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(base.Dependency, outJobHandle));
			chunks.Dispose(base.Dependency);
			m_ModificationBarrier.AddJobHandleForProducer(base.Dependency);
		}
		if (!m_UpdatedQuery.IsEmptyIgnoreFilter)
		{
			MovingObjectsUpdatedJob jobData2 = new MovingObjectsUpdatedJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_HumanNavigationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_HumanNavigation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
				m_InterpolatedTransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Rendering_InterpolatedTransform_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TransformFrameData = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_TransformFrame_RW_BufferLookup, ref base.CheckedStateRef),
				m_SimulationFrame = m_SimulationSystem.frameIndex
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData2, m_UpdatedQuery, base.Dependency);
		}
	}

	public UpdateGroupSizes GetUpdateGroupSizes()
	{
		return m_UpdateGroupSizes;
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
	public UpdateGroupSystem()
	{
	}
}
