// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using HarmonyLib;

using PBModManager = PhantomBrigade.Mods.ModManager;

namespace EchKode.PBMods.BurstFire
{
	public sealed partial class ModLink : PhantomBrigade.Mods.ModLink
	{
		internal static int modIndex;
		internal static string modID;
		internal static string modPath;

		public override void OnLoad(Harmony harmonyInstance)
		{
			modIndex = PBModManager.loadedMods.Count;
			modID = metadata.id;
			modPath = metadata.path;

			LoadSettings();

			var myAssembly = typeof(ModLink).Assembly;
			RegisterFunctions(myAssembly);
		}
	}
}
