using System;

namespace Game.Rendering;

[AttributeUsage(AttributeTargets.Field)]
public class MaterialPropertyAttribute : Attribute
{
	public string ShaderPropertyName { get; protected set; }

	public Type DataType { get; protected set; }

	public bool IsBuiltin { get; protected set; }

	public MaterialPropertyAttribute(string shaderPropertyName, Type dataType, bool isBuiltin = false)
	{
		ShaderPropertyName = shaderPropertyName;
		DataType = dataType;
		IsBuiltin = isBuiltin;
	}
}
