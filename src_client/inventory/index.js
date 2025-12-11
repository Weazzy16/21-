const callRemote = mp.events.callRemote;
const call = mp.events.call;
const callRemoteUnreliable = mp.events.callRemoteUnreliable;
const browsers = mp.browsers;
const _callRemote = mp._events.callRemote ;
const _call = mp._events.call;
require('./rewardslist')
const { itemsInfo, ItemId } = require('./itemsInfo');


global.gamemenu = false;
global.myStats = false;

let openOtherToggled = false;
let DropIntervalId = 0;
let _lastInitOtherDataHash = null;
let _lastInitOtherDataTime = 0;
const INIT_OTHER_DEBOUNCE_MS = 150;

global.pInt = (value) => {
    value = Math.round(value);
    return !value ? 0 : value;
}


global.binderFunctions.GameMenuOpen = () => {
	if (global.tableInFocus)
		return;

    if (!global.gamemenu) OpenGameMenu ();
    else global.binderFunctions.GameMenuClose ();
};

const OpenGameMenu = () => {
	try
	{
		if (!global.loggedin || global.chatActive || global.editing || global.cuffed || global.isDeath == true || global.isDemorgan == true || global.attachedtotrunk || global.menuCheck() || (global.inAirsoftLobby !== undefined && global.inAirsoftLobby >= 0)) return;
		if (!global.myStats) callRemote('server.gamemenu.updatestats');    
		mp.gui.emmit(`window.router.updateStatic("PlayerGameMenu");`);
		global.gamemenu = true;
		global.menuOpen(true);
		mp.game.graphics.startScreenEffect("MenuMGIn", 1, true);
		call("sounds.playInterface", "inventory/open_inv", 0.005);
		gm.discord(translateText("Исследует инвентарь"));
		mp.gui.emmit(`window.events.callEvent("cef.inventory.UpdateSpecialVars", ${global.localplayer.vehicle ? true : false})`);
		        _lastDropsHash = null;
				getDrops();

	}
	catch (e) 
	{
		callRemote("client_trycatch", "inventory/index", "OpenGameMenu", e.toString());
	}
}


gm.events.add(global.renderName ["500ms"], () => {
	getDrops ();
});

let _lastDropsHash = null; // ✅ Хранит хеш последних предметов

