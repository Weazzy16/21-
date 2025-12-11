using Database;
using GTANetworkAPI;
using LinqToDB;
using Localization;
using MySqlConnector;
using NeptuneEvo.Accounts;
using NeptuneEvo.Achievements;
using NeptuneEvo.AleSystems;
using NeptuneEvo.Character;
using NeptuneEvo.Character.Models;
using NeptuneEvo.Chars.Models;
using NeptuneEvo.Core;
using NeptuneEvo.Events;
using NeptuneEvo.Fractions;
using NeptuneEvo.Fractions.Models;
using NeptuneEvo.Fractions.Player;
using NeptuneEvo.Functions;
using NeptuneEvo.GUI;
using NeptuneEvo.Handles;
using NeptuneEvo.Handles;                 // чтобы видеть методы-расширения
using NeptuneEvo.Houses;
using NeptuneEvo.MoneySystem;
using NeptuneEvo.Organizations.Models;
using NeptuneEvo.Organizations.Player;
using NeptuneEvo.Players;
using NeptuneEvo.Players.Models;
using NeptuneEvo.Players.Phone.Messages.Models;
using NeptuneEvo.Players.Popup.List.Models;
using NeptuneEvo.Quests;
using NeptuneEvo.VehicleData.LocalData;
using NeptuneEvo.VehicleData.LocalData.Models;
using NeptuneEvo.VehicleModel;
using Newtonsoft.Json;
using Redage.SDK;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Google.Protobuf.Reflection.SourceCodeInfo.Types;
using static LinqToDB.SqlQuery.SqlPredicate;
using static NeptuneEvo.Core.ReportNew;

namespace NeptuneEvo.Chars
{
    /// <summary>
    /// Работа с данными персонажа
    /// </summary>
    class Repository : Script
    {
            
    // ✅ МАКСИМАЛЬНЫЙ ВЕС ИНВЕНТАРЯ (В КИЛОГРАММАХ)
    public const float MaxInventoryWeight = 40.0f;      // Основной инвентарь
        public const float MaxBackpackWeight = 25.0f;       // Рюкзак
        public const float MaxVehicleWeight = 150.0f;       // Багажник машины
        public const float MaxWarehouseWeight = 1000.0f;    // Склад
        /// <summary>
        /// Логгер
        /// </summary>
        private static readonly nLog Log = new nLog("Chars.Repository");

        [ServerEvent(Event.PlayerDeath)]
        public void onPlayerDeathHandler(ExtPlayer player, ExtPlayer entityKiller, uint weapon)
        {
            try
            {
                RouletteClose(player);
                ChangeAutoNumberClear(player);
            }
            catch (Exception e)
            {
                Log.Write($"onPlayerDeathHandler Exception: {e.ToString()}");
            }
        }


        #region Инвентарь
        public static ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentDictionary<int, InventoryItemData>>> ItemsData = new ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentDictionary<int, InventoryItemData>>>();

        public static ConcurrentDictionary<string, List<ExtPlayer>> InventoryOtherPlayers = new ConcurrentDictionary<string, List<ExtPlayer>>();

        public static IReadOnlyDictionary<int, ItemId> AccessoriesInfo = new Dictionary<int, ItemId>()
        {
            { 0, ItemId.Hat },
            { 1, ItemId.Mask },
            { 2, ItemId.Ears },
            { 3, ItemId.Glasses },
            { 4, ItemId.Jewelry },
            { 5, ItemId.Top },
            { 6, ItemId.Undershit },
            { 7, ItemId.BodyArmor },
            { 8, ItemId.Bag },
            { 9, ItemId.Leg },
            { 10, ItemId.Bracelets },
            { 11, ItemId.Watches },
            { 12, ItemId.Gloves },
            { 13, ItemId.Feet },
            { 14, ItemId.Decals },
        };

        public static IReadOnlyDictionary<ClothesComponent, ClothesComponentId> ClothesComponentToPropId = new Dictionary<ClothesComponent, ClothesComponentId>()
        {
            { ClothesComponent.Hat, new ClothesComponentId(0, ItemId.Hat, 0) },
            { ClothesComponent.Ears, new ClothesComponentId(2, ItemId.Ears, 2) },
            { ClothesComponent.Glasses, new ClothesComponentId(1, ItemId.Glasses, 3) },
            { ClothesComponent.Bracelets, new ClothesComponentId(7, ItemId.Bracelets, 10) },
            { ClothesComponent.Watches, new ClothesComponentId(6, ItemId.Watches, 11) },
        };


        public static IReadOnlyDictionary<ClothesComponent, ClothesComponentId> ClothesComponentToComponentId = new Dictionary<ClothesComponent, ClothesComponentId>()
        {
            { ClothesComponent.Masks, new ClothesComponentId(1, ItemId.Mask, 1) },
            { ClothesComponent.Accessories, new ClothesComponentId(7, ItemId.Jewelry, 4) },//
            { ClothesComponent.Tops, new ClothesComponentId(11, ItemId.Top, 5) },
            { ClothesComponent.Undershort, new ClothesComponentId(8, ItemId.Undershit, 6) },
            { ClothesComponent.BodyArmors, new ClothesComponentId(9, ItemId.BodyArmor, 7) },
            { ClothesComponent.Bugs, new ClothesComponentId(5, ItemId.Bag, 8) },
            { ClothesComponent.Legs, new ClothesComponentId(4, ItemId.Leg, 9) },
            { ClothesComponent.Torsos, new ClothesComponentId(3, ItemId.Gloves, 12) },
            { ClothesComponent.Shoes, new ClothesComponentId(6, ItemId.Feet, 13) },
            { ClothesComponent.Decals, new ClothesComponentId(10, ItemId.Decals, 14) },
        };

        public static IReadOnlyDictionary<ItemId, ItemsInfo> ItemsInfo = new Dictionary<ItemId, ItemsInfo>()
{
     { ItemId.Camera1, new ItemsInfo("Камера видеонаблюдения", "Подойдет для наблюдения", "placingprop", "Размещаемые предметы", NAPI.Util.GetHashKey("prop_cctv_cam_05a"), 1, new Vector3(0.0,0.0,-0.9), new Vector3(), newItemType.None, "gray", 1.5f, 1, 1) },
    { ItemId.Camera2, new ItemsInfo("Камера видеонаблюдения", "Подойдет для наблюдения", "placingprop", "Размещаемые предметы", NAPI.Util.GetHashKey("prop_snow_cam_03a"), 1, new Vector3(0.0,0.0,-0.9), new Vector3(), newItemType.None, "gray", 1.5f, 1, 1) },
};




        private static void OnSaveJsonItemsInfo()
        {
            try
            {
                File.WriteAllText(@$"json/itemsInfo.js", string.Empty);
                using (var saveCoords = new StreamWriter(@$"json/itemsInfo.js", true, Encoding.UTF8))
                {
                    int index = 0;

                    saveCoords.Write("export const ItemType = {\n");

                    foreach (var key in Enum.GetValues(typeof(newItemType)))
                    {
                        index++;

                        if (ItemsInfo.Count == index)
                            saveCoords.Write($"\t{key.ToString()}: {(int)key}\r\n");
                        else
                            saveCoords.Write($"\t{key.ToString()}: {(int)key},\r\n");
                    }

                    saveCoords.Write("}\n\n");
                    index = 0;

                    saveCoords.Write("export const ItemId = {\n");

                    foreach (var key in Enum.GetValues(typeof(ItemId)))
                    {
                        index++;

                        if (ItemsInfo.Count == index)
                            saveCoords.Write($"\t{key.ToString()}: {(int)key}\r\n");
                        else
                            saveCoords.Write($"\t{key.ToString()}: {(int)key},\r\n");
                    }

                    saveCoords.Write("}\n\n");
                    index = 0;

                    saveCoords.Write("export const itemsInfo = {\n");
                    foreach (var itemInfo in ItemsInfo)
                    {
                        index++;

                        if (ItemsInfo.Count == index)
                            saveCoords.Write($"\t[ItemId.{itemInfo.Key.ToString()}]: {JsonConvert.SerializeObject(itemInfo.Value)}\r\n");
                        else
                            saveCoords.Write($"\t[ItemId.{itemInfo.Key.ToString()}]: {JsonConvert.SerializeObject(itemInfo.Value)},\r\n");

                        //saveData.Add(clothes.Key, data);
                    }
                    saveCoords.Write("}");
                    saveCoords.Close();
                }
            }
            catch
            {
                Log.Write($"OnSaveJsonClothes");
            }
        }


        public static ItemId[] StrongWeapons = new ItemId[18]
        {
            ItemId.DoubleAction,
            ItemId.PistolMk2,
            ItemId.SNSPistolMk2,
            ItemId.RevolverMk2,
            ItemId.SMGMk2,
            ItemId.CombatMGMk2,
            ItemId.AssaultRifleMk2,
            ItemId.CarbineRifleMk2,
            ItemId.SpecialCarbineMk2,
            ItemId.BullpupRifleMk2,
            ItemId.MilitaryRifle,
           // ItemId.HeavySniperMk2,
          //  ItemId.MarksmanRifleMk2,
            ItemId.PumpShotgunMk2,
            ItemId.CeramicPistol,
            ItemId.NavyRevolver,
            ItemId.TacticalRifle,
            ItemId.PrecisionRifle,
            ItemId.CombatShotgun,
            ItemId.HeavyRifle
        };

        public static ItemId[] HeavyWeapons = new ItemId[13]
        {
            ItemId.RayPistol,
            ItemId.RayCarbine,
            ItemId.GrenadeLauncher,
            ItemId.RPG,
            ItemId.Minigun,
            ItemId.Firework,
            ItemId.Railgun,
            ItemId.HomingLauncher,
            ItemId.GrenadeLauncherSmoke,
            ItemId.CompactGrenadeLauncher,
            ItemId.Widowmaker,
            ItemId.Glock,
            ItemId.CombatRifle,
        };
        // ✅ НОВАЯ ФУНКЦИЯ: Получаем реальный вес багажника
        // ✅ НОВАЯ ФУНКЦИЯ: Получаем реальный вес багажника
        // ✅ ИСПРАВЛЕНО: Используем int вместо uint
        private static float GetVehicleMaxWeight(ExtPlayer player)
        {
            try
            {
                var sessionData = player.GetSessionData();
                if (sessionData?.InventoryOtherLocationName == null) return MaxVehicleWeight;

                var parts = sessionData.InventoryOtherLocationName.Split('_');

                if (parts.Length >= 2 && int.TryParse(parts[1], out int vehicleSqlId))
                {
                    var vehicleData = VehicleManager.GetVehicleToAutoId(vehicleSqlId);
                    if (vehicleData != null)
                    {
                        // vehicleData.Model может быть строкой (имя модели или числовой хеш) — приводим корректно к uint
                        uint modelHash = 0;
                        try
                        {
                            var modelObj = vehicleData.Model;
                            if (modelObj == null)
                            {
                                Log.Write($"GetVehicleMaxWeight: model is null for vehicleSqlId={vehicleSqlId}");
                                return MaxVehicleWeight;
                            }

                            string modelStr = modelObj.ToString();

                            // Попробуем распарсить как число (hash)
                            if (uint.TryParse(modelStr, out modelHash))
                            {
                                // успешно распарсили числовой хеш
                                Log.Write($"GetVehicleMaxWeight: parsed model as numeric hash -> {modelHash} (vehicleSqlId={vehicleSqlId})");
                            }
                            else
                            {
                                // Иначе считаем, что это имя модели и получаем hash через NAPI.Util.GetHashKey
                                modelHash = NAPI.Util.GetHashKey(modelStr);
                                Log.Write($"GetVehicleMaxWeight: computed hash from model name '{modelStr}' -> {modelHash} (vehicleSqlId={vehicleSqlId})");
                            }
                        }
                        catch (Exception exModel)
                        {
                            Log.Write($"GetVehicleMaxWeight: failed to determine model hash for vehicleSqlId={vehicleSqlId}: {exModel}");
                            return MaxVehicleWeight;
                        }

                        // Берём вес из vMain по модели (GetMaxWeight возвращает граммы) и переводим в кг
                        int grams = vMain.GetMaxWeight(modelHash);
                        float kg = grams / 1000f;
                        Log.Write($"GetVehicleMaxWeight: vehicleSqlId={vehicleSqlId} modelHash={modelHash} grams={grams} kg={kg}");
                        return kg;
                    }
                }

                return MaxVehicleWeight;
            }
            catch (Exception e)
            {
                Log.Write($"GetVehicleMaxWeight Exception: {e.ToString()}");
                return MaxVehicleWeight;
            }
        }
        public static void LoadOtherItemsData(ExtPlayer player, string Name, string Id, int OtherId, int MaxSlots = 0, string selectItemId = "", bool IsArmyCar = false, bool isMyTent = false, bool IsTurn = false)
        {
            try
            {
                var sessionData = player.GetSessionData();
                if (sessionData == null) return;
                string locationName = $"{Name}_{Id}";
                Name = Name.Split('_').Length > 0 ? Name.Split('_')[0] : Name;
                OtherClose(player);
                sessionData.InventoryOtherLocationName = locationName;
                if (!InventoryOtherPlayers.ContainsKey(locationName)) InventoryOtherPlayers.TryAdd(locationName, new List<ExtPlayer>());
                if (!InventoryOtherPlayers[locationName].Contains(player)) InventoryOtherPlayers[locationName].Add(player);

                // Расчёт максимального веса (в КГ по дефолту)
                float maxWeight = MaxInventoryWeight; // по умолчанию (в кг)

                if (Name == "vehicle")
                {
                    maxWeight = GetVehicleMaxWeight(player); // реальный вес багажника в кг
                }
                else if (Name == "backpack")
                {
                    maxWeight = MaxBackpackWeight;
                }
                else if (Name == "warehouse")
                {
                    maxWeight = MaxWarehouseWeight;
                }

                if (Name != "warehouse")
                {
                    var returnData = ClientEventLoadItemsData(locationName, Name, MaxSlots);

                    Log.Write($"LoadOtherItemsData -> sending maxWeightGrams={maxWeight} for location={locationName} name={Name}");

                    Trigger.ClientEvent(player, "client.inventory.InitOtherData", OtherId, Id, returnData.Item1, returnData.Item2, selectItemId, IsArmyCar, isMyTent, maxWeight * 1000f, IsTurn);
                }
                else
                {
                    var returnData = ClientEventLoadWarehouseItemsData(locationName, MaxSlots);
                    var itemsList = JsonConvert.DeserializeObject<List<InventoryItemData>>(returnData.Item1);
                    itemsList = itemsList.Where(item => item != null && item.ItemId != 0).ToList();
                    var fixedJson = JsonConvert.SerializeObject(itemsList);

                    Trigger.ClientEvent(player, "client.inventory.InitOtherDataStock", OtherId, Id, fixedJson, returnData.Item2, selectItemId, IsArmyCar, isMyTent, maxWeight * 1000f);
                }
            }
            catch (Exception e)
            {
                Log.Write($"LoadOtherItemsData Exception: {e.ToString()}");
            }
        }
        // Заменить текущую реализацию на эту
        // Заменить текущую реализацию на эту
        public static float GetCurrentWeight(string locationName, string location)
        {
            try
            {
                float totalWeight = 0f;

                if (ItemsData.ContainsKey(locationName) && ItemsData[locationName].ContainsKey(location))
                {
                    foreach (var item in ItemsData[locationName][location].Values)
                    {
                        if (item.ItemId == ItemId.Debug) continue;

                        if (!ItemsInfo.ContainsKey(item.ItemId)) continue;

                        var itemInfo = ItemsInfo[item.ItemId];
                        // ItemsInfo.Weight — в килограммах
                        totalWeight += itemInfo.Weight * item.Count;
                    }
                }

                Log.Write($"[GetCurrentWeight] locationName={locationName}, location={location}, totalWeight={totalWeight:F1} кг");

                return totalWeight;
            }
            catch (Exception e)
            {
                Log.Write($"GetCurrentWeight Exception: {e.ToString()}");
                return 0f;
            }
        }

