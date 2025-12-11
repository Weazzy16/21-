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

        // ✅ ПОЛУЧАЕМ locationName ДЛЯ КАЖДОГО МАССИВА ОТДЕЛЬНО
        string selectLocationName = GetLocationName(player, selectArrayName);
        string hoverLocationName = GetLocationName(player, hoverArrayName);

        // ✅ ПОЛУЧАЕМ РЕАЛЬНЫЕ LOCATION (без префикса)
        string selectLocation = selectArrayName == "backpack" ? "backpack" :
                                selectArrayName == "other" ? selectLocationName.Split('_')[0] :
                                selectArrayName;

        string hoverLocation = hoverArrayName == "backpack" ? "backpack" :
                               hoverArrayName == "other" ? hoverLocationName.Split('_')[0] :
                               hoverArrayName;

        Log.Write($"[SWAP] selectLocationName={selectLocationName}, selectLocation={selectLocation}");
        Log.Write($"[SWAP] hoverLocationName={hoverLocationName}, hoverLocation={hoverLocation}");

        // ✅ УДАЛЯЕМ ЗАГЛУШКИ ДЛЯ ОБОИХ ПРЕДМЕТОВ
        DeletePlaceholdersForSlot(player, selectLocationName, selectLocation, selectIndex, forceDelete: true);

        if (hoverItem.ItemId != ItemId.Debug)
        {
            DeletePlaceholdersForSlot(player, hoverLocationName, hoverLocation, hoverIndex, forceDelete: true);
        }

        // ✅ ОБНОВЛЯЕМ Index И IsTurn У ПРЕДМЕТОВ
        selectItem.Index = hoverIndex;
        selectItem.IsTurn = isTurn;

        if (hoverItem.ItemId != ItemId.Debug)
        {
            hoverItem.Index = selectIndex;
            // ✅ ОСТАВЛЯЕМ ПОВОРОТ HOVER ПРЕДМЕТА КАК ЕСТЬ!  
        }

        // ✅ РАЗМЕЩАЕМ ПРЕДМЕТЫ
        Log.Write($"[SWAP] Placing selectItem at {hoverArrayName}[{hoverIndex}], IsTurn={selectItem.IsTurn}");
        SetItemData(player, hoverArrayName, hoverIndex, selectItem, send: true, isSqlUpdate: true);

        Log.Write($"[SWAP] Placing hoverItem at {selectArrayName}[{selectIndex}]");
        SetItemData(player, selectArrayName, selectIndex, hoverItem, send: true, isSqlUpdate: true);

        // ✅ СОЗДАЁМ ЗАГЛУШКИ ДЛЯ НОВОГО МЕСТА
        Log.Write($"[SWAP] Creating placeholders for selectItem at {hoverLocationName}/{hoverLocation}[{hoverIndex}]");
        CreatePlaceholdersForSlot(player, hoverLocationName, hoverLocation, hoverIndex, selectItem.ItemId, selectItem.IsTurn);

        if (hoverItem.ItemId != ItemId.Debug)
        {
            Log.Write($"[SWAP] Creating placeholders for hoverItem at {selectLocationName}/{selectLocation}[{selectIndex}]");
            CreatePlaceholdersForSlot(player, selectLocationName, selectLocation, selectIndex, hoverItem.ItemId, hoverItem.IsTurn);
        }

        Log.Write($"[SWAP] SUCCESS!  Swapped items");
    }
    catch (Exception e)
    {
        Log.Write($"ItemsMove Exception: {e.ToString()}");
    }
}