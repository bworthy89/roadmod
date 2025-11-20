using System;
using UnityEngine.InputSystem;

namespace Game;

public static class InputExtensions
{
	public static bool TryGetCompositeOfActionWithName(this InputAction action, string compositeName, out InputActionSetupExtensions.BindingSyntax iterator)
	{
		iterator = new InputActionSetupExtensions.BindingSyntax(action.actionMap, -1, action).NextCompositeBinding();
		while (iterator.valid && !iterator.binding.TriggersAction(action))
		{
			iterator = iterator.NextCompositeBinding();
		}
		while (iterator.valid && iterator.binding.TriggersAction(action) && iterator.binding.name != compositeName)
		{
			iterator = iterator.NextCompositeBinding();
		}
		if (iterator.valid)
		{
			return iterator.binding.TriggersAction(action);
		}
		return false;
	}

	public static bool TryGetFirstCompositeOfAction(this InputAction action, out InputActionSetupExtensions.BindingSyntax iterator)
	{
		iterator = new InputActionSetupExtensions.BindingSyntax(action.actionMap, -1, action).NextCompositeBinding();
		while (iterator.valid && !iterator.binding.TriggersAction(action))
		{
			iterator = iterator.NextCompositeBinding();
		}
		if (iterator.valid)
		{
			return iterator.binding.TriggersAction(action);
		}
		return false;
	}

	public static bool ForEachCompositeOfAction(this InputAction inputAction, InputActionSetupExtensions.BindingSyntax startIterator, Func<InputActionSetupExtensions.BindingSyntax, bool> action, out InputActionSetupExtensions.BindingSyntax endIterator)
	{
		endIterator = startIterator;
		if (action == null)
		{
			return false;
		}
		if (!startIterator.binding.isComposite)
		{
			startIterator = startIterator.NextCompositeBinding();
		}
		while (startIterator.valid && startIterator.binding.TriggersAction(inputAction))
		{
			if (!action(startIterator))
			{
				return false;
			}
			endIterator = startIterator;
			startIterator = startIterator.NextCompositeBinding();
		}
		return true;
	}

	public static bool ForEachCompositeOfAction(this InputAction inputAction, Func<InputActionSetupExtensions.BindingSyntax, bool> action)
	{
		if (action == null)
		{
			return false;
		}
		if (!inputAction.TryGetFirstCompositeOfAction(out var iterator))
		{
			return false;
		}
		InputActionSetupExtensions.BindingSyntax endIterator;
		return inputAction.ForEachCompositeOfAction(iterator, action, out endIterator);
	}

	public static bool ForEachPartOfCompositeWithName(this InputAction inputAction, InputActionSetupExtensions.BindingSyntax startIterator, string partName, Func<InputActionSetupExtensions.BindingSyntax, bool> action, out InputActionSetupExtensions.BindingSyntax endIterator)
	{
		endIterator = startIterator;
		if (string.IsNullOrEmpty(partName))
		{
			return false;
		}
		if (action == null)
		{
			return false;
		}
		if (startIterator.binding.isComposite)
		{
			startIterator = startIterator.NextPartBinding(partName);
		}
		while (startIterator.valid && startIterator.binding.isPartOfComposite && startIterator.binding.TriggersAction(inputAction))
		{
			if (!action(startIterator))
			{
				return false;
			}
			endIterator = startIterator;
			startIterator = startIterator.NextPartBinding(partName);
		}
		return true;
	}

	public static bool ForEachPartOfCompositeWithName(this InputAction inputAction, string partName, Func<InputActionSetupExtensions.BindingSyntax, bool> action)
	{
		if (action == null)
		{
			return false;
		}
		if (!inputAction.TryGetFirstCompositeOfAction(out var iterator))
		{
			return false;
		}
		InputActionSetupExtensions.BindingSyntax endIterator;
		return inputAction.ForEachPartOfCompositeWithName(iterator, partName, action, out endIterator);
	}
}
