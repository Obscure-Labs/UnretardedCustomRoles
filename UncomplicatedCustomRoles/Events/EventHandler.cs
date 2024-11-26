﻿using Exiled.API.Features;
using System.Collections.Generic;
using System.Linq;
using UncomplicatedCustomRoles.Manager;
using UncomplicatedCustomRoles.Interfaces;
using MEC;
using Exiled.Events.EventArgs.Player;
using PlayerRoles;
using Exiled.Permissions.Extensions;
using Exiled.Events.EventArgs.Server;
using Exiled.Events.EventArgs.Scp049;
using Exiled.API.Enums;
using UnityEngine;
using UncomplicatedCustomRoles.Extensions;
using ObscureLabs.API.Features;

namespace UncomplicatedCustomRoles.Events
{
    public class EventHandler
    {
        internal CoroutineHandle EffectCoroutine;

        public void OnRoundStarted()
        {
            if (ExternalGamemode.CheckGamemode()) return;
            Plugin.PlayerRegistry = new();
            Plugin.RolesCount = new();
            foreach (KeyValuePair<int, ICustomRole> Data in Plugin.CustomRoles)
            {
                Plugin.RolesCount[Data.Key] = new();
            }
            Plugin.Instance.DoSpawnBasicRoles = false;
            if (EffectCoroutine.IsRunning)
            {
                Timing.KillCoroutines(EffectCoroutine);
            }
            EffectCoroutine = Timing.RunCoroutine(DoSetInfiniteEffectToPlayers());
            Timing.CallDelayed(5, () =>
            {
                Plugin.Instance.DoSpawnBasicRoles = true;
            });
            foreach (Player Player in Player.List.Where(player => !player.IsNPC))
            {
                DoEvaluateSpawnForPlayer(Player);
            }
        }

        public void OnPlayerSpawned(SpawnedEventArgs Spawned)
        {
            if (ExternalGamemode.CheckGamemode()) return;
            SpawnManager.LimitedClearCustomTypes(Spawned.Player);
            if (!Plugin.Instance.DoSpawnBasicRoles)
            {
                return;
            }

            if (Plugin.PlayerRegistry.ContainsKey(Spawned.Player.Id))
            {
                return;
            }

            if (Spawned.Player.IsNPC)
            {
                return;
            }

            string LogReason = string.Empty;
            if (Plugin.Instance.Config.AllowOnlyNaturalSpawns && !Plugin.RoleSpawnQueue.Contains(Spawned.Player.Id))
            {
                LogManager.Debug("The player is not in the queue for respawning!");
                return;
            }
            else if (Plugin.RoleSpawnQueue.Contains(Spawned.Player.Id))
            {
                Plugin.RoleSpawnQueue.Remove(Spawned.Player.Id);
                LogReason = " [going with a respawn wave OR 049 revival]";
            }

            LogManager.Debug($"Player {Spawned.Player.Nickname} spawned{LogReason}, going to assign a role if needed!");

            Timing.CallDelayed(0.1f, () =>
            {
                DoEvaluateSpawnForPlayer(Spawned.Player);
            });
        }

        public void OnScp049StartReviving(StartingRecallEventArgs Recall)
        {
            if (ExternalGamemode.CheckGamemode()) return;
            if (Plugin.CustomRoles.Where(cr => cr.Value.CanReplaceRoles.Contains(RoleTypeId.Scp0492)).Count() > 0) {
                Plugin.RoleSpawnQueue.Add(Recall.Target.Id);
            }
        }

        public void OnDied(DiedEventArgs Died)
        {
            if (ExternalGamemode.CheckGamemode()) return;
            SpawnManager.ClearCustomTypes(Died.Player);
        }

        public void OnSpawning(SpawningEventArgs Spawning)
        {
            if (ExternalGamemode.CheckGamemode()) return;
            SpawnManager.ClearCustomTypes(Spawning.Player);
        }

        public void OnHurting(HurtingEventArgs Hurting)
        {
            if (ExternalGamemode.CheckGamemode()) return;
            if (Plugin.PlayerRegistry.ContainsKey(Hurting.Player.Id))
            {
                ICustomRole Role = Plugin.CustomRoles[Plugin.PlayerRegistry[Hurting.Player.Id]];
                Hurting.DamageHandler.Damage *= Role.DamageMultiplier;
            }
        }

        public void OnEscaping(EscapingEventArgs Escaping)
        {
            if (ExternalGamemode.CheckGamemode()) return;
            LogManager.Debug($"Player {Escaping.Player.Nickname} triggered the escaping event as {Escaping.Player.Role.Name}");

            if (Plugin.PlayerRegistry.ContainsKey(Escaping.Player.Id))
            {
                LogManager.Debug($"Player IS a custom role: {Plugin.PlayerRegistry[Escaping.Player.Id]}");
                ICustomRole Role = Plugin.CustomRoles[Plugin.PlayerRegistry[Escaping.Player.Id]];

                if (!Role.CanEscape)
                {
                    LogManager.Debug($"Player with the role {Role.Id} ({Role.Name}) can't escape, so nuh uh!");
                    Escaping.IsAllowed = false;
                    return;
                }

                if (Role.CanEscape && (Role.RoleAfterEscape is null || Role.RoleAfterEscape.Length < 2))
                {
                    LogManager.Debug($"Player with the role {Role.Id} ({Role.Name}) evaluated for a natural respawn!");
                    Escaping.IsAllowed = true;
                    return;
                }

                // Try to set the role
                RoleTypeId? NewRole = SpawnManager.ParseEscapeRole(Role.RoleAfterEscape, Escaping.Player);

                if (NewRole is not null)
                {
                    Escaping.IsAllowed = true;
                    Escaping.NewRole = (RoleTypeId)NewRole;
                }
                else
                {
                    Escaping.IsAllowed = false;
                }
            }
        }

