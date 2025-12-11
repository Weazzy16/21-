using GTANetworkAPI;
using NeptuneEvo.Handles;
using Redage.SDK;
using System;
using NeptuneEvo.Core;
using NeptuneEvo.Houses;
using System.Collections.Generic;
using Localization;
using NeptuneEvo.Players;
using NeptuneEvo.Character.Models;
using NeptuneEvo.Character;
using NeptuneEvo.Players.Models;
using NeptuneEvo.Chars.Models;

namespace NeptuneEvo.Chars
{
    class Events : Script
    {
        /// <summary>
        /// Логгер
        /// </summary>
        private static readonly nLog Log = new nLog("Chars.Events");
        [RemoteEvent("server.character.trade")]
        public void TryTrade(ExtPlayer player, ExtPlayer target, string type)
        {
            try
            {
                var sessionData = player.GetSessionData();
                if (sessionData == null) return;
                var characterData = player.GetCharacterData();
                if (characterData == null) return;
                if (DateTime.Now <= sessionData.RequestData.Time && sessionData.RequestData.IsRequested)
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.YouHaveActiveOrders), 3000);
                    return;
                }
                if ((sessionData.SellItemData.Seller != null || sessionData.SellItemData.Buyer != null) && Repository.TradeGet(player))
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.YouCantTrade), 3000);
                    return;
                }
                var targetSessionData = target.GetSessionData();
                if (targetSessionData == null) return;
                var targetCharacterData = target.GetCharacterData();
                if (targetCharacterData == null) return;
                else if (DateTime.Now <= targetSessionData.RequestData.Time && targetSessionData.RequestData.IsRequested)
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.PersonHavBeenBusy), 7000);
                    return;
                }
                else if ((targetSessionData.SellItemData.Seller != null || targetSessionData.SellItemData.Buyer != null) && Repository.TradeGet(target))
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.PersonCantTrade), 3000);
                    return;
                }

                if (type == "vehicle")
                {
                    var vehiclesCount = VehicleManager.GetVehiclesCarCountToPlayer(player.Name);
                    if (vehiclesCount == 0)
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.YouHaveNoCar), 3000);
                        return;
                    }
                    vehiclesCount = VehicleManager.GetVehiclesCarCountToPlayer(target.Name);
                    if (vehiclesCount == 0)
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.PersonHaveNoCar), 3000);
                        return;
                    }
                    targetSessionData.RequestData.IsRequested = true;
                    targetSessionData.RequestData.Request = "trade_vehicle";
                    targetSessionData.RequestData.From = player;
                    targetSessionData.RequestData.Time = DateTime.Now.AddSeconds(10);
                    //Notify.Send(player, NotifyType.Warning, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.YouWantTradeVeh), 3000);
                    //Notify.Send(target, NotifyType.Warning, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.PersonWantTradeVeh, player.Value), 3000);
                    EventSys.SendCoolMsg(player,"Предложение", "Обмен", $"{LangFunc.GetText(LangType.Ru, DataName.YouWantTradeVeh)}", "", 10000);
                    EventSys.SendCoolMsg(target,"Предложение", "Обмен", $"{LangFunc.GetText(LangType.Ru, DataName.PersonWantTradeVeh, player.Value)}", "", 10000);
                }
                else if (type == "house")
                {

                    var house = HouseManager.GetHouse(player, true);
                    if (house == null)
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.NoHome), 3000);
                        return;
                    }
                    House houseTarget = HouseManager.GetHouse(target, true);

                    if (houseTarget == null)
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.PersonNoHome), 3000);
                        return;
                    }

                    targetSessionData.RequestData.IsRequested = true;
                    targetSessionData.RequestData.Request = "trade_house";
                    targetSessionData.RequestData.From = player;
                    targetSessionData.RequestData.Time = DateTime.Now.AddSeconds(10);
                    //Notify.Send(player, NotifyType.Warning, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.YouWantTradeHouse), 3000);
                    //Notify.Send(target, NotifyType.Warning, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.PersonWantTradeHouse), 3000);
                    EventSys.SendCoolMsg(player,"Предложение", "Обмен", $"{LangFunc.GetText(LangType.Ru, DataName.YouWantTradeHouse)}", "", 10000);
                    EventSys.SendCoolMsg(target,"Предложение", "Обмен", $"{LangFunc.GetText(LangType.Ru, DataName.PersonWantTradeHouse)}", "", 10000);
                }
                else if (type == "business")
                {
                    if (characterData.BizIDs.Count == 0)
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.YouHaveNoBusiness), 3000);
                        return;
                    }
                    else if (targetCharacterData.BizIDs.Count == 0)
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.PersonHaveNoBusiness), 3000);
                        return;
                    }
                    targetSessionData.RequestData.IsRequested = true;
                    targetSessionData.RequestData.Request = "trade_business";
                    targetSessionData.RequestData.From = player;
                    targetSessionData.RequestData.Time = DateTime.Now.AddSeconds(10);
                    //Notify.Send(player, NotifyType.Warning, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.YouWantTradeBiz), 3000);
                    //Notify.Send(target, NotifyType.Warning, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.PersonWantTradeBiz), 3000);
                    EventSys.SendCoolMsg(player,"Предложение", "Обмен", $"{LangFunc.GetText(LangType.Ru, DataName.YouWantTradeBiz)}", "", 10000);
                    EventSys.SendCoolMsg(target,"Предложение", "Обмен", $"{LangFunc.GetText(LangType.Ru, DataName.PersonWantTradeBiz)}", "", 10000);
                }
            }
            catch (Exception e)
            {
                Log.Write($"TryTrade Exception: {e.ToString()}");
            }
        }
        [RemoteEvent("server.roullete.buy")]
        public void RoulleteBuy(ExtPlayer player, int caseid, int count)
        {
            try
            {
                Repository.RouletteBuyCase(player, caseid, count);
            }
            catch (Exception e)
            {
                Log.Write($"RoulleteOpen Exception: {e.ToString()}");
            }
        }
        [RemoteEvent("server.roullete.open")]
        public void RoulleteOpen(ExtPlayer player, int caseid, int count)
        {
            try
            {
                Repository.RouletteOpenCase(player, caseid, count);
            }
            catch (Exception e)
            {
                Log.Write($"RoulleteOpen Exception: {e.ToString()}");
            }
        }
        [RemoteEvent("server.roullete.confirm")]
        public static void RoulleteConfirm(ExtPlayer player, bool type, int IndexList)
        {
            try
            {
                if (IndexList == -1)
                {
                    var sessionData = player.GetSessionData();
                    if (sessionData == null) return;

                    List<int> _DoneListId = new List<int>();
                    foreach (RouletteData playerData in sessionData.RouletteData)
                    {
                        if (!playerData.Done)
                            _DoneListId.Add(playerData.IndexList);
                    }
                    foreach(int index in _DoneListId)
                    {
                        Repository.RouletteConfirmCase(player, type, index);
                    }
                }
                else Repository.RouletteConfirmCase(player, type, IndexList);
            }
            catch (Exception e)
            {
                Log.Write($"RoulleteConfirm Exception: {e.ToString()}");
            }
        }
        [RemoteEvent("server.gamemenu.inventory.move")]
        public void InventoryMove(ExtPlayer player, string selectArrayName, int selectIndex, string hoverArrayName, int hoverIndex, bool isTurn = false)
        {
            try
            {
                if (!player.IsCharacterData()) return;

                // ✅ ПРОВЕРЯЕМ ВАЛИДНОСТЬ ИНДЕКСОВ
                if (selectIndex < 0 || hoverIndex < 0)
                {
                    Log.Write($"[ITEMMOVE ERROR] Invalid index: selectIndex={selectIndex}, hoverIndex={hoverIndex}");
                    return;
                }

                // ✅ ЕСЛИ ЭТО ПЕРЕМЕЩЕНИЕ В FASTSLOTS
                if (hoverArrayName == "fastSlots")
                {
                    // ✅ ПРОВЕРЯЕМ ГРАНИЦЫ
                    if (hoverIndex < 0 || hoverIndex >= 3)
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter,
                            "Неверный индекс слота!", 3000);
                        return;
                    }

                    var item = Repository.GetItemData(player, selectArrayName, selectIndex);
                    if (item == null || item.ItemId == ItemId.Debug) return;

                    // ✅ БЕЗОПАСНАЯ ПРОВЕРКА: УЖЕ ЛИ ЭТОТ ПРЕДМЕТ В FASTSLOTS?
                    for (int i = 0; i < 3; i++)
                    {
                        try
                        {
                            var existingItem = Repository.GetItemData(player, "fastSlots", i);

                            // ✅ ПРОВЕРКА НА NULL
                            if (existingItem != null &&
                                existingItem.SqlId == item.SqlId &&
                                existingItem.ItemId != ItemId.Debug)
                            {
                                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter,
                                    "Этот предмет уже в быстрых слотах!", 3000);
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Write($"[FASTSLOT CHECK] Exception at slot {i}: {ex.Message}");
                            // Продолжаем проверку других слотов
                            continue;
                        }
                    }

                    // ✅ ПРОВЕРЯЕМ: ЭТО ОРУЖИЕ ИЛИ РАЗРЕШЁННЫЙ ПРЕДМЕТ?
                    if (!Repository.ItemsInfo.ContainsKey(item.ItemId))
                    {
                        Log.Write($"[ITEMMOVE ERROR] ItemId {item.ItemId} not found in ItemsInfo");
                        return;
                    }

                    var itemInfo = Repository.ItemsInfo[item.ItemId];

                    List<ItemId> allowedItems = new List<ItemId>
            {
                ItemId.HealthKit, ItemId.HealthKit2, ItemId.Bint, ItemId.Epinephrine,
                ItemId.Burger, ItemId.HotDog, ItemId.Sandwich, ItemId.Crisps, ItemId.Pizza,
                ItemId.eCola, ItemId.Sprunk, ItemId.Vape, ItemId.Rose, ItemId.Barbell,
                ItemId.Binoculars, ItemId.Bong, ItemId.Umbrella, ItemId.Camera, ItemId.Microphone,
                ItemId.Guitar, ItemId.NeonStick, ItemId.GlowStick, ItemId.VehicleNumber
            };

                    if (itemInfo.functionType == newItemType.Weapons ||
                        itemInfo.functionType == newItemType.MeleeWeapons ||
                        allowedItems.Contains(item.ItemId))
                    {
                        // ✅ КОПИРУЕМ В FASTSLOTS (БЕЗ УДАЛЕНИЯ ИЗ ИСТОЧНИКА!)
                        Repository.SetItemData(player, hoverArrayName, hoverIndex, item, send: true, isSqlUpdate: true);
                        return;
                    }
                    else
                    {
                        // ✅ ДЛЯ ДРУГИХ ПРЕДМЕТОВ — ПЕРЕМЕЩАЕМ (С УДАЛЕНИЕМ)
                        Repository.ItemsMove(player, selectArrayName, selectIndex, hoverArrayName, hoverIndex);
                        return;
                    }
                }

                // ✅ ЕСЛИ ЭТО ПЕРЕМЕЩЕНИЕ ИЗ FASTSLOTS (УДАЛЕНИЕ)
                if (selectArrayName == "fastSlots" && hoverIndex == -1)
                {
                    if (selectIndex < 0 || selectIndex >= 3)
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter,
                            "Неверный индекс слота!", 3000);
                        return;
                    }

                    // ✅ ПРОСТО ОЧИЩАЕМ СЛОТ
                    Repository.SetItemData(player, selectArrayName, selectIndex, new InventoryItemData(), true);
                    return;
                }

                // ✅ ЕСЛИ ЭТО ПЕРЕМЕЩЕНИЕ ИЗ FASTSLOTS (ОБЫЧНОЕ)
                if (selectArrayName == "fastSlots")
                {
                    if (selectIndex < 0 || selectIndex >= 3)
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter,
                            "Неверный индекс слота!", 3000);
                        return;
                    }

                    var item = Repository.GetItemData(player, selectArrayName, selectIndex);
                    if (item == null || item.ItemId == ItemId.Debug) return;

                    if (!Repository.ItemsInfo.ContainsKey(item.ItemId))
                    {
                        Log.Write($"[ITEMMOVE ERROR] ItemId {item.ItemId} not found in ItemsInfo");
                        return;
                    }

                    var itemInfo = Repository.ItemsInfo[item.ItemId];

                    List<ItemId> allowedItems = new List<ItemId>
            {
                ItemId.HealthKit, ItemId.HealthKit2, ItemId.Bint, ItemId.Epinephrine,
                ItemId.Burger, ItemId.HotDog, ItemId.Sandwich, ItemId.Crisps, ItemId.Pizza,
                ItemId.eCola, ItemId.Sprunk, ItemId.Vape, ItemId.Rose, ItemId.Barbell,
                ItemId.Binoculars, ItemId.Bong, ItemId.Umbrella, ItemId.Camera, ItemId.Microphone,
                ItemId.Guitar, ItemId.NeonStick, ItemId.GlowStick, ItemId.VehicleNumber
            };

                    if (itemInfo.functionType == newItemType.Weapons ||
                        itemInfo.functionType == newItemType.MeleeWeapons ||
                        allowedItems.Contains(item.ItemId))
                    {
                        // ✅ ДЛЯ ОРУЖИЯ И РАЗРЕШЁННЫХ ПРЕДМЕТОВ — ОЧИЩАЕМ FASTSLOT
                        Repository.SetItemData(player, selectArrayName, selectIndex, new InventoryItemData(), true);
                        return;
                    }
                    else
                    {
                        // ✅ ДЛЯ ДРУГИХ ПРЕДМЕТОВ — ПЕРЕМЕЩАЕМ
                        Repository.ItemsMove(player, selectArrayName, selectIndex, hoverArrayName, hoverIndex);
                        return;
                    }
                }

                // ✅ ОБЫЧНОЕ ПЕРЕМЕЩЕНИЕ (НЕ FASTSLOTS)
                Repository.ItemsMove(player, selectArrayName, selectIndex, hoverArrayName, hoverIndex, isTurn);
            }
            catch (Exception e)
            {
                Log.Write($"InventoryMove Exception: {e.ToString()}");
            }
        }
        [RemoteEvent("server. gamemenu.inventory.installmod")]
        public void InstallWeaponModification(ExtPlayer player, string fromArrayName, int fromIndex, int weaponIndex, int modSlotIndex)
        {
            try
            {
                var characterData = player.GetCharacterData();
                if (characterData == null) return;

                var sessionData = player.GetSessionData();
                if (sessionData == null) return;

                Log.Write($"[INSTALLMOD] Player: {player.Name}, From: {fromArrayName}[{fromIndex}], WeaponIndex: {weaponIndex}, ModSlot: {modSlotIndex}");

                string locationName = $"char_{characterData.UUID}";

                // ✅ 1.  ПОЛУЧАЕМ МОДИФИКАЦИЮ ИЗ ИНВЕНТАРЯ
                InventoryItemData modItem = Repository.GetItemData(player, fromArrayName, fromIndex);

                if (modItem.ItemId == ItemId.Debug || modItem.ItemId == 0)
                {
                    Log.Write($"[INSTALLMOD ERROR] Mod item is empty");
                    return;
                }

                Log.Write($"[INSTALLMOD] Mod ItemId: {modItem.ItemId}, Data: {modItem.Data}");

                // ✅ 2. ПРОВЕРЯЕМ, ЧТО ЭТО МОДИФИКАЦИЯ
                if (!Repository.ItemsInfo.ContainsKey(modItem.ItemId))
                {
                    Log.Write($"[INSTALLMOD ERROR] ItemId {modItem.ItemId} not found in ItemsInfo");
                    return;
                }

                var modInfo = Repository.ItemsInfo[modItem.ItemId];
                if (modInfo.functionType != newItemType.Modification)
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Это не модификация!", 3000);
                    return;
                }

                // ✅ 3. ПОЛУЧАЕМ ОРУЖИЕ ИЗ ИНВЕНТАРЯ
                InventoryItemData weaponItem = Repository.GetItemData(player, "inventory", weaponIndex);

                if (weaponItem.ItemId == ItemId.Debug || weaponItem.SqlId == 0)
                {
                    Log.Write($"[INSTALLMOD ERROR] Weapon not found at index {weaponIndex}");
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Оружие не найдено!", 3000);
                    return;
                }

                Log.Write($"[INSTALLMOD] Weapon ItemId: {weaponItem.ItemId}, SqlId: {weaponItem.SqlId}, Data: {weaponItem.Data}");

                // ✅ 4. ПРОВЕРЯЕМ, ЧТО ЭТО ОРУЖИЕ (а не другой предмет)
                if (!Repository.ItemsInfo.ContainsKey(weaponItem.ItemId))
                {
                    Log.Write($"[INSTALLMOD ERROR] Weapon ItemId {weaponItem.ItemId} not found in ItemsInfo");
                    return;
                }

                var weaponInfo = Repository.ItemsInfo[weaponItem.ItemId];
                if (weaponInfo.functionType != newItemType.Weapons && weaponInfo.functionType != newItemType.MeleeWeapons)
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Это не оружие!", 3000);
                    return;
                }

                string weaponLocationName = $"weapon_{weaponItem.SqlId}";

                // ✅ 5. ПРОВЕРЯЕМ ТИП МОДИФИКАЦИИ (чтобы нельзя было ставить 2 глушителя и т.д.)
                // ✅ 5.  ПРОВЕРЯЕМ ТИП МОДИФИКАЦИИ (чтобы нельзя было ставить 2 глушителя и т.д.)
                wComponentsType modType = GetModificationType(modItem.ItemId);

                if (modType == (wComponentsType)0)
                {
                    Log.Write($"[INSTALLMOD ERROR] Unknown modification type for ItemId {modItem.ItemId}");
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Неизвестный тип модификации!", 3000);
                    return;
                }

                if (Repository.ItemsData.ContainsKey(weaponLocationName) &&
                    Repository.ItemsData[weaponLocationName].ContainsKey("weapon"))
                {
                    foreach (var existingMod in Repository.ItemsData[weaponLocationName]["weapon"].Values)
                    {
                        // Проверяем, нет ли уже модификации такого же ТИПА
                        wComponentsType existingModType = GetModificationType(existingMod.ItemId);

                        // Varmod и Camo можно ставить несколько (разные варианты), остальные типы - только один
                        if (existingModType == modType && modType != wComponentsType.Varmod && modType != wComponentsType.Camo)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter,
                                $"Модификация этого типа уже установлена!", 3000);
                            return;
                        }
                    }
                }

                // ✅ 6. УДАЛЯЕМ МОДИФИКАЦИЮ ИЗ ИНВЕНТАРЯ
                Repository.DeletePlaceholdersForSlot(player, locationName, fromArrayName, fromIndex, forceDelete: true);
                Repository.SetItemData(player, fromArrayName, fromIndex, new InventoryItemData(), true, true);

                // ✅ 7. ДОБАВЛЯЕМ МОДИФИКАЦИЮ В ХРАНИЛИЩЕ ОРУЖИЯ
                // Обновляем Data модификации, чтобы она была привязана к текущему оружию
                uint weaponHash = (uint)WeaponRepository.GetHash(weaponItem.ItemId.ToString());
                string newModData = $"{weaponHash}_{GetComponentHashFromModItem(modItem)}";

                InventoryItemData newModItem = new InventoryItemData
                {
                    SqlId = modItem.SqlId,
                    ItemId = modItem.ItemId,
                    Count = modItem.Count,
                    Data = newModData,
                    Index = modSlotIndex
                };

                if (!Repository.ItemsData.ContainsKey(weaponLocationName))
                {
                    Repository.ItemsData[weaponLocationName] = new System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Concurrent.ConcurrentDictionary<int, InventoryItemData>>();
                }

                if (!Repository.ItemsData[weaponLocationName].ContainsKey("weapon"))
                {
                    Repository.ItemsData[weaponLocationName]["weapon"] = new System.Collections.Concurrent.ConcurrentDictionary<int, InventoryItemData>();
                }

                Repository.ItemsData[weaponLocationName]["weapon"][modSlotIndex] = newModItem;

                // ✅ 8. СОХРАНЯЕМ В БД
                Database.Models.Items.AddItemUpdate(
                    newModItem.SqlId,
                    weaponLocationName,
                    newModItem.Count,
                    newModData,
                    "weapon",
                    modSlotIndex,
                    false
                );

                // ✅ 9. ОБНОВЛЯЕМ Data ОРУЖИЯ
                string componentHash = GetComponentHashFromModItem(modItem).ToString();
                string newWeaponData = weaponItem.Data ?? "";
                if (!newWeaponData.Contains(componentHash))
                {
                    newWeaponData += $"_{componentHash}";
                }
                weaponItem.Data = newWeaponData;
                Repository.SetItemData(player, "inventory", weaponIndex, weaponItem, true, true);

                // ✅ 10.  ОТПРАВЛЯЕМ ОБНОВЛЕНИЕ КЛИЕНТУ
                Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter,
                    $"Модификация {modInfo.Name} установлена!", 3000);

                Log.Write($"[INSTALLMOD SUCCESS] Mod {modItem.ItemId} installed on weapon {weaponItem.SqlId} at slot {modSlotIndex}");

                // ✅ 11.  ОБНОВЛЯЕМ ОКНО МОДИФИКАЦИЙ
                if (sessionData.InventoryOtherLocationName != null)
                {
                    var returnData = Repository.ClientEventLoadItemsData(weaponLocationName, "weapon", 5);
                    Trigger.ClientEvent(player, "client.inventory.UpdateModifications", returnData.Item1);
                }
            }
            catch (Exception e)
            {
                Log.Write($"InstallWeaponModification Exception: {e.ToString()}");
            }
        }

        // ✅ ВСПОМОГАТЕЛЬНЫЙ МЕТОД: Получить тип модификации по ItemId
        private wComponentsType GetModificationType(ItemId itemId)
        {
            switch (itemId)
            {
                case ItemId.cVarmod:
                    return wComponentsType.Varmod;
                case ItemId.cClip:
                    return wComponentsType.Clip;
                case ItemId.cSuppressor:
                    return wComponentsType.Suppressor;
                case ItemId.cScope:
                    return wComponentsType.Scope;
                case ItemId.cMuzzlebrake:
                    return wComponentsType.Muzzlebrake;
                case ItemId.cBarrel:
                    return wComponentsType.Barrel;
                case ItemId.cFlashlight:
                    return wComponentsType.Flashlight;
                case ItemId.cGrip:
                    return wComponentsType.Grip;
                case ItemId.cCamo:
                    return wComponentsType.Camo;
                default:
                    return (wComponentsType)0;
            }
        }

        // ✅ ВСПОМОГАТЕЛЬНЫЙ МЕТОД: Получить хеш компонента из Data модификации
        private uint GetComponentHashFromModItem(InventoryItemData modItem)
        {
            try
            {
                if (modItem.Data != null && modItem.Data.Split('_').Length > 1)
                {
                    return Convert.ToUInt32(modItem.Data.Split('_')[1]);
                }
            }
            catch { }

            return 0;
        }
        [RemoteEvent("server.gamemenu.inventory.removemod")]
        public void RemoveWeaponModification(ExtPlayer player, int modSlotIndex, int targetIndex)
        {
            try
            {
                var characterData = player.GetCharacterData();
                if (characterData == null) return;

                var sessionData = player.GetSessionData();
                if (sessionData == null) return;

                Log.Write($"[REMOVEMOD] Player: {player.Name}, ModSlot: {modSlotIndex}, TargetIndex: {targetIndex}");

                string locationName = $"char_{characterData.UUID}";

                // ✅ 1. ПРОВЕРЯЕМ, ЧТО ОТКРЫТО ОКНО МОДИФИКАЦИЙ
                if (sessionData.InventoryOtherLocationName == null || !sessionData.InventoryOtherLocationName.StartsWith("weapon_"))
                {
                    Log.Write($"[REMOVEMOD ERROR] No weapon window open");
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Сначала откройте окно модификаций!", 3000);
                    return;
                }

                string weaponLocationName = sessionData.InventoryOtherLocationName;

                Log.Write($"[REMOVEMOD] Weapon location: {weaponLocationName}");

                // ✅ 2.  ПРОВЕРЯЕМ, ЕСТЬ ЛИ МОДИФИКАЦИЯ В ЭТОМ СЛОТЕ
                if (!Repository.ItemsData.ContainsKey(weaponLocationName) ||
                    !Repository.ItemsData[weaponLocationName].ContainsKey("weapon") ||
                    !Repository.ItemsData[weaponLocationName]["weapon"].ContainsKey(modSlotIndex))
                {
                    Log.Write($"[REMOVEMOD ERROR] No modification in slot {modSlotIndex}");
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "В этом слоте нет модификации!", 3000);
                    return;
                }

                InventoryItemData modItem = Repository.ItemsData[weaponLocationName]["weapon"][modSlotIndex];

                if (modItem.ItemId == ItemId.Debug || modItem.ItemId == 0)
                {
                    Log.Write($"[REMOVEMOD ERROR] Slot is empty");
                    return;
                }

                Log.Write($"[REMOVEMOD] Removing mod: ItemId={modItem.ItemId}, SqlId={modItem.SqlId}");

                // ✅ 3.  ПРОВЕРЯЕМ, ЕСТЬ ЛИ МЕСТО В ИНВЕНТАРЕ
                int freeSlot = targetIndex;
                if (freeSlot < 0)
                {
                    // ✅ ИСПРАВЛЕНО: GetFreeSlot принимает 2 аргумента
                    freeSlot = Repository.GetFreeSlot(locationName, "inventory");

                    if (freeSlot == -1)
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Недостаточно места в инвентаре!", 3000);
                        return;
                    }
                }

                Log.Write($"[REMOVEMOD] Found free slot: {freeSlot}");

                // ✅ 4.  УДАЛЯЕМ МОДИФИКАЦИЮ ИЗ ХРАНИЛИЩА ОРУЖИЯ
                Repository.ItemsData[weaponLocationName]["weapon"].TryRemove(modSlotIndex, out _);

                // ✅ 5. ДОБАВЛЯЕМ МОДИФИКАЦИЮ В ИНВЕНТАРЬ
                InventoryItemData newModItem = new InventoryItemData
                {
                    SqlId = modItem.SqlId,
                    ItemId = modItem.ItemId,
                    Count = modItem.Count,
                    Data = modItem.Data,
                    Index = freeSlot
                };

                Repository.SetItemData(player, "inventory", freeSlot, newModItem, true, true);

                // ✅ 6. ОБНОВЛЯЕМ БД
                Database.Models.Items.AddItemUpdate(
                    modItem.SqlId,
                    locationName,
                    modItem.Count,
                    modItem.Data,
                    "inventory",
                    freeSlot,
                    false
                );

                // ✅ 7. ОБНОВЛЯЕМ Data ОРУЖИЯ (УДАЛЯЕМ ХЕШ КОМПОНЕНТА)
                int weaponSqlId = Convert.ToInt32(weaponLocationName.Split('_')[1]);

                for (int i = 0; i < 102; i++)
                {
                    var item = Repository.GetItemData(player, "inventory", i);
                    if (item.SqlId == weaponSqlId)
                    {
                        string componentHash = modItem.Data.Split("_")[1];
                        string weaponData = item.Data ?? "";

                        if (weaponData.Contains($"_{componentHash}"))
                        {
                            weaponData = weaponData.Replace($"_{componentHash}", "");
                            item.Data = weaponData;
                            Repository.SetItemData(player, "inventory", i, item, true, true);
                            Log.Write($"[REMOVEMOD] Removed component hash from weapon Data");
                        }
                        break;
                    }
                }

                // ✅ 8. ОТПРАВЛЯЕМ ОБНОВЛЕНИЕ КЛИЕНТУ
                Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, "Модификация снята!", 3000);

                Log.Write($"[REMOVEMOD SUCCESS] Mod {modItem.ItemId} removed from slot {modSlotIndex} to inventory[{freeSlot}]");

                // ✅ 9.  ОБНОВЛЯЕМ ОКНО МОДИФИКАЦИЙ
                var returnData = Repository.ClientEventLoadItemsData(weaponLocationName, "weapon", 5);
                Trigger.ClientEvent(player, "client.inventory.UpdateModifications", returnData.Item1);
            }
            catch (Exception e)
            {
                Log.Write($"RemoveWeaponModification Exception: {e.ToString()}");
            }
        }
        [RemoteEvent("server.gamemenu.inventory.move.stack")]
        public void InventoryMoveStack(ExtPlayer player, string selectArrayName, int selectIndex, string hoverArrayName, int hoverIndex, int count)
        {
            try
            {
                if (!player.IsCharacterData()) return;
                Repository.ItemsMoveStack(player, selectArrayName, selectIndex, hoverArrayName, hoverIndex, count);
            }
            catch (Exception e)
            {
                Log.Write($"InventoryMoveStack Exception: {e.ToString()}");
            }
        }
        [RemoteEvent("server.gamemenu.inventory.use")]
        public void InventoryUse(ExtPlayer player, string ArrayName, int Index)
        {
            try
            {
                if (!player.IsCharacterData()) return;
                Repository.ItemsUse(player, ArrayName, Index);
            }
            catch (Exception e)
            {
                Log.Write($"InventoryUse Exception: {e.ToString()}");
            }
        }
        [RemoteEvent("server.note.create")]
        public void NoteCreate(ExtPlayer player, int type, int ItemId, string nameValue, string textValue)
        {
            try
            {
                if (!player.IsCharacterData()) return;
                Repository.NoteCreate(player, type, ItemId, nameValue, textValue);
            }
            catch (Exception e)
            {
                Log.Write($"InventoryUse Exception: {e.ToString()}");
            }
        }
        [RemoteEvent("server.gamemenu.inventory.drop")]
        public void InventoryDrop(ExtPlayer player, string ArrayName, int Index, float posZ)
        {
            try
            {
                var sessionData = player.GetSessionData();
                if (sessionData == null) return;
                if (ArrayName == "fastSlots") return;
                if (DateTime.Now < sessionData.TimingsData.NextDropItem)
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.CooldownItemDrop), 3000);
                    return;
                }
                else if (player.IsInVehicle)
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.NoDropFromCar), 3000);
                    return;
                }
                else if (sessionData.AntiAnimDown)
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.YouCantDrop), 3000);
                    return;
                }
                Repository.ItemsDropToIndex(player, ArrayName, Index, true, posZ: posZ);
            }
            catch (Exception e)
            {
                Log.Write($"InventoryDrop Exception: {e.ToString()}");
            }
        }
        [RemoteEvent("server.gamemenu.inventory.trade")]
        public void InventoryTrade(ExtPlayer player, int status)
        {
            try
            {
                if (!player.IsCharacterData()) return;
                Repository.ItemsTrade(player, status);
            }
            catch (Exception e)
            {
                Log.Write($"InventoryTrade Exception: {e.ToString()}");
            }
        }
        [RemoteEvent("server.gamemenu.inventory.tradeMoney")]
        public void InventoryTradeMoney(ExtPlayer player, int money)
        {
            try
            {
                if (!player.IsCharacterData()) return;
                Repository.ItemsTradeMoney(player, money);
            }
            catch (Exception e)
            {
                Log.Write($"InventoryTradeMoney Exception: {e.ToString()}");
            }
        }
        [RemoteEvent("server.gamemenu.inventory.close")]
        public void InventoryClose(ExtPlayer player)
        {
            try
            {
                if (!player.IsCharacterData()) return;
                Repository.ItemsClose(player);
            }
            catch (Exception e)
            {
                Log.Write($"InventoryClose Exception: {e.ToString()}");
            }
        }
        [RemoteEvent("server.gamemenu.inventory.otherclose")]
        public void InventoryOtherClose(ExtPlayer player)
        {
            try
            {
                if (!player.IsCharacterData()) return;
                Repository.OtherClose(player);
            }
            catch (Exception e)
            {
                Log.Write($"InventoryOtherClose Exception: {e.ToString()}");
            }
        }
        [RemoteEvent("server.gamemenu.inventory.stack")]
        public void InventoryStack(ExtPlayer player, string ArrayName, int Index, int Id, int Value)
        {
            try
            {
                if (!player.IsCharacterData()) return;
                if (ArrayName == "fastSlots") return;
                Repository.ItemStack(player, ArrayName, Index, Id, Value);
            }
            catch (Exception e)
            {
                Log.Write($"InventoryStack Exception: {e.ToString()}");
            }
        }
        [RemoteEvent("server.gamemenu.inventory.buy")]
        public void InventoryBuy(ExtPlayer player, string ArrayName, int Index, int Value)
        {
            try
            {
                var sessionData = player.GetSessionData();
                if (sessionData == null) return;
                if (ArrayName == "fastSlots") return;

                InventoryItemData Item = Repository.GetItemData(player, ArrayName, Index);

                if (Item.ItemId == ItemId.Debug) return;
                if (Item.Price == 0) return;
                if (Repository.isFreeSlots(player, Item.ItemId, Value) != 0) return;

                sessionData.InventoryTentData = new InventoryTentData
                {
                    ArrayName = ArrayName,
                    Index = Index,
                    Value = Value
                };
                Trigger.ClientEvent(player, "openDialog", "buy_tent", LangFunc.GetText(LangType.Ru, DataName.DialogBuyFromTent, Repository.ItemsInfo[Item.ItemId].Name, MoneySystem.Wallet.Format(Item.Price )));

                //Repository.ItemBuy(player, ArrayName, Index, Value);
            }
            catch (Exception e)
            {
                Log.Write($"InventoryStack Exception: {e.ToString()}");
            }
        }
        [RemoteEvent("server.gamemenu.inventory.fastslot.remove")]
        public void InventoryFastSlotRemove(ExtPlayer player, int slotIndex)
        {
            try
            {
                if (!player.IsCharacterData()) return;

                var characterData = player.GetCharacterData();
                if (characterData == null) return;

                if (slotIndex < 0 || slotIndex >= 3)
                {
                    Log.Write($"[FASTSLOT REMOVE] Invalid slot index: {slotIndex}");
                    return;
                }

                string locationName = $"char_{characterData.UUID}";

                // ✅ ПРОВЕРЯЕМ, СУЩЕСТВУЕТ ЛИ FASTSLOTS
                if (!Repository.ItemsData.ContainsKey(locationName) ||
                    !Repository.ItemsData[locationName].ContainsKey("fastSlots"))
                {
                    Log.Write($"[FASTSLOT REMOVE] No fastSlots data found");
                    return;
                }

                // ✅ ПРОВЕРЯЕМ, СУЩЕСТВУЕТ ЛИ СЛОТ
                if (!Repository.ItemsData[locationName]["fastSlots"].ContainsKey(slotIndex))
                {
                    Log.Write($"[FASTSLOT REMOVE] Slot {slotIndex} does not exist in memory");
                    return;
                }

                // ✅ ПОЛУЧАЕМ ПРЕДМЕТ
                InventoryItemData item = Repository.GetItemData(player, "fastSlots", slotIndex);

                if (item.ItemId == ItemId.Debug || item.ItemId == 0)
                {
                    Log.Write($"[FASTSLOT REMOVE] Slot {slotIndex} is empty");
                    return;
                }

                Log.Write($"[FASTSLOT REMOVE] Removing ItemId={item.ItemId}, SqlId={item.SqlId} from fastSlots[{slotIndex}]");

                // ✅ 1. УДАЛЯЕМ ИЗ БД
                if (item.SqlId > 0)
                {
                    Database.Models.Items.AddItemDelete(item.SqlId);
                    GameLog.Items($"deletedItem({item.SqlId})", locationName, (int)item.ItemId, item.Count, item.Data);
                    Repository.OnDellItem(item.ItemId, item.Data);

                    Log.Write($"[FASTSLOT REMOVE] Deleted from DB: SqlId={item.SqlId}");
                }

                // ✅ 2. УДАЛЯЕМ ИЗ ПАМЯТИ
                Repository.ItemsData[locationName]["fastSlots"].TryRemove(slotIndex, out _);

                Log.Write($"[FASTSLOT REMOVE] Removed from memory: SlotId={slotIndex}");

                // ✅ 3. ОТПРАВЛЯЕМ КЛИЕНТУ ОБНОВЛЕНИЕ
                Repository.UpdatePlayerItemData(player, locationName, "fastSlots", slotIndex, new InventoryItemData());

                // ✅ 4. ОТПРАВЛЯЕМ ДРУГИМ ИГРОКАМ (если нужно)
                Repository.ItemsOtherUpdate(player, locationName, "fastSlots", slotIndex, new InventoryItemData());

                Log.Write($"[FASTSLOT REMOVE] Successfully removed item from fastSlots[{slotIndex}]");
            }
            catch (Exception e)
            {
                Log.Write($"InventoryFastSlotRemove Exception: {e.ToString()}");
            }
        }
        [RemoteEvent("server.gamemenu.updatestats")]
        public void UpdateStats(ExtPlayer player)
        {
            try
            {
                if (!player.IsCharacterData()) return;
                Repository.PlayerStats(player);
            }
            catch (Exception e)
            {
                Log.Write($"UpdateStats Exception: {e.ToString()}");
            }
        }
        [RemoteEvent("server.gamemenu.inventory.toput")]
        public void InventoryToput(ExtPlayer player, string ArrayName, int Index)
        {
            try
            {
                if (!player.IsCharacterData()) return;
                Repository.ItemsToput(player, ArrayName, Index);
            }
            catch (Exception e)
            {
                Log.Write($"InventoryToput Exception: {e.ToString()}");
            }
        }
        [RemoteEvent("server.testPing")]
        public static void TestPing(ExtPlayer player)
        {
            Log.Write(">>> server.testPing получил вызов");
            Trigger.ClientEvent(player, "client.testPong");
        }

        [RemoteEvent("animationEvent")]
        public void animationEvent(ExtPlayer player, bool toggle, string dirt, string name, int flag)
        {
            try
            {
                if (!player.IsCharacterData()) return;
                if (!toggle) Trigger.StopAnimation(player);
                player.PlayAnimation(dirt, name, flag);
            }
            catch (Exception e)
            {
                Log.Write($"animationEvent Exception: {e.ToString()}");
            }
        }
        [RemoteEvent("server.dropeditor.finish")]
        public void dropEditorFinish(ExtPlayer player, string arrayName, int index, float posX, float posY, float posZ, float rotX, float rotY, float rotZ)
        {
            try
            {
                if (!player.IsCharacterData()) return;
                if (player.IsInVehicle)
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, LangFunc.GetText(LangType.Ru, DataName.NoDropFromCar), 3000);
                    return;
                }
                Repository.ItemsDropToEditor(player, arrayName, index, posX, posY, posZ, rotX, rotY, rotZ);
            }
            catch (Exception e)
            {
                Log.Write($"dropEditorFinish Exception: {e.ToString()}");
            }
        }
    }
}