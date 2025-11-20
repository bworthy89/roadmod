using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Rendering;

[CompilerGenerated]
public class UndergroundViewSystem : GameSystemBase
{
	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<InfoviewNetGeometryData> __Game_Prefabs_InfoviewNetGeometryData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewNetStatusData> __Game_Prefabs_InfoviewNetStatusData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewCoverageData> __Game_Prefabs_InfoviewCoverageData_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_InfoviewNetGeometryData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfoviewNetGeometryData>(isReadOnly: true);
			__Game_Prefabs_InfoviewNetStatusData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfoviewNetStatusData>(isReadOnly: true);
			__Game_Prefabs_InfoviewCoverageData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfoviewCoverageData>(isReadOnly: true);
		}
	}

	private ToolSystem m_ToolSystem;

	private UtilityLodUpdateSystem m_UtilityLodUpdateSystem;

	private RenderingSystem m_RenderingSystem;

	private EntityQuery m_InfomodeQuery;

	private bool m_LastWasWaterways;

	private bool m_LastWasMarkers;

	private bool m_Loaded;

	private UtilityTypes m_LastUtilityTypes;

	private TypeHandle __TypeHandle;

	public bool undergroundOn { get; private set; }

	public bool tunnelsOn { get; private set; }

	public bool pipelinesOn { get; private set; }

	public bool subPipelinesOn { get; private set; }

	public bool waterwaysOn { get; private set; }

	public bool contourLinesOn { get; private set; }

	public bool markersOn { get; private set; }

	public UtilityTypes utilityTypes { get; private set; }

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_UtilityLodUpdateSystem = base.World.GetOrCreateSystemManaged<UtilityLodUpdateSystem>();
		m_RenderingSystem = base.World.GetOrCreateSystemManaged<RenderingSystem>();
		m_InfomodeQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<InfomodeActive>() },
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<InfoviewNetGeometryData>(),
				ComponentType.ReadOnly<InfoviewNetStatusData>(),
				ComponentType.ReadOnly<InfoviewCoverageData>()
			}
		});
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
		bool loaded = GetLoaded();
		if (m_ToolSystem.activeTool != null)
		{
			m_ToolSystem.activeTool.GetAvailableSnapMask(out var onMask, out var offMask);
			undergroundOn = m_ToolSystem.activeTool.requireUnderground;
			tunnelsOn = m_ToolSystem.activeTool.requireUnderground || (m_ToolSystem.activeTool.requireNet & (Layer.Road | Layer.TrainTrack | Layer.Pathway | Layer.TramTrack | Layer.SubwayTrack | Layer.PublicTransportRoad)) != 0;
			subPipelinesOn = (m_ToolSystem.activeTool.requireNet & (Layer.PowerlineLow | Layer.PowerlineHigh | Layer.WaterPipe | Layer.SewagePipe)) != Layer.None || (undergroundOn && (m_ToolSystem.activeTool.requireNet & Layer.ResourceLine) != 0);
			pipelinesOn = m_ToolSystem.activeTool.requirePipelines || subPipelinesOn || (undergroundOn && tunnelsOn);
			waterwaysOn = (m_ToolSystem.activeTool.requireNet & Layer.Waterway) != 0;
			contourLinesOn = (ToolBaseSystem.GetActualSnap(m_ToolSystem.activeTool.selectedSnap, onMask, offMask) & Snap.ContourLines) != 0;
		}
		else
		{
			undergroundOn = false;
			tunnelsOn = false;
			pipelinesOn = false;
			subPipelinesOn = false;
			waterwaysOn = false;
			contourLinesOn = false;
		}
		markersOn = !m_RenderingSystem.hideOverlay;
		utilityTypes = UtilityTypes.None;
		if (!m_InfomodeQuery.IsEmptyIgnoreFilter)
		{
			ComponentTypeHandle<InfoviewNetGeometryData> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewNetGeometryData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<InfoviewNetStatusData> typeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewNetStatusData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<InfoviewCoverageData> typeHandle3 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewCoverageData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			NativeArray<ArchetypeChunk> nativeArray = m_InfomodeQuery.ToArchetypeChunkArray(Allocator.TempJob);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				ArchetypeChunk archetypeChunk = nativeArray[i];
				NativeArray<InfoviewNetGeometryData> nativeArray2 = archetypeChunk.GetNativeArray(ref typeHandle);
				NativeArray<InfoviewNetStatusData> nativeArray3 = archetypeChunk.GetNativeArray(ref typeHandle2);
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					switch (nativeArray2[j].m_Type)
					{
					case NetType.Road:
						tunnelsOn = true;
						break;
					case NetType.TrainTrack:
						tunnelsOn = true;
						break;
					case NetType.TramTrack:
						tunnelsOn = true;
						break;
					case NetType.Waterway:
						waterwaysOn = true;
						break;
					case NetType.SubwayTrack:
						tunnelsOn = true;
						break;
					}
				}
				for (int k = 0; k < nativeArray3.Length; k++)
				{
					switch (nativeArray3[k].m_Type)
					{
					case NetStatusType.Wear:
						tunnelsOn = true;
						break;
					case NetStatusType.TrafficFlow:
						tunnelsOn = true;
						break;
					case NetStatusType.TrafficVolume:
						tunnelsOn = true;
						break;
					case NetStatusType.LowVoltageFlow:
						pipelinesOn = true;
						subPipelinesOn = true;
						utilityTypes |= UtilityTypes.LowVoltageLine;
						break;
					case NetStatusType.HighVoltageFlow:
						pipelinesOn = true;
						subPipelinesOn = true;
						utilityTypes |= UtilityTypes.HighVoltageLine;
						break;
					case NetStatusType.PipeWaterFlow:
						pipelinesOn = true;
						subPipelinesOn = true;
						utilityTypes |= UtilityTypes.WaterPipe;
						break;
					case NetStatusType.PipeSewageFlow:
						pipelinesOn = true;
						subPipelinesOn = true;
						utilityTypes |= UtilityTypes.SewagePipe;
						break;
					case NetStatusType.OilFlow:
						pipelinesOn = true;
						subPipelinesOn = true;
						utilityTypes |= UtilityTypes.Resource;
						break;
					}
				}
				if (archetypeChunk.Has(ref typeHandle3))
				{
					tunnelsOn = true;
				}
			}
			nativeArray.Dispose();
		}
		if (utilityTypes != m_LastUtilityTypes)
		{
			m_LastUtilityTypes = utilityTypes;
			if (!loaded)
			{
				m_UtilityLodUpdateSystem.Update();
			}
		}
		if (waterwaysOn != m_LastWasWaterways)
		{
			m_LastWasWaterways = waterwaysOn;
			Camera main = Camera.main;
			if (main != null)
			{
				if (waterwaysOn)
				{
					main.cullingMask |= 1 << LayerMask.NameToLayer("Waterway");
				}
				else
				{
					main.cullingMask &= ~(1 << LayerMask.NameToLayer("Waterway"));
				}
			}
		}
		if (markersOn == m_LastWasMarkers)
		{
			return;
		}
		m_LastWasMarkers = markersOn;
		Camera main2 = Camera.main;
		if (main2 != null)
		{
			if (markersOn)
			{
				main2.cullingMask |= 1 << LayerMask.NameToLayer("Marker");
			}
			else
			{
				main2.cullingMask &= ~(1 << LayerMask.NameToLayer("Marker"));
			}
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
	public UndergroundViewSystem()
	{
	}
}
