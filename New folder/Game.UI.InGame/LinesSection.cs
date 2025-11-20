using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Routes;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class LinesSection : InfoSectionBase
{
	private enum Result
	{
		HasRoutes,
		HasPassengers
	}

	[BurstCompile]
	private struct LinesJob : IJob
	{
		[ReadOnly]
		public Entity m_SelectedEntity;

		[ReadOnly]
		public ComponentLookup<Owner> m_Owners;

		[ReadOnly]
		public ComponentLookup<WaitingPassengers> m_WaitingPassengers;

		[ReadOnly]
		public ComponentLookup<Attached> m_Attached;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefs;

		[ReadOnly]
		public ComponentLookup<RouteData> m_RouteDatas;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjectBuffers;

		[ReadOnly]
		public BufferLookup<ConnectedRoute> m_ConnectedRouteBuffers;

		[ReadOnly]
		public BufferLookup<BuildingUpgradeElement> m_BuildingUpgradeElements;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> m_SubNetBuffers;

		[ReadOnly]
		public BufferLookup<SubRoute> m_SubRouteBuffers;

		public NativeArray<bool> m_BoolResult;

		public NativeList<Entity> m_LinesResult;

		public NativeArray<int> m_PassengersResult;

		public void Execute()
		{
			bool supportRoutes = false;
			bool supportPassengers = false;
			int passengerCount = 0;
			CheckEntity(m_SelectedEntity, ref supportRoutes, ref supportPassengers, ref passengerCount);
			m_PassengersResult[0] = passengerCount;
			m_BoolResult[0] = supportRoutes;
			m_BoolResult[1] = supportPassengers;
		}

		private void CheckEntity(Entity entity, ref bool supportRoutes, ref bool supportPassengers, ref int passengerCount)
		{
			if (m_ConnectedRouteBuffers.TryGetBuffer(entity, out var bufferData))
			{
				supportRoutes = true;
				for (int i = 0; i < bufferData.Length; i++)
				{
					if (m_WaitingPassengers.TryGetComponent(bufferData[i].m_Waypoint, out var componentData))
					{
						supportPassengers = true;
						passengerCount += componentData.m_Count;
					}
					if (m_Owners.TryGetComponent(bufferData[i].m_Waypoint, out var componentData2) && !m_LinesResult.Contains(componentData2.m_Owner))
					{
						m_LinesResult.Add(in componentData2.m_Owner);
					}
				}
			}
			if (m_WaitingPassengers.TryGetComponent(entity, out var componentData3))
			{
				supportPassengers = true;
				passengerCount += componentData3.m_Count;
			}
			if (m_SubObjectBuffers.TryGetBuffer(entity, out var bufferData2))
			{
				for (int j = 0; j < bufferData2.Length; j++)
				{
					CheckEntity(bufferData2[j].m_SubObject, ref supportRoutes, ref supportPassengers, ref passengerCount);
				}
			}
			if (m_SubNetBuffers.TryGetBuffer(entity, out var bufferData3))
			{
				for (int k = 0; k < bufferData3.Length; k++)
				{
					if (m_SubObjectBuffers.TryGetBuffer(bufferData3[k].m_SubNet, out var bufferData4))
					{
						for (int l = 0; l < bufferData4.Length; l++)
						{
							CheckEntity(bufferData4[l].m_SubObject, ref supportRoutes, ref supportPassengers, ref passengerCount);
						}
					}
				}
			}
			if (!m_Attached.TryGetComponent(entity, out var componentData4) || !m_PrefabRefs.TryGetComponent(componentData4.m_Parent, out var componentData5) || !m_BuildingUpgradeElements.TryGetBuffer(componentData5.m_Prefab, out var bufferData5))
			{
				return;
			}
			for (int m = 0; m < bufferData5.Length; m++)
			{
				if (!m_RouteDatas.HasComponent(bufferData5[m].m_Upgrade))
				{
					continue;
				}
				supportRoutes = true;
				if (!m_SubRouteBuffers.TryGetBuffer(componentData4.m_Parent, out var bufferData6))
				{
					break;
				}
				for (int n = 0; n < bufferData6.Length; n++)
				{
					if (!m_LinesResult.Contains(bufferData6[n].m_Route))
					{
						ref NativeList<Entity> reference = ref m_LinesResult;
						SubRoute subRoute = bufferData6[n];
						reference.Add(in subRoute.m_Route);
					}
				}
				break;
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WaitingPassengers> __Game_Routes_WaitingPassengers_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attached> __Game_Objects_Attached_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RouteData> __Game_Prefabs_RouteData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> __Game_Net_SubNet_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedRoute> __Game_Routes_ConnectedRoute_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubRoute> __Game_Routes_SubRoute_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<BuildingUpgradeElement> __Game_Prefabs_BuildingUpgradeElement_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Routes_WaitingPassengers_RO_ComponentLookup = state.GetComponentLookup<WaitingPassengers>(isReadOnly: true);
			__Game_Objects_Attached_RO_ComponentLookup = state.GetComponentLookup<Attached>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_RouteData_RO_ComponentLookup = state.GetComponentLookup<RouteData>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Net_SubNet_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubNet>(isReadOnly: true);
			__Game_Routes_ConnectedRoute_RO_BufferLookup = state.GetBufferLookup<ConnectedRoute>(isReadOnly: true);
			__Game_Routes_SubRoute_RO_BufferLookup = state.GetBufferLookup<SubRoute>(isReadOnly: true);
			__Game_Prefabs_BuildingUpgradeElement_RO_BufferLookup = state.GetBufferLookup<BuildingUpgradeElement>(isReadOnly: true);
		}
	}

	private TransportationOverviewUISystem m_TransportationOverviewUISystem;

	private NativeArray<bool> m_BoolResult;

	private NativeArray<int> m_PassengersResult;

	private NativeList<Entity> m_LinesResult;

	private TypeHandle __TypeHandle;

	protected override string group => "LinesSection";

	protected override bool displayForOutsideConnections => true;

	protected override bool displayForUpgrades => true;

	private NativeList<Entity> lines { get; set; }

	protected override void Reset()
	{
		lines.Clear();
		m_LinesResult.Clear();
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_TransportationOverviewUISystem = base.World.GetOrCreateSystemManaged<TransportationOverviewUISystem>();
		m_BoolResult = new NativeArray<bool>(2, Allocator.Persistent);
		m_PassengersResult = new NativeArray<int>(1, Allocator.Persistent);
		m_LinesResult = new NativeList<Entity>(Allocator.Persistent);
		lines = new NativeList<Entity>(Allocator.Persistent);
		AddBinding(new TriggerBinding<Entity, bool>(group, "toggle", OnToggle));
	}

	[Preserve]
	protected override void OnDestroy()
	{
		lines.Dispose();
		m_LinesResult.Dispose();
		m_PassengersResult.Dispose();
		m_BoolResult.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		IJobExtensions.Schedule(new LinesJob
		{
			m_SelectedEntity = selectedEntity,
			m_Owners = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WaitingPassengers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_WaitingPassengers_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Attached = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attached_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RouteDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RouteData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubObjectBuffers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubNetBuffers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubNet_RO_BufferLookup, ref base.CheckedStateRef),
			m_ConnectedRouteBuffers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_ConnectedRoute_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubRouteBuffers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_SubRoute_RO_BufferLookup, ref base.CheckedStateRef),
			m_BuildingUpgradeElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_BuildingUpgradeElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_BoolResult = m_BoolResult,
			m_LinesResult = m_LinesResult,
			m_PassengersResult = m_PassengersResult
		}, base.Dependency).Complete();
		base.visible = m_BoolResult[0] || m_BoolResult[1];
	}

	protected override void OnProcess()
	{
		for (int i = 0; i < m_LinesResult.Length; i++)
		{
			lines.Add(m_LinesResult[i]);
		}
		base.tooltipTags.Add("CargoRoute");
		base.tooltipTags.Add("WorkRoute");
		base.tooltipTags.Add("TransportLine");
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("hasLines");
		writer.Write(m_BoolResult[0]);
		writer.PropertyName("lines");
		writer.ArrayBegin(lines.Length);
		for (int i = 0; i < lines.Length; i++)
		{
			writer.TypeBegin("Game.UI.LinesSection.Line");
			writer.PropertyName("name");
			m_NameSystem.BindName(writer, lines[i]);
			writer.PropertyName("color");
			if (base.EntityManager.TryGetComponent<Game.Routes.Color>(lines[i], out var component))
			{
				writer.Write(component.m_Color);
			}
			else
			{
				writer.Write(UnityEngine.Color.white);
			}
			writer.PropertyName("entity");
			writer.Write(lines[i]);
			bool value = !RouteUtils.CheckOption(base.EntityManager.GetComponentData<Route>(lines[i]), RouteOption.Inactive);
			writer.PropertyName("active");
			writer.Write(value);
			writer.TypeEnd();
		}
		writer.ArrayEnd();
		writer.PropertyName("hasPassengers");
		writer.Write(m_BoolResult[1]);
		writer.PropertyName("passengers");
		writer.Write(m_PassengersResult[0]);
	}

	private void OnToggle(Entity entity, bool state)
	{
		m_TransportationOverviewUISystem.SetLineState(entity, state);
		m_InfoUISystem.RequestUpdate();
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
	public LinesSection()
	{
	}
}
