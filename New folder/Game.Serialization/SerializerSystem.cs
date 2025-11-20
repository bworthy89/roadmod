using System;
using System.Collections.Generic;
using System.Linq;
using Colossal;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Agents;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Effects;
using Game.Prefabs;
using Game.Routes;
using Game.Simulation;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.Serialization;

public class SerializerSystem : GameSystemBase
{
	private SaveGameSystem m_SaveGameSystem;

	private LoadGameSystem m_LoadGameSystem;

	private WriteSystem m_WriteSystem;

	private ReadSystem m_ReadSystem;

	private UpdateSystem m_UpdateSystem;

	private ComponentSerializerLibrary m_ComponentSerializerLibrary;

	private SystemSerializerLibrary m_SystemSerializerLibrary;

	private EntityQuery m_Query;

	public ComponentSerializerLibrary componentLibrary => m_ComponentSerializerLibrary;

	public SystemSerializerLibrary systemLibrary => m_SystemSerializerLibrary;

	public int totalSize { get; set; }

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SaveGameSystem = base.World.GetOrCreateSystemManaged<SaveGameSystem>();
		m_LoadGameSystem = base.World.GetOrCreateSystemManaged<LoadGameSystem>();
		m_WriteSystem = base.World.GetOrCreateSystemManaged<WriteSystem>();
		m_ReadSystem = base.World.GetOrCreateSystemManaged<ReadSystem>();
		m_UpdateSystem = base.World.GetOrCreateSystemManaged<UpdateSystem>();
		CreateQuery(Array.Empty<ComponentType>());
	}

	[Preserve]
	protected override void OnDestroy()
	{
		if (m_ComponentSerializerLibrary != null)
		{
			m_ComponentSerializerLibrary.Dispose();
		}
		if (m_SystemSerializerLibrary != null)
		{
			m_SystemSerializerLibrary.Dispose();
		}
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_ComponentSerializerLibrary == null)
		{
			m_ComponentSerializerLibrary = new ComponentSerializerLibrary();
		}
		if (m_ComponentSerializerLibrary.isDirty)
		{
			m_ComponentSerializerLibrary.Initialize(this, out var serializableComponents);
			CreateQuery(serializableComponents);
		}
		if (m_SystemSerializerLibrary == null)
		{
			m_SystemSerializerLibrary = new SystemSerializerLibrary();
		}
		if (m_SystemSerializerLibrary.isDirty)
		{
			m_SystemSerializerLibrary.Initialize(base.World);
		}
		switch (m_UpdateSystem.currentPhase)
		{
		case SystemUpdatePhase.Serialize:
		{
			EntitySerializer<WriteBuffer> entitySerializer = new EntitySerializer<WriteBuffer>(base.EntityManager, m_ComponentSerializerLibrary, m_SystemSerializerLibrary, m_WriteSystem);
			try
			{
				totalSize = 0;
				Context context2 = m_SaveGameSystem.context;
				entitySerializer.Serialize<BinaryWriter, FormatTags>(context2, m_Query, BufferFormat.CompressedZStd, new ComponentType[1] { ComponentType.ReadWrite<PrefabData>() });
				break;
			}
			finally
			{
				entitySerializer.Dispose();
			}
		}
		case SystemUpdatePhase.Deserialize:
		{
			EntityDeserializer<ReadBuffer> entityDeserializer = new EntityDeserializer<ReadBuffer>(base.EntityManager, m_ComponentSerializerLibrary, m_SystemSerializerLibrary, m_ReadSystem);
			try
			{
				totalSize = 0;
				Context context = m_LoadGameSystem.context;
				bool num = entityDeserializer.Deserialize<BinaryReader, FormatTags>(ref context, Array.Empty<ComponentType>());
				COSystemBase.baseLog.InfoFormat("Serialized version: {0}", context.version);
				if (num)
				{
					string[] names = Enum.GetNames(typeof(FormatTags));
					FormatTags[] array = (FormatTags[])Enum.GetValues(typeof(FormatTags));
					List<string> list = new List<string>(names.Length);
					for (int i = 0; i < array.Length; i++)
					{
						if (context.format.Has(array[i]))
						{
							list.Add(names[i]);
						}
					}
					COSystemBase.baseLog.InfoFormat("Format tags: {0}", string.Join(", ", list));
				}
				else
				{
					Colossal.Hash128 instigatorGuid = context.instigatorGuid;
					Colossal.Serialization.Entities.Purpose purpose = context.purpose switch
					{
						Colossal.Serialization.Entities.Purpose.LoadMap => Colossal.Serialization.Entities.Purpose.NewMap, 
						Colossal.Serialization.Entities.Purpose.LoadGame => Colossal.Serialization.Entities.Purpose.NewGame, 
						_ => context.purpose, 
					};
					if (purpose != context.purpose)
					{
						context.Dispose();
						context = new Context(purpose, Version.current, instigatorGuid, Enum.GetNames(typeof(FormatTags)).Length, Allocator.Persistent);
					}
				}
				m_LoadGameSystem.context = context;
				break;
			}
			finally
			{
				entityDeserializer.Dispose();
			}
		}
		}
	}

	public void SetDirty()
	{
		if (m_ComponentSerializerLibrary != null)
		{
			m_ComponentSerializerLibrary.SetDirty();
		}
		if (m_SystemSerializerLibrary != null)
		{
			m_SystemSerializerLibrary.SetDirty();
		}
	}

	private void CreateQuery(IEnumerable<ComponentType> serializableComponents)
	{
		HashSet<ComponentType> hashSet = new HashSet<ComponentType>
		{
			ComponentType.ReadOnly<LoadedIndex>(),
			ComponentType.ReadOnly<PrefabRef>(),
			ComponentType.ReadOnly<ElectricityFlowNode>(),
			ComponentType.ReadOnly<ElectricityFlowEdge>(),
			ComponentType.ReadOnly<WaterPipeNode>(),
			ComponentType.ReadOnly<WaterPipeEdge>(),
			ComponentType.ReadOnly<ServiceRequest>(),
			ComponentType.ReadOnly<Game.Simulation.WaterSourceData>(),
			ComponentType.ReadOnly<Game.City.City>(),
			ComponentType.ReadOnly<SchoolSeeker>(),
			ComponentType.ReadOnly<JobSeeker>(),
			ComponentType.ReadOnly<CityStatistic>(),
			ComponentType.ReadOnly<ServiceBudgetData>(),
			ComponentType.ReadOnly<FloodCounterData>(),
			ComponentType.ReadOnly<CoordinatedMeeting>(),
			ComponentType.ReadOnly<LookingForPartner>(),
			ComponentType.ReadOnly<AtmosphereData>(),
			ComponentType.ReadOnly<BiomeData>(),
			ComponentType.ReadOnly<TimeData>()
		};
		foreach (ComponentType serializableComponent in serializableComponents)
		{
			hashSet.Add(ComponentType.ReadOnly(serializableComponent.TypeIndex));
		}
		m_Query = GetEntityQuery(new EntityQueryDesc
		{
			Any = hashSet.ToArray(),
			None = new ComponentType[5]
			{
				ComponentType.ReadOnly<NetCompositionData>(),
				ComponentType.ReadOnly<EffectInstance>(),
				ComponentType.ReadOnly<LivePath>(),
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
	}

	[Preserve]
	public SerializerSystem()
	{
	}
}
