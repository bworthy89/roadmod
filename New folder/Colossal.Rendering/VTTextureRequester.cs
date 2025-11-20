using System;
using Colossal.IO.AssetDatabase.VirtualTexturing;
using Colossal.Mathematics;
using Unity.Collections;
using UnityEngine;

namespace Colossal.Rendering;

public class VTTextureRequester : IDisposable
{
	private NativeList<int>[] m_TexturesIndices;

	private NativeList<int>[] m_StackGlobalIndices;

	private NativeList<Bounds2>[] m_TextureBounds;

	private NativeList<float>[] m_TexturesMaxPixels;

	private TextureStreamingSystem m_TextureStreamingSystem;

	private int m_RequestedThisFrame;

	public int stacksCount => m_TexturesIndices.Length;

	public int registeredCount
	{
		get
		{
			int num = 0;
			NativeList<int>[] texturesIndices = m_TexturesIndices;
			foreach (NativeList<int> nativeList in texturesIndices)
			{
				num += nativeList.Length;
			}
			return num;
		}
	}

	public int requestCount => m_RequestedThisFrame;

	public NativeList<float>[] TexturesMaxPixels => m_TexturesMaxPixels;

	public VTTextureRequester(TextureStreamingSystem textureStreamingSystem)
	{
		m_TextureStreamingSystem = textureStreamingSystem;
		m_TexturesIndices = new NativeList<int>[2];
		m_TexturesIndices[0] = new NativeList<int>(100, Allocator.Persistent);
		m_TexturesIndices[1] = new NativeList<int>(100, Allocator.Persistent);
		m_StackGlobalIndices = new NativeList<int>[2];
		m_StackGlobalIndices[0] = new NativeList<int>(100, Allocator.Persistent);
		m_StackGlobalIndices[1] = new NativeList<int>(100, Allocator.Persistent);
		m_TexturesMaxPixels = new NativeList<float>[2];
		m_TexturesMaxPixels[0] = new NativeList<float>(100, Allocator.Persistent);
		m_TexturesMaxPixels[1] = new NativeList<float>(100, Allocator.Persistent);
		m_TextureBounds = new NativeList<Bounds2>[2];
		m_TextureBounds[0] = new NativeList<Bounds2>(100, Allocator.Persistent);
		m_TextureBounds[1] = new NativeList<Bounds2>(100, Allocator.Persistent);
	}

	public void Dispose()
	{
		for (int i = 0; i < m_TexturesIndices.Length; i++)
		{
			m_TexturesIndices[i].Dispose();
		}
		for (int j = 0; j < m_StackGlobalIndices.Length; j++)
		{
			m_StackGlobalIndices[j].Dispose();
		}
		for (int k = 0; k < m_TexturesMaxPixels.Length; k++)
		{
			m_TexturesMaxPixels[k].Dispose();
		}
		for (int l = 0; l < m_TextureBounds.Length; l++)
		{
			m_TextureBounds[l].Dispose();
		}
	}

	public void Clear()
	{
		for (int i = 0; i < m_TexturesIndices.Length; i++)
		{
			m_TexturesIndices[i].Clear();
		}
		for (int j = 0; j < m_StackGlobalIndices.Length; j++)
		{
			m_StackGlobalIndices[j].Clear();
		}
		for (int k = 0; k < m_TexturesMaxPixels.Length; k++)
		{
			m_TexturesMaxPixels[k].Clear();
		}
		for (int l = 0; l < m_TextureBounds.Length; l++)
		{
			m_TextureBounds[l].Clear();
		}
	}

	public void UpdateTexturesVTRequests()
	{
		m_RequestedThisFrame = 0;
		float num = 1f;
		if (m_TextureStreamingSystem.workingSetLodBias > 1f)
		{
			num = 1f / m_TextureStreamingSystem.workingSetLodBias;
		}
		for (int i = 0; i < 2; i++)
		{
			NativeList<int> nativeList = m_TexturesIndices[i];
			NativeList<int> nativeList2 = m_StackGlobalIndices[i];
			NativeList<float> nativeList3 = m_TexturesMaxPixels[i];
			for (int j = 0; j < nativeList.Length; j++)
			{
				float num2 = nativeList3[j] * num;
				if (num2 > 4f)
				{
					m_TextureStreamingSystem.RequestRegion(nativeList2[j], nativeList[j], num2, m_TextureBounds[i][j]);
					nativeList3[j] = -1f;
					m_RequestedThisFrame++;
				}
			}
		}
	}

	public int RegisterTexture(int stackConfigIndex, int stackGlobalIndex, int vtIndex, Bounds2 bounds)
	{
		NativeList<int> nativeList = m_TexturesIndices[stackConfigIndex];
		NativeList<int> nativeList2 = m_StackGlobalIndices[stackConfigIndex];
		NativeList<Bounds2> nativeList3 = m_TextureBounds[stackConfigIndex];
		for (int i = 0; i < nativeList.Length; i++)
		{
			if (nativeList[i] == vtIndex && nativeList2[i] == stackGlobalIndex && nativeList3[i].Equals(bounds))
			{
				return i;
			}
		}
		nativeList.Add(in vtIndex);
		m_TexturesMaxPixels[stackConfigIndex].Add(-1f);
		nativeList3.Add(in bounds);
		nativeList2.Add(in stackGlobalIndex);
		return nativeList.Length - 1;
	}

	public int GetTextureIndex(int stackIndex, int texturesIndex)
	{
		return m_TexturesIndices[stackIndex][texturesIndex];
	}

	public void UpdateMaxPixel(int stackIndex, int texturesIndex, float maxPixel)
	{
		m_TexturesMaxPixels[stackIndex][texturesIndex] = Mathf.Max(m_TexturesMaxPixels[stackIndex][texturesIndex], maxPixel);
	}
}