const getDrops = () => {
    try {
        if (openOtherToggled) return;
        else if (global.localplayer.vehicle) return;
        else if (!global.gamemenu) return;
        
        let DropsObject = [];
        const playerPosition = global.localplayer.position;
        
        let distance, jsonData;
        const usedSlots = new Set(); // ✅ Отслеживаем занятые слоты

        // ✅ ИМПОРТИРУЕМ itemsInfo
        const itemsInfo = require('./itemsInfo').itemsInfo;

        mp.objects.forEachInStreamRangeItems(object => {
            if (object && mp.objects.exists(object) && object['dropData']) {
                distance = mp.game.gameplay.getDistanceBetweenCoords(
                    playerPosition.x, playerPosition.y, playerPosition.z, 
                    object.position.x, object.position.y, object.position.z, true
                );
                
                if (distance < 3) {
                    jsonData = { ...object['dropData'] }; // ✅ Клонируем данные
                    if (jsonData && jsonData.ItemId) {
                        // ✅ ПОЛУЧАЕМ РАЗМЕР ПРЕДМЕТА
                        const itemConfig = itemsInfo[jsonData.ItemId];
                        if (!itemConfig) return;

                        const itemWidth = jsonData.IsTurn ? (itemConfig.Height || 1) : (itemConfig.Width || 1);
                        const itemHeight = jsonData.IsTurn ? (itemConfig.Width || 1) : (itemConfig.Height || 1);

                        // ✅ ИЩЕМ ПЕРВЫЙ СВОБОДНЫЙ БЛОК (6 колонок)
                        const maxCols = 6;
                        const maxRows = Math.ceil(114 / maxCols);

                        let placed = false;
                        for (let i = 0; i < 114; i++) {
                            const startX = i % maxCols;
                            const startY = Math.floor(i / maxCols);

                            // ✅ ПРОВЕРЯЕМ, ПОМЕСТИТСЯ ЛИ ПРЕДМЕТ
                            if (startX + itemWidth > maxCols || startY + itemHeight > maxRows) {
                                continue;
                            }

                            let canPlace = true;
                            for (let y = 0; y < itemHeight; y++) {
                                for (let x = 0; x < itemWidth; x++) {
                                    const checkIndex = (startY + y) * maxCols + (startX + x);
                                    if (usedSlots.has(checkIndex)) {
                                        canPlace = false;
                                        break;
                                    }
                                }
                                if (!canPlace) break;
                            }

                            if (canPlace) {
                                // ✅ РЕЗЕРВИРУЕМ КЛЕТКИ
                                for (let y = 0; y < itemHeight; y++) {
                                    for (let x = 0; x < itemWidth; x++) {
                                        const slotIndex = (startY + y) * maxCols + (startX + x);
                                        usedSlots.add(slotIndex);
                                    }
                                }

                                // ✅ УСТАНАВЛИВАЕМ ПРАВИЛЬНЫЙ ИНДЕКС
                                jsonData.Index = i;
                                jsonData.remoteId = object.remoteId;
                                
                                DropsObject.push(jsonData);
                                placed = true;
                                break;
                            }
                        }

                        if (!placed) {
                            mp.console.logInfo(`[getDrops] No free space for item ${jsonData.ItemId}`);
                        }
                    }
                }
            }
        });

        // ✅ ХЕШИРУЕМ ДАННЫЕ
        const currentHash = JSON.stringify(DropsObject.map(d => ({
            ItemId: d.ItemId,
            Count: d.Count,
            remoteId: d.remoteId
        })));


        if (_lastDropsHash === currentHash) {
            return;
        }

        _lastDropsHash = currentHash;

        if (DropsObject.length > 0) {
            mp.gui.emmit(`window.events.callEvent("cef.inventory.InitOtherData", 8, '${translateText("На земле")}', '${JSON.stringify(DropsObject)}', 114)`);
        } else {
            mp.gui.emmit(`window.events.callEvent("cef.inventory.InitOtherData", 0)`);
            _lastDropsHash = null;
        }
    }
    catch (e) {
        callRemote("client_trycatch", "inventory/index", "getDrops", e.toString());
    }
}

gm.events.add("client.inventory.InitBackpack", (maxSlot, json, use) => {
    mp.gui.emmit(`window.events.callEvent("cef.inventory.InitMyData", ${maxSlot}, '${json}', ${use})`);
});

gm.events.add("client.inventory.InitData", (json, use) => {
    mp.gui.emmit(`window.events.callEvent("cef.inventory.InitData", '${json}', ${use})`);
});
// Замени существующий gm.events.add("client.inventory.InitOtherData", ...) этим кодом
gm.events.add("client.inventory.InitOtherData", (otherId, otherName, json, arg4, selectItemId, isArmyCar, isMyTent, arg8) => {
    try {
        let maxSlot = null;
        let maxWeight = null;
        
        // ✅ ИСПРАВЛЕНО: Правильная обработка аргументов
        if (typeof arg8 !== 'undefined') {
            maxSlot = arg4;
            maxWeight = arg8;
        } else {
            maxSlot = 0; // ✅ БЫЛО: null
            maxWeight = arg4;
        }
        
        maxWeight = Number(maxWeight) || 40000;
        const safeMaxSlot = (maxSlot === null || typeof maxSlot === 'undefined') ? 0 : Number(maxSlot);
        
        const toJs = (v) => {
            if (typeof v === 'string') return JSON.stringify(v);
            if (v === null || v === undefined) return 'null';
            if (typeof v === 'object') return JSON.stringify(v);
            return v;
        };
        
        const otherNameJs = toJs(otherName);
        const jsonJs = toJs(json);
        const selectItemIdJs = toJs(selectItemId);

        const emmitStr = `window.events.callEvent("cef.inventory.InitOtherData", ${otherId}, ${otherNameJs}, ${jsonJs}, ${safeMaxSlot}, ${selectItemIdJs}, ${isArmyCar}, ${isMyTent}, ${maxWeight})`;
        mp.gui.emmit(emmitStr);
        
        openOtherToggled = true;
        if (!global.gamemenu) global.binderFunctions.GameMenuOpen();
    } catch (e) {
        mp.console.logInfo('[ERROR] forwarding InitOtherData to CEF failed: ' + e);
    }
});

