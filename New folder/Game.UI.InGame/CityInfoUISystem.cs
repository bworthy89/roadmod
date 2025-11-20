using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.City;
using Game.Prefabs;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class CityInfoUISystem : UISystemBase, IDefaultSerializable, ISerializable
{
	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<Locked> __Game_Prefabs_Locked_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_Locked_RO_ComponentLookup = state.GetComponentLookup<Locked>(isReadOnly: true);
		}
	}

	public const string kGroup = "cityInfo";

	private SimulationSystem m_SimulationSystem;

	private ResidentialDemandSystem m_ResidentialDemandSystem;

	private CommercialDemandSystem m_CommercialDemandSystem;

	private IndustrialDemandSystem m_IndustrialDemandSystem;

	private CitySystem m_CitySystem;

	private CitizenHappinessSystem m_CitizenHappinessSystem;

	private RawValueBinding m_ResidentialLowFactors;

	private RawValueBinding m_ResidentialMediumFactors;

	private RawValueBinding m_ResidentialHighFactors;

	private RawValueBinding m_CommercialFactors;

	private RawValueBinding m_IndustrialFactors;

	private RawValueBinding m_OfficeFactors;

	private RawValueBinding m_HappinessFactors;

	private float m_ResidentialLowDemand;

	private float m_ResidentialMediumDemand;

	private float m_ResidentialHighDemand;

	private float m_CommercialDemand;

	private float m_IndustrialDemand;

	private float m_OfficeDemand;

	private uint m_LastFrameIndex;

	private int m_AvgHappiness;

	private UIUpdateState m_UpdateState;

	private TypeHandle __TypeHandle;

	private float m_ResidentialLowDemandBindingValue => MathUtils.Snap(m_ResidentialLowDemand, 0.001f);

	private float m_ResidentialMediumDemandBindingValue => MathUtils.Snap(m_ResidentialMediumDemand, 0.001f);

	private float m_ResidentialHighDemandBindingValue => MathUtils.Snap(m_ResidentialHighDemand, 0.001f);

	private float m_CommercialDemandBindingValue => MathUtils.Snap(m_CommercialDemand, 0.001f);

	private float m_IndustrialDemandBindingValue => MathUtils.Snap(m_IndustrialDemand, 0.001f);

	private float m_OfficeDemandBindingValue => MathUtils.Snap(m_OfficeDemand, 0.001f);

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_ResidentialDemandSystem = base.World.GetOrCreateSystemManaged<ResidentialDemandSystem>();
		m_CommercialDemandSystem = base.World.GetOrCreateSystemManaged<CommercialDemandSystem>();
		m_IndustrialDemandSystem = base.World.GetOrCreateSystemManaged<IndustrialDemandSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_CitizenHappinessSystem = base.World.GetOrCreateSystemManaged<CitizenHappinessSystem>();
		AddUpdateBinding(new GetterValueBinding<float>("cityInfo", "residentialLowDemand", () => m_ResidentialLowDemandBindingValue));
		AddUpdateBinding(new GetterValueBinding<float>("cityInfo", "residentialMediumDemand", () => m_ResidentialMediumDemandBindingValue));
		AddUpdateBinding(new GetterValueBinding<float>("cityInfo", "residentialHighDemand", () => m_ResidentialHighDemandBindingValue));
		AddUpdateBinding(new GetterValueBinding<float>("cityInfo", "commercialDemand", () => m_CommercialDemandBindingValue));
		AddUpdateBinding(new GetterValueBinding<float>("cityInfo", "industrialDemand", () => m_IndustrialDemandBindingValue));
		AddUpdateBinding(new GetterValueBinding<float>("cityInfo", "officeDemand", () => m_OfficeDemandBindingValue));
		AddUpdateBinding(new GetterValueBinding<int>("cityInfo", "happiness", () => m_AvgHappiness));
		AddBinding(m_ResidentialLowFactors = new RawValueBinding("cityInfo", "residentialLowFactors", WriteResidentialLowFactors));
		AddBinding(m_ResidentialMediumFactors = new RawValueBinding("cityInfo", "residentialMediumFactors", WriteResidentialMediumFactors));
		AddBinding(m_ResidentialHighFactors = new RawValueBinding("cityInfo", "residentialHighFactors", WriteResidentialHighFactors));
		AddBinding(m_CommercialFactors = new RawValueBinding("cityInfo", "commercialFactors", WriteCommercialFactors));
		AddBinding(m_IndustrialFactors = new RawValueBinding("cityInfo", "industrialFactors", WriteIndustrialFactors));
		AddBinding(m_OfficeFactors = new RawValueBinding("cityInfo", "officeFactors", WriteOfficeFactors));
		AddBinding(m_HappinessFactors = new RawValueBinding("cityInfo", "happinessFactors", WriteHappinessFactors));
		m_UpdateState = UIUpdateState.Create(base.World, 256);
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		m_UpdateState.ForceUpdate();
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float value = m_ResidentialLowDemand;
		writer.Write(value);
		float value2 = m_ResidentialMediumDemand;
		writer.Write(value2);
		float value3 = m_ResidentialHighDemand;
		writer.Write(value3);
		float value4 = m_CommercialDemand;
		writer.Write(value4);
		float value5 = m_IndustrialDemand;
		writer.Write(value5);
		float value6 = m_OfficeDemand;
		writer.Write(value6);
		uint value7 = m_LastFrameIndex;
		writer.Write(value7);
		int value8 = m_AvgHappiness;
		writer.Write(value8);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.residentialDemandSplitUI)
		{
			ref float value = ref m_ResidentialLowDemand;
			reader.Read(out value);
			ref float value2 = ref m_ResidentialMediumDemand;
			reader.Read(out value2);
			ref float value3 = ref m_ResidentialHighDemand;
			reader.Read(out value3);
		}
		else
		{
			reader.Read(out float value4);
			m_ResidentialLowDemand = value4 / 3f;
			m_ResidentialMediumDemand = value4 / 3f;
			m_ResidentialHighDemand = value4 / 3f;
		}
		ref float value5 = ref m_CommercialDemand;
		reader.Read(out value5);
		ref float value6 = ref m_IndustrialDemand;
		reader.Read(out value6);
		ref float value7 = ref m_OfficeDemand;
		reader.Read(out value7);
		ref uint value8 = ref m_LastFrameIndex;
		reader.Read(out value8);
		if (reader.context.version >= Version.populationComponent)
		{
			ref int value9 = ref m_AvgHappiness;
			reader.Read(out value9);
		}
	}

	public void SetDefaults(Context context)
	{
		m_ResidentialLowDemand = 0f;
		m_ResidentialMediumDemand = 0f;
		m_ResidentialHighDemand = 0f;
		m_CommercialDemand = 0f;
		m_IndustrialDemand = 0f;
		m_OfficeDemand = 0f;
		m_LastFrameIndex = 0u;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.OnUpdate();
		uint num = m_SimulationSystem.frameIndex - m_LastFrameIndex;
		if (num != 0)
		{
			m_LastFrameIndex = m_SimulationSystem.frameIndex;
			m_ResidentialLowDemand = AdvanceSmoothDemand(m_ResidentialLowDemand, m_ResidentialDemandSystem.buildingDemand.x, num);
			m_ResidentialMediumDemand = AdvanceSmoothDemand(m_ResidentialMediumDemand, m_ResidentialDemandSystem.buildingDemand.y, num);
			m_ResidentialHighDemand = AdvanceSmoothDemand(m_ResidentialHighDemand, m_ResidentialDemandSystem.buildingDemand.z, num);
			m_CommercialDemand = AdvanceSmoothDemand(m_CommercialDemand, m_CommercialDemandSystem.buildingDemand, num);
			int target = math.max(m_IndustrialDemandSystem.industrialBuildingDemand, m_IndustrialDemandSystem.storageBuildingDemand);
			m_IndustrialDemand = AdvanceSmoothDemand(m_IndustrialDemand, target, num);
			m_OfficeDemand = AdvanceSmoothDemand(m_OfficeDemand, m_IndustrialDemandSystem.officeBuildingDemand, num);
			if (base.EntityManager.HasComponent<Population>(m_CitySystem.City))
			{
				m_AvgHappiness = base.EntityManager.GetComponentData<Population>(m_CitySystem.City).m_AverageHappiness;
			}
			else
			{
				m_AvgHappiness = 50;
			}
		}
		if (m_UpdateState.Advance())
		{
			m_ResidentialLowFactors.Update();
			m_ResidentialMediumFactors.Update();
			m_ResidentialHighFactors.Update();
			m_CommercialFactors.Update();
			m_IndustrialFactors.Update();
			m_OfficeFactors.Update();
			m_HappinessFactors.Update();
		}
	}

	private static float AdvanceSmoothDemand(float current, int target, uint delta)
	{
		return math.clamp((float)target / 100f, current - 0.000625f * (float)delta, current + 0.000125f * (float)delta);
	}

	public void RequestUpdate()
	{
		m_UpdateState.ForceUpdate();
	}

	private void WriteResidentialLowFactors(IJsonWriter writer)
	{
		JobHandle deps;
		NativeArray<int> lowDensityDemandFactors = m_ResidentialDemandSystem.GetLowDensityDemandFactors(out deps);
		WriteDemandFactors(writer, lowDensityDemandFactors, deps);
	}

	private void WriteResidentialMediumFactors(IJsonWriter writer)
	{
		JobHandle deps;
		NativeArray<int> mediumDensityDemandFactors = m_ResidentialDemandSystem.GetMediumDensityDemandFactors(out deps);
		WriteDemandFactors(writer, mediumDensityDemandFactors, deps);
	}

	private void WriteResidentialHighFactors(IJsonWriter writer)
	{
		JobHandle deps;
		NativeArray<int> highDensityDemandFactors = m_ResidentialDemandSystem.GetHighDensityDemandFactors(out deps);
		WriteDemandFactors(writer, highDensityDemandFactors, deps);
	}

	private void WriteCommercialFactors(IJsonWriter writer)
	{
		JobHandle deps;
		NativeArray<int> demandFactors = m_CommercialDemandSystem.GetDemandFactors(out deps);
		WriteDemandFactors(writer, demandFactors, deps);
	}

	private void WriteIndustrialFactors(IJsonWriter writer)
	{
		JobHandle deps;
		NativeArray<int> industrialDemandFactors = m_IndustrialDemandSystem.GetIndustrialDemandFactors(out deps);
		WriteDemandFactors(writer, industrialDemandFactors, deps);
	}

	private void WriteOfficeFactors(IJsonWriter writer)
	{
		JobHandle deps;
		NativeArray<int> officeDemandFactors = m_IndustrialDemandSystem.GetOfficeDemandFactors(out deps);
		WriteDemandFactors(writer, officeDemandFactors, deps);
	}

	private void WriteDemandFactors(IJsonWriter writer, NativeArray<int> factors, JobHandle deps)
	{
		deps.Complete();
		NativeList<FactorInfo> list = FactorInfo.FromFactorArray(factors, Allocator.Temp);
		list.Sort();
		try
		{
			int num = math.min(5, list.Length);
			writer.ArrayBegin(num);
			for (int i = 0; i < num; i++)
			{
				list[i].WriteDemandFactor(writer);
			}
			writer.ArrayEnd();
		}
		finally
		{
			list.Dispose();
		}
	}

	private void WriteHappinessFactors(IJsonWriter writer)
	{
		NativeList<FactorInfo> list = new NativeList<FactorInfo>(26, Allocator.Temp);
		EntityQuery entityQuery = GetEntityQuery(ComponentType.ReadOnly<HappinessFactorParameterData>());
		if (!entityQuery.IsEmptyIgnoreFilter)
		{
			Entity singletonEntity = entityQuery.GetSingletonEntity();
			DynamicBuffer<HappinessFactorParameterData> buffer = base.EntityManager.GetBuffer<HappinessFactorParameterData>(singletonEntity, isReadOnly: true);
			ComponentLookup<Locked> locked = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_Locked_RO_ComponentLookup, ref base.CheckedStateRef);
			for (int i = 0; i < 26; i++)
			{
				int num = Mathf.RoundToInt(m_CitizenHappinessSystem.GetHappinessFactor((CitizenHappinessSystem.HappinessFactor)i, buffer, ref locked).x);
				if (num != 0)
				{
					list.Add(new FactorInfo(i, num));
				}
			}
		}
		list.Sort();
		try
		{
			int num2 = math.min(10, list.Length);
			writer.ArrayBegin(num2);
			for (int j = 0; j < num2; j++)
			{
				list[j].WriteHappinessFactor(writer);
			}
			writer.ArrayEnd();
		}
		finally
		{
			list.Dispose();
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
	public CityInfoUISystem()
	{
	}
}
