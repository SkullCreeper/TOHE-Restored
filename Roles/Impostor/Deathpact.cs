﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AmongUs.GameOptions;
using MS.Internal.Xml.XPath;
using TOHE.Modules;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;
using static UnityEngine.GraphicsBuffer;

namespace TOHE.Roles.Impostor
{
    public static class Deathpact
    {
        private static readonly int Id = 9334634;
        public static List<byte> playerIdList = new();

        public static Dictionary<byte, List<PlayerControl>> PlayersInDeathpact = new();
        public static Dictionary<byte, long> DeathpactTime = new();

        public static List<byte> ActiveDeathpacts = new();

        private static OptionItem KillCooldown;
        private static OptionItem ShapeshiftCooldown;
        private static OptionItem ShapeshiftDuration;
        private static OptionItem DeathpactDuration;
        private static OptionItem NumberOfPlayersInPact;
        private static OptionItem ShowArrowsToOtherPlayersInPact;
        private static OptionItem ReduceVisionWhileInPact;
        private static OptionItem VisionWhileInPact;

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Deathpact);
            KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Deathpact])
                .SetValueFormat(OptionFormat.Seconds);
            ShapeshiftCooldown = FloatOptionItem.Create(Id + 11, "ShapeshiftCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Deathpact])
                .SetValueFormat(OptionFormat.Seconds);
            ShapeshiftDuration = FloatOptionItem.Create(Id + 12, "ShapeshiftDuration", new(0f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Deathpact])
                .SetValueFormat(OptionFormat.Seconds);
            DeathpactDuration = FloatOptionItem.Create(Id + 13, "DeathpactDuration", new(0f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Deathpact])
                .SetValueFormat(OptionFormat.Seconds);
            NumberOfPlayersInPact = IntegerOptionItem.Create(Id + 14, "DeathpactNumberOfPlayersInPact", new(2, 5, 1), 2, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Deathpact])
                .SetValueFormat(OptionFormat.Times);
            ShowArrowsToOtherPlayersInPact = BooleanOptionItem.Create(Id + 15, "DeathpactShowArrowsToOtherPlayersInPact", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Deathpact]);
            ReduceVisionWhileInPact = BooleanOptionItem.Create(Id + 16, "DeathpactReduceVisionWhileInPact", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Deathpact]);
            VisionWhileInPact = FloatOptionItem.Create(Id + 17, "DeathpactVisionWhileInPact", new(0f, 5f, 0.05f), 0.65f, TabGroup.ImpostorRoles, false).SetParent(ReduceVisionWhileInPact)
                .SetValueFormat(OptionFormat.Multiplier);
        }

        public static void Init()
        {
            playerIdList = new();
            PlayersInDeathpact = new();
            DeathpactTime = new();
            ActiveDeathpacts = new();
        }

        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            PlayersInDeathpact.TryAdd(playerId, new List<PlayerControl>());
            DeathpactTime.TryAdd(playerId, 0);
        }

        public static void ApplyGameOptions()
        {
            AURoleOptions.ShapeshifterCooldown = ShapeshiftCooldown.GetFloat();
            AURoleOptions.ShapeshifterDuration = ShapeshiftDuration.GetFloat();
        }
        public static bool IsEnable => playerIdList.Count > 0;

        public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

        public static void OnShapeshift(PlayerControl pc, PlayerControl target)
        {
            if (!pc.IsAlive() || Pelican.IsEaten(pc.PlayerId)) return;

            if (!PlayersInDeathpact[pc.PlayerId].Any(b => b.PlayerId == target.PlayerId))
            {
                PlayersInDeathpact[pc.PlayerId].Add(target);
            }

            if (PlayersInDeathpact[pc.PlayerId].Count < NumberOfPlayersInPact.GetInt())
            {
                return;
            }

            if (ReduceVisionWhileInPact.GetBool())
            {
                MarkEveryoneDirtySettings();
            }

            pc.Notify(GetString("DeathpactComplete"));
            DeathpactTime[pc.PlayerId] = Utils.GetTimeStamp() + (long)DeathpactDuration.GetFloat();
            ActiveDeathpacts.Add(pc.PlayerId);

            foreach (var player in PlayersInDeathpact[pc.PlayerId])
            {
                player.Notify(GetString("DeathpactAssigned"));

                if (!ShowArrowsToOtherPlayersInPact.GetBool())
                {
                    continue;
                }

                foreach (var otherPlayerInPact in PlayersInDeathpact[pc.PlayerId].Where(a => a.PlayerId != player.PlayerId))
                {
                    TargetArrow.Add(player.PlayerId, otherPlayerInPact.PlayerId);
                }
            }
        }

        public static void SetDeathpactVision(PlayerControl player, IGameOptions opt)
        {
            if (!ReduceVisionWhileInPact.GetBool())
            {
                return;
            }

            if (PlayersInDeathpact.Any(a => a.Value.Any(b => b.PlayerId == player.PlayerId) && a.Value.Count == NumberOfPlayersInPact.GetInt() ))
            {
                opt.SetVision(false);
                opt.SetFloat(FloatOptionNames.CrewLightMod, VisionWhileInPact.GetFloat());
                opt.SetFloat(FloatOptionNames.ImpostorLightMod, VisionWhileInPact.GetFloat());
            }
        }

        public static void OnFixedUpdate(PlayerControl player)
        {
            if (!IsEnable || !GameStates.IsInTask || !player.Is(CustomRoles.Deathpact)) return;
            if (!ActiveDeathpacts.Contains(player.PlayerId)) return;
            if (CheckCancelDeathpact(player)) return;
            if (DeathpactTime[player.PlayerId] < Utils.GetTimeStamp() && DeathpactTime[player.PlayerId] != 0)
            {
                foreach (var playerInDeathpact in PlayersInDeathpact[player.PlayerId])
                {
                    KillDeathpactPlayer(player, playerInDeathpact);
                }

                ClearDeathpact(player.PlayerId);
                player.Notify(Translator.GetString("DeathpactExecuted"));
            }
        }

        public static bool CheckCancelDeathpact(PlayerControl deathpact)
        {
            if (PlayersInDeathpact[deathpact.PlayerId].Any(a => a.Data.Disconnected || a.Data.IsDead))
            {
                ClearDeathpact(deathpact.PlayerId);
                deathpact.Notify(Translator.GetString("DeathpactAvertedByKill"));
                return true;
            }

            bool cancelDeathpact = true;

            foreach (var player in PlayersInDeathpact[deathpact.PlayerId])
            {
                float range = NormalGameOptionsV07.KillDistances[Mathf.Clamp(player.Is(CustomRoles.Reach) ? 2 : Main.NormalOptions.KillDistance, 0, 2)] + 0.5f;
                foreach (var otherPlayerInPact in PlayersInDeathpact[deathpact.PlayerId].Where(a => a.PlayerId != player.PlayerId))
                {
                    float dis = Vector2.Distance(player.transform.position, otherPlayerInPact.transform.position);
                    cancelDeathpact = cancelDeathpact && (dis <= range);
                }
            }

            if (cancelDeathpact)
            {
                ClearDeathpact(deathpact.PlayerId);
                deathpact.Notify(Translator.GetString("DeathpactAverted"));
            }

            return cancelDeathpact;
        }

        public static void KillDeathpactPlayer(PlayerControl deathpact, PlayerControl target)
        {
            if (deathpact == null || target == null || target.Data.Disconnected) return;
            if (!target.IsAlive()) return;
            
            Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Suicide;
            target.SetRealKiller(deathpact);
            target.RpcMurderPlayerV3(target);
        }

        public static void ClearDeathpact(byte deathpact)
        {
            DeathpactTime[deathpact] = 0;
            ActiveDeathpacts.Remove(deathpact);
            PlayersInDeathpact[deathpact].Clear();

            if (ReduceVisionWhileInPact.GetBool())
            {
                MarkEveryoneDirtySettings();
            }
        }
    }
}
