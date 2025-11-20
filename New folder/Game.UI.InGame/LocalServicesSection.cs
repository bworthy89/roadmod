using System.Runtime.CompilerServices;
using Colossal.UI.Binding;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class LocalServicesSection : InfoSectionBase
{
	private ImageSystem m_ImageSystem;

	private EntityQuery m_ServiceDistrictBuildingQuery;

	protected override string group => "LocalServicesSection";

	private NativeList<Entity> localServiceBuildings { get; set; }

	private NativeList<Entity> prefabs { get; set; }

	protected override void Reset()
	{
		localServiceBuildings.Clear();
		prefabs.Clear();
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ImageSystem = base.World.GetOrCreateSystemManaged<ImageSystem>();
		m_ServiceDistrictBuildingQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<ServiceDistrict>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		localServiceBuildings = new NativeList<Entity>(Allocator.Persistent);
		prefabs = new NativeList<Entity>(Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		localServiceBuildings.Dispose();
		prefabs.Dispose();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = base.EntityManager.HasComponent<District>(selectedEntity) && base.EntityManager.HasComponent<Area>(selectedEntity);
	}

	protected override void OnProcess()
	{
		NativeArray<Entity> nativeArray = m_ServiceDistrictBuildingQuery.ToEntityArray(Allocator.TempJob);
		NativeArray<PrefabRef> nativeArray2 = m_ServiceDistrictBuildingQuery.ToComponentDataArray<PrefabRef>(Allocator.TempJob);
		try
		{
			for (int i = 0; i < nativeArray.Length; i++)
			{
				DynamicBuffer<ServiceDistrict> buffer = base.EntityManager.GetBuffer<ServiceDistrict>(nativeArray[i], isReadOnly: true);
				for (int j = 0; j < buffer.Length; j++)
				{
					if (buffer[j].m_District == selectedEntity)
					{
						localServiceBuildings.Add(nativeArray[i]);
						NativeList<Entity> nativeList = prefabs;
						PrefabRef prefabRef = nativeArray2[i];
						nativeList.Add(in prefabRef.m_Prefab);
						break;
					}
				}
			}
		}
		finally
		{
			nativeArray.Dispose();
			nativeArray2.Dispose();
		}
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("localServiceBuildings");
		writer.ArrayBegin(localServiceBuildings.Length);
		for (int i = 0; i < localServiceBuildings.Length; i++)
		{
			writer.TypeBegin("selectedInfo.LocalServiceBuilding");
			writer.PropertyName("name");
			m_NameSystem.BindName(writer, localServiceBuildings[i]);
			writer.PropertyName("serviceIcon");
			writer.Write(m_ImageSystem.GetGroupIcon(prefabs[i]));
			writer.PropertyName("entity");
			writer.Write(localServiceBuildings[i]);
			writer.TypeEnd();
		}
		writer.ArrayEnd();
	}

	[Preserve]
	public LocalServicesSection()
	{
	}
}
