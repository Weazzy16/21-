public static void ItemsMove(ExtPlayer player, string selectArrayName, int selectIndex, string hoverArrayName, int hoverIndex, bool isTurn = false)
{
    try
    {
        var sessionData = player.GetSessionData();
        if (sessionData == null) return;

        var characterData = player.GetCharacterData();
        if (characterData == null) return;

        string locationName = $"char_{characterData.UUID}";

        // ✅ ПОЛУЧАЕМ ПРЕДМЕТЫ
        InventoryItemData selectItem = GetItemData(player, selectArrayName, selectIndex);
        InventoryItemData hoverItem = GetItemData(player, hoverArrayName, hoverIndex);

        if (selectItem.ItemId == ItemId.Debug) return;

        // ✅ ПРОВЕРЯЕМ: ЭТО ОРУЖИЕ В РУКАХ?
        if (sessionData.ActiveWeap != null &&
            sessionData.ActiveWeap.Item != null &&
            sessionData.ActiveWeap.Item.SqlId == selectItem.SqlId)
        {
            Log.Write($"[ITEMDROP] Weapon in hands detected: SqlId={selectItem.SqlId}, removing from hands");

            Trigger.ClientEvent(player, "client.weapon.take", true);
            WeaponComponents.Remove(player);

            sessionData.ActiveWeap = new ItemStruct("", -1, null);
            sessionData.LastActiveWeap = 0;

            Trigger.ClientEvent(player, "client.inventory.clearActiveWeapon");
        }

        // ✅ УДАЛЯЕМ ЗАГЛУШКИ ДЛЯ ИСХОДНОГО ПРЕДМЕТА
        DeletePlaceholdersForSlot(player, locationName, selectArrayName, selectIndex);

        // ✅ УДАЛЯЕМ ЗАГЛУШКИ ДЛЯ ЦЕЛЕВОГО ПРЕДМЕТА (ЕСЛИ ОН ЕСТЬ)
        if (hoverItem.ItemId != ItemId.Debug)
        {
            DeletePlaceholdersForSlot(player, locationName, hoverArrayName, hoverIndex);
        }

        // ✅ ОБНОВЛЯЕМ Index И IsTurn У ПРЕДМЕТОВ
        selectItem.Index = hoverIndex;
        selectItem.IsTurn = isTurn;  // ✅ ИСПОЛЬЗУЕМ ПАРАМЕТР ФУНКЦИИ!

        hoverItem.Index = selectIndex;

        // ✅ РАЗМЕЩАЕМ ПРЕДМЕТЫ
        SetItemData(player, hoverArrayName, hoverIndex, selectItem, send: true, isSqlUpdate: true); // ✅ ПЕРЕДАЁМ isTurn
        SetItemData(player, selectArrayName, selectIndex, hoverItem, send: true, isSqlUpdate: true);

        // ✅ СОЗДАЁМ ЗАГЛУШКИ ДЛЯ НОВОГО МЕСТА
        CreatePlaceholdersForSlot(player, locationName, hoverArrayName, hoverIndex, selectItem.ItemId, selectItem.IsTurn);

        // ✅ СОЗДАЁМ ЗАГЛУШКИ ДЛЯ ИСХОДНОГО МЕСТА
        if (hoverItem.ItemId != ItemId.Debug)
        {
            CreatePlaceholdersForSlot(player, locationName, selectArrayName, selectIndex, hoverItem.ItemId, hoverItem.IsTurn);
        }

        Log.Write($"[ITEMMOVE SUCCESS] Moved item: {selectItem.ItemId} from slot {selectIndex} to slot {hoverIndex}, IsTurn={selectItem.IsTurn}");
    }
    catch (Exception e)
    {
        Log.Write($"ItemsMove Exception: {e.ToString()}");
    }
}