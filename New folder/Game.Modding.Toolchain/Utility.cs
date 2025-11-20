using System.Linq;
using System.Text;

namespace Game.Modding.Toolchain;

public static class Utility
{
	public static string Escape(string argument)
	{
		if (argument.Length > 0 && argument.All((char c2) => !char.IsWhiteSpace(c2) && c2 != '"'))
		{
			return argument;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append('"');
		int num = 0;
		while (num < argument.Length)
		{
			char c = argument[num++];
			switch (c)
			{
			case '\\':
			{
				int num2 = 1;
				for (; num < argument.Length && argument[num] == '\\'; num++)
				{
					num2++;
				}
				if (num == argument.Length)
				{
					stringBuilder.Append('\\', num2 * 2);
				}
				else if (argument[num] == '"')
				{
					stringBuilder.Append('\\', num2 * 2 + 1).Append('"');
					num++;
				}
				else
				{
					stringBuilder.Append('\\', num2);
				}
				break;
			}
			case '"':
				stringBuilder.Append('\\').Append('"');
				break;
			default:
				stringBuilder.Append(c);
				break;
			}
		}
		stringBuilder.Append('"');
		return stringBuilder.ToString();
	}
}