        // ✅ МЕТОД ДЛЯ ПРОВЕРКИ МОЖНО ЛИ ДОБАВИТЬ ПРЕДМЕТ
        // ✅ НОВАЯ ВЕРСИЯ: Поддержка vehicle и backpack
        // заменяет существующую реализацию CanAddItemByWeight
        public static bool CanAddItemByWeight(ExtPlayer player, string location, ItemId itemId, int count)
        {
            try
            {
                var characterData = player.GetCharacterData();
                if (characterData == null) return false;

                string locationName = "";

                if (location == "vehicle")
                {
                    var sessionData = player.GetSessionData();
                    if (sessionData?.InventoryOtherLocationName == null) return true;
                    locationName = sessionData.InventoryOtherLocationName;
                }
                else if (location == "backpack")
                {
                    var bagItem = GetItemData(player, "accessories", 8);
                    if (bagItem.ItemId != ItemId.Bag) return true;
                    locationName = $"backpack_{bagItem.SqlId}";
                }
                else
                {
                    locationName = $"char_{characterData.UUID}";
                }

                float maxWeight = location switch
                {
                    "inventory" => MaxInventoryWeight,
                    "backpack" => MaxBackpackWeight,
                    "warehouse" => MaxWarehouseWeight,
                    "vehicle" => GetVehicleMaxWeight(player),
                    _ => MaxInventoryWeight
                };

                // ✅ ИСПРАВЛЕНО: Правильно получаем текущий вес
                float currentWeight = GetCurrentWeight(locationName, location == "vehicle" ? locationName.Split('_')[0] : location);

                // ItemsInfo.Weight — в килограммах
                float itemWeight = ItemsInfo[itemId].Weight * count;

                Log.Write($"[WEIGHT CHECK] location={location}, currentWeight={currentWeight:F1} кг, itemWeight={itemWeight:F1} кг, maxWeight={maxWeight:F1} кг");

                if (currentWeight + itemWeight > maxWeight)
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter,
                        $"Слишком тяжело! ({currentWeight + itemWeight:F1}/{maxWeight:F1} кг)", 3000);
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                Log.Write($"CanAddItemByWeight Exception: {e.ToString()}");
                return false;
            }
        }
        public static void LoadCharItemsData(ExtPlayer player)
        {
            try
            {
                if (!player.IsCharacterData()) return;
                InitInventory(player, null);
            }
            catch (Exception e)
            {
                Log.Write($"LoadCharItemsData Exception: {e.ToString()}");
            }
        }
        public static string GetVehicleName(string data)
        {
            try
            {
                if (data != null && data.Split('_').Length >= 2 && int.TryParse(data.Split('_')[0], out int SqlId))
                {
                    var vehicleData = VehicleManager.GetVehicleToAutoId(SqlId);
                    if (vehicleData != null)
                        return $"{vehicleData.Model.ToUpper()}_{vehicleData.Number}";
                }
            }
            catch (Exception e)
            {
                Log.Write($"GetVehicleName Exception: {e.ToString()}");
            }
            return "-_-";
        }
        public static void InitInventory(ExtPlayer player, ExtPlayer Target = null)
        {
            try
            {
                if (!player.IsCharacterData()) return;
                var getPlayer = Target == null ? player : Target;

                var targetCharacterData = getPlayer.GetCharacterData();
                if (targetCharacterData == null)
                    return;

                InventoryItemData ItemBag = null;

                List<InventoryItemData> _JsonAccessoriesItemData = new List<InventoryItemData>();
                List<InventoryItemData> _JsonInventoryItemData = new List<InventoryItemData>();
                List<InventoryItemData> _JsonFastSlotsItemData = new List<InventoryItemData>();

                string locationName = $"char_{targetCharacterData.UUID}";

                if (ItemsData.ContainsKey(locationName))
                {
                    // ✅ 1. СНАЧАЛА ЗАГРУЖАЕМ ВСЕ ПРЕДМЕТЫ (КРОМЕ FASTSLOTS)
                    foreach (string Location in ItemsData[locationName].Keys)
                    {
                        if (Location == "fastSlots") continue; // ❌ ПРОПУСКАЕМ FASTSLOTS

                        var sortedItemData = ItemsData[locationName][Location].Values.OrderBy(i => i.Index).ToList();
                        foreach (InventoryItemData item in sortedItemData)
                        {
                            if (item.ItemId == ItemId.Debug) continue;

                            InventoryItemData newItem = new InventoryItemData(
                                SqlId: item.SqlId,
                                ItemId: item.ItemId,
                                Count: item.Count,
                                Data: item.Data,
                                Index: item.Index,
                                IsTurn: item.IsTurn
                            );

                            if (newItem.ItemId == ItemId.Bag) ItemBag = newItem;
                            if (newItem.ItemId == ItemId.CarKey) newItem.Data = GetVehicleName(newItem.Data);

                            if (Location == "accessories") _JsonAccessoriesItemData.Add(newItem);
                            else if (Location == "inventory") _JsonInventoryItemData.Add(newItem);
                        }
                    }

                    // ✅ 2. ТЕПЕРЬ ЗАГРУЖАЕМ FASTSLOTS (КОПИИ)
                    if (ItemsData[locationName].ContainsKey("fastSlots"))
                    {
                        var sortedFastSlots = ItemsData[locationName]["fastSlots"].Values.OrderBy(i => i.Index).ToList();

                        foreach (InventoryItemData item in sortedFastSlots)
                        {
                            // ✅ ПРОВЕРЯЕМ: ЭТО КОПИЯ?
                            if (item.Data != null && item.Data.StartsWith("orig_"))
                            {
                                var parts = item.Data.Split('_');
                                if (parts.Length >= 2 && int.TryParse(parts[1], out int origSqlId))
                                {
                                    Log.Write($"[INITINVENTORY] Loading copy from fastSlots: Data={item.Data}, ItemId={item.ItemId}");

                                    // ✅ ПРОВЕРЯЕМ: СУЩЕСТВУЕТ ЛИ ОРИГИНАЛ?
                                    bool originalExists = false;

                                    if (ItemsData.ContainsKey(locationName))
                                    {
                                        foreach (var loc in ItemsData[locationName].Values)
                                        {
                                            foreach (var invItem in loc.Values)
                                            {
                                                if (invItem.SqlId == origSqlId && invItem.ItemId == item.ItemId)
                                                {
                                                    originalExists = true;
                                                    break;
                                                }
                                            }
                                            if (originalExists) break;
                                        }
                                    }

                                    if (originalExists)
                                    {
                                        // ✅ ОРИГИНАЛ СУЩЕСТВУЕТ → ОТОБРАЖАЕМ КОПИЮ
                                        InventoryItemData copyItem = new InventoryItemData(
                                            SqlId: item.SqlId,
                                            ItemId: item.ItemId,
                                            Count: item.Count,
                                            Data: item.Data, // ✅ СОХРАНЯЕМ orig_
                                            Index: item.Index,
                                            IsTurn: item.IsTurn
                                        );

                                        _JsonFastSlotsItemData.Add(copyItem);
                                        Log.Write($"[INITINVENTORY] Copy loaded: ItemId={item.ItemId}, SqlId={item.SqlId}, OriginalSqlId={origSqlId}");
                                    }
                                    else
                                    {
                                        // ❌ ОРИГИНАЛ УДАЛЁН → УДАЛЯЕМ КОПИЮ
                                        Log.Write($"[INITINVENTORY] Copy broken (original deleted): {item.Data}");

                                        if (item.SqlId > 0)
                                        {
                                            Database.Models.Items.AddItemDelete(item.SqlId);
                                        }

                                        // ✅ ИСПРАВЛЕНО: Используем правильную переменную
                                        ItemsData[locationName]["fastSlots"].TryRemove(item.Index, out _);
                                    }
                                }
                            }
                            else if (item.ItemId != ItemId.Debug)
                            {
                                // ✅ ОБЫЧНЫЙ ПРЕДМЕТ (НЕ КОПИЯ)
                                InventoryItemData newItem = new InventoryItemData(
                                    SqlId: item.SqlId,
                                    ItemId: item.ItemId,
                                    Count: item.Count,
                                    Data: item.Data,
                                    Index: item.Index,
                                    IsTurn: item.IsTurn
                                );

                                _JsonFastSlotsItemData.Add(newItem);
                            }
                        }
                    }
                }

                // ✅ 3. ОТПРАВЛЯЕМ ДАННЫЕ КЛИЕНТУ
                string _ItemsData = JsonConvert.SerializeObject(new Dictionary<string, List<InventoryItemData>>
        {
            { "accessories", _JsonAccessoriesItemData },
            { "inventory", _JsonInventoryItemData },
            { "fastSlots", _JsonFastSlotsItemData },
        });

                Trigger.ClientEvent(player, "client.inventory.InitData", _ItemsData, Target == null ? true : false);

                if (Target == null)
                {
                    isRadio(player);
                }
                else if (Target != null)
                {
                    if (ItemBag != null)
                    {
                        int maxSlots = 20;
                        Dictionary<string, int> PlayerBugData = ItemBag.GetData();
                        ConcurrentDictionary<int, ClothesData> ShoesData = ClothesComponents.ClothesBugsData;
                        if (ShoesData.ContainsKey(PlayerBugData["Variation"]) && ShoesData[PlayerBugData["Variation"]].MaxSlots > 0)
                            maxSlots = ShoesData[PlayerBugData["Variation"]].MaxSlots;

                        Tuple<string, int> returnData = ClientEventLoadItemsData($"backpack_{ItemBag.SqlId}", "backpack", maxSlots);
                        Trigger.ClientEvent(player, "client.inventory.InitBackpack", maxSlots, returnData.Item1, false);
                    }
                    else
                    {
                        Trigger.ClientEvent(player, "client.inventory.InitBackpack", 0, "[]", false);
                    }
                }

                PlayerStats(player, Target);

                // ✅ 4. ОТПРАВЛЯЕМ ВЕС ПРИ ОТКРЫТИИ ИНВЕНТАРЯ
                if (Target == null && player.IsCharacterData())
                {
                    var characterData = player.GetCharacterData();
                    if (characterData != null)
                    {
                        float inventoryWeight = GetCurrentWeight(locationName, "inventory");
                        float backpackWeight = 0f;

                        InventoryItemData bagItem = GetItemData(player, "accessories", 8);
                        if (bagItem.ItemId == ItemId.Bag)
                        {
                            backpackWeight = GetCurrentWeight($"backpack_{bagItem.SqlId}", "backpack");
                        }

                        characterData.InventoryWeight = inventoryWeight;
                        characterData.BackpackWeight = backpackWeight;
                        characterData.MaxInventoryWeight = 40;
                        characterData.MaxBackpackWeight = 25;

                        Log.Write($"[InitInventory] Sending weight: inventoryWeight={inventoryWeight:F1} кг, backpackWeight={backpackWeight:F1} кг");

                        Trigger.ClientEvent(player, "client.inventory.UpdateWeight", inventoryWeight, backpackWeight);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Write($"InitInventory Exception: {e.ToString()}");
            }
        }
        public static void isRadio(ExtPlayer player)
        {
            try
            {
                var sessionData = player.GetSessionData();
                if (sessionData == null) return;
                if (isItem(player, "inventory", ItemId.Radio) != null)
                {
                    if (sessionData.WalkieTalkieFrequency == -99)
                        sessionData.WalkieTalkieFrequency = -1;
                }
                else sessionData.WalkieTalkieFrequency = -99;

                sessionData.IsRadioInterceptor = isItem(player, "inventory", ItemId.RadioInterceptor) != null;
            }
            catch (Exception e)
            {
                Log.Write($"isRadio Exception: {e.ToString()}");
            }
        }

        public static int isBackpackItemsData(ExtPlayer player, bool init = false)
        {
            try
            {
                if (!player.IsCharacterData()) return 0;
                InventoryItemData Bags = GetItemData(player, "accessories", 8);
                if (Bags.ItemId == ItemId.Bag)
                {
                    if (init)
                    {
                        int maxSlots = 20;
                        Dictionary<string, int> PlayerBugData = Bags.GetData();
                        ConcurrentDictionary<int, ClothesData> ShoesData = ClothesComponents.ClothesBugsData;
                        if (ShoesData.ContainsKey(PlayerBugData["Variation"]) && ShoesData[PlayerBugData["Variation"]].MaxSlots > 0) maxSlots = ShoesData[PlayerBugData["Variation"]].MaxSlots;
                        Tuple<string, int> returnData = ClientEventLoadItemsData($"backpack_{Bags.SqlId}", "backpack", maxSlots);
                        Trigger.ClientEvent(player, "client.inventory.InitBackpack", maxSlots, returnData.Item1, true);
                    }
                    return Bags.SqlId;
                }
                else if (init) Trigger.ClientEvent(player, "client.inventory.InitBackpack", 0, "[]", false);
                return 0;
            }
            catch (Exception e)
            {
                Log.Write($"isBackpackItemsData Exception: {e.ToString()}");
                return 0;
            }
        }
        public static int GetFreeSlot(string locationName, string Location)
        {
            try
            {
                if (ItemsData.ContainsKey(locationName) && ItemsData[locationName].ContainsKey(Location) && ItemsData[locationName][Location].Count > 0)
                {
                    var items = ItemsData[locationName][Location];

                    var sortedItemData = items.Values.OrderByDescending(i => i.Index).FirstOrDefault();

                    if (sortedItemData != null)
                    {
                        for (var i = 0; i < sortedItemData.Index + 10; i++)
                        {
                            // ✅ ПРОПУСКАЕМ ЗАНЯТЫЕ СЛОТЫ
                            if (!items.ContainsKey(i))
                            {
                                return i;
                            }

                            // ✅ ПРОПУСКАЕМ ЗАГЛУШКИ (ТОЛЬКО для inventory/backpack)
                            if (Location == "inventory" || Location == "backpack")
                            {
                                if (items[i].ItemId == ItemId.Debug &&
                                    (items[i].Data == null || !items[i].Data.StartsWith("placeholder_")))
                                {
                                    return i;
                                }
                            }
                            else
                            {
                                // ✅ ДЛЯ VEHICLE/WAREHOUSE — Debug = свободный слот
                                if (items[i].ItemId == ItemId.Debug)
                                {
                                    return i;
                                }
                            }
                        }
                    }
                }
                return 0;
            }
            catch (Exception e)
            {
                Log.Write($"GetFreeSlot Exception: {e.ToString()}");
                return 0;
            }
        }

        public static int GetItemsDataLastId(string locationName, string Location)
        {
            try
            {
                int SlotId = 0;
                if (ItemsData.ContainsKey(locationName) && ItemsData[locationName].ContainsKey(Location) && ItemsData[locationName][Location].Count > 0)
                {
                    var sortedItemData = ItemsData[locationName][Location].Values.OrderBy(i => i.Index).ToList();

                    foreach (InventoryItemData item in sortedItemData)
                    {
                        if (item.ItemId != ItemId.Debug)
                        {
                            if (item.Index >= SlotId) SlotId = item.Index + 1;
                        }
                    }
                }
                return SlotId;
            }
            catch (Exception e)
            {
                Log.Write($"GetWarehouseLastId Exception: {e.ToString()}");
                return 0;
            }
        }
        public static Tuple<string, int> ClientEventLoadWarehouseItemsData(string locationName, int MaxSlots)
        {
            try
            {
                var Location = "warehouse";
                if (ItemsData.ContainsKey(locationName) && ItemsData[locationName].ContainsKey(Location) && ItemsData[locationName][Location].Count > 0)
                {
                    var items = ItemsData[locationName][Location];

                    var maxIndex = items.Values.OrderByDescending(i => i.Index).Select(i => i.Index).FirstOrDefault();

                    var index = 0;
                    for (var i = 0; i < maxIndex + 10; i++)
                    {
                        if (!items.ContainsKey(i) || items[i].ItemId == ItemId.Debug)
                        {
                            var lastItem = items.LastOrDefault();

                            if (lastItem.Value != null)
                            {
                                items.TryRemove(lastItem.Key, out _);

                                items[i] = new InventoryItemData(lastItem.Value.SqlId, lastItem.Value.ItemId, lastItem.Value.Count, lastItem.Value.Data, i);

                                UpdateSqlItemData(locationName, Location, i, items[i]);
                            }
                        }
                        if (++index >= 300)
                            break;
                    }



                    /*var sortedItemData = ItemsData[locationName][Location].Values.OrderBy(i => i.Index).ToList();
                    var index = 0;
                    foreach (var item in sortedItemData)
                    {
                        if (item.ItemId != ItemId.Debug)
                        {
                            var newItem = new InventoryItemData(SqlId: item.SqlId, ItemId: item.ItemId, Count: item.Count, Data: item.Data, Index: item.Index);
                            newItem.Price = item.Price;


                            if (newItem.Index != index)
                            {
                                var SlotId = GetFreeSlot(locationName, Location);

                                Console.WriteLine($"SlotId - ${newItem.ItemId} - ${newItem.Index} - {SlotId}");
                                newItem.Index = SlotId;

                                ItemsData[locationName][Location][SlotId] = new InventoryItemData(newItem.SqlId, newItem.ItemId, newItem.Count, newItem.Data, SlotId);

                                UpdateSqlItemData(locationName, Location, SlotId, ItemsData[locationName][Location][SlotId]);
                                
                                
                            }

                            index++;
                        }
                    }*/
                    List<InventoryItemData> _JsonInventoryItemData = new List<InventoryItemData>();
                    var sortedItemData = ItemsData[locationName][Location].Values.OrderBy(i => i.Index).ToList();
                    foreach (InventoryItemData item in sortedItemData)
                    {
                        if (item.ItemId != ItemId.Debug)
                        {
                            InventoryItemData newItem = new InventoryItemData(SqlId: item.SqlId, ItemId: item.ItemId, Count: item.Count, Data: item.Data, Index: item.Index);
                            newItem.Price = item.Price;
                            if (newItem.Index >= MaxSlots) MaxSlots = newItem.Index + 1;
                            if (newItem.ItemId == ItemId.CarKey) newItem.Data = GetVehicleName(newItem.Data);
                            _JsonInventoryItemData.Add(newItem);

                            if (_JsonInventoryItemData.Count == 300)
                                break;
                        }
                    }
                    return new Tuple<string, int>(JsonConvert.SerializeObject(_JsonInventoryItemData), MaxSlots);
                }
                return new Tuple<string, int>(JsonConvert.SerializeObject(new List<InventoryItemData>()), MaxSlots);
            }
            catch (Exception e)
            {
                Log.Write($"ClientEventLoadWarehouseItemsData Exception: {e.ToString()}");
                return new Tuple<string, int>(JsonConvert.SerializeObject(new List<InventoryItemData>()), MaxSlots);
            }

        }
        public static Tuple<string, int> ClientEventLoadItemsData(string locationName, string Location, int MaxSlots)
        {
            try
            {
                if (ItemsData.ContainsKey(locationName) && ItemsData[locationName].ContainsKey(Location) && ItemsData[locationName][Location].Count > 0)
                {
                    List<InventoryItemData> _JsonInventoryItemData = new List<InventoryItemData>();
                    var sortedItemData = ItemsData[locationName][Location].Values.OrderBy(i => i.Index).ToList();

                    foreach (InventoryItemData item in sortedItemData)
                    {
                        InventoryItemData newItem = new InventoryItemData(SqlId: item.SqlId, ItemId: item.ItemId, Count: item.Count, Data: item.Data, Index: item.Index);
                        newItem.Price = item.Price;
                        newItem.IsTurn = item.IsTurn;

                        if (newItem.Index >= MaxSlots) MaxSlots = newItem.Index + 1;
                        if (newItem.ItemId == ItemId.CarKey) newItem.Data = GetVehicleName(newItem.Data);

                        _JsonInventoryItemData.Add(newItem);

                        // ✅ ДОБАВЬ ЛОГИРОВАНИЕ
                        if (item.ItemId == ItemId.Debug)
                        {
                            Log.Write($"[PLACEHOLDER] Sending placeholder: Index={item.Index}, Data={item.Data}");
                        }
                        else
                        {
                            Log.Write($"[ITEM] Sending item: ItemId={item.ItemId}, Index={item.Index}, Width={ItemsInfo[item.ItemId].Width}, Height={ItemsInfo[item.ItemId].Height}");
                        }

                        if (_JsonInventoryItemData.Count == 250)
                            break;
                    }

                    Log.Write($"[CLIENTLOAD] Total items sent: {_JsonInventoryItemData.Count} for location={locationName}");
                    return new Tuple<string, int>(JsonConvert.SerializeObject(_JsonInventoryItemData), MaxSlots);
                }
                return new Tuple<string, int>(JsonConvert.SerializeObject(new List<InventoryItemData>()), MaxSlots);
            }
            catch (Exception e)
            {
                Log.Write($"ClientEventLoadItemsData Exception: {e.ToString()}");
                return new Tuple<string, int>(JsonConvert.SerializeObject(new List<InventoryItemData>()), MaxSlots);
            }
        }
        public static void InitItems()
        {
            OnSaveJsonItemsInfo();
            CleanupPlaceholders();
            DateTime TestSpeedLoad = DateTime.Now;
            using MySqlCommand countSql = new MySqlCommand
            {
                CommandText = "DELETE FROM `items_data` WHERE data_id='drop' OR item_id=0"
            };
            MySQL.Query(countSql);
            using MySqlCommand cmd = new MySqlCommand()
            {
                CommandText = "SELECT * FROM `items_data`"
            };
            using DataTable result = MySQL.QueryRead(cmd);

            Dictionary<string, List<InventoryItemData>> AddWarehouseLocal = new Dictionary<string, List<InventoryItemData>>();
            if (result != null)
            {
                int SqlID;
                ItemId ItemId;
                int Count;
                string Data;
                string Location;
                int SlotId;
                string locationName;
                int count = 0;
                foreach (DataRow Row in result.Rows)
                {
                    count++;
                    locationName = Convert.ToString(Row["data_id"]);
                    SqlID = Convert.ToInt32(Row["auto_id"]);
                    ItemId = (ItemId)Convert.ToInt32(Row["item_id"]);
                    Count = Convert.ToInt32(Row["item_count"]);
                    Data = Convert.ToString(Row["item_data"]);
                    Location = Convert.ToString(Row["location"]);
                    SlotId = Convert.ToInt32(Row["slotId"]);
                    bool IsTurn = Convert.ToBoolean(Row["is_turn"]); // ✅ ЧИТАЕМ ИЗ БД

                    if (ItemId == ItemId.Debug && (Data == null || !Data.StartsWith("placeholder_")))
                        continue; // Пропускаем ТОЛЬКО пустые слоты (не заглушки)

                    if (!ItemsData.ContainsKey(locationName))
                        ItemsData.TryAdd(locationName, new ConcurrentDictionary<string, ConcurrentDictionary<int, InventoryItemData>>());

                    if (!ItemsData[locationName].ContainsKey(Location))
                        ItemsData[locationName].TryAdd(Location, new ConcurrentDictionary<int, InventoryItemData>());

                    if (Location == "tent")
                    {
                        if (!AddWarehouseLocal.ContainsKey(locationName)) AddWarehouseLocal.Add(locationName, new List<InventoryItemData>());
                        AddWarehouseLocal[locationName].Add(new InventoryItemData(SqlID, ItemId, Count, Data, SlotId));
                    }
                    else if (!ItemsData[locationName][Location].ContainsKey(SlotId))
                    {
                        // ✅ ИСПРАВЛЕНО: СНАЧАЛА СОЗДАЁМ itemData С IsTurn
                        var itemData = new InventoryItemData(SqlID, ItemId, Count, Data, SlotId)
                        {
                            IsTurn = IsTurn // ✅ УСТАНАВЛИВАЕМ ПОВОРОТ ИЗ БД
                        };

                        ItemsData[locationName][Location].TryAdd(SlotId, itemData);

                        // ✅ ТЕПЕРЬ СОЗДАЁМ ЗАГЛУШКИ С ПРАВИЛЬНЫМ IsTurn
                        if (ItemId != ItemId.Debug && (Location == "inventory" || Location == "backpack"))
                        {
                            if (ItemsInfo.ContainsKey(ItemId))
                            {
                                var itemInfo = ItemsInfo[ItemId];

                                // ✅ ИСПРАВЛЕНО: ИСПОЛЬЗУЕМ itemData.IsTurn (а не переменную IsTurn напрямую)
                                int itemWidth = itemData.IsTurn ? itemInfo.Height : itemInfo.Width;
                                int itemHeight = itemData.IsTurn ? itemInfo.Width : itemInfo.Height;

                                if (itemWidth > 1 || itemHeight > 1)
                                {
                                    int maxCols = 6;
                                    int startX = SlotId % maxCols;
                                    int startY = SlotId / maxCols;

                                    for (int y = 0; y < itemHeight; y++)
                                    {
                                        for (int x = 0; x < itemWidth; x++)
                                        {
                                            if (x == 0 && y == 0) continue;

                                            int placeholderIndex = (startY + y) * maxCols + (startX + x);

                                            var placeholder = new InventoryItemData(
                                                SqlId: -1,
                                                ItemId: ItemId.Debug,
                                                Count: 0,
                                                Data: $"placeholder_{SlotId}",
                                                Index: placeholderIndex
                                            );

                                            if (!ItemsData[locationName][Location].ContainsKey(placeholderIndex))
                                            {
                                                ItemsData[locationName][Location].TryAdd(placeholderIndex, placeholder);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (InventoryLocation.ContainsKey(Location) || Location == "warehouse")
                    {
                        if (!AddWarehouseLocal.ContainsKey(locationName)) AddWarehouseLocal.Add(locationName, new List<InventoryItemData>());
                        AddWarehouseLocal[locationName].Add(new InventoryItemData(SqlID, ItemId, Count, Data, SlotId));
                    }

                    if (ItemId != ItemId.Debug)
                        OnAddItem(ItemId, Data);
                }
                Log.Write($"[{DateTime.Now - TestSpeedLoad}] Inventory system loaded ({count})");
            }

            TestSpeedLoad = DateTime.Now;
            if (AddWarehouseLocal.Count > 0)
            {
                string Location = "warehouse";
                int count = 0;

                foreach (KeyValuePair<string, List<InventoryItemData>> WarehouseLocalData in AddWarehouseLocal)
                {
                    try
                    {
                        string locationName = WarehouseLocalData.Key;
                        locationName = $"warehouse_{locationName.Split('_')[1]}";
                        if (!ItemsData.ContainsKey(locationName))
                            ItemsData.TryAdd(locationName, new ConcurrentDictionary<string, ConcurrentDictionary<int, InventoryItemData>>());
                        if (!ItemsData[locationName].ContainsKey(Location))
                            ItemsData[locationName].TryAdd(Location, new ConcurrentDictionary<int, InventoryItemData>());

                        foreach (var Item in WarehouseLocalData.Value)
                        {
                            short SlotId = (short)GetItemsDataLastId(locationName, Location);
                            if (ItemsData[locationName][Location].ContainsKey(SlotId))
                                ItemsData[locationName][Location].TryRemove(SlotId, out _);
                            ItemsData[locationName][Location].TryAdd(SlotId, new InventoryItemData(Item.SqlId, Item.ItemId, Item.Count, Item.Data, SlotId));

                            Database.Models.Items.AddItemUpdate(Item.SqlId, locationName, Item.Count, Item.Data, Location, SlotId);

                            count++;
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Write($"WarehouseLocalData Exception: {e.ToString()}");
                    }
                }
                Log.Write($"[{DateTime.Now - TestSpeedLoad}] Add Warehouse system loaded ({count})");
            }

        }

        static readonly IReadOnlyDictionary<string, string> InventoryLocation = new Dictionary<string, string>()
        {
            { "accessories", "char" },
            { "inventory", "char" },
            { "fastSlots", "char" },
            { "trade", "char" },
            { "with_trade", "char" }
        };

        public static string GetLocationName(ExtPlayer player, string Location)
        {
            try
            {
                var sessionData = player.GetSessionData();
                if (sessionData == null) return Location;

                var characterData = player.GetCharacterData();
                string locationName = Location;

                if (Location == "other" && sessionData.InventoryOtherLocationName != null) locationName = sessionData.InventoryOtherLocationName;
                else if (characterData == null && Location != "other" && Location != "backpack") locationName = $"char_{sessionData.SelectUUID}";
                else if (characterData != null && Location != "other" && Location != "backpack") locationName = $"char_{characterData.UUID}";
                else if (Location == "backpack")
                {
                    InventoryItemData Bags = GetItemData(player, "accessories", 8);
                    locationName = $"backpack_{Bags.SqlId}";
                }
                return locationName;
            }
            catch (Exception e)
            {
                Log.Write($"GetLocationName Exception: {e.ToString()}");
                return Location;
            }
        }
        public static void AddInventoryArray(string locationName, string Location)
        {
            try
            {
                if (!ItemsData.ContainsKey(locationName)) ItemsData.TryAdd(locationName, new ConcurrentDictionary<string, ConcurrentDictionary<int, InventoryItemData>>());
                if (!ItemsData[locationName].ContainsKey(Location)) ItemsData[locationName].TryAdd(Location, new ConcurrentDictionary<int, InventoryItemData>());
            }
            catch (Exception e)
            {
                Log.Write($"AddInventoryArray Exception: {e.ToString()}");
            }
        }

        public static void Remove(ExtPlayer player, string locationName, string Location, ItemId ItemId, int count = 1, string data = null)
        {
            try
            {
                if (player != null && !player.IsCharacterData()) return;

                AddInventoryArray(locationName, Location);

                List<ItemStruct> _Items = new List<ItemStruct>();

                foreach (var item in ItemsData[locationName][Location])
                {
                    if (item.Value.ItemId == ItemId && (data == null ? true : item.Value.Data == data))
                    {
                        if (ItemsInfo[ItemId].Stack > 1 && (item.Value.Count - count) > 0)
                        {
                            _Items.Add(new ItemStruct(Location, item.Key, new InventoryItemData(item.Value.SqlId, item.Value.ItemId, count, item.Value.Data, item.Value.Index)));
                            count = 0;
                        }
                        else
                        {
                            if (ItemsInfo[ItemId].Stack > 1) count -= item.Value.Count;
                            else count -= 1;
                            _Items.Add(new ItemStruct(Location, item.Key, item.Value));
                        }

                        if (count == 0) break;
                    }
                }
                RemoveFix(player, locationName, _Items);
            }
            catch (Exception e)
            {
                Log.Write($"Remove Exception: {e.ToString()}");
            }
        }
        public static void RemoveIndex(ExtPlayer player, string Location, int SlotId, int count = 1)
        {
            try
            {
                var sessionData = player.GetSessionData();
                if (sessionData == null) return;
                string locationName = GetLocationName(player, Location);
                if (locationName != null)
                {
                    InventoryItemData _Item = GetItemData(player, Location, SlotId);
                    if (_Item.ItemId != ItemId.Debug)
                    {
                        if (Location == "fastSlots" && sessionData.ActiveWeap.Index == SlotId)
                        {
                            WeaponRepository.RemoveHands(player);
                        }

                        if (ItemsInfo[_Item.ItemId].Stack > 1 && (_Item.Count - count) < 1) Remove(player, locationName, Location, _Item.ItemId, count - _Item.Count);
                        if ((_Item.Count -= count) < 1) _Item.ItemId = ItemId.Debug;
                        SetItemData(player, Location, SlotId, _Item, true);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Write($"RemoveIndex Exception: {e.ToString()}");
            }
        }
        public static bool RemoveAllIllegal(ExtPlayer player)
        {
            try
            {
                var characterData = player.GetCharacterData();
                if (characterData == null) return false;

                string locationName = $"char_{characterData.UUID}";
                bool removed = false;
                List<ItemStruct> _Items = new List<ItemStruct>();
                if (ItemsData.ContainsKey(locationName))
                {
                    foreach (string Location in ItemsData[locationName].Keys)
                    {
                        foreach (var itemData in ItemsData[locationName][Location])
                        {
                            if (itemData.Value.ItemId == ItemId.Material || itemData.Value.ItemId == ItemId.Drugs || itemData.Value.ItemId == ItemId.BodyArmor)
                            {
                                _Items.Add(new ItemStruct(Location, itemData.Key, itemData.Value));
                                removed = true;
                            }
                        }
                    }
                }
                RemoveFix(player, locationName, _Items);

                int BagsSqlId = isBackpackItemsData(player);
                if (BagsSqlId != 0 && ItemsData.ContainsKey($"backpack_{BagsSqlId}") && ItemsData[$"backpack_{BagsSqlId}"].ContainsKey("backpack"))
                {
                    _Items = new List<ItemStruct>();
                    foreach (var itemData in ItemsData[$"backpack_{BagsSqlId}"]["backpack"])
                    {
                        if (itemData.Value.ItemId == ItemId.Material || itemData.Value.ItemId == ItemId.Drugs || itemData.Value.ItemId == ItemId.BodyArmor)
                        {
                            _Items.Add(new ItemStruct("backpack", itemData.Key, itemData.Value));
                            removed = true;
                        }
                    }
                    RemoveFix(player, $"backpack_{BagsSqlId}", _Items);
                }
                return removed;
            }
            catch (Exception e)
            {
                Log.Write($"RemoveAllIllegal Exception: {e.ToString()}");
                return false;
            }
        }
        public static bool IsBeard(bool gender, InventoryItemData item)
        {
            int Variation = -1;
            if (item.ItemId == ItemId.Mask) Variation = Convert.ToInt32(item.Data.Split('_')[0]);

            var MaskData = Chars.ClothesComponents.ClothesComponentData[gender][Chars.ClothesComponent.Masks];
            if (Variation != -1 && MaskData.ContainsKey(Variation) && MaskData[Variation].Donate > 0)
                return false;
            else if (Variation != -1 &&
                     Variation != 125 &&
                     Variation != 127 &&
                     Variation != 196 &&
                     Variation != 197 &&
                     Variation != 204 &&
                     Variation != 182 &&
                     Variation != 183 &&
                     Variation != 190 &&
                     Variation != 210 &&
                     Variation != 209 &&
                     Variation != 224 &&
                     Variation != 223 &&
                     Variation != 225 &&
                     Variation != 228 &&
                     Variation != 229 &&
                     Variation != 230 &&
                     Variation != 235 &&
                     Variation != 236 &&
                     Variation != 237 &&
                     Variation != 238 &&
                     Variation != 239 &&
                     Variation != 227 &&
                     Variation != 232 &&
                     Variation != 240 &&
                     Variation != 241 &&
                     Variation != 242 &&
                     Variation != 233 &&
                     Variation != 231 &&
                     Variation != 215 &&
                     Variation != 216 &&
                     Variation != 217 &&
                     Variation != 243 &&
                     Variation != 244 &&
                     Variation != 212)
                return true;

            return false;
        }
        public static void RemoveAllWeapons(ExtPlayer player, bool ammo, bool styawki = false, bool armour = false)
        {
            try
            {
                var characterData = player.GetCharacterData();
                if (characterData == null) return;

                string locationName = $"char_{characterData.UUID}";
                InventoryItemData Bags = null;

                List<ItemStruct> _Items = new List<ItemStruct>();
                if (ItemsData.ContainsKey(locationName))
                {
                    foreach (string Location in ItemsData[locationName].Keys)
                    {
                        //if (Location.Key == "accessories") continue;
                        foreach (var itemData in ItemsData[locationName][Location])
                        {
                            if (itemData.Value.ItemId == ItemId.Bag) Bags = itemData.Value;
                            if (itemData.Value.ItemId == ItemId.Wrench || itemData.Value.ItemId == ItemId.Flashlight || itemData.Value.ItemId == ItemId.Ball || itemData.Value.Data == "126_0_True") continue;

                            InventoryItemData item = itemData.Value;
                            ItemsInfo ItemInfo = ItemsInfo[item.ItemId];
                            if (ItemInfo.functionType == newItemType.Weapons ||
                                ItemInfo.functionType == newItemType.MeleeWeapons ||
                                item.ItemId == ItemId.StunGun ||
                                (ammo && ItemInfo.functionType == newItemType.Ammo) ||
                                (styawki == true && item.ItemId == ItemId.Cuffs) ||
                                (armour == true && item.ItemId == ItemId.BodyArmor))
                            {
                                _Items.Add(new ItemStruct(Location, itemData.Key, item));
                            }
                        }
                    }
                }
                RemoveFix(player, locationName, _Items);
                if (Bags != null && ItemsData.ContainsKey($"backpack_{Bags.SqlId}") && ItemsData[$"backpack_{Bags.SqlId}"].ContainsKey("backpack"))
                {
                    _Items = new List<ItemStruct>();
                    foreach (var itemData in ItemsData[$"backpack_{Bags.SqlId}"]["backpack"])
                    {
                        if (itemData.Value.ItemId == ItemId.Wrench || itemData.Value.ItemId == ItemId.Flashlight || itemData.Value.ItemId == ItemId.Ball) continue;

                        InventoryItemData item = itemData.Value;
                        ItemsInfo ItemInfo = ItemsInfo[item.ItemId];
                        if (ItemInfo.functionType == newItemType.Weapons ||
                            ItemInfo.functionType == newItemType.MeleeWeapons ||
                            item.ItemId == ItemId.StunGun ||
                            (ammo && ItemInfo.functionType == newItemType.Ammo) ||
                            (styawki == true && item.ItemId == ItemId.Cuffs) ||
                            (armour == true && item.ItemId == ItemId.BodyArmor))
                        {
                            _Items.Add(new ItemStruct("backpack", itemData.Key, item));
                        }
                    }
                    RemoveFix(player, $"backpack_{Bags.SqlId}", _Items);
                }
                Trigger.ClientEvent(player, "removeAllWeapons");
                player.RemoveAllWeapons();
            }
            catch (Exception e)
            {
                Log.Write($"RemoveAllWeapons Exception: {e.ToString()}");
            }
        }
        public static void RemoveFix(ExtPlayer player, string locationName, List<ItemStruct> Items)
        {
            try
            {
                var characterData = player.GetCharacterData();
                if (player != null && characterData == null) return;
                bool UpdateClothes = false;
                if (ItemsData.ContainsKey(locationName))
                {
                    foreach (ItemStruct ItemStruct in Items)
                    {
                        if (!ItemsData[locationName].ContainsKey(ItemStruct.Location)) continue;
                        else if (!ItemsData[locationName][ItemStruct.Location].ContainsKey(ItemStruct.Index)) continue;

                        InventoryItemData item = ItemsData[locationName][ItemStruct.Location][ItemStruct.Index];
                        if (item.SqlId == ItemStruct.Item.SqlId)
                        {
                            if (ItemStruct.Item.Count == item.Count)
                            {
                                ItemId ItemIdDell = item.ItemId;
                                item.ItemId = ItemId.Debug;
                                UpdateSqlItemData(locationName, ItemStruct.Location, ItemStruct.Index, item, ItemIdDell);
                                if (player != null)
                                    UpdatePlayerItemData(player, locationName, ItemStruct.Location, ItemStruct.Index, new InventoryItemData());

                                ItemsOtherUpdate(player, locationName, ItemStruct.Location, ItemStruct.Index, new InventoryItemData());
                                if (player != null && ItemStruct.Location == "accessories")
                                {
                                    AccessoriesUse(player, ItemStruct.Index);
                                    UpdateClothes = true;
                                }
                                ItemsData[locationName][ItemStruct.Location].TryRemove(ItemStruct.Index, out _);
                                // ✅ Обновляем вес после удаления
                               
                                    {
                                        characterData.InventoryWeight = CalculateInventoryWeight(characterData.UUID);
                                        characterData.BackpackWeight = CalculateBackpackWeight(characterData.UUID);
                                    }
                                
                                if (ItemsData[locationName][ItemStruct.Location].Count < 1)
                                    ItemsData[locationName].TryRemove(ItemStruct.Location, out _);
                            }
                            else
                            {
                                item.Count -= ItemStruct.Item.Count;
                                UpdateSqlItemData(locationName, ItemStruct.Location, ItemStruct.Index, item);
                                if (player != null)
                                    UpdatePlayerItemData(player, locationName, ItemStruct.Location, ItemStruct.Index, item);

                                ItemsOtherUpdate(player, locationName, ItemStruct.Location, ItemStruct.Index, item);
                                if (player != null && ItemStruct.Location == "accessories")
                                {
                                    AccessoriesUse(player, ItemStruct.Index);
                                    UpdateClothes = true;
                                }
                            }
                        }
                    }
                }
                if (player != null && UpdateClothes)
                    ClothesComponents.UpdateClothes(player);
            }
            catch (Exception e)
            {
                Log.Write($"RemoveFix Exception: {e.ToString()}");
            }
        }

        public static void RemoveAllIllegalStuff(ExtPlayer player, bool IsRemoveBag = true)
        {
            try
            {
                var characterData = player.GetCharacterData();
                if (characterData == null) return;

                string locationName = $"char_{characterData.UUID}";
                InventoryItemData Bags = null;

                List<ItemStruct> _Items = new List<ItemStruct>();
                foreach (string Location in ItemsData[locationName].Keys)
                {
                    foreach (var itemData in ItemsData[locationName][Location])
                    {
                        if (itemData.Value.ItemId == ItemId.Debug) continue;
                        if (itemData.Value.ItemId == ItemId.Bag) Bags = itemData.Value;
                        InventoryItemData item = itemData.Value;
                        ItemsInfo ItemInfo = ItemsInfo[item.ItemId];
                        if (Police.IllegalsItems.ContainsKey(item.ItemId) || ItemInfo.functionType == newItemType.Ammo)
                        {
                            _Items.Add(new ItemStruct(Location, itemData.Key, item));
                        }
                    }
                }
                RemoveFix(player, locationName, _Items);
                if (IsRemoveBag && Bags != null && ItemsData.ContainsKey($"backpack_{Bags.SqlId}") && ItemsData[$"backpack_{Bags.SqlId}"].ContainsKey("backpack"))
                {
                    _Items = new List<ItemStruct>();
                    foreach (var itemData in ItemsData[$"backpack_{Bags.SqlId}"]["backpack"])
                    {
                        InventoryItemData item = itemData.Value;
                        ItemsInfo ItemInfo = ItemsInfo[item.ItemId];
                        if (Police.IllegalsItems.ContainsKey(item.ItemId) || ItemInfo.functionType == newItemType.Ammo)
                        {
                            _Items.Add(new ItemStruct("backpack", itemData.Key, item));
                        }
                    }
                    RemoveFix(player, $"backpack_{Bags.SqlId}", _Items);
                }
                Trigger.ClientEvent(player, "removeAllWeapons");
                player.RemoveAllWeapons();
            }
            catch (Exception e)
            {
                Log.Write($"RemoveAllIllegalStuff Exception: {e.ToString()}");
            }
        }
        public static void RemoveAllClothes(ExtPlayer player)
        {
            try
            {
                var characterData = player.GetCharacterData();
                if (characterData == null) return;

                string locationName = $"char_{characterData.UUID}";
                InventoryItemData Bags = null;

                List<ItemStruct> _Items = new List<ItemStruct>();
                foreach (string Location in ItemsData[locationName].Keys)
                {
                    if (Location == "fastSlots") continue;
                    foreach (var itemData in ItemsData[locationName][Location])
                    {
                        InventoryItemData item = itemData.Value;
                        if (item.ItemId == ItemId.Bag) Bags = item;
                        ItemsInfo ItemInfo = ItemsInfo[item.ItemId];
                        //if (ItemInfo.functionType == newItemType.Clothes && item.ItemId != ItemId.Bag &&
                        //    (item.ItemId != ItemId.Mask || IsBeard(characterData.Gender, item)))
                        //{
                        //    _Items.Add(new ItemStruct(Location, itemData.Key, item));
                        //}
                    }
                }
                RemoveFix(player, locationName, _Items);
                if (Bags != null && ItemsData.ContainsKey($"backpack_{Bags.SqlId}") && ItemsData[$"backpack_{Bags.SqlId}"].ContainsKey("backpack"))
                {
                    _Items = new List<ItemStruct>();
                    foreach (var itemData in ItemsData[$"backpack_{Bags.SqlId}"]["backpack"])
                    {
                        InventoryItemData item = itemData.Value;
                        ItemsInfo ItemInfo = ItemsInfo[item.ItemId];
                        if (ItemInfo.functionType == newItemType.Clothes &&
                            (item.ItemId != ItemId.Mask || IsBeard(characterData.Gender, item)))
                        {
                            _Items.Add(new ItemStruct("backpack", itemData.Key, item));
                        }
                    }
                    RemoveFix(player, $"backpack_{Bags.SqlId}", _Items);
                }
                Trigger.ClientEvent(player, "removeAllWeapons");
                player.RemoveAllWeapons();
            }
            catch (Exception e)
            {
                Log.Write($"RemoveAllClothes Exception: {e.ToString()}");
            }
        }
        public static void RemoveAll(string locationName)
        {
            try
            {
                if (ItemsData.ContainsKey(locationName))
                {
                    foreach (var _ItemsData in ItemsData[locationName].Values)
                    {
                        foreach (InventoryItemData itemData in _ItemsData.Values)
                        {
                            OnDellItem(itemData.ItemId, itemData.Data);

                            GameLog.Items($"deletedItem({itemData.SqlId})", locationName, (int)itemData.ItemId, itemData.Count, itemData.Data);
                            Database.Models.Items.AddItemDelete(itemData.SqlId);
                        }
                    }
                    ItemsData.TryRemove(locationName, out _);
                }
                ItemsAllClose(locationName);
                if (InventoryOtherPlayers.ContainsKey(locationName))
                    InventoryOtherPlayers.TryRemove(locationName, out _);
            }
            catch (Exception e)
            {
                Log.Write($"RemoveAll Exception: {e.ToString()}");
            }
        }

        public static IReadOnlyDictionary<string, int> InventoryMaxSlots = new Dictionary<string, int>()
        {
            { "accessories", 15 },
            { "inventory", 102 },
            { "backpack", 48 },
            { "fastSlots", 3 },
            { "trade", 8 },
            { "vehicle", 25 },//76
            { "CarKey", 38 },
            { "Fraction", 300 },
            { "Organization", 300 },
            { "furniture", 25 },
            { "tent", 16 },
            { "marketStorage", EternalDev.MarketPlace.Manager.Config.MaxSlotsInStorage },
        };
        public const int MaxSlotsInventory = 102;
        public static int GetMaxSlots(ExtPlayer player, string Location)
        {
            if (Location == "backpack" && player.IsCharacterData())
            {

                InventoryItemData Bags = GetItemData(player, "accessories", 8);
                if (Bags.ItemId == ItemId.Bag)
                {
                    int maxSlots = 48;
                    Dictionary<string, int> PlayerBugData = Bags.GetData();
                    ConcurrentDictionary<int, ClothesData> ShoesData = ClothesComponents.ClothesBugsData;
                    if (ShoesData.ContainsKey(PlayerBugData["Variation"]) && ShoesData[PlayerBugData["Variation"]].MaxSlots > 0) maxSlots = ShoesData[PlayerBugData["Variation"]].MaxSlots;
                    return maxSlots;
                }
            }
            if (InventoryMaxSlots.ContainsKey(Location))
            {
                return InventoryMaxSlots[Location];
            }
            return MaxSlotsInventory;
        }
        public static int AddItem(ExtPlayer player, string locationName, string Location, InventoryItemData Item, int MaxSlots = MaxSlotsInventory, bool isWarehouse = false)
        {
            try
            {
                if (!player.IsCharacterData()) return -1;
                int success = -1;
                AddInventoryArray(locationName, Location);

                // ✅ СТАКАНИЕ
                if (ItemsInfo[Item.ItemId].Stack > 1)
                {
                    foreach (int SlotId in ItemsData[locationName][Location].Keys)
                    {
                        InventoryItemData item = ItemsData[locationName][Location][SlotId];
                        if (item.ItemId == Item.ItemId && ItemsInfo[Item.ItemId].Stack > item.Count)
                        {
                            if (item.Price != Item.Price) continue;
                            if (item.IsTurn != Item.IsTurn) continue;

                            if (ItemsInfo[Item.ItemId].Stack >= (item.Count + Item.Count))
                            {
                                item.Count += Item.Count;
                                Item.Count = 0;
                            }
                            else
                            {
                                Item.Count = (item.Count + Item.Count) - ItemsInfo[Item.ItemId].Stack;
                                item.Count = ItemsInfo[Item.ItemId].Stack;
                            }

                            UpdateSqlItemData(locationName, Location, SlotId, item);
                            UpdatePlayerItemData(player, locationName, Location, SlotId, item);
                            ItemsOtherUpdate(player, locationName, Location, SlotId, item);

                            if (Item.Count == 0) return SlotId;
                            else success = SlotId;
                        }
                    }
                }

                // ✅ ПОИСК СВОБОДНОГО МЕСТА
                int itemWidth = ItemsInfo[Item.ItemId].Width;
                int itemHeight = ItemsInfo[Item.ItemId].Height;

                int maxCols = Location switch
                {
                    "inventory" => 6,
                    "backpack" => 6,
                    "other" => 6,
                    _ => 5
                };

                int maxRows = MaxSlots / maxCols;

                for (int i = 0; i < MaxSlots; i++)
                {
                    if (!ItemsData[locationName][Location].ContainsKey(i) || ItemsData[locationName][Location][i].ItemId == ItemId.Debug)
                    {
                        int startX = i % maxCols;
                        int startY = i / maxCols;

                        bool canPlace = true;

                        if (startX + itemWidth > maxCols || startY + itemHeight > maxRows)
                        {
                            canPlace = false;
                        }
                        else
                        {
                            // ✅ ПРОВЕРЯЕМ ЗАГЛУШКИ
                            for (int y = 0; y < itemHeight; y++)
                            {
                                for (int x = 0; x < itemWidth; x++)
                                {
                                    int checkIndex = (startY + y) * maxCols + (startX + x);

                                    if (ItemsData[locationName][Location].ContainsKey(checkIndex))
                                    {
                                        var existingItem = ItemsData[locationName][Location][checkIndex];

                                        // ✅ Слот занят, если там реальный предмет ИЛИ заглушка
                                        if (existingItem.ItemId != ItemId.Debug ||
                                            (existingItem.Data != null && existingItem.Data.StartsWith("placeholder_")))
                                        {
                                            canPlace = false;
                                            break;
                                        }
                                    }
                                }
                                if (!canPlace) break;
                            }
                        }

                        if (canPlace)
                        {
                            if (canPlace)
                            {
                                if (Location == "fastSlots" && i == 4 && Item.ItemId != ItemId.Mask) continue;
                              

                                // ✅ УДАЛЯЕМ СТАРЫЕ ЗАГЛУШКИ ТОЛЬКО ЕСЛИ ЭТО ЗАМЕНА!
                                if (ItemsData[locationName][Location].ContainsKey(i) && ItemsData[locationName][Location][i].ItemId != ItemId.Debug)
                                {
                                    var oldItem = ItemsData[locationName][Location][i];
                                    DeletePlaceholdersForSlot(player, locationName, Location, i, forceDelete: true);

                                }

                                ItemsData[locationName][Location][i] = new InventoryItemData(Item.SqlId, Item.ItemId, Item.Count, Item.Data, i);
                                ItemsData[locationName][Location][i].Price = Item.Price;
                                ItemsData[locationName][Location][i].IsTurn = Item.IsTurn;

                                /// ✅ СОЗДАЁМ ЗАГЛУШКИ ТОЛЬКО ДЛЯ ИНВЕНТАРЯ И РЮКЗАКА
                                if ((Location == "inventory" || Location == "backpack") && (itemWidth > 1 || itemHeight > 1))
                                {
                                    for (int y = 0; y < itemHeight; y++)
                                    {
                                        for (int x = 0; x < itemWidth; x++)
                                        {
                                            if (x == 0 && y == 0) continue;

                                            int placeholderIndex = (startY + y) * maxCols + (startX + x);

                                            if (!ItemsData[locationName][Location].ContainsKey(placeholderIndex))
                                            {
                                                var placeholder = new InventoryItemData(
                                                    SqlId: -1,
                                                    ItemId: ItemId.Debug,
                                                    Count: 0,
                                                    Data: $"placeholder_{i}",
                                                    Index: placeholderIndex
                                                );

                                                ItemsData[locationName][Location][placeholderIndex] = placeholder;

                                                Trigger.SetTask(() =>
                                                {
                                                    SavePlaceholderToDatabase(locationName, Location, placeholderIndex, $"placeholder_{i}");
                                                });
                                            }
                                        }
                                    }
                                }

                                UpdateSqlItemData(locationName, Location, i, ItemsData[locationName][Location][i]);
                                UpdatePlayerItemData(player, locationName, Location, i, ItemsData[locationName][Location][i]);
                                ItemsOtherUpdate(player, locationName, Location, i, ItemsData[locationName][Location][i]);

                                if (player.IsCharacterData())
                                {
                                    var characterData = player.GetCharacterData();
                                    if (characterData != null)
                                    {
                                        characterData.InventoryWeight = CalculateInventoryWeight(characterData.UUID);
                                        characterData.BackpackWeight = CalculateBackpackWeight(characterData.UUID);
                                    }
                                }

                                return i;
                            }
                        }
                        }
                }

                if (isWarehouse)
                    AddItemWarehouse(player, Item, 10000);

                return success;
            }
            catch (Exception e)
            {
                Log.Write($"AddItem Exception: {e.ToString()}");
                return -1;
            }
        }

        // ✅ НОВАЯ ФУНКЦИЯ: Очистка заглушек по слоту
        // ✅ ИСПРАВЛЕНО: Добавлен параметр player

        public static int AddNewItem(ExtPlayer player, string locationName, string Location, ItemId ItemId, int count = 1, string ItemData = "", bool stack = true, int MaxSlots = MaxSlotsInventory, bool isInfo = true, int price = 0, bool addInWarehouse = false)
        {
            try
            {
                if (player != null && !player.IsCharacterData()) return -1;
                if (!CanAddItemByWeight(player, Location, ItemId, count))
                    return -1;
                if (Location == "weapon")
                {
                    int freeSlot = GetFreeSlot(locationName, Location);
                    AddSqlItem(player, locationName, Location, ItemId, freeSlot, count, ItemData, price);
                    return freeSlot;
                }
                int success = -1;
                AddInventoryArray(locationName, Location);

                // ✅ СТАКАНИЕ (не трогаем)
                if (ItemsInfo[ItemId].Stack > 1 && stack)
                {
                    foreach (int SlotId in ItemsData[locationName][Location].Keys)
                    {
                        InventoryItemData item = ItemsData[locationName][Location][SlotId];

                        // ✅ ПРОВЕРЯЕМ, ЧТО ПРЕДМЕТ НЕ ПОМЕЧЕН НА УДАЛЕНИЕ
                        if (Database.Models.Items.IsItemUpdate(item.SqlId))
                        {
                            Log.Write($"[AddNewItem] Skipping SqlId={item.SqlId} (marked for update/delete)");
                            continue;
                        }

                        if (item.ItemId == ItemId && ItemsInfo[ItemId].Stack > item.Count)
                        {
                            if (price != item.Price) continue;
                            if (item.IsTurn != false) continue;

                            if (ItemsInfo[ItemId].Stack >= (item.Count + count))
                            {
                                item.Count += count;
                                count = 0;
                            }
                            else
                            {
                                count = (item.Count + count) - ItemsInfo[ItemId].Stack;
                                item.Count = ItemsInfo[ItemId].Stack;
                            }

                            UpdateSqlItemData(locationName, Location, SlotId, item);
                            if (player != null) UpdatePlayerItemData(player, locationName, Location, SlotId, item, isInfo);
                            ItemsOtherUpdate(player, locationName, Location, SlotId, item);

                            if (player != null && player.IsCharacterData())
                            {
                                var characterData = player.GetCharacterData();
                                if (characterData != null)
                                {
                                    characterData.InventoryWeight = CalculateInventoryWeight(characterData.UUID);
                                    characterData.BackpackWeight = CalculateBackpackWeight(characterData.UUID);
                                }
                            }

                            if (count == 0) return SlotId;
                            else success = SlotId;
                        }
                    }
                }

                // ✅ ПОИСК СВОБОДНОГО МЕСТА (БЕЗ ЗАГЛУШЕК!)
                if (ItemsInfo[ItemId].Stack > 1 && stack)
                {
                    foreach (int SlotId in ItemsData[locationName][Location].Keys)
                    {
                        InventoryItemData item = ItemsData[locationName][Location][SlotId];
                        if (item.ItemId == ItemId && ItemsInfo[ItemId].Stack > item.Count)
                        {
                            if (item.Price != price) continue;
                            if (item.IsTurn != false) continue;

                            if (ItemsInfo[ItemId].Stack >= (item.Count + count))
                            {
                                item.Count += count;
                                count = 0;
                            }
                            else
                            {
                                count = (item.Count + count) - ItemsInfo[ItemId].Stack;
                                item.Count = ItemsInfo[ItemId].Stack;
                            }

                            UpdateSqlItemData(locationName, Location, SlotId, item);
                            if (player != null) UpdatePlayerItemData(player, locationName, Location, SlotId, item, isInfo);
                            ItemsOtherUpdate(player, locationName, Location, SlotId, item);

                            if (player != null && player.IsCharacterData())
                            {
                                var characterData = player.GetCharacterData();
                                if (characterData != null)
                                {
                                    characterData.InventoryWeight = CalculateInventoryWeight(characterData.UUID);
                                    characterData.BackpackWeight = CalculateBackpackWeight(characterData.UUID);
                                }
                            }

                            if (count == 0) return SlotId;
                            else success = SlotId;
                        }
                    }
                }

                // ✅ ПРОВЕРКА: Для vehicle/warehouse НЕ нужны клетки
                if (Location == "vehicle" || Location == "warehouse" || Location == "tent")
                {
                    // ✅ ДОБАВЛЯЕМ В СЛЕДУЮЩИЙ СВОБОДНЫЙ СЛОТ
                    int freeSlot = GetFreeSlot(locationName, Location);

                    if (count > ItemsInfo[ItemId].Stack)
                    {
                        AddSqlItem(player, locationName, Location, ItemId, freeSlot, ItemsInfo[ItemId].Stack, ItemData, price);
                        count -= ItemsInfo[ItemId].Stack;
                    }
                    else
                    {
                        AddSqlItem(player, locationName, Location, ItemId, freeSlot, count, ItemData, price);
                        count = 0;
                    }

                    if (count == 0) return freeSlot;
                    else success = freeSlot;
                }

                // ✅ ДЛЯ ИНВЕНТАРЯ/РЮКЗАКА — ИЩЕМ СВОБОДНЫЕ КЛЕТКИ
                int itemWidth = ItemsInfo[ItemId].Width;
                int itemHeight = ItemsInfo[ItemId].Height;

                int maxCols = Location switch
                {
                    "inventory" => 6,
                    "backpack" => 6,
                    "other" => 6,
                    _ => 5
                };

                int maxRows = MaxSlots / maxCols;

                // ✅ СОЗДАЁМ МАТРИЦУ ЗАНЯТОСТИ (БЕЗ ЗАГЛУШЕК!)
                bool[,] matrix = new bool[maxRows, maxCols];

                var items = ItemsData[locationName][Location];
                foreach (var kvp in items)
                {
                    var item = kvp.Value;
                    if (item.ItemId == ItemId.Debug) continue; // ❌ ИГНОРИРУЕМ ЗАГЛУШКИ

                    var itemConfig = ItemsInfo[item.ItemId];
                    int x = item.Index % maxCols;
                    int y = item.Index / maxCols;

                    int width = item.IsTurn ? itemConfig.Height : itemConfig.Width;
                    int height = item.IsTurn ? itemConfig.Width : itemConfig.Height;

                    for (int dy = 0; dy < height; dy++)
                    {
                        for (int dx = 0; dx < width; dx++)
                        {
                            int checkY = y + dy;
                            int checkX = x + dx;
                            if (checkY < maxRows && checkX < maxCols)
                            {
                                matrix[checkY, checkX] = true;
                            }
                        }
                    }
                }

                // ✅ ИЩЕМ СВОБОДНОЕ МЕСТО
                for (int i = 0; i < MaxSlots; i++)
                {
                    int startX = i % maxCols;
                    int startY = i / maxCols;

                    if (startX + itemWidth > maxCols || startY + itemHeight > maxRows)
                        continue;

                    bool canPlace = true;
                    for (int y = 0; y < itemHeight; y++)
                    {
                        for (int x = 0; x < itemWidth; x++)
                        {
                            int checkY = startY + y;
                            int checkX = startX + x;
                            if (matrix[checkY, checkX])
                            {
                                canPlace = false;
                                break;
                            }
                        }
                        if (!canPlace) break;
                    }

                    if (canPlace)
                    {
                        if (count > ItemsInfo[ItemId].Stack)
                        {
                            AddSqlItem(player, locationName, Location, ItemId, i, ItemsInfo[ItemId].Stack, ItemData, price: price); // ✅ Явно указываем параметр
                            count -= ItemsInfo[ItemId].Stack;
                        }
                        else
                        {
                            AddSqlItem(player, locationName, Location, ItemId, i, count, ItemData, price);
                            count = 0;
                        }

                        if (player != null && player.IsCharacterData())
                        {
                            var characterData = player.GetCharacterData();
                            if (characterData != null)
                            {
                                characterData.InventoryWeight = CalculateInventoryWeight(characterData.UUID);
                                characterData.BackpackWeight = CalculateBackpackWeight(characterData.UUID);
                            }
                        }

                        if (count == 0) return i;
                        else success = i;
                    }
                }

                if (success == -1 && addInWarehouse)
                {
                    AddNewItemWarehouse(player, ItemId, count, ItemData);
                    Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.NoSpaceInventoryThenSclad), 10000);
                }

                return success;
            }
            catch (Exception e)
            {
                Log.Write($"AddNewItem Exception: {e.ToString()}");
                return -1;
            }
        }
        public static void AddItemWarehouse(ExtPlayer player, InventoryItemData Item, int MaxSlots = MaxSlotsInventory)
        {
            try
            {
                var characterData = player.GetCharacterData();
                if (characterData == null) return;

                string locationName = $"warehouse_{characterData.UUID}";
                string Location = "warehouse";

                AddInventoryArray(locationName, Location);
                if (ItemsInfo[Item.ItemId].Stack > 1)
                {
                    foreach (int SlotId in ItemsData[locationName][Location].Keys)
                    {
                        InventoryItemData item = ItemsData[locationName][Location][SlotId];
                        if (item.ItemId == Item.ItemId && ItemsInfo[Item.ItemId].Stack > item.Count)
                        {
                            if (ItemsInfo[Item.ItemId].Stack >= (item.Count + Item.Count))
                            {
                                item.Count += Item.Count;
                                Item.Count = 0;
                            }
                            else
                            {
                                Item.Count = (item.Count + Item.Count) - ItemsInfo[Item.ItemId].Stack;
                                item.Count = ItemsInfo[Item.ItemId].Stack;
                            }
                            ItemId ItemIdDell = Item.ItemId;
                            UpdateSqlItemData(locationName, Location, SlotId, item);
                            if (Item.Count == 0)
                            {
                                Item.ItemId = ItemId.Debug;
                                UpdateSqlItemData(locationName, Location, Item.Index, Item, ItemIdDell);
                                return;
                            }
                        }
                    }
                }

                for (int i = 0; i < MaxSlots; i++)
                {
                    if (!ItemsData[locationName][Location].ContainsKey(i) || ItemsData[locationName][Location][i].ItemId == ItemId.Debug)
                    {
                        if (Location == "fastSlots" && i == 4 && Item.ItemId != ItemId.Mask) continue;

                        if (ItemsData[locationName][Location].ContainsKey(i))
                            ItemsData[locationName][Location].TryRemove(i, out _);

                        ItemsData[locationName][Location].TryAdd(i, new InventoryItemData(Item.SqlId, Item.ItemId, Item.Count, Item.Data, i));
                        UpdateSqlItemData(locationName, Location, i, ItemsData[locationName][Location][i]);
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Write($"AddItem Exception: {e.ToString()}");
            }
        }
        public static void AddNewItemWarehouse(ExtPlayer player, ItemId ItemId, int count = 1, string ItemData = "")
        {
            try
            {
                if (player == null)
                    return;

                var uuid = player.GetUUID();

                OnAddItem(ItemId, ItemData);

                string locationName = $"warehouse_{uuid}";
                string Location = "warehouse";

                short SlotId = (short)GetItemsDataLastId(locationName, Location);

                if (!ItemsData.ContainsKey(locationName))
                    ItemsData.TryAdd(locationName, new ConcurrentDictionary<string, ConcurrentDictionary<int, InventoryItemData>>());

                if (!ItemsData[locationName].ContainsKey(Location))
                    ItemsData[locationName].TryAdd(Location, new ConcurrentDictionary<int, InventoryItemData>());

                if (ItemsData[locationName][Location].ContainsKey(SlotId))
                    ItemsData[locationName][Location].TryRemove(SlotId, out _);

                ItemsData[locationName][Location].TryAdd(SlotId, new InventoryItemData(-1, ItemId, count, ItemData, SlotId));
                Players.Phone.Messages.Repository.AddSystemMessage(player, (int)DefaultNumber.Warehouse, LangFunc.GetText(LangType.Ru, DataName.Posilka, Chars.Repository.ItemsInfo[ItemId].Name), DateTime.Now);

                Trigger.SetTask(() =>
                {
                    AddNewItemWarehouseThread(uuid, ItemId, count, ItemData, SlotId);
                });
            }
            catch (Exception e)
            {
                Log.Write($"AddNewItemWarehouse Exception: {e.ToString()}");
            }
        }
        public static async void AddNewItemWarehouseThread(int uuid, ItemId ItemId, int count = 1, string ItemData = "", short SlotId = 0, sbyte IsTurn = 0)
        {
            try
            {
                string locationName = $"warehouse_{uuid}";
                string Location = "warehouse";

                await using var db = new ServerBD("MainDB");//В отдельном потоке

                int itemSqlID = await db.InsertWithInt32IdentityAsync(new ItemsData
                {
                    DataId = locationName,
                    ItemId = (short)ItemId,
                    ItemCount = (short)count,
                    ItemData = ItemData,
                    Location = Location,
                    SlotId = SlotId,
                    IsTurn = IsTurn,

                });
                ItemsData[locationName][Location][SlotId].SqlId = itemSqlID;
                GameLog.Items($"newItem({itemSqlID})", locationName, (int)ItemId, count, ItemData);
            }
            catch (Exception e)
            {
                Log.Write($"AddNewItemWarehouse Exception: {e.ToString()}");
            }
        }

        public static async void AddNewItemWarehouseThread(ServerBD db, int uuid, ItemId ItemId, int count = 1, string ItemData = "", short SlotId = 0)
        {
            try
            {
                string locationName = $"warehouse_{uuid}";
                string Location = "warehouse";

                int itemSqlID = await db.InsertWithInt32IdentityAsync(new ItemsData
                {
                    DataId = locationName,
                    ItemId = (short)ItemId,
                    ItemCount = (short)count,
                    ItemData = ItemData,
                    Location = Location,
                    SlotId = SlotId,
                });
                ItemsData[locationName][Location][SlotId].SqlId = itemSqlID;
                GameLog.Items($"newItem({itemSqlID})", locationName, (int)ItemId, count, ItemData);
            }
            catch (Exception e)
            {
                Log.Write($"AddNewItemWarehouse Exception: {e.ToString()}");
            }
        }
        public static void AddSqlItem(ExtPlayer player, string locationName, string Location, ItemId ItemId, int Index, int count = 1, string ItemData = "", int price = 0)
        {
            try
            {
                Log.Write($"[AddSqlItem] Adding item: Location={Location}, Index={Index}, ItemId={ItemId}, Count={count}", nLog.Type.Warn);

                AddInventoryArray(locationName, Location);
                OnAddItem(ItemId, ItemData);

                // ✅ ИСПРАВЛЕНО: Явно передаём IsTurn
                bool isTurn = false; // По умолчанию — не повёрнут

                ItemsData[locationName][Location][Index] = new InventoryItemData(-1, ItemId, count, ItemData, Index)
                {
                    Price = price,
                    IsTurn = isTurn // ✅ СОХРАНЯЕМ
                };

                // ✅ ЗАГЛУШКИ СОЗДАЁМ ТОЛЬКО ДЛЯ ИНВЕНТАРЯ И РЮКЗАКА
                if (Location == "inventory" || Location == "backpack")
                {
                    CreatePlaceholdersForSlot(player, locationName, Location, Index, ItemId, isTurn); // ✅ ПЕРЕДАЁМ isTurn
                }

                Trigger.SetTask(() =>
                {
                    AddSqlItemThread(player, locationName, Location, ItemId, Index, count, ItemData, isTurn); // ✅ ПЕРЕДАЁМ isTurn
                });
            }
            catch (Exception e)
            {
                Log.Write($"AddSqlItem Exception: {e.ToString()}");
            }
        }

        // ✅ НОВЫЙ МЕТОД: Сохранение заглушки в БД
        public static async void SavePlaceholderToDatabase(string locationName, string location, int slotId, string data)
        {
            try
            {
                await using var db = new ServerBD("MainDB");

                int placeholderSqlID = await db.InsertWithInt32IdentityAsync(new ItemsData
                {
                    DataId = locationName,
                    ItemId = 0, // ItemId.Debug
                    ItemCount = 0,
                    ItemData = data,
                    Location = location,
                    SlotId = (short)slotId,
                    IsTurn = 0
                });

                if (ItemsData.ContainsKey(locationName) &&
                    ItemsData[locationName].ContainsKey(location) &&
                    ItemsData[locationName][location].ContainsKey(slotId))
                {
                    ItemsData[locationName][location][slotId].SqlId = placeholderSqlID;
                }
            }
            catch (Exception e)
            {
                Log.Write($"SavePlaceholderToDatabase Exception: {e.ToString()}");
            }
        }
        public static async void AddSqlItemThread(ExtPlayer player, string locationName, string Location, ItemId ItemId, int Index, int count = 1, string ItemData = "", bool isTurn = false) // ✅ ДОБАВЛЕН ПАРАМЕТР
        {
            try
            {
                await using var db = new ServerBD("MainDB");

                int itemSqlID = await db.InsertWithInt32IdentityAsync(new ItemsData
                {
                    DataId = locationName,
                    ItemId = (short)ItemId,
                    ItemCount = (short)count,
                    ItemData = ItemData,
                    Location = Location,
                    SlotId = (short)Index,
                    IsTurn = (sbyte)(isTurn ? 1 : 0) // ✅ ИСПОЛЬЗУЕМ ПАРАМЕТР
                });

                var itemData = ItemsData[locationName][Location][Index];
                itemData.SqlId = itemSqlID;
                GameLog.Items($"newItem({itemSqlID})", locationName, (int)ItemId, count, ItemData);

                NAPI.Task.Run(() =>
                {
                    try
                    {
                        if (InventoryLocation.ContainsKey(Location) && player.IsCharacterData())
                        {
                            SetItemData(player, Location, Index, itemData, send: true, isSqlUpdate: false, isInfo: true);

                            // ✅ ОТПРАВЛЯЕМ ЗАГЛУШКИ КЛИЕНТУ
                            var itemInfo = ItemsInfo[ItemId];
                            if (itemInfo.Width > 1 || itemInfo.Height > 1)
                            {
                                int maxCols = Location switch
                                {
                                    "inventory" => 6,
                                    "backpack" => 6,
                                    "other" => 6,
                                    _ => 5
                                };

                                int startX = Index % maxCols;
                                int startY = Index / maxCols;

                                for (int y = 0; y < itemInfo.Height; y++)
                                {
                                    for (int x = 0; x < itemInfo.Width; x++)
                                    {
                                        if (x == 0 && y == 0) continue;

                                        int placeholderIndex = (startY + y) * maxCols + (startX + x);

                                        if (ItemsData[locationName][Location].ContainsKey(placeholderIndex))
                                        {
                                            var placeholder = ItemsData[locationName][Location][placeholderIndex];
                                            UpdatePlayerItemData(player, locationName, Location, placeholderIndex, placeholder);
                                        }
                                    }
                                }
                            }
                        }
                        else if (InventoryOtherPlayers.ContainsKey(locationName))
                        {
                            foreach (ExtPlayer foreachPlayer in InventoryOtherPlayers[locationName])
                            {
                                if (!foreachPlayer.IsCharacterData()) continue;
                                UpdatePlayerItemData(foreachPlayer, locationName, Location, Index, itemData);

                                // ✅ ОТПРАВЛЯЕМ ЗАГЛУШКИ ДРУГИМ ИГРОКАМ
                                var itemInfo = ItemsInfo[ItemId];
                                if (itemInfo.Width > 1 || itemInfo.Height > 1)
                                {
                                    int maxCols = Location switch
                                    {
                                        "inventory" => 6,
                                        "backpack" => 6,
                                        "other" => 6,
                                        _ => 5
                                    };

                                    int startX = Index % maxCols;
                                    int startY = Index / maxCols;

                                    for (int y = 0; y < itemInfo.Height; y++)
                                    {
                                        for (int x = 0; x < itemInfo.Width; x++)
                                        {
                                            if (x == 0 && y == 0) continue;

                                            int placeholderIndex = (startY + y) * maxCols + (startX + x);

                                            if (ItemsData[locationName][Location].ContainsKey(placeholderIndex))
                                            {
                                                var placeholder = ItemsData[locationName][Location][placeholderIndex];
                                                UpdatePlayerItemData(foreachPlayer, locationName, Location, placeholderIndex, placeholder);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Write($"AddSqlItem NAPI.Task.Run Exception: {e.ToString()}");
                    }
                });
            }
            catch (Exception e)
            {
                Log.Write($"AddSqlItem Exception: {e.ToString()}");
            }
        }

        public static ItemStruct isItem(ExtPlayer player, string Location, ItemId ItemId, string Data = null)
        {
            try
            {
                var characterData = player.GetCharacterData();
                if (characterData == null) return null;
                string locationName = $"char_{characterData.UUID}";
                if (ItemsData.ContainsKey(locationName) && ItemsData[locationName].ContainsKey(Location))
                {
                    foreach (var item in ItemsData[locationName][Location])//Todo
                    {
                        if (item.Value.ItemId == ItemId && (Data == null || (Data != null && Data == item.Value.Data)))
                        {
                            return new ItemStruct(Location, item.Key, item.Value);
                        }
                        ;
                    }
                }
                if (Location == "inventory" && ItemsData.ContainsKey(locationName))
                {
                    foreach (string _location in ItemsData[locationName].Keys)
                    {
                        if (_location != "accessories" && _location != "fastSlots") continue;
                        else if (_location == "accessories" && ItemsInfo[ItemId].functionType != newItemType.Clothes && ItemId != ItemId.BagWithDrill & ItemId != ItemId.BagWithMoney) continue;
                        foreach (var item in ItemsData[locationName][_location])//Todo
                        {
                            if (item.Value.ItemId == ItemId && (Data == null || (Data != null && Data == item.Value.Data)))
                            {
                                return new ItemStruct(_location, item.Key, item.Value);
                            }
                            ;
                        }
                    }
                }
                return null;
            }
            catch (Exception e)
            {
                Log.Write($"isItem Exception: {e.ToString()}");
                return null;
            }
        }
        public static ItemStruct isItem(ExtPlayer player, string Location, int sqlId)
        {
            try
            {
                var characterData = player.GetCharacterData();
                if (characterData == null) return null;
                string locationName = $"char_{characterData.UUID}";
                if (ItemsData.ContainsKey(locationName) && ItemsData[locationName].ContainsKey(Location))
                {
                    foreach (var item in ItemsData[locationName][Location])//Todo
                    {
                        if (item.Value.SqlId == sqlId)
                        {
                            return new ItemStruct(Location, item.Key, item.Value);
                        }
                        ;
                    }
                }
                if (Location == "inventory" && ItemsData.ContainsKey(locationName))
                {
                    foreach (string _location in ItemsData[locationName].Keys)
                    {
                        if (_location != "accessories" && _location != "fastSlots") continue;
                        //else if (_location == "accessories" && ItemsInfo[ItemId].functionType != newItemType.Clothes && ItemId != ItemId.BagWithDrill & ItemId != ItemId.BagWithMoney) continue;
                        foreach (var item in ItemsData[locationName][_location])//Todo
                        {
                            if (item.Value.SqlId == sqlId)
                            {
                                return new ItemStruct(_location, item.Key, item.Value);
                            }
                            ;
                        }
                    }
                }
                return null;
            }
            catch (Exception e)
            {
                Log.Write($"isItem Exception: {e.ToString()}");
                return null;
            }
        }
        public static int itemCount(ExtPlayer player, string Location, ItemId ItemId, string Data = null)
        {
            try
            {
                var characterData = player.GetCharacterData();
                if (characterData == null) return 0;
                string locationName = $"char_{characterData.UUID}";
                var count = 0;
                if (ItemsData.ContainsKey(locationName) && ItemsData[locationName].ContainsKey(Location))
                {
                    foreach (var item in ItemsData[locationName][Location])//Todo
                    {
                        if (item.Value.ItemId == ItemId && (Data == null || (Data != null && Data == item.Value.Data)))
                            count++;
                    }
                }
                if (Location == "inventory" && ItemsData.ContainsKey(locationName))
                {
                    foreach (string _location in ItemsData[locationName].Keys)
                    {
                        if (_location != "accessories" && _location != "fastSlots") continue;
                        else if (_location == "accessories" && ItemsInfo[ItemId].functionType != newItemType.Clothes && ItemId != ItemId.BagWithDrill & ItemId != ItemId.BagWithMoney) continue;
                        foreach (var item in ItemsData[locationName][_location])//Todo
                        {
                            if (item.Value.ItemId == ItemId && (Data == null || (Data != null && Data == item.Value.Data)))
                                count++;
                        }
                    }
                }
                return count;
            }
            catch (Exception e)
            {
                Log.Write($"isItem Exception: {e.ToString()}");
                return 0;
            }
        }
        public static ItemStruct isItemOther(string locationName, ItemId ItemId, string Data = null)
        {
            try
            {
                if (!ItemsData.ContainsKey(locationName)) return null;
                if (!ItemsData[locationName].ContainsKey("CarKey")) return null;
                foreach (var item in ItemsData[locationName]["CarKey"])//Todo
                {
                    if (item.Value.ItemId == ItemId && (Data == null || (Data != null && Data == item.Value.Data)))
                    {
                        return new ItemStruct("vehicle", item.Key, item.Value);
                    }
                    ;
                }
                return null;
            }
            catch (Exception e)
            {
                Log.Write($"isItemOther Exception: {e.ToString()}");
                return null;
            }
        }

        //-1 - Если нет слотогв
        //Если 0 то есть

        public static int maxItemCount = 3;

        public static int isFreeSlots(ExtPlayer player, ItemId ItemId, int count = 1, bool send = true, string Location = "inventory", int ignoreSqlId = -1)
        {
            try
            {
                var characterData = player.GetCharacterData();
                if (characterData == null) return 0;

                string locationName = $"char_{characterData.UUID}";
                AddInventoryArray(locationName, Location);

                // ✅ 1. ПРОВЕРКА ВЕСА
                float maxWeight = Location switch
                {
                    "inventory" => MaxInventoryWeight,
                    "backpack" => MaxBackpackWeight,
                    "warehouse" => MaxWarehouseWeight,
                    "vehicle" => GetVehicleMaxWeight(player),
                    _ => MaxInventoryWeight
                };

                float currentWeight = GetCurrentWeight(locationName, Location);
                float itemWeight = ItemsInfo[ItemId].Weight * count;

                if (currentWeight + itemWeight > maxWeight)
                {
                    if (send)
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter,
                            $"Слишком тяжело! ({currentWeight + itemWeight:F1}/{maxWeight} кг)", 3000);
                    return -1;
                }

                // ✅ 2. ПРОВЕРКА КЛЕТОК (ТОЛЬКО ДЛЯ ИНВЕНТАРЯ И РЮКЗАКА!)
                if (Location == "vehicle" || Location == "warehouse" || Location == "tent")
                {
                    // ✅ ДЛЯ БАГАЖНИКА/СКЛАДА — ПРОВЕРЯЕМ ТОЛЬКО ВЕС
                    return 0;
                }

                // ✅ ДЛЯ ИНВЕНТАРЯ/РЮКЗАКА — ПРОВЕРЯЕМ КЛЕТКИ
                int itemWidth = ItemsInfo[ItemId].Width;
                int itemHeight = ItemsInfo[ItemId].Height;

                int slotsNeeded = 1;
                if (ItemsInfo[ItemId].Stack > 1 && count > ItemsInfo[ItemId].Stack)
                {
                    slotsNeeded = (int)Math.Ceiling((double)count / ItemsInfo[ItemId].Stack);
                }

                if (!HasFreeSlotsWithMatrix(locationName, Location, itemWidth, itemHeight, slotsNeeded, ignoreSqlId))
                {
                    if (send)
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter,
                            LangFunc.GetText(LangType.Ru, DataName.NoSpaceInventory), 3000);
                    return -1;
                }

                return 0;
            }
            catch (Exception e)
            {
                Log.Write($"isFreeSlots Exception: {e.ToString()}");
                return 0;
            }
        }

        // ✅ НОВАЯ ФУНКЦИЯ: ПРОВЕРКА СВОБОДНЫХ КЛЕТОК
        private static bool HasFreeSlotsWithMatrix(string locationName, string location, int itemWidth, int itemHeight, int slotsNeeded, int ignoreSqlId)
        {
            int maxCols = 6;
            int maxRows = location switch
            {
                "inventory" => 17,
                "backpack" => 6,
                "other" => 19,
                _ => 17
            };

            // ✅ СОЗДАЁМ МАТРИЦУ ЗАНЯТОСТИ
            bool[,] matrix = new bool[maxRows, maxCols];

            // ✅ ИСПРАВЛЕНО: InventoryData → ItemsData
            var items = ItemsData.GetValueOrDefault(locationName)?.GetValueOrDefault(location);

            if (items != null)
            {
                foreach (var item in items.Values)
                {
                    if (item.ItemId == 0 || item.SqlId == ignoreSqlId) continue;

                    var itemInfo = ItemsInfo.GetValueOrDefault(item.ItemId);
                    if (itemInfo == null) continue;

                    int x = item.Index % maxCols;
                    int y = item.Index / maxCols;

                    int width = itemInfo.Width;
                    int height = itemInfo.Height;

                    // ✅ ПОМЕЧАЕМ ЗАНЯТЫЕ КЛЕТКИ
                    for (int dy = 0; dy < height; dy++)
                    {
                        for (int dx = 0; dx < width; dx++)
                        {
                            int checkY = y + dy;
                            int checkX = x + dx;
                            if (checkY < maxRows && checkX < maxCols)
                            {
                                matrix[checkY, checkX] = true;
                            }
                        }
                    }
                }
            }

            // ✅ ИЩЕМ СВОБОДНОЕ МЕСТО ДЛЯ НОВОГО ПРЕДМЕТА
            int foundSlots = 0;
            for (int y = 0; y < maxRows && foundSlots < slotsNeeded; y++)
            {
                for (int x = 0; x < maxCols && foundSlots < slotsNeeded; x++)
                {
                    // ✅ ПРОВЕРЯЕМ, ПОМЕСТИТСЯ ЛИ ПРЕДМЕТ
                    if (x + itemWidth <= maxCols && y + itemHeight <= maxRows)
                    {
                        bool canPlace = true;
                        for (int dy = 0; dy < itemHeight && canPlace; dy++)
                        {
                            for (int dx = 0; dx < itemWidth && canPlace; dx++)
                            {
                                if (matrix[y + dy, x + dx])
                                {
                                    canPlace = false;
                                }
                            }
                        }

                        if (canPlace)
                        {
                            foundSlots++;
                            // ✅ ПОМЕЧАЕМ КАК ЗАНЯТОЕ (для следующих слотов)
                            for (int dy = 0; dy < itemHeight; dy++)
                            {
                                for (int dx = 0; dx < itemWidth; dx++)
                                {
                                    matrix[y + dy, x + dx] = true;
                                }
                            }
                        }
                    }
                }
            }

            return foundSlots >= slotsNeeded;
        }

       

        // ✅ ВСПОМОГАТЕЛЬНАЯ ФУНКЦИЯ: МОЖНО ЛИ РАЗМЕСТИТЬ ПРЕДМЕТ
        private static bool CanPlaceItem(bool[,] matrix, int startX, int startY, int width, int height, int maxCols, int maxRows)
        {
            // Проверка границ
            if (startX + width > maxCols || startY + height > maxRows)
                return false;

            // Проверка занятости клеток
            for (int dy = 0; dy < height; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    if (matrix[startY + dy, startX + dx])
                        return false;
                }
            }

            return true;
        }

        public static int getCountToLacationItem(string locationName, string Location, ItemId ItemId, string data = null)
        {
            try
            {
                int count = 0;
                if (ItemsData.ContainsKey(locationName) && ItemsData[locationName].ContainsKey(Location))
                {
                    foreach (InventoryItemData itemData in ItemsData[locationName][Location].Values)//Todo
                    {
                        if (itemData.ItemId == ItemId.Debug) continue;
                        else if (itemData.ItemId != ItemId || (data == null ? false : itemData.Data != data))
                            continue;
                        count += itemData.Count < 1 ? 1 : itemData.Count;
                    }
                }
                return count;
            }
            catch (Exception e)
            {
                Log.Write($"getCountToLacationItem Exception: {e.ToString()}");
                return 0;
            }
        }

        public static int getCountItem(string locationName, ItemId ItemId, bool bagsToggled = true)
        {
            try
            {
                int count = 0;
                int BagsSqlId = -1;
                if (ItemsData.ContainsKey(locationName))
                {
                    foreach (var _itemsData in ItemsData[locationName].Values)//Todo
                    {
                        foreach (InventoryItemData itemData in _itemsData.Values)
                        {
                            if (itemData.ItemId == ItemId.Bag) BagsSqlId = itemData.SqlId;
                            if (itemData.ItemId == ItemId.Debug) continue;
                            else if (itemData.ItemId != ItemId) continue;
                            count += itemData.Count < 1 ? 1 : itemData.Count;
                        }
                    }
                }
                if (bagsToggled && BagsSqlId != -1 && ItemsData.ContainsKey($"backpack_{BagsSqlId}") && ItemsData[$"backpack_{BagsSqlId}"].ContainsKey("backpack"))
                {
                    foreach (InventoryItemData itemData in ItemsData[$"backpack_{BagsSqlId}"]["backpack"].Values)
                    {
                        if (itemData.ItemId == ItemId.Debug) continue;
                        else if (itemData.ItemId != ItemId) continue;
                        count += itemData.Count < 1 ? 1 : itemData.Count;
                    }
                }
                return count;
            }
            catch (Exception e)
            {
                Log.Write($"getCountItem Exception: {e.ToString()}");
                return 0;
            }
        }
        public static int getCountItem(string locationName, List<ItemId> ItemsId, bool bagsToggled = true)
        {
            try
            {
                int count = 0;
                int BagsSqlId = -1;
                if (ItemsData.ContainsKey(locationName))
                {
                    foreach (var _itemsData in ItemsData[locationName].Values)//Todo
                    {
                        foreach (InventoryItemData itemData in _itemsData.Values)
                        {
                            if (itemData.ItemId == ItemId.Bag) BagsSqlId = itemData.SqlId;
                            if (itemData.ItemId == ItemId.Debug) continue;
                            else if (!ItemsId.Contains(itemData.ItemId)) continue;
                            count += itemData.Count < 1 ? 1 : itemData.Count;
                        }
                    }
                }
                if (bagsToggled && BagsSqlId != -1 && ItemsData.ContainsKey($"backpack_{BagsSqlId}") && ItemsData[$"backpack_{BagsSqlId}"].ContainsKey("backpack"))
                {
                    foreach (InventoryItemData itemData in ItemsData[$"backpack_{BagsSqlId}"]["backpack"].Values)
                    {
                        if (itemData.ItemId == ItemId.Debug) continue;
                        else if (!ItemsId.Contains(itemData.ItemId)) continue;
                        count += itemData.Count < 1 ? 1 : itemData.Count;
                    }
                }
                return count;
            }
            catch (Exception e)
            {
                Log.Write($"getCountItem Exception: {e.ToString()}");
                return 0;
            }
        }

        public static int getCountItems(string locationName, ItemId itemId = ItemId.Debug)
        {
            try
            {
                int count = 0;
                int BagsSqlId = -1;
                if (ItemsData.ContainsKey(locationName))
                {
                    foreach (var _itemsData in ItemsData[locationName].Values)//Todo
                    {
                        foreach (InventoryItemData itemData in _itemsData.Values)
                        {
                            if (itemData.ItemId == ItemId.Bag) BagsSqlId = itemData.SqlId;
                            if (itemData.ItemId == ItemId.Debug) continue;
                            else if (itemId != ItemId.Debug && itemId != itemData.ItemId) continue;
                            count += itemData.Count < 1 ? 1 : itemData.Count;
                        }
                    }
                }
                if (BagsSqlId != -1 && ItemsData.ContainsKey($"backpack_{BagsSqlId}") && ItemsData[$"backpack_{BagsSqlId}"].ContainsKey("backpack"))
                {
                    foreach (InventoryItemData itemData in ItemsData[$"backpack_{BagsSqlId}"]["backpack"].Values)
                    {
                        if (itemData.ItemId == ItemId.Debug) continue;
                        else if (itemId != ItemId.Debug && itemId != itemData.ItemId) continue;
                        count += itemData.Count < 1 ? 1 : itemData.Count;
                    }
                }
                return count;
            }
            catch (Exception e)
            {
                Log.Write($"getCountItems Exception: {e.ToString()}");
                return 0;
            }
        }

        public static string getCountToStockItems(string locationName)
        {
            try
            {
                int ammoCount = 0;
                int otherCount = 0;
                if (ItemsData.ContainsKey(locationName))
                {
                    foreach (var _itemsData in ItemsData[locationName].Values)//Todo
                    {
                        foreach (InventoryItemData itemData in _itemsData.Values)
                        {
                            if (itemData.ItemId == ItemId.Debug) continue;
                            if (ItemsInfo[itemData.ItemId].functionType == newItemType.Ammo) ammoCount += itemData.Count < 1 ? 1 : itemData.Count;
                            else otherCount += 1;
                        }
                    }
                }

                return $"Оружия: {otherCount} шт. | Патронов: {ammoCount} шт.";
            }
            catch (Exception e)
            {
                Log.Write($"getCountToStockItems Exception: {e.ToString()}");
                return $"Оружия: 0 шт. | Патронов: 0 шт.";
            }
        }
        public static void UpdateSqlItemData(string locationName, string Location, int SlotId, InventoryItemData item, ItemId ItemIdDell = ItemId.Debug)
        {
            UpdateSqlItemDataThread(locationName, Location, SlotId, item, ItemIdDell);
        }
        public static void OnAddItem(ItemId itemId, string data)
        {
            if (itemId == ItemId.SimCard)
            {
                var sim = 0;
                if (int.TryParse(data, out sim))
                    Players.Phone.Sim.Repository.Add(sim);
            }
            if (itemId == ItemId.VehicleNumber)
                VehicleManager.AddVehicleNumber(data);
        }

        public static void OnDellItem(ItemId itemId, string data)
        {
            int sim;

            if (itemId == ItemId.SimCard && int.TryParse(data, out sim))
                Players.Phone.Sim.Repository.Remove(sim);

            if (itemId == ItemId.VehicleNumber)
                VehicleManager.RemoveVehicleNumber(data);
        }
        public static void UpdateSqlItemDataThread(string locationName, string Location, int SlotId, InventoryItemData item, ItemId ItemIdDell)
        {
            try
            {
                if (item.SqlId == 0)
                {
                    Log.Write($"[UpdateSqlItemData] SqlId=0, skipping");
                    return;
                }

                Log.Write($"[UpdateSqlItemData] SqlId={item.SqlId}, ItemId={item.ItemId}, Location={Location}, SlotId={SlotId}");

                // ✅ УДАЛЯЕМ ЗАГЛУШКИ ИЛИ ПРЕДМЕТЫ
                if (item.ItemId == ItemId.Debug || item.ItemId == 0)
                {
                    if (item.Data != null && item.Data.StartsWith("placeholder_"))
                    {
                        Log.Write($"[DELETEPLACEHOLDER SQL] Deleting placeholder from DB: SqlId={item.SqlId}, Data={item.Data}");
                    }
                    else
                    {
                        Log.Write($"[DELETEITEM SQL] Deleting item from DB: SqlId={item.SqlId}, ItemId={ItemIdDell}");
                        GameLog.Items($"deletedItem({item.SqlId})", locationName, (int)ItemIdDell, item.Count, item.Data);
                        OnDellItem(ItemIdDell, item.Data);
                    }

                    Database.Models.Items.AddItemDelete(item.SqlId);
                }
                else
                {
                    // ✅ ОБНОВЛЯЕМ ПРЕДМЕТ
                    if (!Database.Models.Items.IsItemUpdate(item.SqlId))
                        GameLog.Items($"updatedItem({item.SqlId})", locationName, (int)item.ItemId, item.Count, item.Data);

                    Database.Models.Items.AddItemUpdate(
                        item.SqlId,
                        locationName,
                        item.Count,
                        item.Data,
                        Location,
                        SlotId,
                        item.IsTurn
                    );

                    Log.Write($"[UpdateSqlItemData] Updated item: SqlId={item.SqlId}, SlotId={SlotId}");
                }
            }
            catch (Exception e)
            {
                Log.Write($"UpdateSqlItemData Exception: {e.ToString()}");
            }
        }

        public static void UpdatePlayerItemData(ExtPlayer player, string locationName, string Location, int SlotId, InventoryItemData item, bool isInfo = false)
        {
            try
            {
                if (!player.IsCharacterData()) return;

                if (Location != "backpack" && !InventoryLocation.ContainsKey(Location))
                    Location = "other";

                if (Location == "other" && (!InventoryOtherPlayers.ContainsKey(locationName) || !InventoryOtherPlayers[locationName].Contains(player)))
                    return;

                InventoryItemData newItem = new InventoryItemData(
                    SqlId: item.SqlId,
                    ItemId: item.ItemId,
                    Count: item.Count,
                    Data: item.Data,
                    Index: item.Index
                );
                newItem.Price = item.Price;
                newItem.IsTurn = item.IsTurn;

                // ✅ НЕ МЕНЯЕМ Data У ЗАГЛУШЕК
                if (newItem.ItemId != ItemId.Debug && newItem.ItemId == ItemId.CarKey)
                    newItem.Data = GetVehicleName(newItem.Data);

                // ✅ ВАЖНО! Отправляем даже пустые слоты (Debug)
                Log.Write($"[UpdatePlayerItemData] Sending to client: Location={Location}, SlotId={SlotId}, ItemId={newItem.ItemId}, IsTurn={newItem.IsTurn}"); // ✅ ЛОГИРОВАНИЕ!

                Trigger.ClientEvent(player, "client.inventory.UpdateSlot", Location, SlotId, JsonConvert.SerializeObject(newItem), isInfo);

                // Обновление веса
                var characterData = player.GetCharacterData();
                if (characterData != null)
                {
                    float inventoryWeight = CalculateInventoryWeight(characterData.UUID);
                    float backpackWeight = CalculateBackpackWeight(characterData.UUID);

                    characterData.InventoryWeight = inventoryWeight;
                    characterData.BackpackWeight = backpackWeight;

                    Trigger.ClientEvent(player, "client.inventory.UpdateWeight", inventoryWeight, backpackWeight);
                }

                if (Location == "inventory")
                    isRadio(player);
            }
            catch (Exception e)
            {
                Log.Write($"UpdatePlayerItemData Exception: {e.ToString()}");
            }
        }
        public static InventoryItemData GetItemData(ExtPlayer player, string Location, int SlotId)
        {
            try
            {
                //player.IsInstanceAlive
                string locationName = GetLocationName(player, Location);

                if (Location == "other")
                    Location = locationName.Split('_')[0];

                if (locationName != null && ItemsData.ContainsKey(locationName) && ItemsData[locationName].ContainsKey(Location) && ItemsData[locationName][Location].ContainsKey(SlotId))
                {
                    InventoryItemData Item = ItemsData[locationName][Location][SlotId];

                    var newItem = new InventoryItemData(Item.SqlId, Item.ItemId, Item.Count, Item.Data, Item.Index, Item.IsTurn);
                    newItem.Price = Item.Price;

                    return newItem;
                }
                return new InventoryItemData();
            }
            catch (Exception e)
            {
                Log.Write($"GetItemData Exception: {e.ToString()}");
                return new InventoryItemData();
            }
        }
        public static void SetItemData(ExtPlayer player, string Location, int SlotId, InventoryItemData item, bool send = false, bool check = false, bool isSqlUpdate = true, bool isInfo = false, bool isWeaponShot = false)
        {
            try
            {
                Log.Write($"[SETITEMDATA] Location: {Location}, SlotId: {SlotId}, ItemId: {item.ItemId}, SqlId: {item.SqlId}, isSqlUpdate: {isSqlUpdate}");

                var characterData = player.GetCharacterData();
                if (characterData == null) return;
                var sessionData = player.GetSessionData();
                if (sessionData == null) return;

                string locationName = GetLocationName(player, Location);
                if (Location == "other") Location = locationName.Split('_')[0];

                if (locationName != null)
                {
                    AddInventoryArray(locationName, Location);

                    InventoryItemData itemLog = item;
                    string textLog = "положил на склад";

                    item.Index = SlotId;

                    // ✅ ЕСЛИ ЭТО FASTSLOTS — СОЗДАЁМ КОПИЮ
                    if (Location == "fastSlots" && item.ItemId != ItemId.Debug && item.SqlId > 0)
                    {
                        Log.Write($"[SETITEMDATA FASTSLOTS] Adding item to fastSlots: ItemId={item.ItemId}, OriginalSqlId={item.SqlId}, SlotId={SlotId}");

                        if (ItemsData.ContainsKey(locationName) &&
                            ItemsData[locationName].ContainsKey(Location) &&
                            ItemsData[locationName][Location].ContainsKey(SlotId))
                        {
                            var existingItem = ItemsData[locationName][Location][SlotId];

                            if (existingItem.Data != null && existingItem.Data.StartsWith("orig_"))
                            {
                                var parts = existingItem.Data.Split('_');
                                if (parts.Length >= 2 && int.TryParse(parts[1], out int origSqlId))
                                {
                                    if (origSqlId == item.SqlId)
                                    {
                                        Log.Write($"[SETITEMDATA] Item already in fastSlots[{SlotId}]: OriginalSqlId={item.SqlId}");
                                        return;
                                    }
                                    else
                                    {
                                        Log.Write($"[SETITEMDATA] Removing old copy from fastSlots[{SlotId}]: OldOriginalSqlId={origSqlId}, OldSqlId={existingItem.SqlId}");

                                        if (existingItem.SqlId > 0)
                                        {
                                            Database.Models.Items.AddItemDelete(existingItem.SqlId);
                                            GameLog.Items($"deletedItem({existingItem.SqlId})", locationName, (int)existingItem.ItemId, 0, existingItem.Data);
                                        }

                                        if (ItemsData.ContainsKey(locationName) &&
                                            ItemsData[locationName].ContainsKey(Location) &&
                                            ItemsData[locationName][Location].ContainsKey(SlotId))
                                        {
                                            ItemsData[locationName][Location].TryRemove(SlotId, out _);
                                            Log.Write($"[SETITEMDATA] Removed old copy from memory: SlotId={SlotId}");
                                        }

                                        UpdatePlayerItemData(player, locationName, Location, SlotId, new InventoryItemData());
                                    }
                                }
                            }
                        }

                        int originalSqlId = item.SqlId;

                        var copyItem = new InventoryItemData(
                            SqlId: -1,
                            ItemId: item.ItemId,
                            Count: item.Count,
                            Data: $"orig_{originalSqlId}",
                            Index: SlotId,
                            IsTurn: item.IsTurn
                        );

                        if (ItemsData[locationName][Location].ContainsKey(SlotId))
                            ItemsData[locationName][Location].TryRemove(SlotId, out _);

                        ItemsData[locationName][Location].TryAdd(SlotId, copyItem);

                        Trigger.SetTask(async () =>
                        {
                            try
                            {
                                await using var db = new ServerBD("MainDB");

                                int newSqlId = await db.InsertWithInt32IdentityAsync(new ItemsData
                                {
                                    DataId = locationName,
                                    ItemId = (short)item.ItemId,
                                    ItemCount = (short)item.Count,
                                    ItemData = $"orig_{originalSqlId}",
                                    Location = Location,
                                    SlotId = (short)SlotId,
                                    IsTurn = (sbyte)(item.IsTurn ? 1 : 0)
                                });

                                copyItem.SqlId = newSqlId;

                                if (ItemsData.ContainsKey(locationName) &&
                                    ItemsData[locationName].ContainsKey(Location) &&
                                    ItemsData[locationName][Location].ContainsKey(SlotId))
                                {
                                    ItemsData[locationName][Location][SlotId].SqlId = newSqlId;
                                }

                                Log.Write($"[SETITEMDATA FASTSLOTS] Copy saved to DB: NewSqlId={newSqlId}, OriginalSqlId={originalSqlId}");
                            }
                            catch (Exception e)
                            {
                                Log.Write($"SetItemData SaveCopy Exception: {e.ToString()}");
                            }
                        });

                        var displayItem = new InventoryItemData(
                            SqlId: copyItem.SqlId,
                            ItemId: item.ItemId,
                            Count: item.Count,
                            Data: item.Data,
                            Index: SlotId,
                            IsTurn: item.IsTurn
                        );

                        if (send)
                            UpdatePlayerItemData(player, locationName, Location, SlotId, displayItem, isInfo: isInfo);

                        ItemsOtherUpdate(player, locationName, Location, SlotId, displayItem);

                        Log.Write($"[SETITEMDATA FASTSLOTS] Copy created: NewSqlId={copyItem.SqlId}");
                        return;
                    }

                    // ✅ ОБЫЧНАЯ ЛОГИКА (НЕ FASTSLOTS)
                    if (Location == "Fraction")
                    {
                        Fractions.Table.Logs.Repository.AddLogs(player, FractionLogsType.TakeStock, $"{textLog} {ItemsInfo[itemLog.ItemId].Name} (x{itemLog.Count})");
                        var fracId = player.GetFractionId();

                        if (fracId > (int)Fractions.Models.Fractions.None)
                            Manager.sendFractionMessage(fracId, "!{#636363}[F] " + $"{player.Name} ({player.Value}) {textLog} {ItemsInfo[itemLog.ItemId].Name} (x{itemLog.Count}).", true);
                    }
                    else if (Location == "Organization")
                        Organizations.Table.Logs.Repository.AddLogs(player, OrganizationLogsType.TakeStock, $"{textLog} {ItemsInfo[itemLog.ItemId].Name} (x{itemLog.Count})");

                    // ✅ КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: ОБНОВЛЯЕМ БД **ДО** ОТПРАВКИ КЛИЕНТУ
                    if (isSqlUpdate)
                    {
                        Log.Write($"[SETITEMDATA] Updating DB: SqlId={item.SqlId}, SlotId={SlotId}, IsTurn={item.IsTurn}");
                        UpdateSqlItemData(locationName, Location, SlotId, item);
                    }

                    // ✅ ОБНОВЛЯЕМ ПАМЯТЬ
                    if (ItemsData[locationName][Location].ContainsKey(SlotId))
                        ItemsData[locationName][Location][SlotId] = item;
                    else
                        ItemsData[locationName][Location].TryAdd(SlotId, item);

                    // ✅ ОТПРАВЛЯЕМ КЛИЕНТУ
                    if (send)
                        UpdatePlayerItemData(player, locationName, Location, SlotId, item, isInfo: isInfo);

                    ItemsOtherUpdate(player, locationName, Location, SlotId, item);

                    if (Location == "accessories")
                    {
                        AccessoriesUse(player, SlotId);
                        ClothesComponents.UpdateClothes(player);
                    }
                    else if (Location == "fastSlots" && check)
                    {
                        WeaponRepository.RemoveHands(player);
                    }
                    else if (Location == "fastSlots" && sessionData.ActiveWeap.Index == SlotId && !isWeaponShot)
                    {
                        WeaponRepository.RemoveHands(player);
                    }
                    else if (Location == "vehicle")
                    {
                        BattlePass.Repository.UpdateReward(player, 90);
                    }
                    else if (Location == "CarKey")
                    {
                        BattlePass.Repository.UpdateReward(player, 73);
                    }

                    if (player.IsCharacterData())
                    {
                        characterData.InventoryWeight = CalculateInventoryWeight(characterData.UUID);
                        characterData.BackpackWeight = CalculateBackpackWeight(characterData.UUID);
                        Trigger.ClientEvent(player, "client.inventory.UpdateWeight", characterData.InventoryWeight, characterData.BackpackWeight);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Write($"SetItemData Exception: {e.ToString()}");
            }
        }
        public static void ItemsOtherUpdate(ExtPlayer player, string locationName, string Location, int SlotId, InventoryItemData item)
        {
            try
            {
                if (Location == "trade" && player.IsCharacterData())
                {
                    var sessionData = player.GetSessionData();
                    if (sessionData.ItemsTrade != null)
                    {
                        ItemsTrade TradeItem = sessionData.ItemsTrade;
                        if (TradeItem.Target.IsCharacterData())
                            UpdatePlayerItemData(TradeItem.Target, locationName, "with_trade", SlotId, item);
                    }
                }
                else if (InventoryOtherPlayers.ContainsKey(locationName))
                {
                    foreach (ExtPlayer foreachPlayer in InventoryOtherPlayers[locationName])
                    {
                        if (!foreachPlayer.IsCharacterData()) continue;
                        else if (player != null && foreachPlayer.Value == player.Value) continue;
                        UpdatePlayerItemData(foreachPlayer, locationName, Location, SlotId, item);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Write($"ItemsOtherUpdate Exception: {e.ToString()}");
            }
        }
        public static void ItemsMoveStack(ExtPlayer player, string selectLocation, int selectSlotId, string hoverLocation, int hoverSlotId, int count)
        {
            try
            {
                if (!player.IsCharacterData()) return;
                InventoryItemData _sItem = GetItemData(player, selectLocation, selectSlotId);
                InventoryItemData _hItem = GetItemData(player, hoverLocation, hoverSlotId);
                ItemsInfo _hInfoItem = ItemsInfo[_hItem.ItemId];
                if (_sItem.ItemId == _hItem.ItemId || _hItem.ItemId == ItemId.Debug)
                {
                    /*if (_sItem.Count == _hInfoItem.Stack)
                    {
                        SetItemData(player, hoverLocation, hoverSlotId, _sItem, true);
                        SetItemData(player, selectLocation, selectSlotId, _hItem, true);
                    }
                    else */
                    if (_sItem.ItemId == _hItem.ItemId)
                    {
                        _hItem.Count += count;
                        _sItem.Count -= count;
                        SetItemData(player, hoverLocation, hoverSlotId, _hItem, true);
                        SetItemData(player, selectLocation, selectSlotId, _sItem, true);
                    }
                    else
                    {
                        _sItem.Count -= count;
                        SetItemData(player, selectLocation, selectSlotId, _sItem, true);

                        string locationName = GetLocationName(player, hoverLocation);
                        if (hoverLocation == "other") hoverLocation = locationName.Split('_')[0];

                        AddSqlItem(player, locationName, hoverLocation, _sItem.ItemId, hoverSlotId, count, _sItem.Data);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Write($"ItemsMoveStack Exception: {e.ToString()}");
            }
        }
        private static bool CanPlaceItemAt(ExtPlayer player, string location, int slotId, ItemId itemId, bool isTurn, int ignoreSlot = -1)
        {
            try
            {
                var characterData = player.GetCharacterData();
                if (characterData == null) return false;

                string locationName = GetLocationName(player, location);
                if (string.IsNullOrEmpty(locationName)) return false;

                if (location == "other")
                    location = locationName.Split('_')[0];

                if (!ItemsData.ContainsKey(locationName) || !ItemsData[locationName].ContainsKey(location))
                    return true;

                var items = ItemsData[locationName][location];

                // ✅ Получаем размеры предмета
                if (!ItemsInfo.ContainsKey(itemId)) return false;

                var itemInfo = ItemsInfo[itemId];
                int itemWidth = isTurn ? itemInfo.Height : itemInfo.Width;
                int itemHeight = isTurn ? itemInfo.Width : itemInfo.Height;

                // ✅ Определяем количество колонок
                int maxCols = location switch
                {
                    "inventory" => 6,
                    "backpack" => 6,
                    "other" => 6,
                    _ => 5
                };

                int maxRows = location switch
                {
                    "inventory" => 17,
                    "backpack" => 8,
                    "other" => 19,
                    _ => 17
                };

                int startX = slotId % maxCols;
                int startY = slotId / maxCols;

                // ✅ Проверяем границы
                if (startX + itemWidth > maxCols || startY + itemHeight > maxRows)
                    return false;

                // ✅ Проверяем занятость клеток
                for (int y = 0; y < itemHeight; y++)
                {
                    for (int x = 0; x < itemWidth; x++)
                    {
                        int checkIndex = (startY + y) * maxCols + (startX + x);

                        // ✅ Пропускаем слот, с которого перемещаем
                        if (checkIndex == ignoreSlot) continue;

                        if (items.ContainsKey(checkIndex))
                        {
                            var existingItem = items[checkIndex];

                            // ✅ Слот занят, если там реальный предмет ИЛИ заглушка от ДРУГОГО предмета
                            if (existingItem.ItemId != ItemId.Debug ||
                                (existingItem.Data != null && existingItem.Data.StartsWith("placeholder_") && existingItem.Data != $"placeholder_{slotId}"))
                            {
                                return false;
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Log.Write($"CanPlaceItemAt Exception: {e.ToString()}");
                return false;
            }
        }
        /// <summary>
        /// Удаляет ВСЕ заглушки для указанного слота
        /// </summary>
        public static void DeletePlaceholdersForSlot(ExtPlayer player, string locationName, string location, int startIndex, bool forceDelete = false)
        {
            try
            {
                if (!ItemsData.ContainsKey(locationName) || !ItemsData[locationName].ContainsKey(location))
                {
                    Log.Write($"[DELETEPLACEHOLDER] Location not found: {locationName}/{location}");
                    return;
                }

                var itemsToDelete = new List<int>();

                // ✅ ИЩЕМ ВСЕ ЗАГЛУШКИ, СВЯЗАННЫЕ С ЭТИМ ПРЕДМЕТОМ
                foreach (var kvp in ItemsData[locationName][location])
                {
                    var item = kvp.Value;
                    if (item.ItemId == ItemId.Debug &&
                        item.Data != null &&
                        item.Data == $"placeholder_{startIndex}")
                    {
                        itemsToDelete.Add(kvp.Key);
                        Log.Write($"[DELETEPLACEHOLDER] Found placeholder: Index={kvp.Key}, Data={item.Data}, SqlId={item.SqlId}");
                    }
                }

                // ✅ УДАЛЯЕМ ЗАГЛУШКИ ИЗ ПАМЯТИ И БД
                foreach (var index in itemsToDelete)
                {
                    var placeholder = ItemsData[locationName][location][index];

                    Log.Write($"[DELETEPLACEHOLDER] Deleting placeholder: Index={index}, Data={placeholder.Data}, SqlId={placeholder.SqlId}");

                    // ✅ УДАЛЯЕМ ИЗ БД
                    if (placeholder.SqlId > 0)
                    {
                        Database.Models.Items.AddItemDelete(placeholder.SqlId);
                    }

                    // ✅ УДАЛЯЕМ ИЗ ПАМЯТИ
                    ItemsData[locationName][location].TryRemove(index, out _);

                    // ✅ ОТПРАВЛЯЕМ КЛИЕНТУ ОБНОВЛЕНИЕ
                    if (player != null && player.IsCharacterData())
                    {
                        UpdatePlayerItemData(player, locationName, location, index, new InventoryItemData());
                    }
                }

                Log.Write($"[DELETEPLACEHOLDER] Deleted {itemsToDelete.Count} placeholders for startIndex={startIndex}");
            }
            catch (Exception e)
            {
                Log.Write($"DeletePlaceholdersForSlot Exception: {e.ToString()}");
            }
        }
        /// <summary>
        /// Создаёт заглушки для предмета в указанном слоте
        /// </summary>
        /// <summary>
        /// Создаёт заглушки для предмета в указанном слоте
        /// </summary>
        public static void CreatePlaceholdersForSlot(ExtPlayer player, string locationName, string location, int slotId, ItemId itemId, bool isTurn)
        {
            try
            {
                // ✅ ДОБАВЬТЕ ЛОГИРОВАНИЕ
                Log.Write($"[CreatePlaceholders] Location: {location}, SlotId: {slotId}, ItemId: {itemId}, IsTurn: {isTurn}", nLog.Type.Warn);

                // ✅ Заглушки нужны ТОЛЬКО для инвентаря и рюкзака
                if (location != "inventory" && location != "backpack")
                {
                    return; // ❌ Для vehicle, warehouse, tent и т.д. заглушки НЕ нужны
                }

                if (!ItemsInfo.ContainsKey(itemId)) return;

                var itemInfo = ItemsInfo[itemId];
                int itemWidth = isTurn ? itemInfo.Height : itemInfo.Width;
                int itemHeight = isTurn ? itemInfo.Width : itemInfo.Height;

                // ✅ ДОБАВЬТЕ ЛОГИРОВАНИЕ РАЗМЕРОВ
                Log.Write($"[CreatePlaceholders] ItemSize: {itemWidth}x{itemHeight}", nLog.Type.Warn);

                // ✅ Если предмет 1x1 — заглушки не нужны
                if (itemWidth == 1 && itemHeight == 1) return;

                // ✅ Определяем количество колонок
                int maxCols = location switch
                {
                    "inventory" => 6,
                    "backpack" => 6,
                    _ => 6
                };

                int startX = slotId % maxCols;
                int startY = slotId / maxCols;

                // ✅ ДОБАВЬТЕ ЛОГИРОВАНИЕ СТАРТОВОЙ ПОЗИЦИИ
                Log.Write($"[CreatePlaceholders] StartPos: X={startX}, Y={startY}, MaxCols={maxCols}", nLog.Type.Warn);

                // ✅ Создаём заглушки
                for (int y = 0; y < itemHeight; y++)
                {
                    for (int x = 0; x < itemWidth; x++)
                    {
                        if (x == 0 && y == 0) continue; // Пропускаем главную клетку

                        int placeholderIndex = (startY + y) * maxCols + (startX + x);

                        // ✅ ДОБАВЬТЕ ЛОГИРОВАНИЕ ИНДЕКСОВ ЗАГЛУШЕК
                        Log.Write($"[CreatePlaceholders] Placeholder created at index: {placeholderIndex} (offset: x={x}, y={y})", nLog.Type.Warn);

                        // ✅ Создаём заглушку в памяти
                        var placeholder = new InventoryItemData(
                            SqlId: -1,
                            ItemId: ItemId.Debug,
                            Count: 0,
                            Data: $"placeholder_{slotId}",
                            Index: placeholderIndex
                        );

                        if (!ItemsData.ContainsKey(locationName))
                            ItemsData.TryAdd(locationName, new ConcurrentDictionary<string, ConcurrentDictionary<int, InventoryItemData>>());

                        if (!ItemsData[locationName].ContainsKey(location))
                            ItemsData[locationName].TryAdd(location, new ConcurrentDictionary<int, InventoryItemData>());

                        ItemsData[locationName][location][placeholderIndex] = placeholder;

                        // ✅ Сохраняем в БД
                        Trigger.SetTask(() =>
                        {
                            SavePlaceholderToDatabase(locationName, location, placeholderIndex, $"placeholder_{slotId}");
                        });

                        // ✅ Отправляем клиенту
                        if (player != null && player.IsCharacterData())
                        {
                            UpdatePlayerItemData(player, locationName, location, placeholderIndex, placeholder);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Write($"CreatePlaceholdersForSlot Exception: {e.ToString()}");
            }
        }
        /// <summary>
        /// Удаляет предмет из fastSlots, если он там есть (по SqlId оригинала)
        /// </summary>
        public static void ClearItemFromFastSlots(ExtPlayer player, int originalSqlId)
        {
            try
            {
                var characterData = player.GetCharacterData();
                if (characterData == null) return;

                string locationName = $"char_{characterData.UUID}";

                if (!ItemsData.ContainsKey(locationName) || !ItemsData[locationName].ContainsKey("fastSlots"))
                    return;

                // ✅ ИЩЕМ ССЫЛКИ НА ЭТОТ ПРЕДМЕТ
                var slotsToRemove = new List<int>();

                foreach (var kvp in ItemsData[locationName]["fastSlots"])
                {
                    var item = kvp.Value;

                    if (item.Data != null && item.Data.StartsWith("orig_"))
                    {
                        var parts = item.Data.Split('_');
                        if (parts.Length >= 2 && int.TryParse(parts[1], out int origSqlId))
                        {
                            if (origSqlId == originalSqlId)
                            {
                                slotsToRemove.Add(kvp.Key);
                            }
                        }
                    }
                }

                // ✅ УДАЛЯЕМ
                // ✅ УДАЛЯЕМ
                foreach (var slotId in slotsToRemove)
                {
                    var itemToRemove = ItemsData[locationName]["fastSlots"][slotId];

                    Log.Write($"[CLEARFASTSLOTS] Removing link from fastSlots[{slotId}] for SqlId={originalSqlId}, LinkSqlId={itemToRemove.SqlId}");

                    // ✅ УДАЛЯЕМ ИЗ БД
                    if (itemToRemove.SqlId > 0)
                    {
                        Database.Models.Items.AddItemDelete(itemToRemove.SqlId);
                        GameLog.Items($"deletedItem({itemToRemove.SqlId})", locationName, 0, 0, itemToRemove.Data);
                    }

                    // ✅ УДАЛЯЕМ ИЗ ПАМЯТИ
                    ItemsData[locationName]["fastSlots"].TryRemove(slotId, out _);

                    // ✅ ОТПРАВЛЯЕМ КЛИЕНТУ
                    UpdatePlayerItemData(player, locationName, "fastSlots", slotId, new InventoryItemData());

                    // ✅ ОЧИЩАЕМ UI
                    Trigger.ClientEvent(player, "client.inventory.clearFastSlot", slotId);
                }
            }
            catch (Exception e)
            {
                Log.Write($"ClearItemFromFastSlots Exception: {e.ToString()}");
            }
        }
        public static void ItemsMove(ExtPlayer player, string selectArrayName, int selectIndex, string hoverArrayName, int hoverIndex, bool isTurn = false)
        {
            try
            {
                var sessionData = player.GetSessionData();
                if (sessionData == null) return;

                var characterData = player.GetCharacterData();
                if (characterData == null) return;

                // ✅ ПОЛУЧАЕМ ПРЕДМЕТЫ
                InventoryItemData selectItem = GetItemData(player, selectArrayName, selectIndex);
                InventoryItemData hoverItem = GetItemData(player, hoverArrayName, hoverIndex);

                if (selectItem.ItemId == ItemId.Debug) return;

                Log.Write($"[SWAP] Moving item: from {selectArrayName}[{selectIndex}] to {hoverArrayName}[{hoverIndex}], isTurn={isTurn}");
                Log.Write($"[SWAP] SelectItem: ItemId={selectItem.ItemId}, IsTurn={selectItem.IsTurn}");
                Log.Write($"[SWAP] HoverItem: ItemId={hoverItem.ItemId}, IsTurn={hoverItem.IsTurn}");

                // ✅ ПРОВЕРЯЕМ: ЭТО ОРУЖИЕ В РУКАХ?
                if (sessionData.ActiveWeap != null &&
                    sessionData.ActiveWeap.Item != null &&
                    sessionData.ActiveWeap.Item.SqlId == selectItem.SqlId)
                {
                    Log.Write($"[SWAP] Weapon in hands detected: SqlId={selectItem.SqlId}, removing from hands");

                    Trigger.ClientEvent(player, "client.weapon.take", true);
                    WeaponComponents.Remove(player);

                    sessionData.ActiveWeap = new ItemStruct("", -1, null);
                    sessionData.LastActiveWeap = 0;

                    Trigger.ClientEvent(player, "client.inventory.clearActiveWeapon");
                }

                // ✅ УПРОЩЁННАЯ ЛОГИКА: ИСПОЛЬЗУЕМ GetLocationName БЕЗ РАЗДЕЛЕНИЯ! 
                string selectLocationName = GetLocationName(player, selectArrayName);
                string hoverLocationName = GetLocationName(player, hoverArrayName);

                Log.Write($"[SWAP] selectLocationName={selectLocationName}");
                Log.Write($"[SWAP] hoverLocationName={hoverLocationName}");

                // ✅ УДАЛЯЕМ ЗАГЛУШКИ ДЛЯ ОБОИХ ПРЕДМЕТОВ
                DeletePlaceholdersForSlot(player, selectLocationName, selectArrayName, selectIndex);

                if (hoverItem.ItemId != ItemId.Debug)
                {
                    DeletePlaceholdersForSlot(player, hoverLocationName, hoverArrayName, hoverIndex);
                }

                // ✅ ОБНОВЛЯЕМ Index И IsTurn У ПРЕДМЕТОВ
                selectItem.Index = hoverIndex;
                selectItem.IsTurn = isTurn;

                if (hoverItem.ItemId != ItemId.Debug)
                {
                    hoverItem.Index = selectIndex;
                }

                // ✅ РАЗМЕЩАЕМ ПРЕДМЕТЫ
                Log.Write($"[SWAP] Placing selectItem at {hoverArrayName}[{hoverIndex}], IsTurn={selectItem.IsTurn}");
                SetItemData(player, hoverArrayName, hoverIndex, selectItem, send: true, isSqlUpdate: true);

                Log.Write($"[SWAP] Placing hoverItem at {selectArrayName}[{selectIndex}]");
                SetItemData(player, selectArrayName, selectIndex, hoverItem, send: true, isSqlUpdate: true);

                // ✅ СОЗДАЁМ ЗАГЛУШКИ ДЛЯ НОВОГО МЕСТА
                Log.Write($"[SWAP] Creating placeholders for selectItem at {hoverLocationName}[{hoverIndex}]");
                CreatePlaceholdersForSlot(player, hoverLocationName, hoverArrayName, hoverIndex, selectItem.ItemId, selectItem.IsTurn);

                if (hoverItem.ItemId != ItemId.Debug)
                {
                    Log.Write($"[SWAP] Creating placeholders for hoverItem at {selectLocationName}[{selectIndex}]");
                    CreatePlaceholdersForSlot(player, selectLocationName, selectArrayName, selectIndex, hoverItem.ItemId, hoverItem.IsTurn);
                }

                Log.Write($"[SWAP] SUCCESS!  Swapped items");
            }
            catch (Exception e)
            {
                Log.Write($"ItemsMove Exception: {e.ToString()}");
            }
        }
        /// <summary>
        /// Удаляет предмет из fastSlots, если он там есть
        /// </summary>

        /// <summary>
        /// Удаляет заглушки для предмета перед перемещением
        /// </summary>
        public static void ClearPlaceholdersForItem(ExtPlayer player, string location, int slotId, InventoryItemData item)
        {
            try
            {
                if (item.ItemId == ItemId.Debug) return;

                var characterData = player.GetCharacterData();
                if (characterData == null) return;

                string locationName = GetLocationName(player, location);
                if (string.IsNullOrEmpty(locationName)) return;

                if (location == "other")
                    location = locationName.Split('_')[0];

                if (!ItemsData.ContainsKey(locationName) || !ItemsData[locationName].ContainsKey(location))
                    return;

                var items = ItemsData[locationName][location];

                // ✅ Получаем размеры предмета
                if (!ItemsInfo.ContainsKey(item.ItemId)) return;

                var itemInfo = ItemsInfo[item.ItemId];
                int itemWidth = item.IsTurn ? itemInfo.Height : itemInfo.Width;
                int itemHeight = item.IsTurn ? itemInfo.Width : itemInfo.Height;

                // ✅ Определяем количество колонок
                int maxCols = location switch
                {
                    "inventory" => 6,
                    "backpack" => 6,
                    "other" => 6,
                    _ => 5
                };

                int startX = slotId % maxCols;
                int startY = slotId / maxCols;

                // ✅ Удаляем все заглушки для этого предмета
                for (int y = 0; y < itemHeight; y++)
                {
                    for (int x = 0; x < itemWidth; x++)
                    {
                        if (x == 0 && y == 0) continue; // Пропускаем главную клетку

                        int placeholderIndex = (startY + y) * maxCols + (startX + x);

                        if (items.ContainsKey(placeholderIndex))
                        {
                            var existingItem = items[placeholderIndex];

                            // ✅ Удаляем заглушку
                            if (existingItem.ItemId == ItemId.Debug &&
                                existingItem.Data != null &&
                                existingItem.Data == $"placeholder_{slotId}")
                            {
                                items.TryRemove(placeholderIndex, out _);

                                // ✅ Удаляем из БД
                                if (existingItem.SqlId > 0)
                                {
                                    Database.Models.Items.AddItemDelete(existingItem.SqlId);
                                }

                                // ✅ Отправляем обновление клиенту
                                UpdatePlayerItemData(player, locationName, location, placeholderIndex, new InventoryItemData());
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Write($"ClearPlaceholdersForItem Exception: {e.ToString()}");
            }
        }
        #region Одежда

        public static void LoadAccessories(ExtPlayer player, bool gender = true, bool isUpdateClothes = true)
        {
            try
            {
                var sessionData = player.GetSessionData();
                if (sessionData == null)
                    return;

                var characterData = player.GetCharacterData();

                string locationName = $"";
                if (characterData == null)
                    locationName = $"char_{sessionData.SelectUUID}";
                else
                {
                    gender = characterData.Gender;
                    locationName = $"char_{characterData.UUID}";
                }

                ClothesComponents.SetClothes(player, 10, Customization.EmtptySlots[gender][10], 0);

                /*var itemData = ClothesComponents.GetItemData(player, "accessories", 7);
                if (isUpdateClothes && characterData != null && itemData.ItemId == ItemId.BodyArmor)
                {
                    if (sessionData.ArmorHealth != -1 && player.Armor > sessionData.ArmorHealth) 
                        WeaponRepository.PlayerKickAntiCheat(player, 3, true);
                    
                    itemData.Data = player.Armor.ToString();
                    SetItemData(player, "accessories", 7, itemData, true);
                }*/

                //ConcurrentDictionary<int, InventoryItemData> accessories = null;

                //if (ItemsData.ContainsKey(locationName) && ItemsData[locationName].ContainsKey("accessories"))
                //    accessories = ItemsData[locationName]["accessories"];

                var clothes = false;
                var hat = false;
                for (int index = 0; index < InventoryMaxSlots["accessories"]; index++)
                {
                    if (new List<int>() { 0, 1, 2, 3 }.Contains(index))
                    {
                        if (!hat)
                        {
                            AccessoriesUse(player, index, true, gender: gender);
                            hat = true;
                        }
                    }
                    else if (new List<int>() { 5, 6, 9, 12 }.Contains(index))
                    {
                        if (!clothes)
                        {
                            AccessoriesUse(player, index, true, gender: gender);
                            clothes = true;
                        }
                    }
                    else
                        AccessoriesUse(player, index, true, gender: gender);
                }

                if (isUpdateClothes)
                    ClothesComponents.UpdateClothes(player);
            }
            catch (Exception e)
            {
                Log.Write($"LoadAccessories Exception: {e.ToString()}");
            }
        }

        public static bool ChangeAccessoriesItem(ExtPlayer player, int SlotId, string ItemData = "", bool UpdateSlot = false, ItemId DunamicId = ItemId.Debug)
        {
            try
            {
                var characterData = player.GetCharacterData();
                if (characterData == null) return false;
                ItemId ItemId = DunamicId == ItemId.Debug ? AccessoriesInfo[SlotId] : DunamicId;
                if (!UpdateSlot)
                {
                    InventoryItemData _Item = GetItemData(player, "accessories", SlotId);
                    if (_Item.ItemId == ItemId.Debug)
                    {
                        AddSqlItem(player, $"char_{characterData.UUID}", "accessories", ItemId, SlotId, 1, ItemData);
                    }
                    else if (AddNewItem(player, $"char_{characterData.UUID}", "inventory", ItemId, 1, ItemData) == -1) return false;
                }
                else
                {
                    InventoryItemData _Item = GetItemData(player, "accessories", SlotId);

                    if (_Item.ItemId != ItemId.Debug)
                    {
                        int FreeSlotId = AddItem(player, $"char_{characterData.UUID}", "inventory", _Item);
                        if (FreeSlotId == -1) return false;
                    }
                    AddSqlItem(player, $"char_{characterData.UUID}", "accessories", ItemId, SlotId, 1, ItemData);
                }
                return true;
            }
            catch (Exception e)
            {
                Log.Write($"ChangeAccessoriesItem Exception: {e.ToString()}");
                return false;
            }
        }
        public static void AccessoriesUse(ExtPlayer player, int SlotId, bool toggled = false, bool gender = true)
        {
            try
            {
                var characterData = player.GetCharacterData();
                var sessionData = player.GetSessionData();
                if (sessionData == null) return;
                //if (!toggled && characterData != null && SlotId != 7 && SlotId != 8 && SlotId != 1 && ((sessionData.WorkData.OnDuty && Manager.FractionTypes[memberFractionData.Id] == FractionsType.Gov && memberFractionData.Id != (int)Fractions.Models.Fractions.FIB) || sessionData.WorkData.OnWork)) return;

                //if (!toggled && characterData != null && SlotId != 7 && player.Accessories != null && player.Accessories.ContainsKey(SlotId))
                //    return;

                //if (characterData != null && SlotId == 7 && player.Accessories != null && player.Accessories.ContainsKey(SlotId))
                //    Item = ClothesComponents.GetItemData(player, "accessories", 7);

                if (characterData != null)
                {
                    //Skin
                    if (player.GetSkin() == PedHash.FreemodeMale01 || player.GetSkin() == PedHash.FreemodeFemale01)
                    {
                        gender = player.GetSkin() == PedHash.FreemodeMale01;
                    }
                    else
                        gender = characterData.Gender;
                }


                //player.SetSkin((characterData.Gender) ? PedHash.FreemodeMale01 : PedHash.FreemodeFemale01);
                var Item = ClothesComponents.GetItemData(player, "accessories", SlotId);
                var ItemData = Item.GetData();
                var Variation = ItemData["Variation"] == -1 ? 0 : ItemData["Variation"];
                var Texture = ItemData["Texture"] == -1 ? 0 : ItemData["Texture"];

                switch (SlotId)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                        ClothesComponents.SetHat(player, gender);
                        break;
                    case 4://Accessories
                        if (Item.ItemId == ItemId.Debug)
                        {
                            Variation = Customization.EmtptySlots[gender][7];
                        }
                        else
                        {
                            ConcurrentDictionary<int, ClothesData> AccessoriesData = ClothesComponents.ClothesComponentData[gender][ClothesComponent.Accessories];
                            if (AccessoriesData.ContainsKey(Variation)) Variation = AccessoriesData[Variation].Variation;
                            else Variation = Customization.EmtptySlots[gender][7];
                        }
                        ClothesComponents.SetClothes(player, 7, Variation, Texture);
                        break;

                    case 5:
                    case 6:
                    case 9:
                    case 12:
                        //ApplyClothes(player);
                        ClothesComponents.SetTop(player, gender);
                        break;
                    case 7:


                        var bodyArmorsData = ClothesComponents.ClothesComponentData[gender][ClothesComponent.BodyArmors];

                        if (bodyArmorsData.ContainsKey(Variation))
                            Variation = bodyArmorsData[Variation].Variation;

                        if (Item.ItemId != ItemId.Debug && Variation == 0)
                        {
                            Variation = 12;
                            Texture = 1;
                        }
                        ClothesComponents.SetClothes(player, 9, Variation, Texture);

                        //
                        Item = GetItemData(player, "accessories", 7);

                        if (Item.ItemId == ItemId.Debug)
                        {
                            if (characterData != null && sessionData.ArmorHealth != -1)
                            {
                                sessionData.ArmorHealth = -1;
                                player.Armor = 0;
                                Trigger.ClientEvent(player, "client.isArmor", false);
                            }
                        }
                        else
                        {
                            if (int.TryParse(Item.Data, out int armdata) && characterData != null/* && sessionData.ArmorHealth == -1*/)
                            {
                                sessionData.ArmorHealth = armdata;
                                player.Armor = armdata;
                                Trigger.ClientEvent(player, "client.isArmor", true);

                                Main.OnAntiAnim(player);

                                if (!player.IsInVehicle)
                                    Trigger.PlayAnimation(player, "clothingshirt", "try_shirt_positive_d", 1);

                                Timers.StartOnce(1500, () =>
                                {
                                    try
                                    {
                                        if (!player.IsCharacterData()) return;

                                        if (!player.IsInVehicle) Trigger.StopAnimation(player);
                                        else Trigger.StopAnimation(player, false);

                                        Main.OffAntiAnim(player);
                                    }
                                    catch (Exception e)
                                    {
                                        Log.Write($"UseArmor Task Exception: {e.ToString()}");
                                    }
                                }, true);
                            }
                        }
                        break;
                    case 8:
                        if (characterData != null)
                            isBackpackItemsData(player, true);
                        if (Item.ItemId == ItemId.BagWithMoney)
                        {
                            if (int.TryParse(Item.Data, out int moneyinbag))
                            {
                                if (moneyinbag < SafeMain.MaxMoneyInBag)
                                    ClothesComponents.SetClothes(player, 5, 44, 0);
                                else
                                    ClothesComponents.SetClothes(player, 5, 45, 0);
                            }
                            else
                                ClothesComponents.SetClothes(player, 5, 44, 0);
                        }
                        else if (Item.ItemId == ItemId.BagWithDrill)
                            ClothesComponents.SetClothes(player, 5, 41, 0);
                        else if (Item.ItemId == ItemId.Bag)
                        {
                            var BugsData = ClothesComponents.ClothesBugsData;
                            if (BugsData.ContainsKey(Variation)) Variation = BugsData[Variation].Variation;
                            else Variation = BugsData[108].Variation;//Customization.EmtptySlots[gender][5];108

                            ClothesComponents.SetClothes(player, 5, Variation, Texture);
                        }
                        else
                            ClothesComponents.SetClothes(player, 5, Customization.EmtptySlots[gender][5], 0);
                        break;
                    case 10:
                        if (Item.ItemId == ItemId.Debug)
                        {
                            ClothesComponents.ClearAccessory(player, 7, isBlock: false);
                        }
                        else
                        {
                            var braceletsData = ClothesComponents.ClothesComponentData[gender][ClothesComponent.Bracelets];
                            if (braceletsData.ContainsKey(Variation)) Variation = braceletsData[Variation].Variation;

                            ClothesComponents.SetAccessories(player, 7, Variation, Texture);
                        }
                        break;
                    case 11:
                        if (Item.ItemId == ItemId.Debug)
                        {
                            ClothesComponents.ClearAccessory(player, 6, isBlock: false);
                        }
                        else
                        {
                            var watchesData = ClothesComponents.ClothesComponentData[gender][ClothesComponent.Watches];
                            if (watchesData.ContainsKey(Variation))
                                Variation = watchesData[Variation].Variation;

                            ClothesComponents.SetAccessories(player, 6, Variation, Texture);
                        }
                        break;
                    case 13:
                        if (Item.ItemId == ItemId.Debug)
                        {
                            Variation = Customization.EmtptySlots[gender][6];
                        }
                        else
                        {
                            ConcurrentDictionary<int, ClothesData> ShoesData = ClothesComponents.ClothesComponentData[gender][ClothesComponent.Shoes];
                            if (ShoesData.ContainsKey(Variation)) Variation = ShoesData[Variation].Variation;
                            else Variation = Customization.EmtptySlots[gender][6];
                        }
                        //ClothesComponents.SetSpecialClothes(player, 6, Variation, Texture);
                        ClothesComponents.SetClothes(player, 6, Variation, Texture);
                        break;
                    case 14:
                        if (Item.ItemId == ItemId.Debug)
                        {
                            var decalsData = ClothesComponents.ClothesComponentData[gender][ClothesComponent.Decals];
                            if (decalsData.ContainsKey(Variation))
                                Variation = decalsData[Variation].Variation;
                        }
                        //ClothesComponents.SetSpecialClothes(player, 6, Variation, Texture);
                        ClothesComponents.SetClothes(player, 10, Variation, Texture);
                        break;
                }
            }
            catch (Exception e)
            {
                Log.Write($"AccessoriesUse Exception: {e.ToString()}");
            }
        }
        #endregion

        #region Использование
        public static void ItemsUse(ExtPlayer player, string ArrayName, int Index, bool fastSlotsToggled = true)
        {
            try
            {
                var characterData = player.GetCharacterData();
                if (characterData == null) return;
                var sessionData = player.GetSessionData();
                if (sessionData == null) return;
                InventoryItemData Item = GetItemData(player, ArrayName, Index);
                if (Item.ItemId == ItemId.Debug) return;
                ItemsInfo ItemInfo = ItemsInfo[Item.ItemId];
                string Name = ItemInfo.Name;

                if (ArrayName == "accessories")
                {
                    if (Index == 8 && (Item.ItemId == ItemId.BagWithDrill || Item.ItemId == ItemId.BagWithMoney))
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.CantDoThisNow), 3000);
                        return;
                    }
                    if (Item.ItemId == ItemId.BodyArmor)
                    {
                        if (player.IsInVehicle)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.CantSnyatBronikInVeh), 3000);
                            return;
                        }

                        if (sessionData.ArmorHealth != -1 && player.Armor > sessionData.ArmorHealth) WeaponRepository.PlayerKickAntiCheat(player, 3, true);
                        Item.Data = player.Armor.ToString();
                    }

                    if (AddItem(player, $"char_{characterData.UUID}", "inventory", Item) == -1)
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.NoSpaceInventory), 3000);
                        return;
                    }
                    Sounds.PlayPlayer3d(player, "inventory/clothes");
                    SetItemData(player, ArrayName, Index, new InventoryItemData(), true);
                    return;
                }
                else if (ArrayName == "fastSlots" && fastSlotsToggled)
                {
                    WeaponRepository.RemoteEvent_changeWeapon(player, Index + 1);
                    return;
                }
                else if (ArrayName != "inventory" && ArrayName != "fastSlots")
                {
                    if (AddItem(player, $"char_{characterData.UUID}", "inventory", Item) == -1)
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.NoSpaceInventory), 3000);
                        return;
                    }
                    SetItemData(player, ArrayName, Index, new InventoryItemData(), true);
                    Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.YouGot, Name), 3000);
                    return;
                }

                bool gender = characterData.Gender;

                bool success = false;

                if (ItemInfo.functionType == newItemType.Clothes/* && Item.ItemId != ItemId.BodyArmor && Item.ItemId != ItemId.Mask*/)
                {
                    if (Item.ItemId == ItemId.BodyArmor && player.IsInVehicle)
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Невозможно надеть бронежилет находясь в транспорте.", 3000);
                        return;
                    }
                    if (Item.ItemId != ItemId.Bag && Item.ItemId != ItemId.BodyArmor && Item.ItemId != ItemId.Mask && gender != Item.GetGender())
                    {
                        string error_gender = (Item.GetGender()) ? LangFunc.GetText(LangType.Ru, DataName.MansC) : LangFunc.GetText(LangType.Ru, DataName.WomansC);
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.ErrorGender, error_gender), 3000);
                        return;
                    }
                    foreach (KeyValuePair<int, ItemId> key in AccessoriesInfo)
                    {
                        if (key.Value == Item.ItemId)
                        {
                            InventoryItemData AccessoriesItem = GetItemData(player, "accessories", key.Key);
                            if (key.Key == 8 && (AccessoriesItem.ItemId == ItemId.BagWithDrill || AccessoriesItem.ItemId == ItemId.BagWithMoney))
                            {
                                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.CantDoThisNow), 3000);
                                return;
                            }
                            if (AccessoriesItem.ItemId == ItemId.BodyArmor) AccessoriesItem.Data = player.Armor.ToString();
                            Sounds.PlayPlayer3d(player, "inventory/clothes");
                            if (AccessoriesItem.ItemId != ItemId.Debug) SetItemData(player, ArrayName, Index, AccessoriesItem, true);
                            else SetItemData(player, ArrayName, Index, new InventoryItemData(), true);
                            SetItemData(player, "accessories", key.Key, Item, true);
                            return;
                        }
                    }
                }
                else if (ItemInfo.functionType == newItemType.Weapons || ItemInfo.functionType == newItemType.MeleeWeapons)
                {
                    uint weaponId = (uint)WeaponRepository.GetHash(Item.ItemId.ToString());
                    if (WeaponComponents.WeaponsComponents.ContainsKey(weaponId))
                    {
                        if (sessionData.InventoryOtherLocationName == $"weapon_{Item.SqlId}") OtherClose(player);
                        else LoadOtherItemsData(player, "weapon", Item.SqlId.ToString(), 9, WeaponComponents.WeaponsComponents[weaponId].Count, $"{(int)Item.ItemId}_{Item.SqlId}");
                        return;
                    }
                    if (AddItem(player, $"char_{characterData.UUID}", "fastSlots", Item, MaxSlots: InventoryMaxSlots["fastSlots"]) == -1)
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.NoSpaceInventory), 3000);
                        return;
                    }
                    SetItemData(player, ArrayName, Index, new InventoryItemData(), true);
                    Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.FastSlotAdd), 3000);
                    return;
                }
                else if (Item.ItemId == ItemId.Spank)
                {
                    if (sessionData.ResistData.Time > 0)
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Вы уже под действием алкоголя/наркотиков.", 3000);
                        return;
                    }
                    if (DateTime.Now > sessionData.TimingsData.NextEat)
                    {

                        sessionData.TimingsData.NextEat = DateTime.Now.AddSeconds(3);

                        ResistData rdata = sessionData.ResistData;

                        Trigger.ClientEvent(player, "setResistStage", rdata.Stage);
                        Trigger.PlayAnimation(player, "mp_suicide", "pill_fp", 49);

                        NAPI.Task.Run(() =>
                        {
                            AlcoholBoost.Set(player, ItemId.Spank);
                            sessionData.ResistData.Time = 90;
                            player.Health = 100;
                            var time = $"{sessionData.ResistData.Time}";

                            Trigger.ClientEvent(player, "start:AlcoSystem::client");

                            if (sessionData.TimersData.ResistTimer != null) Timers.Stop(sessionData.TimersData.ResistTimer);
                            sessionData.TimersData.ResistTimer = Timers.Start(1000, () => AlcoFabrication.ResistTimer(player));

                            Trigger.StopAnimation(player);
                        }, 3300);

                        success = true;
                    }
                    else
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.CantDoThisNow), 3000);
                        return;
                    }
                }
               
                else if (Item.ItemId == ItemId.BioAdditiveLvl1)
                {
                    if (StartSkillBoost(player, 1800)) success = true; else return;
                }
                else if (Item.ItemId == ItemId.BioAdditiveLvl2)
                {
                    if (StartSkillBoost(player, 3600)) success = true; else return;
                }
                else if (Item.ItemId == ItemId.BioAdditiveLvl3)
                {
                    if (StartSkillBoost(player, 7200)) success = true; else return;
                }
                else if (Item.ItemId == ItemId.PizzaSlice)
                {
                    if (DateTime.Now > sessionData.TimingsData.NextEat)
                    {
                        sessionData.TimingsData.NextEat = DateTime.Now.AddSeconds(3);
                        characterData.Golod = Math.Min(characterData.Golod + 50, 100);
                        GolodSystem.GolodChanger(player);
                        Sounds.PlayPlayer3d(player, "inventory/eat");
                        success = true;
                    }
                    else
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.CantDoThisNow), 3000);
                        return;
                    }
                }
                else if (Item.ItemId == ItemId.WeaponRepairKit)
                {
                    Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, "Оружие отремонтировано", 3000);
                    success = true;
                }
                else if (Item.ItemId == ItemId.CarRepairKitBig)
                {
                    Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, "Автомобиль отремонтирован", 3000);
                    success = true;
                }
                else if (Item.ItemId == ItemId.ImmortalitixPill)
                {
                    if (Main.rnd.Next(2) == 0)
                    {
                        player.Health = 100;
                        Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, "Вы полностью здоровы", 3000);
                    }
                    else
                    {
                        player.Health = Math.Max(0, player.Health - 50);
                        Notify.Send(player, NotifyType.Warning, NotifyPosition.BottomCenter, "Побочный эффект!", 3000);
                    }
                    success = true;
                }
                else if (Item.ItemId == ItemId.CannedBeans)
                {
                    if (DateTime.Now > sessionData.TimingsData.NextEat)
                    {
                        sessionData.TimingsData.NextEat = DateTime.Now.AddSeconds(3);
                        characterData.Golod = Math.Min(characterData.Golod + 70, 100);
                        characterData.Water = Math.Min(characterData.Water + 70, 100);
                        GolodSystem.GolodChanger(player);
                        GolodSystem.WaterChanger(player);
                        Sounds.PlayPlayer3d(player, "inventory/eat");
                        success = true;
                    }
                    else
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.CantDoThisNow), 3000);
                        return;
                    }
                }
                else if (Item.ItemId == ItemId.HairStyling)
                {
                    if (StartHairStyling(player, 10800)) success = true; else return;
                }
                else if (Item.ItemId == ItemId.GovernorsLetter)
                {
                    if (StartSalaryBonus(player, 18000)) success = true; else return;
                }
                else if (Item.ItemId == ItemId.SpoiledBurger)
                {
                    characterData.Golod = 0;
                    characterData.Water = 0;
                    GolodSystem.GolodChanger(player);
                    GolodSystem.WaterChanger(player);
                    success = true;
                }
                else if (Item.ItemId == ItemId.CivilDrone)
                {
                    Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, "Дрон активирован", 3000);
                    success = true;
                }
                else if (Item.ItemId == ItemId.RecoveryCapsules)
                {
                    StartRecovery(player, 5);
                    success = true;
                }
                else if (Item.ItemId == ItemId.ProteinBar)
                {
                    if (StartSkillBoost(player, 1200)) success = true; else return;
                }
                else if (Item.ItemId == ItemId.AdvancedMetalDetector)
                {
                    Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, "Металлоискатель активирован", 3000);
                    success = true;
                }
                else if (Item.ItemId == ItemId.ClothesDiscount)
                {
                    Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, "Купон использован", 3000);
                    success = true;
                }
                else if (Item.ItemId == ItemId.RedneckCocktail)
                {
                    int r = Main.rnd.Next(3);
                    if (r == 0)
                    {
                        Trigger.ClientEvent(player, "startScreenEffect", "DrugsMichaelAliensFight", 60000, false);
                    }
                    else if (r == 1)
                    {
                        player.Health = 100;
                        characterData.Golod = 100;
                        characterData.Water = 100;
                        GolodSystem.GolodChanger(player);
                        GolodSystem.WaterChanger(player);
                    }
                    else
                    {
                        player.Health = Math.Max(0, player.Health - 30);
                    }
                    success = true;
                }
                else if (Item.ItemId == ItemId.Beer)
                {
                    Trigger.TaskPlayAnim(player, "amb@world_human_drinking@beer@male@idle_a", "idle_c", 49, attachmentName: "beer");
                    Sounds.PlayPlayer3d(player, "inventory/drink");
                    BattlePass.Repository.UpdateReward(player, 66);

                    //Attachments.AddAttachment(player, Attachments.AttachmentsName.Beer);
                    //Trigger.PlayAnimation(player, "amb@world_human_drinking@beer@male@idle_a", "idle_c", 49);
                    NAPI.Task.Run(() =>
                    {
                        try
                        {
                            if (player.IsCharacterData())
                            {
                                if (player.IsInVehicle) sessionData.ToResetAnimPhone = true;
                                Trigger.ClientEvent(player, "startScreenEffect", "PPFilter", 150000, false);
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Write($"ItemsUse Task#1 Exception: {e.ToString()}");
                        }
                    }, 3000);
                    Commands.RPChat("sme", player, LangFunc.GetText(LangType.Ru, DataName.Bahnuv) + (characterData.Gender ? "" : "а") + LangFunc.GetText(LangType.Ru, DataName.Pivka));

                    GameLog.Items($"usedItem({Item.SqlId})", $"char_{characterData.UUID}", Convert.ToInt32(Item.ItemId), 1, Item.Data);
                    success = true;
                }
                else if (ItemInfo.functionType == newItemType.Alco)
                {
                    int stage = Convert.ToInt32(Item.ItemId.ToString().Split("Drink")[1]);
                    stage = stage == -1 ? 0 : stage;

                    ResistData rdata = sessionData.ResistData;

                    if (rdata.Ban)
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.SoDrunk), 3000);
                        return;
                    }
                    int curStage = rdata.Stage;
                    int[] stageTimes = new int[4] { 0, 300, 420, 600 };

                    if (curStage == 0 || curStage == stage)
                    {
                        rdata.Stage = stage;
                        rdata.Time += stageTimes[stage];
                    }
                    else if (curStage < stage) rdata.Stage = stage;
                    else if (curStage > stage) rdata.Time += stageTimes[stage];

                    if (rdata.Time >= 1500) rdata.Ban = true;

                    Trigger.ClientEvent(player, "setResistStage", rdata.Stage);

                    Attachments.AddAttachment(player, ItemInfo.Model);

                    Main.OnAntiAnim(player);

                    if (!player.IsInVehicle) player.PlayAnimation("amb@world_human_drinking@beer@male@idle_a", "idle_c", 49);
                    // Trigger.ClientEventInRange(player.Position, 250f, "PlayAnimToKey", player, false, "drink");
                    Sounds.PlayPlayer3d(player, "inventory/drink");

                    NAPI.Task.Run(() =>
                    {
                        try
                        {
                            var sessionData = player.GetSessionData();
                            if (sessionData == null) return;
                            if (!player.IsInVehicle) Trigger.StopAnimation(player);
                            else sessionData.ToResetAnimPhone = true;
                            Main.OffAntiAnim(player);
                            Trigger.ClientEvent(player, "startScreenEffect", "PPFilter", sessionData.ResistData.Time * 300, false);
                            Attachments.RemoveAttachment(player, ItemInfo.Model);
                        }
                        catch (Exception e)
                        {
                            Log.Write($"ItemsUse Task#2 Exception: {e.ToString()}");
                        }
                    }, 3000);

                    if (sessionData.TimersData.ResistTimer != null) Timers.Stop(sessionData.TimersData.ResistTimer);
                    sessionData.TimersData.ResistTimer = Timers.Start(1000, () => AlcoFabrication.ResistTimer(player));
                    // Commands.RPChat("sme", player, $"выпил" + (characterData.Gender ? "" : "а") + $" бутылку {Name}");
                    GameLog.Items($"usedItem({Item.SqlId})", $"char_{characterData.UUID}", Convert.ToInt32(Item.ItemId), 1, Item.Data);
                    success = true;
                }
                else if (ItemInfo.functionType == newItemType.Improvement)
                {
                    EternalDev.Improvements.ImprovementsManager.OnUse(player, Item, ArrayName, Index);
                    return;
                }
                else if (ItemInfo.functionType == newItemType.Modification)
                {
                    // ✅ УНИВЕРСАЛЬНЫЕ МОДИФИКАЦИИ - НЕ ПРИВЯЗАНЫ К ОРУЖИЮ

                    // Ищем любое оружие в инвентаре
                    ItemStruct weaponStruct = null;

                    for (int i = 0; i < 102; i++)
                    {
                        var item = GetItemData(player, "inventory", i);
                        if (item.ItemId == ItemId.Debug) continue;

                        // Проверяем, является ли предмет оружием
                        if (ItemsInfo.ContainsKey(item.ItemId) &&
                            (ItemsInfo[item.ItemId].functionType == newItemType.Weapons ||
                             ItemsInfo[item.ItemId].functionType == newItemType.MeleeWeapons))
                        {
                            uint weaponHash = (uint)WeaponRepository.GetHash(item.ItemId.ToString());

                            // Проверяем, поддерживает ли оружие модификации
                            if (WeaponComponents.WeaponsComponents.ContainsKey(weaponHash))
                            {
                                weaponStruct = new ItemStruct("inventory", i, item);
                                break;
                            }
                        }
                    }

                    if (weaponStruct == null)
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter,
                            "У вас нет оружия, поддерживающего модификации!", 3000);
                        return;
                    }

                    uint weaponId = (uint)WeaponRepository.GetHash(weaponStruct.Item.ItemId.ToString());

                    // ✅ Проверяем, не установлена ли уже такая модификация
                    if (ItemsData.ContainsKey($"weapon_{weaponStruct.Item.SqlId}") &&
                        ItemsData[$"weapon_{weaponStruct.Item.SqlId}"].ContainsKey("weapon"))
                    {
                        foreach (InventoryItemData itemData in ItemsData[$"weapon_{weaponStruct.Item.SqlId}"]["weapon"].Values)
                        {
                            if (itemData.ItemId == Item.ItemId)
                            {
                                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter,
                                    LangFunc.GetText(LangType.Ru, DataName.AlreadyModifiedWeapon), 3000);
                                return;
                            }
                        }
                    }

                    // ✅ Добавляем модификацию в хранилище оружия
                    if (AddItem(player, $"weapon_{weaponStruct.Item.SqlId}", "weapon", Item,
                        MaxSlots: WeaponComponents.WeaponsComponents[weaponId].Count) == -1)
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter,
                            LangFunc.GetText(LangType.Ru, DataName.KeyChainNoSpace), 3000);
                        return;
                    }

                    SetItemData(player, ArrayName, Index, new InventoryItemData(), true);
                    Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter,
                        LangFunc.GetText(LangType.Ru, DataName.ModifWeapon), 3000);
                    return;
                }

                switch (Item.ItemId)
                {

                    case ItemId.KeyRing:
                        if (sessionData.InventoryOtherLocationName == $"CarKey_{Item.SqlId}") OtherClose(player);
                        else LoadOtherItemsData(player, "CarKey", Item.SqlId.ToString(), 7, InventoryMaxSlots["CarKey"], $"{(int)Item.ItemId}_{Item.SqlId}");
                        return;
                    case ItemId.CarKey:

                        ItemStruct aItemStruct = isItem(player, "inventory", ItemId.KeyRing);
                        if (aItemStruct == null) return;

                        if (AddItem(player, $"CarKey_{aItemStruct.Item.SqlId}", "CarKey", Item, MaxSlots: InventoryMaxSlots["CarKey"]) == -1)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.KeyChainNoSpace), 3000);
                            return;
                        }
                        SetItemData(player, ArrayName, Index, new InventoryItemData(), true);
                        Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.KeyChainSucc), 3000);
                        BattlePass.Repository.UpdateReward(player, 73);
                        return;
                    case ItemId.Material:
                        ItemsClose(player, true);

                        Manager.OpenGunCraftMenu(player);
                        return;
                    case ItemId.Burger:
                        if (DateTime.Now > sessionData.TimingsData.NextEat)
                        {
                            /*if (player.Health >= 100 || sessionData.InTanksLobby > -1)
                            {
                                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.DontWantToEat), 3000);
                                return;
                            }*/
                            sessionData.TimingsData.NextEat = DateTime.Now.AddSeconds(5);
                            if (sessionData.ResistData.Time < 600) Trigger.ClientEvent(player, "stopScreenEffect", "PPFilter");
                            // Commands.RPChat("sme", player, $"съел" + (characterData.Gender ? "" : "а") + $" {ItemInfo.Name}");
                            BattlePass.Repository.UpdateReward(player, 140);
                            UseEat(player, 1);
                            Sounds.PlayPlayer3d(player, "inventory/eat");
                        }
                        else
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.CantDoThisNow), 3000);
                            return;
                        }
                        break;
                    case ItemId.Drugs:
                        if (DateTime.Now > sessionData.TimingsData.NextDrugs)
                        {
                            if (sessionData.InTanksLobby > -1) return;
                            Trigger.ClientEvent(player, "startScreenEffect", "DrugsTrevorClownsFight", 300000, false);
                            BattlePass.Repository.UpdateReward(player, 139);
                            switch (Main.rnd.Next(10))
                            {
                                case 0:
                                    Commands.RPChat("sme", player, $"задымил" + (characterData.Gender ? "" : "а") + " блант");
                                    break;
                                case 1:
                                    Commands.RPChat("sme", player, $"вмазал" + (characterData.Gender ? "" : "а") + " плюшку");
                                    break;
                                case 2:
                                    Commands.RPChat("sme", player, $"заварил" + (characterData.Gender ? "" : "а") + " плюху");
                                    break;
                                case 3:
                                    Commands.RPChat("sme", player, $"подкурил" + (characterData.Gender ? "" : "а") + " джоинт");
                                    break;
                                case 4:
                                    Commands.RPChat("sme", player, $"сделал" + (characterData.Gender ? "" : "а") + " хапку");
                                    break;
                                case 5:
                                    Commands.RPChat("sme", player, $"дунул" + (characterData.Gender ? "" : "а") + " трубку");
                                    break;
                                case 6:
                                    Commands.RPChat("sme", player, $"затянул" + (characterData.Gender ? "" : "а") + " дурь");
                                    break;
                                case 7:
                                    Commands.RPChat("sme", player, $"взорвал" + (characterData.Gender ? "" : "а") + " ракету");
                                    break;
                                case 8:
                                    Commands.RPChat("sme", player, $"хапнул" + (characterData.Gender ? "" : "а") + " шмальца");
                                    break;
                                default:
                                    Commands.RPChat("sme", player, $"закурил" + (characterData.Gender ? "" : "а") + " косяк");
                                    break;
                            }
                            sessionData.TimingsData.NextDrugs = DateTime.Now.AddMinutes(1.5);
                            UseEat(player, 2);
                        }
                        else
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.DrugsTooMany), 3000);
                            return;
                        }
                        break;
                    case ItemId.Cocaine:
                        if (DateTime.Now > sessionData.TimingsData.NextDrugs)
                        {
                            if (sessionData.InTanksLobby > -1) return;
                            Trigger.ClientEvent(player, "startScreenEffect", "DrugsTrevorClownsFight", 300000, false);
                            BattlePass.Repository.UpdateReward(player, 139);
                            switch (Main.rnd.Next(10))
                            {
                                case 0:
                                    Commands.RPChat("sme", player, $"нюхнул" + (characterData.Gender ? "" : "а") + " дорожку");
                                    break;
                                case 1:
                                    Commands.RPChat("sme", player, $"вмазал" + (characterData.Gender ? "" : "а") + " дорожку");
                                    break;
                                case 2:
                                    Commands.RPChat("sme", player, $"носиком всосал" + (characterData.Gender ? "" : "а") + " дорожку");
                                    break;
                                case 3:
                                    Commands.RPChat("sme", player, $"занюхнул" + (characterData.Gender ? "" : "а") + " дорогу");
                                    break;
                                case 4:
                                    Commands.RPChat("sme", player, $"сделал" + (characterData.Gender ? "" : "а") + " внюх");
                                    break;
                                case 5:
                                    Commands.RPChat("sme", player, $"дунул" + (characterData.Gender ? "" : "а") + " кокоса");
                                    break;
                                case 6:
                                    Commands.RPChat("sme", player, $"нюхнул" + (characterData.Gender ? "" : "а") + " дурь");
                                    break;
                                case 7:
                                    Commands.RPChat("sme", player, $"поднюхнул" + (characterData.Gender ? "" : "а") + " кокосик");
                                    break;
                                case 8:
                                    Commands.RPChat("sme", player, $"хапнул" + (characterData.Gender ? "" : "а") + " кокосик");
                                    break;
                                default:
                                    Commands.RPChat("sme", player, $"занюхнул" + (characterData.Gender ? "" : "а") + " кокс");
                                    break;
                            }
                            sessionData.TimingsData.NextDrugs = DateTime.Now.AddMinutes(1.5);
                            UseEat(player, 9);
                        }
                        else
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.DrugsTooMany), 3000);
                            return;
                        }
                        break;
                    case ItemId.eCola:
                        if (DateTime.Now > sessionData.TimingsData.NextEat)
                        {
                            /*if (player.Health >= 100 || sessionData.InTanksLobby > -1)
                            {
                                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.DontWantToEat), 3000);
                                return;
                            }*/
                            sessionData.TimingsData.NextEat = DateTime.Now.AddSeconds(5);
                            Commands.RPChat("sme", player, LangFunc.GetText(LangType.Ru, DataName.Drink) + (characterData.Gender ? "" : "а") + $" {ItemInfo.Name}");
                            BattlePass.Repository.UpdateReward(player, 138);
                            UseEat(player, 3);
                            Sounds.PlayPlayer3d(player, "inventory/drink");
                        }
                        else
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.CantDoThisNow), 3000);
                            return;
                        }
                        break;
                    case ItemId.GasCan:
                        if (!player.IsInVehicle)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.MustInCar), 3000);
                            ItemsClose(player, true);
                            return;
                        }
                        var veh = (ExtVehicle)player.Vehicle;
                        var vehicleLocalData = veh.GetVehicleLocalData();
                        if (vehicleLocalData != null)
                        {
                            if (vehicleLocalData.Petrol <= -1) return;
                            int fuel = vehicleLocalData.Petrol;
                            if (fuel >= VehicleManager.VehicleTank[veh.Class])
                            {
                                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.FullFuel), 3000);
                                ItemsClose(player, true);
                                return;
                            }
                            fuel += 15;

                            if (fuel > VehicleManager.VehicleTank[veh.Class])
                                fuel = VehicleManager.VehicleTank[veh.Class];

                            veh.SetSharedData("PETROL", fuel);
                            vehicleLocalData.Petrol = fuel;
                            if (vehicleLocalData.Access == VehicleAccess.Garage || vehicleLocalData.Access == VehicleAccess.Personal)
                            {
                                string number = player.Vehicle.NumberPlate;
                                var vehicleData = VehicleManager.GetVehicleToNumber(number);
                                if (vehicleData != null)
                                    vehicleData.Fuel = fuel;
                            }
                            BattlePass.Repository.UpdateReward(player, 42);
                        }
                        break;
                    case ItemId.HealthKit:
                        if (DateTime.Now > sessionData.TimingsData.NextMedKit)
                        {

                            // Если у игрока уже 100 HP — показываем ошибку и не применяем аптечку
                            if (player.Health >= 100)
                            {
                                Notify.Send(
                                    player,
                                    NotifyType.Error,
                                    NotifyPosition.BottomCenter,
                                    LangFunc.GetText(LangType.Ru, DataName.YouFullHP),
                                    3000
                                );
                                return;
                            }

                            // Добавляем 50 HP, но не больше 100
                            const int healAmount = 50;
                            int newHealth = player.Health + healAmount;
                            player.Health = newHealth > 100 ? 100 : newHealth;

                            sessionData.TimingsData.NextMedKit = DateTime.Now.AddSeconds(5);
                            Commands.RPChat("sme", player, LangFunc.GetText(LangType.Ru, DataName.used) + (characterData.Gender ? "" : "а") + LangFunc.GetText(LangType.Ru, DataName.healthkity));
                            Main.OnAntiAnim(player);
                            Sounds.PlayPlayer3d(player, "inventory/medkit");
                            Trigger.ClientEvent(player, "blockMove", true);
                            if (!player.IsInVehicle)
                            {
                                player.PlayAnimation("amb@code_human_wander_texting_fat@female@enter", "enter", 49);
                                //, attachmentName: "HealthKit"
                            }
                            // Trigger.ClientEventInRange(player.Position, 250f, "PlayAnimToKey", player, false, "healthkit");
                            NAPI.Task.Run(() =>
                            {
                                try
                                {
                                    if (!player.IsCharacterData()) return;
                                    if (!player.IsInVehicle) Trigger.StopAnimation(player);
                                    else sessionData.ToResetAnimPhone = true;
                                    Main.OffAntiAnim(player);
                                    Trigger.ClientEvent(player, "stopScreenEffect", "PPFilter");
                                    Trigger.ClientEvent(player, "blockMove", false);
                                }
                                catch (Exception e)
                                {
                                    Log.Write($"ItemsUse Task#3 Exception: {e.ToString()}");
                                }
                            }, 3000);
                        }
                        else
                        {
                            long ticks = sessionData.TimingsData.NextMedKit.Ticks - DateTime.Now.Ticks;
                            if (ticks <= 0) return;
                            DateTime g = new DateTime(ticks);
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.AptekCooldown, g.Minute, g.Second), 3000);
                            return;
                        }
                        BattlePass.Repository.UpdateReward(player, 94);
                        break;
                    case ItemId.HealthKit2:
                        if (DateTime.Now > sessionData.TimingsData.NextMedKit)
                        {
                            if (player.Health >= 100)
                            {
                                Notify.Send(
                                    player,
                                    NotifyType.Error,
                                    NotifyPosition.BottomCenter,
                                    LangFunc.GetText(LangType.Ru, DataName.YouFullHP),
                                    3000
                                );
                                return;
                            }
                            const int healAmount = 75;

                            // Рассчитываем новое здоровье (с учётом максимума 100)
                            int newHealth = player.Health + healAmount;
                            player.Health = newHealth > 100 ? 100 : newHealth;

                            sessionData.TimingsData.NextMedKit = DateTime.Now.AddSeconds(5);
                            Commands.RPChat("sme", player, LangFunc.GetText(LangType.Ru, DataName.used) + (characterData.Gender ? "" : "а") + LangFunc.GetText(LangType.Ru, DataName.healthkity));
                            Main.OnAntiAnim(player);
                            Sounds.PlayPlayer3d(player, "inventory/medkit");
                            Trigger.ClientEvent(player, "blockMove", true);
                            if (!player.IsInVehicle)
                            {
                                player.PlayAnimation("amb@code_human_wander_texting_fat@female@enter", "enter", 49);
                                //, attachmentName: "HealthKit"
                            }
                            // Trigger.ClientEventInRange(player.Position, 250f, "PlayAnimToKey", player, false, "healthkit");
                            NAPI.Task.Run(() =>
                            {
                                try
                                {
                                    if (!player.IsCharacterData()) return;
                                    if (!player.IsInVehicle) Trigger.StopAnimation(player);
                                    else sessionData.ToResetAnimPhone = true;
                                    Main.OffAntiAnim(player);
                                    Trigger.ClientEvent(player, "stopScreenEffect", "PPFilter");
                                    Trigger.ClientEvent(player, "blockMove", false);
                                }
                                catch (Exception e)
                                {
                                    Log.Write($"ItemsUse Task#3 Exception: {e.ToString()}");
                                }
                            }, 3000);
                        }
                        else
                        {
                            long ticks = sessionData.TimingsData.NextMedKit.Ticks - DateTime.Now.Ticks;
                            if (ticks <= 0) return;
                            DateTime g = new DateTime(ticks);
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.AptekCooldown, g.Minute, g.Second), 3000);
                            return;
                        }
                        BattlePass.Repository.UpdateReward(player, 94);
                        break;
                    case ItemId.Epinephrine:
                        if (player.Health >= 100)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.YouFullHP), 3000);
                            return;
                        }
                        player.Health = 100;
                        sessionData.TimingsData.NextMedKit = DateTime.Now.AddMinutes(5);
                        Commands.RPChat("sme", player, LangFunc.GetText(LangType.Ru, DataName.used) + (characterData.Gender ? "" : "а") + LangFunc.GetText(LangType.Ru, DataName.healthkity));
                        Main.OnAntiAnim(player);
                        Sounds.PlayPlayer3d(player, "inventory/medkit");
                        Trigger.ClientEvent(player, "blockMove", true);


                        // Trigger.ClientEventInRange(player.Position, 250f, "PlayAnimToKey", player, false, "healthkit");
                        NAPI.Task.Run(() =>
                        {
                            try
                            {
                                if (!player.IsCharacterData()) return;
                                if (!player.IsInVehicle) Trigger.StopAnimation(player);
                                else sessionData.ToResetAnimPhone = true;
                                Main.OffAntiAnim(player);
                                Trigger.ClientEvent(player, "stopScreenEffect", "PPFilter");
                                Trigger.ClientEvent(player, "blockMove", false);
                            }
                            catch (Exception e)
                            {
                                Log.Write($"ItemsUse Task#3 Exception: {e.ToString()}");
                            }
                        }, 3000);

                        BattlePass.Repository.UpdateReward(player, 94);
                        break;
                    case ItemId.Bint:
                        if (DateTime.Now > sessionData.TimingsData.NextMedKit)
                        {
                            if (player.Health >= 100)
                            {
                                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.YouFullHP), 3000);
                                return;
                            }

                            player.Health += 40;
                            sessionData.TimingsData.NextMedKit = DateTime.Now.AddSeconds(30);
                            Commands.RPChat("sme", player, LangFunc.GetText(LangType.Ru, DataName.used) + (characterData.Gender ? "" : "а") + LangFunc.GetText(LangType.Ru, DataName.bint));
                            Main.OnAntiAnim(player);
                            Sounds.PlayPlayer3d(player, "inventory/medkit");
                            Trigger.ClientEvent(player, "blockMove", true);
                            if (!player.IsInVehicle) player.PlayAnimation("amb@code_human_wander_texting@female@enter", "enter", 49);
                            // Trigger.ClientEventInRange(player.Position, 250f, "PlayAnimToKey", player, false, "healthkit");
                            NAPI.Task.Run(() =>
                            {
                                try
                                {
                                    if (!player.IsCharacterData()) return;
                                    if (!player.IsInVehicle) Trigger.StopAnimation(player);
                                    else sessionData.ToResetAnimPhone = true;
                                    Main.OffAntiAnim(player);
                                    Trigger.ClientEvent(player, "stopScreenEffect", "PPFilter");
                                    Trigger.ClientEvent(player, "blockMove", false);
                                }
                                catch (Exception e)
                                {
                                    Log.Write($"ItemsUse Task#3 Exception: {e.ToString()}");
                                }
                            }, 3000);
                        }
                        else
                        {
                            long ticks = sessionData.TimingsData.NextMedKit.Ticks - DateTime.Now.Ticks;
                            if (ticks <= 0) return;
                            DateTime g = new DateTime(ticks);
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.BintCooldown, g.Minute, g.Second), 3000);
                            return;
                        }
                        break;
                    case ItemId.HotDog:
                        if (DateTime.Now > sessionData.TimingsData.NextEat)
                        {
                            /*if (player.Health >= 100 || sessionData.InTanksLobby > -1)
                            {
                                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.DontWantToEat), 3000);
                                return;
                            }*/
                            sessionData.TimingsData.NextEat = DateTime.Now.AddSeconds(5);
                            if (sessionData.ResistData.Time < 600) Trigger.ClientEvent(player, "stopScreenEffect", "PPFilter");
                            //  Commands.RPChat("sme", player, $"съел" + (characterData.Gender ? "" : "а") + $" {ItemInfo.Name}");
                            BattlePass.Repository.UpdateReward(player, 137);
                            UseEat(player, 4);
                            Sounds.PlayPlayer3d(player, "inventory/eat");
                        }
                        else
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.CantDoThisNow), 3000);
                            return;
                        }
                        break;
                    case ItemId.ArmyLockpick:
                        if (!player.IsInVehicle || player.Vehicle.Model != (uint)VehicleHash.Barracks && player.Vehicle.Model != (uint)VehicleHash.Brickade)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.MustBeInArmyTruck), 3000);
                            return;
                        }
                        var vehicle = (ExtVehicle)player.Vehicle;
                        if (VehicleStreaming.GetEngineState(vehicle))
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.CarAlreadyOn), 3000);
                            return;
                        }
                        int lucky = new Random().Next(0, 5);
                        if (lucky == 5) Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.ErrorCarOn), 3000);
                        else
                        {
                            Sounds.PlayPlayer3d(player, "inventory/keys");
                            VehicleStreaming.SetEngineState(vehicle, true);
                            Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.CarOn), 3000);
                        }
                        break;
                    case ItemId.Pizza:
                        if (DateTime.Now > sessionData.TimingsData.NextEat)
                        {
                            /*if (player.Health >= 100 || sessionData.InTanksLobby > -1)
                            {
                                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.DontWantToEat), 3000);
                                return;
                            }*/
                            sessionData.TimingsData.NextEat = DateTime.Now.AddSeconds(5);
                            if (sessionData.ResistData.Time < 600) Trigger.ClientEvent(player, "stopScreenEffect", "PPFilter");
                            // Commands.RPChat("sme", player, $"съел" + (characterData.Gender ? "" : "а") + $" {ItemInfo.Name}");
                            BattlePass.Repository.UpdateReward(player, 134);
                            UseEat(player, 5);
                            Sounds.PlayPlayer3d(player, "inventory/eat");
                        }
                        else
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.CantDoThisNow), 3000);
                            return;
                        }
                        break;
                    case ItemId.Sandwich:
                        if (DateTime.Now > sessionData.TimingsData.NextEat)
                        {
                            /*if (player.Health >= 100 || sessionData.InTanksLobby > -1)
                            {
                                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.DontWantToEat), 3000);
                                return;
                            }*/
                            sessionData.TimingsData.NextEat = DateTime.Now.AddSeconds(5);
                            if (sessionData.ResistData.Time < 600) Trigger.ClientEvent(player, "stopScreenEffect", "PPFilter");
                            //  Commands.RPChat("sme", player, $"съел" + (characterData.Gender ? "" : "а") + $" {ItemInfo.Name}");
                            BattlePass.Repository.UpdateReward(player, 135);
                            UseEat(player, 6);
                            Sounds.PlayPlayer3d(player, "inventory/eat");
                        }
                        else
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.CantDoThisNow), 3000);
                            return;
                        }
                        break;
                    case ItemId.Sprunk:
                        if (DateTime.Now > sessionData.TimingsData.NextEat)
                        {
                            /*if (player.Health >= 100 || sessionData.InTanksLobby > -1)
                            {
                                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.DontWantToEat), 3000);
                                return;
                            }*/
                            sessionData.TimingsData.NextEat = DateTime.Now.AddSeconds(5);
                            if (sessionData.ResistData.Time < 600) Trigger.ClientEvent(player, "stopScreenEffect", "PPFilter");
                            // Commands.RPChat("sme", player, $"выпил" + (characterData.Gender ? "" : "а") + $" {ItemInfo.Name}");
                            BattlePass.Repository.UpdateReward(player, 136);
                            UseEat(player, 7);
                            Sounds.PlayPlayer3d(player, "inventory/drink");
                        }
                        else
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.CantDoThisNow), 3000);
                            return;
                        }
                        break;
                    case ItemId.Crisps:
                        if (DateTime.Now > sessionData.TimingsData.NextEat)
                        {
                            /*if (player.Health >= 100 || sessionData.InTanksLobby > -1)
                            {
                                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.DontWantToEat), 3000);
                                return;
                            }*/
                            sessionData.TimingsData.NextEat = DateTime.Now.AddSeconds(5);
                            if (sessionData.ResistData.Time < 600) Trigger.ClientEvent(player, "stopScreenEffect", "PPFilter");
                            // Commands.RPChat("sme", player, $"съел" + (characterData.Gender ? "" : "а") + $" {ItemInfo.Name}");
                            BattlePass.Repository.UpdateReward(player, 61);
                            UseEat(player, 8);
                            Sounds.PlayPlayer3d(player, "inventory/eat");
                        }
                        else
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.CantDoThisNow), 3000);
                            return;
                        }
                        break;
                    case ItemId.Present:
                        Main.CheckMyBonusCode(player, Item.Data);
                        return;
                    case ItemId.BagWithDrill:
                    case ItemId.BagWithMoney:
                        InventoryItemData AccessoriesItem = GetItemData(player, "accessories", 8);
                        if (AccessoriesItem.ItemId != ItemId.Debug) SetItemData(player, ArrayName, Index, AccessoriesItem, true);
                        else SetItemData(player, ArrayName, Index, new InventoryItemData(), true);
                        SetItemData(player, "accessories", 8, Item, true);
                        Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.SucWearing, ItemInfo.Name), 3000);
                        return;
                    case ItemId.CarCoupon:
                        int vehiclesCount = VehicleManager.GetVehiclesCarCountToPlayer(player.Name);
                        if (vehiclesCount >= GarageManager.MaxGarageCars)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.MaxcarsCoupon), 6000);
                            return;
                        }
                        var house = HouseManager.GetHouse(player, true);
                        if (house != null)
                        {
                            var garage = house.GetGarageData();
                            if (garage == null || vehiclesCount >= GarageManager.GarageTypes[garage.Type].MaxCars)
                            {
                                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.MaxcarsCoupon), 6000);
                                return;
                            }
                        }
                        VehicleManager.Create(player, Item.Data, new Color(225, 225, 225), new Color(225, 225, 225), Text: LangFunc.GetText(LangType.Ru, DataName.CouponActivate, Item.Data), Logs: $"CarCoupon({Item.Data}");
                        break;
                    case ItemId.Note:
                    case ItemId.LoveNote:
                        ItemsClose(player, true);
                        LoadNote(player, Item);
                        return;
                    case ItemId.Vape:
                        int value = 0;
                        try
                        {
                            value = Convert.ToInt32(Item.Data);
                        }
                        catch
                        {
                            value = 0;
                        }
                        if (value <= 0)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.VapeBroken), 3000);
                            Chars.Repository.RemoveIndex(player, ArrayName, Index);
                            return;
                        }
                        Item.Data = $"{value - 1}";
                        SetItemData(player, ArrayName, Index, Item, true);
                        UseSmoke(player);
                        return;
                    case ItemId.Hookah:
                        ItemsToput(player, ArrayName, Index);
                        BattlePass.Repository.UpdateReward(player, 67);
                        return;
                    //
                    case ItemId.Fire:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Matras:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Tent:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Lezhak:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Towel:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Flag:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Barrell:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Surf:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Vedro:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Flagstok:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Tenttwo:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Polotence:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Beachbag:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Zontik:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Zontiktwo:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Zontikthree:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Closedzontik:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Vball:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Bball:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Boomboxxx:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Table:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Tabletwo:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Tablethree:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Tablefour:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Chair:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Chairtwo:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Chaierthree:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Chaierfour:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Chairtable:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Korzina:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Light:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Alco:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Alcotwo:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Alcothree:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Alcofour:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Cocktail:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Cocktailtwo:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Fruit:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Fruittwo:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Packet:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Buter:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Patatoes:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Coffee:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Podnosfood:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Bbqtwo:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Bbq:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Firework1:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Firework2:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Firework3:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Firework4:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Vaza:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Flagwtokk:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Flagau:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Flagbr:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Flagch:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Flagcz:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Flageng:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Flageu:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Flagfin:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Flagfr:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Flagger:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Flagire:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Flagisr:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Flagit:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Flagjam:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Flagjap:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Flagmex:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Flagnig:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Flagnorw:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Flagpol:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Flagrus:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Flagbel:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Flagscot:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Flagscr:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Flagslovak:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Flagslov:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Flagsou:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Flagspain:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Flagswede:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Flagswitz:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Flagturk:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Flaguk:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Flagus:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Flagwales:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Flowerrr:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Konus:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Konuss:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Otboynik1:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Otboynik2:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Dontcross:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Stop:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.NetProezda:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Kpp:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Zabor1:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Zabor2:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Airlight:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Camera1:
                        ItemsToput(player, ArrayName, Index);
                        return;
                    case ItemId.Camera2:
                        ItemsToput(player, ArrayName, Index);
                        return;

                    //
                    case ItemId.Bong:
                        value = 0;
                        try
                        {
                            value = Convert.ToInt32(Item.Data);
                        }
                        catch
                        {
                            value = 0;
                        }
                        if (value <= 0)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.VapeBroken), 3000);
                            Chars.Repository.RemoveIndex(player, ArrayName, Index);
                            return;
                        }
                        Item.Data = $"{value - 1}";
                        SetItemData(player, ArrayName, Index, Item, true);
                        UseBong(player);
                        BattlePass.Repository.UpdateReward(player, 69);
                        return;
                    //case ItemId.BigGift:
                    //  case ItemId.MediumGift:
                    //  case ItemId.SmallGift:
                    //      ItemsClose(player, true);
                    //      NeptuneEvo.Events.Christmas.Gifts.GiftsManager.Open(player, Item);
                    //    return;
                    case ItemId.BigGift:
                    case ItemId.MediumGift:
                    case ItemId.SmallGift:
                        ItemsClose(player, true);
                        NeptuneEvo.Events.Christmas.Gifts.GiftsManager.Open(player, Item);
                        return;
                    case ItemId.Biolink:
                        if (sessionData.TimersData.BiolinkTimer != null)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Эффект уже активен", 3000);
                            return;
                        }
                        if (DateTime.Now < sessionData.TimingsData.NextBiolink)
                        {
                            long ticks = sessionData.TimingsData.NextBiolink.Ticks - DateTime.Now.Ticks;
                            if (ticks > 0)
                            {
                                DateTime g = new DateTime(ticks);
                                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.BiolinkCooldown, g.Hour, g.Minute), 3000);
                                return;
                            }
                        }
                        sessionData.TimingsData.NextEat = DateTime.Now.AddSeconds(5);
                        UseEat(player, 10);
                        characterData.Golod = 100;
                        characterData.Water = 100;
                        GolodSystem.GolodChanger(player);
                        GolodSystem.WaterChanger(player);
                        if (sessionData.TimersData.GolodTimer != null) Timers.Stop(sessionData.TimersData.GolodTimer);
                        if (sessionData.TimersData.WaterTimer != null) Timers.Stop(sessionData.TimersData.WaterTimer);
                        characterData.BaffTime = 18000;
                        MySQL.Query($"UPDATE `characters` SET `baff` = {characterData.BaffTime} WHERE `uuid` = {characterData.UUID}");
                        sessionData.TimingsData.NextBiolink = DateTime.Now.AddHours(5);
                        sessionData.TimersData.BiolinkTimer = Timers.Start(30000, () => BiolinkTimer(player));
                        Sounds.PlayPlayer3d(player, "inventory/eat");
                        Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.YouUsed, Name), 3000);
                        GameLog.Items($"usedItem({Item.SqlId})", $"char_{characterData.UUID}", Convert.ToInt32(Item.ItemId), 1, Item.Data);
                        RemoveIndex(player, ArrayName, Index, 1);
                        ItemsClose(player, true);
                        return;
                    case ItemId.SimCard:
                        if (characterData.UUID == Convert.ToInt32(Item.Data))
                            return;

                        var phoneData = player.getPhoneData();
                        if (phoneData == null || phoneData.Settings == null)
                            return;

                        if (phoneData.Settings.SimUpdateAntiFlood > DateTime.Now)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Сим карту можно менять раз в 5 минут", 3000);
                            return;
                        }
                        phoneData.Settings.SimUpdateAntiFlood = DateTime.Now.AddMinutes(5);

                        var sim = Players.Phone.Settings.Repository.OnAddSim(player, Convert.ToInt32(Item.Data));

                        if (sim != -1)
                        {
                            Item.Data = sim.ToString();
                            SetItemData(player, ArrayName, Index, Item, true);
                            ItemsClose(player, true);
                            return;
                        }
                        break;
                    default:
                        if (!success)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.CantDoThisNowv2), 3000);
                            return;
                        }
                        break;
                }
                Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.YouUsed, Name), 3000);
                GameLog.Items($"usedItem({Item.SqlId})", $"char_{characterData.UUID}", Convert.ToInt32(Item.ItemId), 1, Item.Data);
                RemoveIndex(player, ArrayName, Index, 1);
                ItemsClose(player, true);

                if (Item.ItemId == ItemId.SimCard)
                    Players.Phone.Sim.Repository.Add(Convert.ToInt32(Item.Data));
            }
            catch (Exception e)
            {
                Log.Write($"ItemsUse Exception: {e.ToString()}");
            }
            return;
        }
        public static void LoadNote(ExtPlayer player, InventoryItemData Item)
        {
            try
            {
                if (!player.IsCharacterData()) return;
                Trigger.SetTask(() =>
                {
                    LoadNoteThread(player, Item);
                });
            }
            catch (Exception e)
            {
                Log.Write($"LoadNote Exception: {e.ToString()}");
            }
        }
        [RemoteEvent("server.gamemenu.inventory.takehands")]
        public static void TakeWeaponInHands(ExtPlayer player, string arrayName, int index)
        {
            try
            {
                if (!player.IsCharacterData()) return;

                var characterData = player.GetCharacterData();
                if (characterData == null) return;

                var sessionData = player.GetSessionData();
                if (sessionData == null) return;

                // ✅ ПОЛУЧАЕМ ПРЕДМЕТ ИЗ ИНВЕНТАРЯ
                InventoryItemData item = GetItemData(player, arrayName, index);

                if (item.ItemId == ItemId.Debug)
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Предмет не найден!", 3000);
                    return;
                }

                // ✅ ПРОВЕРЯЕМ, ЧТО ЭТО ОРУЖИЕ
                if (!ItemsInfo.ContainsKey(item.ItemId))
                    return;

                var itemInfo = ItemsInfo[item.ItemId];

                if (itemInfo.functionType != newItemType.Weapons && itemInfo.functionType != newItemType.MeleeWeapons)
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Это не оружие!", 3000);
                    return;
                }

                // ✅ ПРОВЕРЯЕМ АРЕСТ/ДЕММОРГАН
                if (characterData.ArrestTime >= 1 || characterData.DemorganTime >= 1)
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Вы не можете брать оружие сейчас!", 3000);
                    return;
                }

                // ✅ ПРОВЕРЯЕМ, ЕСТЬ ЛИ УЖЕ ОРУЖИЕ В РУКАХ
                if (sessionData.ActiveWeap != null && sessionData.ActiveWeap.Item != null && sessionData.ActiveWeap.Item.ItemId != ItemId.Debug)
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Сначала уберите текущее оружие!", 3000);
                    return;
                }

                // ✅ ЗАПОМИНАЕМ ОРУЖИЕ (БЕЗ УДАЛЕНИЯ ИЗ ИНВЕНТАРЯ!)
                sessionData.ActiveWeap = new ItemStruct(arrayName, index, item);
                sessionData.LastActiveWeap = item.SqlId;

                // ✅ ВЫДАЁМ ОРУЖИЕ В РУКИ
                Hash weaponHash = (Hash)WeaponRepository.GetHash(item.ItemId.ToString());

                if (WeaponRepository.WeaponsAmmoTypes.ContainsKey(item.ItemId))
                {
                    ItemId ammoType = WeaponRepository.WeaponsAmmoTypes[item.ItemId];
                    ItemStruct ammoStruct = isItem(player, "inventory", ammoType);
                    int ammoCount = 0;

                    if (ammoStruct != null && ammoStruct.Item != null)
                    {
                        int maxAmmo = WeaponRepository.WeaponsClipsMax[item.ItemId];
                        ammoCount = Math.Min(ammoStruct.Item.Count, maxAmmo);

                        if (ammoCount > 0)
                        {
                            RemoveIndex(player, ammoStruct.Location, ammoStruct.Index, ammoCount);
                        }
                    }

                    Trigger.ClientEvent(player, "client.weapon.give", (int)weaponHash, ammoCount, false, item.ItemId);
                }
                else
                {
                    int ammo = item.ItemId == ItemId.Snowball ? 10 : 1;
                    Trigger.ClientEvent(player, "client.weapon.give", (int)weaponHash, ammo, false, item.ItemId);
                }

                WeaponComponents.Give(player, (uint)weaponHash, $"weapon_{item.SqlId}", "weapon");

                // ✅ ОТПРАВЛЯЕМ UI (БЕЗ УДАЛЕНИЯ ИЗ ИНВЕНТАРЯ!)
                Trigger.ClientEvent(player, "client.inventory.setActiveWeapon", item.SqlId, (int)item.ItemId, item.Data, index);

                // ✅ РП чат
                Commands.RPChat("sme", player, $"достал" + (characterData.Gender ? "" : "а") + $" {itemInfo.Name}");

                Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы достали {itemInfo.Name}", 3000);
            }
            catch (Exception e)
            {
                Log.Write($"TakeWeaponInHands Exception: {e.ToString()}");
            }
        }
        [RemoteEvent("server.weapon.take")]
        public static void TakeWeaponBack(ExtPlayer player)
        {
            try
            {
                if (!player.IsCharacterData()) return;

                var characterData = player.GetCharacterData();
                if (characterData == null) return;

                var sessionData = player.GetSessionData();
                if (sessionData == null) return;

                // ✅ ПРОВЕРЯЕМ, ЕСТЬ ЛИ ОРУЖИЕ В РУКАХ
                if (sessionData.ActiveWeap == null || sessionData.ActiveWeap.Item == null || sessionData.ActiveWeap.Item.ItemId == ItemId.Debug)
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "У вас нет оружия в руках!", 3000);
                    return;
                }

                var weaponItem = sessionData.ActiveWeap.Item;
                var itemInfo = ItemsInfo[weaponItem.ItemId];

                // ✅ УБИРАЕМ ОРУЖИЕ ИЗ РУК
                Trigger.ClientEvent(player, "client.weapon.take", true);
                WeaponComponents.Remove(player);

                // ✅ ОЧИЩАЕМ АКТИВНОЕ ОРУЖИЕ (НО НЕ УДАЛЯЕМ ИЗ ИНВЕНТАРЯ!)
                sessionData.ActiveWeap = new ItemStruct("", -1, null);
                sessionData.LastActiveWeap = 0;

                // ✅ ОТПРАВЛЯЕМ КЛИЕНТУ ОЧИСТКУ АКТИВНОГО ОРУЖИЯ
                Trigger.ClientEvent(player, "client.inventory.clearActiveWeapon");

                // ✅ РП чат
                Commands.RPChat("sme", player, $"убрал" + (characterData.Gender ? "" : "а") + $" {itemInfo.Name}");

                Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, "Вы убрали оружие", 3000);
            }
            catch (Exception e)
            {
                Log.Write($"TakeWeaponBack Exception: {e.ToString()}");
            }
        }
        [RemoteEvent("server.weapon.dropFromHands")]
        public static void DropWeaponFromHands(ExtPlayer player)
        {
            try
            {
                var sessionData = player.GetSessionData();
                if (sessionData == null) return;

                // ✅ ПРОВЕРЯЕМ, ЕСТЬ ЛИ ОРУЖИЕ В РУКАХ
                if (sessionData.ActiveWeap == null ||
                    sessionData.ActiveWeap.Item == null ||
                    sessionData.ActiveWeap.Item.ItemId == ItemId.Debug)
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "У вас нет оружия в руках!", 3000);
                    return;
                }

                var weaponItem = sessionData.ActiveWeap.Item;
                int originalIndex = sessionData.ActiveWeap.Index;
                string originalLocation = sessionData.ActiveWeap.Location;

                // ✅ ВЫБРАСЫВАЕМ ОРУЖИЕ
                Vector3 position = player.Position;

                InventoryItemData dropItem = new InventoryItemData(
                    weaponItem.SqlId,
                    weaponItem.ItemId,
                    weaponItem.Count,
                    weaponItem.Data,
                    originalIndex,
                    weaponItem.IsTurn
                );

                Inventory.Drop.Repository.PutToObject(player, dropItem, position.X, position.Y, position.Z, 0, 0, 0);

                // ✅ УБИРАЕМ ОРУЖИЕ ИЗ РУК
                Trigger.ClientEvent(player, "client.weapon.take", true);
                WeaponComponents.Remove(player);

                // ✅ УДАЛЯЕМ ИЗ ИНВЕНТАРЯ (ТОЛЬКО СЕЙЧАС!)
                var characterData = player.GetCharacterData();
                if (characterData == null) return;

                string locationName = originalLocation == "backpack"
                    ? $"backpack_{GetItemData(player, "accessories", 8).SqlId}"
                    : $"char_{characterData.UUID}";

                string location = originalLocation == "backpack" ? "backpack" : originalLocation;

                DeletePlaceholdersForSlot(player, locationName, location, originalIndex, forceDelete: true);

                SetItemData(player, originalLocation, originalIndex, new InventoryItemData(), true);

                sessionData.ActiveWeap = new ItemStruct("", -1, null);
                sessionData.LastActiveWeap = 0;

                // ✅ ОЧИЩАЕМ UI
                Trigger.ClientEvent(player, "client.inventory.clearActiveWeapon");

                Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, "Оружие выброшено", 3000);
            }
            catch (Exception e)
            {
                Log.Write($"DropWeaponFromHands Exception: {e.ToString()}");
            }
        
}

        public static async void LoadNoteThread(ExtPlayer player, InventoryItemData Item)
        {
            try
            {
                if (!player.IsCharacterData()) return;

                await using var db = new ServerBD("MainDB");//В отдельном потоке

                var note = await db.Notes
                    .Where(v => v.ItemId == Item.SqlId)
                    .FirstOrDefaultAsync();

                Dictionary<string, object> _NoteData = new Dictionary<string, object>();
                _NoteData.Add("Type", Item.ItemId == ItemId.Note ? 0 : 1);
                if (note == null)
                {
                    _NoteData.Add("ItemId", Item.SqlId);
                }
                else
                {
                    _NoteData.Add("Name", note.Name);
                    _NoteData.Add("Text", note.Text);
                }
                Trigger.ClientEvent(player, "client.note.open", JsonConvert.SerializeObject(_NoteData));
            }
            catch (Exception e)
            {
                Log.Write($"LoadNoteThread Exception: {e.ToString()}");
            }
        }
        public static void UseSmoke(ExtPlayer player, ItemId ItemId = ItemId.Vape)
        {
            try
            {
                var characterData = player.GetCharacterData();
                if (characterData == null) return;
                var sessionData = player.GetSessionData();
                if (sessionData == null) return;

                var name = "";
                uint AttachmentsName = 0;
                var sound = "";
                float scale = 0.5f;

                WeaponRepository.RemoveHands(player);

                switch (ItemId)
                {
                    case ItemId.Vape:
                        if (Attachments.HasAttachment(player, Attachments.AttachmentsName.Vape)) return;
                        AttachmentsName = Attachments.AttachmentsName.Vape;
                        name = "вейп";
                        sound = "vape/one";
                        BattlePass.Repository.UpdateReward(player, 68);
                        BattlePass.Repository.UpdateReward(player, 71);
                        break;
                    case ItemId.Hookah:
                        name = "кальян";
                        sound = "kalik/kalik";
                        scale = 0.85f;
                        BattlePass.Repository.UpdateReward(player, 67);
                        break;
                }

                Main.OnAntiAnim(player);
                // Commands.RPChat("sme", player, $"использовал" + (characterData.Gender ? "" : "а") + $" {name}");

                if (AttachmentsName != 0)
                    Attachments.AddAttachment(player, AttachmentsName);

                if (!player.IsInVehicle) Trigger.PlayAnimation(player, "mp_player_inteat@burger", "mp_player_int_eat_burger", 49, false);
                // Trigger.ClientEventInRange(player.Position, 250f, "PlayAnimToKey", player, false, "vape");
                Timers.StartOnce(100, () =>
                {
                    try
                    {
                        if (!player.IsCharacterData()) return;
                        Sounds.PlayPlayer3d(player, sound, new SoundData
                        {
                            volume = 0.15
                        });
                    }
                    catch (Exception e)
                    {
                        Log.Write($"UseSmoke Task#3 Exception: {e.ToString()}");
                    }
                }, true);
                Timers.StartOnce(1400, () =>
                {
                    try
                    {
                        if (!player.IsCharacterData()) return;
                        if (!player.IsInVehicle) Trigger.PlayAnimation(player, "anim@heists@humane_labs@finale@keycards", "ped_a_enter_loop", 49, false);
                        // Trigger.ClientEventInRange(player.Position, 250f, "PlayAnimToKey", player, false, "smokeloop");
                        ParticleFx.PlayFXonEntityBone(player.Position, 25.0f, player, 20279, "core", "exp_grd_bzgas_smoke", 3400, new ParticleFxData(scale: scale));
                    }
                    catch (Exception e)
                    {
                        Log.Write($"UseSmoke Task#3 Exception: {e.ToString()}");
                    }
                }, true);
                Timers.StartOnce(4400, () =>
                {
                    try
                    {
                        if (!player.IsCharacterData()) return;

                        if (AttachmentsName != 0)
                            Attachments.RemoveAttachment(player, Attachments.AttachmentsName.Vape);

                        if (!player.IsInVehicle) Trigger.StopAnimation(player);
                        else Trigger.StopAnimation(player, false);

                        Main.OffAntiAnim(player);
                    }
                    catch (Exception e)
                    {
                        Log.Write($"UseSmoke Task#3 Exception: {e.ToString()}");
                    }
                }, true);
            }
            catch (Exception e)
            {
                Log.Write($"UseSmoke Exception: {e.ToString()}");
            }
        }
        public static void UseBong(ExtPlayer player, float scale = 0.5f)
        {
            try
            {
                var characterData = player.GetCharacterData();
                if (characterData == null) return;

                var sessionData = player.GetSessionData();
                if (sessionData == null) return;
                else if (Attachments.HasAttachment(player, Attachments.AttachmentsName.Bong)) return;

                int playerDrugsAmount = getCountToLacationItem($"char_{characterData.UUID}", "inventory", ItemId.Drugs);
                if (playerDrugsAmount < 1)
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.NeedNarcoToUse), 3000);
                    return;
                }

                if (DateTime.Now < sessionData.TimingsData.NextDrugs)
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.BongTooMany), 3000);
                    return;
                }

                ItemsClose(player, true);

                WeaponRepository.RemoveHands(player);
                Main.OnAntiAnim(player);

                switch (Main.rnd.Next(10))
                {
                    case 0:
                        Commands.RPChat("sme", player, $"задымил" + (characterData.Gender ? "" : "а") + " бонг");
                        break;
                    case 1:
                        Commands.RPChat("sme", player, $"вмазал" + (characterData.Gender ? "" : "а") + " бонг");
                        break;
                    case 2:
                        Commands.RPChat("sme", player, $"заварил" + (characterData.Gender ? "" : "а") + " бонг");
                        break;
                    case 3:
                        Commands.RPChat("sme", player, $"подкурил" + (characterData.Gender ? "" : "а") + " бонг");
                        break;
                    case 4:
                        Commands.RPChat("sme", player, $"сделал" + (characterData.Gender ? "" : "а") + " затяжику из бонга");
                        break;
                    case 5:
                        Commands.RPChat("sme", player, $"дунул" + (characterData.Gender ? "" : "а") + " бонг");
                        break;
                    case 6:
                        Commands.RPChat("sme", player, $"затянул" + (characterData.Gender ? "" : "а") + " бонг");
                        break;
                    case 7:
                        Commands.RPChat("sme", player, $"подолбил" + (characterData.Gender ? "" : "а") + " бонг");
                        break;
                    case 8:
                        Commands.RPChat("sme", player, $"хапнул" + (characterData.Gender ? "" : "а") + " шмальца из бонга");
                        break;
                    default:
                        Commands.RPChat("sme", player, $"закурил" + (characterData.Gender ? "" : "а") + " бонг");
                        break;
                }
                Attachments.AddAttachment(player, Attachments.AttachmentsName.Bong);
                if (!player.IsInVehicle) Trigger.PlayAnimation(player, "mp_player_inteat@burger", "mp_player_int_eat_burger", 49, false);
                // Trigger.ClientEventInRange(player.Position, 250f, "PlayAnimToKey", player, false, "vape");

                player.Health = (player.Health + 50 > 100) ? 100 : player.Health + 50;
                Remove(player, $"char_{characterData.UUID}", "inventory", ItemId.Drugs, 1);
                sessionData.TimingsData.NextDrugs = DateTime.Now.AddMinutes(2.5);
                Trigger.ClientEvent(player, "startScreenEffect", "DrugsTrevorClownsFight", 300000, false);

                Timers.StartOnce(100, () =>
                {
                    try
                    {
                        if (!player.IsCharacterData()) return;
                        Sounds.PlayPlayer3d(player, "bong/bong", new SoundData
                        {
                            volume = 0.15
                        }); // Звук для бонга
                    }
                    catch (Exception e)
                    {
                        Log.Write($"UseBong Task#1 Exception: {e.ToString()}");
                    }
                }, true);

                Timers.StartOnce(1400, () =>
                {
                    try
                    {
                        if (!player.IsCharacterData()) return;
                        if (!player.IsInVehicle) Trigger.PlayAnimation(player, "amb@code_human_in_car_mp_actions@smoke@std@rps@base", "idle_c", 49, false);
                        // Trigger.ClientEventInRange(player.Position, 250f, "PlayAnimToKey", player, false, "bong");
                        ParticleFx.PlayFXonEntityBone(player.Position, 25.0f, player, 20279, "core", "exp_grd_bzgas_smoke", 3400, new ParticleFxData(scale: scale));
                    }
                    catch (Exception e)
                    {
                        Log.Write($"UseBong Task#2 Exception: {e.ToString()}");
                    }
                }, true);

                Timers.StartOnce(4400, () =>
                {
                    try
                    {
                        if (!player.IsCharacterData()) return;
                        Attachments.RemoveAttachment(player, Attachments.AttachmentsName.Bong);
                        if (!player.IsInVehicle) Trigger.StopAnimation(player);
                        else Trigger.StopAnimation(player, false);
                        Main.OffAntiAnim(player);
                    }
                    catch (Exception e)
                    {
                        Log.Write($"UseBong Task#3 Exception: {e.ToString()}");
                    }
                }, true);
            }
            catch (Exception e)
            {
                Log.Write($"UseSmoke Exception: {e.ToString()}");
            }
        }
        public static int ItemsHands(ExtPlayer player, string ArrayName, int Index, InventoryItemData Item)
        {
            try
            {
                var characterData = player.GetCharacterData();
                if (characterData == null) return 0;

                //if (player.GetSharedData<string>("ANIM_USE") == null || player.GetSharedData<string>("ANIM_USE") == "null")
                //{
                ItemId ItemId = Item.ItemId;
                ItemsInfo ItemInfo = ItemsInfo[ItemId];
                switch (ItemId)
                {
                    case ItemId.Vape:
                        int value = 0;
                        try
                        {
                            value = Convert.ToInt32(Item.Data);
                        }
                        catch
                        {
                            value = 0;
                        }
                        if (value <= 0)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.VapeBroken), 3000);
                            Chars.Repository.RemoveIndex(player, ArrayName, Index);
                            return -1;
                        }
                        Item.Data = $"{value - 1}";
                        SetItemData(player, ArrayName, Index, Item, true);
                        UseSmoke(player);
                        return 0;
                    case ItemId.Rose:
                        if (Attachments.HasAttachment(player, Attachments.AttachmentsName.Rose))
                        {
                            Attachments.RemoveAttachment(player, Attachments.AttachmentsName.Rose);
                            if (!player.IsInVehicle) Trigger.StopAnimation(player);
                            Main.OffAntiAnim(player);
                        }
                        else
                        {
                            //Commands.RPChat("sme", player, $"достал" + (characterData.Gender ? "" : "а") + $" {ItemInfo.Name}");
                            Attachments.AddAttachment(player, Attachments.AttachmentsName.Rose);
                            if (!player.IsInVehicle) player.PlayAnimation("anim@heists@humane_labs@finale@keycards", "ped_b_enter_loop", 49);
                            // Trigger.ClientEventInRange(player.Position, 250f, "PlayAnimToKey", player, false, "rose");
                        }
                        break;
                    case ItemId.Barbell:
                        if (player.IsInVehicle) return 0;
                        if (Attachments.HasAttachment(player, Attachments.AttachmentsName.Barbell))
                        {
                            Attachments.RemoveAttachment(player, Attachments.AttachmentsName.Barbell);
                            if (!player.IsInVehicle) Trigger.StopAnimation(player);
                            Main.OffAntiAnim(player);
                        }
                        else
                        {
                            //Commands.RPChat("sme", player, $"достал" + (characterData.Gender ? "" : "а") + $" {ItemInfo.Name}");
                            Trigger.PlayAnimation(player, "amb@world_human_muscle_free_weights@male@barbell@idle_a", "idle_a", 49, attachmentName: "barbell");
                            // Trigger.ClientEventInRange(player.Position, 250f, "PlayAnimToKey", player, false, "barbell");
                        }
                        break;
                    /*case ItemId.Bear:
                        if (Attachments.HasAttachment(player, Attachments.AttachmentsName.Teddy))
                        {
                            Attachments.AddAttachment(player, Attachments.AttachmentsName.Teddy, true);
                        }
                        else
                        {
                            Commands.RPChat("sme", player, $"достал" + (characterData.Gender ? "" : "а") + $" {ItemInfo.Name}");
                            Attachments.AddAttachment(player, Attachments.AttachmentsName.Teddy);
                        }
                        return;*/
                    case ItemId.Binoculars:
                        if (Attachments.HasAttachment(player, Attachments.AttachmentsName.Binoculars))
                        {
                            Trigger.ClientEvent(player, "binoculars.stop");
                            Attachments.RemoveAttachment(player, Attachments.AttachmentsName.Binoculars);
                            if (!player.IsInVehicle) Trigger.StopAnimation(player);
                            //Trigger.TaskPlayAnim(player, "oddjobs@hunter", "binoculars_outro", 50, true);
                        }
                        else
                        {
                            //Commands.RPChat("sme", player, $"достал" + (characterData.Gender ? "" : "а") + $" {ItemInfo.Name}");
                            Attachments.AddAttachment(player, Attachments.AttachmentsName.Binoculars);
                            if (!player.IsInVehicle) Trigger.PlayAnimation(player, "oddjobs@hunter", "binoculars_loop", 49);
                            // Trigger.ClientEventInRange(player.Position, 250f, "PlayAnimToKey", player, false, "binoculars");
                            Trigger.ClientEvent(player, "binoculars.start");
                        }
                        break;
                    case ItemId.Umbrella:
                        if (Attachments.HasAttachment(player, Attachments.AttachmentsName.Umbrella))
                        {
                            Attachments.RemoveAttachment(player, Attachments.AttachmentsName.Umbrella);
                            if (!player.IsInVehicle) Trigger.StopAnimation(player);
                            Main.OffAntiAnim(player);
                        }
                        else
                        {
                            //Commands.RPChat("sme", player, $"достал" + (characterData.Gender ? "" : "а") + $" {ItemInfo.Name}");
                            Attachments.AddAttachment(player, Attachments.AttachmentsName.Umbrella);
                            if (!player.IsInVehicle) player.PlayAnimation("anim@heists@humane_labs@finale@keycards", "ped_b_enter_loop", 49);
                            // Trigger.ClientEventInRange(player.Position, 250f, "PlayAnimToKey", player, false, "umbrella");
                        }
                        break;
                    case ItemId.Microphone:
                        if (Attachments.HasAttachment(player, Attachments.AttachmentsName.News_mic))
                        {
                            Attachments.RemoveAttachment(player, Attachments.AttachmentsName.News_mic);
                            if (!player.IsInVehicle) Trigger.StopAnimation(player);
                            Main.OffAntiAnim(player);
                        }
                        else
                        {
                            // Commands.RPChat("sme", player, $"достал" + (characterData.Gender ? "" : "а") + $" {ItemInfo.Name}");
                            Attachments.AddAttachment(player, Attachments.AttachmentsName.News_mic);
                            if (!player.IsInVehicle) player.PlayAnimation("anim@heists@humane_labs@finale@keycards", "ped_b_enter_loop", 49);
                            // Trigger.ClientEventInRange(player.Position, 250f, "PlayAnimToKey", player, false, "mic");
                        }
                        break;
                    case ItemId.Guitar:
                        if (Attachments.HasAttachment(player, Attachments.AttachmentsName.Guitar))
                        {
                            Attachments.RemoveAttachment(player, Attachments.AttachmentsName.Guitar);
                            if (!player.IsInVehicle) Trigger.StopAnimation(player);
                            Main.OffAntiAnim(player);
                        }
                        else
                        {
                            //Commands.RPChat("sme", player, $"достал" + (characterData.Gender ? "" : "а") + $" {ItemInfo.Name}");
                            Attachments.AddAttachment(player, Attachments.AttachmentsName.Guitar);
                            if (!player.IsInVehicle) player.PlayAnimation("amb@lo_res_idles@", "world_human_musician_guitar_lo_res_base", 49);
                            // Trigger.ClientEventInRange(player.Position, 250f, "PlayAnimToKey", player, false, "guitar");
                        }
                        break;
                    case ItemId.Camera:
                        if (Attachments.HasAttachment(player, Attachments.AttachmentsName.News_camera))
                        {
                            Attachments.RemoveAttachment(player, Attachments.AttachmentsName.News_camera);
                            if (!player.IsInVehicle) Trigger.StopAnimation(player);
                            Main.OffAntiAnim(player);
                        }
                        else
                        {
                            // Commands.RPChat("sme", player, $"достал" + (characterData.Gender ? "" : "а") + $" {ItemInfo.Name}");
                            Attachments.AddAttachment(player, Attachments.AttachmentsName.News_camera);
                            if (!player.IsInVehicle) player.PlayAnimation("misscarsteal4@meltdown", "_rehearsal_camera_man", 49);
                            // Trigger.ClientEventInRange(player.Position, 250f, "PlayAnimToKey", player, false, "camera");
                        }
                        break;
                    case ItemId.VehicleNumber:
                        if (Attachments.HasAttachment(player, Attachments.AttachmentsName.VehicleNumber))
                        {
                            Attachments.RemoveAttachment(player, Attachments.AttachmentsName.VehicleNumber);
                        }
                        else
                        {
                            // Commands.RPChat("sme", player, $"достал" + (characterData.Gender ? "" : "а") + $" {ItemInfo.Name}");
                            Attachments.AddAttachment(player, Attachments.AttachmentsName.VehicleNumber);
                        }
                        break;
                    case ItemId.NeonStick:
                        if (Attachments.HasAttachment(player, Attachments.AttachmentsName.Neonstick))
                        {
                            Attachments.RemoveAttachment(player, Attachments.AttachmentsName.Neonstick);
                            Attachments.RemoveAttachment(player, Attachments.AttachmentsName.Neonstickr);
                            if (!player.IsInVehicle) Trigger.StopAnimation(player);
                            Main.OffAntiAnim(player);
                        }
                        else
                        {
                            // Commands.RPChat("sme", player, $"достал" + (characterData.Gender ? "" : "а") + $" {ItemInfo.Name}");
                            Attachments.AddAttachment(player, Attachments.AttachmentsName.Neonstick);
                            Attachments.AddAttachment(player, Attachments.AttachmentsName.Neonstickr);
                            if (!player.IsInVehicle) player.PlayAnimation("none", "none", 49);
                            // Trigger.ClientEventInRange(player.Position, 250f, "PlayAnimToKey", player, false, "rose");
                        }
                        break;
                    case ItemId.GlowStick:
                        if (Attachments.HasAttachment(player, Attachments.AttachmentsName.Glowstick))
                        {
                            Attachments.RemoveAttachment(player, Attachments.AttachmentsName.Glowstick);
                            Attachments.RemoveAttachment(player, Attachments.AttachmentsName.Glowstickr);
                            if (!player.IsInVehicle) Trigger.StopAnimation(player);
                            Main.OffAntiAnim(player);
                        }
                        else
                        {
                            // Commands.RPChat("sme", player, $"достал" + (characterData.Gender ? "" : "а") + $" {ItemInfo.Name}");
                            Attachments.AddAttachment(player, Attachments.AttachmentsName.Glowstick);
                            Attachments.AddAttachment(player, Attachments.AttachmentsName.Glowstickr);
                            if (!player.IsInVehicle) player.PlayAnimation("none", "none", 49);
                            // Trigger.ClientEventInRange(player.Position, 250f, "PlayAnimToKey", player, false, "rose");
                        }
                        break;
                    default:
                        return -1;
                        //}
                }
                return 1;
            }
            catch (Exception e)
            {
                Log.Write($"ItemsHands Task#3 Exception: {e.ToString()}");
            }
            return -1;
        }
        public static void NoteCreate(ExtPlayer player, int type, int ItemId, string nameValue, string textValue)
        {
            try
            {
                Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.SucSaveLetter), 3000);
                Trigger.SetTask(async () =>
                {
                    var characterData = player.GetCharacterData();
                    if (characterData == null) return;

                    await using var db = new ServerBD("MainDB");//В отдельном потоке

                    await db.InsertAsync(new Notes
                    {
                        ItemId = ItemId,
                        Name = nameValue,
                        Text = textValue,
                        Type = Convert.ToBoolean(type)
                    });

                    GameLog.AddInfo($"(Note) player({characterData.UUID}): {textValue}");
                });
            }
            catch (Exception e)
            {
                Log.Write($"NoteCreate Task#3 Exception: {e.ToString()}");
            }
        }
        public static void UseEat(ExtPlayer player, byte numb)
        {
            try
            {
                var sessionData = player.GetSessionData();
                if (sessionData == null) return;
                WeaponRepository.RemoveHands(player);
                Main.OnAntiAnim(player);
                if (!player.IsInVehicle)
                {
                    sessionData.LastCoverState = player.IsInCover;
                    if (!player.IsInCover)
                    {
                        Animations.AnimationStop(player);
                        Trigger.ClientEvent(player, "blockMove", true);
                        switch (numb)
                        {
                            case 1:
                            case 4:
                            case 5:
                            case 6:
                            case 8:
                            case 10:
                                Trigger.PlayAnimation(player, "amb@code_human_wander_eating_donut@male@idle_a", "idle_b", 48);
                                // Trigger.ClientEventInRange(player.Position, 250f, "PlayAnimToKey", player, false, "eat");
                                if (numb == 1) Attachments.AddAttachment(player, Attachments.AttachmentsName.Burger);
                                else if (numb == 4) Attachments.AddAttachment(player, Attachments.AttachmentsName.HotDog);
                                else if (numb == 5) Attachments.AddAttachment(player, Attachments.AttachmentsName.Pizza);
                                else if (numb == 6) Attachments.AddAttachment(player, Attachments.AttachmentsName.Sandwich);
                                else if (numb == 8) Attachments.AddAttachment(player, Attachments.AttachmentsName.Crisps);
                                break;
                            case 2:
                                Trigger.PlayAnimation(player, "amb@code_human_wander_smoking@male@idle_a", "idle_a", 49);
                                // Trigger.ClientEventInRange(player.Position, 250f, "PlayAnimToKey", player, false, "joint");
                                Attachments.AddAttachment(player, Attachments.AttachmentsName.Joint);
                                break;
                            case 9:
                                Trigger.PlayAnimation(player, "amb@code_human_wander_smoking@male@idle_a", "idle_a", 49);
                                // Trigger.ClientEventInRange(player.Position, 250f, "PlayAnimToKey", player, false, "joint");
                                Attachments.AddAttachment(player, Attachments.AttachmentsName.Cocaine);
                                break;
                            case 3:
                            case 7:
                                Trigger.PlayAnimation(player, "amb@code_human_wander_drinking@male@idle_a", "idle_c", 49);
                                // Trigger.ClientEventInRange(player.Position, 250f, "PlayAnimToKey", player, false, "cola");
                                if (numb == 3) Attachments.AddAttachment(player, Attachments.AttachmentsName.eCola);
                                else if (numb == 7) Attachments.AddAttachment(player, Attachments.AttachmentsName.Sprunk);
                                break;
                            default:
                                // Not supposed to end up here. 
                                break;
                        }
                    }
                }
                NAPI.Task.Run(() =>
                {
                    try
                    {
                        var characterData = player.GetCharacterData();
                        if (characterData == null) return;
                        bool cover = player.IsInCover;
                        if (!cover)
                        {
                            switch (numb)
                            {
                                case 1:
                                    Attachments.RemoveAttachment(player, Attachments.AttachmentsName.Burger);
                                    break;
                                case 4:
                                    Attachments.RemoveAttachment(player, Attachments.AttachmentsName.HotDog);
                                    break;
                                case 5:
                                    Attachments.RemoveAttachment(player, Attachments.AttachmentsName.Pizza);
                                    break;
                                case 6:
                                    Attachments.RemoveAttachment(player, Attachments.AttachmentsName.Sandwich);
                                    break;
                                case 8:
                                    Attachments.RemoveAttachment(player, Attachments.AttachmentsName.Crisps);
                                    break;
                                case 10:
                                    break;
                                case 2:
                                    Attachments.RemoveAttachment(player, Attachments.AttachmentsName.Joint);
                                    break;
                                case 3:
                                    Attachments.RemoveAttachment(player, Attachments.AttachmentsName.eCola);
                                    break;
                                case 7:
                                    Attachments.RemoveAttachment(player, Attachments.AttachmentsName.Sprunk);
                                    break;
                                case 9:
                                    Attachments.RemoveAttachment(player, Attachments.AttachmentsName.Cocaine);
                                    break;
                                default:
                                    // Not supposed to end up here. 
                                    break;
                            }
                        }
                        if (player.Health <= 0) return;
                        Main.OffAntiAnim(player);
                        characterData.EatTimes++;
                        if (sessionData.LastCoverState == true)
                        {
                            sessionData.LastCoverState = false;
                            if (characterData.IsAlive && !player.IsInCover)
                            {
                                player.Health = (player.Health + 5 > 100) ? 100 : player.Health + 5;
                                
                                return;
                            }
                        }
                        if (characterData.IsAlive)
                        {
                            if (!player.IsInVehicle && !cover) Trigger.StopAnimation(player);
                            switch (numb)
                            {
                                case 1:
                                case 4:
                                case 5:
                                case 6:
                                case 8:
                                case 10:
                                    player.Health = (player.Health + 30 > 100) ? 100 : player.Health + 30;
                                    break;
                                case 2:
                                    player.Health = (player.Health + 50 > 100) ? 100 : player.Health + 50;
                                    break;
                                case 3:
                                case 7:
                                    player.Health = (player.Health + 10 > 100) ? 100 : player.Health + 10;
                                    break;
                                default:
                                    // Not supposed to end up here. 
                                    break;
                            }
                        }
                        if (!sessionData.CuffedData.Cuffed) Trigger.ClientEvent(player, "blockMove", false);
                    }
                    catch (Exception e)
                    {
                        Log.Write($"UseEat Task Exception: {e.ToString()}");
                    }
                }, 3000);
            }
            catch (Exception e)
            {
                Log.Write($"UseEat Exception: {e.ToString()}");
            }
        }
        #endregion
        #region Выбросить
        public static void ItemsDrops(ExtPlayer player, ItemId ItemId)
        {
            try
            {
                var characterData = player.GetCharacterData();
                if (characterData == null) return;

                string locationName = $"char_{characterData.UUID}";

                InventoryItemData item = new InventoryItemData();
                string ArrayName = "";
                int Index = 0;
                InventoryItemData Bags = null;
                List<ItemStruct> _Items = new List<ItemStruct>();
                if (ItemsData.ContainsKey(locationName))
                {
                    foreach (var Location in ItemsData[locationName])
                    {
                        if (!ItemsData[locationName].ContainsKey(Location.Key)) continue;
                        foreach (var itemData in Location.Value)
                        {
                            if (!ItemsData[locationName][Location.Key].ContainsKey(itemData.Key)) continue;
                            InventoryItemData sItem = itemData.Value;
                            if (sItem.ItemId == ItemId.Bag) Bags = sItem;
                            if (sItem.ItemId != ItemId) continue;
                            if (item.ItemId == ItemId.Debug)
                            {
                                item = sItem;
                                ArrayName = Location.Key;
                                Index = itemData.Key;
                            }
                            else
                            {
                                item.Count += sItem.Count;
                                _Items.Add(new ItemStruct(Location.Key, itemData.Key, itemData.Value));
                            }
                        }
                    }
                    if (item.ItemId != ItemId.Debug)
                    {
                        RemoveFix(player, locationName, _Items);
                        //SetItemData(player, ArrayName, Index, item);
                        ItemsDropToIndex(player, ArrayName, Index);
                    }
                    if (Bags != null && ItemsData.ContainsKey($"backpack_{Bags.SqlId}") && ItemsData[$"backpack_{Bags.SqlId}"].ContainsKey("backpack"))
                    {
                        _Items = new List<ItemStruct>();
                        item = new InventoryItemData();
                        foreach (var itemData in ItemsData[$"backpack_{Bags.SqlId}"]["backpack"])
                        {
                            if (itemData.Value.ItemId == ItemId.Debug) continue;
                            InventoryItemData sItem = itemData.Value;
                            if (sItem.ItemId != ItemId) continue;
                            if (item.ItemId == ItemId.Debug)
                            {
                                item = sItem;
                                Index = itemData.Key;
                            }
                            else
                            {
                                item.Count += sItem.Count;
                                _Items.Add(new ItemStruct("backpack", itemData.Key, itemData.Value));
                            }
                        }
                        if (item.ItemId != ItemId.Debug)
                        {
                            RemoveFix(player, $"backpack_{Bags.SqlId}", _Items);
                            //SetItemData(player, ArrayName, Index, item);
                            //ItemsDropToIndex(player, "backpack", Index);
                            //isBackpackItemsData(player, true);

                            ItemsDrop(player, new InventoryItemData(0, item.ItemId, item.Count, item.Data));
                            if (ItemsData.ContainsKey($"backpack_{Bags.SqlId}") && ItemsData[$"backpack_{Bags.SqlId}"].ContainsKey("backpack") && ItemsData[$"backpack_{Bags.SqlId}"]["backpack"].ContainsKey(item.Index))
                            {
                                ItemId ItemIdDell = ItemsData[$"backpack_{Bags.SqlId}"]["backpack"][item.Index].ItemId;
                                ItemsData[$"backpack_{Bags.SqlId}"]["backpack"][item.Index].ItemId = ItemId.Debug;
                                UpdateSqlItemData(locationName, "backpack", item.Index, ItemsData[$"backpack_{Bags.SqlId}"]["backpack"][item.Index], ItemIdDell);
                                ItemsData[$"backpack_{Bags.SqlId}"]["backpack"].TryRemove(item.Index, out _);

                                isBackpackItemsData(player, true);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Write($"ItemsDrops Exception: {e.ToString()}");
            }
        }

        public static void ItemsDropToIndex(ExtPlayer player, string ArrayName, int Index, bool me = false, bool check = false, float posZ = -1f)
        {
            try
            {
                var sessionData = player.GetSessionData();
                if (sessionData == null) return;

                var characterData = player.GetCharacterData();
                if (characterData == null) return;

                InventoryItemData Item = GetItemData(player, ArrayName, Index);
                if (Item.ItemId == ItemId.Debug) return;

                if (ArrayName == "accessories" && Index == 7)
                {
                    if (sessionData.ArmorHealth != -1 && player.Armor > sessionData.ArmorHealth)
                        WeaponRepository.PlayerKickAntiCheat(player, 3, true);
                    Item.Data = player.Armor.ToString();
                }

                // ✅ НОВОЕ: УДАЛЯЕМ ИЗ FASTSLOTS ПЕРЕД ВЫБРОСОМ
                if (ArrayName == "inventory")
                {
                    ClearItemFromFastSlots(player, Item.SqlId);
                }

                // ✅ ПРОВЕРЯЕМ: ЭТО ОРУЖИЕ В РУКАХ?
                if (sessionData.ActiveWeap != null &&
                    sessionData.ActiveWeap.Item != null &&
                    sessionData.ActiveWeap.Item.SqlId == Item.SqlId)
                {
                    Log.Write($"[ITEMDROP] Weapon in hands detected: SqlId={Item.SqlId}, removing from hands");

                    Trigger.ClientEvent(player, "client.weapon.take", true);
                    WeaponComponents.Remove(player);

                    sessionData.ActiveWeap = new ItemStruct("", -1, null);
                    sessionData.LastActiveWeap = 0;

                    Trigger.ClientEvent(player, "client.inventory.clearActiveWeapon");
                }

                // ✅ ВЫБРАСЫВАЕМ ПРЕДМЕТ
                if (!ItemsDrop(player, Item, me, posZ: posZ)) return;

                // ✅ УДАЛЯЕМ ЗАГЛУШКИ ПОСЛЕ УСПЕШНОГО ВЫБРОСА
                string locationName = ArrayName == "backpack"
                    ? $"backpack_{GetItemData(player, "accessories", 8).SqlId}"
                    : $"char_{characterData.UUID}";

                string location = ArrayName == "backpack" ? "backpack" : ArrayName;

                Log.Write($"[ITEMDROP DELETE] Deleting placeholders: locationName={locationName}, location={location}, Index={Index}");

                DeletePlaceholdersForSlot(player, locationName, location, Index, forceDelete: true);

                // ✅ КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: УДАЛЯЕМ ПРЕДМЕТ ИЗ СЛОТА
                SetItemData(player, ArrayName, Index, new InventoryItemData(), send: true, check: check);

                Log.Write($"[ITEMDROP SUCCESS] Item dropped and removed from slot {Index}");
            }
            catch (Exception e)
            {
                Log.Write($"ItemsDropToIndex Exception: {e.ToString()}");
            }
        }
        public static bool ItemsDrop(ExtPlayer player, InventoryItemData Item, bool me = false, float posZ = -1f)
        {
            try
            {
                var sessionData = player.GetSessionData();
                if (sessionData == null)
                    return false;

                var characterData = player.GetCharacterData();
                if (characterData == null)
                    return false;

                sessionData.TimingsData.NextDropItem = DateTime.Now.AddMilliseconds(150);
                var itemInfo = ItemsInfo[Item.ItemId];
                double xrnd = 0;
                double yrnd = 0;
                if (me)
                {
                    Random rnd = new Random();
                    xrnd = rnd.NextDouble();
                    yrnd = rnd.NextDouble();
                    BattlePass.Repository.UpdateReward(player, 16);
                }

                Vector3 pos = player.Position;
                if (posZ != -1f) pos.Z = posZ + 1f;

                var position = pos + itemInfo.PosOffset + new Vector3(xrnd, yrnd, 0);
                var rotation = player.Rotation + itemInfo.RotOffset;

                // ✅ ДОБАВЬ ЛОГИРОВАНИЕ
                Log.Write($"[ITEMDROP] Dropping item: ItemId={Item.ItemId}, Width={itemInfo.Width}, Height={itemInfo.Height}, IsTurn={Item.IsTurn}");

                Inventory.Drop.Repository.PutToObject(player, Item, position.X, position.Y, position.Z, rotation.X, rotation.Y, rotation.Z);
                return true;
            }
            catch (Exception e)
            {
                Log.Write($"ItemsDrop Exception: {e.ToString()}");
                return false;
            }
        }
        private static readonly IReadOnlyDictionary<ItemId, FireworkData> FireworkTypeData = new Dictionary<ItemId, FireworkData>()
        {
            { ItemId.Firework4, new FireworkData("scr_indep_firework_fountain", "PLACE_FIREWORK_4_CONE", 7500) },
            { ItemId.Firework3, new FireworkData("scr_indep_firework_shotburst", "PLACE_FIREWORK_3_BOX", 5000) },
            { ItemId.Firework2, new FireworkData("scr_indep_firework_starburst", "PLACE_FIREWORK_2_CYLINDER", 5500) },
            { ItemId.Firework1, new FireworkData("scr_indep_firework_trailburst", "PLACE_FIREWORK_1_ROCKET", 3200) }
        };
        private static ItemId[] ItemsToMy = new ItemId[]
        {
            ItemId.Hookah,
            ItemId.Fire,
            ItemId.Matras,
            ItemId.Tent,
            ItemId.Lezhak,
            ItemId.Towel,
            ItemId.Flag,
            ItemId.Barrell,
            ItemId.Surf,
            ItemId.Vedro,
            ItemId.Flagstok,
            ItemId.Tenttwo,
            ItemId.Polotence,
            ItemId.Beachbag,
            ItemId.Zontik,
            ItemId.Zontiktwo,
            ItemId.Zontikthree,
            ItemId.Closedzontik,
            ItemId.Vball,
            ItemId.Bball,
            ItemId.Boomboxxx,
            ItemId.Table,
            ItemId.Tabletwo,
            ItemId.Tablethree,
            ItemId.Tablefour,
            ItemId.Chair,
            ItemId.Chairtwo,
            ItemId.Chaierthree,
            ItemId.Chaierfour,
            ItemId.Chairtable,
            ItemId.Korzina,
            ItemId.Light,
            ItemId.Alco,
            ItemId.Alcotwo,
            ItemId.Alcothree,
            ItemId.Alcofour,
            ItemId.Cocktail,
            ItemId.Cocktailtwo,
            ItemId.Fruit,
            ItemId.Fruittwo,
            ItemId.Packet,
            ItemId.Buter,
            ItemId.Patatoes,
            ItemId.Coffee,
            ItemId.Podnosfood,
            ItemId.Bbqtwo,
            ItemId.Bbq,
            ItemId.Vaza,
            ItemId.Flagwtokk,
            ItemId.Flagau,
            ItemId.Flagbr,
            ItemId.Flagch,
            ItemId.Flagcz,
            ItemId.Flageng,
            ItemId.Flageu,
            ItemId.Flagfin,
            ItemId.Flagfr,
            ItemId.Flagger,
            ItemId.Flagire,
            ItemId.Flagisr,
            ItemId.Flagit,
            ItemId.Flagjam,
            ItemId.Flagjap,
            ItemId.Flagmex,
            ItemId.Flagnet,
            ItemId.Flagnig,
            ItemId.Flagnorw,
            ItemId.Flagpol,
            ItemId.Flagrus,
            ItemId.Flagbel,
            ItemId.Flagscot,
            ItemId.Flagscr,
            ItemId.Flagslov,
            ItemId.Flagslovak,
            ItemId.Flagsou,
            ItemId.Flagspain,
            ItemId.Flagswede,
            ItemId.Flagswitz,
            ItemId.Flagturk,
            ItemId.Flaguk,
            ItemId.Flagus,
            ItemId.Flagwales,
            ItemId.Flowerrr,
            ItemId.Konus,
            ItemId.Konuss,
            ItemId.Otboynik1,
            ItemId.Otboynik2,
            ItemId.Dontcross,
            ItemId.Stop,
            ItemId.NetProezda,
            ItemId.Kpp,
            ItemId.Zabor1,
            ItemId.Zabor2,
            ItemId.Airlight,
            ItemId.Camera1,
            ItemId.Camera2,
        };

        public static void ItemsDropToEditor(ExtPlayer player, string arrayName, int index, float posX, float posY, float posZ, float rotX, float rotY, float rotZ)
        {
            try
            {
                var characterData = player.GetCharacterData();
                if (characterData == null)
                    return;

                var sessionData = player.GetSessionData();
                if (sessionData == null)
                    return;

                InventoryItemData Item = GetItemData(player, arrayName, index);
                if (Item.ItemId == ItemId.Debug || Item.ItemId == ItemId.GasCan) return;
                sessionData.TimingsData.NextDropItem = DateTime.Now.AddMilliseconds(150);
                ItemsInfo ItemInfo = ItemsInfo[Item.ItemId];
                // Commands.RPChat("sme", player, $"поставил" + (characterData.Gender ? "" : "а") + $" {ItemInfo.Name}");
                //BattlePass.Repository.UpdateReward(player, 130);

                if (FireworkTypeData.ContainsKey(Item.ItemId))
                {
                    SetItemData(player, arrayName, index, new InventoryItemData(Item.SqlId), true);
                    Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.fireworkstand), 7000);
                    BattlePass.Repository.UpdateReward(player, 23);
                    uint dim = UpdateData.GetPlayerDimension(player);
                    var obj = NAPI.Object.CreateObject(ItemInfo.Model, new Vector3(posX, posY, posZ), new Vector3(0, 0, rotZ), 255, dim);
                    Timers.StartOnce(5000, () =>
                    {
                        if (obj != null && obj.Exists)
                            obj.Delete();

                        ParticleFx.PlayFXonPos(new Vector3(posX, posY, posZ), 500f, posX, posY, posZ, "scr_indep_fireworks", FireworkTypeData[Item.ItemId].ParticleName, FireworkTypeData[Item.ItemId].EffectTime);
                    }, true);
                }
                else if (ItemsToMy.Contains(Item.ItemId))
                {
                    SetItemData(player, arrayName, index, new InventoryItemData(), true);

                    Inventory.Drop.Repository.PutToObject(player, Item, posX, posY, posZ, rotX, rotY, rotZ, isMy: true);
                }
                else
                {
                    if (arrayName == "accessories" && index == 7)
                    {
                        if (sessionData.ArmorHealth != -1 && player.Armor > sessionData.ArmorHealth) WeaponRepository.PlayerKickAntiCheat(player, 3, true);
                        Item.Data = player.Armor.ToString();
                    }
                    SetItemData(player, arrayName, index, new InventoryItemData(), true);

                    Inventory.Drop.Repository.PutToObject(player, Item, posX, posY, posZ, rotX, rotY, rotZ);
                }
            }
            catch (Exception e)
            {
                Log.Write($"ItemsDropToEditor Exception: {e.ToString()}");
            }
        }
        #endregion


        #region Трейд

        public static void StartTrade(ExtPlayer player, ExtPlayer target)
        {
            try
            {
                if (!player.IsCharacterData() || !target.IsCharacterData()) return;
                var sessionData = player.GetSessionData();
                if (sessionData == null) return;
                var characterData = player.GetCharacterData();
                if (characterData == null) return;
                else if (sessionData.CuffedData.Cuffed || sessionData.DeathData.InDeath || characterData.LVL < 1)
                {
                    Notify.Send(target, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.CantTrade, player.Value), 3000);
                    return;
                }
                var targetSessionData = target.GetSessionData();
                if (targetSessionData == null) return;
                var targetCharacterData = target.GetCharacterData();
                if (targetCharacterData == null) return;
                else if (targetSessionData.CuffedData.Cuffed || targetSessionData.DeathData.InDeath || targetCharacterData.LVL < 1)
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.CantTrade, player.Value), 3000);
                    return;
                }
                sessionData.ItemsTrade = new ItemsTrade(target);
                targetSessionData.ItemsTrade = new ItemsTrade(player);
                Trigger.ClientEvent(player, "client.inventory.InitTradeData", target.Name);
                Trigger.ClientEvent(target, "client.inventory.InitTradeData", player.Name);
            }
            catch (Exception e)
            {
                Log.Write($"StartTrade Exception: {e.ToString()}");
            }
        }
        public static void OtherClose(ExtPlayer player)
        {
            try
            {
                var sessionData = player.GetSessionData();
                if (sessionData == null) return;

                if (sessionData.InventoryOtherLocationName != null)
                {
                    string locationName = sessionData.InventoryOtherLocationName;

                    if (InventoryOtherPlayers.ContainsKey(locationName) && InventoryOtherPlayers[locationName].Contains(player))
                        InventoryOtherPlayers[locationName].Remove(player);

                    if (InventoryOtherPlayers.ContainsKey(locationName) && InventoryOtherPlayers[locationName].Count == 0)
                        InventoryOtherPlayers.TryRemove(locationName, out _);

                    sessionData.InventoryOtherLocationName = null;
                }
            }
            catch (Exception e)
            {
                Log.Write($"OtherClose Exception: {e.ToString()}");
            }
        }
        public static void ItemsClose(ExtPlayer player, bool Event = false)
        {
            try
            {
                var sessionData = player.GetSessionData();
                if (sessionData == null) return;
                var characterData = player.GetCharacterData();
                if (characterData == null) return;

                if (Event) Trigger.ClientEvent(player, "client.inventory.Close");


                if (sessionData.SelectData.SelectedVeh != null)
                {
                    var vehicle = (ExtVehicle)sessionData.SelectData.SelectedVeh;
                    var vehicleLocalData = vehicle.GetVehicleLocalData();
                    if (vehicleLocalData != null) vehicleLocalData.BagInUse = false;
                }

                OtherClose(player);
                if (sessionData.ItemsTrade != null)
                {
                    ItemsTrade TradeData = sessionData.ItemsTrade;

                    ExtPlayer target = TradeData.Target;
                    var targetSessionData = target.GetSessionData();
                    if (targetSessionData == null) return;
                    var targetCharacterData = target.GetCharacterData();
                    if (targetCharacterData == null) return;
                    sessionData.ItemsTrade = null;

                    string locationName = GetLocationName(player, "trade");

                    if (ItemsData.ContainsKey(locationName) && ItemsData[locationName].ContainsKey("trade"))
                    {
                        foreach (var item in ItemsData[locationName]["trade"])
                        {
                            if (item.Value.ItemId != ItemId.Debug) AddItem(player, $"char_{characterData.UUID}", "inventory", item.Value, isWarehouse: true);
                        }
                        ItemsData[locationName].TryRemove("trade", out _);
                    }

                    locationName = GetLocationName(target, "trade");

                    if (ItemsData.ContainsKey(locationName) && ItemsData[locationName].ContainsKey("trade"))
                    {
                        foreach (var item in ItemsData[locationName]["trade"])
                        {
                            if (item.Value.ItemId != ItemId.Debug) AddItem(target, $"char_{targetCharacterData.UUID}", "inventory", item.Value, isWarehouse: true);
                        }
                        ItemsData[locationName].TryRemove("trade", out _);
                    }

                    if (targetSessionData != null)
                    {
                        Trigger.ClientEvent(target, "client.inventory.Close");
                        targetSessionData.ItemsTrade = null;
                    }
                }
                if (sessionData.LookingStats)
                {
                    sessionData.LookingStats = false;
                    InitInventory(player);
                }
            }
            catch (Exception e)
            {
                Log.Write($"ItemsClose Exception: {e.ToString()}");
            }
        }
        public static void ItemsAllClose(string locationName)
        {
            try
            {
                if (InventoryOtherPlayers.ContainsKey(locationName))
                {
                    foreach (ExtPlayer foreachPlayer in InventoryOtherPlayers[locationName].ToList())
                    {
                        if (!foreachPlayer.IsCharacterData()) continue;
                        ItemsClose(foreachPlayer, true);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Write($"ItemsAllClose Exception: {e.ToString()}");
            }
        }
        public static void ItemsTrade(ExtPlayer player, int status)
        {
            try
            {
                if (!player.IsCharacterData()) return;
                var sessionData = player.GetSessionData();
                if (sessionData.ItemsTrade == null)
                {
                    ItemsClose(player, true);
                    return;
                }
                ItemsTrade TradeData = sessionData.ItemsTrade;
                ExtPlayer target = TradeData.Target;
                if (!target.IsCharacterData())
                {
                    ItemsClose(player, true);
                    return;
                }
                var targetSessionData = target.GetSessionData();
                if (targetSessionData.ItemsTrade == null)
                {
                    ItemsClose(player, true);
                    return;
                }
                if (status == 1 && targetSessionData.ItemsTrade.Status == 1)
                {
                    string locationName = GetLocationName(player, "trade");
                    string tLocationName = GetLocationName(target, "trade");

                    int myCount = ItemsData.ContainsKey(locationName) && ItemsData[locationName].ContainsKey("trade") ? ItemsData[locationName]["trade"].Count : 0;
                    int tCount = ItemsData.ContainsKey(tLocationName) && ItemsData[tLocationName].ContainsKey("trade") ? ItemsData[tLocationName]["trade"].Count : 0;
                    if (myCount < 1 && TradeData.Money < 1 &&
                        tCount < 1 && targetSessionData.ItemsTrade.Money < 1)
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.NobodyVibral, target.Name), 3000);
                        return;
                    }

                    int myInvCount = ItemsData.ContainsKey(locationName) && ItemsData[locationName].ContainsKey("inventory") ? ItemsData[locationName]["inventory"].Count : 0;
                    int tInvCount = ItemsData.ContainsKey(tLocationName) && ItemsData[tLocationName].ContainsKey("inventory") ? ItemsData[tLocationName]["inventory"].Count : 0;
                    if (myCount > (InventoryMaxSlots["inventory"] - tInvCount))
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.NetMestaTrade, target.Name), 3000);
                        return;
                    }
                    else if (tCount > (InventoryMaxSlots["inventory"] - myInvCount))
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.YouNetMestaTrade, target.Name), 3000);
                        return;
                    }
                }
                if (status == 2)
                {
                    string locationName = GetLocationName(target, "trade");
                    if (ItemsData.ContainsKey(locationName) && ItemsData[locationName].ContainsKey("trade") && ItemsData[locationName]["trade"].Count > 0)
                    {
                        foreach (var item in ItemsData[locationName]["trade"])
                        {
                            if (item.Value.ItemId != ItemId.Debug && isFreeSlots(player, item.Value.ItemId, item.Value.Count) != 0) return;
                        }
                    }
                }

                TradeData.Status = status;
                if (status == 1) Trigger.ClientEvent(player, "client.inventory.TradeUpdate", -1);
                else if (status == 2) Trigger.ClientEvent(player, "client.inventory.TradeUpdate", -2);
                if (status == 0 && targetSessionData.ItemsTrade.Status == 2) targetSessionData.ItemsTrade.Status = 1;
                Trigger.ClientEvent(target, "client.inventory.TradeUpdate", status);
                if (status == 2 && targetSessionData.ItemsTrade.Status == 2) ConfirmTrade(player);
            }
            catch (Exception e)
            {
                Log.Write($"ItemsTrade Exception: {e.ToString()}");
            }
        }
        public static void ItemsTradeMoney(ExtPlayer player, int value)
        {
            try
            {
                var sessionData = player.GetSessionData();
                if (sessionData == null) return;
                var characterData = player.GetCharacterData();
                if (characterData == null) return;
                if (sessionData.ItemsTrade == null)
                {
                    ItemsClose(player, true);
                    return;
                }
                ItemsTrade TradeData = sessionData.ItemsTrade;
                ExtPlayer target = TradeData.Target;
                if (!target.IsCharacterData())
                {
                    ItemsClose(player, true);
                    return;
                }
                var targetSessionData = target.GetSessionData();
                if (targetSessionData == null || targetSessionData.ItemsTrade == null)
                {
                    ItemsClose(player, true);
                    return;
                }
                else if (TradeData.Status != 0)
                {
                    Trigger.ClientEvent(player, "client.inventory.tradeMoney", "YourMoney", TradeData.Money);
                    return;
                }
                else if (characterData.Money < value)
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.NoMoney), 3000);
                    TradeData.Money = (int)characterData.Money;
                    Trigger.ClientEvent(target, "client.inventory.tradeMoney", "WithMoney", TradeData.Money);
                    Trigger.ClientEvent(player, "client.inventory.tradeMoney", "YourMoney", TradeData.Money);
                    return;
                }
                TradeData.Money = value;
                Trigger.ClientEvent(target, "client.inventory.tradeMoney", "WithMoney", TradeData.Money);
            }
            catch (Exception e)
            {
                Log.Write($"ItemsTradeMoney Exception: {e.ToString()}");
            }
        }
        public static void ConfirmTrade(ExtPlayer player)
        {
            try
            {
                var sessionData = player.GetSessionData();
                if (sessionData == null) return;
                var characterData = player.GetCharacterData();
                if (characterData == null) return;
                if (sessionData.ItemsTrade == null)
                {
                    ItemsClose(player, true);
                    return;
                }

                ItemsTrade TradeData = sessionData.ItemsTrade;
                ExtPlayer target = TradeData.Target;

                sessionData.ItemsTrade = null;
                if (!target.IsCharacterData())
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Обмен отменён", 3000);
                    ItemsClose(player, true);
                    return;
                }
                var targetSessionData = target.GetSessionData();
                var targetCharacterData = target.GetCharacterData();
                if (targetSessionData == null || targetCharacterData == null || targetSessionData.ItemsTrade == null)
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.TradeCancelled), 3000);
                    ItemsClose(player, true);
                    return;
                }
                else if (UpdateData.CanIChange(player, TradeData.Money) != 255)
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.NoMoney), 3000);
                    Notify.Send(target, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.DoesntHaveMoney, player.Name), 3000);
                    ItemsClose(player, true);
                    return;
                }
                else if (UpdateData.CanIChange(target, targetSessionData.ItemsTrade.Money) != 255)
                {
                    Notify.Send(target, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.NoMoney), 3000);
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.DoesntHaveMoney, target.Name), 3000);
                    ItemsClose(target, true);
                    return;
                }
                /*else if (Main.ServerNumber != 0 && (characterData.AdminLVL >= 1 && characterData.AdminLVL <= 6))
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.AdminTransferRestricted), 3000);
                    return;
                }
                else if (Main.ServerNumber != 0 && (targetCharacterData.AdminLVL >= 1 && targetCharacterData.AdminLVL <= 6))
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.AdminTransferRestricted), 3000);
                    return;
                }*/

                string locationName = GetLocationName(player, "trade");

                if (ItemsData.ContainsKey(locationName) && ItemsData[locationName].ContainsKey("trade") && ItemsData[locationName]["trade"].Count > 0)
                {
                    foreach (var item in ItemsData[locationName]["trade"])
                    {
                        if (item.Value.ItemId != ItemId.Debug)
                        {
                            if (item.Value.ItemId == ItemId.LoveNote)
                                BattlePass.Repository.UpdateReward(player, 36);
                            else if (item.Value.ItemId == ItemId.Rose)
                                BattlePass.Repository.UpdateReward(player, 75);
                            else if (item.Value.ItemId == ItemId.Pizza)
                                BattlePass.Repository.UpdateReward(player, 52);
                            else if (item.Value.ItemId == ItemId.HotDog)
                                BattlePass.Repository.UpdateReward(player, 122);
                            else if (item.Value.ItemId == ItemId.Beer)
                                BattlePass.Repository.UpdateReward(player, 37);
                            else if (item.Value.ItemId == ItemId.eCola)
                                BattlePass.Repository.UpdateReward(player, 141);
                            else if (item.Value.ItemId == ItemId.Sprunk)
                                BattlePass.Repository.UpdateReward(player, 142);
                            else if (item.Value.ItemId == ItemId.Burger)
                                BattlePass.Repository.UpdateReward(player, 143);
                            else if (item.Value.ItemId == ItemId.Sandwich)
                                BattlePass.Repository.UpdateReward(player, 144);
                            else if (item.Value.ItemId == ItemId.HealthKit)
                                BattlePass.Repository.UpdateReward(player, 145);
                            else if (item.Value.ItemId == ItemId.Note)
                                BattlePass.Repository.UpdateReward(player, 146);
                            else if (item.Value.ItemId == ItemId.Revolver)
                                BattlePass.Repository.UpdateReward(player, 41);
                            else if (item.Value.ItemId == ItemId.Drugs)
                                BattlePass.Repository.UpdateReward(player, 54);
                            else if (item.Value.ItemId == ItemId.Case0)
                                BattlePass.Repository.UpdateReward(player, 123);
                            else if (item.Value.ItemId == ItemId.Wrench)
                                BattlePass.Repository.UpdateReward(player, 133);

                            AddItem(target, $"char_{targetCharacterData.UUID}", "inventory", item.Value, isWarehouse: true);
                        }
                    }
                    ItemsData[locationName].TryRemove("trade", out _);
                }
                if (TradeData.Money > 0)
                {
                    Wallet.Change(player, -TradeData.Money);
                    Wallet.Change(target, TradeData.Money);
                    GameLog.Money($"player({characterData.UUID})", $"player({targetCharacterData.UUID})", TradeData.Money, $"trade");
                    Commands.RPChat("sme", player, "передал" + (characterData.Gender ? "" : "а") + $" {Wallet.Format(TradeData.Money)}$ " + "{name}", target);

                    // if (TradeData.Money >= 1000000)
                    //     Admin.AdminsLog(1, $"[ВНИМАНИЕ] Игрок {target.Name}({target.Value}) получил {TradeData.Money}$ единой
                    //     ей от {player.Name}({player.Value}) (ConfirmTrade-1 - Обмен)", 1, "#FF0000");
                    //
                    // if (TradeData.Money >= 10000 && targetSessionData.LastCashOperationSum == TradeData.Money)
                    // {
                    //     Admin.AdminsLog(1, $"[ВНИМАНИЕ] Игрок {target.Name}({target.Value}) два раза подряд получил по {TradeData.Money}$ от {player.Name}({player.Value}) (ConfirmTrade-1 - Обмен)", 1, "#FF0000");
                    //     targetSessionData.LastCashOperationSum = 0;
                    // }
                    // else
                    // {
                    //     targetSessionData.LastCashOperationSum = TradeData.Money;
                    // }
                }
                Notify.Send(target, NotifyType.Success, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.DealSuccess), 3000);

                locationName = GetLocationName(target, "trade");
                TradeData = targetSessionData.ItemsTrade;
                targetSessionData.ItemsTrade = null;
                if (ItemsData.ContainsKey(locationName) && ItemsData[locationName].ContainsKey("trade") && ItemsData[locationName]["trade"].Count > 0)
                {
                    foreach (var item in ItemsData[locationName]["trade"])
                    {
                        if (item.Value.ItemId != ItemId.Debug)
                        {
                            if (item.Value.ItemId == ItemId.LoveNote)
                                BattlePass.Repository.UpdateReward(target, 36);
                            else if (item.Value.ItemId == ItemId.Rose)
                                BattlePass.Repository.UpdateReward(target, 75);
                            else if (item.Value.ItemId == ItemId.Pizza)
                                BattlePass.Repository.UpdateReward(target, 52);
                            else if (item.Value.ItemId == ItemId.HotDog)
                                BattlePass.Repository.UpdateReward(target, 122);
                            else if (item.Value.ItemId == ItemId.Beer)
                                BattlePass.Repository.UpdateReward(target, 37);
                            else if (item.Value.ItemId == ItemId.eCola)
                                BattlePass.Repository.UpdateReward(target, 141);
                            else if (item.Value.ItemId == ItemId.Sprunk)
                                BattlePass.Repository.UpdateReward(target, 142);
                            else if (item.Value.ItemId == ItemId.Burger)
                                BattlePass.Repository.UpdateReward(target, 143);
                            else if (item.Value.ItemId == ItemId.Sandwich)
                                BattlePass.Repository.UpdateReward(target, 144);
                            else if (item.Value.ItemId == ItemId.HealthKit)
                                BattlePass.Repository.UpdateReward(target, 145);
                            else if (item.Value.ItemId == ItemId.Note)
                                BattlePass.Repository.UpdateReward(target, 146);
                            else if (item.Value.ItemId == ItemId.Revolver)
                                BattlePass.Repository.UpdateReward(target, 41);
                            else if (item.Value.ItemId == ItemId.Drugs)
                                BattlePass.Repository.UpdateReward(target, 54);
                            else if (item.Value.ItemId == ItemId.Case0)
                                BattlePass.Repository.UpdateReward(target, 123);
                            else if (item.Value.ItemId == ItemId.Wrench)
                                BattlePass.Repository.UpdateReward(target, 133);

                            AddItem(player, $"char_{characterData.UUID}", "inventory", item.Value, isWarehouse: true);

                        }
                    }
                    ItemsData[locationName].TryRemove("trade", out _);
                }
                if (TradeData.Money > 0)
                {
                    Wallet.Change(target, -TradeData.Money);
                    Wallet.Change(player, TradeData.Money);
                    GameLog.Money($"player({targetCharacterData.UUID})", $"player({characterData.UUID})", TradeData.Money, $"trade");
                    Commands.RPChat("sme", target, "передал" + (targetCharacterData.Gender ? "" : "а") + $" {Wallet.Format(TradeData.Money)}$ " + "{name}", player);

                    // if (TradeData.Money >= 1000000)
                    //     Admin.AdminsLog(1, $"[ВНИМАНИЕ] Игрок {player.Name}({player.Value}) получил {TradeData.Money}$ единой операцией от {target.Name}({target.Value}) (ConfirmTrade-1 - Обмен)", 1, "#FF0000");
                    //
                    // if (TradeData.Money >= 10000 && targetSessionData.LastCashOperationSum == TradeData.Money)
                    // {
                    //     Admin.AdminsLog(1, $"[ВНИМАНИЕ] Игрок {target.Name}({target.Value}) два раза подряд получил по {TradeData.Money}$ от {player.Name}({player.Value}) (ConfirmTrade-1 - Обмен)", 1, "#FF0000");
                    //     targetSessionData.LastCashOperationSum = 0;
                    // }
                    // else
                    // {
                    //     targetSessionData.LastCashOperationSum = TradeData.Money;
                    // }
                }
                Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.DealSuccess), 3000);
                BattlePass.Repository.UpdateReward(player, 72);
                BattlePass.Repository.UpdateReward(target, 72);
                ItemsClose(player, true);
                ItemsClose(target, true);
            }
            catch (Exception e)
            {
                Log.Write($"ConfirmTrade Exception: {e.ToString()}");
            }
        }
        #endregion

        #region Информация о игроке
        public static void PlayerStats(ExtPlayer player, ExtPlayer target = null)
        {
            try
            {
                if (!player.IsCharacterData()) return;
                else if (target != null && !target.IsCharacterData()) return;

                ExtPlayer getPlayer = target == null ? player : target;

                var targetSessionData = getPlayer.GetSessionData();
                if (targetSessionData == null) return;
                var targetAccountData = getPlayer.GetAccountData();
                if (targetAccountData == null) return;
                var targetCharacterData = getPlayer.GetCharacterData();
                if (targetCharacterData == null) return;

                var charData = new List<object>();

                charData.Add(targetAccountData.Login);//0
                charData.Add(targetAccountData.VipLvl);//1
                charData.Add(targetAccountData.VipDate);//2
                charData.Add(targetCharacterData.Warns);//3
                charData.Add(targetCharacterData.Unwarn);//4
                targetCharacterData.Time = Main.GetCurrencyTime(target == null ? player : null, targetCharacterData.Time);
                charData.Add(targetCharacterData.Time.TodayTime);//5
                charData.Add(targetCharacterData.Time.MonthTime);//6
                charData.Add(targetCharacterData.Time.YearTime);//7
                charData.Add(targetCharacterData.Time.TotalTime);//8
                                                                 //
                var jobSkills = targetCharacterData.JobSkills;
                if (!jobSkills.ContainsKey(8))
                    jobSkills.Add(8, 0);
                else
                    jobSkills[8] = 0;

                charData.Add(jobSkills);//9

                charData.Add(Main.GetPlayerJobsNextLevel(getPlayer));//10
                int[] currentLevelsInfo = { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                for (int i = 0; i <= 7; i++)
                {
                    if (targetCharacterData.JobSkills.ContainsKey(i))
                        currentLevelsInfo[i] = Main.GetPlayerJobLevelBonus((sbyte)i, targetCharacterData.JobSkills[i]).Item1;
                }

                currentLevelsInfo[8] = 0;
                charData.Add(currentLevelsInfo);//11
                                                //
                charData.Add($"{targetCharacterData.FirstName} {targetCharacterData.LastName}");//12
                charData.Add(targetCharacterData.AdminLVL);//13
                charData.Add(targetCharacterData.WeddingName.Length > 5 ? targetCharacterData.WeddingName : "Нет");//14
                charData.Add(targetCharacterData.Gender);//15
                charData.Add(targetCharacterData.LVL);//16
                charData.Add(targetCharacterData.EXP);//17
                charData.Add(targetCharacterData.Sim);//18
                charData.Add(targetCharacterData.WorkID);//19


                var targetMemberFractionData = Fractions.Manager.GetFractionMemberData(targetCharacterData.UUID);
                if (targetMemberFractionData != null)
                {
                    charData.Add(targetMemberFractionData.Id);//20
                    charData.Add(Fractions.Manager.GetFractionRankName(targetMemberFractionData.Id, targetMemberFractionData.Rank));//21
                }
                else
                {
                    charData.Add(null);//20
                    charData.Add(null);//21
                }

                var targetMemberOrganizationData = Organizations.Manager.GetOrganizationMemberData(targetCharacterData.UUID);
                if (targetMemberOrganizationData != null)
                {
                    charData.Add(targetMemberOrganizationData.Id);//22
                    charData.Add(Organizations.Manager.GetOrganizationRankName(targetMemberOrganizationData.Id, targetMemberOrganizationData.Rank));//23
                }
                else
                {
                    charData.Add(null);//22
                    charData.Add(null);//23
                }


                charData.Add(targetCharacterData.UUID);//24
                charData.Add(targetCharacterData.Bank);//25
                charData.Add(Bank.GetBalance(targetCharacterData.Bank));//26
                charData.Add(targetCharacterData.Money);//27
                charData.Add(targetCharacterData.CreateDate);//28

                var house = HouseManager.GetHouse($"{targetCharacterData.FirstName}_{targetCharacterData.LastName}", false);
                var garage = house?.GetGarageData();
                if (house != null)
                {
                    charData.Add(house.ID);//29
                    int houseBank = (int)Bank.GetBalance(house.BankID);
                    charData.Add(HouseManager.HouseTypeList[house.Type].Name);//30
                    charData.Add(house.Price == 0 ? "$0 / $0" : $"${Wallet.Format(houseBank)} / ${Wallet.Format(GetTax(house.Price, targetAccountData.VipLvl))}");//31
                    var tax = Convert.ToInt32(house.Price / 100 * 0.026);
                    charData.Add(house.Price == 0 ? "$0" : $"${Wallet.Format(tax)}");//32
                    int paid = (houseBank == 0 || tax == 0) ? 0 : Convert.ToInt32(houseBank / tax);
                    charData.Add(paid);//33
                    charData.Add(garage != null ? GarageManager.GarageTypes[garage.Type].MaxCars : 0);//34
                    charData.Add(house.Price);//35
                    charData.Add(house.Position);//36 
                }
                else
                {
                    charData.Add(null);//29
                    charData.Add(null);//30
                    charData.Add(null);//31
                    charData.Add(null);//32
                    charData.Add(null);//33
                    charData.Add(null);//34
                }

                if (targetCharacterData.BizIDs.Count > 0)
                {
                    Business biz = BusinessManager.BizList[targetCharacterData.BizIDs[0]];
                    charData.Add(biz.ID);//37
                    int BizBank = (int)Bank.GetBalance(biz.BankID);
                    charData.Add(biz.SellPrice == 0 ? "$0 / $0" : $"${Wallet.Format(BizBank)} / ${Wallet.Format(Repository.GetTax(biz.SellPrice, targetAccountData.VipLvl, biz.Tax))}");//38
                    charData.Add(biz.SellPrice == 0 ? "$0" : $"${Wallet.Format(Convert.ToInt32(biz.SellPrice / 100 * biz.Tax))}");//39
                    int paid = (BizBank == 0 || biz.SellPrice == 0) ? 0 : BizBank / Convert.ToInt32(biz.SellPrice / 100 * biz.Tax);
                    charData.Add(paid);//40
                    charData.Add(biz.SellPrice);//41
                    charData.Add(biz.EnterPoint);//42
                    charData.Add(biz.Type);//43
                }
                else
                {
                    charData.Add(null);//37
                    charData.Add(null);//38
                    charData.Add(null);//39
                    charData.Add(null);//40
                }

                charData.Add(targetCharacterData.Licenses);//44
                if (targetCharacterData.WantedLVL != null)
                    charData.Add(targetCharacterData.WantedLVL.Level);//45
                else
                    charData.Add(0);//45          
                charData.Add(targetCharacterData.EarnedMoneyDay);//46
                charData.Add(targetCharacterData.EarnedMoneyMonth);//47
                charData.Add(targetCharacterData.EarnedMoney);//48
                charData.Add(targetCharacterData.SpentMoneyDay);//49
                charData.Add(targetCharacterData.SpentMoneyMonth);//50
                charData.Add(targetCharacterData.SpentMoney);//51

                string locationName = $"char_{targetCharacterData.UUID}";

                // Подсчёт веса инвентаря
                float inventoryWeight = GetCurrentWeight(locationName, "inventory");
                charData.Add(inventoryWeight);//52
                charData.Add(MaxInventoryWeight);//53

                // Подсчёт веса рюкзака
                InventoryItemData bagItem = GetItemData(getPlayer, "accessories", 8);
                if (bagItem.ItemId == ItemId.Bag)
                {
                    float backpackWeight = GetCurrentWeight($"backpack_{bagItem.SqlId}", "backpack");
                    charData.Add(backpackWeight);//54
                    charData.Add(MaxBackpackWeight);//55
                }
                else
                {
                    charData.Add(0f);//54
                    charData.Add(0f);//55
                }

                var statsData = JsonConvert.SerializeObject(charData);

                var sessionData = player.GetSessionData();
                if (sessionData == null) return;

                if (sessionData.oldPlayerStats != statsData || target != null)
                {
                    if (target == null)
                    {
                        sessionData.oldPlayerStats = statsData;
                        Trigger.ClientEvent(player, "client.inventory.stats", statsData);


                        // ✅ ============================================
                        // ✅ КОНЕЦ ВСТАВКИ
                        // ✅ ========================   ====================
                    }
                    else
                    {
                        Trigger.ClientEvent(player, "client.accountStore.otherStatsData", statsData);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Write($"PlayerStats Exception: {e.ToString()}");
            }
        }

        [RemoteEvent("server.logs.getCriminalRecords")]
        public static void GetCriminalRecords(ExtPlayer player)
        {
            var chdata = player.GetCharacterData();
            if (chdata == null) return;

            var accountId = chdata.UUID;
            var list = new List<object>();
            var table = MySQL.QueryRead(
                "SELECT `time`, `player` AS `officer`, `reason`, `stars` " +
                "FROM `arrestlog` " +
                $"WHERE `player` = {accountId} " +
                "ORDER BY `time` DESC"
            );

            if (table != null)
            {
                foreach (DataRow row in table.Rows)
                {
                    var time = Convert.ToDateTime(row["time"]);
                    var officer = Convert.ToInt32(row["officer"]);
                    var reason = row["reason"]?.ToString() ?? "";
                    var stars = Convert.ToInt32(row["stars"]);

                    list.Add(new
                    {
                        date = time.ToString("s"),
                        officer = officer.ToString(),
                        reason = reason,
                        duration = $"{stars} зв.",
                        end = time.AddMinutes(stars).ToString("s")
                    });
                }
            }

            Trigger.ClientEvent(player, "client.logs.setCriminalRecords", JsonConvert.SerializeObject(list));
        }   
        public static int GetTax(int Price, int VipLevel, double Tax = 0.026)
        {
            try
            {
                if (Price == 0) return 0;
                List<int> VipDays = new List<int>()
                {
                    7,7,14,21,28,28
                };

                int correctSum = Convert.ToInt32(Price / 100 * Tax * 24 * VipDays[VipLevel]);
                return correctSum >= 5 ? correctSum : 5;
            }
            catch (Exception e)
            {
                Log.Write($"GetTax Exception: {e.ToString()}");
                return 0;
            }
        }

        #endregion

        #region Информация о игроке
        public static void Event_PlayerDeath(ExtPlayer player)
        {
            try
            {
                if (!FunctionsAccess.IsWorking("dropitem")) return;

                var characterData = player.GetCharacterData();
                if (characterData == null) return;

                if (characterData.DemorganTime >= 1) return;

                Log.Write($"[DEATH] Player died: {player.Name}");

                // ✅ 1. ВЫБРАСЫВАЕМ ВСЁ ИЗ FASTSLOTS
                DropAllFromFastSlots(player);

                // ✅ 2. ПРОВЕРЯЕМ ОРУЖИЕ В РУКАХ
                CheckLastActive(player);
                CheckAntiSave(player);
            }
            catch (Exception e)
            {
                Log.Write($"Event_PlayerDeath Exception: {e.ToString()}");
            }
        }
        /// <summary>
        /// Выбрасывает ВСЕ предметы из fastSlots при смерти (и удаляет оригиналы из inventory)
        /// </summary>
        public static void DropAllFromFastSlots(ExtPlayer player)
        {
            try
            {
                var characterData = player.GetCharacterData();
                if (characterData == null) return;

                var sessionData = player.GetSessionData();
                if (sessionData == null) return;

                string locationName = $"char_{characterData.UUID}";

                Log.Write($"[DEATH DROP] Starting drop from fastSlots for player {player.Name}");

                // ✅ ПРОВЕРЯЕМ, ЕСТЬ ЛИ ПРЕДМЕТЫ В FASTSLOTS
                if (!ItemsData.ContainsKey(locationName) ||
                    !ItemsData[locationName].ContainsKey("fastSlots"))
                {
                    Log.Write($"[DEATH DROP] No fastSlots data found");
                    return;
                }

                // ✅ СОБИРАЕМ ВСЕ ItemId ИЗ FASTSLOTS (ЧТОБЫ УДАЛИТЬ ИЗ INVENTORY)
                List<ItemId> itemsToRemove = new List<ItemId>();

                // ✅ ПРОХОДИМ ПО ВСЕМ 3 СЛОТАМ И ВЫБРАСЫВАЕМ
                for (int slotId = 0; slotId < 3; slotId++)
                {
                    try
                    {
                        InventoryItemData item = GetItemData(player, "fastSlots", slotId);

                        if (item.ItemId == ItemId.Debug || item.ItemId == 0)
                        {
                            Log.Write($"[DEATH DROP] Slot {slotId} is empty");
                            continue;
                        }

                        Log.Write($"[DEATH DROP] Dropping from fastSlots slot {slotId}: ItemId={item.ItemId}, SqlId={item.SqlId}");

                        // ✅ ЗАПОМИНАЕМ ItemId ДЛЯ УДАЛЕНИЯ ИЗ INVENTORY
                        if (!itemsToRemove.Contains(item.ItemId))
                        {
                            itemsToRemove.Add(item.ItemId);
                        }

                        // ✅ ВЫБРАСЫВАЕМ ИЗ FASTSLOTS
                        ItemsDropToIndex(player, "fastSlots", slotId, me: true);

                        Log.Write($"[DEATH DROP] Successfully dropped item from fastSlots slot {slotId}");
                    }
                    catch (Exception ex)
                    {
                        Log.Write($"[DEATH DROP] Exception in fastSlots slot {slotId}: {ex.ToString()}");
                    }
                }

                // ✅ ТЕПЕРЬ УДАЛЯЕМ ЭТИ ЖЕ ПРЕДМЕТЫ ИЗ INVENTORY
                foreach (var itemId in itemsToRemove)
                {
                    try
                    {
                        Log.Write($"[DEATH DROP] Removing {itemId} from inventory");

                        // ✅ ИЩЕМ ВСЕ ЭКЗЕМПЛЯРЫ ЭТОГО ПРЕДМЕТА В INVENTORY
                        if (ItemsData.ContainsKey(locationName) && ItemsData[locationName].ContainsKey("inventory"))
                        {
                            var inventoryItems = ItemsData[locationName]["inventory"].Values
                                .Where(i => i.ItemId == itemId)
                                .ToList();

                            foreach (var invItem in inventoryItems)
                            {
                                Log.Write($"[DEATH DROP] Found {itemId} in inventory: SqlId={invItem.SqlId}, Index={invItem.Index}");

                                // ✅ УДАЛЯЕМ ИЗ INVENTORY (НЕ ВЫБРАСЫВАЕМ, А ПРОСТО УДАЛЯЕМ!)
                                int index = invItem.Index;

                                // ✅ УДАЛЯЕМ ЗАГЛУШКИ
                                DeletePlaceholdersForSlot(player, locationName, "inventory", index, forceDelete: true);

                                // ✅ УДАЛЯЕМ ПРЕДМЕТ
                                SetItemData(player, "inventory", index, new InventoryItemData(), send: true, isSqlUpdate: true);

                                Log.Write($"[DEATH DROP] Removed {itemId} from inventory slot {index}");
                            }
                        }

                        // ✅ ТАКЖЕ ПРОВЕРЯЕМ РЮКЗАК
                        InventoryItemData bagItem = GetItemData(player, "accessories", 8);
                        if (bagItem.ItemId == ItemId.Bag)
                        {
                            string backpackLocation = $"backpack_{bagItem.SqlId}";

                            if (ItemsData.ContainsKey(backpackLocation) && ItemsData[backpackLocation].ContainsKey("backpack"))
                            {
                                var backpackItems = ItemsData[backpackLocation]["backpack"].Values
                                    .Where(i => i.ItemId == itemId)
                                    .ToList();

                                foreach (var bpItem in backpackItems)
                                {
                                    Log.Write($"[DEATH DROP] Found {itemId} in backpack: SqlId={bpItem.SqlId}, Index={bpItem.Index}");

                                    int index = bpItem.Index;

                                    // ✅ УДАЛЯЕМ ЗАГЛУШКИ
                                    DeletePlaceholdersForSlot(player, backpackLocation, "backpack", index, forceDelete: true);

                                    // ✅ УДАЛЯЕМ ПРЕДМЕТ
                                    SetItemData(player, "backpack", index, new InventoryItemData(), send: true, isSqlUpdate: true);

                                    Log.Write($"[DEATH DROP] Removed {itemId} from backpack slot {index}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Write($"[DEATH DROP] Exception removing {itemId} from inventory: {ex.ToString()}");
                    }
                }

                // ✅ ОЧИЩАЕМ ActiveWeap
                if (sessionData.ActiveWeap != null && sessionData.ActiveWeap.Location == "fastSlots")
                {
                    Log.Write($"[DEATH DROP] Clearing ActiveWeap");

                    Trigger.ClientEvent(player, "client.weapon.take", true);
                    WeaponComponents.Remove(player);

                    sessionData.ActiveWeap = new ItemStruct("", -1, null);
                    sessionData.LastActiveWeap = 0;

                    Trigger.ClientEvent(player, "client.inventory.clearActiveWeapon");
                }

                Log.Write($"[DEATH DROP] Finished dropping items from fastSlots");
            }
            catch (Exception e)
            {
                Log.Write($"DropAllFromFastSlots Exception: {e.ToString()}");
            }
        }
        public static bool CheckAntiSave(ExtPlayer player)
        {
            try
            {
                var sessionData = player.GetSessionData();
                if (sessionData == null)
                    return false;

                var lastWeapon = sessionData.LastActive;
                if (lastWeapon.WeaponSqlId == 0)
                {
                    WeaponRepository.OnClearTimerWeaponUpdate(player);
                    return true;
                }
                ItemStruct ItemStruct = isItem(player, "inventory", lastWeapon.WeaponSqlId);
                if (ItemStruct == null || ItemStruct.Item.ItemId == ItemId.Debug)
                {
                    WeaponRepository.OnClearTimerWeaponUpdate(player);
                    return true;
                }
                ItemsDropToIndex(player, ItemStruct.Location, ItemStruct.Index, check: true);
                Trigger.ClientEvent(player, "removeAllWeapons");
                player.RemoveAllWeapons();
                sessionData.LastActiveWeap = 0;
                WeaponRepository.OnClearTimerWeaponUpdate(player);
            }
            catch (Exception e)
            {
                Log.Write($"CheckAntiSave Exception: {e.ToString()}");
            }
            return false;
        }
     
        public static bool CheckLastActive(ExtPlayer player)
        {
            try
            {
                var sessionData = player.GetSessionData();
                if (sessionData == null) return false;
                if (sessionData.LastActiveWeap == 0) return true;
                ItemStruct ItemStruct = isItem(player, "inventory", sessionData.LastActiveWeap);
                if (ItemStruct == null || ItemStruct.Item.ItemId == ItemId.Debug)
                {
                    sessionData.LastActiveWeap = 0;
                    return true;
                }
                ItemsDropToIndex(player, ItemStruct.Location, ItemStruct.Index);
                Trigger.ClientEvent(player, "removeAllWeapons");
                player.RemoveAllWeapons();
                sessionData.LastActiveWeap = 0;
            }
            catch (Exception e)
            {
                Log.Write($"CheckLastActive Exception: {e.ToString()}");
            }
            return false;
        }


        #endregion
        public static void ItemBuy(ExtPlayer player, string ArrayName, int Index, int Value)
        {
            var sessionData = player.GetSessionData();
            if (sessionData == null) return;

            var characterData = player.GetCharacterData();
            if (characterData == null) return;

            InventoryItemData Item = GetItemData(player, ArrayName, Index);

            if (Item.ItemId == ItemId.Debug) return;
            else if (Item.Price == 0) return;
            else if (Item.Count < Value) return;
            else if (isFreeSlots(player, Item.ItemId, Value) != 0) return;

            if (Value == 0) Value = 1;

            int price = Item.Price * Value;

            string locationName = null;

            if (ArrayName == "other" && sessionData.InventoryOtherLocationName != null) locationName = sessionData.InventoryOtherLocationName;

            if (locationName != null)
            {
                int index = Convert.ToInt32(locationName.Split('_')[1]);

                index = Inventory.Tent.Repository.GetUUIDToIndex(index);

                if (!Inventory.Tent.Repository.TentsData.ContainsKey(index)) return;

                var tentData = Inventory.Tent.Repository.TentsData[index];

                var target = tentData.player;
                var targetCharacterData = target.GetCharacterData();
                if (targetCharacterData == null) return;

                if (UpdateData.CanIChange(player, price, true) != 255) return;

                //GameLog.Money($"player({characterData.UUID})", $"tent({index})", price, "itemTent");

                MoneySystem.Wallet.Change(player, -price);

                MoneySystem.Wallet.Change(target, +price);

                GameLog.Money($"player({characterData.UUID})", $"player({targetCharacterData.UUID})", price, $"itemTent");

                //Notify.Send(target, NotifyType.Success, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.YouItemBuyed, ItemsInfo[Item.ItemId].Name, price), 10000);

                Players.Phone.Messages.Repository.AddSystemMessage(target, (int)DefaultNumber.Tent, LangFunc.GetText(LangType.Ru, DataName.YouItemBuyed, ItemsInfo[Item.ItemId].Name, price), DateTime.Now);

                BattlePass.Repository.UpdateReward(player, 48);

                ItemStack(player, ArrayName, Index, 2, Value, isBuy: true);
            }
        }
        public static void ItemStack(ExtPlayer player, string ArrayName, int Index, int Id, int Value, bool isBuy = false)
        {
            try
            {
                Log.Write($"[ITEMSTACK START] Player: {player.Name}, ArrayName: {ArrayName}, Index: {Index}, Id: {Id}, Value: {Value}");

                var characterData = player.GetCharacterData();
                if (characterData == null) return;

                var sessionData = player.GetSessionData();
                if (sessionData == null) return;

                var Item = GetItemData(player, ArrayName, Index);

                if (Item.ItemId == ItemId.Debug)
                {
                    Log.Write($"[ITEMSTACK ERROR] Item is Debug");
                    return;
                }

                Log.Write($"[ITEMSTACK] Item: ItemId={Item.ItemId}, Count={Item.Count}, SqlId={Item.SqlId}");

                string locationName = null;
                if (ArrayName == "other" && sessionData.InventoryOtherLocationName != null)
                    locationName = sessionData.InventoryOtherLocationName;
                else if (ArrayName != "other" && ArrayName != "backpack")
                    locationName = $"char_{characterData.UUID}";
                else if (ArrayName == "backpack")
                    locationName = $"backpack_{GetItemData(player, "accessories", 8).SqlId}";

                if (locationName == null)
                {
                    Log.Write($"[ITEMSTACK ERROR] locationName is null");
                    return;
                }

                string Location = "inventory";
                if (ArrayName == "other")
                    Location = locationName.Split('_')[0];
                else if (ArrayName == "backpack")
                    Location = "backpack";
                else if (ArrayName == "trade")
                    Location = "trade";

                var itemInfo = ItemsInfo[Item.ItemId];

                if (Item.ItemId == ItemId.Coal || Item.ItemId == ItemId.Iron || Item.ItemId == ItemId.Gold ||
                    Item.ItemId == ItemId.Sulfur || Item.ItemId == ItemId.Emerald || Item.ItemId == ItemId.Ruby)
                {
                    if (locationName == "vehicle" || Location == "vehicle")
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.CantMoveItem), 3000);
                        return;
                    }
                }

                if (Value < 1 || ((Id == 0 && Item.Count <= Value) || (Id != 0 && Item.Count < Value)))
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.SomethingWrong), 3000);
                    return;
                }

                if (Id == 0) // ✅ РАЗДЕЛЕНИЕ СТАКА
                {
                    if (AddNewItem(player, locationName, Location, Item.ItemId, Value, Item.Data, false, MaxSlots: GetMaxSlots(player, Location)) == -1)
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.NoSpaceInventory), 3000);
                        return;
                    }

                    // ✅ УМЕНЬШАЕМ ИСХОДНЫЙ СТАК
                    Item.Count -= Value;
                    if (Item.Count <= 0)
                    {
                        SetItemData(player, ArrayName, Index, new InventoryItemData(), true);
                    }
                    else
                    {
                        SetItemData(player, ArrayName, Index, Item, true);
                    }

                    Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.SuccSplit, itemInfo.Name), 3000);
                }
                else if (Id == 1) // ✅ ВЫБРОС
                {
                    if (player.IsInVehicle)
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.NoDropFromCar), 3000);
                        return;
                    }

                    if (!ItemsDrop(player, new InventoryItemData(0, Item.ItemId, Value, Item.Data))) return;

                    string sourceLocationName = ArrayName == "other" ? sessionData.InventoryOtherLocationName :
                                                ArrayName == "backpack" ? $"backpack_{GetItemData(player, "accessories", 8).SqlId}" :
                                                ArrayName == "trade" ? $"char_{characterData.UUID}" :
                                                $"char_{characterData.UUID}";

                    string sourceLocation = ArrayName == "other" ? (sessionData.InventoryOtherLocationName?.Split('_')[0] ?? "inventory") :
                                           ArrayName == "backpack" ? "backpack" :
                                           ArrayName == "trade" ? "trade" :
                                           "inventory";

                    if (Value == Item.Count)
                    {
                        DeletePlaceholdersForSlot(player, sourceLocationName, sourceLocation, Index, forceDelete: true);
                    }

                    RemoveIndex(player, ArrayName, Index, Value);

                    Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.SuccDrop, itemInfo.Name), 3000);
                }
                else // ✅ Id == 2 (ПЕРЕМЕЩЕНИЕ В ДРУГОЕ ХРАНИЛИЩЕ)
                {
                    // ✅ ОПРЕДЕЛЯЕМ ЦЕЛЕВУЮ ЛОКАЦИЮ
                    string targetLocationName = null;

                    if (ArrayName == "other" || ArrayName == "backpack" || ArrayName == "trade")
                        targetLocationName = $"char_{characterData.UUID}";
                    else if (sessionData.InventoryOtherLocationName != null)
                        targetLocationName = sessionData.InventoryOtherLocationName;
                    else if (sessionData.InventoryOtherLocationName == null && sessionData.ItemsTrade != null)
                        targetLocationName = $"char_{characterData.UUID}";
                    else if (sessionData.InventoryOtherLocationName == null && isBackpackItemsData(player) != 0)
                        targetLocationName = $"backpack_{isBackpackItemsData(player)}";

                    if (targetLocationName == null)
                    {
                        Log.Write($"[ITEMSTACK ERROR] targetLocationName is null");
                        return;
                    }

                    string targetLocation = null;

                    if (ArrayName == "other" || ArrayName == "backpack" || ArrayName == "trade")
                        targetLocation = "inventory";
                    else if (sessionData.InventoryOtherLocationName != null)
                        targetLocation = targetLocationName.Split('_')[0];
                    else if (sessionData.InventoryOtherLocationName == null && sessionData.ItemsTrade != null)
                        targetLocation = "trade";
                    else if (sessionData.InventoryOtherLocationName == null && isBackpackItemsData(player) != 0)
                        targetLocation = "backpack";

                    if (targetLocation == null)
                    {
                        Log.Write($"[ITEMSTACK ERROR] targetLocation is null");
                        return;
                    }

                    Log.Write($"[ITEMSTACK TRANSFER] From: {ArrayName}[{Index}] ({locationName}/{Location}) → To: {targetLocationName}/{targetLocation}, Value: {Value}, Total: {Item.Count}");
                    if (ArrayName == "inventory" && (targetLocation == "vehicle" || targetLocation == "backpack" || targetLocation == "tent"))
                    {
                        Log.Write($"[ITEMSTACK] Clearing fastSlots for SqlId={Item.SqlId} before moving to {targetLocation}");
                        ClearItemFromFastSlots(player, Item.SqlId);
                    }
                    if (targetLocation == "tent")
                    {
                        if (sessionData.TentIndex == -1) return;

                        sessionData.InventoryTentData = new InventoryTentData
                        {
                            ArrayName = ArrayName,
                            Index = Index,
                            Value = Value
                        };
                        Trigger.ClientEvent(player, "openInput", LangFunc.GetText(LangType.Ru, DataName.ItemSell), LangFunc.GetText(LangType.Ru, DataName.ItemSellInput), 8, "sell_tent");
                        return;
                    }
                    else if (targetLocation == "inventory" && isFreeSlots(player, Item.ItemId, Value) != 0)
                        return;

                    var itemPrice = Item.Price;
                    Item.Price = 0;

                    // ✅ ИСХОДНАЯ ЛОКАЦИЯ
                    string sourceLocationName = ArrayName == "other" ? sessionData.InventoryOtherLocationName :
                                               ArrayName == "backpack" ? $"backpack_{GetItemData(player, "accessories", 8).SqlId}" :
                                               ArrayName == "trade" ? $"char_{characterData.UUID}" :
                                               $"char_{characterData.UUID}";

                    string sourceLocation = ArrayName == "other" ? (sessionData.InventoryOtherLocationName?.Split('_')[0] ?? "inventory") :
                                           ArrayName == "backpack" ? "backpack" :
                                           ArrayName == "trade" ? "trade" :
                                           "inventory";

                    // ✅ КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: СНАЧАЛА УМЕНЬШАЕМ ИСХОДНЫЙ СТАК
                    Log.Write($"[ITEMSTACK] BEFORE: sourceLocationName={sourceLocationName}, sourceLocation={sourceLocation}, Index={Index}, Item.Count={Item.Count}");

                    if (Value >= Item.Count)
                    {
                        // ✅ Переносим ВСЁ → удаляем слот
                        Log.Write($"[ITEMSTACK] Transferring ALL items (Value={Value}, Item.Count={Item.Count}, SqlId={Item.SqlId})");

                        // ✅ 1. СНАЧАЛА УДАЛЯЕМ ИСХОДНЫЙ ПРЕДМЕТ
                        DeletePlaceholdersForSlot(player, sourceLocationName, sourceLocation, Index, forceDelete: true);

                        // ✅ 2. УДАЛЯЕМ ИЗ ПАМЯТИ
                        ItemId ItemIdDell = Item.ItemId;
                        int SqlIdDell = Item.SqlId;

                        if (ItemsData.ContainsKey(sourceLocationName) &&
                            ItemsData[sourceLocationName].ContainsKey(sourceLocation) &&
                            ItemsData[sourceLocationName][sourceLocation].ContainsKey(Index))
                        {
                            ItemsData[sourceLocationName][sourceLocation].TryRemove(Index, out _);
                        }

                        // ✅ 3. УДАЛЯЕМ ИЗ БД
                        // ✅ 3. УДАЛЯЕМ ИЗ БД
                        if (SqlIdDell > 0)
                        {
                            Log.Write($"[ITEMSTACK] Deleting from DB: SqlId={SqlIdDell}");

                            // ✅ ПРЯМОЙ ВЫЗОВ
                            Database.Models.Items.AddItemDelete(SqlIdDell);

                            // ✅ Логируем
                            GameLog.Items($"deletedItem({SqlIdDell})", sourceLocationName, (int)ItemIdDell, 0, "");
                            OnDellItem(ItemIdDell, "");
                        }

                        // ✅ 4. ОБНОВЛЯЕМ КЛИЕНТА
                        UpdatePlayerItemData(player, sourceLocationName, sourceLocation, Index, new InventoryItemData());

                        // ✅ 5. ДОБАВЛЯЕМ В ЦЕЛЕВУЮ ЛОКАЦИЮ
                        if (AddNewItem(player, targetLocationName, targetLocation, Item.ItemId, Value, Item.Data, MaxSlots: GetMaxSlots(player, targetLocation)) == -1)
                        {
                            // ❌ ОТКАТ: Восстанавливаем предмет
                            Log.Write($"[ITEMSTACK ERROR] Failed to add item, rolling back");

                            ItemsData[sourceLocationName][sourceLocation][Index] = new InventoryItemData(SqlIdDell, ItemIdDell, Value, Item.Data, Index);
                            UpdatePlayerItemData(player, sourceLocationName, sourceLocation, Index, Item);

                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.NoSpaceInventory), 3000);
                            return;
                        }

                        Log.Write($"[ITEMSTACK] Transfer completed: SqlId={SqlIdDell} deleted from {sourceLocationName}/{sourceLocation}");
                    }
                    else
                    {
                        // ✅ Переносим ЧАСТЬ → уменьшаем количество
                        Log.Write($"[ITEMSTACK] Transferring PART (Value={Value}, Item.Count={Item.Count})");

                        // ✅ 1. Уменьшаем исходный стак
                        Item.Count -= Value;
                        SetItemData(player, ArrayName, Index, Item, true);

                        Log.Write($"[ITEMSTACK] AFTER: Item.Count={Item.Count}");

                        // ✅ 2. Добавляем в целевую локацию
                        if (AddNewItem(player, targetLocationName, targetLocation, Item.ItemId, Value, Item.Data, MaxSlots: GetMaxSlots(player, targetLocation)) == -1)
                        {
                            // ❌ ОТКАТ: Если не удалось добавить, восстанавливаем исходный стак
                            Item.Count += Value;
                            SetItemData(player, ArrayName, Index, Item, true);

                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.NoSpaceInventory), 3000);
                            return;
                        }
                    }

                    if (isBuy)
                        EventSys.SendCoolMsg(player, "Рынок", "Покупка предмета", LangFunc.GetText(LangType.Ru, DataName.YouBuy, itemInfo.Name, itemPrice), "", 5000);
                    else if (ArrayName != "other" && ArrayName != "backpack" && ArrayName != "trade")
                        Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.YouSuccGive, itemInfo.Name), 3000);
                    else
                        Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.YouGetItem, itemInfo.Name), 3000);

                    Log.Write($"[ITEMSTACK SUCCESS] Transfer completed");
                }
            }
            catch (Exception e)
            {
                Log.Write($"ItemStack Exception: {e.ToString()}");
            }
        }

        public static void ItemsToput(ExtPlayer player, string ArrayName, int Index)
        {
            try
            {
                if (!player.IsCharacterData()) return;
                InventoryItemData Item = GetItemData(player, ArrayName, Index);
                if (Item.ItemId == ItemId.Debug) return;
                ItemsInfo ItemInfo = ItemsInfo[Item.ItemId];
                Trigger.ClientEvent(player, "client.inventory.objecteditor", ItemInfo.Model, ArrayName, Index);
            }
            catch (Exception e)
            {
                Log.Write($"ItemsToput Exception: {e.ToString()}");
            }
        }

        public static void CleanupPlaceholders()
        {
            try
            {
                using MySqlCommand cmd = new MySqlCommand
                {
                    CommandText = @"
                DELETE FROM `items_data` 
                WHERE `item_id` = 0 
                AND (`item_data` = '' OR `item_data` IS NULL OR `item_data` LIKE 'placeholder_%')"
                };
                MySQL.Query(cmd);
                Log.Write("[CLEANUP] Удалены все пустые заглушки и старые placeholder из БД");
            }
            catch (Exception e)
            {
                Log.Write($"CleanupPlaceholders Exception: {e.ToString()}");
            }
        }
        #endregion







        public static float CalculateInventoryWeight(int uuid)
        {
            try
            {
                float totalWeight = 0f;
                string locationName = $"char_{uuid}";

                if (ItemsData.ContainsKey(locationName) && ItemsData[locationName].ContainsKey("inventory"))
                {
                    var items = ItemsData[locationName]["inventory"];

                    foreach (var item in items.Values)
                    {
                        if (item.ItemId <= 0) continue; // Пропускаем пустые слоты

                        if (!ItemsInfo.ContainsKey(item.ItemId)) continue;

                        var itemInfo = ItemsInfo[item.ItemId];
                        // ItemsInfo.Weight — в килограммах
                        totalWeight += itemInfo.Weight * item.Count;
                    }
                }

                return totalWeight;
            }
            catch (Exception e)
            {
                Log.Write($"CalculateInventoryWeight({uuid}) Exception: {e.ToString()}");
                return 0f;
            }
        }

        public static float CalculateBackpackWeight(int uuid)
        {
            try
            {
                float totalWeight = 0f;
                string locationName = $"char_{uuid}";

                if (ItemsData.ContainsKey(locationName) && ItemsData[locationName].ContainsKey("accessories"))
                {
                    var bagItem = ItemsData[locationName]["accessories"].Values.FirstOrDefault(x => x.ItemId == ItemId.Bag);
                    if (bagItem != null)
                    {
                        string backpackLocation = $"backpack_{bagItem.SqlId}";

                        if (ItemsData.ContainsKey(backpackLocation) && ItemsData[backpackLocation].ContainsKey("backpack"))
                        {
                            var items = ItemsData[backpackLocation]["backpack"];

                            foreach (var item in items.Values)
                            {
                                if (item.ItemId <= 0) continue;
                                if (!ItemsInfo.ContainsKey(item.ItemId)) continue;

                                var itemInfo = ItemsInfo[item.ItemId];
                                // ItemsInfo.Weight — в килограммах
                                totalWeight += itemInfo.Weight * item.Count;
                            }
                        }
                    }
                }

                return totalWeight;
            }
            catch (Exception e)
            {
                Log.Write($"CalculateBackpackWeight({uuid}) Exception: {e.ToString()}");
                return 0f;
            }
        }
    }
}
