using System;
using System.Collections.Generic;
using Colossal.UI.Binding;
using Game.Economy;
using Game.Prefabs;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

public class OutsideConnectionsInfoviewUISystem : InfoviewUISystemBase
{
	public struct TopResource : IComparable<TopResource>
	{
		public string id;

		public int amount;

		public Color color;

		public TopResource(string id, int amount, Color color)
		{
			this.id = id;
			this.amount = amount;
			this.color = color;
		}

		public int CompareTo(TopResource other)
		{
			int num = other.amount - amount;
			if (num == 0)
			{
				return string.Compare(id, other.id, StringComparison.Ordinal);
			}
			return num;
		}
	}

	private const string kGroup = "outsideInfo";

	private CommercialDemandSystem m_CommercialDemandSystem;

	private IndustrialDemandSystem m_IndustrialDemandSystem;

	private CountCompanyDataSystem m_CountCompanyDataSystem;

	private PrefabSystem m_PrefabSystem;

	private EntityQuery m_ResourceQuery;

	private RawValueBinding m_TopImportNames;

	private RawValueBinding m_TopExportNames;

	private RawValueBinding m_TopImportColors;

	private RawValueBinding m_TopExportColors;

	private RawValueBinding m_TopImportData;

	private RawValueBinding m_TopExportData;

	private List<TopResource> m_TopImports;

	private List<TopResource> m_TopExports;

