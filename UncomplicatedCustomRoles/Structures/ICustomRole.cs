﻿using Exiled.API.Enums;
using PlayerRoles;
using System.Collections.Generic;
using UncomplicatedCustomRoles.Elements;
using UnityEngine;

namespace UncomplicatedCustomRoles.Structures
{
    public interface ICustomRole
    {
        public abstract int Id { get; set; }
        public abstract string Name { get; set; }
        public abstract SpawnCondition SpawnCondition { get; set; }
        public abstract int MaxPlayers { get; set; }
        public abstract int SpawnChance { get; set; }
        public abstract RoleTypeId Role { get; set; }
        public abstract List<RoleTypeId> CanReplaceRoles { get; set; }
        public abstract float Healt { get; set; }
        public abstract float MaxHealt { get; set; }
        public abstract float Ahp { get; set; }
        public abstract string SpawnBroadcast { get; set; }
        public abstract ushort SpawnBroadcastDuration { get; set; }
        public abstract List<ItemType> Inventory { get; set; }
        public abstract List<uint> CustomItemsInventory { get; set; }
        public abstract Dictionary<AmmoType, ushort> Ammo { get; set; }
        public abstract SpawnLocationType Spawn { get; set; }
        public abstract List<ZoneType> SpawnZones { get; set; }
        public abstract List<RoomType> SpawnRooms { get; set; }
        public Vector3 SpawnPosition { get; set; }
        public abstract bool IgnoreSpawnSystem { get; set; }
    }
}