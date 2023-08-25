using System.Collections.Generic;

using PhantomBrigade;
using PhantomBrigade.Data;

using UnityEngine;

namespace EchKode.PBMods.BurstFire
{
	public class StrafeProjectileCreated : ISubsystemFunctionProjectileCreated
	{
		public float turnrate;

		public void OnPartProjectileCreated(
			EquipmentEntity subsystem,
			string context,
			ActionEntity action,
			ActionEntity subaction,
			float currentTime,
			CombatEntity owner,
			CombatEntity projectile,
			Vector3 firingPoint,
			Vector3 firingDirection,
			Vector3 targetPoint)
		{
			if (context != PartEventsProjectileCreated.OnPartProjectileCreated)
			{
				return;
			}

			if (action.hasTargetedEntity)
			{
				return;
			}

			if (!action.hasTargetedPointFinal)
			{
				return;
			}

			if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.ExecutionDetails))
			{
				Debug.LogFormat(
					"Mod {0} ({1}) {2} | subsystem: {3} | action: {4} | subaction {5}: {6} | projectile: {7} | firing point: {8:F1} | target point: {9:F1} | action target point: {10:F1} | action final target point: {11:F1} | previous attack target point: {12:F1}",
					ModLink.modIndex,
					ModLink.modID,
					nameof(StrafeProjectileCreated),
					subsystem.ToLog(),
					action.ToLog(),
					subaction.subActionIndex.i,
					subaction.ToLog(),
					projectile.ToLog(),
					firingPoint,
					targetPoint,
					action.targetedPoint.v,
					action.targetedPointFinal.v,
					action.hasPreviousAttackPoint ? action.previousAttackPoint.to : Vector3.zero);
			}

			if (subaction.ScheduledAttackEnd)
			{
				RestoreSubsystem(subsystem);
				action.ReplaceTargetedPoint(action.targetedPointFinal.v);

				if (scatterDirectionMap.TryGetValue(action.id.id, out var entry))
				{
					projectile.ReplaceFacing(entry.Direction);
					projectile.ReplaceRotation(Quaternion.LookRotation(entry.Direction));
					scatterDirectionMap.Remove(action.id.id);
				}

				return;
			}

			action.ReplacePreviousAttackPoint(owner.position.v, action.targetedPointFinal.v, currentTime);
			action.RemoveTargetedPointFinal();

			if (subaction.ScheduledAttackStart)
			{
				ReplaceSubsystem(subsystem);
				scatterDirectionMap.Add(action.id.id, (firingPoint, projectile.facing.v, currentTime));
				return;
			}

			if (!scatterDirectionMap.TryGetValue(action.id.id, out var scatter))
			{
				return;
			}

			projectile.ReplaceFacing(scatter.Direction);
			projectile.ReplaceRotation(Quaternion.LookRotation(scatter.Direction));

			AdjustForLag(
				subsystem,
				action,
				subaction,
				currentTime,
				projectile,
				firingPoint,
				scatter);
		}

		private void ReplaceSubsystem(EquipmentEntity subsystem)
		{
			if (alternateSubsystem == null)
			{
				alternateSubsystem = DataMultiLinkerSubsystem.GenerateEntry(subsystem.dataKeySubsystem.s, 0, forceRegenerate: true);
				var data = subsystem.dataLinkSubsystem.data;
				alternateSubsystem.groupMainKey = data.groupMainKey;
				alternateSubsystem.groupFilterKeys = data.groupFilterKeys;
				alternateSubsystem.parentHierarchy = data.parentHierarchy;
				alternateSubsystem.children = data.children;
				alternateSubsystem.textNameProcessed = data.textNameProcessed;
				alternateSubsystem.textDescProcessed = data.textDescProcessed;
				alternateSubsystem.hardpointsProcessed = data.hardpointsProcessed;
				alternateSubsystem.tagsProcessed = data.tagsProcessed;
				alternateSubsystem.statsProcessed = data.statsProcessed;
				alternateSubsystem.visualsProcessed = data.visualsProcessed;
				alternateSubsystem.attachmentsProcessed = data.attachmentsProcessed;
				alternateSubsystem.activationProcessed = data.activationProcessed;
				alternateSubsystem.projectileProcessed = data.projectileProcessed;
				alternateSubsystem.beamProcessed = data.beamProcessed;
				alternateSubsystem.customProcessed = data.customProcessed;
				alternateSubsystem.functionsProcessed = data.functionsProcessed;
				alternateSubsystem.stats[UnitStats.weaponTurnRateLimit].value = turnrate;
				alternateSubsystem.statsProcessed[UnitStats.weaponTurnRateLimit].value = turnrate;
			}
			subsystem.ReplaceDataLinkSubsystem(alternateSubsystem);
			var part = IDUtility.GetEquipmentEntity(subsystem.subsystemParentPart.equipmentID);
			DataHelperStats.RefreshStatCacheForPart(UnitStats.weaponTurnRateLimit, part);

		}

		private void RestoreSubsystem(EquipmentEntity subsystem)
		{
			var originalSubsystem = DataMultiLinker<DataContainerSubsystem>.GetEntry(subsystem.dataKeySubsystem.s, false);
			subsystem.ReplaceDataLinkSubsystem(originalSubsystem);
			var part = IDUtility.GetEquipmentEntity(subsystem.subsystemParentPart.equipmentID);
			DataHelperStats.RefreshStatCacheForPart(UnitStats.weaponTurnRateLimit, part);
		}

		private void AdjustForLag(
			EquipmentEntity subsystem,
			ActionEntity action,
			ActionEntity subaction,
			float currentTime,
			CombatEntity projectile,
			Vector3 firingPoint,
			(Vector3 FiringPoint, Vector3 Direction, float Time) scatter)
		{
			var lag = currentTime - subaction.startTime.f;
			if (lag < lagEpsilon)
			{
				scatterDirectionMap[action.id.id] = (firingPoint, scatter.Direction, currentTime);
				return;
			}

			var afp = scatter.FiringPoint;
			if (subaction.startTime.f > scatter.Time)
			{
				var interpolant = (subaction.startTime.f - scatter.Time) / (currentTime - scatter.Time);
				afp = Vector3.Lerp(scatter.FiringPoint, firingPoint, interpolant);
			}

			var part = IDUtility.GetEquipmentEntity(subsystem.subsystemParentPart.equipmentID);
			var wpnSpeed = Mathf.Max(1f, DataHelperStats.GetCachedStatForPart(UnitStats.weaponProjectileSpeed, part));
			var position = afp + scatter.Direction * wpnSpeed * lag;
			projectile.ReplacePosition(position);
		}

		private static DataContainerSubsystem alternateSubsystem;
		private static Dictionary<int, (Vector3 FiringPoint, Vector3 Direction, float Time)> scatterDirectionMap =
			new Dictionary<int, (Vector3, Vector3, float)>();

		private const float lagEpsilon = 0.005f;
	}
}
