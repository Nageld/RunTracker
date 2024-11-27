using System;
using System.Collections.Generic;
using BazaarBattleService.Models;
using BazaarGameShared.Domain.Cards;
using BazaarGameShared.Domain.Core.Types;
using Newtonsoft.Json;
using UnityEngine;

namespace RunTracker;

public class RunInfo
{
    public string GameModeId;
    public String Character; 
    public List<SkillInfo> Skills;
    public List<CardInfo> Cards; 
    public List<CardInfo> Stash; 
    public uint Wins;
    public String Trophy;
    public int Day;
    public int Level;
    public double Health; //might not be useful to be double instead of int, would need to check
    
    
    public class SkillInfo
    {
        public ETier Tier;
        public Guid TemplateId;
    }
    public class CardInfo
    {
        public ETier Tier;
        public String Name;
        public Guid TemplateId;
    }
    
    public class Serializer
    {
        public string SerializeToJson(RunInfo runInfo)
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented, 
                NullValueHandling = NullValueHandling.Ignore 
            };
            return JsonConvert.SerializeObject(runInfo, settings);
        }

        public static RunInfo DeserializeFromJson(string json)
        {
            return JsonConvert.DeserializeObject<RunInfo>(json);
        }
    }
}