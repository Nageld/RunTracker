﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BazaarGameClient.Domain.Models;
using BazaarGameClient.Domain.Models.Cards;
using BazaarGameShared.Domain.Cards;
using BepInEx;
using BepInEx.Unity.Mono;
using HarmonyLib;
using TheBazaar;
using TheBazaar.UI.EndOfRun;
using UnityEngine;
using System.Net.Http;
using System.Text;
using BazaarGameShared.Domain.Core.Types;
using System.Threading.Tasks;
using BepInEx.Logging;
using BazaarGameClient.Infra.SocketClient;
using TheBazaar.ProfileData;

namespace RunTracker;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private readonly Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
    public static String CharacterName = "";
    internal static new ManualLogSource Logger;
    public static bool? isAbandonned = null;

    private void Awake()
    {
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        harmony.PatchAll();
    }

    private void Start()
    {
    }

    [HarmonyPatch(typeof(SocketClient), "SendAbandonRun")]
    class AbandonRun
    {
        [HarmonyPrefix]
        static void Prefix()
        {
            isAbandonned = true;
        }
    }

    [HarmonyPatch(typeof(EndOfRunSummaryController), "OnContinueButtonPressed")]
    class Chests
    {
        [HarmonyPrefix]
        static void Prefix()
        {
            string outputDir = Path.Combine(Paths.PluginPath, "RunImages");
            Directory.CreateDirectory(outputDir);
            long unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            ScreenCapture.CaptureScreenshot(Path.Combine(outputDir, $"{unixTimestamp}_{CharacterName}.png"),2);
        }
    }

    [HarmonyPatch(typeof(EndOfRunSummaryController), "Init")]
    class EndRun
    {
        static bool should_send_request = false;
        [HarmonyPostfix]
        static void Postfix(Player player)
        {
            RunInfo runInfo = new RunInfo();
            runInfo.GameModeId = Data.SelectedPlayMode.ToString();
            runInfo.Wins = Data.Run.Victories;
            runInfo.Character = Data.Run.Player.Hero.ToString();
            CharacterName = Data.Run.Player.Hero.ToString();
            runInfo.Health = (int)Data.Run.Player.Attributes[EPlayerAttributeType.Health];
            runInfo.Level = (int)Data.Run.Player.Attributes[EPlayerAttributeType.Level];
            runInfo.Day = (int)Data.Run.Day;
            runInfo.Rating = Data.Rank.CurrentSeasonRank.Rating;
            runInfo.IsAbandoned = isAbandonned;
            isAbandonned = null;//reset
            List<ICard> HandItems = player.Hand.GetItems();
            runInfo.Cards = new List<RunInfo.CardInfo>();
            foreach (var card in HandItems)
            {
                RunInfo.CardInfo info = new RunInfo.CardInfo
                {
                    TemplateId = card.TemplateId,
                    Tier = card.Tier
                };
                runInfo.Cards.Add(info);
            }
            List<ICard> StashItems = player.Stash.GetItems();
            runInfo.Stash = new List<RunInfo.CardInfo>();
            foreach (var card in StashItems)
            {
                RunInfo.CardInfo info = new RunInfo.CardInfo
                {
                    TemplateId = card.TemplateId,
                    Tier = card.Tier
                };
                ((IList)runInfo.Stash).Add(info);
            }
            runInfo.Skills = new List<RunInfo.SkillInfo>();

            foreach (SkillCard skill in Data.Run.Player.Skills)
            {
                RunInfo.SkillInfo info = new RunInfo.SkillInfo
                {
                    TemplateId = skill.TemplateId,
                    Tier = skill.Tier
                };
                ((IList)runInfo.Skills).Add(info);
            }
            string outputDir = Path.Combine(Paths.PluginPath, "RunData");
            Directory.CreateDirectory(outputDir);
            long unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string filename = $"{unixTimestamp}.json";
            string filePath = Path.Combine(outputDir, filename);
            string runInfoJson = RunInfo.Serializer.SerializeToJson(runInfo);
            File.WriteAllText(filePath, runInfoJson);
            if(should_send_request)
            {
                _ = PostRunInfo(runInfoJson);
            }
            //Logger.LogWarning(runInfoJson);
        }
    }
    static async Task PostRunInfo(string jsonContent)
    {
        using (HttpClient client = new HttpClient())
        {
            try
            {
                string url = "http://localhost/api/postruninfo"; //modified to localhost to not disclose the server IP

                // Create the JSON content to send in the request
                StringContent content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Send the POST request
                HttpResponseMessage response = await client.PostAsync(url, content);

                // Check if the request was successful
                if (response.IsSuccessStatusCode)
                {
                    Plugin.Logger.LogInfo("Request succeeded!");
                }
                else
                {
                    Plugin.Logger.LogError($"Request failed. Status code: {response.StatusCode}");
                    string responseBody = await response.Content.ReadAsStringAsync();
                    Plugin.Logger.LogError($"Response body: {responseBody}");
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                Plugin.Logger.LogError($"Error sending request: {ex.Message}");
            }
        }
    }
}
