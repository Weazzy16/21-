using GTANetworkAPI;
using NeptuneEvo.Handles;
using System;
using System.Collections.Generic;

namespace NeptuneEvo.Chars.Models
{
    public enum wComponentsType
    {
        Invalid = 0,
        Clip, //0 патроны
        Clip2, //1 б патроны
        Suppressor, //2 глушитель
        Scope,//3 прицел 1 
        Scope2,//4 прицел 2
        Flashlight,//5 фонарик
        Grip,//6 рукоятка
        
    }
    public class wComponentsData
    {
        /// <summary>
        /// Колличество
        /// </summary>
        public int Count { get; set; }
        //Список компонентов
        public Dictionary<uint, wComponentData> Components { get; set; }

        public wComponentsData(int Count, Dictionary<uint, wComponentData> Components)
        {
            this.Count = Count;
            this.Components = Components;
        }
    }
    public class wComponentData
    {
        public string Name { get; set; }
        public string Desc { get; set; }
        public int Price { get; set; }
        public wComponentsType Type { get; set; }

        public wComponentData(string Name, string Desc, int Price, wComponentsType Type)
        {
            this.Name = Name;
            this.Desc = Desc;
            this.Price = Price;
            this.Type = Type;
        }
    }
}
