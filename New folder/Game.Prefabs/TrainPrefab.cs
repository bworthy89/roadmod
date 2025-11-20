using System.Collections.Generic;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.PSI;
using Game.Vehicles;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[ExcludeGeneratedModTag]
public abstract class TrainPrefab : VehiclePrefab
{
	public TrackTypes m_TrackType = TrackTypes.Train;

	public EnergyTypes m_EnergyType = EnergyTypes.Electricity;

	public float m_MaxSpeed = 200f;

	public float m_Acceleration = 5f;

	public float m_Braking = 10f;

	public float2 m_Turning = new float2(90f, 15f);

	public float2 m_BogieOffset = new float2(4f, 4f);

	public float2 m_AttachOffset = new float2(0f, 0f);

	public override IEnumerable<string> modTags
	{
		get
		{
			foreach (string modTag in base.modTags)
			{
				yield return modTag;
			}
			foreach (string enumFlagTag in ModTags.GetEnumFlagTags(m_TrackType, TrackTypes.Train))
			{
				yield return enumFlagTag;
			}
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<TrainData>());
		components.Add(ComponentType.ReadWrite<TrainObjectData>());
		components.Add(ComponentType.ReadWrite<UpdateFrameData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
		components.Add(ComponentType.ReadWrite<Train>());
		if (components.Contains(ComponentType.ReadWrite<Stopped>()))
		{
			components.Add(ComponentType.ReadWrite<ParkedTrain>());
		}
		if (components.Contains(ComponentType.ReadWrite<Moving>()))
		{
			components.Add(ComponentType.ReadWrite<TrainNavigation>());
			components.Add(ComponentType.ReadWrite<TrainCurrentLane>());
			components.Add(ComponentType.ReadWrite<TrainBogieFrame>());
			if (components.Contains(ComponentType.ReadWrite<LayoutElement>()))
			{
				components.Add(ComponentType.ReadWrite<PathOwner>());
				components.Add(ComponentType.ReadWrite<PathElement>());
				components.Add(ComponentType.ReadWrite<Target>());
				components.Add(ComponentType.ReadWrite<Blocker>());
				components.Add(ComponentType.ReadWrite<TrainNavigationLane>());
			}
		}
	}

	protected override void RefreshArchetype(EntityManager entityManager, Entity entity)
	{
		List<ComponentBase> list = new List<ComponentBase>();
		GetComponents(list);
		ObjectData componentData = default(ObjectData);
		MovingObjectData componentData2 = default(MovingObjectData);
		TrainObjectData componentData3 = default(TrainObjectData);
		HashSet<ComponentType> hashSet = new HashSet<ComponentType>();
		hashSet.Add(ComponentType.ReadWrite<Controller>());
		hashSet.Add(ComponentType.ReadWrite<Moving>());
		for (int i = 0; i < list.Count; i++)
		{
			list[i].GetArchetypeComponents(hashSet);
		}
		hashSet.Add(ComponentType.ReadWrite<Created>());
		hashSet.Add(ComponentType.ReadWrite<Updated>());
		componentData.m_Archetype = entityManager.CreateArchetype(PrefabUtils.ToArray(hashSet));
		hashSet.Clear();
		hashSet.Add(ComponentType.ReadWrite<Controller>());
		hashSet.Add(ComponentType.ReadWrite<Stopped>());
		for (int j = 0; j < list.Count; j++)
		{
			list[j].GetArchetypeComponents(hashSet);
		}
		hashSet.Add(ComponentType.ReadWrite<Created>());
		hashSet.Add(ComponentType.ReadWrite<Updated>());
		componentData2.m_StoppedArchetype = entityManager.CreateArchetype(PrefabUtils.ToArray(hashSet));
		hashSet.Clear();
		hashSet.Add(ComponentType.ReadWrite<Controller>());
		hashSet.Add(ComponentType.ReadWrite<Moving>());
		hashSet.Add(ComponentType.ReadWrite<LayoutElement>());
		for (int k = 0; k < list.Count; k++)
		{
			list[k].GetArchetypeComponents(hashSet);
		}
		hashSet.Add(ComponentType.ReadWrite<Created>());
		hashSet.Add(ComponentType.ReadWrite<Updated>());
		componentData3.m_ControllerArchetype = entityManager.CreateArchetype(PrefabUtils.ToArray(hashSet));
		hashSet.Clear();
		hashSet.Add(ComponentType.ReadWrite<Controller>());
		hashSet.Add(ComponentType.ReadWrite<Stopped>());
		hashSet.Add(ComponentType.ReadWrite<LayoutElement>());
		for (int l = 0; l < list.Count; l++)
		{
			list[l].GetArchetypeComponents(hashSet);
		}
		hashSet.Add(ComponentType.ReadWrite<Created>());
		hashSet.Add(ComponentType.ReadWrite<Updated>());
		componentData3.m_StoppedControllerArchetype = entityManager.CreateArchetype(PrefabUtils.ToArray(hashSet));
		entityManager.SetComponentData(entity, componentData);
		entityManager.SetComponentData(entity, componentData2);
		entityManager.SetComponentData(entity, componentData3);
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		entityManager.SetComponentData(entity, new UpdateFrameData(3));
	}
}
