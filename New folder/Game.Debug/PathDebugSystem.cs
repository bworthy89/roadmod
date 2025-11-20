using System.Runtime.CompilerServices;
using Colossal;
using Colossal.Mathematics;
using Game.Agents;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Creatures;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Routes;
using Game.Simulation;
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

namespace Game.Debug;

[CompilerGenerated]
public class PathDebugSystem : BaseDebugSystem
{
	[BurstCompile]
	private struct PathGizmoJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PathOwner> m_PathOwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Vehicle> m_VehicleType;

		[ReadOnly]
		public ComponentTypeHandle<PersonalCar> m_PersonalCarType;

		[ReadOnly]
		public ComponentTypeHandle<DeliveryTruck> m_DeliveryTruckType;

		[ReadOnly]
		public ComponentTypeHandle<Creature> m_CreatureType;

		[ReadOnly]
		public ComponentTypeHandle<Citizen> m_CitizenType;

		[ReadOnly]
		public ComponentTypeHandle<JobSeeker> m_JobSeekerType;

		[ReadOnly]
		public ComponentTypeHandle<SchoolSeeker> m_SchoolSeekerType;

		[ReadOnly]
		public ComponentTypeHandle<Household> m_HouseholdType;

		[ReadOnly]
		public ComponentTypeHandle<CompanyData> m_CompanyType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Routes.Segment> m_RouteSegmentType;

		[ReadOnly]
		public ComponentTypeHandle<GoodsDeliveryRequest> m_GoodsDeliveryRequestType;

		[ReadOnly]
		public ComponentTypeHandle<ServiceRequest> m_ServiceRequestType;

		[ReadOnly]
		public BufferTypeHandle<PathElement> m_PathElementType;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Position> m_PositionData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public bool m_PersonalCarOption;

		[ReadOnly]
		public bool m_DeliveryTruckOption;

		[ReadOnly]
		public bool m_ServiceVehicleOption;

		[ReadOnly]
		public bool m_ResidentOption;

		[ReadOnly]
		public bool m_CitizenOption;

		[ReadOnly]
		public bool m_JobSeekerOption;

		[ReadOnly]
		public bool m_SchoolSeekerOption;

		[ReadOnly]
		public bool m_HouseholdOption;

		[ReadOnly]
		public bool m_CompanyOption;

		[ReadOnly]
		public bool m_RouteOption;

		[ReadOnly]
		public bool m_DeliveryRequestOption;

		[ReadOnly]
		public bool m_ServiceRequestOption;

		[ReadOnly]
		public float m_TimeOffset;

		[ReadOnly]
		public Entity m_Selected;

