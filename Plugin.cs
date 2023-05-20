using BepInEx;
using UnityEngine;
using HarmonyLib;
using BepInEx.Logging;
using System.Reflection;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using System.Linq;
using System.Xml;
using Hellession;
using System.IO;

namespace EndListFactions
{
    [BepInPlugin("com.hellession.endlistfactions", "End List Factions", "1.0.0.1")]
    public class Plugin : BaseUnityPlugin
    {
        //all TT players... it probably shouldn't be a list, but hey I wanna hold my hopes up
        //actually No. This is not legit. Because the game LITERALLY forces only one TT at a time.
        //public static List<int> TraitorIndex { get; set; } = new List<int>();
        public static int TraitorIndex { get; set; }
        //probably not needed
        //Nope, it is actually needed
        public static bool IsCovenTT { get; set; }

        public static List<BasicChatListItem> GamePhaseChats { get; set; } = new List<BasicChatListItem>();

        public static Dictionary<string, Sprite> ModdedSprites { get; set; } = new Dictionary<string, Sprite>();

        public static HLSNAnimator MyAnimator { get; set; } = new HLSNAnimator()
        {
            MyDelayMode = HLSNAnimator.DelayMode.NextFrame,
            MyTimeMode = HLSNAnimator.TimeMode.Time
        };

        internal static ManualLogSource Log;
        
        private void Awake()
        {
            // Plugin startup logic
            Plugin.Log = base.Logger;

            var harmony = new Harmony("com.hellession.endlistfactions");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Logger.LogInfo($"Plugin End List Factions is loaded!");
        }
    }
    
    //END GAME SCREEN: Set TT role icon color
    [HarmonyPatch(typeof(BaseEndGamePartyMemberListItem), "SetRoleIcon")]
    class Patch
    {
        static void Postfix(EndGamePartyMemberInfo data, BaseEndGamePartyMemberListItem __instance)
        {
            Plugin.Log.LogInfo($"POSTFIX TRIGGERED ON BaseEndGamePartyMemberListItem.SetRoleIcon()!");

            Role role = GlobalServiceLocator.GameRulesService.GetGameRules().Roles[data.BeginningRoleId];
            if (__instance.RoleIconAtlas != null && role != null && role.Icons.ContainsKey("main") && __instance.RoleIconAtlas.GetSprite(role.Icons["main"]) != null)
            {
                if(Plugin.TraitorIndex == data.Position)
                {
                    __instance.RoleName.SetText("TT " + GlobalServiceLocator.GameRulesService.GetGameRules().Roles[data.BeginningRoleId].Name);
                    Color color = Plugin.IsCovenTT ? GlobalServiceLocator.GameRulesService.GetGameRules().Roles[56].Color : GlobalServiceLocator.GameRulesService.GetGameRules().Roles[15].Color;
                    __instance.PlayerIcon.color = color;
                }
            }
        }
    }

    //TRAITOR REVEALED

    [HarmonyPatch(typeof(GameService), "OnServerTraitor")]
    class Patch2
    {
        static void Postfix(BaseMessage msg, GameService __instance)
        {
            Plugin.Log.LogInfo($"POSTFIX TRIGGERED ON GameService.OnServerTraitor()!");

            bool flag = __instance.ActiveGameState != null && __instance.ActiveGameState.Me != null && __instance.ActiveGameState.Players != null && __instance.ActiveGameState.Players.Count > 0;
            if(flag)
            {
                TraitorMessage traitorMessage = (TraitorMessage)msg;
                Plugin.TraitorIndex = traitorMessage.Position;
                Plugin.IsCovenTT = __instance.ActiveGameState.GameMode.Id == 20;
            }
        }
    }

    //Game started, reset traitor
    [HarmonyPatch(typeof(GameService), "OnServerPickNames")]
    class Patch3
    {
        static void Postfix(BaseMessage msg, GameService __instance)
        {
            Plugin.Log.LogInfo($"POSTFIX TRIGGERED ON GameService.OnServerPickNames()!");
            Plugin.GamePhaseChats.Clear();
            Plugin.TraitorIndex = -1;
        }
    }

