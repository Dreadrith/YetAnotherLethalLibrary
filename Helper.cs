using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using UnityEngine;

namespace YetAnotherLethalLibrary
{
	public static class Helper
	{
		/*internal static Type ExtendedGetType(string typeName)
		{
			var myType = Type.GetType(typeName);
			if (myType != null)
				return myType;
			foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
			{
				var types = assembly.GetTypes();
				myType = types.FirstOrDefault(t  => t.FullName == typeName);
				if (myType != null)
					return myType;
				myType = types.FirstOrDefault(t => t.Name == typeName);
				if (myType != null)
					return myType;
			}
			return null;
		}

		internal static Type HarmonyGetType(string typeName) => AccessTools.TypeByName(typeName);*/
		
		internal static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
		{ foreach (var item in enumerable) action(item); }

		internal static bool ConditionLog(string message, bool condition = true, LogLevel logLevel = LogLevel.Info)
		{
			if (condition) MyPlugin.instance.logSource.Log(logLevel, message);
			return condition;
		}

		public static float[] NormalizeChances(params float[] chances)
		{
			float[] normalizedChances = new float[chances.Length];
			float total = chances.Sum();
			for (int i = 0; i < chances.Length; i++)
				normalizedChances[i] = chances[i] / total;
			return normalizedChances;
		}
		
		public static FloatRange[] SpreadNormalizedChances(float[] normalizedChances, float maxRange = 100)
		{
			FloatRange[] ranges = new FloatRange[normalizedChances.Length];
			float total = 0;
			for (int i = 0; i < normalizedChances.Length; i++)
			{
				float rangeSpan = normalizedChances[i] * maxRange;
				ranges[i] = new FloatRange(total, total + rangeSpan);
				total += rangeSpan;
			}
			return ranges;
		}

		public struct FloatRange
		{
			public float min;
			public float max;

			public FloatRange(float min, float max)
			{
				this.min = min;
				this.max = max;
			}

			public float RandomInRange() => UnityEngine.Random.Range(min, max);
			public bool HasInRange(float value) => value >= min && value <= max;
		}

	}
}