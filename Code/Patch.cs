// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using Entitas;

using HarmonyLib;

using PhantomBrigade.Data;
using PBEquipmentActionSystem = PhantomBrigade.Combat.Systems.EquipmentActionSystem;
using PBScheduledAttackSystem = PhantomBrigade.Combat.Systems.ScheduledAttackSystem;

using UnityEngine;

namespace EchKode.PBMods.BurstFire
{
	[HarmonyPatch]
	static class Patch
	{
		[HarmonyPatch(typeof(PBEquipmentActionSystem), "Execute", new System.Type[] { typeof(List<CombatEntity>) })]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Execute_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var subactionList = generator.DeclareLocal(typeof(List<ActionEntity>));
			var cm = new CodeMatcher(instructions, generator);
			var gaematch = new CodeMatch(OpCodes.Callvirt, AccessTools.DeclaredMethod(typeof(IGroup<ActionEntity>), nameof(IGroup<ActionEntity>.GetEntities), new System.Type[] { }));
			var febrmatch = new CodeMatch(OpCodes.Br);
			var storeactionmatch = new CodeMatch(OpCodes.Stloc_S);
			var apmatch = new CodeMatch(OpCodes.Ldfld, AccessTools.DeclaredField(typeof(DataContainerSubsystem), "activationProcessed"));
			var apbrmatch = new CodeMatch(OpCodes.Brfalse);
			var rcbrmatch = new CodeMatch(OpCodes.Ble);
			var listCtor = AccessTools.FirstConstructor(typeof(List<ActionEntity>), (ConstructorInfo x) => !x.IsStatic && x.GetParameters().Length == 0);
			var instructionNewList = new[]
			{
				new CodeInstruction(OpCodes.Newobj, listCtor),
				new CodeInstruction(OpCodes.Stloc, subactionList)
			};
			var startTimeGetter = AccessTools.DeclaredPropertyGetter(typeof(ActionEntity), nameof(ActionEntity.startTime));
			var propstmatch = new CodeMatch(OpCodes.Callvirt, startTimeGetter);
			var storestmatch = new CodeMatch(OpCodes.Stloc_S);
			var submatch = new CodeMatch(OpCodes.Sub);
			var createEntityMethod = AccessTools.DeclaredMethod(typeof(Context<ActionEntity>), nameof(Context<ActionEntity>.CreateEntity));
			var createSubactionMatch = new CodeMatch(OpCodes.Callvirt, createEntityMethod);
			var peeField = AccessTools.DeclaredField(typeof(DataContainerSettingsSimulation), nameof(DataContainerSettingsSimulation.partEventsEnabled));
			var peematch = new CodeMatch(OpCodes.Ldfld, peeField);
			var peebrmatch = new CodeMatch(OpCodes.Brfalse_S);
			var retmatch = new CodeMatch(OpCodes.Ret);
			var simGetter = AccessTools.DeclaredPropertyGetter(typeof(DataShortcuts), nameof(DataShortcuts.sim));
			var partEventFiringMethod = AccessTools.DeclaredMethod(typeof(PartEventsFiring), nameof(PartEventsFiring.OnPartEventFiring));
			var partEventActionMethod = AccessTools.DeclaredMethod(typeof(PartEventsAction), nameof(PartEventsAction.OnPartEventAction));

			var action = cm.MatchEndForward(gaematch)
				.MatchEndForward(febrmatch)  // foreach first does a branch
				.MatchEndForward(storeactionmatch)
				.Operand;
			var rcbranch = cm.MatchEndForward(apmatch)
				.MatchEndForward(apbrmatch)  // this is the branch on activationProcess != null
				.MatchEndForward(rcbrmatch)  // this is the branch on act_count > 0
				.Instruction;
			var adjustedStartTime = cm.Advance(1)
				.InsertAndAdvance(instructionNewList)  // sneak in some extra instructions here
				.MatchEndForward(propstmatch)
				.MatchEndForward(storestmatch)  // this is where the adjusted start time is stored
				.Operand;
			var adjustedDuration = cm.MatchEndForward(submatch).Advance(1).Operand;
			var subaction = cm.MatchEndForward(createSubactionMatch).Advance(1).Operand;

			var listAddMethod = AccessTools.DeclaredMethod(typeof(List<ActionEntity>), nameof(List<ActionEntity>.Add));
			var storeSubaction = new[]
			{
				new CodeInstruction(OpCodes.Ldloc, subactionList),
				new CodeInstruction(OpCodes.Ldloc_S, subaction),
				new CodeInstruction(OpCodes.Callvirt, listAddMethod),
			};
			cm.Advance(1).InsertAndAdvance(storeSubaction);

			var part = cm.MatchEndForward(peematch)
				.MatchEndForward(peebrmatch)
				.Advance(1)
				.Operand;
			cm.Advance(1)
				.SetOperandAndAdvance(PartEventsFiring.OnPartFiring)
				.SetAndAdvance(OpCodes.Ldloc_S, action);  // stomp on existing call because we're going to replace it
			var firingEventCall = new[]
			{
				new CodeInstruction(OpCodes.Ldloc_S, adjustedStartTime),
				new CodeInstruction(OpCodes.Ldloc_S, adjustedDuration),
				new CodeInstruction(OpCodes.Ldloc, subactionList),
				new CodeInstruction(OpCodes.Call, partEventFiringMethod),
			};
			cm.InsertAndAdvance(firingEventCall);