	protected override bool Active
	{
		get
		{
			if (!base.Active && !m_TopImportNames.active && !m_TopImportColors.active && !m_TopImportData.active && !m_TopExportNames.active && !m_TopExportColors.active)
			{
				return m_TopExportData.active;
			}
			return true;
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CommercialDemandSystem = base.World.GetOrCreateSystemManaged<CommercialDemandSystem>();
		m_IndustrialDemandSystem = base.World.GetOrCreateSystemManaged<IndustrialDemandSystem>();
		m_CountCompanyDataSystem = base.World.GetOrCreateSystemManaged<CountCompanyDataSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_ResourceQuery = GetEntityQuery(ComponentType.ReadOnly<ResourceData>(), ComponentType.ReadOnly<TaxableResourceData>(), ComponentType.ReadOnly<PrefabData>());
		m_TopImports = new List<TopResource>(42);
		m_TopExports = new List<TopResource>(42);
		UpdateCache();
		AddBinding(m_TopImportNames = new RawValueBinding("outsideInfo", "topImportNames", UpdateImportNames));
		AddBinding(m_TopImportColors = new RawValueBinding("outsideInfo", "topImportColors", UpdateImportColors));
		AddBinding(m_TopImportData = new RawValueBinding("outsideInfo", "topImportData", UpdateImportData));
		AddBinding(m_TopExportNames = new RawValueBinding("outsideInfo", "topExportNames", UpdateExportNames));
		AddBinding(m_TopExportColors = new RawValueBinding("outsideInfo", "topExportColors", UpdateExportColors));
		AddBinding(m_TopExportData = new RawValueBinding("outsideInfo", "topExportData", UpdateExportData));
	}

	protected override void PerformUpdate()
	{
		UpdateCache();
		m_TopImportNames.Update();
		m_TopImportColors.Update();
		m_TopImportData.Update();
		m_TopExportNames.Update();
		m_TopExportColors.Update();
		m_TopExportData.Update();
	}

	private void UpdateCache()
	{
		NativeArray<Entity> nativeArray = m_ResourceQuery.ToEntityArray(Allocator.TempJob);
		NativeArray<PrefabData> nativeArray2 = m_ResourceQuery.ToComponentDataArray<PrefabData>(Allocator.TempJob);
		JobHandle deps;
		NativeArray<int> production = m_CountCompanyDataSystem.GetProduction(out deps);
		JobHandle deps2;
		NativeArray<int> consumption = m_IndustrialDemandSystem.GetConsumption(out deps2);
		JobHandle deps3;
		NativeArray<int> consumption2 = m_CommercialDemandSystem.GetConsumption(out deps3);
		JobHandle.CompleteAll(ref deps, ref deps2, ref deps3);
		m_TopImports.Clear();
		m_TopExports.Clear();
		try
		{
			for (int i = 0; i < nativeArray.Length; i++)
			{
				ResourcePrefab prefab = m_PrefabSystem.GetPrefab<ResourcePrefab>(nativeArray2[i]);
				int resourceIndex = EconomyUtils.GetResourceIndex(EconomyUtils.GetResource(prefab.m_Resource));
				int num = production[resourceIndex];
				int num2 = consumption[resourceIndex];
				int num3 = consumption2[resourceIndex];
				int num4 = math.min(num2 + num3, num);
				int num5 = math.min(num2, num4);
				int num6 = num2 - num5;
				int num7 = math.min(num3, num4 - num5);
				int amount = num3 - num7 + num6;
				int amount2 = num - num4;
				m_TopImports.Add(new TopResource(prefab.name, amount, prefab.m_Color));
				m_TopExports.Add(new TopResource(prefab.name, amount2, prefab.m_Color));
			}
			m_TopImports.Sort();
			m_TopExports.Sort();
		}
		finally
		{
			nativeArray.Dispose();
			nativeArray2.Dispose();
		}
	}

	private void UpdateImportNames(IJsonWriter binder)
	{
		int num = 10;
		if (m_TopImports.Count < num)
		{
			num = m_TopImports.Count;
		}
		binder.ArrayBegin(num);
		for (int i = 0; i < num; i++)
		{
			binder.Write(m_TopImports[i].id);
		}
		binder.ArrayEnd();
	}

	private void UpdateImportColors(IJsonWriter binder)
	{
		int num = 10;
		if (m_TopImports.Count < num)
		{
			num = m_TopImports.Count;
		}
		binder.ArrayBegin(num);
		for (int i = 0; i < num; i++)
		{
			binder.Write(m_TopImports[i].color.ToHexCode());
		}
		binder.ArrayEnd();
	}

	private void UpdateImportData(IJsonWriter binder)
	{
		int num = 0;
		int num2 = 10;
		if (m_TopImports.Count < num2)
		{
			num2 = m_TopImports.Count;
		}
		binder.TypeBegin("infoviews.ChartData");
		binder.PropertyName("values");
		binder.ArrayBegin(num2);
		for (int i = 0; i < num2; i++)
		{
			binder.Write(m_TopImports[i].amount);
			num += m_TopImports[i].amount;
		}
		binder.ArrayEnd();
		binder.PropertyName("total");
		binder.Write(num);
		binder.TypeEnd();
	}

	private void UpdateExportNames(IJsonWriter binder)
	{
		int num = 10;
		if (m_TopExports.Count < num)
		{
			num = m_TopExports.Count;
		}
		binder.ArrayBegin(num);
		for (int i = 0; i < num; i++)
		{
			binder.Write(m_TopExports[i].id);
		}
		binder.ArrayEnd();
	}

	private void UpdateExportColors(IJsonWriter binder)
	{
		int num = 10;
		if (m_TopExports.Count < num)
		{
			num = m_TopExports.Count;
		}
		binder.ArrayBegin(num);
		for (int i = 0; i < num; i++)
		{
			binder.Write(m_TopExports[i].color.ToHexCode());
		}
		binder.ArrayEnd();
	}

	private void UpdateExportData(IJsonWriter binder)
	{
		int num = 0;
		int num2 = 10;
		if (m_TopExports.Count < num2)
		{
			num2 = m_TopExports.Count;
		}
		binder.TypeBegin("infoviews.ChartData");
		binder.PropertyName("values");
		binder.ArrayBegin(num2);
		for (int i = 0; i < num2; i++)
		{
			binder.Write(m_TopExports[i].amount);
			num += m_TopExports[i].amount;
		}
		binder.ArrayEnd();
		binder.PropertyName("total");
		binder.Write(num);
		binder.TypeEnd();
	}

	[Preserve]
	public OutsideConnectionsInfoviewUISystem()
	{
	}
}
