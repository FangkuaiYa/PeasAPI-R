using System;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using Il2CppSystem.Text;
using InnerNet;
using PeasAPI.CustomRpc;
using Reactor.Networking.Rpc;
using Reactor.Utilities.Extensions;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PeasAPI.Options
{
    [HarmonyPatch]
    public static class Patches
    {
        private static readonly float AllOptionSize = 6.73f;
        private static float LowestOption = -7.85f;
        private static readonly float OptionSize = 0.5f;
        private static readonly float HudTextSize = 1.4f;

        private static Scroller OptionsScroller;

        [HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Start))]
        [HarmonyPostfix]
        private static void GameOptionsMenuStartPatch(GameOptionsMenu __instance)
        {
            var numberOptionPrefab = OptionManager.NumberOptionPrefab = Object.FindObjectsOfType<NumberOption>().FirstOrDefault();
            
            var toggleOptionPrefab = OptionManager.ToggleOptionPrefab = Object.FindObjectOfType<ToggleOption>(); 
            
            var stringOptionPrefab = OptionManager.StringOptionPrefab = Object.FindObjectsOfType<StringOption>().FirstOrDefault();

            LowestOption = 1.15f - __instance.Children.Length * 0.5f;

            foreach (var customOption in OptionManager.CustomOptions.Where(option => !option.AdvancedRoleOption))
            {
                OptionBehaviour option = null;

                if (customOption.GetType() == typeof(CustomToggleOption))
                {
                    option = ((CustomToggleOption)customOption).CreateOption(toggleOptionPrefab, stringOptionPrefab);
                }
                else if (customOption.GetType() == typeof(CustomNumberOption))
                {
                    option = ((CustomNumberOption) customOption).CreateOption(numberOptionPrefab);
                }
                else if (customOption.GetType() == typeof(CustomStringOption))
                {
                    option = ((CustomStringOption) customOption).CreateOption(stringOptionPrefab);
                }
                else if (customOption.GetType() == typeof(CustomOptionHeader))
                {
                    option = ((CustomOptionHeader) customOption).CreateOption(toggleOptionPrefab);
                }
                else if (customOption.GetType() == typeof(CustomOptionButton))
                {
                    option = ((CustomOptionButton) customOption).CreateOption(toggleOptionPrefab, stringOptionPrefab);
                }

                option.transform.localPosition = new Vector3(option.transform.localPosition.x,
                    LowestOption + 2 * OptionSize - (OptionManager.CustomOptions.IndexOf(customOption) + 1) * OptionSize, -1);

                var options = __instance.Children.ToList();
                options.Add(option);
                __instance.Children = options.ToArray();
            }

            __instance.GetComponentInParent<Scroller>().ContentYBounds.max =
                AllOptionSize + OptionManager.MenuVisibleOptions.Count * 0.5f - 2 * OptionSize;
        }

        [HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Update))]
        [HarmonyPostfix]
        private static void GameOptionsMenuUpdatePatch(GameOptionsMenu __instance)
        {
            __instance.GetComponentInParent<Scroller>().ContentYBounds.max =
                AllOptionSize + OptionManager.MenuVisibleOptions.Count * 0.5f - 2 * OptionSize;

            foreach (var option in __instance.Children.ToList().FindAll(option => option.IsCustom()))
            {
                var customOption = option.GetCustom();
                if (customOption == null)
                    continue;
                
                option.gameObject.SetActive(customOption.MenuVisible);
                
                if (customOption.MenuVisible)
                    option.transform.localPosition = new Vector3(option.transform.localPosition.x,
                        LowestOption + 2 * OptionSize - (OptionManager.MenuVisibleOptions.IndexOf(customOption) + 1) * OptionSize, -1);

                if (option.gameObject.GetComponent<ToggleOption>() != null)
                    option.gameObject.GetComponent<ToggleOption>().TitleText.text = customOption.Title;
                else if (option.gameObject.GetComponent<NumberOption>() != null)
                    option.gameObject.GetComponent<NumberOption>().TitleText.text = customOption.Title;
                else if (option.gameObject.GetComponent<StringOption>() != null)
                    option.gameObject.GetComponent<StringOption>().TitleText.text = customOption.Title;
            }
        }

        [HarmonyPatch(typeof(RolesSettingsMenu), nameof(RolesSettingsMenu.OnEnable))]
        [HarmonyPostfix]
        public static void RoleOptionCreatePatch(RolesSettingsMenu __instance)
        {
            var roleSettingPrefab = __instance.AllRoleSettings.ToArray()[0];
            var roleTabPrefab = __instance.AllAdvancedSettingTabs.ToArray()[0].Tab;
            foreach (var option in OptionManager.CustomRoleOptions)
            {
                if (option.GetType() == typeof(CustomRoleOption))
                {
                    var newSetting = option.CreateOption(roleSettingPrefab);
                    newSetting.transform.localPosition = roleSettingPrefab.transform.localPosition - new Vector3(0f , (__instance.AllRoleSettings.ToArray().Count + OptionManager.CustomRoleOptions.IndexOf(option) + 1) * 0.5f);
                    
                    if (!option.AdjustRoleSettings)
                    {
                        var roleOptionObject = (RoleOptionSetting)newSetting;
                        //roleOptionObject.ChanceText.gameObject.SetActive(false);
                        //roleOptionObject.CountText.gameObject.SetActive(false);
                        roleOptionObject.transform.FindChild("Count Plus_TMP").gameObject.SetActive(false);
                        roleOptionObject.transform.FindChild("Count Minus_TMP").gameObject.SetActive(false);
                        roleOptionObject.transform.FindChild("Chance Plus_TMP").gameObject.SetActive(false);
                        roleOptionObject.transform.FindChild("Chance Minus_TMP").gameObject.SetActive(false);
                    }

                    if (__instance.AllAdvancedSettingTabs.WrapToSystem()
                            .Find(button => button.Type == option.Role.RoleBehaviour.Role) == null)
                    {
                        var tab = option.CreateAdvancedOptions(roleTabPrefab);
                        if (tab != null)
                            __instance.AllAdvancedSettingTabs.Add(tab);
                    }
                
                    option.UpdateValuesAndText();
                }
            }

            var scroller = roleSettingPrefab.gameObject.transform.parent.parent.GetComponent<Scroller>();
            /*scroller.OnScrollYEvent += (Action<float>) (value =>
            {
                for (int i = 0; i < scroller.Inner.childCount; i++)
                {
                    var child = scroller.Inner.GetChild(i);
                    child.gameObject.SetActive(child.position.y < GameObject.Find("RolesChancesGroup/Text").transform.position.y);
                }
            });*/
            scroller.ContentYBounds.max = (OptionManager.CustomRoleOptions.Count - 3) * 0.5f;
            scroller.transform.FindChild("UI_Scrollbar").gameObject.SetActive(true);
        }
        [HarmonyPatch(typeof(RolesSettingsMenu), nameof(RolesSettingsMenu.ValueChanged))]
        [HarmonyPrefix]
        public static bool RoleOptionRoleRateChangePatch(RolesSettingsMenu __instance,
            [HarmonyArgument(0)] OptionBehaviour optionBehaviour)
        {
            var customOption = optionBehaviour.GetCustomOption();
            PeasAPI.Logger.LogInfo(customOption);
        
            if (customOption == null || customOption is not CustomRoleOption)
                return true;

            var roleOption = optionBehaviour as RoleOptionSetting;
            var customRoleOption = customOption as CustomRoleOption;
            customRoleOption.SetValue(roleOption.RoleMaxCount, roleOption.RoleChance);
            customRoleOption.UpdateValuesAndText();
            PeasAPI.Logger.LogInfo("Data has changed: " + roleOption.RoleMaxCount + " " + roleOption.RoleChance);
        
            return false;
        }

        [HarmonyPatch(typeof(RolesSettingsMenu), nameof(RolesSettingsMenu.ValueChanged))]
        [HarmonyPostfix]
        public static void RoleOptionValueChangedPatch(RolesSettingsMenu __instance, [HarmonyArgument(0)] OptionBehaviour obj)
        {
            var custom = obj.GetCustom();
            if (custom != null)
            {
                switch (custom)
                {
                    /*case CustomRoleOption option:
                        var rates = GameOptionsManager.Instance.currentNormalGameOptions.roleOptions.roles[option.Role.RoleBehaviour.Role].Rate;
                        option.SetValue(rates.MaxCount, rates.Chance);
                        break;*/
                    case CustomNumberOption option:
                        option.SetValue(obj.GetFloat());
                        break;
                    case CustomToggleOption option:
                        option.SetValue(obj.GetBool());
                        break;
                    case CustomStringOption option:
                        option.SetValue(obj.GetInt());
                        break;
                }
            }
        }
        
        [HarmonyPatch(typeof(OptionBehaviour), nameof(OptionBehaviour.SetAsPlayer))]
        public static class OptionBehaviourSetAsPlayerPatch
        {
            public static bool Prefix(OptionBehaviour __instance)
            {
                foreach (var button in __instance.GetComponentsInChildren<PassiveButton>())
                {
                    button.Destroy();
                    button.gameObject.SetActive(button.gameObject.name == __instance.gameObject.name &&
                                                __instance.Title != StringNames.GameRecommendedSettings);
                }
                
                return false;
            }
        }
        
        [HarmonyPatch(typeof(NumberOption), nameof(NumberOption.FixedUpdate))]
        [HarmonyPostfix]
        private static void NumberOptionFixedUpdatePatch(NumberOption __instance)
        {
            var customOption = (CustomNumberOption) __instance.GetCustom();
            if (customOption != null)
            {
                if (__instance.SuffixType == NumberSuffixes.None)
                {
                    __instance.ValueText.text = customOption.Value.ToString();
                    return;
                }

                if (__instance.SuffixType == NumberSuffixes.Multiplier)
                {
                    __instance.ValueText.text = customOption.Value + "x";
                    return;
                }

                if (__instance.SuffixType == NumberSuffixes.Seconds)
                {
                    __instance.ValueText.text = customOption.Value + "s";
                    return;
                }

                __instance.ValueText.text = customOption.Value.ToString();
                
            }
        }

        private static bool OnModdedPage;
        
        [HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.Update))]
        [HarmonyPostfix]
        private static void SwitchSettingsPagesPatch(KeyboardJoystick __instance)
        {
            if (Input.GetKeyDown(KeyCode.RightShift))
                OnModdedPage = !OnModdedPage;
        }
        
        [HarmonyPatch(typeof(IGameOptionsExtensions), nameof(IGameOptionsExtensions.ToHudString))]
        [HarmonyPrefix]
        private static bool AddInformationPatch([HarmonyArgument(0)] IGameOptions gameOptions, ref string __result)
        {
            var builder = IGameOptionsExtensions.SettingsStringBuilder;
            if (OnModdedPage)
            {
                builder.Length = 0;
                builder.AppendLine("Press <b>RightShift</b> to switch to the vanilla settings");
                builder.AppendLine();
                
                builder.AppendLine("<u>Roles:</u>");
                foreach (var option in OptionManager.CustomRoleOptions)
                {
                    builder.AppendLine(String.Format(option.HudFormat, $"{option.Role.Color.ToTextColor()}{option.Role.Name}{Utility.StringColor.Reset}",
                        gameOptions.RoleOptions.GetNumPerGame(option.Role.RoleBehaviour.Role),
                        gameOptions.RoleOptions.GetChancePerGame(option.Role.RoleBehaviour.Role)));
                    option.AdvancedOptions.Where(_option => _option.HudVisible).Do(_option => RenderOption(_option, builder, option.AdvancedOptionPrefix) );
                }
            
                OptionManager.HudVisibleOptions.Where(option => !option.IsFromPeasAPI && !option.AdvancedRoleOption).Do(option => RenderOption(option, builder) );
                
                return false;
            }
            return true;
        }
        
        [HarmonyPatch(typeof(IGameOptionsExtensions), nameof(IGameOptionsExtensions.ToHudString))]
        [HarmonyPostfix]
        private static void GameOptionsDataToHudStringPatch([HarmonyArgument(0)] IGameOptions gameOptions, ref string __result)
        {
            var builder = IGameOptionsExtensions.SettingsStringBuilder;
            if (!OnModdedPage)
            {
                var text = builder.ToString();
                builder.Clear();
                builder.AppendLine("Press <b>RightShift</b> to switch to the modded settings");
                builder.AppendLine();
                builder.AppendLine(text);
                
                OptionManager.HudVisibleOptions.Where(option => option.IsFromPeasAPI).Do(option => RenderOption(option, builder) );
            }

            __result = builder.ToString();
        }

        internal static void RenderOption(CustomOption option, StringBuilder builder, string prefix = "")
        {
            switch (option)
            {
                case CustomToggleOption _option:
                    builder.AppendLine(prefix + String.Format(_option.HudFormat, _option.Title, _option.Value ? "On" : "Off") + Utility.StringColor.Reset);
                    break;
                case CustomNumberOption _option:
                    builder.AppendLine(prefix + String.Format(_option.HudFormat, _option.Title, _option.Value, _option.SuffixType switch
                    {
                        NumberSuffixes.None => "",
                        NumberSuffixes.Multiplier => "x",
                        NumberSuffixes.Seconds => "s",
                        _ => ""
                    }) + Utility.StringColor.Reset);
                    break;
                case CustomStringOption _option:
                    builder.AppendLine(prefix + String.Format(_option.HudFormat, _option.Title, _option.StringValue) + Utility.StringColor.Reset);
                    break;
                case CustomOptionHeader _option:
                    builder.AppendLine(prefix + String.Format(_option.HudFormat, _option.Title) + Utility.StringColor.Reset);
                    break;
            }
        }

        [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
        [HarmonyPostfix]
        private static void HudManagerUpdatePatch(HudManager __instance)
        {
            if (__instance.GameSettings == null)
                return;
            
            __instance.GameSettings.fontSizeMin =
                __instance.GameSettings.fontSizeMax = 
                    __instance.GameSettings.fontSize = HudTextSize;
            
            CreateScroller(__instance);

            var bottomLeft = Camera.main.ScreenToWorldPoint(new Vector3(0, 0, 0)) - Camera.main.transform.localPosition;
            
            OptionsScroller.ContentYBounds = new FloatRange(-bottomLeft.y, Mathf.Max(-bottomLeft.y, __instance.GameSettings.renderedHeight - -bottomLeft.y + 0.02F));
        }

        //THIS BIT IS SKIDDED FROM ESSENTIALS: https://github.com/DorCoMaNdO/Reactor-Essentials
        private static void CreateScroller(HudManager hudManager)
        {
            if (OptionsScroller != null) return;

            OptionsScroller = new GameObject("OptionsScroller").AddComponent<Scroller>();
            OptionsScroller.transform.SetParent(hudManager.GameSettings.transform.parent);
            OptionsScroller.gameObject.layer = 5;

            OptionsScroller.transform.localScale = Vector3.one;
            OptionsScroller.allowX = false;
            OptionsScroller.allowY = true;
            OptionsScroller.active = true;
            OptionsScroller.velocity = new Vector2(0, 0);
            OptionsScroller.ContentYBounds = new FloatRange(0, 0);
            OptionsScroller.enabled = true;

            OptionsScroller.Inner = hudManager.GameSettings.transform;
            hudManager.GameSettings.transform.SetParent(OptionsScroller.transform);
        }

        [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
        [HarmonyPostfix]
        private static void RoleOptionInitialisePatch(AmongUsClient __instance)
        {
            if (!__instance.AmHost)
                return;
            
            foreach (var option in OptionManager.CustomRoleOptions)
            {
                GameOptionsManager.Instance.currentNormalGameOptions.RoleOptions.SetRoleRate(option.Role.RoleBehaviour.Role, option.Count, option.Chance);
            }
        }

        [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
        [HarmonyPostfix]
        private static void AmongUsClientOnPlayerJoinedPatch(AmongUsClient __instance,
            [HarmonyArgument(0)] ClientData client)
        {
            if (__instance.AmHost)
            {
                foreach (var option in OptionManager.CustomOptions)
                {
                    if (option.GetType() == typeof(CustomToggleOption))
                    {
                        Rpc<RpcUpdateSetting>.Instance.SendTo(client.Id, new RpcUpdateSetting.Data(option, ((CustomToggleOption) option).Value));
                    }
                    else if (option.GetType() == typeof(CustomNumberOption))
                    {
                        Rpc<RpcUpdateSetting>.Instance.SendTo(client.Id, new RpcUpdateSetting.Data(option, ((CustomNumberOption) option).Value));
                    }
                    else if (option.GetType() == typeof(CustomStringOption))
                    {
                        Rpc<RpcUpdateSetting>.Instance.SendTo(client.Id, new RpcUpdateSetting.Data(option, ((CustomStringOption) option).Value));
                    }
                }
            }
        }
    }
}