        public void OnRespawningWave(RespawningTeamEventArgs Respawn)
        {
            if (ExternalGamemode.CheckGamemode()) return;
            foreach (Player Player in Respawn.Players)
            {
                Plugin.RoleSpawnQueue.Add(Player.Id);
            }
        }

        public void OnItemUsed(UsedItemEventArgs UsedItem)
        {
            if (ExternalGamemode.CheckGamemode()) return;
            if (UsedItem.Player is not null && UsedItem.Player.HasCustomRole() && Plugin.PermanentEffectStatus.ContainsKey(UsedItem.Player.Id) && UsedItem.Item.Type == ItemType.SCP500)
            {
                foreach (IUCREffect Effect in Plugin.PermanentEffectStatus[UsedItem.Player.Id])
                {
                    if (Effect.Removable)
                    {
                        Plugin.PermanentEffectStatus[UsedItem.Player.Id].Remove(Effect);
                    }
                }
                SpawnManager.SetAllActiveEffect(UsedItem.Player);
            }
        }

        public IEnumerator<float> DoSetInfiniteEffectToPlayers()
        {
            if (ExternalGamemode.CheckGamemode()) yield break;
            while (Round.InProgress)
            {
                foreach (Player Player in Player.List.Where(player => Plugin.PermanentEffectStatus.ContainsKey(player.Id) && player.IsAlive && Plugin.PlayerRegistry.ContainsKey(player.Id)))
                {
                    SpawnManager.SetAllActiveEffect(Player);
                }

                // Here we can see and trigger role for SCPs escape event
                foreach (Player Player in Player.List.Where(player => player.IsScp && Vector3.Distance(new(123.85f, 988.8f, 18.9f), player.Position) < 2.5f)) 
                {
                    LogManager.Debug("Calling respawn event for plauer -> position");
                    // Let's make this SCP escape
                    OnEscaping(new(Player, RoleTypeId.ChaosConscript, EscapeScenario.None));
                }

                yield return Timing.WaitForSeconds(2f);
            }
        }

        public static IEnumerator<float> DoSpawnPlayer(Player Player, int Id, bool DoBypassRoleOverwrite = true)
        {
            if (ExternalGamemode.CheckGamemode()) yield break;
            yield return Timing.WaitForSeconds(0.1f);
            SpawnManager.SummonCustomSubclass(Player, Id, DoBypassRoleOverwrite);
        }

        public static void DoEvaluateSpawnForPlayer(Player Player)
        {
            if (ExternalGamemode.CheckGamemode()) return;
            Dictionary<RoleTypeId, List<ICustomRole>> RolePercentage = new()
            {
                { RoleTypeId.ClassD, new() },
                { RoleTypeId.Scientist, new() },
                { RoleTypeId.NtfPrivate, new() },
                { RoleTypeId.NtfSergeant, new() },
                { RoleTypeId.NtfCaptain, new() },
                { RoleTypeId.NtfSpecialist, new() },
                { RoleTypeId.ChaosConscript, new() },
                { RoleTypeId.ChaosMarauder, new() },
                { RoleTypeId.ChaosRepressor, new() },
                { RoleTypeId.ChaosRifleman, new() },
                { RoleTypeId.Tutorial, new() },
                { RoleTypeId.Scp049, new() },
                { RoleTypeId.Scp0492, new() },
                { RoleTypeId.Scp079, new() },
                { RoleTypeId.Scp173, new() },
                { RoleTypeId.Scp939, new() },
                { RoleTypeId.Scp096, new() },
                { RoleTypeId.Scp106, new() },
                { RoleTypeId.Scp3114, new() },
                { RoleTypeId.FacilityGuard, new() }
            };

            foreach (KeyValuePair<int, ICustomRole> Role in Plugin.CustomRoles)
            {
                if (!Role.Value.IgnoreSpawnSystem && Player.List.Count() >= Role.Value.MinPlayers)
                {
                    if (Role.Value.RequiredPermission != null && Role.Value.RequiredPermission != string.Empty && !Player.CheckPermission(Role.Value.RequiredPermission))
                    {
                        LogManager.Debug($"[NOTICE] Ignoring the role {Role.Value.Id} [{Role.Value.Name}] while creating the list for the player {Player.Nickname} due to: cannot [permissions].");
                        continue;
                    }

                    foreach (RoleTypeId RoleType in Role.Value.CanReplaceRoles)
                    {
                        for (int a = 0; a < Role.Value.SpawnChance; a++)
                        {
                            RolePercentage[RoleType].Add(Role.Value);
                        }
                    }
                }
            }

            if (Plugin.PlayerRegistry.ContainsKey(Player.Id))
            {
                LogManager.Debug("Was evalutating role select for an already custom role player, stopping");
                return;
            }

            if (RolePercentage.ContainsKey(Player.Role.Type))
            {
                // We can proceed with the chance
                int Chance = new System.Random().Next(0, 100);
                if (Chance < RolePercentage[Player.Role.Type].Count())
                {
                    // The role exists, good, let's give the player a role
                    int RoleId = RolePercentage[Player.Role.Type].RandomItem().Id;

                    if (Plugin.RolesCount[RoleId].Count() < Plugin.CustomRoles[RoleId].MaxPlayers)
                    {
                        Timing.RunCoroutine(DoSpawnPlayer(Player, RoleId, false));
                        Plugin.RolesCount[RoleId].Add(Player.Id);
                        LogManager.Debug($"Player {Player.Nickname} spawned as CustomRole {RoleId}");
                    }
                    else
                    {
                        LogManager.Debug($"Player {Player.Nickname} won't be spawned as CustomRole {RoleId} because it has reached the maximus number");
                    }
                }
            }
        }
    }
}