    //Someone's role is being revealed in Game Screen. Change color if TT.
    [HarmonyPatch(typeof(BaseGameSceneUIController), "RevealRole")]
    class Patch4
    {
        static void Prefix(Player player, BaseGameSceneUIController __instance)
        {
            Plugin.Log.LogInfo($"PREFIX TRIGGERED ON BaseGameSceneUIController.RevealRole()!");
            if(__instance.WhoDiedAndHowPanel != null && __instance.WhoDiedAndHowPanel.RoleImage != null)
            {
                if(player.IsRoleKnown)
                {
                    if (player.Tags.IsSet(PlayerTag.Traitor))
                    {
                        Color color = GlobalServiceLocator.GameRulesService.GetGameRules().Roles[15].Color;
                        if (GlobalServiceLocator.GameService.ActiveGameState.GameMode.Id == 20)
                            color = GlobalServiceLocator.GameRulesService.GetGameRules().Roles[56].Color;
                        __instance.WhoDiedAndHowPanel.RoleImage.color = color;
                    }
                    else
                        __instance.WhoDiedAndHowPanel.RoleImage.color = Color.white;
                }
                else
                    __instance.WhoDiedAndHowPanel.RoleImage.color = Color.white;
            }
        }
    }

    //Someone has won. If TT gamemode, TT unknown and Coven/Maf won, scan all winners for a Town role, set them to TT.
    [HarmonyPatch(typeof(GameService), "OnServerSomeoneHasWon")]
    class PatchSomeoneHasWon
    {
        static void Postfix(BaseMessage msg, GameService __instance)
        {
            Plugin.Log.LogInfo($"POSTFIX TRIGGERED ON GameService.OnServerSomeoneHasWon()!");

            bool flag = __instance.ActiveGameState != null && __instance.ActiveGameState.Me != null && __instance.ActiveGameState.Players != null && __instance.ActiveGameState.Players.Count > 0;
            if (flag)
            {
                if((__instance.ActiveGameState.GameMode.Id == 20 || __instance.ActiveGameState.GameMode.Id == 21) && Plugin.TraitorIndex == -1)
                {
                    //it's a TT game, but the TT is still unknown. Try digging through every winning player to see if they are meant to be Town.
                    SomeoneHasWonMessage someoneHasWonMessage = (SomeoneHasWonMessage)msg;
                    if (someoneHasWonMessage != null && someoneHasWonMessage.WinningPositions != null && someoneHasWonMessage.WinningPositions.Count > 0)
                    {
                        if(someoneHasWonMessage.WinningFaction != 1)
                        {
                            //Town didn't win
                            foreach(var kv in someoneHasWonMessage.WinningPositions)
                            {
                                //are winning positions zero-based indices or not? They appear to be zero-based...
                                
                                //CurrentRole is a Role object. From what I've gathered, Role objects should be derived from GameRules? I guess?
                                //..and if that's the case, GameRules should have the Faction for each Role that they're in *by default*... so check that?
                                if(__instance.ActiveGameState.Players[kv].CurrentRole.Faction == 1)
                                {
                                    //This is probably TT.
                                    Plugin.Log.LogInfo($"Winning player of index {kv} has Faction 1, when TT wasn't discovered, but Town lost. This means that they are probably TT... setting.");
                                    //this sounds stupid... why just not add 1 to the index? whatever...
                                    Plugin.TraitorIndex = __instance.ActiveGameState.Players[kv].Position;
                                    break;
                                }
                            }
                        }
                    }
                    else
                        Plugin.Log.LogInfo($"No one won??? WinningPositions = null or someonehasWonMessage = null.");
                }
            }
        }
    }

