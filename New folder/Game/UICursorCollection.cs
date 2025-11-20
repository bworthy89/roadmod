using System;
using System.Collections.Generic;
using cohtml.Net;
using UnityEngine;

namespace Game;

[CreateAssetMenu(menuName = "Colossal/UI/UICursorCollection", order = 1)]
public class UICursorCollection : ScriptableObject
{
	[Serializable]
	public class CursorInfo
	{
		public Texture2D m_Texture;

		public Vector2 m_Hotspot;

		public void Apply()
		{
			Cursor.SetCursor(m_Texture, m_Hotspot, CursorMode.Auto);
		}
	}

	[Serializable]
	public class NamedCursorInfo : CursorInfo
	{
		public string m_Name;
	}

	public CursorInfo m_Pointer;

	public CursorInfo m_Text;

	public CursorInfo m_Move;

	public NamedCursorInfo[] m_NamedCursors;

	private Dictionary<string, CursorInfo> m_NamedCursorsDict;

	private void OnEnable()
	{
		if (m_NamedCursors == null)
		{
			m_NamedCursors = new NamedCursorInfo[0];
		}
		m_NamedCursorsDict = new Dictionary<string, CursorInfo>();
		RefreshNamedCursorsDict();
	}

	public void SetCursor(Cursors cursor)
	{
		switch (cursor)
		{
		case Cursors.Pointer:
			m_Pointer.Apply();
			break;
		case Cursors.Text:
			m_Text.Apply();
			break;
		case Cursors.Move:
			m_Move.Apply();
			break;
		default:
			ResetCursor();
			break;
		}
	}

	public void SetCursor(string cursorName)
	{
		if (m_NamedCursorsDict.TryGetValue(cursorName, out var value))
		{
			value.Apply();
		}
		else
		{
			ResetCursor();
		}
	}

	public static void ResetCursor()
	{
		Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
	}

	private void RefreshNamedCursorsDict()
	{
		m_NamedCursorsDict.Clear();
		NamedCursorInfo[] namedCursors = m_NamedCursors;
		foreach (NamedCursorInfo namedCursorInfo in namedCursors)
		{
			m_NamedCursorsDict["cursor://" + namedCursorInfo.m_Name] = namedCursorInfo;
		}
	}
}
