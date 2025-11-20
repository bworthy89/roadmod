using System.Runtime.InteropServices;
using Unity.Collections;

namespace Game.Rendering;

public struct CullingObjectElement
{
	public NativeArray<int> vertices;

	public int m_prefabIndex;

	public static int SizeOf => Marshal.SizeOf(typeof(CullingObjectElement));

	public CullingObjectElement(int[] vertices, int index)
	{
		this.vertices = new NativeArray<int>(vertices, Allocator.Persistent);
		m_prefabIndex = index;
	}
}
