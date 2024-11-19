using System;
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

namespace RunTracker;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private readonly Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
    public static String CharacterName = "";
    private void Awake()
    {
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        harmony.PatchAll();
    }

    private void Start()
    {
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
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(outputDir, $"{CharacterName}_{unixTimestamp}.png"),
                2);

        }
    }

    [HarmonyPatch(typeof(EndOfRunSummaryController), "Init")]
    class EndRun
    {
        [HarmonyPostfix]
        static void Postfix(Player player)
        {
            RunInfo runInfo = new RunInfo();
            runInfo.Wins = Data.Run.Victories;
            runInfo.Character = Data.Run.Player.Hero.ToString();
            CharacterName = Data.Run.Player.Hero.ToString();
            List<ICard> temp = player.Hand.GetItems();
            runInfo.Cards = new List<RunInfo.CardInfo>();
            runInfo.Day = (int)Data.Run.Day;
            foreach (var card in temp)
            {
                RunInfo.CardInfo info = new RunInfo.CardInfo
                {
                    TemplateId = card.TemplateId,
                    Tier = card.Tier
                };
                runInfo.Cards.Add(info);
            }
            List<ICard> temp2 = player.Stash.GetItems();
            runInfo.Stash = new List<RunInfo.CardInfo>();
            foreach (var card in temp2)
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

            RunInfo.Serializer test = new RunInfo.Serializer();
            long unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string filename = $"{unixTimestamp}.json";
            string filePath = Path.Combine(outputDir, filename);
            File.WriteAllText(filePath, test.SerializeToJson(runInfo));
        }
    }
}
