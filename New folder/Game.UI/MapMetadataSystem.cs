using System.Runtime.CompilerServices;
using Colossal.Annotations;
using Colossal.Entities;
using Colossal.Json;
using Colossal.Mathematics;
using Colossal.UI.Binding;
using Game.Areas;
using Game.City;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Prefabs.Climate;
using Game.Simulation;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.UI;

[CompilerGenerated]
public class MapMetadataSystem : GameSystemBase
{
	public struct Resources : IJsonWritable
	{
		public float fertile;

		public float forest;

		public float oil;

		public float ore;

		public float fish;

		public ProxyObject ToVariant()
		{
			return new ProxyObject
			{
				{
					"fertile",
					new ProxyNumber(fertile)
				},
				{
					"forest",
					new ProxyNumber(forest)
				},
				{
					"oil",
					new ProxyNumber(oil)
				},
				{
					"ore",
					new ProxyNumber(ore)
				},
				{
					"fish",
					new ProxyNumber(fish)
				}
			};
		}

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin(GetType().FullName);
			writer.PropertyName("fertile");
			writer.Write(fertile);
			writer.PropertyName("forest");
			writer.Write(forest);
			writer.PropertyName("oil");
			writer.Write(oil);
			writer.PropertyName("ore");
			writer.Write(ore);
			writer.PropertyName("fish");
			writer.Write(fish);
			writer.TypeEnd();
		}
	}

	public struct Connections : IJsonWritable
	{
		public bool road;

		public bool train;

		public bool air;

		public bool ship;

		public bool electricity;

		public bool water;

		public ProxyObject ToVariant()
		{
			return new ProxyObject
			{
				{
					"road",
					new ProxyBoolean(road)
				},
				{
					"train",
					new ProxyBoolean(train)
				},
				{
					"air",
					new ProxyBoolean(air)
				},
				{
					"ship",
					new ProxyBoolean(ship)
				},
				{
					"electricity",
					new ProxyBoolean(electricity)
				},
				{
					"water",
					new ProxyBoolean(water)
				}
			};
		}

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin(GetType().FullName);
			writer.PropertyName("road");
			writer.Write(road);
			writer.PropertyName("train");
			writer.Write(train);
			writer.PropertyName("air");
			writer.Write(air);
			writer.PropertyName("ship");
			writer.Write(ship);
			writer.PropertyName("electricity");
			writer.Write(electricity);
			writer.PropertyName("water");
			writer.Write(water);
			writer.TypeEnd();
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public BufferTypeHandle<MapFeatureElement> __Game_Areas_MapFeatureElement_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.ElectricityOutsideConnection> __Game_Objects_ElectricityOutsideConnection_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.WaterPipeOutsideConnection> __Game_Objects_WaterPipeOutsideConnection_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Areas_MapFeatureElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<MapFeatureElement>(isReadOnly: true);
			__Game_Objects_ElectricityOutsideConnection_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.ElectricityOutsideConnection>(isReadOnly: true);
			__Game_Objects_WaterPipeOutsideConnection_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.WaterPipeOutsideConnection>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
		}
	}

	private PlanetarySystem m_PlanetarySystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private ClimateSystem m_ClimateSystem;

	private PrefabSystem m_PrefabSystem;

	private float m_Area;

	private float m_BuildableLand;

	private float m_SurfaceWaterAvailability;

	private float m_GroundWaterAvailability;

	private Resources m_Resources;

	private Connections m_Connections;

	private EntityQuery m_MapTileQuery;

	private EntityQuery m_OutsideConnectionQuery;

	private TypeHandle __TypeHandle;

	[CanBeNull]
	public string mapName { get; set; }

	[CanBeNull]
	public string theme
	{
		get
		{
			if (m_CityConfigurationSystem.defaultTheme != Entity.Null)
			{
				return m_PrefabSystem.GetPrefab<ThemePrefab>(m_CityConfigurationSystem.defaultTheme).name;
			}
			return null;
		}
	}

	public Bounds1 temperatureRange
	{
		get
		{
			if (m_ClimateSystem.currentClimate != Entity.Null)
			{
				return m_PrefabSystem.GetPrefab<ClimatePrefab>(m_ClimateSystem.currentClimate).temperatureRange;
			}
			return default(Bounds1);
		}
	}

	public float cloudiness
	{
		get
		{
			if (m_ClimateSystem.currentClimate != Entity.Null)
			{
				return m_PrefabSystem.GetPrefab<ClimatePrefab>(m_ClimateSystem.currentClimate).averageCloudiness;
			}
			return 0f;
		}
	}

	public float precipitation
	{
		get
		{
			if (m_ClimateSystem.currentClimate != Entity.Null)
			{
				return m_PrefabSystem.GetPrefab<ClimatePrefab>(m_ClimateSystem.currentClimate).averagePrecipitation;
			}
			return 0f;
		}
	}

	public float latitude => m_PlanetarySystem.latitude;

	public float longitude => m_PlanetarySystem.longitude;

	public float area => m_Area;

	public float buildableLand => m_BuildableLand;

	public float surfaceWaterAvailability => m_SurfaceWaterAvailability;

	public float groundWaterAvailability => m_GroundWaterAvailability;

	public Resources resources => m_Resources;

	public Connections connections => m_Connections;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PlanetarySystem = base.World.GetOrCreateSystemManaged<PlanetarySystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_ClimateSystem = base.World.GetOrCreateSystemManaged<ClimateSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_MapTileQuery = GetEntityQuery(ComponentType.ReadOnly<MapTile>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_OutsideConnectionQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Game.Objects.OutsideConnection>(),
				ComponentType.ReadOnly<Game.Objects.ElectricityOutsideConnection>(),
				ComponentType.ReadOnly<Game.Objects.WaterPipeOutsideConnection>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
	}

	[Preserve]
	protected override void OnUpdate()
	{
		CompleteDependency();
		UpdateResources();
		UpdateConnections();
	}

	private void UpdateResources()
	{
		m_Area = 0f;
		m_BuildableLand = 0f;
		m_SurfaceWaterAvailability = 0f;
		m_GroundWaterAvailability = 0f;
		m_Resources = default(Resources);
		BufferTypeHandle<MapFeatureElement> bufferTypeHandle = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_MapFeatureElement_RO_BufferTypeHandle, ref base.CheckedStateRef);
		NativeArray<ArchetypeChunk> nativeArray = m_MapTileQuery.ToArchetypeChunkArray(Allocator.Temp);
		try
		{
			for (int i = 0; i < nativeArray.Length; i++)
			{
				BufferAccessor<MapFeatureElement> bufferAccessor = nativeArray[i].GetBufferAccessor(ref bufferTypeHandle);
				for (int j = 0; j < bufferAccessor.Length; j++)
				{
					DynamicBuffer<MapFeatureElement> dynamicBuffer = bufferAccessor[j];
					m_Area += dynamicBuffer[0].m_Amount;
					m_BuildableLand += dynamicBuffer[1].m_Amount;
					m_SurfaceWaterAvailability += dynamicBuffer[6].m_Amount;
					m_GroundWaterAvailability += dynamicBuffer[7].m_Amount;
					m_Resources.fertile += dynamicBuffer[2].m_Amount;
					m_Resources.forest += dynamicBuffer[3].m_Amount;
					m_Resources.oil += dynamicBuffer[4].m_Amount;
					m_Resources.ore += dynamicBuffer[5].m_Amount;
					m_Resources.fish += dynamicBuffer[8].m_Amount;
				}
			}
		}
		finally
		{
			nativeArray.Dispose();
		}
	}

	private void UpdateConnections()
	{
		m_Connections = default(Connections);
		ComponentTypeHandle<Game.Objects.ElectricityOutsideConnection> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_ElectricityOutsideConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<Game.Objects.WaterPipeOutsideConnection> typeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_WaterPipeOutsideConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<PrefabRef> typeHandle3 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		NativeArray<ArchetypeChunk> nativeArray = m_OutsideConnectionQuery.ToArchetypeChunkArray(Allocator.Temp);
		try
		{
			for (int i = 0; i < nativeArray.Length; i++)
			{
				ArchetypeChunk archetypeChunk = nativeArray[i];
				NativeArray<PrefabRef> nativeArray2 = archetypeChunk.GetNativeArray(ref typeHandle3);
				m_Connections.electricity |= archetypeChunk.Has(ref typeHandle);
				m_Connections.water |= archetypeChunk.Has(ref typeHandle2);
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					if (base.EntityManager.TryGetComponent<OutsideConnectionData>(nativeArray2[j].m_Prefab, out var component))
					{
						m_Connections.road |= (component.m_Type & OutsideConnectionTransferType.Road) != 0;
						m_Connections.train |= (component.m_Type & OutsideConnectionTransferType.Train) != 0;
						m_Connections.air |= (component.m_Type & OutsideConnectionTransferType.Air) != 0;
						m_Connections.ship |= (component.m_Type & OutsideConnectionTransferType.Ship) != 0;
					}
				}
			}
		}
		finally
		{
			nativeArray.Dispose();
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
	public MapMetadataSystem()
	{
	}
}
