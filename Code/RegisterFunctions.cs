using System.Reflection;

using Content.Code.Utility;

using HarmonyLib;

using Sirenix.Utilities;

using UnityEngine;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace EchKode.PBMods.BurstFire
{
	partial class ModLink
	{
		static void RegisterFunctions(Assembly assemblyWithFunctions)
		{
			var tagMappings = UtilitiesYAML.GetTagMappings();
			var registeredMappingCount = 0;

			foreach (var definedType in assemblyWithFunctions.DefinedTypes)
			{
				var isTypeHinted = TypeExtensions.GetCustomAttribute<TypeHintedAttribute>(definedType, true) != null;
				if (!isTypeHinted)
				{
					foreach (var type in definedType.GetInterfaces())
					{
						isTypeHinted = TypeExtensions.GetCustomAttribute<TypeHintedAttribute>(type, true) != null;
						if (isTypeHinted)
						{
							break;
						}
					}
				}
				if (!isTypeHinted)
				{
					continue;
				}

				// Functions provided by mods should use the full namespace of the mod to avoid naming collisions.
				// This will also help people looking at ConfigEdits to distinguish functions provided by the game
				// ("built-in") from ones supplied by mods.
				var key = "!" + definedType.Namespace + "." + definedType.GetUserFriendlyName();
				if (tagMappings.ContainsKey(key))
				{
					var registeredType = tagMappings[key];
					if (definedType != registeredType)
					{
						Debug.LogWarningFormat(
							"Mod {0} ({1}) unable to register YAML tag {2} for function {3} because the tag is already registered to function {4}",
							modIndex,
							modID,
							key,
							definedType.FullName,
							registeredType.FullName);
					}
					continue;
				}

				tagMappings.Add(key, definedType);
				if (Settings.IsLoggingEnabled(ModSettings.LoggingFlag.FunctionRegistration))
				{
					Debug.LogFormat(
						"Mod {0} ({1}) registered YAML tag {2} for function {3}",
						modIndex,
						modID,
						key,
						definedType.FullName);
				}
				registeredMappingCount += 1;
			}

			RebuildYAMLDeserializer(registeredMappingCount);
		}

		static void RebuildYAMLDeserializer(int registeredMappingCount)
		{
			if (registeredMappingCount == 0)
			{
				return;
			}

			var t = Traverse.Create(typeof(UtilitiesYAML));
			var deserializer = t.Field<IDeserializer>("deserializer");
			if (deserializer.Value == null)
			{
				return;
			}

			if (Settings.IsLoggingEnabled(ModSettings.LoggingFlag.FunctionRegistration))
			{
				Debug.LogFormat(
					"Mod {0} ({1}) registered new YAML tag mappings, rebuilding deserializer | registered mapping count: {2}",
					modIndex,
					modID,
					registeredMappingCount);
			}

			var builder = new DeserializerBuilder();
			t.Method("AddTagsToDeserializer", builder).GetValue();
			builder.IgnoreUnmatchedProperties();
			builder.WithNamingConvention(new NullNamingConvention());
			deserializer.Value = builder.Build();
		}
	}
}