gm.events.add("client.gamemenu.inventory.installmod", (fromArrayName, fromIndex, weaponIndex, modSlotIndex) => {
    try {
        mp.console.logInfo(`[INSTALLMOD] Installing mod: from=${fromArrayName}[${fromIndex}], weaponIndex=${weaponIndex}, modSlot=${modSlotIndex}`);
        
        callRemote('server.gamemenu.inventory.installmod', 
            String(fromArrayName), 
            global.pInt(fromIndex), 
            global.pInt(weaponIndex), 
            global.pInt(modSlotIndex)
        );
    } catch (e) {
        mp.console.logError(`[INSTALLMOD ERROR] ${e.toString()}`);
        callRemote("client_trycatch", "inventory/index", "installmod", e.toString());
    }
});
// ✅ ОБРАБОТЧИК ОБНОВЛЕНИЯ МОДИФИКАЦИЙ (ОТДЕЛЬНО ОТ OTHER!)
gm.events.add("client.inventory.UpdateModifications", (dataJson) => {
    try {
        mp.console.logInfo(`[DEBUG] UpdateModifications received`);
        mp.gui.emmit(`window.events.callEvent("cef.inventory.UpdateModifications", '${dataJson}')`);
    } catch (e) {
        mp. console.logError(`[UpdateModifications ERROR] ${e.toString()}`);
        callRemote("client_trycatch", "inventory/index", "UpdateModifications", e. toString());
    }
});
gm.events.add("client.gamemenu.inventory.removemod", (modSlotIndex, targetIndex) => {
    try {
        mp.console.logInfo(`[REMOVEMOD] Removing mod from slot ${modSlotIndex} to inventory[${targetIndex}]`);
        
        callRemote('server.gamemenu.inventory.removemod', 
            global.pInt(modSlotIndex), 
            global.pInt(targetIndex)
        );
    } catch (e) {
        mp. console.logError(`[REMOVEMOD ERROR] ${e.toString()}`);
        callRemote("client_trycatch", "inventory/index", "removemod", e. toString());
    }
});
gm.events.add("client.inventory.UpdateOtherData", (dataJson) => {
    try {
        mp. console.logInfo(`[DEBUG] UpdateOtherData received`);
        mp.gui.emmit(`window.events.callEvent("cef.inventory.UpdateOtherData", '${dataJson}')`);
    } catch (e) {
        mp.console.logError(`[UpdateOtherData ERROR] ${e.toString()}`);
        callRemote("client_trycatch", "inventory/index", "UpdateOtherData", e.toString());
    }
});

gm.events.add("client.inventory.InitOtherDataStock", (otherId, otherName, json, maxSlot, selectItemId, isArmyCar, isMyTent, maxWeight) => {
    mp.console.logInfo(`[DEBUG RAGE -> CEF] InitOtherDataStock received -> otherId=${otherId} maxSlot=${maxSlot} maxWeight=${maxWeight}`);
    try {
        const otherNameJs = JSON.stringify(otherName);
        const jsonJs = JSON.stringify(json);
        const selectItemIdJs = JSON.stringify(selectItemId);

        mp.gui.emmit(`window.events.callEvent("cef.inventory.InitOtherDataStock", ${otherId}, ${otherNameJs}, ${jsonJs}, ${maxSlot}, ${selectItemIdJs}, ${isArmyCar}, ${isMyTent}, ${maxWeight})`);
    } catch (e) {
        mp.console.logInfo('[ERROR] forwarding InitOtherDataStock to CEF failed: ' + e);
    }
});
gm.events.add("client.inventory.SlotToPrice", (json) => {
    mp.gui.emmit(`window.events.callEvent("cef.inventory.SlotToPrice", '${json}')`);
});

gm.events.add("client.inventory.OtherClose", () => {
    callRemote('server.gamemenu.inventory.otherclose');
    openOtherToggled = false;
	    _lastDropsHash = null;
    getDrops ();
});

gm.events.add("client.inventory.InitTradeData", (Name) => {
    mp.gui.emmit(`window.events.callEvent("cef.inventory.InitTradeData", '${Name}')`);
    
    openOtherToggled = true;
    if (!global.gamemenu) global.binderFunctions.GameMenuOpen ();
});

