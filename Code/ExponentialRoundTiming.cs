// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;

using PhantomBrigade;

using UnityEngine;

namespace EchKode.PBMods.BurstFire
{
	public class ExponentialRoundTiming : ISubsystemFunctionFiring
	{
		public float timeFrom;
		public float timeTo = 1f;
		public float exponent = 1f;

		public void OnPartEventFiring(EquipmentEntity subsystem, string context, ActionEntity action, float startTime, float duration, List<ActionEntity> subactions)
		{
			var clampedFrom = Mathf.Clamp01(timeFrom);
			var clampedTo = Mathf.Clamp01(timeTo);
			var clampedExponent = Mathf.Clamp(exponent, exponentLowerBound, exponentUpperBound);
			var endTime = startTime + duration;

			var timingStart = Mathf.Lerp(startTime, endTime, clampedFrom);
			var timingEnd = Mathf.Lerp(startTime, endTime, clampedTo);
			var exponential = !clampedExponent.RoughlyEqual(1f);

			foreach (var subaction in subactions)
			{
				var interpolant = subaction.subActionIndex.i / (subactions.Count - 1f);
				if (exponential)
				{
					interpolant = Mathf.Pow(interpolant, clampedExponent);
				}
				var newTiming = Mathf.Lerp(timingStart, timingEnd, interpolant);
				if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.ExecutionDetails))
				{
					Debug.LogFormat(
						"Mod {0} ({1}) {2} -- timing change {6:F3}s → {7:F3}s | subsystem: {3} | action: {4} | subaction: {5}",
						ModLink.modIndex,
						ModLink.modID,
						nameof(ExponentialRoundTiming),
						subsystem.ToLog(),
						action.ToLog(),
						subaction.ToLog(),
						subaction.startTime.f,
						newTiming);
				}
				subaction.ReplaceStartTime(newTiming);
			}
		}

		// These bounds come from the recommendations in the wiki describing the new activation timing feature.
		// https://wiki.braceyourselfgames.com/en/PhantomBrigade/Modding/ModSystemChanges
		private const float exponentLowerBound = 0.5f;
		private const float exponentUpperBound = 2f;
	}
}
