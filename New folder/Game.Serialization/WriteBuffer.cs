using System;
using Colossal.Serialization.Entities;
using Unity.Collections;
using Unity.Jobs;

namespace Game.Serialization;

public class WriteBuffer : IWriteBuffer, IDisposable
{
	private JobHandle m_WriteDependencies;

	private bool m_HasDependencies;

	private bool m_IsDone;

	public NativeList<byte> buffer { get; private set; }

	public bool isCompleted
	{
		get
		{
			if (m_IsDone)
			{
				if (m_HasDependencies)
				{
					return m_WriteDependencies.IsCompleted;
				}
				return true;
			}
			return false;
		}
	}

	public WriteBuffer()
	{
		buffer = new NativeList<byte>(Allocator.Persistent);
	}

	public void CompleteDependencies()
	{
		if (m_HasDependencies)
		{
			m_WriteDependencies.Complete();
			m_WriteDependencies = default(JobHandle);
			m_HasDependencies = false;
		}
	}

	private void DisposeBuffers()
	{
		CompleteDependencies();
		NativeList<byte> nativeList = buffer;
		if (nativeList.IsCreated)
		{
			nativeList.Dispose();
		}
		buffer = nativeList;
	}

	public void Dispose()
	{
		DisposeBuffers();
	}

	public void Done(JobHandle handle)
	{
		m_WriteDependencies = JobHandle.CombineDependencies(m_WriteDependencies, handle);
		m_HasDependencies = true;
		m_IsDone = true;
	}

	public void Done()
	{
		m_IsDone = true;
	}
}
