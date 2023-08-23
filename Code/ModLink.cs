﻿using HarmonyLib;

using PBModManager = PhantomBrigade.Mods.ModManager;

using UnityEngine;

namespace EchKode.PBMods.BurstFire
{
	public sealed partial class ModLink : PhantomBrigade.Mods.ModLink
	{
		internal static int modIndex;
		internal static string modID;
		internal static string modPath;

		public override void OnLoad(Harmony harmonyInstance)
		{
			// Uncomment to get a file on the desktop showing the IL of the patched methods.
			// Output from FileLog.Log() will trigger the generation of that file regardless if this is set so
			// FileLog.Log() should be put in a guard.
			//Harmony.DEBUG = true;

			modIndex = PBModManager.loadedMods.Count;
			modID = metadata.id;
			modPath = metadata.path;

			LoadSettings();

			var patchAssembly = typeof(ModLink).Assembly;
			Debug.LogFormat(
				"Mod {0} is executing OnLoad | Using HarmonyInstance.PatchAll on assembly ({1}) | Directory: {2} | Full path: {3}",
				metadata.id,
				patchAssembly.FullName,
				metadata.directory,
				metadata.path);
			harmonyInstance.PatchAll(patchAssembly);

			RegisterFunctions(patchAssembly);
		}
	}
}
