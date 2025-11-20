using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class AttractivenessSection : InfoSectionBase
{
	private readonly struct AttractivenessFactor : IJsonWritable, IComparable<AttractivenessFactor>
	{
		private int localeKey { get; }

		private float delta { get; }

		public AttractivenessFactor(int factor, float delta)
		{
			localeKey = factor;
			this.delta = delta;
		}

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin(typeof(AttractionSystem.AttractivenessFactor).FullName);
			writer.PropertyName("localeKey");
			writer.Write(Enum.GetName(typeof(AttractionSystem.AttractivenessFactor), (AttractionSystem.AttractivenessFactor)localeKey));
			writer.PropertyName("delta");
			writer.Write(delta);
			writer.TypeEnd();
		}

		public int CompareTo(AttractivenessFactor other)
		{
			return delta.CompareTo(other.delta);
		}
	}

	private TerrainAttractivenessSystem m_TerrainAttractivenessSystem;

	private TerrainSystem m_TerrainSystem;

	private EntityQuery m_SettingsQuery;

	protected override string group => "AttractivenessSection";

	private float baseAttractiveness { get; set; }

	private float attractiveness { get; set; }

	private List<AttractivenessFactor> factors { get; set; }

	protected override void Reset()
	{
		baseAttractiveness = 0f;
		factors.Clear();
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		factors = new List<AttractivenessFactor>(5);
		m_TerrainAttractivenessSystem = base.World.GetOrCreateSystemManaged<TerrainAttractivenessSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_SettingsQuery = GetEntityQuery(ComponentType.ReadOnly<AttractivenessParameterData>());
	}

	private bool Visible()
	{
		if (!base.EntityManager.HasComponent<AttractionData>(selectedPrefab))
		{
			return base.EntityManager.HasComponent<AttractivenessProvider>(selectedEntity);
		}
		return true;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = Visible();
	}

	protected override void OnProcess()
	{
		NativeArray<int> nativeArray = new NativeArray<int>(5, Allocator.TempJob);
		if (TryGetComponentWithUpgrades<AttractionData>(selectedEntity, selectedPrefab, out var data))
		{
			baseAttractiveness = data.m_Attractiveness;
		}
		attractiveness = baseAttractiveness;
		if (!base.EntityManager.HasComponent<Signature>(selectedEntity) && base.EntityManager.TryGetBuffer(selectedEntity, isReadOnly: true, out DynamicBuffer<Efficiency> buffer))
		{
			float efficiency = BuildingUtils.GetEfficiency(buffer);
			attractiveness *= efficiency;
			AttractionSystem.SetFactor(nativeArray, AttractionSystem.AttractivenessFactor.Efficiency, (efficiency - 1f) * 100f);
		}
		if (base.EntityManager.TryGetComponent<Game.Buildings.Park>(selectedEntity, out var component) && TryGetComponentWithUpgrades<ParkData>(selectedEntity, selectedPrefab, out var data2))
		{
			float num = ((data2.m_MaintenancePool > 0) ? ((float)component.m_Maintenance / (float)data2.m_MaintenancePool) : 0f);
			float num2 = Mathf.Min(1f, 0.25f + 0.25f * (float)Mathf.FloorToInt(num / 0.3f));
			attractiveness *= num2;
			AttractionSystem.SetFactor(nativeArray, AttractionSystem.AttractivenessFactor.Maintenance, (num2 - 1f) * 100f);
		}
		if (base.EntityManager.TryGetComponent<Game.Objects.Transform>(selectedEntity, out var component2))
		{
			JobHandle dependencies;
			CellMapData<TerrainAttractiveness> data3 = m_TerrainAttractivenessSystem.GetData(readOnly: true, out dependencies);
			base.Dependency = JobHandle.CombineDependencies(base.Dependency, dependencies);
			base.Dependency.Complete();
			TerrainHeightData heightData = m_TerrainSystem.GetHeightData();
			AttractivenessParameterData singleton = m_SettingsQuery.GetSingleton<AttractivenessParameterData>();
			attractiveness *= 1f + 0.01f * TerrainAttractivenessSystem.EvaluateAttractiveness(component2.m_Position, data3, heightData, singleton, nativeArray);
		}
		for (int i = 0; i < nativeArray.Length; i++)
		{
			if (nativeArray[i] != 0)
			{
				factors.Add(new AttractivenessFactor(i, nativeArray[i]));
			}
		}
		nativeArray.Dispose();
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("attractiveness");
		writer.Write(attractiveness);
		writer.PropertyName("baseAttractiveness");
		writer.Write(baseAttractiveness);
		writer.PropertyName("factors");
		writer.ArrayBegin(factors.Count);
		for (int i = 0; i < factors.Count; i++)
		{
			writer.Write(factors[i]);
		}
		writer.ArrayEnd();
	}

	[Preserve]
	public AttractivenessSection()
	{
	}
}