gm.events.add("client.inventory.UpdateSlot", (Location, SlotId, json, isInfo) => {
    mp.gui.emmit(`window.events.callEvent("cef.inventory.UpdateSlot", '${Location}', ${SlotId}, '${json}', ${isInfo})`);
});

gm.events.add("start:HPAR::client", (hp, ar) => {
	mp.gui.emmit(`window.events.callEvent("cef.hp.Open")`);
	mp.gui.emmit(`window.events.callEvent("cef.hp.UpdateHealth", ${hp})`);
});

gm.events.add("update:HP::client", (hp) => {
	mp.gui.emmit(`window.events.callEvent("cef.hp.UpdateHealth", ${hp})`);
});



gm.events.add("client:close::HPAR", () => {
	mp.gui.emmit(`window.events.callEvent("cef.hp.Close")`);
});

let cefEatWaterReady = false;

gm.events.add('cefEatWaterReady', () => {
	cefEatWaterReady = true;
	callRemote("cefEatWaterReady");
	
});

// Начальная инициализация
gm.events.add("start:EatSystem::client", (eat) => {
	mp.gui.emmit(`window.events.callEvent("cef.eatwater.Open")`);
	mp.gui.emmit(`window.events.callEvent("cef.eatwater.UpdateEat", "${eat.replace('%','')}")`);
});

gm.events.add("start:WaterSystem::client", (water) => {
	mp.gui.emmit(`window.events.callEvent("cef.eatwater.UpdateWater", "${water.replace('%','')}")`);
});

// Обновления еды и воды
gm.events.add("update:EatSystem::client", (eat) => {
	mp.gui.emmit(`window.events.callEvent("cef.eatwater.UpdateEat", "${eat.replace('%','')}")`);
});
gm.events.add("client.inventory.InitActiveWeapon", (json) => {
    try {
        mp.console.logInfo(`[DEBUG] InitActiveWeapon received: ${json}`);
        mp.gui.emmit(`window.events.callEvent("cef.inventory.InitActiveWeapon", '${json}')`);
    } catch (e) {
        callRemote("client_trycatch", "inventory/index", "InitActiveWeapon", e.toString());
    }
});
gm.events.add("update:WaterSystem::client", (water) => {
	mp.gui.emmit(`window.events.callEvent("cef.eatwater.UpdateWater", "${water.replace('%','')}")`);
});

gm.events.add("client:close::EatWater", () => {
	mp.gui.emmit(`window.events.callEvent("cef.eatwater.Close")`);
});


gm.events.add("client.inventory.TradeUpdate", (status) => {
    mp.gui.emmit(`window.events.callEvent("cef.inventory.TradeUpdate", ${status})`);
});

gm.events.add("client.inventory.tradeMoney", (name, value) => {
    mp.gui.emmit(`window.events.callEvent("cef.inventory.tradeMoney", "${name}", "${value}")`);
});

gm.events.add("client.inventory.Close", () => {
    global.binderFunctions.GameMenuClose ();
});

gm.events.add("client.inventory.Open", async () => {
    global.myStats = true;
      
    await global.wait(50);
    global.binderFunctions.GameMenuOpen ();
});

gm.events.add("client.gamemenu.inventory.move", (selectArrayName, selectIndex, hoverArrayName, hoverIndex, isTurn) => {
    // ✅ ПРОВЕРЯЕМ ПАРАМЕТРЫ
    if (typeof selectArrayName !== 'string' || typeof hoverArrayName !== 'string') {
        mp.console.logError(`[INVENTORY MOVE ERROR] Invalid arrayName: selectArrayName=${selectArrayName}, hoverArrayName=${hoverArrayName}`);
        return;
    }
    
    if (typeof selectIndex !== 'number' || typeof hoverIndex !== 'number') {
        mp.console.logError(`[INVENTORY MOVE ERROR] Invalid index: selectIndex=${selectIndex}, hoverIndex=${hoverIndex}`);
        return;
    }
    
    // ✅ БЕЗОПАСНОЕ ПРЕОБРАЗОВАНИЕ
    const safeSelectArrayName = String(selectArrayName);
    const safeSelectIndex = global.pInt(selectIndex);
    const safeHoverArrayName = String(hoverArrayName);
    const safeHoverIndex = global.pInt(hoverIndex);
    const safeIsTurn = Boolean(isTurn); // ✅ ДОБАВЛЕН ПАРАМЕТР
    
    mp.console.logInfo(`[INVENTORY MOVE] Calling server: ${safeSelectArrayName}[${safeSelectIndex}] → ${safeHoverArrayName}[${safeHoverIndex}], isTurn=${safeIsTurn}`);
    
    // ✅ ОТПРАВЛЯЕМ 5 ПАРАМЕТРОВ
    callRemote('server.gamemenu.inventory.move', 
        safeSelectArrayName, 
        safeSelectIndex, 
        safeHoverArrayName, 
        safeHoverIndex, 
        safeIsTurn  // ✅ ДОБАВЛЕН ПАРАМЕТР
    );
});

