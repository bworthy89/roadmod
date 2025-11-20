using System.Linq;
using UnityEngine;

namespace Game.Input;

[CreateAssetMenu(menuName = "Colossal/UI/UIInputActionCollection")]
public class UIInputActionCollection : ScriptableObject
{
	public UIBaseInputAction[] m_InputActions;

	public IProxyAction GetActionState(string actionName, string source)
	{
		UIBaseInputAction uIBaseInputAction = m_InputActions.FirstOrDefault((UIBaseInputAction a) => a.aliasName == actionName);
		if (!(uIBaseInputAction != null))
		{
			return null;
		}
		return uIBaseInputAction.GetState(actionName + " (" + source + ")");
	}
}