			var partEventsActionBranch = new CodeInstruction(OpCodes.Br, rcbranch.operand);
			cm.InsertAndAdvance(partEventsActionBranch);  // skip checking the partEventsEnabled flag again below

			cm.Insert(new CodeInstruction(OpCodes.Call, simGetter));
			cm.CreateLabel(out var partEventsActionCheckLabel);
			var actionLoopEndLabel = rcbranch.operand;
			rcbranch.operand = partEventsActionCheckLabel;
			cm.Advance(1);

			var partEventsActionCheck = new[]
			{
				new CodeInstruction(OpCodes.Ldfld, peeField),
				new CodeInstruction(OpCodes.Brfalse_S, actionLoopEndLabel),
			};
			cm.InsertAndAdvance(partEventsActionCheck);

			cm.Insert(new CodeInstruction(OpCodes.Ldloc_S, part));
			cm.CreateLabel(out var partEventsActionLabel);
			partEventsActionBranch.operand = partEventsActionLabel;
			cm.Advance(1);

			var partEventsActionCall = new[]
			{
				new CodeInstruction(OpCodes.Ldstr, PartEventsGeneral.OnPartActivation),  // the constant should be moved to PartEventsAction
				new CodeInstruction(OpCodes.Ldloc_S, action),
				new CodeInstruction(OpCodes.Call, partEventActionMethod),
			};
			cm.InsertAndAdvance(partEventsActionCall);

			return cm.InstructionEnumeration();
		}

		[HarmonyPatch(typeof(PBScheduledAttackSystem), "ProcessProjectiles", new[]
		{
			typeof(float),
			typeof(CombatEntity),
			typeof(EquipmentEntity),
			typeof(ActionEntity),
			typeof(ActionEntity),
		})]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> ProcessProjectiles_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var cm = new CodeMatcher(instructions, generator);
			var firingDirection = generator.DeclareLocal(typeof(Vector3));
			var actionArg = cm.MatchEndForward(new CodeMatch(OpCodes.Ldarg_S)).Operand;
			var subActionArg = cm.MatchStartForward(new CodeMatch(OpCodes.Callvirt, AccessTools.DeclaredPropertyGetter(typeof(ActionEntity), nameof(ActionEntity.ScheduledAttackStart))))
				.Advance(-1)
				.Operand;
			var targetPoint = cm.MatchStartForward(new CodeMatch(OpCodes.Call, AccessTools.DeclaredMethod(typeof(Utilities), nameof(Utilities.GetDirection))))
				.Advance(-1)
				.Operand;
			var firingPoint = cm.Advance(-1).Operand;
			var projectile = cm.MatchEndForward(new CodeMatch(OpCodes.Callvirt, AccessTools.DeclaredMethod(typeof(Context<CombatEntity>), nameof(Context<CombatEntity>.CreateEntity))))
				.Advance(1)
				.Operand;
			var facing = cm.MatchEndBackwards(new CodeMatch(OpCodes.Callvirt, AccessTools.DeclaredPropertyGetter(typeof(Transform), nameof(Transform.forward))))
				.Advance(1)
				.Operand;
			var forwardJumpLabels = cm.Advance(1).Labels;
			// Firing direction gets modified by scatter angle so preserve a copy of the direction before that modification happens.
			var storeDirection = new[]
			{
				new CodeInstruction(OpCodes.Ldloc_S, facing),
				new CodeInstruction(OpCodes.Stloc_S, firingDirection),
			};
			var itfCall = new[]
			{
				new CodeInstruction(OpCodes.Ldarg_3),  // part
				new CodeInstruction(OpCodes.Ldstr, PartEventsProjectileCreated.OnPartProjectileCreated),
				new CodeInstruction(OpCodes.Ldarg_S, actionArg),
				new CodeInstruction(OpCodes.Ldarg_S, subActionArg),
				new CodeInstruction(OpCodes.Ldarg_1),  // current time
				new CodeInstruction(OpCodes.Ldarg_2),  // owner
				new CodeInstruction(OpCodes.Ldloc_S, projectile),
				new CodeInstruction(OpCodes.Ldloc_S, firingPoint),
				new CodeInstruction(OpCodes.Ldloc_S, firingDirection),
				new CodeInstruction(OpCodes.Ldloc_S, targetPoint),
				new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(PartEventsProjectileCreated), nameof(PartEventsProjectileCreated.OnPartEventProjectileCreated))),
			};

			cm.Insert(storeDirection);
			cm.AddLabels(forwardJumpLabels);
			cm.Advance(storeDirection.Length);
			cm.Labels.Clear();
			cm.MatchEndForward(new CodeMatch(OpCodes.Call, AccessTools.DeclaredMethod(typeof(PBScheduledAttackSystem), nameof(PBScheduledAttackSystem.AddInflictedDamageComponents))))
				.Advance(1)
				.Insert(itfCall);

			return cm.InstructionEnumeration();
		}
	}
}