    //Someone has won. If TT gamemode, TT unknown and Coven/Maf won, scan all winners for a Town role, set them to TT.
    [HarmonyPatch(typeof(ChatLogController), nameof(ChatLogController.AddMessage), new Type[]{ typeof(ChatMessage), typeof(int)})]
    class PatchAddMessage
    {
        static bool Prefix(ChatMessage chatMessage, int itemRendererIndex, ChatLogController __instance, float ___scrollBarZero, ref bool ___m_someChatHidden)
        {
            Plugin.Log.LogInfo($"PREFIX TRIGGERED at ChatLogController.AddMessage()");
            if (__instance.chatList == null)
            {
                return false;
            }
            if (__instance.ListItemRenderers.Length != 0 && itemRendererIndex >= 0 && itemRendererIndex < __instance.ListItemRenderers.Length)
            {
                BasicChatListItem basicChatListItem = UnityEngine.Object.Instantiate<BasicChatListItem>(__instance.ListItemRenderers[itemRendererIndex]);
                //attempt to determine if it is a day / Night indicator
                string dayVal = GetLocalizedString("GUI_DAY_NUMBER");
                string nightVal = GetLocalizedString("GUI_NIGHT_NUMBER");
                bool isDayNightIndicator = false;
                if(string.IsNullOrEmpty(chatMessage.Sender) && (chatMessage.Text.StartsWith(dayVal) || chatMessage.Text.StartsWith(nightVal)) && chatMessage.Text.Length < 15 && int.TryParse(chatMessage.Text.Split(' ').Last(), out _))
                {
                    Plugin.Log.LogInfo($"Detected chat message of day night indicator. The style of the message is Bold: {chatMessage.Style.Bold}, Background color: {chatMessage.Style.BackgroundColor}, Sender Color: {chatMessage.Style.SenderColor}");
                    //ok yea I am pretty confident it's a day / night indicator
                    isDayNightIndicator = true;
                    Plugin.GamePhaseChats.Add(basicChatListItem);
                }
                //...very lazily
                __instance.chatList.AddListItem(basicChatListItem, chatMessage);
                if (__instance.chatScrollBar.value > ___scrollBarZero)
                {
                    basicChatListItem.gameObject.SetActive(false);
                    ___m_someChatHidden = true;
                }
                if (__instance.PositionFilter > 0)
                {
                    if (((BaseGameChatListItem)basicChatListItem).PositionNumber != __instance.PositionFilter)
                    {
                        if (__instance.FilterToggle.isOn && !isDayNightIndicator)
                        {
                            basicChatListItem.gameObject.SetActive(false);
                        }
                    }
                    else if (!__instance.FilterToggle.isOn)
                    {
                        ((BaseGameChatListItem)basicChatListItem).Highlight.SetActive(true);
                    }
                }
                BasicChatListItem listItem = UnityEngine.Object.Instantiate<BasicChatListItem>(__instance.ListItemRenderers[itemRendererIndex]);
                __instance.nonScrollableChatList.AddListItem(listItem, chatMessage);
                __instance.nonScrollableScrollBar.value = 0f;
                return false;
            }
            return false;
        }
        
        static string GetLocalizedString(string key)
        {
            if (GlobalServiceLocator.LocalizationService != null)
            {
                return GlobalServiceLocator.LocalizationService.GetLocalizedString(key);
            }
            return key;
        }
    }

    //If someone's messages are filtered, show Night / Day phases too
    [HarmonyPatch(typeof(ChatLogController), "ApplyFilter")]
    class PatchChatFilters
    {
        static bool Prefix(ChatLogController __instance)
        {
            Plugin.Log.LogInfo($"PREFIX TRIGGERED at ChatLogController.ApplyFilter()");
            bool isOn = __instance.FilterToggle.isOn;
            BaseGameChatListItem pastItem = null;
            int pastPhaseChats = 0;
            foreach (ListItem<ChatMessage> listItem in __instance.chatList.ListItems)
            {
                BaseGameChatListItem chatListItem = (BaseGameChatListItem)listItem;
                if (isOn)
                {
                    listItem.gameObject.SetActive(chatListItem.PositionNumber == __instance.PositionFilter || Plugin.GamePhaseChats.Contains(chatListItem));
                    ((BaseGameChatListItem)listItem).Highlight.SetActive(false);
                    if(Plugin.GamePhaseChats.Contains(chatListItem))
                    {
                        Plugin.Log.LogDebug($"GamePhaseChat text is: {chatListItem.TextField.text}");
                        if(((BaseGameChatListItem)listItem).TextField.text.StartsWith("Day "))
                        {
                            //the pastItem is 
                            pastItem.gameObject.SetActive(pastPhaseChats > 0);
                        }
                        pastItem = (BaseGameChatListItem)listItem;
                        pastPhaseChats = 0;
                    }
                    else if(listItem.gameObject.activeSelf)
                    {
                        //I would use GameObject.activeInHierarchy preferrably, but I am not sure that it is active in hierarchy because BMG code could have some other weirdness
                        pastPhaseChats++;
                    }
                }
                else
                {
                    listItem.gameObject.SetActive(true);
                    ((BaseGameChatListItem)listItem).Highlight.SetActive(((BaseGameChatListItem)listItem).PositionNumber == __instance.PositionFilter);
                }
            }
            try
            {
                //I doubt this would work
                Traverse.Create(__instance).Method("ScrollToBottom").GetValue();
                //__instance.ScrollToBottom();
            }
            catch(Exception e)
            {
                Plugin.Log.LogError($"TRAVERSE FAILED: {e}");
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(GameRulesService), nameof(GameRulesService.Load))]
    class PatchExtractGameRules
    {
        static void Prefix(XmlDocument xmlData)
        {
            Plugin.Log.LogInfo($"PREFIX TRIGGERED at GameRulesService.Load(). Extracting XML...");
            xmlData.Save(Application.dataPath + "/Hellession/GameRules.xml");
        }
    }
}