gm.events.add("client.gamemenu.inventory.move.stack", (selectArrayName, selectIndex, hoverArrayName, hoverIndex, count) => {
    callRemote('server.gamemenu.inventory.move.stack', String(selectArrayName), global.pInt (selectIndex), String(hoverArrayName), global.pInt (hoverIndex), global.pInt (count));
});

gm.events.add("client.gamemenu.inventory.use", (ArrayName, Index) => {
    callRemote('server.gamemenu.inventory.use', String(ArrayName), global.pInt (Index));
});
gm.events.add("client.gamemenu.inventory.takehands", (arrayName, index) => {
    try {
        mp.console.logInfo(`[TAKEHANDS] arrayName=${arrayName}, index=${index}`); // ✅ ОТЛАДКА
        callRemote("server.gamemenu.inventory.takehands", arrayName, index);
    } catch (e) {
        mp.console.logError(`[TAKEHANDS ERROR] ${e.toString()}`);
        callRemote("client_trycatch", "inventory/index", "takehands", e.toString());
    }
});
// ✅ УСТАНОВКА АКТИВНОГО ОРУЖИЯ (КОГДА ДОСТАЮТ)
// ✅ УБЕДИТЕСЬ, ЧТО ЭТИ ОБРАБОТЧИКИ ЕСТЬ В index.js:

// 1️⃣ УСТАНОВКА АКТИВНОГО ОРУЖИЯ (КОГДА ДОСТАЮТ)
gm.events.add("client.inventory.setActiveWeapon", (sqlId, itemId, data, index) => { // ✅ ДОБАВЛЕН index
    try {
        mp.console.logInfo(`[DEBUG] setActiveWeapon: SqlId=${sqlId}, ItemId=${itemId}, Data=${data}, Index=${index}`);
        
        // ✅ ОТПРАВЛЯЕМ В CEF (С Index!)
        mp.gui.emmit(`window.events.callEvent("cef.inventory.setActiveWeapon", ${sqlId}, ${itemId}, '${data}', ${index})`);
    } catch (e) {
        mp.console.logError(`[setActiveWeapon ERROR] ${e.toString()}`);
        callRemote("client_trycatch", "inventory/index", "setActiveWeapon", e.toString());
    }
});

// 2️⃣ ОЧИСТКА АКТИВНОГО ОРУЖИЯ (КОГДА УБИРАЮТ)
gm.events.add("client.inventory.clearActiveWeapon", () => {
    try {
        mp.console.logInfo('[DEBUG] clearActiveWeapon called');
        
        // ✅ ОТПРАВЛЯЕМ В CEF
        mp.gui.emmit(`window.events.callEvent("cef.inventory.clearActiveWeapon")`);
    } catch (e) {
        mp.console.logError(`[clearActiveWeapon ERROR] ${e.toString()}`);
        callRemote("client_trycatch", "inventory/index", "clearActiveWeapon", e.toString());
    }
});

