using System;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Economy;
using Game.Prefabs;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.UI.InGame;

public readonly struct UIResource : IJsonWritable, IComparable<UIResource>, IEquatable<UIResource>
{
	public enum ResourceStatus
	{
		None,
		Deficit,
		Normal,
		Surplus
	}

	public enum StorageType
	{
		None,
		Company,
		Warehouse,
		Cargo,
		Service
	}

	public Resource key { get; }

	public int amount { get; }

	public ResourceStatus status { get; }

	public bool isRawMaterial { get; }

	public UIResource(Resource resource, int amount, EntityManager entityManager, ResourcePrefabs prefabs)
	{
		key = resource;
		this.amount = amount;
		status = ResourceStatus.None;
		if (entityManager.TryGetComponent<ResourceData>(prefabs[resource], out var component))
		{
			isRawMaterial = component.m_IsMaterial;
		}
		else
		{
			isRawMaterial = false;
		}
	}

	public UIResource(Resources resource, EntityManager entityManager, ResourcePrefabs prefabs)
		: this(resource.m_Resource, resource.m_Amount, entityManager, prefabs)
	{
	}

	public UIResource(Resource resource, int amount, int perResourceLimit, StorageType storageType, EntityManager entityManager, ResourcePrefabs prefabs)
		: this(resource, amount, entityManager, prefabs)
	{
		switch (storageType)
		{
		case StorageType.Cargo:
			status = (((float)amount > math.min(math.ceil((float)perResourceLimit * 0.8f / 10000f) * 10000f, StorageCompanySystem.kStationExportStartAmount)) ? ResourceStatus.Surplus : ((!((float)amount >= math.min(math.ceil((float)perResourceLimit * 0.5f / 10000f) * 10000f, StorageCompanySystem.kStationLowStockAmount))) ? ResourceStatus.Deficit : ResourceStatus.Normal));
			break;
		case StorageType.Warehouse:
			status = (((float)amount > math.min(math.ceil((float)perResourceLimit * 0.8f / 10000f) * 10000f, StorageCompanySystem.kStorageExportStartAmount)) ? ResourceStatus.Surplus : ((!((float)amount >= math.min(math.ceil((float)perResourceLimit * 0.5f / 10000f) * 10000f, StorageCompanySystem.kStorageLowStockAmount))) ? ResourceStatus.Deficit : ResourceStatus.Normal));
			break;
		}
		if ((resource & (Resource)28672uL) != Resource.NoResource)
		{
			status = ResourceStatus.Normal;
		}
	}

	public UIResource(Resources resource, StorageType storageType, EntityManager entityManager, ResourcePrefabs prefabs)
		: this(resource.m_Resource, resource.m_Amount, 0, storageType, entityManager, prefabs)
	{
	}

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin(GetType().FullName);
		writer.PropertyName("key");
		writer.Write(Enum.GetName(typeof(Resource), key));
		writer.PropertyName("amount");
		writer.Write(amount);
		writer.PropertyName("status");
		writer.Write(Enum.GetName(typeof(ResourceStatus), status));
		writer.TypeEnd();
	}

	public int CompareTo(UIResource other)
	{
		if ((other.key & (Resource)28672uL) != Resource.NoResource && (key & (Resource)28672uL) == Resource.NoResource)
		{
			return -1;
		}
		if ((key & (Resource)28672uL) != Resource.NoResource && (other.key & (Resource)28672uL) == Resource.NoResource)
		{
			return 1;
		}
		int num = other.isRawMaterial.CompareTo(isRawMaterial);
		if (num != 0)
		{
			return num;
		}
		return other.amount.CompareTo(amount);
	}

	public bool Equals(UIResource other)
	{
		return key == other.key;
	}

	public override bool Equals(object obj)
	{
		if (obj is UIResource other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return key.GetHashCode();
	}

	public static void CategorizeResources(Resource resource, int amount, NativeList<UIResource> rawMaterials, NativeList<UIResource> processedGoods, NativeList<UIResource> mail, EntityManager entityManager, ResourcePrefabs resourcePrefabs, StorageType storageType = StorageType.None, int perResourceLimit = 0)
	{
		ResourceData component;
		if ((resource & (Resource)28672uL) != Resource.NoResource)
		{
			mail.Add(new UIResource(resource, amount, perResourceLimit, storageType, entityManager, resourcePrefabs));
		}
		else if (entityManager.TryGetComponent<ResourceData>(resourcePrefabs[resource], out component) && component.m_IsMaterial)
		{
			rawMaterials.Add(new UIResource(resource, amount, perResourceLimit, storageType, entityManager, resourcePrefabs));
		}
		else
		{
			processedGoods.Add(new UIResource(resource, amount, perResourceLimit, storageType, entityManager, resourcePrefabs));
		}
	}
}
