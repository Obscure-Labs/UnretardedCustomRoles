using System;
using System.Collections.Generic;
using System.IO;
using Exiled.API.Features;
using UncomplicatedCustomRoles.Interfaces;
using UncomplicatedCustomRoles.Manager;
using Handler = UncomplicatedCustomRoles.Events.EventHandler;
using PlayerHandler = Exiled.Events.Handlers.Player;
using Scp049Handler = Exiled.Events.Handlers.Scp049;
using ServerHandler = Exiled.Events.Handlers.Server;

namespace UncomplicatedCustomRoles
{
    internal class Plugin : Plugin<Config>
    {
        public override string Name => "UncomplicatedCustomRoles";

        public override string Prefix => "UncomplicatedCustomRoles";

        public override string Author => "FoxWorn3365, Dr.Agenda";

        public override Version Version { get; } = new(2, 2, 0);

        public override Version RequiredExiledVersion { get; } = new(8, 9, 4);

        internal static Plugin Instance;

        internal Handler Handler;

        internal static Dictionary<int, ICustomRole> CustomRoles;

        // PlayerId => RoleId
        internal static Dictionary<int, int> PlayerRegistry = new();

        // RolesCount: RoleId => [PlayerId, PlayerId, ...]
        internal static Dictionary<int, List<int>> RolesCount = new();

        // PlayerId => List<IUCREffect>
        internal static Dictionary<int, List<IUCREffect>> PermanentEffectStatus = new();

        // List of PlayerIds
        internal static List<int> RoleSpawnQueue = new();

        // useful because when the spawn manager overrides the tags they will be saved here so when the role will be removed they will be reassigned
        // PlayerId => [color, name]
        internal static Dictionary<int, string[]> Tags = new();

        internal bool DoSpawnBasicRoles = false;

        internal static FileConfigs FileConfigs;

        public override void OnEnabled()
        {
            Instance = this;

            Handler = new();
            CustomRoles = new();
            FileConfigs = new();

            ServerHandler.RespawningTeam += Handler.OnRespawningWave;
            ServerHandler.RoundStarted += Handler.OnRoundStarted;
            PlayerHandler.Died += Handler.OnDied;
            PlayerHandler.Spawning += Handler.OnSpawning;
            PlayerHandler.Spawned += Handler.OnPlayerSpawned;
            PlayerHandler.Escaping += Handler.OnEscaping;
            PlayerHandler.UsedItem += Handler.OnItemUsed;
            PlayerHandler.Hurting += Handler.OnHurting;
            Scp049Handler.StartingRecall += Handler.OnScp049StartReviving;

            if (Config.EnableBasicLogs)
            {
                LogManager.Info("===========================================");
                LogManager.Info(" Thanks for using UncomplicatedCustomRoles");
                LogManager.Info("        by FoxWorn3365 & Dr.Agenda");
                LogManager.Info("===========================================");
                LogManager.Info(">> Join our discord: https://discord.gg/5StRGu8EJV <<");
            }

            FileConfigs.Welcome();
            FileConfigs.Welcome(Server.Port.ToString());
            FileConfigs.LoadAll();
            FileConfigs.LoadAll(Server.Port.ToString());

            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            Instance = null;

            ServerHandler.RespawningTeam -= Handler.OnRespawningWave;
            ServerHandler.RoundStarted -= Handler.OnRoundStarted;
            PlayerHandler.Died -= Handler.OnDied;
            PlayerHandler.Spawning -= Handler.OnSpawning;
            PlayerHandler.Spawned -= Handler.OnPlayerSpawned;
            PlayerHandler.Escaping -= Handler.OnEscaping;
            PlayerHandler.UsedItem -= Handler.OnItemUsed;
            PlayerHandler.Hurting -= Handler.OnHurting;
            Scp049Handler.StartingRecall -= Handler.OnScp049StartReviving;

            Handler = null;
            CustomRoles = null;

            base.OnDisabled();
        }
    }
}