// 3️⃣ УБИРАНИЕ ОРУЖИЯ ИЗ РУК (УЖЕ ДОЛЖНО БЫТЬ)
// ✅ ВЫБРОСИТЬ ОРУЖИЕ ИЗ РУК
gm.events.add("client.weapon.dropFromHands", () => {
    try {
        mp.console.logInfo('[DEBUG] Dropping weapon from hands...');
        
        // ✅ ВЫЗЫВАЕМ СЕРВЕРНУЮ ФУНКЦИЮ
        callRemote('server.weapon.dropFromHands');
        
        // ✅ ОЧИЩАЕМ UI В CEF
        mp.gui.emmit(`window.events.callEvent("cef.inventory.clearActiveWeapon")`);
    } catch (e) {
        callRemote("client_trycatch", "inventory/index", "dropFromHands", e.toString());
    }
});

// ✅ УБРАТЬ ОРУЖИЕ (УЖЕ ДОЛЖНО БЫТЬ, НО ПРОВЕРЬТЕ!)
gm.events.add("client.weapon.takeweapon", () => {
    try {
        mp.console.logInfo('[DEBUG] Taking weapon away from hands...');
        
        // ✅ ВЫЗЫВАЕМ СЕРВЕРНУЮ ФУНКЦИЮ
        callRemote('server.weapon.take');
        
        // ✅ ОЧИЩАЕМ UI В CEF
        mp.gui.emmit(`window.events.callEvent("cef.inventory.clearActiveWeapon")`);
    } catch (e) {
        callRemote("client_trycatch", "inventory/index", "takeweapon", e.toString());
    }
});
// ✅ ИНИЦИАЛИЗАЦИЯ АКТИВНОГО ОРУЖИЯ (ПРИ ОТКРЫТИИ ИНВЕНТАРЯ)
gm.events.add("client.inventory.InitActiveWeapon", (json) => {
    try {
        mp.console.logInfo(`[DEBUG] InitActiveWeapon received: ${json}`);
        mp.gui.emmit(`window.events.callEvent("cef.inventory.InitActiveWeapon", '${json}')`);
    } catch (e) {
        callRemote("client_trycatch", "inventory/index", "InitActiveWeapon", e.toString());
    }
});

// ✅ ОБНОВЛЕНИЕ АКТИВНОГО ОРУЖИЯ (КОГДА ДОСТАЮТ/УБИРАЮТ В РЕАЛЬНОМ ВРЕМЕНИ)
gm.events.add("cef.inventory.UpdateActiveWeapon", (itemId, sqlId) => {
    try {
        mp.console.logInfo(`[DEBUG] UpdateActiveWeapon received: ItemId=${itemId}, SqlId=${sqlId}`);
        mp.gui.emmit(`window.events.callEvent("cef.inventory.UpdateActiveWeapon", ${itemId}, ${sqlId})`);
    } catch (e) {
        mp.console.logError(`[ERROR] UpdateActiveWeapon: ${e.toString()}`);
        callRemote("client_trycatch", "inventory/index", "UpdateActiveWeapon", e.toString());
    }
});

gm.events.add("client.gamemenu.inventory.drop", (ArrayName, Index) => {
	let position = global.localplayer.position;
	position.z = mp.game.gameplay.getGroundZFor3dCoord(position.x, position.y, position.z, 0.0, false);
    callRemote('server.gamemenu.inventory.drop', String(ArrayName), global.pInt (Index), position.z);
});

gm.events.add("client.gamemenu.inventory.stack", (ArrayName, Index, id, value) => {
    callRemote('server.gamemenu.inventory.stack', String(ArrayName), global.pInt (Index), global.pInt (id), global.pInt (value));
});

gm.events.add("client.gamemenu.inventory.buy", (ArrayName, Index, value) => {
    callRemote('server.gamemenu.inventory.buy', String(ArrayName), global.pInt (Index), global.pInt (value));
});

gm.events.add("client.gamemenu.inventory.trade", (status) => {
    callRemote('server.gamemenu.inventory.trade', global.pInt (status));
});
gm.events.add("client.test.weaponcheck", (msg) => {
    callRemote('server.test.weaponcheck', global.pInt (msg));
});
gm.events.add("client.gamemenu.inventory.tradeMoney", (value) => {
    callRemote('server.gamemenu.inventory.tradeMoney', global.pInt (value));
});

gm.events.add("client.gamemenu.inventory.toput", (ArrayName, Index) => {
    callRemote('server.gamemenu.inventory.toput', String(ArrayName), global.pInt (Index));
});

