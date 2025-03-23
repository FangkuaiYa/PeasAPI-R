using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using PeasAPI.CustomRpc;
using Reactor.Localization.Utilities;
using Reactor.Networking.Rpc;
using Reactor.Utilities.Extensions;
using UnityEngine;

namespace PeasAPI.Roles
{
    public static class RoleManager
    {
        public static List<byte> Crewmates => Utility.GetAllPlayers().Where(p => !p.Data.Role.IsImpostor).ToList().ConvertAll(p => p.PlayerId);

        public static List<byte> Impostors => Utility.GetAllPlayers().Where(p => p.Data.Role.IsImpostor).ToList().ConvertAll(p => p.PlayerId);

        public static List<BaseRole> Roles = new List<BaseRole>();

        public static int GetRoleId() => Roles.Count;

        public static void RegisterRole(BaseRole role) => Roles.Add(role);

        internal static RoleBehaviour ToRoleBehaviour(BaseRole customRole)
        {
            if (GameObject.Find($"{customRole.Name}-Role"))
            {
                return GameObject.Find($"{customRole.Name}-Role").GetComponent<ModRole>();
            }

            var roleObject = new GameObject($"{customRole.Name}-Role");
            roleObject.DontDestroy();

            var role = roleObject.AddComponent<ModRole>();
            role.StringName = CustomStringName.CreateAndRegister(customRole.Name);
            role.BlurbName = CustomStringName.CreateAndRegister(customRole.Description);
            role.BlurbNameLong = CustomStringName.CreateAndRegister(customRole.LongDescription);
            role.BlurbNameMed = CustomStringName.CreateAndRegister(customRole.Name);
            role.Role = (RoleTypes) (8 + customRole.Id);
            role.NameColor = customRole.Color;
            
            var abilityButtonSettings = ScriptableObject.CreateInstance<AbilityButtonSettings>();
            abilityButtonSettings.Image = customRole.Icon;
            abilityButtonSettings.Text = CustomStringName.CreateAndRegister("Please work");
            abilityButtonSettings.FontMaterial = Material.GetDefaultMaterial();
            role.Ability = abilityButtonSettings;
            
            role.MaxCount = customRole.MaxCount;
            role.TasksCountTowardProgress = customRole.HasToDoTasks;
            role.CanVent = customRole.CanVent;
            role.CanUseKillButton = customRole.CanKill();
        
            PeasAPI.Logger.LogInfo($"Created RoleBehaviour for Role {customRole.Name}");
            
            return role;
        }

        
        public static void ResetRoles()
        {
            foreach (var _role in Roles)
            {
                _role.Members.Clear();
            }
        }
        
        public static void RpcResetRoles()
        {
            Rpc<RpcResetRoles>.Instance.Send();
        }
        
        public static BaseRole GetRole(int id)
        {
            foreach (var _role in Roles)
            {
                if (_role.Id == id)
                    return _role;
            }

            return null;
        }

        public static T GetRole<T>() where T : BaseRole
        {
            foreach (var _role in Roles)
            {
                if (_role.GetType() == typeof(T))
                    return (T) _role;
            }

            return null;
        }

        public static class HostMod
        {
            public static Dictionary<BaseRole, bool> IsRole { get; set; } = new Dictionary<BaseRole, bool>();
            public static bool IsImpostor;
        }
    }
}