using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Triggers;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class ChirpLinkSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	public struct CachedChirpData : ISerializable
	{
		public CachedEntityName m_Sender;

		public CachedEntityName[] m_Links;

		public CachedChirpData(NameSystem nameSystem, Chirp chirpData)
		{
			m_Sender = new CachedEntityName(nameSystem, chirpData.m_Sender);
			m_Links = null;
		}

		public CachedChirpData(NameSystem nameSystem, Chirp chirpData, DynamicBuffer<ChirpEntity> chirpEntities)
		{
			m_Sender = new CachedEntityName(nameSystem, chirpData.m_Sender);
			m_Links = new CachedEntityName[chirpEntities.Length];
			for (int i = 0; i < chirpEntities.Length; i++)
			{
				m_Links[i] = new CachedEntityName(nameSystem, chirpEntities[i].m_Entity);
			}
		}

		public CachedChirpData Update(NameSystem nameSystem, Entity entity)
		{
			if (m_Sender.m_Entity == entity)
			{
				m_Sender = new CachedEntityName(nameSystem, entity);
			}
			if (m_Links != null)
			{
				for (int i = 0; i < m_Links.Length; i++)
				{
					if (m_Links[i].m_Entity == entity)
					{
						m_Links[i] = new CachedEntityName(nameSystem, entity);
					}
				}
			}
			return this;
		}

		public CachedChirpData Remove(Entity entity)
		{
			if (m_Sender.m_Entity == entity)
			{
				m_Sender.m_Entity = Entity.Null;
			}
			if (m_Links != null)
			{
				for (int i = 0; i < m_Links.Length; i++)
				{
					if (m_Links[i].m_Entity == entity)
					{
						m_Links[i] = new CachedEntityName
						{
							m_Entity = Entity.Null,
							m_Name = m_Links[i].m_Name
						};
					}
				}
			}
			return this;
		}

		public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
		{
			CachedEntityName value = m_Sender;
			writer.Write(value);
			int num = ((m_Links != null) ? m_Links.Length : 0);
			writer.Write(num);
			for (int i = 0; i < num; i++)
			{
				CachedEntityName value2 = m_Links[i];
				writer.Write(value2);
			}
		}

		public void Deserialize<TReader>(TReader reader) where TReader : IReader
		{
			ref CachedEntityName value = ref m_Sender;
			reader.Read(out value);
			reader.Read(out int value2);
			if (value2 > 0)
			{
				m_Links = new CachedEntityName[value2];
				for (int i = 0; i < value2; i++)
				{
					ref CachedEntityName value3 = ref m_Links[i];
					reader.Read(out value3);
				}
			}
			else
			{
				m_Links = null;
			}
		}
	}

	public struct CachedEntityName : ISerializable
	{
		public Entity m_Entity;

		public NameSystem.Name m_Name;

		public CachedEntityName(NameSystem nameSystem, Entity entity)
		{
			m_Entity = entity;
			m_Name = nameSystem.GetName(entity);
		}

		public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
		{
			Entity value = m_Entity;
			writer.Write(value);
			NameSystem.Name value2 = m_Name;
			writer.Write(value2);
		}

		public void Deserialize<TReader>(TReader reader) where TReader : IReader
		{
			ref Entity value = ref m_Entity;
			reader.Read(out value);
			ref NameSystem.Name value2 = ref m_Name;
			reader.Read(out value2);
		}
	}

	private NameSystem m_NameSystem;

	private EntityQuery m_CreatedChirpQuery;

	private EntityQuery m_AllChirpsQuery;

	private EntityQuery m_DeletedChirpQuery;

	private EntityQuery m_UpdatedLinkEntityQuery;

	private EntityQuery m_DeletedLinkEntityQuery;

	private Dictionary<Entity, CachedChirpData> m_CachedChirpData;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_NameSystem = base.World.GetOrCreateSystemManaged<NameSystem>();
		m_CreatedChirpQuery = GetEntityQuery(ComponentType.ReadOnly<Chirp>(), ComponentType.ReadOnly<Created>(), ComponentType.Exclude<Deleted>());
		m_AllChirpsQuery = GetEntityQuery(ComponentType.ReadOnly<Chirp>(), ComponentType.Exclude<Deleted>());
		m_DeletedChirpQuery = GetEntityQuery(ComponentType.ReadOnly<Chirp>(), ComponentType.ReadOnly<Deleted>());
		m_UpdatedLinkEntityQuery = GetEntityQuery(ComponentType.ReadOnly<ChirpLink>(), ComponentType.ReadOnly<Updated>(), ComponentType.Exclude<Deleted>());
		m_DeletedLinkEntityQuery = GetEntityQuery(ComponentType.ReadOnly<ChirpLink>(), ComponentType.ReadOnly<Deleted>());
		m_CachedChirpData = new Dictionary<Entity, CachedChirpData>();
		RequireAnyForUpdate(m_CreatedChirpQuery, m_UpdatedLinkEntityQuery, m_DeletedLinkEntityQuery, m_DeletedChirpQuery);
	}

	public bool TryGetData(Entity chirp, out CachedChirpData data)
	{
		if (m_CachedChirpData.TryGetValue(chirp, out data))
		{
			return true;
		}
		return false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_CreatedChirpQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray = m_CreatedChirpQuery.ToEntityArray(Allocator.TempJob);
			NativeArray<Chirp> nativeArray2 = m_CreatedChirpQuery.ToComponentDataArray<Chirp>(Allocator.TempJob);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				if (base.EntityManager.TryGetBuffer(nativeArray[i], isReadOnly: true, out DynamicBuffer<ChirpEntity> buffer))
				{
					m_CachedChirpData[nativeArray[i]] = new CachedChirpData(m_NameSystem, nativeArray2[i], buffer);
					NativeArray<ChirpEntity> nativeArray3 = new NativeArray<ChirpEntity>(buffer.AsNativeArray(), Allocator.Temp);
					for (int j = 0; j < nativeArray3.Length; j++)
					{
						if (base.EntityManager.Exists(nativeArray3[j].m_Entity))
						{
							RegisterLink(nativeArray3[j].m_Entity, nativeArray[i]);
						}
					}
					nativeArray3.Dispose();
				}
				else
				{
					m_CachedChirpData[nativeArray[i]] = new CachedChirpData(m_NameSystem, nativeArray2[i]);
				}
				if (base.EntityManager.Exists(nativeArray2[i].m_Sender))
				{
					RegisterLink(nativeArray2[i].m_Sender, nativeArray[i]);
				}
			}
			nativeArray.Dispose();
			nativeArray2.Dispose();
		}
		if (!m_UpdatedLinkEntityQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray4 = m_UpdatedLinkEntityQuery.ToEntityArray(Allocator.TempJob);
			for (int k = 0; k < nativeArray4.Length; k++)
			{
				DynamicBuffer<ChirpLink> buffer2 = base.EntityManager.GetBuffer<ChirpLink>(nativeArray4[k], isReadOnly: true);
				for (int l = 0; l < buffer2.Length; l++)
				{
					if (m_CachedChirpData.TryGetValue(buffer2[l].m_Chirp, out var value))
					{
						m_CachedChirpData[buffer2[l].m_Chirp] = value.Update(m_NameSystem, nativeArray4[k]);
					}
				}
			}
			nativeArray4.Dispose();
		}
		if (!m_DeletedLinkEntityQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray5 = m_DeletedLinkEntityQuery.ToEntityArray(Allocator.TempJob);
			for (int m = 0; m < nativeArray5.Length; m++)
			{
				DynamicBuffer<ChirpLink> buffer3 = base.EntityManager.GetBuffer<ChirpLink>(nativeArray5[m], isReadOnly: true);
				for (int n = 0; n < buffer3.Length; n++)
				{
					if (m_CachedChirpData.TryGetValue(buffer3[n].m_Chirp, out var value2))
					{
						m_CachedChirpData[buffer3[n].m_Chirp] = value2.Remove(nativeArray5[m]);
					}
				}
			}
			nativeArray5.Dispose();
		}
		if (m_DeletedChirpQuery.IsEmptyIgnoreFilter)
		{
			return;
		}
		NativeArray<Entity> nativeArray6 = m_DeletedChirpQuery.ToEntityArray(Allocator.TempJob);
		NativeArray<Chirp> nativeArray7 = m_DeletedChirpQuery.ToComponentDataArray<Chirp>(Allocator.TempJob);
		for (int num = 0; num < nativeArray6.Length; num++)
		{
			if (base.EntityManager.TryGetBuffer(nativeArray6[num], isReadOnly: true, out DynamicBuffer<ChirpEntity> buffer4))
			{
				NativeArray<ChirpEntity> nativeArray8 = new NativeArray<ChirpEntity>(buffer4.AsNativeArray(), Allocator.Temp);
				for (int num2 = 0; num2 < nativeArray8.Length; num2++)
				{
					UnregisterLink(nativeArray8[num2].m_Entity, nativeArray6[num]);
				}
				nativeArray8.Dispose();
			}
			UnregisterLink(nativeArray7[num].m_Sender, nativeArray6[num]);
			m_CachedChirpData.Remove(nativeArray6[num]);
		}
		nativeArray7.Dispose();
		nativeArray6.Dispose();
	}

	private void RegisterLink(Entity linkEntity, Entity chirpEntity)
	{
		if (!base.EntityManager.TryGetBuffer(linkEntity, isReadOnly: false, out DynamicBuffer<ChirpLink> buffer))
		{
			buffer = base.EntityManager.AddBuffer<ChirpLink>(linkEntity);
		}
		if (!LinkExists(buffer, chirpEntity))
		{
			buffer.Add(new ChirpLink
			{
				m_Chirp = chirpEntity
			});
		}
	}

	private void UnregisterLink(Entity linkEntity, Entity chirpEntity)
	{
		if (!base.EntityManager.TryGetBuffer(linkEntity, isReadOnly: false, out DynamicBuffer<ChirpLink> buffer))
		{
			return;
		}
		for (int i = 0; i < buffer.Length; i++)
		{
			if (buffer[i].m_Chirp == chirpEntity)
			{
				buffer.RemoveAt(i);
				break;
			}
		}
		if (buffer.Length == 0)
		{
			base.EntityManager.RemoveComponent<ChirpLink>(linkEntity);
		}
	}

	private bool LinkExists(DynamicBuffer<ChirpLink> links, Entity link)
	{
		for (int i = 0; i < links.Length; i++)
		{
			if (links[i].m_Chirp == link)
			{
				return true;
			}
		}
		return false;
	}

	private void Initialize()
	{
		m_CachedChirpData.Clear();
		NativeArray<Chirp> nativeArray = m_AllChirpsQuery.ToComponentDataArray<Chirp>(Allocator.TempJob);
		NativeArray<Entity> nativeArray2 = m_AllChirpsQuery.ToEntityArray(Allocator.TempJob);
		for (int i = 0; i < nativeArray2.Length; i++)
		{
			if (base.EntityManager.TryGetBuffer(nativeArray2[i], isReadOnly: true, out DynamicBuffer<ChirpEntity> buffer))
			{
				m_CachedChirpData[nativeArray2[i]] = new CachedChirpData(m_NameSystem, nativeArray[i], buffer);
				NativeArray<ChirpEntity> nativeArray3 = new NativeArray<ChirpEntity>(buffer.AsNativeArray(), Allocator.Temp);
				for (int j = 0; j < nativeArray3.Length; j++)
				{
					if (base.EntityManager.Exists(nativeArray3[j].m_Entity))
					{
						RegisterLink(nativeArray3[j].m_Entity, nativeArray2[i]);
					}
				}
				nativeArray3.Dispose();
			}
			else
			{
				m_CachedChirpData[nativeArray2[i]] = new CachedChirpData(m_NameSystem, nativeArray[i]);
			}
			if (base.EntityManager.Exists(nativeArray[i].m_Sender))
			{
				RegisterLink(nativeArray[i].m_Sender, nativeArray2[i]);
			}
		}
		nativeArray2.Dispose();
		nativeArray.Dispose();
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int count = m_CachedChirpData.Count;
		writer.Write(count);
		foreach (KeyValuePair<Entity, CachedChirpData> item in m_CachedChirpData)
		{
			Entity key = item.Key;
			writer.Write(key);
			CachedChirpData value = item.Value;
			writer.Write(value);
		}
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		m_CachedChirpData.Clear();
		reader.Read(out int value);
		for (int i = 0; i < value; i++)
		{
			reader.Read(out Entity value2);
			reader.Read(out CachedChirpData value3);
			m_CachedChirpData[value2] = value3;
		}
	}

	public void SetDefaults(Context context)
	{
		m_CachedChirpData.Clear();
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		base.OnGameLoaded(serializationContext);
		if (m_CachedChirpData.Count == 0)
		{
			Initialize();
		}
	}

	[Preserve]
	public ChirpLinkSystem()
	{
	}
}