gm.events.add("client.gamemenu.inventory.nearby", (remoteId) => {
	const object = mp.objects.atRemoteId(remoteId);
	if (object && object.doesExist() && object.handle) {
    	callRemote('server.raise', object);
	}
});

gm.events.add("checkClientSpecialVars", () => {
	if (!global.menuCheck()) return;
	mp.gui.emmit(`window.events.callEvent("cef.inventory.UpdateSpecialVars", ${global.localplayer.vehicle ? true : false})`);
});


global.binderFunctions.GameMenuClose = (toggled = true) => {
	try
	{
		if (!global.gamemenu) return; 
    
		DropIntervalId = 0;
		if (toggled) {
			callRemote('server.gamemenu.inventory.close');
			mp.gui.emmit(`window.accountStore.otherStatsData ('{}')`);
		}
		global.myStats = false;
		mp.gui.emmit(`window.router.setHud();`);
		mp.gui.emmit(`window.events.callEvent("cef.inventory.Close")`);
		global.gamemenu = false;
		global.menuClose();
		openOtherToggled = false;
		call("sounds.playInterface", "inventory/open_inv", 0.005);
		call('client.everydayawards.close');
		mp.game.graphics.stopScreenEffect("MenuMGIn");
		        _lastDropsHash = null;

	}
	catch (e) 
	{
		callRemote("client_trycatch", "inventory/index", "global.binderFunctions.GameMenuClose", e.toString());
	}
}

global.GetItemData = (entity) => {
	try
	{
		if (entity == null || entity.type != "object" || !mp.objects.exists(entity)) return;
		if (entity['dropData'] && entity['dropData'].ItemId != undefined) {				
			mp.gui.emmit(`window.hudItem.drop (${entity['dropData'].ItemId}, ${entity['dropData'].Count}, '${entity['dropData'].Data}')`);
		} //else if () {
		//	ObjectName
		//}
	}
	catch (e) 
	{
		callRemote("client_trycatch", "inventory/index", "global.GetItemData", e.toString());
	}
}

global.GetItem = (ItemId) => {
	mp.gui.emmit(`window.isItem([${ItemId}])`);
}

global.GetItems = (ItemsId) => {
	mp.gui.emmit(`window.isItem('${JSON.stringify(ItemsId)}')`);
}
// В index.js добавьте:
// ✅ 1. ОБРАБОТЧИК ДЛЯ УДАЛЕНИЯ ИЗ FASTSLOTS
gm.events.add("client.gamemenu.inventory.fastslot.remove", (slotIndex) => {
    try {
        mp.console.logInfo(`[FASTSLOT REMOVE] Removing item from slot ${slotIndex}`);
        callRemote('server.gamemenu.inventory.fastslot.remove', global.pInt(slotIndex));
    } catch (e) {
        callRemote("client_trycatch", "inventory/index", "fastslot.remove", e.toString());
    }
});

// ✅ 2. ОБРАБОТЧИК ДЛЯ ОБНОВЛЕНИЯ ВЕСА
gm.events.add("client.inventory.UpdateWeight", (inventoryWeight, backpackWeight) => {
    try {
        mp.console.logInfo(`[DEBUG] UpdateWeight received: Inventory=${inventoryWeight}, Backpack=${backpackWeight}`);
        mp.gui.emmit(`window.events.callEvent("cef.inventory.UpdateWeight", ${inventoryWeight}, ${backpackWeight})`);
    } catch (e) {
        callRemote("client_trycatch", "inventory/index", "UpdateWeight", e.toString());
    }
});
mp.events.add('render', () => {
    if (global.gamemenu) {
        // Отключаем клавиши движения
        mp.game.controls.disableControlAction(0, 32, true); // W — вперёд
        mp.game.controls.disableControlAction(0, 33, true); // S — назад
        mp.game.controls.disableControlAction(0, 34, true); // A — влево
        mp.game.controls.disableControlAction(0, 35, true); // D — вправо

        // Отключаем другие необходимые действия (коды могут варьироваться)
        mp.game.controls.disableControlAction(0, 44, true); // Q (например, смена камеры или др.)
        mp.game.controls.disableControlAction(0, 74, true); // F (вход/выход из транспорта)
        mp.game.controls.disableControlAction(0, 47, true); // G (бросок предметов и т.п.)
    }
});