using AmongUs.GameOptions;
using HarmonyLib;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using UnhollowerBaseLib;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
internal class HudManagerPatch
{
    public static bool ShowDebugText = false;
    public static int LastCallNotifyRolesPerSecond = 0;
    public static int NowCallNotifyRolesCount = 0;
    public static int LastSetNameDesyncCount = 0;
    public static int LastFPS = 0;
    public static int NowFrameCount = 0;
    public static float FrameRateTimer = 0.0f;
    public static TMPro.TextMeshPro LowerInfoText;
    public static void Postfix(HudManager __instance)
    {
        if (!GameStates.IsModHost) return;
        var player = PlayerControl.LocalPlayer;
        if (player == null) return;
        var TaskTextPrefix = "";
        var FakeTasksText = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.FakeTasks, new Il2CppReferenceArray<Il2CppSystem.Object>(0));
        //壁抜け
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            if ((!AmongUsClient.Instance.IsGameStarted || !GameStates.IsOnlineGame)
                && player.CanMove)
            {
                player.Collider.offset = new Vector2(0f, 127f);
            }
        }
        //壁抜け解除
        if (player.Collider.offset.y == 127f)
        {
            if (!Input.GetKey(KeyCode.LeftControl) || (AmongUsClient.Instance.IsGameStarted && GameStates.IsOnlineGame))
            {
                player.Collider.offset = new Vector2(0f, -0.3636f);
            }
        }
        if (GameStates.IsLobby)
        {
            var POM = GameObject.Find("PlayerOptionsMenu(Clone)");
            __instance.GameSettings.text = POM != null ? "" : OptionShower.GetText();
            __instance.GameSettings.fontSizeMin =
            __instance.GameSettings.fontSizeMax = 1f;
        }
        //ゲーム中でなければ以下は実行されない
        if (!AmongUsClient.Instance.IsGameStarted) return;

        Utils.CountAlivePlayers();

        if (SetHudActivePatch.IsActive)
        {
            if (player.IsAlive())
            {
                //MOD入り用のボタン下テキスト変更
                switch (player.GetCustomRole())
                {
                    case CustomRoles.Sniper:
                        Sniper.OverrideShapeText(player.PlayerId);
                        break;
                    case CustomRoles.FireWorks:
                        if (FireWorks.nowFireWorksCount.ContainsKey(player.PlayerId) && FireWorks.nowFireWorksCount[player.PlayerId] == 0)
                            __instance.AbilityButton.OverrideText($"{GetString("FireWorksExplosionButtonText")}");
                        else
                            __instance.AbilityButton.OverrideText($"{GetString("FireWorksInstallAtionButtonText")}");
                        break;
                    case CustomRoles.SerialKiller:
                        SerialKiller.GetAbilityButtonText(__instance, player);
                        break;
                    case CustomRoles.Warlock:
                        if (!(Main.CheckShapeshift.TryGetValue(player.PlayerId, out bool shapeshifting) && shapeshifting) && !(Main.isCurseAndKill.TryGetValue(player.PlayerId, out bool curse) && curse))
                        {
                            __instance.KillButton.OverrideText($"{GetString("WarlockCurseButtonText")}");
                        }
                        else
                        {
                            __instance.KillButton.OverrideText($"{GetString("KillButtonText")}");
                        }
                        break;
                    case CustomRoles.Assassin:
                        if (Main.CheckShapeshift.ContainsKey(player.PlayerId) && !Main.CheckShapeshift[player.PlayerId] && !Main.isMarkAndKill[player.PlayerId])
                        {
                            __instance.KillButton.OverrideText($"{GetString("AssassinMarkButtonText")}");
                        }
                        else
                        {
                            __instance.KillButton.OverrideText($"{GetString("KillButtonText")}");
                        }
                        break;
                    case CustomRoles.Miner:
                        __instance.AbilityButton.OverrideText($"{GetString("MinerTeleButtonText")}");
                        break;
                    case CustomRoles.Witch:
                        Witch.GetAbilityButtonText(__instance);
                        break;
                    case CustomRoles.Vampire:
                        Vampire.SetKillButtonText();
                        break;
                    case CustomRoles.Arsonist:
                        __instance.KillButton.OverrideText($"{GetString("ArsonistDouseButtonText")}");
                        break;
                    case CustomRoles.Revolutionist:
                        __instance.KillButton.OverrideText($"{GetString("RevolutionistDrawButtonText")}");
                        break;
                    case CustomRoles.Puppeteer:
                        __instance.KillButton.OverrideText($"{GetString("PuppeteerOperateButtonText")}");
                        break;
                    case CustomRoles.BountyHunter:
                        BountyHunter.SetAbilityButtonText(__instance);
                        break;
                    case CustomRoles.EvilTracker:
                        EvilTracker.GetAbilityButtonText(__instance, player.PlayerId);
                        break;
                    case CustomRoles.Innocent:
                        __instance.KillButton.OverrideText($"{GetString("InnocentButtonText")}");
                        break;
                    case CustomRoles.Capitalism:
                        __instance.KillButton.OverrideText($"{GetString("CapitalismButtonText")}");
                        break;
                    case CustomRoles.Pelican:
                        __instance.KillButton.OverrideText($"{GetString("PelicanButtonText")}");
                        break;
                    case CustomRoles.Counterfeiter:
                        __instance.KillButton.OverrideText($"{GetString("CounterfeiterButtonText")}");
                        break;
                    case CustomRoles.Gangster:
                        Gangster.SetKillButtonText(player.PlayerId);
                        break;
                    case CustomRoles.FFF:
                        __instance.KillButton.OverrideText($"{GetString("FFFButtonText")}");
                        break;
                    case CustomRoles.Medicaler:
                        __instance.KillButton.OverrideText($"{GetString("MedicalerButtonText")}");
                        break;
                    case CustomRoles.Gamer:
                        __instance.KillButton.OverrideText($"{GetString("GamerButtonText")}");
                        break;
                }

                //バウンティハンターのターゲットテキスト
                if (LowerInfoText == null)
                {
                    LowerInfoText = Object.Instantiate(__instance.KillButton.buttonLabelText);
                    LowerInfoText.transform.parent = __instance.transform;
                    LowerInfoText.transform.localPosition = new Vector3(0, -2f, 0);
                    LowerInfoText.alignment = TMPro.TextAlignmentOptions.Center;
                    LowerInfoText.overflowMode = TMPro.TextOverflowModes.Overflow;
                    LowerInfoText.enableWordWrapping = false;
                    LowerInfoText.color = Palette.EnabledColor;
                    LowerInfoText.fontSizeMin = 2.0f;
                    LowerInfoText.fontSizeMax = 2.0f;
                }

                if (player.Is(CustomRoles.BountyHunter))
                {
                    LowerInfoText.text = BountyHunter.GetTargetText(player, true);
                }
                else if (player.Is(CustomRoles.Witch))
                {
                    //魔女用処理
                    LowerInfoText.text = Witch.GetSpellModeText(player, true);
                    LowerInfoText.enabled = true;
                }
                else if (player.Is(CustomRoles.FireWorks))
                {
                    var stateText = FireWorks.GetStateText(player);
                    LowerInfoText.text = stateText;
                    LowerInfoText.enabled = true;
                }
                else
                {
                    LowerInfoText.text = "";
                }
                LowerInfoText.enabled = LowerInfoText.text != "";
                if (!AmongUsClient.Instance.IsGameStarted && AmongUsClient.Instance.NetworkMode != NetworkModes.FreePlay)
                {
                    LowerInfoText.enabled = false;
                }

                if (player.CanUseKillButton())
                {
                    __instance.KillButton.ToggleVisible(player.IsAlive() && GameStates.IsInTask);
                    player.Data.Role.CanUseKillButton = true;
                }
                else
                {
                    __instance.KillButton.SetDisabled();
                    __instance.KillButton.ToggleVisible(false);
                }
                switch (player.GetCustomRole())
                {
                    case CustomRoles.Jester:
                        TaskTextPrefix += GetString(StringNames.FakeTasks);
                        break;
                }

                bool CanUseVent = player.CanUseImpostorVentButton();
                __instance.ImpostorVentButton.ToggleVisible(CanUseVent);
                player.Data.Role.CanVent = CanUseVent;
            }
            else
            {
                __instance.ReportButton.Hide();
                __instance.ImpostorVentButton.Hide();
                __instance.KillButton.Hide();
                __instance.AbilityButton.Show();
                __instance.AbilityButton.OverrideText(GetString(StringNames.HauntAbilityName));
            }
        }


        if (Input.GetKeyDown(KeyCode.Y) && AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay)
        {
            __instance.ToggleMapVisible(new MapOptions()
            {
                Mode = MapOptions.Modes.Sabotage,
                AllowMovementWhileMapOpen = true
            });
            if (player.AmOwner)
            {
                player.MyPhysics.inputHandler.enabled = true;
                ConsoleJoystick.SetMode_Task();
            }
        }

        if (AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame) RepairSender.enabled = false;
        if (Input.GetKeyDown(KeyCode.RightShift) && AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
        {
            RepairSender.enabled = !RepairSender.enabled;
            RepairSender.Reset();
        }
        if (RepairSender.enabled && AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0)) RepairSender.Input(0);
            if (Input.GetKeyDown(KeyCode.Alpha1)) RepairSender.Input(1);
            if (Input.GetKeyDown(KeyCode.Alpha2)) RepairSender.Input(2);
            if (Input.GetKeyDown(KeyCode.Alpha3)) RepairSender.Input(3);
            if (Input.GetKeyDown(KeyCode.Alpha4)) RepairSender.Input(4);
            if (Input.GetKeyDown(KeyCode.Alpha5)) RepairSender.Input(5);
            if (Input.GetKeyDown(KeyCode.Alpha6)) RepairSender.Input(6);
            if (Input.GetKeyDown(KeyCode.Alpha7)) RepairSender.Input(7);
            if (Input.GetKeyDown(KeyCode.Alpha8)) RepairSender.Input(8);
            if (Input.GetKeyDown(KeyCode.Alpha9)) RepairSender.Input(9);
            if (Input.GetKeyDown(KeyCode.Return)) RepairSender.InputEnter();
        }
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ToggleHighlight))]
internal class ToggleHighlightPatch
{
    public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] bool active, [HarmonyArgument(1)] RoleTeamTypes team)
    {
        var player = PlayerControl.LocalPlayer;
        if (!GameStates.IsInTask) return;

        if (player.CanUseKillButton())
        {
            __instance.cosmetics.currentBodySprite.BodySprite.material.SetColor("_OutlineColor", Utils.GetRoleColor(player.GetCustomRole()));
        }
    }
}
[HarmonyPatch(typeof(Vent), nameof(Vent.SetOutline))]
internal class SetVentOutlinePatch
{
    public static void Postfix(Vent __instance, [HarmonyArgument(1)] ref bool mainTarget)
    {
        var player = PlayerControl.LocalPlayer;
        Color color = PlayerControl.LocalPlayer.GetRoleColor();
        __instance.myRend.material.SetColor("_OutlineColor", color);
        __instance.myRend.material.SetColor("_AddColor", mainTarget ? color : Color.clear);
    }
}
[HarmonyPatch(typeof(HudManager), nameof(HudManager.SetHudActive), new System.Type[] { typeof(PlayerControl), typeof(RoleBehaviour), typeof(bool) })]
internal class SetHudActivePatch
{
    public static bool IsActive = false;
    public static void Postfix(HudManager __instance, [HarmonyArgument(2)] bool isActive)
    {
        __instance.ReportButton.ToggleVisible(!GameStates.IsLobby && isActive);
        if (!GameStates.IsModHost) return;
        IsActive = isActive;
        if (!isActive) return;

        var player = PlayerControl.LocalPlayer;
        if (player == null) return;
        switch (player.GetCustomRole())
        {
            case CustomRoles.Sheriff:
            case CustomRoles.SwordsMan:
            case CustomRoles.Arsonist:
            case CustomRoles.Innocent:
            case CustomRoles.Pelican:
            case CustomRoles.Revolutionist:
            case CustomRoles.FFF:
            case CustomRoles.Medicaler:
            case CustomRoles.Gamer:
                __instance.SabotageButton.ToggleVisible(false);
                __instance.AbilityButton.ToggleVisible(false);
                break;
            case CustomRoles.Minimalism:
                __instance.SabotageButton.ToggleVisible(false);
                __instance.AbilityButton.ToggleVisible(false);
                __instance.ReportButton.ToggleVisible(false);
                break;
            case CustomRoles.Jackal:
                Jackal.SetHudActive(__instance, isActive);
                break;
        }

        foreach (var subRole in Main.PlayerStates[player.PlayerId].SubRoles)
        {
            switch (subRole)
            {
                case CustomRoles.Oblivious:
                    __instance.ReportButton.ToggleVisible(false);
                    break;
            }
        }

        __instance.KillButton.ToggleVisible(player.CanUseKillButton());
        __instance.ImpostorVentButton.ToggleVisible(player.CanUseImpostorVentButton());
    }
}
[HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.Show))]
internal class MapBehaviourShowPatch
{
    public static void Prefix(MapBehaviour __instance, ref MapOptions opts)
    {
        if (GameStates.IsMeeting) return;

        if (opts.Mode is MapOptions.Modes.Normal or MapOptions.Modes.Sabotage)
        {
            var player = PlayerControl.LocalPlayer;
            opts.Mode = player.Is(CustomRoleTypes.Impostor) || (player.Is(CustomRoles.Jackal) && Jackal.CanUseSabotage.GetBool())
                ? MapOptions.Modes.Sabotage
                : MapOptions.Modes.Normal;
        }
    }
}
[HarmonyPatch(typeof(TaskPanelBehaviour), nameof(TaskPanelBehaviour.SetTaskText))]
internal class TaskPanelBehaviourPatch
{
    // タスク表示の文章が更新・適用された後に実行される
    public static void Postfix(TaskPanelBehaviour __instance)
    {
        if (!GameStates.IsModHost) return;
        PlayerControl player = PlayerControl.LocalPlayer;

        // 役職説明表示
        if (!player.GetCustomRole().IsVanilla())
        {
            var RoleWithInfo = $"{player.GetDisplayRoleName()}:\r\n";
            RoleWithInfo += player.GetRoleInfo();
            __instance.taskText.text = Utils.ColorString(player.GetRoleColor(), RoleWithInfo) + "\n" + __instance.taskText.text;
        }

        // RepairSenderの表示
        if (RepairSender.enabled && AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
        {
            __instance.taskText.text = RepairSender.GetText();
        }
    }
}

internal class RepairSender
{
    public static bool enabled = false;
    public static bool TypingAmount = false;

    public static int SystemType;
    public static int amount;

    public static void Input(int num)
    {
        if (!TypingAmount)
        {
            //SystemType入力中
            SystemType *= 10;
            SystemType += num;
        }
        else
        {
            //Amount入力中
            amount *= 10;
            amount += num;
        }
    }
    public static void InputEnter()
    {
        if (!TypingAmount)
        {
            //SystemType入力中
            TypingAmount = true;
        }
        else
        {
            //Amount入力中
            Send();
        }
    }
    public static void Send()
    {
        ShipStatus.Instance.RpcRepairSystem((SystemTypes)SystemType, amount);
        Reset();
    }
    public static void Reset()
    {
        TypingAmount = false;
        SystemType = 0;
        amount = 0;
    }
    public static string GetText()
    {
        return SystemType.ToString() + "(" + ((SystemTypes)SystemType).ToString() + ")\r\n" + amount;
    }
}