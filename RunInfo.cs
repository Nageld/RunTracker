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
    public int Health;
    public bool? IsAbandoned;
    public int Rating;
    
    
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
    
    public static class Serializer
    {
        public static string SerializeToJson(object toconvert)//no need to have it as RunInfo type, here we can use it as anything
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented, 
                NullValueHandling = NullValueHandling.Ignore 
            };
            return JsonConvert.SerializeObject(toconvert, settings);
        }

        public static RunInfo DeserializeFromJson(string json)
        {
            return JsonConvert.DeserializeObject<RunInfo>(json);
        }
    }
}