		public GizmoBatcher m_GizmoBatcher;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			int num;
			int num2;
			if (m_Selected != Entity.Null)
			{
				num = (num2 = -1);
				NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
				for (int i = 0; i < nativeArray.Length; i++)
				{
					if (nativeArray[i] == m_Selected)
					{
						num = i;
						num2 = i + 1;
						break;
					}
				}
				if (num == -1)
				{
					return;
				}
			}
			else
			{
				num = 0;
				num2 = chunk.Count;
				if (chunk.Has(ref m_VehicleType))
				{
					if (chunk.Has(ref m_PersonalCarType))
					{
						if (!m_PersonalCarOption)
						{
							return;
						}
					}
					else if (chunk.Has(ref m_DeliveryTruckType))
					{
						if (!m_DeliveryTruckOption)
						{
							return;
						}
					}
					else if (!m_ServiceVehicleOption)
					{
						return;
					}
				}
				else if (chunk.Has(ref m_CreatureType))
				{
					if (!m_ResidentOption)
					{
						return;
					}
				}
				else if (chunk.Has(ref m_CitizenType))
				{
					if (!m_CitizenOption)
					{
						return;
					}
				}
				else if (chunk.Has(ref m_JobSeekerType))
				{
					if (!m_JobSeekerOption)
					{
						return;
					}
				}
				else if (chunk.Has(ref m_SchoolSeekerType))
				{
					if (!m_SchoolSeekerOption)
					{
						return;
					}
				}
				else if (chunk.Has(ref m_HouseholdType))
				{
					if (!m_HouseholdOption)
					{
						return;
					}
				}
				else if (chunk.Has(ref m_CompanyType))
				{
					if (!m_CompanyOption)
					{
						return;
					}
				}
				else if (chunk.Has(ref m_RouteSegmentType))
				{
					if (!m_RouteOption)
					{
						return;
					}
				}
				else if (chunk.Has(ref m_GoodsDeliveryRequestType))
				{
					if (!m_DeliveryRequestOption)
					{
						return;
					}
				}
				else if (chunk.Has(ref m_ServiceRequestType) && !m_ServiceRequestOption)
				{
					return;
				}
			}
			NativeArray<PathOwner> nativeArray2 = chunk.GetNativeArray(ref m_PathOwnerType);
			BufferAccessor<PathElement> bufferAccessor = chunk.GetBufferAccessor(ref m_PathElementType);
			float timeOffset = m_TimeOffset * 10f;
			UnityEngine.Color white = UnityEngine.Color.white;
			UnityEngine.Color magenta = UnityEngine.Color.magenta;
			UnityEngine.Color red = UnityEngine.Color.red;
			UnityEngine.Color green = UnityEngine.Color.green;
			if (nativeArray2.Length == 0)
			{
				white *= 0.5f;
				magenta *= 0.5f;
				red *= 0.5f;
				green *= 0.5f;
			}
			for (int j = num; j < num2; j++)
			{
				int num3 = 0;
				if (nativeArray2.Length != 0)
				{
					num3 = nativeArray2[j].m_ElementIndex;
				}
				DynamicBuffer<PathElement> dynamicBuffer = bufferAccessor[j];
				float3 @float = default(float3);
				bool flag = false;
				bool flag2 = false;
				for (int k = num3; k < dynamicBuffer.Length; k++)
				{
					PathElement pathElement = dynamicBuffer[k];
					if (m_CurveData.HasComponent(pathElement.m_Target))
					{
						Curve curve = m_CurveData[pathElement.m_Target];
						Bezier4x3 curve2 = MathUtils.Cut(curve.m_Bezier, pathElement.m_TargetDelta);
						float length = curve.m_Length * math.abs(pathElement.m_TargetDelta.y - pathElement.m_TargetDelta.x);
						if (flag && math.lengthsq(curve2.a - @float) > 1E-06f)
						{
							m_GizmoBatcher.DrawLine(@float, curve2.a, magenta);
						}
						DrawPathCurve(curve2, length, timeOffset, white);
						if (k == num3)
						{
							m_GizmoBatcher.DrawWireNode(curve2.a, 1f, red);
						}
						if (k == dynamicBuffer.Length - 1)
						{
							m_GizmoBatcher.DrawWireNode(curve2.d, 1f, green);
						}
						@float = curve2.d;
						flag = true;
						flag2 = false;
						continue;
					}
					float3 position;
					if (m_PositionData.HasComponent(pathElement.m_Target))
					{
						position = m_PositionData[pathElement.m_Target].m_Position;
					}
					else
					{
						if (!m_TransformData.HasComponent(pathElement.m_Target))
						{
							continue;
						}
						position = m_TransformData[pathElement.m_Target].m_Position;
					}
					if (flag && math.lengthsq(position - @float) > 1E-06f)
					{
						if (flag2)
						{
							Bezier4x3 curve3 = NetUtils.StraightCurve(@float, position);
							DrawPathCurve(curve3, math.distance(@float, position), timeOffset, white);
						}
						else
						{
							m_GizmoBatcher.DrawLine(@float, position, magenta);
						}
					}
					if (k == num3)
					{
						m_GizmoBatcher.DrawWireNode(position, 1f, red);
					}
					else if (k == dynamicBuffer.Length - 1)
					{
						m_GizmoBatcher.DrawWireNode(position, 1f, green);
					}
					@float = position;
					flag = true;
					flag2 = true;
				}
			}
		}

		private void DrawPathCurve(Bezier4x3 curve, float length, float timeOffset, UnityEngine.Color color)
		{
			if (length >= 1f)
			{
				int arrowCount = (int)math.ceil(length * 0.01f);
				m_GizmoBatcher.DrawFlowCurve(curve, length, color, timeOffset, reverse: false, arrowCount, -1, 1f);
			}
			else
			{
				m_GizmoBatcher.DrawCurve(curve, length, color);
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
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PathOwner> __Game_Pathfind_PathOwner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Vehicle> __Game_Vehicles_Vehicle_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PersonalCar> __Game_Vehicles_PersonalCar_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<DeliveryTruck> __Game_Vehicles_DeliveryTruck_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Creature> __Game_Creatures_Creature_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Citizen> __Game_Citizens_Citizen_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<JobSeeker> __Game_Agents_JobSeeker_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<SchoolSeeker> __Game_Citizens_SchoolSeeker_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Household> __Game_Citizens_Household_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CompanyData> __Game_Companies_CompanyData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Routes.Segment> __Game_Routes_Segment_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<GoodsDeliveryRequest> __Game_Simulation_GoodsDeliveryRequest_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ServiceRequest> __Game_Simulation_ServiceRequest_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<PathElement> __Game_Pathfind_PathElement_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Position> __Game_Routes_Position_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Pathfind_PathOwner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PathOwner>(isReadOnly: true);
			__Game_Vehicles_Vehicle_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Vehicle>(isReadOnly: true);
			__Game_Vehicles_PersonalCar_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PersonalCar>(isReadOnly: true);
			__Game_Vehicles_DeliveryTruck_RO_ComponentTypeHandle = state.GetComponentTypeHandle<DeliveryTruck>(isReadOnly: true);
			__Game_Creatures_Creature_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Creature>(isReadOnly: true);
			__Game_Citizens_Citizen_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Citizen>(isReadOnly: true);
			__Game_Agents_JobSeeker_RO_ComponentTypeHandle = state.GetComponentTypeHandle<JobSeeker>(isReadOnly: true);
			__Game_Citizens_SchoolSeeker_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SchoolSeeker>(isReadOnly: true);
			__Game_Citizens_Household_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Household>(isReadOnly: true);
			__Game_Companies_CompanyData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CompanyData>(isReadOnly: true);
			__Game_Routes_Segment_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Routes.Segment>(isReadOnly: true);
			__Game_Simulation_GoodsDeliveryRequest_RO_ComponentTypeHandle = state.GetComponentTypeHandle<GoodsDeliveryRequest>(isReadOnly: true);
			__Game_Simulation_ServiceRequest_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ServiceRequest>(isReadOnly: true);
			__Game_Pathfind_PathElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<PathElement>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Routes_Position_RO_ComponentLookup = state.GetComponentLookup<Position>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
		}
	}

	private EntityQuery m_PathGroup;

	private GizmosSystem m_GizmosSystem;

	private ToolSystem m_ToolSystem;

	private Option m_PersonalCarOption;

	private Option m_DeliveryTruckOption;

	private Option m_ServiceVehicleOption;

	private Option m_ResidentOption;

	private Option m_CitizenOption;

	private Option m_CompanyOption;

	private Option m_RouteOption;

	private Option m_DeliveryRequestOption;

	private Option m_ServiceRequestOption;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_GizmosSystem = base.World.GetOrCreateSystemManaged<GizmosSystem>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_PathGroup = GetEntityQuery(ComponentType.ReadOnly<PathElement>(), ComponentType.Exclude<Deleted>());
		m_PersonalCarOption = AddOption("Personal cars", defaultEnabled: true);
		m_DeliveryTruckOption = AddOption("Delivery trucks", defaultEnabled: true);
		m_ServiceVehicleOption = AddOption("Service vehicles", defaultEnabled: true);
		m_ResidentOption = AddOption("Citizens (instance)", defaultEnabled: true);
		m_CitizenOption = AddOption("Citizens (agent)", defaultEnabled: false);
		m_CompanyOption = AddOption("Companies", defaultEnabled: false);
		m_RouteOption = AddOption("Transport routes", defaultEnabled: false);
		m_DeliveryRequestOption = AddOption("Delivery requests", defaultEnabled: false);
		m_ServiceRequestOption = AddOption("Service requests", defaultEnabled: false);
		base.Enabled = false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_PathGroup.IsEmptyIgnoreFilter)
		{
			base.Dependency = DrawPathGizmos(m_PathGroup, base.Dependency);
		}
	}

	private JobHandle DrawPathGizmos(EntityQuery group, JobHandle inputDeps)
	{
		JobHandle dependencies;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new PathGizmoJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PathOwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathOwner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_VehicleType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Vehicle_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PersonalCarType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_PersonalCar_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DeliveryTruckType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_DeliveryTruck_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CreatureType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_Creature_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CitizenType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_JobSeekerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Agents_JobSeeker_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SchoolSeekerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_SchoolSeeker_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_HouseholdType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Household_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CompanyType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_CompanyData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_RouteSegmentType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_Segment_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_GoodsDeliveryRequestType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_GoodsDeliveryRequest_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ServiceRequestType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_ServiceRequest_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Pathfind_PathElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Position_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PersonalCarOption = m_PersonalCarOption.enabled,
			m_DeliveryTruckOption = m_DeliveryTruckOption.enabled,
			m_ServiceVehicleOption = m_ServiceVehicleOption.enabled,
			m_ResidentOption = m_ResidentOption.enabled,
			m_CitizenOption = m_CitizenOption.enabled,
			m_CompanyOption = m_CompanyOption.enabled,
			m_RouteOption = m_RouteOption.enabled,
			m_DeliveryRequestOption = m_DeliveryRequestOption.enabled,
			m_ServiceRequestOption = m_ServiceRequestOption.enabled,
			m_TimeOffset = UnityEngine.Time.realtimeSinceStartup,
			m_Selected = m_ToolSystem.selected,
			m_GizmoBatcher = m_GizmosSystem.GetGizmosBatcher(out dependencies)
		}, group, JobHandle.CombineDependencies(inputDeps, dependencies));
		m_GizmosSystem.AddGizmosBatcherWriter(jobHandle);
		return jobHandle;
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
	public PathDebugSystem()
	{
	}
}
