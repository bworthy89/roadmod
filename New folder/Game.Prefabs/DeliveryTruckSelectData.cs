using Game.Common;
using Game.Economy;
using Game.Objects;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct DeliveryTruckSelectData
{
	private NativeArray<DeliveryTruckSelectItem> m_Items;

	public DeliveryTruckSelectData(NativeArray<DeliveryTruckSelectItem> items)
	{
		m_Items = items;
	}

	public void GetCapacityRange(Resource resources, out int min, out int max)
	{
		min = 0;
		max = 0;
		for (int i = 0; i < m_Items.Length; i++)
		{
			DeliveryTruckSelectItem deliveryTruckSelectItem = m_Items[i];
			if ((deliveryTruckSelectItem.m_Resources & resources) == resources)
			{
				min = deliveryTruckSelectItem.m_Capacity;
				break;
			}
		}
		for (int num = m_Items.Length - 1; num >= 0; num--)
		{
			DeliveryTruckSelectItem deliveryTruckSelectItem2 = m_Items[num];
			if ((deliveryTruckSelectItem2.m_Resources & resources) == resources)
			{
				max = deliveryTruckSelectItem2.m_Capacity;
				break;
			}
		}
	}

	public bool TrySelectItem(ref Random random, Resource resources, int capacity, out DeliveryTruckSelectItem item)
	{
		int2 x = new int2(0, m_Items.Length);
		while (x.y > x.x)
		{
			int num = math.csum(x) >> 1;
			DeliveryTruckSelectItem deliveryTruckSelectItem = m_Items[num];
			if (deliveryTruckSelectItem.m_Capacity == capacity)
			{
				x = num;
				break;
			}
			x = math.select(new int2(num + 1, x.y), new int2(x.x, num), deliveryTruckSelectItem.m_Capacity > capacity);
		}
		item = default(DeliveryTruckSelectItem);
		int num2 = 0;
		while (x.y < m_Items.Length)
		{
			DeliveryTruckSelectItem deliveryTruckSelectItem2 = m_Items[x.y++];
			int2 @int = new int2(deliveryTruckSelectItem2.m_Cost, item.m_Cost) * math.min(capacity, new int2(item.m_Capacity, deliveryTruckSelectItem2.m_Capacity));
			if (@int.x > @int.y)
			{
				break;
			}
			bool flag = (deliveryTruckSelectItem2.m_Resources & resources) == resources;
			int num3 = math.select(0, 100, flag);
			num2 = num3 + math.select(num2, 0, flag & (@int.x < @int.y));
			if (random.NextInt(num2) < num3)
			{
				item = deliveryTruckSelectItem2;
			}
		}
		while (x.x > 0)
		{
			DeliveryTruckSelectItem deliveryTruckSelectItem3 = m_Items[--x.x];
			int2 int2 = new int2(deliveryTruckSelectItem3.m_Cost, item.m_Cost) * math.min(capacity, new int2(item.m_Capacity, deliveryTruckSelectItem3.m_Capacity));
			if (int2.x > int2.y)
			{
				break;
			}
			bool flag2 = (deliveryTruckSelectItem3.m_Resources & resources) == resources;
			int num4 = math.select(0, 100, flag2);
			num2 = num4 + math.select(num2, 0, flag2 & (int2.x < int2.y));
			if (random.NextInt(num2) < num4)
			{
				item = deliveryTruckSelectItem3;
			}
		}
		return item.m_Prefab1 != Entity.Null;
	}

	public Entity CreateVehicle(EntityCommandBuffer.ParallelWriter commandBuffer, int jobIndex, ref Random random, ref ComponentLookup<DeliveryTruckData> deliveryTruckDatas, ref ComponentLookup<ObjectData> objectDatas, Resource resource, Resource returnResource, ref int amount, ref int returnAmount, Transform transform, Entity source, DeliveryTruckFlags state, uint delay = 0u)
	{
		Resource resources = resource | returnResource;
		int capacity = math.max(amount, returnAmount);
		if (TrySelectItem(ref random, resources, capacity, out var item))
		{
			return CreateVehicle(commandBuffer, jobIndex, ref random, ref deliveryTruckDatas, ref objectDatas, item, resource, returnResource, ref amount, ref returnAmount, transform, source, state, delay);
		}
		return Entity.Null;
	}

	public Entity CreateVehicle(EntityCommandBuffer.ParallelWriter commandBuffer, int jobIndex, ref Random random, ref ComponentLookup<DeliveryTruckData> deliveryTruckDatas, ref ComponentLookup<ObjectData> objectDatas, DeliveryTruckSelectItem selectItem, Resource resource, Resource returnResource, ref int amount, ref int returnAmount, Transform transform, Entity source, DeliveryTruckFlags state, uint delay = 0u)
	{
		int amount2 = amount;
		int returnAmount2 = returnAmount;
		Entity entity = CreateVehicle(commandBuffer, jobIndex, ref random, ref deliveryTruckDatas, ref objectDatas, selectItem.m_Prefab1, resource, returnResource, ref amount2, ref returnAmount2, transform, source, state, delay);
		if (selectItem.m_Prefab2 != Entity.Null)
		{
			DynamicBuffer<LayoutElement> dynamicBuffer = commandBuffer.AddBuffer<LayoutElement>(jobIndex, entity);
			dynamicBuffer.Add(new LayoutElement(entity));
			Entity entity2 = CreateVehicle(commandBuffer, jobIndex, ref random, ref deliveryTruckDatas, ref objectDatas, selectItem.m_Prefab2, resource, returnResource, ref amount2, ref returnAmount2, transform, source, state & DeliveryTruckFlags.Loaded, delay);
			commandBuffer.SetComponent(jobIndex, entity2, new Controller(entity));
			dynamicBuffer.Add(new LayoutElement(entity2));
			if (selectItem.m_Prefab3 != Entity.Null)
			{
				entity2 = CreateVehicle(commandBuffer, jobIndex, ref random, ref deliveryTruckDatas, ref objectDatas, selectItem.m_Prefab3, resource, returnResource, ref amount2, ref returnAmount2, transform, source, state & DeliveryTruckFlags.Loaded, delay);
				commandBuffer.SetComponent(jobIndex, entity2, new Controller(entity));
				dynamicBuffer.Add(new LayoutElement(entity2));
			}
			if (selectItem.m_Prefab4 != Entity.Null)
			{
				entity2 = CreateVehicle(commandBuffer, jobIndex, ref random, ref deliveryTruckDatas, ref objectDatas, selectItem.m_Prefab4, resource, returnResource, ref amount2, ref returnAmount2, transform, source, state & DeliveryTruckFlags.Loaded, delay);
				commandBuffer.SetComponent(jobIndex, entity2, new Controller(entity));
				dynamicBuffer.Add(new LayoutElement(entity2));
			}
		}
		amount -= amount2;
		returnAmount -= returnAmount2;
		return entity;
	}

	private Entity CreateVehicle(EntityCommandBuffer.ParallelWriter commandBuffer, int jobIndex, ref Random random, ref ComponentLookup<DeliveryTruckData> deliveryTruckDatas, ref ComponentLookup<ObjectData> objectDatas, Entity prefab, Resource resource, Resource returnResource, ref int amount, ref int returnAmount, Transform transform, Entity source, DeliveryTruckFlags state, uint delay)
	{
		DeliveryTruckData deliveryTruckData = deliveryTruckDatas[prefab];
		ObjectData objectData = objectDatas[prefab];
		Game.Vehicles.DeliveryTruck component = new Game.Vehicles.DeliveryTruck
		{
			m_State = state
		};
		if ((resource & deliveryTruckData.m_TransportedResources) != Resource.NoResource && amount > 0)
		{
			component.m_Amount = math.min(amount, deliveryTruckData.m_CargoCapacity);
			if (component.m_Amount > 0)
			{
				component.m_Resource = resource;
				amount -= component.m_Amount;
			}
		}
		Entity entity = commandBuffer.CreateEntity(jobIndex, objectData.m_Archetype);
		commandBuffer.SetComponent(jobIndex, entity, transform);
		commandBuffer.SetComponent(jobIndex, entity, component);
		commandBuffer.SetComponent(jobIndex, entity, new PrefabRef(prefab));
		commandBuffer.SetComponent(jobIndex, entity, new PseudoRandomSeed(ref random));
		if (source != Entity.Null)
		{
			commandBuffer.AddComponent(jobIndex, entity, new TripSource(source, delay));
			commandBuffer.AddComponent(jobIndex, entity, default(Unspawned));
		}
		if ((returnResource & deliveryTruckData.m_TransportedResources) != Resource.NoResource)
		{
			ReturnLoad component2 = new ReturnLoad
			{
				m_Amount = math.min(returnAmount, deliveryTruckData.m_CargoCapacity)
			};
			if (component2.m_Amount > 0)
			{
				component2.m_Resource = returnResource;
				returnAmount -= component2.m_Amount;
				commandBuffer.AddComponent(jobIndex, entity, component2);
			}
		}
		return entity;
	}
}
