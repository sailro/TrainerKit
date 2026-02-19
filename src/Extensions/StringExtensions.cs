using UnityEngine;

#nullable enable

namespace TrainerKit.Extensions;

public static class StringExtensions
{
	extension(string str)
	{
		public string Color(Color color)
		{
			// ColorUtility.ToHtmlStringRGB is stripped in IL2CPP — manual conversion
			var r = (int)(Mathf.Clamp01(color.r) * 255);
			var g = (int)(Mathf.Clamp01(color.g) * 255);
			var b = (int)(Mathf.Clamp01(color.b) * 255);
			return $"<color=#{r:X2}{g:X2}{b:X2}>{str}</color>";
		}

		public string Blue()
		{
			return str.Color(UnityEngine.Color.blue);
		}

		public string Yellow()
		{
			return str.Color(UnityEngine.Color.yellow);
		}

		public string Red()
		{
			return str.Color(UnityEngine.Color.red);
		}

		public string Green()
		{
			return str.Color(UnityEngine.Color.green);
		}

		public string Cyan()
		{
			return str.Color(UnityEngine.Color.cyan);
		}
	}
}
