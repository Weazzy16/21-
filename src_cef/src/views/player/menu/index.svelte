<script>
    import './main.sass';
    import './main.css';
    import './inventory.css';
    import './fonts/inv/style.css';
    import './fonts/items/style.css';
    import './fonts/gamemenu/style.css';
    import './fonts/Gilroy/stylesheet.css';
    import './fonts/SFPro/stylesheet.css';
    import { translateText } from 'lang'
    export let visible;
    import { otherStatsData } from 'store/account'
    import { charData } from 'store/chars';

    import { charGender, charMoney, charGolod, charWater, charHealth, charArmor } from 'store/chars'
    import { executeClient } from 'api/rage'
    import { ItemType, ItemId, itemsInfo } from 'json/itemsInfo.js'
    
    import { clothesData, ItemToWeaponHash, WeaponHashToItem, stageItem, clearSlot, defaulSelectItem, defaulHoverItem, maxSlots, otherName, otherType, clothes, clothesId, clothesName, itemIdCaseToId } from './functions.js';
    import { format } from 'api/formatter'
    import wComponents from './wComponents.js';
    import wMaxHP from './wMaxHP.js';
    //import rangeslider from 'components/rangeslider/index'

    import { onMount } from 'svelte';
    import { spring } from 'svelte/motion';
    import './fonts/newinv/style.css'
    import { getPng } from './getPng.js'
// ✅ ПОДПИСКА НА ВЕС ИЗ charData

    const _rebuildTimers = {};
const REBUILD_DEBOUNCE_MS = 60; // уменьшите/увеличьте по опыту
    let activeItem = null; 
    let cdn = "https://cdn.majestic-files.com/public/master/static";

    import inventoryWeapons from 'json/inventoryweapons.js'
    

    let useVisible = -1;

    export let selectCharData;

    $: {
        if (useVisible != visible) {
            if (visible && $otherStatsData.Name) {
                selectCharData = $otherStatsData;
            } else if (visible && !$otherStatsData.Name && selectCharData !== $charData) {
                selectCharData = $charData;
            } else if (!visible && $otherStatsData.Name) {
                selectCharData = $charData;
                window.accountStore.otherStatsData ('{}');
            }
            useVisible = visible;
        }
    }

    // ========================
    // ✅ ФУНКЦИЯ РАЗМЕРА ПРЕДМЕТА
    // ========================
function getItemSize(item, isForPicture = false) {
    const baseSize = 5.02778; // vh
    
    if (!item || !item.ItemId) {
        return `width: ${baseSize}vh; height: ${baseSize}vh;`;
    }
    
    const itemConfig = itemsInfo[item.ItemId] || {};
    const baseWidth = itemConfig.Width || 1;
    const baseHeight = itemConfig.Height || 1;
    const isTurned = item.isTurn || item.IsTurn || false;
    
    if (isForPicture) {
        // ✅ ДЛЯ КАРТИНКИ: ВСЕГДА ОРИГИНАЛЬНЫЕ РАЗМЕРЫ + ПОВОРОТ
        let styles = `width: ${baseSize * baseWidth}vh; height: ${baseSize * baseHeight}vh;`;
        
        if (isTurned) {
            styles += ` transform: rotate(90deg); transform-origin: center center;`;
        }
        
        return styles;
    } else {
        // ✅ ДЛЯ КОНТЕЙНЕРА: РАЗМЕРЫ С УЧЁТОМ ПОВОРОТА
        const resultWidth = isTurned ? baseHeight : baseWidth;
        const resultHeight = isTurned ? baseWidth : baseHeight;
        
        return `width: ${baseSize * resultWidth}vh; height: ${baseSize * resultHeight}vh;`;
    }
}
    // ========================
    // ✅ ПРОВЕРКА МОЖНО ЛИ ПОЛОЖИТЬ
    // ========================
    function checkSlot(matrix, item, startX, startY) {
        if (!item || !matrix || !item.ItemId) return false;
        
        const itemConfig = itemsInfo[item.ItemId] || {};
        
        // ✅ УЧИТЫВАЕМ ПОВОРОТ
        const width = item.isTurn ? (itemConfig.Height || 1) : (itemConfig.Width || 1);
        const height = item.isTurn ? (itemConfig.Width || 1) : (itemConfig.Height || 1);
        
        // Проверяем границы
        if (startY + height > matrix.length || startX + width > (matrix[0]?.length || 0)) {
            return false;
        }
        
        // Проверяем занятость слотов
        for (let y = startY; y < startY + height; y++) {
            for (let x = startX; x < startX + width; x++) {
                const slotItem = matrix[y]?.[x];
                // Слот занят другим предметом (проверяем по SqlId чтобы игнорировать сам предмет)
                if (slotItem && slotItem.SqlId && slotItem.SqlId !== item.SqlId) {
                    return false;
                }
            }
        }
        
        return true;
    }
    // ========================
    // ✅ СОЗДАНИЕ МАТРИЦЫ
    // ========================
function createMatrix(arrayName) {
    let rows = 17, cols = 6;
    
    switch(arrayName) {
        case "other":
            rows = 19;
            break;
        case "backpack":
            rows = 6;
            break;
        case "inventory":
            rows = 17;
            break;
    }
    
    const matrix = Array(rows).fill(null).map(() => Array(cols).fill(null));
    
    const items = ItemsData[arrayName] || [];
    items.forEach((item) => {
        if (!item || !item.ItemId || item.ItemId === 0 || item.Index === undefined) return;
        
        // ✅ ЗАГЛУШКИ УЖЕ В items, ПРОСТО ПОМЕЧАЕМ КАК ЗАНЯТЫЕ
        if (item.Data?.startsWith("placeholder_")) {
            const x = item.Index % cols;
            const y = Math.floor(item.Index / cols);
            if (y < rows && x < cols) {
                matrix[y][x] = item; // Помечаем как занятую
            }
            return;
        }
        
        const x = item.Index % cols;
        const y = Math.floor(item.Index / cols);
        
        const itemConfig = itemsInfo[item.ItemId] || {};
        const width = item.isTurn ? (itemConfig.Height || 1) : (itemConfig.Width || 1);
        const height = item.isTurn ? (itemConfig.Width || 1) : (itemConfig.Height || 1);
        
        // ✅ ЗАПОЛНЯЕМ ВСЕ КЛЕТКИ
        for (let dy = 0; dy < height; dy++) {
            for (let dx = 0; dx < width; dx++) {
                const checkY = y + dy;
                const checkX = x + dx;
                if (checkY < rows && checkX < cols) {
                    matrix[checkY][checkX] = item;
                }
            }
        }
    });
    
    return matrix;
}
// ✅ ФУНКЦИЯ ОЧИСТКИ ЗАГЛУШЕК
function clearPlaceholders(arrayName) {
    ItemsData[arrayName] = ItemsData[arrayName].map(item => {
        if (item && item.Data?.startsWith("placeholder_")) {
            return { ...clearSlot };
        }
        return item;
    });
}

// ✅ ФУНКЦИЯ ПЕРЕСОЗДАНИЯ МАТРИЦЫ (ОЧИЩАЕТ СТАРЫЕ ЗАГЛУШКИ И СОЗДАЁТ НОВЫЕ)
// Замените существующую функцию rebuildMatrix(arrayName) на эту реализацию
// Отладочная и защищённая версия rebuildMatrix
// Отладочная и защищённая версия rebuildMatrix
function scheduleRebuild(arrayName) {
    if (_rebuildTimers[arrayName]) clearTimeout(_rebuildTimers[arrayName]);
    _rebuildTimers[arrayName] = setTimeout(() => {
        _rebuildTimers[arrayName] = null;
        rebuildMatrixAtomic(arrayName);
    }, REBUILD_DEBOUNCE_MS);
}
let _lastRebuildHash = {}; // ✅ Хранит хеш последнего состояния

// Новая безопасная atomic-версия rebuildMatrix
function rebuildMatrixAtomic(arrayName) {
    // ✅ НЕ ПЕРЕСТРАИВАЕМ ВО ВРЕМЯ ПЕРЕТАСКИВАНИЯ!
    if (isDragging) {
        console.log(`[REBUILD] Skipping rebuild for ${arrayName} - isDragging = true`);
        return;
    }
    
    try {
        console.log(`[REBUILD] atomic rebuild start for ${arrayName}`);
        
        let rows = 17, cols = 6;
        if (arrayName === "other") rows = 19;
        if (arrayName === "backpack") rows = 6;

        const maxSlot = arrayName === "inventory" ? 102 :
                        arrayName === "backpack" ? 48 :
                        arrayName === "other" ? 114 : 102;

        // ✅ СОЗДАЁМ ПУСТОЙ МАССИВ
        const newArr = Array(maxSlot).fill(null).map(() => ({ ...clearSlot }));
        
        // ✅ ПОЛУЧАЕМ ИСТОЧНИК ДАННЫХ
        const src = ItemsData[arrayName] || [];
        console.log(`[REBUILD] Source data for ${arrayName}:`, src.filter(i => i.ItemId && i.ItemId !== 0));
        
        // ✅ РАЗМЕЩАЕМ ПРЕДМЕТЫ НА ИХ ИСХОДНЫХ ПОЗИЦИЯХ
        src.forEach((item, index) => {
        if (!item || !item.ItemId || item.ItemId === 0) return;
        
        const targetIndex = item.Index !== undefined ? item.Index : index;
        
        // ✅ ЯВНО БЕРЁМ ПОВОРОТ
        const actualTurn = item.IsTurn !== undefined ? item.IsTurn : (item.isTurn || false);
        
        console.log(`[REBUILD] Placing ItemId=${item.ItemId} at Index=${targetIndex}, isTurn=${actualTurn}`);
        
        if (targetIndex >= 0 && targetIndex < maxSlot) {
            newArr[targetIndex] = {
                ...item,
                isTurn: actualTurn, // ✅ СОХРАНЯЕМ!
                IsTurn: actualTurn
            };
            
            // ✅ СОЗДАЁМ ЗАГЛУШКИ (С УЧЁТОМ ПОВОРОТА!)
            if ((arrayName === "inventory" || arrayName === "backpack") && !item.Data?.startsWith("placeholder_")) {
                const itemConfig = itemsInfo[item.ItemId] || {};
                
                const itemWidth = actualTurn ? (itemConfig.Height || 1) : (itemConfig.Width || 1);
                const itemHeight = actualTurn ? (itemConfig.Width || 1) : (itemConfig.Height || 1);
                
                if (itemWidth > 1 || itemHeight > 1) {
                    const startX = targetIndex % cols;
                    const startY = Math.floor(targetIndex / cols);
                    
                    for (let dy = 0; dy < itemHeight; dy++) {
                        for (let dx = 0; dx < itemWidth; dx++) {
                            if (dx === 0 && dy === 0) continue;
                            
                            const placeholderIndex = (startY + dy) * cols + (startX + dx);
                            if (placeholderIndex < maxSlot && placeholderIndex >= 0) {
                                newArr[placeholderIndex] = {
                                    ...clearSlot,
                                    Data: `placeholder_${targetIndex}`
                                };
                            }
                        }
                    }
                }
            }
        }
    });

        // ✅ ПРИСВАИВАЕМ ГОТОВУЮ СТРУКТУРУ
        ItemsData[arrayName] = newArr;

        // ✅ ПЕРЕСЧИТЫВАЕМ ВЕС (БЕЗ РЕКУРСИИ!)
        if (arrayName === "inventory" || arrayName === "backpack") {
            recalculateWeight(arrayName);
        }

        const finalItems = newArr.filter(i => i.ItemId && i.ItemId !== 0);
        const placeholders = newArr.filter(i => i.Data && i.Data.startsWith("placeholder_"));
        
        console.log(`[REBUILD] ${arrayName} done: ${finalItems.length} items, ${placeholders.length} placeholders`);
    } catch (e) {
        console.error('[REBUILD] atomic rebuild error', e);
    }
}

    let slotSize = 0;
    

    let selcetinv = false;

    const onSelectedInv = (type) => {
        selcetinv = type;
    }

    let searchText = "";

    let itemsWithDataCount = 0;

    $: itemsWithDataCount = ItemsData["other"].filter(item => item.ItemId && window.getItem(item.ItemId)).length;
// ✅ ПОСЛЕ let maxBackpackWeight = 30;
let warehouseVisible = false; // Видимость склада
let warehouseItems = [];      // Предметы склада
let warehouseWeight = 0;      // Вес склада
let warehouseCapacity = 100;  // Макс. вес склада
// ✅ ПЕРЕМЕННЫЕ ДЛЯ МОДИФИКАЦИЙ
let showModificationsModal = false;
let modificationsWeaponItem = null;
let modModalX = 0;  // ✅ ФИКСИРОВАННАЯ ПОЗИЦИЯ X
let modModalY = 0;  // ✅ ФИКСИРОВАННАЯ ПОЗИЦИЯ Y
let warehouseTitle = "";      // Название склада
let trunkMaxWeight = 40000;
    let
        fastSlots = [1, 2, 3],
        clickTime = 0,
        invOpacity = 1,
        invOldOpacity = -1,
        ItemStack = -1,
        StackValue = 1,
        tradeMoney = "",
        useInventoryArea = false,
        mouseLeaveSelectedItem = false,
        mainInventoryArea = false,
        selectItem = defaulSelectItem,
        hoverItem = defaulHoverItem,
        infoItem = defaulHoverItem,
        isMoveBlock = false,
        isDragging = false,
        moveBlock = {
            accessories: [null, null],
            inventory: [null, null],
            backpack: [null, null],
            other: [null, null],
            fastSlots: [null, null],
            trade: [null, null],
            with_trade: [null, null],
        },
        ItemsData = {
            accessories: Array(maxSlots.accessories).fill(clearSlot),
            inventory: Array(maxSlots.inventory).fill(clearSlot),
            backpack: Array(maxSlots.backpack).fill({ ...clearSlot, use: false }),
            other: Array(maxSlots.other).fill(clearSlot),
            fastSlots: Array(maxSlots.fastSlots).fill(clearSlot),
            trade: Array(maxSlots.trade).fill(clearSlot),
            with_trade: Array(maxSlots.with_trade).fill(clearSlot),
        },
        SlotToPrice = [],
        tradeInfo = {
            Active: false,
            
            YourStatus: false,
            YourStatusChange: false,
            YourMoney: 0,

            WithName: "Deluxe",
            WithStatus: false,
            WithStatusChange: false,
            WithMoney: 0
        },
        PlayerInfo = {
            Sex: 0,
            Name: "",
            Backpack: false
        },
        OtherInfo = {
            Id: 0,
            Name: "",
        },
        OtherItemId = 0,
        OtherSqlId = 0,
        isArmyCar = false,
        isInVehicle = false;

    /* Functions */
    let coords = spring({ x: 0, y: 0 }, {
        stiffness: 1.0,
        damping: 1.0
    });

const Close = () => {
    tradeInfo = {
        Active: false,
        
        YourStatus: false,		 	// Статус готовности обмена
        YourStatusChange: false, 	// Нажата кнопка "Обмен"
        YourMoney: "",

        WithName: "Deluxe",			// Имя игрока, с которым вы обмениваетесь
        WithStatus: false,			// Статус готовности обмена игрока, с которым вы обмениваетесь
        WithStatusChange: false, 	// Нажата кнопка "Обмен"
        WithMoney: ""
    }
    OtherInfo.Id = otherType.None;
    OtherInfo.Name = "";
    ItemsData.other = Array(maxSlots.other).fill(clearSlot);
    ItemsData.trade = Array(maxSlots.trade).fill(clearSlot);
    ItemsData.with_trade = Array(maxSlots.with_trade).fill(clearSlot);

    if (invOldOpacity != -1) {
        invOpacity = invOldOpacity;
        invOldOpacity = -1;
    }
    itemNoUse (1);
}
window.getItemToCount = (_ItemId) => {
    let count = 0;
    for (let arrayName in ItemsData) {
        if (arrayName !== "other" && arrayName !== "trade" && arrayName !== "with_trade" && arrayName !== "backpack") 
        {
            ItemsData[arrayName].forEach((i) => {
                if (i.ItemId == _ItemId) {
                    count += Math.round (i.Count);
                }
            })
        }
    }
    return count;
}
window.isItem = (_ItemsId) => {
    _ItemsId = JSON.parse (_ItemsId);
    let rItemId = [];
    for (let arrayName in ItemsData) {
        if (arrayName !== "other" && arrayName !== "trade" && arrayName !== "with_trade" && arrayName !== "backpack") 
        {
            ItemsData[arrayName].forEach((i) => {
                if (_ItemsId.includes(i.ItemId)) {
                    rItemId.push(i.ItemId);
                }
            })
        }
    }
    if (rItemId.length > 0) {
        executeClient ("client.inventory.GetItem", JSON.stringify(rItemId), true);
        return true;
    } else {
        executeClient ("client.inventory.GetItem", _ItemsId, false);
        return false;
    }
}
const Bool = (text) => {
    return String(text).toLowerCase() === "true";
}

// ✅ ТИПЫ СЛОТОВ МОДИФИКАЦИЙ
const modSlotTypes = {
    0: "clip",      // Магазин
    1: "suppressor", // Глушитель  
    2: "muzzle",    // Дульный тормоз
    3: "flashlight", // Фонарик
    4: "grip",      // Рукоятка
    5: "scope"      // Прицел
};

// ✅ ПРОВЕРКА СОВМЕСТИМОСТИ МОДИФИКАЦИИ СО СЛОТОМ
// ✅ МАППИНГ: ИНДЕКС СЛОТА → ТИП КОМПОНЕНТА ИЗ wComponents. js
const slotIndexToComponentType = {
    0: [0],        // Слот 0 → Магазин.  (к этому типу также будут маппиться cClip и cClip2)
    1: [4, 5],     // Слот 1 → Прицел. Допускаем как Scope (4), так и Scope2 (5)
    2: [8],        // Рукоять
    3: [7],        // Фонарик
    4: [3]         // Глушитель
};

const canPlaceModification = (item, slotIndex) => {
    if (! item || ! item.ItemId || item.ItemId === 0) return true;
    if (!modificationsWeaponItem) { console.log('[CANPLACE] No weapon opened'); return false; }

    const itemInfo = itemsInfo[item.ItemId];
    if (!itemInfo || itemInfo.functionType !== ItemType.Modification) {
        console.log('[CANPLACE] Not a modification'); return false;
    }

    // Маппинг ItemId -> логический тип компонента (замените числовые ключи на ваши ItemId константы)
    // ВАЖНО: сюда добавляем и Clip и Clip2, и Scope и Scope2 и т.д.
    const itemIdToType = {
        // Пример — замените 207, 206, 209, 210 и т.д. на реальные ItemId ваших модификаций:
        207: 0, // cClip  -> логический тип "магазин"
        751: 0, // cClip2 -> тоже "магазин" (если у вас другой ItemId)
        209: 4, // cScope -> Scope
        750: 5, // cScope2 -> Scope2 (если у вас другой код)
        208: 3, // cSuppressor -> Suppressor
        212: 7, // cFlashlight -> Flash
        213: 8  // cGrip -> Grip
    };

    const componentType = itemIdToType[item.ItemId];
    if (componentType === undefined) {
        console.log(`[CANPLACE] Unknown ItemId: ${item.ItemId}`);
        return false;
    }

    const expected = slotIndexToComponentType[slotIndex];
    if (expected === undefined) {
        console.log(`[CANPLACE] Slot ${slotIndex} not supported`);
        return false;
    }

    // expected может быть числом или массивом — нормализуем
    const expectedArr = Array.isArray(expected) ? expected : [expected];

    if (!expectedArr.includes(componentType)) {
        console.log(`[CANPLACE] Type mismatch: component=${componentType}, expected=${expectedArr}`);
        return false;
    }

    console.log(`[CANPLACE] ✅ OK!  componentType=${componentType}, expected=${expectedArr}`);
    return true;
};
//Выгрука

const InitData = (json, use = true) => {
    let itemsArray = JSON.parse(json);
    for (let arrayName in itemsArray) {
        const result = LoadData(maxSlots[arrayName], itemsArray[arrayName], use);
        ItemsData[arrayName] = result.items;
        
        // ✅ ОБНОВЛЯЕМ ВЕС ИНВЕНТАРЯ
        if (arrayName === "inventory") {
            inventoryWeight = result.weight;
        }
        // ✅ ДОБАВЛЯЕМ ОБНОВЛЕНИЕ ВЕСА РЮКЗАКА
        if (arrayName === "backpack") {
            backpackWeight = result.weight;
        }
        
        // ✅ ДОБАВЛЕНО: ПЕРЕСОЗДАЁМ МАТРИЦУ СРАЗУ ПОСЛЕ ЗАГРУЗКИ
        if (arrayName === "inventory" || arrayName === "backpack") {
            rebuildMatrixAtomic(arrayName);
        }
    }
    
    // ✅ ДОБАВЛЯЕМ: Если данные из charData отличаются - используем их
    if ($charData) {
        if ($charData.InventoryWeight !== undefined && $charData.InventoryWeight !== inventoryWeight) {
            inventoryWeight = $charData.InventoryWeight;
        }
        if ($charData.BackpackWeight !== undefined && $charData.BackpackWeight !== backpackWeight) {
            backpackWeight = $charData.BackpackWeight;
        }
        if ($charData.MaxInventoryWeight !== undefined) {
            maxInventoryWeight = $charData.MaxInventoryWeight;
        }
        if ($charData.MaxBackpackWeight !== undefined) {
            maxBackpackWeight = $charData.MaxBackpackWeight;
        }
    }
}

let maxSlotBackpack = 30;
const InitMyData = (maxSlot, json, use = true) => {
    maxSlotBackpack = maxSlot;
    const result = LoadData(maxSlot, JSON.parse(json), use);
    ItemsData["backpack"] = result.items;
    
    // ✅ ОБНОВЛЯЕМ ВЕС РЮКЗАКА
    backpackWeight = result.weight;
    
    // ✅ ДОБАВЛЕНО: ПЕРЕСОЗДАЁМ МАТРИЦУ СРАЗУ ПОСЛЕ ЗАГРУЗКИ
    rebuildMatrixAtomic("backpack");
    
    // ✅ ДОБАВЛЯЕМ: Синхронизация с charData
    if ($charData && $charData.BackpackWeight !== undefined) {
        backpackWeight = $charData.BackpackWeight;
    }
    if ($charData && $charData.MaxBackpackWeight !== undefined) {
        maxBackpackWeight = $charData.MaxBackpackWeight;
    }
}

const UpdateSpecialVars = (isInVehicle_info = false) => {
    isInVehicle = isInVehicle_info;
}

const LoadData = (maxSlot, json, use = true) => {
    let returnArray = [];
    let itemsArray = json;
    let totalWeight = 0;
    
    // ✅ 1. Создаём пустой массив
    Array(maxSlot).fill(0).forEach((_, index) => {
        returnArray[index] = { ...clearSlot, use: use };
    });

    // ✅ 2. Размещаем предметы И создаём заглушки
    itemsArray.forEach((item) => {
        if (!item || item.ItemId === 0) return;
        
        const itemConfig = itemsInfo[item.ItemId] || {};
        
        // ✅ КРИТИЧНО: БЕРЁМ isTurn ИЗ СЕРВЕРНЫХ ДАННЫХ!
        const actualTurn = item.IsTurn !== undefined ? item.IsTurn : (item.isTurn || false);
        
        const itemWidth = actualTurn ? (itemConfig.Height || 1) : (itemConfig.Width || 1);
        const itemHeight = actualTurn ? (itemConfig.Width || 1) : (itemConfig.Height || 1);
        
        const startX = item.Index % 6;
        const startY = Math.floor(item.Index / 6);
        
        // ✅ Размещаем предмет (С СОХРАНЕНИЕМ isTurn!)
        returnArray[item.Index] = {
            ...clearSlot,
            ...item,
            use: use,
            isTurn: actualTurn, // ✅ ЯВНО СОХРАНЯЕМ!
            IsTurn: actualTurn  // ✅ И ЭТО ТОЖЕ!
        };
        
        // ✅ СОЗДАЁМ ЗАГЛУШКИ (С УЧЁТОМ ПОВОРОТА!)
        for (let dy = 0; dy < itemHeight; dy++) {
            for (let dx = 0; dx < itemWidth; dx++) {
                if (dx === 0 && dy === 0) continue; // Пропускаем главную клетку
                
                const placeholderIndex = (startY + dy) * 6 + (startX + dx);
                if (placeholderIndex < maxSlot) {
                    returnArray[placeholderIndex] = {
                        ...clearSlot,
                        Data: `placeholder_${item.Index}`,
                        use: false
                    };
                    
                    console.log(`[LOAD] Created placeholder at ${placeholderIndex} for item at ${item.Index} (isTurn=${actualTurn})`);
                }
            }
        }
        
        // ✅ Подсчёт веса
        if (!item.Data?.startsWith("placeholder_")) {
            const itemWeightGrams = itemConfig.Weight || 0;
            const count = item.Count || 1;
            totalWeight += (itemWeightGrams / 1000) * count;
        }
    });
    
    return { items: returnArray, weight: totalWeight };
}

const InitSlotToPrice = (json = "[]") => {
    SlotToPrice = JSON.parse(json);
}

const InitOtherData = (otherId, otherName, json, maxSlot = 20, selectItemId = 0, isArmyCar_info = false, _isMyTent = false, _SlotToPrice = "[]", maxWeight = undefined) => {
    // Корректировка сигнатуры
    if (typeof maxWeight === 'undefined' && (typeof _SlotToPrice === 'number' || (! isNaN(Number(_SlotToPrice)) && String(_SlotToPrice).length < 7))) {
        maxWeight = Number(_SlotToPrice);
        _SlotToPrice = "[]";
    }

    maxWeight = Number(maxWeight) || 40000;
    trunkMaxWeight = maxWeight / 1000;

    if (otherId == otherType. None) {
        if (OtherInfo.Id == otherType.None) return;
        closeOther();
        return;
    }
    
    OtherInfo. Id = otherId;
    OtherInfo.Name = otherName;
    OtherInfo.IsMyTent = _isMyTent;
    
    if (selectItemId != 0 && selectItemId. split("_").length) {
        OtherItemId = selectItemId.split("_")[0];
        OtherSqlId = selectItemId.split("_")[1];
    }

    console.log('[CEF DEBUG] InitOtherData otherId:', otherId, 'otherName:', otherName);
    
    let itemsArray = JSON.parse(json);
    
    SlotToPrice = JSON.parse(_SlotToPrice);

    // ✅ ЕСЛИ ЭТО МОДИФИКАЦИИ ОРУЖИЯ (ID = 9)
    if (otherId === otherType.wComponents || otherId === 9) {
        console.log('[CEF DEBUG] Loading WEAPON MODIFICATIONS');
        
        // ✅ ОЧИЩАЕМ weaponModifications
        weaponModifications = Array(5).fill(null). map(() => ({ ... clearSlot }));
        
        // ✅ НЕ ТРОГАЕМ ItemsData["other"] — СОХРАНЯЕМ ОКРУЖЕНИЕ! 
        
        itemsArray. forEach((item) => {
            if (! item || !item.ItemId || item.ItemId === 0) return;
            
            const index = item.Index;
            if (index >= 0 && index < 5) {
                weaponModifications[index] = {
                    ... clearSlot,
                    ... item,
                    arrayName: "weaponMods" // ✅ ВАЖНО! 
                };
                console.log(`[CEF DEBUG] Placed modification at slot ${index}:`, item);
            }
        });
        
        // ✅ ОТКРЫВАЕМ ОКНО МОДИФИКАЦИЙ
        showModificationsModal = true;
        
        isArmyCar = isArmyCar_info;
        return; // ✅ ВЫХОДИМ, НЕ ИЗМЕНЯЕМ ItemsData["other"]! 
    }
    
    // ✅ ЕСЛИ ЭТО Nearby (ID = 8) — ЗАГРУЖАЕМ В ItemsData["other"]
    if (otherId === otherType.Nearby || otherId === 8) {
        console.log('[CEF DEBUG] Loading NEARBY items');
        
        let returnArray = Array(maxSlot).fill(null). map(() => ({ ... clearSlot }));

        itemsArray. forEach((item) => {
            if (! item || !item. ItemId || item. ItemId === 0) return;
            
            const index = item.Index;
            if (index >= 0 && index < maxSlot) {
                returnArray[index] = {
                    ...clearSlot,
                    ...item
                };
            }
        });

        ItemsData. other = returnArray;
        isArmyCar = isArmyCar_info;
        rebuildMatrixAtomic("other");
        return;
    }
    
    // ✅ ДЛЯ ВСЕХ ОСТАЛЬНЫХ ТИПОВ (Багажник, Сейф и т.д.) — 
    // ОНИ ОТКРЫВАЮТСЯ В TrunkInventory, ПОЭТОМУ НЕ ТРОГАЕМ "other"
    console.log('[CEF DEBUG] Loading OTHER type:', otherId);
    
    let returnArray = Array(maxSlot).fill(null).map(() => ({ ...clearSlot }));

    itemsArray.forEach((item) => {
        if (!item || !item.ItemId || item.ItemId === 0) return;
        
        const index = item. Index;
        if (index >= 0 && index < maxSlot) {
            returnArray[index] = {
                ...clearSlot,
                ...item
            };
        }
    });

    ItemsData.other = returnArray;
    isArmyCar = isArmyCar_info;
    rebuildMatrixAtomic("other");
}

const InitTradeData = (Name) => {
    tradeInfo.Active = true;

    tradeInfo.YourStatus = false;
    tradeInfo.YourStatusChange = false;
    ItemsData.trade = Array(maxSlots ["trade"]).fill(clearSlot);
    tradeInfo.YourMoney = "";

    tradeInfo.WithStatus = false;
    tradeInfo.WithStatusChange = false;
    tradeInfo.WithName = Name;
    tradeInfo.WithMoney = "";
    
    ItemsData.with_trade = Array(maxSlots ["with_trade"]).fill(clearSlot);
}

const UpdateSlot = (inventoryType, inventoryIndex, json, isInfo) => {
    console.log('[UpdateSlot] Called:', { inventoryType, inventoryIndex, json });
    
    try {
        // ✅ 1. ПАРСИМ JSON
        const updatedItems = JSON.parse(json);
        console.log('[UpdateSlot] Parsed items:', updatedItems);
        
        // ✅ 2. ОБНОВЛЯЕМ ItemsData
        if (Array.isArray(updatedItems)) {
            updatedItems.forEach((item) => {
                if (item && item.Index !== undefined) {
                    const index = item.Index;
                    
                    if (ItemsData[inventoryType] && ItemsData[inventoryType][index]) {
                        ItemsData[inventoryType][index] = {
                            ...clearSlot,
                            ...item,
                            use: ItemsData[inventoryType][index].use || true
                        };
                        
                        console.log(`[UpdateSlot] Updated ${inventoryType}[${index}]:`, ItemsData[inventoryType][index]);
                    }
                }
            });
        }
        
        // ✅ 3. ПРИНУДИТЕЛЬНО ПЕРЕСОЗДАЁМ МАТРИЦУ (ДАЖЕ ВО ВРЕМЯ ПЕРЕТАСКИВАНИЯ!)
        const wasDragging = isDragging;
        isDragging = false; // ✅ ВРЕМЕННО ОТКЛЮЧАЕМ БЛОКИРОВКУ
        
        if (inventoryType === "inventory" || inventoryType === "backpack" || inventoryType === "other") {
            rebuildMatrixAtomic(inventoryType);
        }
        
        isDragging = wasDragging; // ✅ ВОССТАНАВЛИВАЕМ СОСТОЯНИЕ
        
        // ✅ 4. ОБНОВЛЯЕМ HOVER/INFO
        let hoverIndex = -1,
            hoverArrayName = -1;
            
        if (hoverItem !== defaulHoverItem) {
            hoverIndex = hoverItem.index;
            hoverArrayName = hoverItem.arrayName;
        }
        
        if (hoverIndex === -1 && hoverArrayName === -1) {
            infoItem = defaulHoverItem;
        } else {            
            const _Item = getItemToIndex(hoverIndex, hoverArrayName);
            if (_Item.ItemId != 0) {
                infoItem = {
                    ..._Item,
                    index: hoverIndex,
                    arrayName: hoverArrayName
                };
            } else {
                infoItem = defaulHoverItem;
            }
        }
        
        // ✅ 5. ПЕРЕСЧИТЫВАЕМ ВЕС
        recalculateWeight(inventoryType);
        
        console.log('[UpdateSlot] Update complete');
    } catch (e) {
        console.error('[UpdateSlot] Error:', e);
    }
}
function recalculateWeight(arrayName) {
    let totalWeight = 0;
    
    ItemsData[arrayName].forEach((item) => {
        if (item && item.ItemId && item.ItemId !== 0) {
            const itemConfig = itemsInfo[item.ItemId] || {};
            const itemWeightGrams = itemConfig.Weight || 0; // Вес в граммах
            const count = item.Count || 1;
            
            // ✅ КОНВЕРТИРУЕМ В КИЛОГРАММЫ
            totalWeight += (itemWeightGrams / 1000) * count;
        }
    });
    
    if (arrayName === "inventory") {
        inventoryWeight = totalWeight;
    } else if (arrayName === "backpack") {
        backpackWeight = totalWeight;
    }
}
window.getItem = (item) => {
    if (itemsInfo [item]) {
        return itemsInfo [item];
    }
    return {
        Name: "",
        icon: "",
        Type: "",
        Text: "",
        functionType: 0,
    }
}

const FastSlots = (json) => {
    fastSlots = JSON.parse(json);
}
onMount(() => {
if ($charData) {
        maxInventoryWeight = $charData.MaxInventoryWeight || 40;
        maxBackpackWeight = $charData.MaxBackpackWeight || 25;
    }
// ✅ ОБРАБОТЧИК setActiveWeapon (СОХРАНЯЕМ В accessories)
window.events.addEvent("cef.inventory.setActiveWeapon", (sqlId, itemId, data, index) => {
    console.log('[CEF] setActiveWeapon called:', { sqlId, itemId, data, index });
    
    const weaponSlotIndex = 19;
    
    ItemsData["accessories"][weaponSlotIndex] = {
        SqlId: sqlId,
        ItemId: itemId,
        Data: data,
        Count: 1,
        Index: index, // ✅ СОХРАНЯЕМ Index!
        isTurn: false,
        use: true
    };
    
    activeWeapon = ItemsData["accessories"][weaponSlotIndex];
    
    console.log('[CEF] activeWeapon set in accessories[19]:', activeWeapon);
});

// ✅ ОБРАБОТЧИК clearActiveWeapon
window.events.addEvent("cef.inventory.clearActiveWeapon", () => {
    console.log('[CEF] clearActiveWeapon called');
    
    const weaponSlotIndex = 19;
    ItemsData["accessories"][weaponSlotIndex] = { ...clearSlot };
    activeWeapon = null;
});
     window.events.addEvent("cef.inventory.UpdateActiveWeapon", updateActiveWeapon);

    // Инициализация инвентаря игрока
    window.events.addEvent("cef.inventory.InitData", InitData);

    window.events.addEvent("cef.inventory.InitMyData", InitMyData);

    window.events.addEvent("cef.inventory.UpdateSpecialVars", UpdateSpecialVars);
    
    // Инициализация инвентаря при взаимодействии с чем то
    window.events.addEvent("cef.inventory.InitOtherData", InitOtherData);
    
     window.events.addEvent("cef.inventory.UpdateWeight", (invWeight, bpWeight) => {
        console.log(`[CEF] UpdateWeight received: Inventory=${invWeight}, Backpack=${bpWeight}`);
        
        // ✅ ОБНОВЛЯЕМ ВЕС
        inventoryWeight = parseFloat(invWeight) || 0;
        backpackWeight = parseFloat(bpWeight) || 0;
        
        console.log(`[CEF] Updated: inventoryWeight=${inventoryWeight}, backpackWeight=${backpackWeight}`);
    });
    // Инициализация инвентаря при трейде
    window.events.addEvent("cef.inventory.InitTradeData", InitTradeData);

    // Обновление слота в любом ивентаре
    window.events.addEvent("cef.inventory.UpdateSlot", (inventoryType, inventoryIndex, json, isInfo) => {
    console.log('[UpdateSlot] EVENT RECEIVED!');
    console.log('[UpdateSlot] inventoryType:', inventoryType);
    console.log('[UpdateSlot] inventoryIndex:', inventoryIndex);
    console.log('[UpdateSlot] json:', json);
    
    try {
        const updatedItems = JSON.parse(json);
        console.log('[UpdateSlot] Parsed items:', updatedItems);
        
        // ✅ ЕСЛИ ЭТО ОДИН ПРЕДМЕТ (НЕ МАССИВ)
        if (!Array.isArray(updatedItems)) {
            const item = updatedItems;
            const index = inventoryIndex;

            if (ItemsData[inventoryType] && ItemsData[inventoryType][index] !== undefined) {
                // ✅ ЕСЛИ ItemId === 0 → УДАЛЯЕМ ПРЕДМЕТ
                if (item.ItemId === 0 || item.ItemId === undefined) {
                    console.log(`[UpdateSlot] Removing item from ${inventoryType}[${index}]`);
                    
                    ItemsData[inventoryType][index] = { 
                        ...clearSlot, 
                        use: true 
                    };
                    
                    // ✅ УДАЛЯЕМ ЗАГЛУШКИ
                    if (inventoryType === "inventory" || inventoryType === "backpack") {
                        for (let i = 0; i < ItemsData[inventoryType].length; i++) {
                            const slot = ItemsData[inventoryType][i];
                            if (slot && slot.Data === `placeholder_${index}`) {
                                console.log(`[UpdateSlot] Removing placeholder at index ${i}`);
                                ItemsData[inventoryType][i] = { ...clearSlot, use: true };
                            }
                        }
                    }
                } else {
                    console.log(`[UpdateSlot] Updating ${inventoryType}[${index}] with:`, item);
                    
                    // ✅ КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: ЯВНО БЕРЁМ IsTurn ИЗ ОТВЕТА СЕРВЕРА!
                    ItemsData[inventoryType][index] = {
                        ...clearSlot,
                        ...item,
                        Index: index,
                        isTurn: item.IsTurn !== undefined ? item.IsTurn : (item.isTurn || false), // ✅ ПРИОРИТЕТ У IsTurn!
                        use: true
                    };
                    
                    console.log('[UpdateSlot] After update:', ItemsData[inventoryType][index]);
                }
            }
        }
        // ✅ ЕСЛИ ЭТО МАССИВ ПРЕДМЕТОВ
        else {
            updatedItems.forEach((item) => {
                if (item && item.Index !== undefined) {
                    const index = item.Index;
                    
                    if (ItemsData[inventoryType] && ItemsData[inventoryType][index]) {
                        if (item.ItemId === 0 || item.ItemId === undefined) {
                            ItemsData[inventoryType][index] = { ...clearSlot, use: true };
                        } else {
                            ItemsData[inventoryType][index] = {
                                ...clearSlot,
                                ...item,
                                Index: index,
                                isTurn: item.IsTurn !== undefined ? item.IsTurn : (item.isTurn || false), // ✅ ИСПРАВЛЕНО!
                                use: true
                            };
                        }
                    }
                }
            });
        }
        
        // ✅ ПЕРЕСОЗДАЁМ МАТРИЦУ
        if (inventoryType === "inventory" || inventoryType === "backpack" || inventoryType === "other") {
            const wasDragging = isDragging;
            isDragging = false;
            rebuildMatrixAtomic(inventoryType);
            isDragging = wasDragging;
        }
        
        // ✅ ПЕРЕСЧИТЫВАЕМ ВЕС
        recalculateWeight(inventoryType);
        
        // ✅ ОБНОВЛЯЕМ SVELTE
        ItemsData = ItemsData;
        
        console.log('[UpdateSlot] Update complete!');
    } catch (e) {
        console.error('[UpdateSlot] ERROR:', e);
    }
});

// ✅ ОБРАБОТЧИК ОБНОВЛЕНИЯ МОДИФИКАЦИЙ
window.events.addEvent("cef.inventory.UpdateModifications", (dataJson) => {
    try {
        console.log('[CEF] UpdateModifications received');
        
        const items = JSON.parse(dataJson);
        
        // ✅ ОЧИЩАЕМ weaponModifications
        weaponModifications = Array(5).fill(null). map(() => ({ ...clearSlot }));
        
        // ✅ ЗАПОЛНЯЕМ НОВЫМИ ДАННЫМИ
        items.forEach(item => {
            if (item && item.ItemId && item.ItemId !== 0) {
                const index = item.Index;
                if (index >= 0 && index < 5) {
                    weaponModifications[index] = {
                        ...clearSlot,
                        ... item,
                        arrayName: "weaponMods"
                    };
                }
            }
        });
        
        console.log('[CEF] Updated weaponModifications:', weaponModifications);
    } catch (e) {
        console. error('[CEF] UpdateModifications error:', e);
    }
});
    // Инициализация инвентаря игрока
    window.events.addEvent("cef.inventory.TradeUpdate", TradeUpdate);
    
    // Инициализация инвентаря игрока
    window.events.addEvent("cef.inventory.tradeMoney", handleInputChange);
    
    // Обновление информации о бучтрых слотах
    window.events.addEvent("cef.inventory.fastSlots", FastSlots);

    // Закрытие инвентаря
    window.events.addEvent("cef.inventory.Close", Close);

    window.events.addEvent("cef.inventory.SlotToPrice", InitSlotToPrice);
    // ✅ УСТАНАВЛИВАЕМ ДЕФОЛТЫ
    maxInventoryWeight = 40;
    maxBackpackWeight = 25;

    window.events.addEvent('cef.hp.Open', open);
    window.events.addEvent('cef.hp.Close', close);
    window.events.addEvent('cef.hp.UpdateHealth', updateHealth);
    window.events.addEvent('cef.eatwater.Open', open);
    window.events.addEvent('cef.eatwater.Close', close);
    window.events.addEvent('cef.eatwater.UpdateEat', updateEat);
    window.events.addEvent('cef.eatwater.UpdateWater', updateWater);

    window.mp.trigger("cefEatWaterReady");
    window.mp.trigger("cefReady");
    setTimeout(() => {
            const slot = document.querySelector('.slot');
            if (slot) {
                slotSize = slot.getBoundingClientRect().width;
            }
        }, 100);
});
$: if ($charData) {
    if ($charData.MaxInventoryWeight !== undefined) {
        maxInventoryWeight = $charData.MaxInventoryWeight;
    }
    if ($charData.MaxBackpackWeight !== undefined) {
        maxBackpackWeight = $charData.MaxBackpackWeight;
    }
}
import { onDestroy } from 'svelte'
onDestroy(() => {
     window.events.removeEvent("cef.inventory.setActiveWeapon");
    window.events.removeEvent("cef.inventory.clearActiveWeapon");
    window.events.removeEvent("cef.inventory.UpdateActiveWeapon", updateActiveWeapon);
    window.events.removeEvent("client.inventory.InitActiveWeapon");

    // Инициализация инвентаря игрока
    window.events.removeEvent("cef.inventory.InitData", InitData);
    
    window.events.removeEvent("cef.inventory.InitMyData", InitMyData);

    window.events.removeEvent("cef.inventory.UpdateSpecialVars", UpdateSpecialVars);
    
    // Инициализация инвентаря при взаимодействии с чем то
    window.events.removeEvent("cef.inventory.InitOtherData", InitOtherData);
    
    // Инициализация инвентаря при трейде
    window.events.removeEvent("cef.inventory.InitTradeData", InitTradeData);

    // Обновление слота в любом ивентаре
    window.events.removeEvent("cef.inventory.UpdateSlot", UpdateSlot);

    // Обновление информации о items
    window.events.removeEvent("cef.inventory.Init", Init);

    // Инициализация инвентаря игрока
    window.events.removeEvent("cef.inventory.TradeUpdate", TradeUpdate);
    
    // Инициализация инвентаря игрока
    window.events.removeEvent("cef.inventory.tradeMoney", handleInputChange);
    
    // Обновление информации о бучтрых слотах
    window.events.removeEvent("cef.inventory.fastSlots", FastSlots);

    // Закрытие инвентаря
    window.events.removeEvent("cef.inventory.Close", Close);

    window.events.removeEvent("cef.inventory.SlotToPrice", InitSlotToPrice);

    
});

const onKeyDown = (event) => {
    if (!visible) return;
    
    // CTRL (прозрачность)
    if (event.which === 17 && invOldOpacity === -1) {
        invOldOpacity = invOpacity;
        invOpacity = 0;
    }
    
    // ✅ ПРОБЕЛ (ПОВОРОТ ПРЕДМЕТА) - УЛУЧШЕННАЯ ВЕРСИЯ
    // ✅ ПРОБЕЛ (ПОВОРОТ ПРЕДМЕТА) - УЛУЧШЕННАЯ ВЕРСИЯ
if (event.which === 32) {
    console.log('[ROTATE] Space pressed! ');
    console.log('[ROTATE] isDragging:', isDragging);
    console.log('[ROTATE] selectItem:', selectItem);
    console.log('[ROTATE] selectItem.ItemId:', selectItem.ItemId);
    
    // ✅ ПРОВЕРЯЕМ: ПЕРЕТАСКИВАЕМ ЛИ МЫ ПРЕДМЕТ? 
    if (isDragging && selectItem && selectItem.ItemId && selectItem.ItemId !== 0) {
        event.preventDefault();
        event.stopPropagation();
        
        const itemConfig = itemsInfo[selectItem.ItemId] || {};
        const width = itemConfig.Width || 1;
        const height = itemConfig.Height || 1;
        
        console.log('[ROTATE] Item size:', { width, height });
        
        // Если предмет квадратный (1x1, 2x2 и т.д.) - не поворачиваем
        if (width === height) {
            console.log('[ROTATE] Item is square, rotation not needed');
            return;
        }
        
        const newTurnState = !selectItem.isTurn;
        console.log('[ROTATE] Old isTurn:', selectItem.isTurn);
        console.log('[ROTATE] New isTurn:', newTurnState);
        
        // ✅ ОБНОВЛЯЕМ selectItem
        selectItem = {
            ...selectItem,
            isTurn: newTurnState,
            IsTurn: newTurnState
        };
        
        console.log('[ROTATE] selectItem updated:', selectItem);
        
        // ✅ ОБНОВЛЯЕМ КООРДИНАТЫ ДЛЯ ПЕРЕРИСОВКИ
        coords.update(c => ({
            x: c.x,
            y: c.y
        }));
        
        console.log('[ROTATE] Rotation complete! ');
    } else {
        console.log('[ROTATE] Conditions not met for rotation');
        console.log('[ROTATE] - isDragging:', isDragging);
        console.log('[ROTATE] - selectItem exists:', !!selectItem);
        console.log('[ROTATE] - selectItem.ItemId:', selectItem?. ItemId);
    }
}
}

    const onKeyUp = (event) => {  
        if (!visible) return;
        
        // CTRL (возврат прозрачности)
        if (event.which === 17 && invOldOpacity != -1) {
            invOpacity = invOldOpacity;
            invOldOpacity = -1;
        }
    }
    function checkCanPlaceItem(targetIndex, targetArrayName, itemWidth, itemHeight, sourceIndex, sourceArrayName) {
    if (! targetArrayName || targetIndex === undefined) {
        return false;
    }
    
    // ✅ ЕСЛИ sourceIndex === -1 → ЭТО НОВЫЙ ПРЕДМЕТ (НЕ SWAP!)
    const isNewItem = (sourceIndex === -1 || sourceIndex === undefined || ! sourceArrayName);
    
    console.log(`[CHECK] Checking placement: targetIndex=${targetIndex}, targetArrayName=${targetArrayName}`);
    console.log(`[CHECK] sourceIndex=${sourceIndex}, sourceArrayName=${sourceArrayName}, isNewItem=${isNewItem}`);
    
    const matrix = createMatrix(targetArrayName);
    
    let maxCols = 6;
    let maxRows = matrix.length;
    
    const startX = targetIndex % maxCols;
    const startY = Math.floor(targetIndex / maxCols);
    
    // ✅ ПРОВЕРКА ВЫХОДА ЗА ГРАНИЦЫ
    if (startX + itemWidth > maxCols || startY + itemHeight > maxRows) {
        console.log(`[CHECK] Out of bounds: startX=${startX}, itemWidth=${itemWidth}, maxCols=${maxCols}`);
        return false;
    }
    
    // ✅ ПОЛУЧАЕМ ИСХОДНЫЙ ПРЕДМЕТ (ЕСЛИ ЭТО SWAP)
    const sourceItem = ! isNewItem ? ItemsData[sourceArrayName]?.[sourceIndex] : null;
    
    // ✅ ПОЛУЧАЕМ ЦЕЛЕВОЙ ПРЕДМЕТ
    const targetItem = ItemsData[targetArrayName]?.[targetIndex];
    
    // ✅ ПОЛУЧАЕМ РАЗМЕРЫ ИСХОДНОГО ПРЕДМЕТА (ДЛЯ SWAP)
    let sourceWidth = 0, sourceHeight = 0, sourceStartX = 0, sourceStartY = 0;
    
    if (sourceItem) {
        const sourceItemConfig = itemsInfo[sourceItem.ItemId] || {};
        const sourceTurn = sourceItem.isTurn || sourceItem.IsTurn || false;
        sourceWidth = sourceTurn ? (sourceItemConfig.Height || 1) : (sourceItemConfig.Width || 1);
        sourceHeight = sourceTurn ? (sourceItemConfig.Width || 1) : (sourceItemConfig.Height || 1);
        
        sourceStartX = sourceIndex % maxCols;
        sourceStartY = Math.floor(sourceIndex / maxCols);
        
        console.log(`[CHECK] Source item: ${sourceWidth}x${sourceHeight} at (${sourceStartX}, ${sourceStartY})`);
    }
    
    // ✅ ПРОВЕРЯЕМ ВСЕ КЛЕТКИ, КОТОРЫЕ ЗАЙМЁТ ЦЕЛЕВОЙ ПРЕДМЕТ
    for (let y = 0; y < itemHeight; y++) {
        for (let x = 0; x < itemWidth; x++) {
            const checkY = startY + y;
            const checkX = startX + x;
            
            if (checkY >= maxRows || checkX >= maxCols) {
                console.log(`[CHECK] Cell out of bounds: checkX=${checkX}, checkY=${checkY}`);
                return false;
            }
            
            const cellItem = matrix[checkY]?.[checkX];
            
            // ✅ ИГНОРИРУЕМ ПУСТЫЕ КЛЕТКИ
            if (! cellItem || ! cellItem.ItemId || cellItem.ItemId === 0) {
                continue;
            }
            
            // ✅ ЗАГЛУШКИ
            if (cellItem.Data && cellItem.Data.startsWith("placeholder_")) {
                const placeholderMainIndex = parseInt(cellItem.Data.split("_")[1]);
                
                console.log(`[CHECK] Found placeholder at (${checkX}, ${checkY}), mainIndex=${placeholderMainIndex}`);
                
                // ✅ ЕСЛИ ЭТО НОВЫЙ ПРЕДМЕТ - ЗАГЛУШКИ = ЗАНЯТО! 
                if (isNewItem) {
                    console.log(`[CHECK] ❌ Placeholder found (new item mode)`);
                    return false;
                }
                
                // ✅ ЕСЛИ ЭТО SWAP В ОДНОМ МАССИВЕ - ИГНОРИРУЕМ ЗАГЛУШКИ ОБОИХ ПРЕДМЕТОВ
                if (targetArrayName === sourceArrayName) {
                    // Игнорируем заглушки исходного предмета
                    if (placeholderMainIndex === sourceIndex) {
                        console.log(`[CHECK] Ignoring source placeholder at (${checkX}, ${checkY})`);
                        continue;
                    }
                    
                    // Игнорируем заглушки целевого предмета
                    if (placeholderMainIndex === targetIndex) {
                        console.log(`[CHECK] Ignoring target placeholder at (${checkX}, ${checkY})`);
                        continue;
                    }
                }
                
                // ✅ ЕСЛИ ЭТО SWAP МЕЖДУ РАЗНЫМИ МАССИВАМИ - ИГНОРИРУЕМ ТОЛЬКО ЗАГЛУШКИ ЦЕЛЕВОГО ПРЕДМЕТА
                else {
                    if (placeholderMainIndex === targetIndex) {
                        console.log(`[CHECK] Ignoring target placeholder at (${checkX}, ${checkY})`);
                        continue;
                    }
                }
                
                // Заглушка другого предмета - ЗАНЯТО
                console.log(`[CHECK] ❌ Placeholder of another item at (${checkX}, ${checkY})`);
                return false;
            }
            
            // ✅ РЕАЛЬНЫЙ ПРЕДМЕТ
            
            // ✅ ЕСЛИ ЭТО НОВЫЙ ПРЕДМЕТ - ЛЮБОЙ ПРЕДМЕТ = ЗАНЯТО! 
            if (isNewItem) {
                console.log(`[CHECK] ❌ Cell occupied (new item mode)`);
                return false;
            }
            
            // ✅ ЕСЛИ ЭТО ИСХОДНЫЙ ПРЕДМЕТ - ИГНОРИРУЕМ
            if (targetArrayName === sourceArrayName && 
                sourceItem && 
                cellItem.SqlId === sourceItem.SqlId && 
                cellItem.ItemId === sourceItem.ItemId) {
                console.log(`[CHECK] Ignoring source item at (${checkX}, ${checkY})`);
                continue;
            }
            
            // ✅ ЕСЛИ ЭТО ОБЛАСТЬ ИСХОДНОГО ПРЕДМЕТА - ИГНОРИРУЕМ
            if (targetArrayName === sourceArrayName && sourceItem) {
                const inSourceArea = (
                    checkX >= sourceStartX && 
                    checkX < sourceStartX + sourceWidth &&
                    checkY >= sourceStartY && 
                    checkY < sourceStartY + sourceHeight
                );
                
                if (inSourceArea) {
                    console.log(`[CHECK] Cell (${checkX}, ${checkY}) is in source area - ignoring`);
                    continue;
                }
            }
            
            // ✅ ЕСЛИ ЭТО ЦЕЛЕВОЙ ПРЕДМЕТ (SWAP) - ИГНОРИРУЕМ
            if (targetItem && 
                cellItem.SqlId === targetItem. SqlId && 
                cellItem.ItemId === targetItem.ItemId) {
                console.log(`[CHECK] Ignoring target item at (${checkX}, ${checkY})`);
                continue;
            }
            
            // ✅ КЛЕТКА ЗАНЯТА ДРУГИМ ПРЕДМЕТОМ
            console.log(`[CHECK] ❌ Cell occupied by another item at (${checkX}, ${checkY}):`, cellItem);
            return false;
        }
    }
    
    console.log(`[CHECK] ✅ Can place! `);
    return true;
}
// Слот
const handleSlotMouseEnter = (event, index, arrayName) => {
    // ✅ ПОЛУЧАЕМ ПРЕДМЕТ В ТЕКУЩЕМ СЛОТЕ
   const currentSlotItem = getItemToIndex(index, arrayName);
    
    // ✅ ОТЛАДКА: ВЫВОДИМ ИНФОРМАЦИЮ О СЛОТЕ
    console.log(`[HOVER] index=${index}, arrayName=${arrayName}`, currentSlotItem);
    
    // ✅ ЕСЛИ ЭТО ЗАГЛУШКА - ИГНОРИРУЕМ
    if (currentSlotItem && currentSlotItem.Data && currentSlotItem.Data.startsWith("placeholder_")) {
        console.log(`[HOVER] Ignoring placeholder at index ${index}`);
        return;
    }
    
    // ✅ ЕСЛИ ЭТО ПРЕДМЕТ, НО НЕ ГЛАВНЫЙ СЛОТ - ИГНОРИРУЕМ
    if (currentSlotItem && currentSlotItem.ItemId && currentSlotItem.ItemId !== 0) {
        console.log(`[HOVER] Item found: ItemId=${currentSlotItem.ItemId}, Index=${currentSlotItem.Index}, isTurn=${currentSlotItem.isTurn}`);
        
        if (currentSlotItem.Index !== index) {
            console.log(`[HOVER] Not main slot (Index ${currentSlotItem.Index} != ${index}), ignoring`);
            return;
        }
    }
    
    if (selectItem.use === stageItem.move && hoverItem === defaulHoverItem) {
        hoverItem = {
            index: index,
            arrayName: arrayName
        };
    }
    
    // ✅ HIGHLIGHT ПРИ ПЕРЕТАСКИВАНИИ
    if (selectItem.use === stageItem.move && selectItem && selectItem.ItemId && selectItem.ItemId !== 0) {
        const slot = event.currentTarget;
        if (!slot) return;
                // ✅ ПРОВЕРКА ДЛЯ ОКНА МОДИФИКАЦИЙ
        if (arrayName === "other" && showModificationsModal && modificationsWeaponItem) {
            const slotRect = slot. getBoundingClientRect();
            const slotSize = slotRect.width;
            
            const canPlace = canPlaceModification(selectItem, index);
            
            if (canPlace) {
                highlight. style.backgroundColor = "rgba(105, 240, 108, 0.3)";
            } else {
                highlight.style.backgroundColor = "rgba(247, 20, 43, 0.3)";
            }
            
            highlight.style.width = `${slotSize}px`;
            highlight. style.height = `${slotSize}px`;
            return; // ✅ ВАЖНО: ВЫХОДИМ, НЕ ПРОДОЛЖАЕМ ДАЛЬШЕ
        }
        const highlight = slot.querySelector('.highlight');
        if (!highlight) return;
        
        const itemConfig = itemsInfo[selectItem.ItemId];
        if (!itemConfig) return;
        
        const itemWidth = selectItem.isTurn ? (itemConfig.Height || 1) : (itemConfig.Width || 1);
        const itemHeight = selectItem.isTurn ? (itemConfig.Width || 1) : (itemConfig.Height || 1);
        
        const slotRect = slot.getBoundingClientRect();
        const slotSize = slotRect.width;
        
        if (arrayName === "accessories" && index === 19) {
            const _sInfoItem = window.getItem(selectItem.ItemId);
            const isWeapon = _sInfoItem.functionType === ItemType.Weapons || 
                            _sInfoItem.functionType === ItemType.MeleeWeapons;
            
            if (isWeapon && selectItem.arrayName === "inventory") {
                highlight.style.backgroundColor = "rgba(105, 240, 108, 0.3)";
            } else {
                highlight.style.backgroundColor = "rgba(247, 20, 43, 0.3)";
            }
            
            highlight.style.width = `${slotSize}px`;
            highlight.style.height = `${slotSize}px`;
            return;
        }
        
        if (arrayName === "accessories") {
            const _sInfoItem = window.getItem(selectItem.ItemId);
            const isWeapon = _sInfoItem.functionType === ItemType.Weapons || 
                            _sInfoItem.functionType === ItemType.MeleeWeapons;
            
            if (isWeapon && (index === clothes.Armors.slotId || index === clothes.Bags.slotId)) {
                highlight.style.backgroundColor = "rgba(247, 20, 43, 0.3)";
                highlight.style.width = `${slotSize}px`;
                highlight.style.height = `${slotSize}px`;
                return;
            }
        }
        
        const canPlace = checkCanPlaceItem(index, arrayName, itemWidth, itemHeight, selectItem.index, selectItem.arrayName);
        
        if (canPlace) {
            highlight.style.backgroundColor = "rgba(105, 240, 108, 0.3)";
        } else {
            highlight.style.backgroundColor = "rgba(247, 20, 43, 0.3)";
        }
        
        highlight.style.width = `${slotSize * itemWidth}px`;
        highlight.style.height = `${slotSize * itemHeight}px`;
    }
    
    // ✅ НЕ ПОКАЗЫВАТЬ TOOLTIP ДЛЯ FASTSLOTS
    if (arrayName === "fastSlots") {
        return;
    }
    
    // ✅ НЕ ПОКАЗЫВАТЬ TOOLTIP ДЛЯ ПУСТЫХ СЛОТОВ
    if (!currentSlotItem || !currentSlotItem.ItemId || currentSlotItem.ItemId === 0) {
        return;
    }
    
    // ✅ НЕ ПОКАЗЫВАТЬ TOOLTIP ВО ВРЕМЯ ПЕРЕТАСКИВАНИЯ
    if (selectItem.use === stageItem.move || selectItem.use === stageItem.useItem) {
        return;
    }
    
    // ✅ ПОКАЗЫВАЕМ TOOLTIP ТОЛЬКО ДЛЯ ГЛАВНОГО СЛОТА ПРЕДМЕТА
    const targetItem = getItemToIndex(index, arrayName);
    if (targetItem && 
        targetItem.ItemId && 
        targetItem.ItemId !== 0 && 
        !targetItem.Data?.startsWith("placeholder_") &&
        targetItem.Index === index) { // ✅ ПРОВЕРЯЕМ, ЧТО ЭТО ГЛАВНЫЙ СЛОТ!
        
        const target = event.target.getBoundingClientRect();
        coords.set({ x: (target.x + target.width/2), y: target.y });
        infoItem = {
            ...targetItem,
            index: index,
            arrayName: arrayName
        };
    }
}
    // ✅ ДОБАВЬТЕ НОВУЮ ФУНКЦИЮ ДЛЯ СЛОТОВ МОДИФИКАЦИЙ
const handleModSlotMouseLeave = (event) => {
    // Сброс highlight
    const slot = event.currentTarget;
    const highlight = slot.querySelector('.highlight');
    if (highlight) {
        highlight.style. backgroundColor = "";
        highlight.style.width = "0";
        highlight.style. height = "0";
    }
    
    // ✅ СБРАСЫВАЕМ hoverItem
    if (hoverItem !== defaulHoverItem && hoverItem.arrayName === "weaponMods") {
        hoverItem = defaulHoverItem;
    }
    
    if (infoItem !== defaulHoverItem && infoItem. arrayName === "weaponMods") {
        infoItem = defaulHoverItem;
    }
}

// Когда выходим из зоны ячейки
const handleSlotMouseLeave = (event) => {
    // Сброс highlight
    const slot = event.currentTarget;
    const highlight = slot.querySelector('.highlight');
    if (highlight) {
        highlight.style.backgroundColor = "";
        highlight.style.width = "0";
        highlight.style.height = "0";
    }
    
    if (hoverItem !== defaulHoverItem) hoverItem = defaulHoverItem;
    if (infoItem !== defaulHoverItem) infoItem = defaulHoverItem;
    if (mouseLeaveSelectedItem === false) mouseLeaveSelectedItem = true;
}
//

const closeOther = () => {
    OtherInfo.Id = otherType.None;
    OtherItemId = 0;
    OtherSqlId = 0;
    OtherInfo.Name = "";
    ItemsData.other = Array(maxSlots.other).fill(clearSlot);
    executeClient ("client.inventory.OtherClose");
}

const handleSlotMouseUp = () => {
    if (selectItem.use === stageItem.info && clickTime >= new Date().getTime()) {
        const index = selectItem.index;
        const arrayName = selectItem.arrayName;
        const _sItem = getItemToIndex (index, arrayName);
        const _sInfoItem = window.getItem (_sItem.ItemId);

        if (selectItem.arrayName === "other" || selectItem.arrayName === "backpack") {
    let MaxStakcItems = 0;
    
    // ✅ ПЕРЕДАЁМ selectItem.arrayName И selectItem.index
    if ((MaxStakcItems = getMaxStakcItems(_sItem, _sInfoItem, selectItem.arrayName, selectItem.index)) == -1) {
        itemNoUse(2);
        return;
    }
    
    if (MaxStakcItems > 0) 
        executeClient("client.gamemenu.inventory.stack", arrayName, index, 2, MaxStakcItems);
    else 
        executeClient("client.gamemenu.inventory.stack", arrayName, index, 2, _sItem.Count);

        } else if (_sInfoItem.functionType === ItemType.Cases && itemIdCaseToId [Number (_sItem.ItemId)] !== undefined) {
            window.router.setPopUp("PopupRoulette", itemIdCaseToId [Number (_sItem.ItemId)]);
        } else if (OtherSqlId && Number (OtherSqlId) === Number (_sItem.SqlId)) {
            closeOther ();
            executeClient ("client.gamemenu.inventory.use", arrayName, index);                
        } else executeClient ("client.gamemenu.inventory.use", arrayName, index);
        itemNoUse (3);
    }
    else if (selectItem.use === stageItem.get || selectItem.use === stageItem.info) {
        //if (getItemsUse (selectItem) !== false && (arrayName === "accessories" || arrayName === "inventory" || arrayName === "backpack" || arrayName === "other" || arrayName === "other")) {
            const index = selectItem.index;

            const arrayName = selectItem.arrayName;

            if (updateItem(index, arrayName, "hover")) {
                selectItem = {
                    ...getItemToIndex(index, arrayName),
                    use: stageItem.info,
                    index: index,
                    arrayName: arrayName
                }
            } else selectItem = defaulSelectItem;
        //} else itemNoUse (4);
    }
}

const setAccessories = () => {
    if (selectItem === defaulSelectItem) return;
    else if (hoverItem === defaulHoverItem && selectItem.use === stageItem.move && selectItem.arrayName !== "accessories") {
        const selectIndex = selectItem.index;
        const selectArrayName = selectItem.arrayName;
        const _sItem = getItemToIndex (selectIndex, selectArrayName);
        const _sInfoItem = window.getItem (_sItem.ItemId);
        if (selectArrayName !== "inventory") {
            itemNoUse (5);
            window.notificationAdd(4, 9, translateText('player1', 'Сначала переложите предмет в собственный инвентарь!'), 3000);
            return;
        }
        
        let hoverIndex = setClothes (_sItem.ItemId);

        if (hoverIndex == -2) return itemNoUse (6);

        hoverIndex = hoverIndex.slotId;
        const hoverArrayName = "accessories";

        const _hItem = getItemToIndex (hoverIndex, hoverArrayName);

        const _hInfoItem = window.getItem (_hItem.ItemId);

        if (isMove (hoverIndex, hoverArrayName, _sItem, _sInfoItem) == -2) {
            itemNoUse (7);
            return;
        }

        executeClient ("client.gamemenu.inventory.move", selectArrayName, selectIndex, hoverArrayName, hoverIndex);

        //{"Name":"Маска","Description":"","Icon":"item-pizza","Type":"Одежда","Model":3887136870,"Stack":1,"functionType":1}
        if (_hItem.ItemId === _sItem.ItemId && Number (_hInfoItem.Stack) > 1) {
            const amount = (_hItem.Count === undefined || _hItem.Count < 2 || !isNumber(_hItem.Count)) ? 1 : _hItem.Count;

            if (Number (_hInfoItem.Stack) >= (amount + _sItem.Count)) {
                _sItem.Count += amount;
                setItem (hoverIndex, hoverArrayName, _sItem);
                setItem (selectIndex, selectArrayName, clearSlot);

            } else {
                _hItem.Count = (amount + _sItem.Count) - _hInfoItem.Stack;
                _sItem.Count = _hInfoItem.Stack;
                setItem (hoverIndex, hoverArrayName, _sItem);
                setItem (selectIndex, selectArrayName, _hItem);
            }
        } else {
            setItem (hoverIndex, hoverArrayName, _sItem);
            setItem (selectIndex, selectArrayName, _hItem);
        }
        itemNoUse (8);
    }
}

const UpdateClothes = (event, componentId, drawableId, textureId) => {
    
    executeClient (event, componentId, drawableId, textureId);
}

const handleMouseDown = (event, index, arrayName) => {
    if (event.which == 1) {
        executeClient("sounds.playInterface", "inventory/keys", 0.005);
        
        const item = getItemToIndex(index, arrayName);

        // ✅ ПРОВЕРКА НА СУЩЕСТВОВАНИЕ ПРЕДМЕТА
        if (!item || !item.ItemId || item.ItemId === 0) return;

        if (((selectItem.use === stageItem.info && 
             (selectItem.index !== index || selectItem.arrayName !== arrayName)) ||
             selectItem.use !== stageItem.info) && 
             item.use) {

            if (arrayName === "other" && OtherInfo.Id === otherType.Nearby) {
                if (OtherInfo.Id === otherType.Nearby && item.remoteId) 
                    return executeClient ("client.gamemenu.inventory.nearby", item.remoteId);
            } 
            else if (arrayName === "other" && OtherInfo.Id === otherType.Tent) {
                   let _infoItem = window.getItem (item.ItemId);

                    const _selectItem = {
                        ...item,
                        use: stageItem.useItem,
                        index: index,
                        arrayName: arrayName,
                        tent: true,
                        info: _infoItem
                    }

                    unHoverAll ();
                    updateItem(index, arrayName, "hover", true);
                    infoItem = defaulHoverItem;
                    
                    coords.set({ x: event.clientX, y: event.clientY });
                    clickTime = new Date().getTime() + 200;
                    StackValue = 1;
                    selectItem = _selectItem;
                    
                    if (item.Count > 1) {
                        rangeslidercreate (item.Count);
                    }

                    return;
                }
                
                if (OtherSqlId && Number (OtherSqlId) === Number (getItemToIndex(index, arrayName).SqlId)) {
                    closeOther ();
                }

                itemNoUse(9);

                const target = event.target.getBoundingClientRect();
                const offsetInElementX = (target.width - (target.right - event.clientX));
                const offsetInElementY = (target.height - (target.bottom - event.clientY));

                coords.set({ x: event.clientX, y: event.clientY });
                clickTime = new Date().getTime() + 1000;
                
                
                selectItem = {
                    ...getItemToIndex(index, arrayName),
                    use: stageItem.get, // ✅ СНАЧАЛА stageItem.get
                    width: target.width,
                    height: target.height,
                    offsetInElementX: offsetInElementX,
                    offsetInElementY: offsetInElementY,
                    clientX: event.clientX,
                    clientY: event.clientY,
                    index: index,
                    arrayName: arrayName,
                }

                mouseLeaveSelectedItem = false;
            }
            
        } else if (event.which == 3 && (arrayName !== "other" || (arrayName === "other" && OtherInfo.Id !== otherType.Nearby && OtherInfo.Id !== otherType.Tent)) && ItemsData[arrayName][index].ItemId != 0 && getItemToIndex(index, arrayName).use) {
            const item = getItemToIndex(index, arrayName);

            const _selectItem = {
                ...item,
                use: stageItem.useItem,
                index: index,
                arrayName: arrayName
            }
            
            if (getItemsUse (selectItem) === false && OtherInfo.Id <= otherType.None && item.Count <= 0 && getDropItem (arrayName, item.ItemId) === false) return;

            unHoverAll ();
            updateItem(index, arrayName, "hover", true);
            infoItem = defaulHoverItem;
            
            coords.set({ x: event.clientX, y: event.clientY });
            selectItem = _selectItem;
        }
    }

function getPositionId(arrayName) {
        const positions = {
            'inventory': '1',
            'other': '7',
            'backpack': '18',
            'accessories': '9',
            'fastSlots': '20'
        };
        return positions[arrayName] || '1';
    }
const handleInputChange = (name, value) => {
    value = Math.round(value.replace(/\D+/g, ""));
    if (value < 0) value = 0;
    else if (value > 9999999) value = 9999999;
    tradeInfo[name] = value;
    
    if (name === "YourMoney") executeClient ("client.gamemenu.inventory.tradeMoney", value);
}

const onBlur = () => {
    if (tradeInfo.YourMoney < 0) {
        window.notificationAdd(4, 9, translateText('player1', 'Сумма не может быть меньше 0.'), 3000);
        tradeInfo.YourMoney = 0;
        //return;
    } else if (tradeInfo.YourMoney > 9999999) {
        window.notificationAdd(4, 9, translateText('player1', 'Сумма не может быть больше $9.999.999.'), 3000);
        tradeInfo.YourMoney = 9999999;
        //return;
    } else if (Number($charMoney) < tradeInfo.YourMoney) {
        tradeInfo.YourMoney = Number($charMoney);
        window.notificationAdd(4, 9, `${translateText('player1', 'Сумма не может быть больше')} ${format("money", tradeInfo.YourMoney)}.`, 3000);
        //return;
    }
    executeClient ("client.gamemenu.inventory.tradeMoney", Math.round(tradeInfo.YourMoney));
}

//Глобальные
const handleGlobalMouseMove = (event) => {
    if (!visible) return;
    if (isDraggingModModal) {
        modModalX = event. clientX - modModalDragOffsetX;
        modModalY = event.clientY - modModalDragOffsetY;
        
        // Ограничиваем выход за пределы экрана
        modModalX = Math. max(0, Math.min(modModalX, window. innerWidth - 400));
        modModalY = Math.max(0, Math. min(modModalY, window.innerHeight - 150));
        return; // ✅ ВАЖНО: НЕ ОБРАБАТЫВАЕМ ДАЛЬШЕ! 
    }
    else if (isMoveBlock) {
        moveBlock[isMoveBlock] = [
            event.clientY - selectItem.offsetInElementY,
            event.clientX - selectItem.offsetInElementX
        ]
        
        if (moveBlock[isMoveBlock][0] + selectItem.height > window.innerHeight) moveBlock[isMoveBlock][0] = window.innerHeight - selectItem.height;
        else if (moveBlock[isMoveBlock][0] < 0) moveBlock[isMoveBlock][0] = 0;

        if (moveBlock[isMoveBlock][1] + selectItem.width > window.innerWidth) moveBlock[isMoveBlock][1] = window.innerWidth - selectItem.width;
        else if (moveBlock[isMoveBlock][1] < 0) moveBlock[isMoveBlock][1] = 0;
    }
    else if (infoItem !== defaulHoverItem) {
        boxInfoLeft = fixOutToCenter ($coords.x, boxItemInfo);
        boxInfoTop = fixOutToTop ($coords.y, boxItemInfo);
    }
    else if ((selectItem.use === stageItem.move && infoItem === defaulHoverItem) || (selectItem.use !== stageItem.useItem && selectItem.use !== stageItem.get && infoItem === defaulHoverItem)) {
        let clientX = event.clientX;
        let clientY = event.clientY;
        
        if (clientY + selectItem.height > window.innerHeight) clientY = window.innerHeight - selectItem.height;
        else if (clientY < 0) clientY = 0;

        if (clientX + selectItem.width > window.innerWidth) clientX = window.innerWidth - selectItem.width;
        else if (clientX < 0) clientX = 0;
        
        coords.set({ x: clientX, y: clientY });

    } else if (selectItem.use === stageItem.get && (selectItem.clientX !== event.clientX || selectItem.clientY !== event.clientY)) {
    unHoverAll();
    
    isDragging = true;
    
    const item = ItemsData[selectItem.arrayName][selectItem.index];
    const itemConfig = itemsInfo[item.ItemId] || {};
    const actualTurn = item.isTurn || item.IsTurn || false;
    
    // ✅ СОХРАНЯЕМ ИСХОДНЫЙ ПОВОРОТ! 
    const originalTurn = actualTurn;
    
    const itemWidth = actualTurn ? (itemConfig.Height || 1) : (itemConfig.Width || 1);
    const itemHeight = actualTurn ? (itemConfig.Width || 1) : (itemConfig.Height || 1);
    
    const maxCols = 6;
    const startX = selectItem.index % maxCols;
    const startY = Math.floor(selectItem.index / maxCols);
    
    console.log(`[DRAG START] Removing item from index ${selectItem.index} (${selectItem.arrayName})`);
    console.log(`[DRAG START] Item size: ${itemWidth}x${itemHeight}, isTurn=${actualTurn}`);
    
    // ✅ УДАЛЯЕМ ПРЕДМЕТ И ВСЕ ЕГО ЗАГЛУШКИ
    if (selectItem.arrayName === "inventory" || selectItem.arrayName === "backpack") {
        for (let dy = 0; dy < itemHeight; dy++) {
            for (let dx = 0; dx < itemWidth; dx++) {
                const clearIndex = (startY + dy) * maxCols + (startX + dx);
                
                if (clearIndex < ItemsData[selectItem.arrayName]. length) {
                    console.log(`[DRAG START] Clearing slot ${clearIndex}`);
                    ItemsData[selectItem.arrayName][clearIndex] = { ... clearSlot };
                }
            }
        }
    } else {
        ItemsData[selectItem.arrayName][selectItem.index] = { ... clearSlot };
    }
    
    // ✅ ОБНОВЛЯЕМ selectItem (СОХРАНЯЕМ ИСХОДНЫЙ ПОВОРОТ!)
    selectItem = {
        ... selectItem,
        use: stageItem.move,
        isTurn: actualTurn,
        IsTurn: actualTurn,
        originalTurn: originalTurn  // ✅ НОВОЕ ПОЛЕ! 
    };
    
    let clientX = event.clientX;
    let clientY = event.clientY;
    
    if (clientY + selectItem.height > window.innerHeight) clientY = window.innerHeight - selectItem.height;
    else if (clientY < 0) clientY = 0;

    if (clientX + selectItem.width > window.innerWidth) clientX = window.innerWidth - selectItem.width;
    else if (clientX < 0) clientX = 0;

    coords.set({ x: clientX, y: clientY });
    
    console.log(`[DRAG START] Drag started!  selectItem.use = stageItem.move`);
}
}

const onUseItem = () => {
    if (selectItem.use === stageItem.useItem) {
        const selectIndex = selectItem.index;
        const selectArrayName = selectItem.arrayName;

        const Item = getItemToIndex(selectIndex, selectArrayName);
        const InfoItem = window.getItem(Item.ItemId);

        // ✅ ПРОВЕРКА НА МОДИФИКАЦИИ ОРУЖИЯ
        if (selectArrayName !== "fastSlots" && 
            (InfoItem.functionType === ItemType.Weapons || InfoItem.functionType === ItemType.MeleeWeapons) && 
            ItemToWeaponHash[Item.ItemId] && 
            wComponents[ItemToWeaponHash[Item.ItemId]] && 
            wComponents[ItemToWeaponHash[Item.ItemId]]. Components) {
             modModalX = cursorX;
    modModalY = cursorY;
            // ✅ ОТКРЫВАЕМ ОКНО МОДИФИКАЦИЙ
            showModificationsModal = true;
            modificationsWeaponItem = Item;
            
            // ✅ ОТПРАВЛЯЕМ НА СЕРВЕР ДЛЯ ЗАГРУЗКИ УСТАНОВЛЕННЫХ МОДИФИКАЦИЙ
            executeClient("client.gamemenu.inventory.use", selectArrayName, selectIndex);
            itemNoUse(10);
            return;
        }

        if (InfoItem.functionType === ItemType.Cases && itemIdCaseToId[Number(Item.ItemId)] !== undefined) {
            window.router.setPopUp("PopupRoulette", itemIdCaseToId[Number(Item. ItemId)]);
        } else if (OtherSqlId && Number(OtherSqlId) === Number(Item.SqlId)) {
            closeOther();
        } else {
            executeClient("client.gamemenu.inventory. use", selectArrayName, selectIndex);
        }
        itemNoUse(10);
    }
}

const onDropItem = () => {
    if (selectItem.use === stageItem.useItem) {
        const selectIndex = selectItem.index;
        const selectArrayName = selectItem.arrayName;

        if (selectItem.Count > 1) {
            ItemStack = 1;
            rangeslidercreate (selectItem.Count);
        } else {
            itemNoUse (11);
            executeClient ("client.gamemenu.inventory.drop", selectArrayName, selectIndex);
        }
    }
}
// ✅ ФУНКЦИЯ ПОИСКА ПЕРВОГО СВОБОДНОГО СЛОТА
function findFreeSlot(arrayName, item) {
    if (!ItemsData[arrayName]) return -1;
    
    const itemConfig = itemsInfo[item.ItemId] || {};
    const itemWidth = itemConfig.Width || 1;
    const itemHeight = itemConfig.Height || 1;
    
    const matrix = createMatrix(arrayName);
    const maxCols = 6;
    const maxRows = arrayName === "inventory" ? 17 : 
                    arrayName === "backpack" ? 8 : 19;
    
    // Ищем первую свободную позицию
    for (let y = 0; y < maxRows; y++) {
        for (let x = 0; x < maxCols; x++) {
            const startIndex = y * maxCols + x;
            
            // Проверяем, поместится ли предмет
            if (checkCanPlaceItem(startIndex, arrayName, itemWidth, itemHeight, -1, "accessories")) {
                return startIndex;
            }
        }
    }
    
    return -1; // Нет свободного места
}
const onTransfer = () => {
    if (selectItem.use === stageItem.useItem) {
        const selectIndex = selectItem.index;
        const selectArrayName = selectItem.arrayName;

        if (selectItem.Count > 1) {
            ItemStack = 2;
            rangeslidercreate(selectItem.Count);
        } else {
            const _sItem = getItemToIndex(selectIndex, selectArrayName);
            const _sInfoItem = window.getItem(_sItem.ItemId);
            
            if (selectItem.arrayName !== "other" && isMove(selectIndex, "other", _sItem, _sInfoItem) == -2) {
                itemNoUse(12);
                return;
            } 
            
            // ✅ ПЕРЕДАЁМ selectArrayName И selectIndex
            else if ((selectItem.arrayName === "other" || selectItem.arrayName === "backpack") && 
                     getMaxStakcItems(_sItem, _sInfoItem, selectArrayName, selectIndex) != 0) {
                itemNoUse(13);
                return;
            }
            
            executeClient("client.gamemenu.inventory.stack", selectArrayName, selectIndex, 2, 1);
            itemNoUse(14);
        }
    }
}

const handleInputStackChange = (value) => {
    value = Math.round(value.replace(/\D+/g, ""));
    if (value < 0) value = 0;
    else if (ItemStack === 0 && value > selectItem.Count - 1) value = selectItem.Count - 1;
    else if (ItemStack !== 0 && value > selectItem.Count) value = selectItem.Count;
    StackValue = value;
}

const onBlurStack = () => {
    if (StackValue < 1) StackValue = 1;
    else if (StackValue > selectItem.Count - 1) StackValue = selectItem.Count - 1;
}

const onStack = () => {
    if (selectItem.use === stageItem.useItem) {
        const selectIndex = selectItem.index;
        const selectArrayName = selectItem.arrayName;
        
        if (ItemStack == 2) {
            const _sItem = getItemToIndex(selectIndex, selectArrayName);
            const _sInfoItem = window.getItem(_sItem.ItemId);
            
            if (selectItem.arrayName !== "other" && isMove(selectIndex, "other", _sItem, _sInfoItem) == -2) {
                itemNoUse(15);
                return;
            }
            
            let MaxStakcItems = 0;
            
            // ✅ ПЕРЕДАЁМ selectArrayName И selectIndex
            if ((selectItem.arrayName === "other" || selectItem.arrayName === "backpack") && 
                (MaxStakcItems = getMaxStakcItems(_sItem, _sInfoItem, selectArrayName, selectIndex)) == -1) {
                itemNoUse(16);
                return;
            }
            
            if (MaxStakcItems > 0) StackValue = MaxStakcItems;
        }
        
        executeClient("client.gamemenu.inventory.stack", selectArrayName, selectIndex, ItemStack, StackValue);
        itemNoUse(17);
    }
}
const onBuy = () => {
    if (selectItem.use === stageItem.useItem) {
        const selectIndex = selectItem.index;
        const selectArrayName = selectItem.arrayName;
        executeClient ("client.gamemenu.inventory.buy", selectArrayName, selectIndex, StackValue);
        itemNoUse (17);
    }
}

const setClothes = (ItemId) => {
    if (ItemId === 12 || ItemId === 15) {
        return clothes["Bags"];
    } else if (ItemId === -15) {
        return clothes["Watches"];
    }
    let returnSlotId = -2;
    clothesId.forEach((item) => {
        if (clothes[item].itemId === ItemId) returnSlotId = clothes[item];
    });
    return returnSlotId;
}

const isMove = (index, arrayName, item, itemInfo) => {
    if (arrayName === "fastSlots") {
        window.notificationAdd(4, 9, 'Нельзя перемещать предметы в быстрые слоты!', 3000);
        return -2;
    }
        // ✅ ПРОВЕРКА ДЛЯ ОКНА МОДИФИКАЦИЙ
    if (arrayName === "other" && showModificationsModal && modificationsWeaponItem) {
        if (item.ItemId !== 0 && itemInfo.functionType !== ItemType.Modification) {
            window.notificationAdd(4, 9, 'В этот слот можно положить только модификацию!', 3000);
            return -2;
        }
        
        if (item.ItemId !== 0 && !canPlaceModification(item, index)) {
            window.notificationAdd(4, 9, 'Эта модификация не подходит для данного слота!', 3000);
            return -2;
        }
    }
    const OtherInfoId = OtherInfo.Id;
    let dataParse;
    if (item.ItemId != 0 && item.Data.split("_").length) dataParse = item.Data.split("_");

    if (arrayName === "accessories" && index === 8 && (getItemToIndex (8, "accessories").ItemId === 12 || getItemToIndex (8, "accessories").ItemId === 15))
    {
        window.notificationAdd(4, 9, translateText('player1', 'Это действие недоступно'), 3000);
        return -2;
    }
    else if (arrayName === "accessories" && item.ItemId != 0 && item.ItemId != -9 && item.ItemId != -5 && item.ItemId != -1 && itemInfo.functionType === ItemType.Clothes && dataParse && dataParse.length >= 2 && Bool(dataParse[2]) !== Bool($charGender)) {
        window.notificationAdd(4, 9, `Это ${Bool(dataParse[2]) ? translateText('player1', 'мужская') : translateText('player1', 'женская')} ${translateText('player1', 'одежда')}`, 3000);
        return -2;
    } else if (arrayName === "accessories" && item.ItemId != 0 && (clothes[clothesId[index]].itemId !== item.ItemId || (index === 8 && item.ItemId !== 12 && item.ItemId !== 15) || (index === 11 && item.ItemId !== -15))) {
        const _id = setClothes (item.ItemId);
        if (_id == -2) {
            window.notificationAdd(4, 9, translateText('player1', 'Данный слот не доступен!'), 3000);
            return -2;
        }
        //else if (_id.itemId == item.ItemId) return -2;
        return _id.slotId;
    } else if (arrayName === "accessories" && item.ItemId != 0 && index === 1 && !$charGender && (item.Data === "127_0_True" || item.Data === "127_2_True")) {
        window.notificationAdd(4, 9, translateText('player1', 'Вы не можете надеть этот уникальный аксессуар'), 3000);
        return -2;
    } else if (arrayName === "other" && OtherInfoId === otherType.Safe && item.ItemId != 0 && itemInfo.functionType !== ItemType.Weapons &&  itemInfo.functionType !== ItemType.MeleeWeapons) {//Оружейный сейф
        window.notificationAdd(4, 9, translateText('player1', 'В данный сейф можно положить только оружие'), 3000);
        return -2;
    } else if (arrayName === "other" && OtherInfoId === otherType.Chiffonier && item.ItemId != 0 && itemInfo.functionType !== ItemType.Clothes) {//Cейф под одежду
        window.notificationAdd(4, 9, translateText('player1', 'В данный шкаф можно положить только одежду'), 3000);
        return -2;
    } else if (arrayName === "other" && OtherInfoId === otherType.Wardrobe && item.ItemId != 0 && (item.ItemId == 109 || item.ItemId == 12 || item.ItemId == 15 || item.ItemId == 19 || item.ItemId == 40 || item.ItemId == 41 || itemInfo.functionType === ItemType.Clothes || itemInfo.functionType === ItemType.Weapons)) {
        window.notificationAdd(4, 9, translateText('player1', 'Эта вещь не предназначена для этого шкафа'), 3000);
        return -2;
    } else if (arrayName === "other" && OtherInfoId === otherType.Fraction && item.ItemId != 0 && itemInfo.functionType !== ItemType.Weapons && itemInfo.functionType !== ItemType.Ammo && item.ItemId != -9) {
        window.notificationAdd(4, 9, translateText('player1', 'На склад можно положить только оружие или патроны'), 3000);
        return -2;
    } else if ((arrayName === "inventory" || arrayName === "backpack") && OtherInfoId === otherType.Fraction && item.ItemId != 0) {
    let checkItem = getItemToIndex(index, arrayName);
    if (checkItem.ItemId != 0) {
        window.notificationAdd(4, 9, 'Переложить можно только в пустой слот', 3000);
        return -2;
    }

    // ✅ ПЕРЕДАЁМ arrayName И index
    if (getMaxStakcItems(item, itemInfo, arrayName, index) === -1) return -2;

        

        /*let success = true;
        let count = item.Count;
        ItemsData[arrayName].forEach((i) => {
            if (success && item.ItemId == i.ItemId && item.SqlId != i.SqlId) {
                count += i.Count;
                if (count > itemInfo.Stack) {
                    success = false;
                    window.notificationAdd(4, 9, translateText('player1', 'Недостаточно места для такого количества'), 3000);
                }
            }
        })*/

    } else if (arrayName === "other" && OtherInfoId === otherType.Organization && item.ItemId != 0 && itemInfo.functionType !== ItemType.Weapons &&  itemInfo.functionType !== ItemType.Ammo && item.ItemId != -9) {
        window.notificationAdd(4, 9, translateText('player1', 'На склад можно положить только оружие или патроны'), 3000);
        return -2;
    } else if (arrayName === "other" && OtherInfoId === otherType.Key && item.ItemId != 0 && item.ItemId != 19) {
        window.notificationAdd(4, 9, translateText('player1', 'Только ключи от т/c'), 3000);
        return -2;
    } else if (arrayName === "other" && OtherInfoId === otherType.Nearby) {
        window.notificationAdd(4, 9, translateText('player1', 'Данное действие недоступно!'), 3000);
        return -2;
    } else if (arrayName === "other" && OtherInfoId === otherType.Tent && !OtherInfo.IsMyTent) {
        window.notificationAdd(4, 9, translateText('player1', 'Данное действие недоступно!'), 3000);
        return -2; 
    } else if (arrayName === "other" && OtherInfoId === otherType.Tent && OtherInfo.IsMyTent && item.ItemId != 0 && (item.ItemId == 19)) {
        window.notificationAdd(4, 9, translateText('player1', 'Нельзя продавать этот предмет.'), 3000);
        return -2;
    } else if (OtherInfoId === otherType.Vehicle && isArmyCar === true && item.ItemId != 0 && (item.ItemId == 237 || item.ItemId == 238 || item.ItemId == 239 || item.ItemId == 240 || item.ItemId == 241 || item.ItemId == 242)) {
        window.notificationAdd(4, 9, translateText('player1', 'Нельзя перекладывать этот предмет.'), 3000);
        return -2;
    } else if ((arrayName === "inventory" || arrayName === "accessories") && item.ItemId == -9 && isInVehicle === true) {
        window.notificationAdd(4, 9, translateText('player1', 'Невозможно снять бронежилет находясь в транспорте.'), 3000);
        executeClient ("checkClientSpecialVars");
        return -2;
    } else if (arrayName === "other" && OtherInfoId === otherType.wComponents && item.ItemId != 0) {            
        const weaponHash = ItemToWeaponHash [OtherItemId];
        let success = 0;
        if (!(item.ItemId >= 206 && 217 >= item.ItemId)) {
            window.notificationAdd(4, 9, translateText('player1', 'Этот слот предназначен только для модификаций'), 3000);
            return -2;
        }
        else if (item.ItemId >= 206 && 217 >= item.ItemId) {
            if (Number (dataParse[0]) == Number (weaponHash)) {
                success = 1;
            }
            if (wComponents [weaponHash] && wComponents [weaponHash].Components) {
                let componentHash;
                let typeId = -1;
                //Сначала проверяем есть ли он в списке компонентов
                for (componentHash in wComponents [weaponHash].Components) {
                    if (Number (dataParse[1]) == Number (componentHash)) {
                        success = 1;
                        typeId = wComponents [weaponHash].Components [dataParse[1]].Type;
                    }
                }
                //Затем проверяемм есть ли уже такой тип 
                if (success == 1) {
                    ItemsData["other"].forEach((i) => {
                        if (success != -1 && i.ItemId != 0 && i.Data.split("_").length) {
                            let dParse = i.Data.split("_");                    
                            if (wComponents [weaponHash].Components [dParse[1]] && wComponents [weaponHash].Components [dParse[1]].Type == typeId) {
                                success = -1;
                            }
                        }
                    })
                }
            }
        }
        if (success == 1) return -1;
        else if (success == -1) window.notificationAdd(4, 9, translateText('player1', 'Модификация такого типа уже установлена на данномм оружие!'), 3000);
        else window.notificationAdd(4, 9, translateText('player1', 'Данная модификация не подходит к этому оружию!'), 3000);
        return -2;
    } else if (arrayName === "fastSlots" && index !== 4 &&
                item.ItemId != 0 &&
                itemInfo.functionType !== ItemType.Weapons &&
                itemInfo.functionType !== ItemType.MeleeWeapons &&
                item.ItemId != 6 &&
                item.ItemId != 7 &&
                item.ItemId != 8 &&
                item.ItemId != 3 &&
                item.ItemId != 5 &&
                item.ItemId != 9 &&
                item.ItemId != 10 &&
                item.ItemId != 1 &&
                item.ItemId != 280 &&
                item.ItemId != 225 &&
                item.ItemId != 226 &&
                item.ItemId != 227 &&
                item.ItemId != 228 &&
                item.ItemId != 229 &&
                item.ItemId != 230 &&
                item.ItemId != 231 &&
                item.ItemId != 232 &&
                item.ItemId != 233 &&
                item.ItemId != 388 &&
                item.ItemId != 389 &&
                item.ItemId != ItemId.VehicleNumber) {
        window.notificationAdd(4, 9, `${translateText('player1', 'Вы не можете положить сюда')} ${itemInfo.Name}`, 3000);
        return -2;
    }/* else if (arrayName === "other" && !getMaxStakcItems (item, itemInfo)) {
        window.notificationAdd(4, 9, `Вы не можете больше вместить ${itemInfo.Name}`, 3000);
        return -2;
    }*/
    else if ((arrayName === "backpack" && (item.ItemId == -5 || item.ItemId == 12)) || (arrayName === "other" && ((item.ItemId === -5 && Number(item.Data.split("_")[0]) == 40) || item.ItemId == 12))) 
    {
        window.notificationAdd(4, 9, `${translateText('player1', 'Вы не можете положить сюда')} ${itemInfo.Name}`, 3000);
        return -2;
    }
    else if ((arrayName !== "backpack" && arrayName !== "other") && (item.ItemId == 13 || item.ItemId == 1)) 
    {
        let success = true;
        let count = 0;
        ItemsData[arrayName].forEach((i) => {
            if (success && item.ItemId == i.ItemId && item.SqlId != i.SqlId) {
                count += i.Count;
                if (count >= itemInfo.Stack) {
                    success = false;
                    window.notificationAdd(4, 9, `${translateText('player1', 'Вы не можете положить')} ${itemInfo.Name}`, 3000);
                }
            }
        })
        if (success) return -1;
        else return -2;
    } else if (arrayName === "other" && (OtherInfoId === otherType.Storage || OtherInfoId === otherType.Case) && item.ItemId != 0) {
        window.notificationAdd(4, 9, translateText('player1', 'Данный склад предназначен лишь для изъятия предметов из него.'), 3000);
        return -2;
    }
    return -1;
}

const maxItemCount = 3;
const getMaxStakcItems = (item, itemInfo, ignoreArrayName = null, ignoreIndex = null) => {
    if (item.ItemId == 0) return true;
    
    // ✅ 1. ПРОВЕРКА ВЕСА
    const maxWeight = 50.0; // Максимальный вес инвентаря в КГ
    let currentWeight = 0;

    // Подсчитываем текущий вес инвентаря
    for (let arrayName in ItemsData) {
        if (arrayName === "inventory") {
            ItemsData[arrayName].forEach((i, idx) => {
                if (i.ItemId == 0) return;
                
                // ✅ ИГНОРИРУЕМ ПЕРЕМЕЩАЕМЫЙ ПРЕДМЕТ
                if (arrayName === ignoreArrayName && idx === ignoreIndex) return;
                
                const iInfo = itemsInfo[i.ItemId];
                if (iInfo) {
                    // ✅ КОНВЕРТИРУЕМ ГРАММЫ В КГ
                    currentWeight += (iInfo.Weight / 1000) * i.Count;
                }
            });
        }
    }

    // Вес добавляемого предмета (в КГ)
    const itemWeight = (itemInfo.Weight / 1000) * item.Count;

    // Проверяем, не превышен ли вес
    if (currentWeight + itemWeight > maxWeight) {
        window.notificationAdd(4, 9, 
            `Слишком тяжело! (${(currentWeight + itemWeight).toFixed(1)}/${maxWeight} кг)`, 
            3000);
        return -1;
    }

    // ✅ 2. ПРОВЕРКА СВОБОДНЫХ КЛЕТОК
    const maxCols = 5;
    const maxRows = 7;
    const matrix = Array.from({ length: maxRows }, () => Array(maxCols).fill(false));

    // Заполняем матрицу занятыми клетками
    for (let arrayName in ItemsData) {
        if (arrayName === "inventory") {
            ItemsData[arrayName].forEach((i, idx) => {
                if (i.ItemId == 0) return;
                
                // ✅ ИГНОРИРУЕМ ПЕРЕМЕЩАЕМЫЙ ПРЕДМЕТ
                if (arrayName === ignoreArrayName && idx === ignoreIndex) return;
                
                const iInfo = itemsInfo[i.ItemId];
                if (!iInfo) return;

                const x = idx % maxCols;
                const y = Math.floor(idx / maxCols);
                const width = iInfo.Width;
                const height = iInfo.Height;

                // Помечаем занятые клетки
                for (let dy = 0; dy < height; dy++) {
                    for (let dx = 0; dx < width; dx++) {
                        const checkY = y + dy;
                        const checkX = x + dx;
                        if (checkY < maxRows && checkX < maxCols) {
                            matrix[checkY][checkX] = true;
                        }
                    }
                }
            });
        }
    }

    // Проверяем, есть ли место для нового предмета
    const itemWidth = itemInfo.Width;
    const itemHeight = itemInfo.Height;
    let canPlace = false;

    for (let y = 0; y < maxRows; y++) {
        for (let x = 0; x < maxCols; x++) {
            // Проверяем, поместится ли предмет начиная с этой позиции
            if (x + itemWidth <= maxCols && y + itemHeight <= maxRows) {
                let free = true;
                for (let dy = 0; dy < itemHeight; dy++) {
                    for (let dx = 0; dx < itemWidth; dx++) {
                        if (matrix[y + dy][x + dx]) {
                            free = false;
                            break;
                        }
                    }
                    if (!free) break;
                }
                if (free) {
                    canPlace = true;
                    break;
                }
            }
        }
        if (canPlace) break;
    }

    if (!canPlace) {
        window.notificationAdd(4, 9, "Недостаточно места в инвентаре", 3000);
        return -1;
    }

    // ✅ ВСЁ ОК - МОЖНО ДОБАВИТЬ
    return 0;
}
// ✅ ФУНКЦИЯ ВОЗВРАТА ПРЕДМЕТА НА ИСХОДНОЕ МЕСТО
function returnItemToSource() {
    if (!selectItem || ! selectItem.ItemId || selectItem.ItemId === 0 || !selectItem.arrayName || selectItem.index === undefined) {
        console.log('[RETURN] Nothing to return');
        return;
    }
    if (selectItem.arrayName === "weaponMods") {
        console.log(`[RETURN] Returning modification to slot ${selectItem. index}`);
        weaponModifications[selectItem.index] = {
            ...selectItem,
            Index: selectItem.index
        };
        return;
    }
    
    console.log(`[RETURN] Returning item to ${selectItem.arrayName}[${selectItem.index}]`);
    
    // ✅ ИСПОЛЬЗУЕМ ИСХОДНЫЙ ПОВОРОТ! 
    const returnTurn = selectItem.originalTurn !== undefined ? selectItem.originalTurn : selectItem.isTurn;
    
    console.log(`[RETURN] Original turn: ${selectItem.originalTurn}, Current turn: ${selectItem.isTurn}, Using: ${returnTurn}`);
    
    const itemConfig = itemsInfo[selectItem.ItemId] || {};
    const itemWidth = returnTurn ? (itemConfig. Height || 1) : (itemConfig.Width || 1);
    const itemHeight = returnTurn ? (itemConfig.Width || 1) : (itemConfig.Height || 1);
    
    // ✅ ВОЗВРАЩАЕМ ПРЕДМЕТ С ИСХОДНЫМ ПОВОРОТОМ! 
    setItem(selectItem. index, selectItem.arrayName, {
        ...selectItem,
        isTurn: returnTurn,
        IsTurn: returnTurn,
        isDragging: false
    });
    
    // ✅ ВОССТАНАВЛИВАЕМ ЗАГЛУШКИ С ИСХОДНЫМ ПОВОРОТОМ! 
    if (selectItem.arrayName === "inventory" || selectItem.arrayName === "backpack") {
        const maxCols = 6;
        const startX = selectItem.index % maxCols;
        const startY = Math.floor(selectItem.index / maxCols);
        
        for (let dy = 0; dy < itemHeight; dy++) {
            for (let dx = 0; dx < itemWidth; dx++) {
                if (dx === 0 && dy === 0) continue;
                
                const placeholderIndex = (startY + dy) * maxCols + (startX + dx);
                if (placeholderIndex < ItemsData[selectItem.arrayName]. length) {
                    ItemsData[selectItem.arrayName][placeholderIndex] = {
                        ...clearSlot,
                        Data: `placeholder_${selectItem.index}`
                    };
                }
            }
        }
    }
    
    console.log('[RETURN] Item returned successfully with original rotation');
}
const handleGlobalMouseUp = (event) => {
    if (! visible) return;
    else if (event.which !== 1) return;
    
    if (isDraggingModModal) {
        isDraggingModModal = false;
        return;
    }
    
    if (isMoveBlock) {
        selectItem = defaulSelectItem;
        isMoveBlock = false;
        return;
    }
    
    // ✅ ОБРАБОТКА DROP ИЗ ОКНА МОДИФИКАЦИЙ (weaponMods) В ИНВЕНТАРЬ
if (selectItem. use === stageItem.move && selectItem.arrayName === "weaponMods") {
    console.log('[DRAG END] Dropping from weaponMods');
    
    // ✅ ЕСЛИ БРОСИЛИ НА ИНВЕНТАРЬ
    if (hoverItem !== defaulHoverItem && hoverItem. arrayName === "inventory") {
        const hoverIndex = hoverItem.index;
        
        const itemConfig = itemsInfo[selectItem.ItemId] || {};
        const itemWidth = itemConfig.Width || 1;
        const itemHeight = itemConfig. Height || 1;
        
        if (checkCanPlaceItem(hoverIndex, "inventory", itemWidth, itemHeight, -1, null)) {
            console.log(`[REMOVEMOD] Sending removemod: modSlot=${selectItem.index}, targetIndex=${hoverIndex}`);
            
            // ✅ ОТПРАВЛЯЕМ КОМАНДУ СНЯТИЯ МОДИФИКАЦИИ
            executeClient("client.gamemenu.inventory.removemod", selectItem.index, hoverIndex);
            
            // ✅ ВИЗУАЛЬНО РАЗМЕЩАЕМ В ИНВЕНТАРЕ (сервер подтвердит)
            ItemsData["inventory"][hoverIndex] = {
                ... selectItem,
                Index: hoverIndex,
                index: hoverIndex,
                isTurn: false,
                isDragging: false,
                arrayName: "inventory"
            };
            
            // ✅ ПЕРЕСОЗДАЁМ МАТРИЦУ
            rebuildMatrixAtomic("inventory");
            
            executeClient("sounds.playInterface", "inventory/drag_drop", 0.05);
            
            // ✅ СБРАСЫВАЕМ
            selectItem = defaulSelectItem;
            isDragging = false;
            hoverItem = defaulHoverItem;
            infoItem = defaulHoverItem;
            
            document.querySelectorAll('.highlight').forEach(el => {
                el.style.backgroundColor = "";
                el.style.width = "0";
                el.style.height = "0";
            });
            
            return;
        } else {
            window.notificationAdd(4, 9, "Недостаточно места в инвентаре", 3000);
        }
    }
    
    // ✅ ВОЗВРАЩАЕМ МОДИФИКАЦИЮ ОБРАТНО В СЛОТ (ЕСЛИ НЕ БРОСИЛИ НА ИНВЕНТАРЬ)
    console.log('[DRAG END] Returning modification to slot', selectItem.index);
    const returnIndex = selectItem.index;
    weaponModifications[returnIndex] = {
        ...selectItem,
        Index: returnIndex,
        arrayName: "weaponMods"
    };
    
    // ✅ СБРАСЫВАЕМ
    selectItem = defaulSelectItem;
    isDragging = false;
    hoverItem = defaulHoverItem;
    infoItem = defaulHoverItem;
    
    document. querySelectorAll('. highlight').forEach(el => {
        el.style.backgroundColor = "";
        el. style.width = "0";
        el.style.height = "0";
    });
    
    return;
}
    else if (event.which !== 1) return;
     if (isDraggingModModal) {
        isDraggingModModal = false;
        return; // ✅ ВАЖНО: НЕ ОБРАБАТЫВАЕМ ДАЛЬШЕ!
    }
    if (isMoveBlock) {
        selectItem = defaulSelectItem;
        isMoveBlock = false;
        return;
    }
    // ✅ НОВОЕ: ОБРАБОТКА DROP ИЗ ИНВЕНТАРЯ В СЛОТ МОДИФИКАЦИЙ (weaponMods)
    // ✅ НОВОЕ: ОБРАБОТКА DROP ИЗ ИНВЕНТАРЯ В СЛОТ МОДИФИКАЦИЙ (weaponMods)
if (selectItem.use === stageItem.move && 
    hoverItem !== defaulHoverItem && 
    hoverItem. arrayName === "weaponMods" &&
    showModificationsModal && 
    modificationsWeaponItem) {
    
    console.log('[DRAG END] Installing modification to slot', hoverItem.index);
    
    const slotIndex = hoverItem.index;
    
    // ✅ ПРОВЕРЯЕМ, МОЖНО ЛИ УСТАНОВИТЬ МОДИФИКАЦИЮ
    if (! canPlaceModification(selectItem, slotIndex)) {
        window.notificationAdd(4, 9, "Эта модификация не подходит для данного слота!", 3000);
        returnItemToSource();
        
        selectItem = defaulSelectItem;
        isDragging = false;
        hoverItem = defaulHoverItem;
        infoItem = defaulHoverItem;
        
        document.querySelectorAll('.highlight').forEach(el => {
            el.style.backgroundColor = "";
            el.style.width = "0";
            el.style.height = "0";
        });
        
        return;
    }
    
    // ✅ ПОЛУЧАЕМ ИНДЕКС ОРУЖИЯ В ИНВЕНТАРЕ
    const weaponIndex = modificationsWeaponItem.Index;
    
    console.log('[INSTALLMOD] Sending to server:', {
        fromArrayName: selectItem.arrayName,
        fromIndex: selectItem.index,
        weaponIndex: weaponIndex,
        modSlotIndex: slotIndex
    });
    
    // ✅ ОТПРАВЛЯЕМ СПЕЦИАЛЬНУЮ КОМАНДУ ДЛЯ УСТАНОВКИ МОДИФИКАЦИИ
    executeClient("client.gamemenu.inventory.installmod", 
        selectItem. arrayName,  // откуда берём модификацию ("inventory")
        selectItem. index,      // индекс модификации в инвентаре
        weaponIndex,           // индекс оружия
        slotIndex              // слот модификации (0-4)
    );
    
    // ✅ ВИЗУАЛЬНО РАЗМЕЩАЕМ (сервер потом подтвердит)
    weaponModifications[slotIndex] = {
        ... selectItem,
        Index: slotIndex,
        arrayName: "weaponMods"
    };
    
    executeClient("sounds.playInterface", "inventory/drag_drop", 0.05);
    
    // ✅ СБРАСЫВАЕМ
    selectItem = defaulSelectItem;
    isDragging = false;
    hoverItem = defaulHoverItem;
    infoItem = defaulHoverItem;
    
    document. querySelectorAll('.highlight').forEach(el => {
        el.style.backgroundColor = "";
        el. style.width = "0";
        el.style.height = "0";
    });
    
    return;
}
    // ✅ БЛОКИРУЕМ DROP НА FASTSLOTS
    if (selectItem.use === stageItem.move && 
        hoverItem !== defaulHoverItem && 
        hoverItem.arrayName === "fastSlots") {
        window.notificationAdd(4, 9, 'Нельзя перемещать предметы в быстрые слоты!', 3000);
        
        // ✅ ВОЗВРАЩАЕМ ПРЕДМЕТ НА МЕСТО!
        returnItemToSource();
        itemNoUse(46);
        return;
    }
    
    // ✅ ПРОВЕРКА КЛИКА НА СЛОТ "АКТИВНЫЕ ПРЕДМЕТЫ"
    if (selectItem.use === stageItem.move && 
        hoverItem !== defaulHoverItem && 
        hoverItem.arrayName === "accessories" && 
        hoverItem.index === 19) {
        
        const _sInfoItem = window.getItem(selectItem.ItemId);
        const isWeapon = _sInfoItem.functionType === ItemType.Weapons || 
                        _sInfoItem.functionType === ItemType.MeleeWeapons;
        
        if (isWeapon && selectItem.arrayName === "inventory") {
            executeClient("client.gamemenu.inventory.takehands", selectItem.arrayName, selectItem.index);
            itemNoUse(44);
            return;
        } else {
            window.notificationAdd(4, 9, "Сюда можно положить только оружие из инвентаря!", 3000);
            
            // ✅ ВОЗВРАЩАЕМ ПРЕДМЕТ НА МЕСТО!
            returnItemToSource();
            itemNoUse(45);
            return;
        }
    }
    
    else if (selectItem.use === stageItem.move) {
        // ✅ DROP ВНЕ ИНВЕНТАРЯ
        if (hoverItem === defaulHoverItem && 
            tradeInfo.Active === false && 
            selectItem && 
            selectItem !== defaulSelectItem && 
            selectItem.ItemId && 
            selectItem.ItemId !== 0 &&
            getDropItem(selectItem.arrayName, selectItem.ItemId) !== false) {
            
            const selectIndex = selectItem.index;
            const selectArrayName = selectItem.arrayName;
            
            if (mouseLeaveSelectedItem === true && mainInventoryArea === false) {
                executeClient("client.gamemenu.inventory.drop", selectArrayName, selectIndex);
            } else {
                // ✅ НЕ ВЫБРОСИЛИ - ВОЗВРАЩАЕМ!
                returnItemToSource();
            }
            itemNoUse(18);
        } 
        else if (hoverItem !== defaulHoverItem && 
            (hoverItem.index !== selectItem.index || hoverItem.arrayName !== selectItem.arrayName)) {
            
            // ✅ ПРОВЕРКА НА СУЩЕСТВОВАНИЕ ПРЕДМЕТА
            if (!selectItem || !selectItem.ItemId || selectItem.ItemId === 0) {
                itemNoUse(36);
                return;
            }
    
            let hoverIndex = hoverItem.index;
            const hoverArrayName = hoverItem.arrayName;
            
            let _hItem = getItemToIndex(hoverIndex, hoverArrayName);
            let _hInfoItem = window.getItem(_hItem.ItemId);
            
            let selectIndex = selectItem.index;
            const selectArrayName = selectItem.arrayName;
            let _sItem = selectItem; // ✅ ИСПОЛЬЗУЕМ selectItem!
            let _sInfoItem = window.getItem(_sItem.ItemId);

            const itemConfig = itemsInfo[selectItem.ItemId] || {};
            const itemWidth = selectItem.isTurn ? (itemConfig.Height || 1) : (itemConfig.Width || 1);
            const itemHeight = selectItem.isTurn ? (itemConfig.Width || 1) : (itemConfig.Height || 1);

console.log('[DRAG END] Checking placement:');
console.log('- selectItem:', selectItem);
console. log('- hoverIndex:', hoverIndex);
console. log('- hoverArrayName:', hoverArrayName);
console.log('- itemWidth:', itemWidth);
console. log('- itemHeight:', itemHeight);
console.log('- _hItem:', _hItem);
            // ✅ ПРОВЕРЯЕМ МОЖНО ЛИ ПОЛОЖИТЬ selectItem В hoverIndex
if (! checkCanPlaceItem(hoverIndex, hoverArrayName, itemWidth, itemHeight, selectItem. index, selectItem.arrayName)) {
    console.log('[DRAG END] Cannot place selectItem - returning to source');
    returnItemToSource();
    window.notificationAdd(4, 9, "Недостаточно места для размещения предмета", 3000);
    itemNoUse(37);
    return;
}

// ✅ ЕСЛИ ЕСТЬ hoverItem - ПРОВЕРЯЕМ, МОЖНО ЛИ ЕГО ПОЛОЖИТЬ В selectIndex
if (_hItem && _hItem.ItemId && _hItem.ItemId !== 0) {
    const hoverItemConfig = itemsInfo[_hItem.ItemId] || {};
    const hoverTurn = _hItem.isTurn || _hItem.IsTurn || false;
    const hoverWidth = hoverTurn ? (hoverItemConfig.Height || 1) : (hoverItemConfig.Width || 1);
    const hoverHeight = hoverTurn ?  (hoverItemConfig.Width || 1) : (hoverItemConfig.Height || 1);
    
    if (! checkCanPlaceItem(selectItem.index, selectItem.arrayName, hoverWidth, hoverHeight, hoverIndex, hoverArrayName)) {
        console. log('[DRAG END] Cannot place hoverItem - returning to source');
        returnItemToSource();
        window.notificationAdd(4, 9, "Недостаточно места для обмена предметов", 3000);
        itemNoUse(47);
        return;
    }
}
            
            // ✅ ВСЕ ОСТАЛЬНЫЕ ПРОВЕРКИ (isMove, MaxStakcItems и т.д.)
            let returnMove = -1;
            if (!_hItem.use || hoverArrayName === "with_trade") {
                returnItemToSource();
                window.notificationAdd(4, 9, translateText('player1', 'Данный слот не доступен!'), 3000);
                itemNoUse(19);
                return;
            } else if ((hoverArrayName === "accessories" || hoverArrayName === "fastSlots") && selectArrayName !== "inventory") {
                returnItemToSource();
                window.notificationAdd(4, 9, translateText('player1', 'Сначала переложите предмет в собственный инвентарь!'), 3000);
                itemNoUse(20);
                return;
            } else if ((selectArrayName === "accessories" || selectArrayName === "fastSlots") && hoverArrayName !== "inventory") {
                returnItemToSource();
                window.notificationAdd(4, 9, translateText('player1', 'Сначала переложите предмет в собственный инвентарь!'), 3000);
                itemNoUse(21);
                return;
            } else if (hoverArrayName === "other" && OtherInfo.Id === otherType.Nearby) {
                executeClient("client.gamemenu.inventory.drop", selectArrayName, selectIndex);   
                itemNoUse(18);
                return;
            }          
            
            if (hoverArrayName !== selectArrayName && (returnMove = isMove(hoverIndex, hoverArrayName, _sItem, _sInfoItem)) == -2) {
                returnItemToSource();
                itemNoUse(22);
                return;
            }
            
            if (hoverArrayName === "other" && OtherInfo.Id === otherType.Tent && OtherInfo.IsMyTent) {
                executeClient("client.gamemenu.inventory.stack", selectArrayName, selectIndex, 2, _sItem.Count);
                itemNoUse(18);
                return;
            }  

            if (returnMove !== -1) {
                hoverIndex = returnMove;
                _hItem = getItemToIndex(hoverIndex, hoverArrayName);
                _hInfoItem = window.getItem(_hItem.ItemId);
                if (isMove(hoverIndex, hoverArrayName, _sItem, _sInfoItem) == -2) {
                    returnItemToSource();
                    itemNoUse(23);
                    return;
                }
            }
            returnMove = -1;
            
            if (hoverArrayName !== selectArrayName && (returnMove = isMove(selectIndex, selectArrayName, _hItem, _hInfoItem)) == -2) {
                returnItemToSource();
                itemNoUse(24);
                return;
            }

            if (returnMove !== -1) {
                selectIndex = returnMove;
                _sItem = getItemToIndex(selectIndex, selectArrayName);
                _sInfoItem = window.getItem(_sItem.ItemId);
            
                if (isMove(selectIndex, selectArrayName, _hItem, _hInfoItem) == -2) {
                    returnItemToSource();
                    itemNoUse(25);
                    return;
                }
            }

            let MaxStakcItems = 0;
            if ((hoverArrayName !== "other" && hoverArrayName !== "backpack") && 
                (selectArrayName === "other" || selectArrayName === "backpack") && 
                ![0, 237, 238, 239, 240, 241, 242, 245, 246, 247].includes(_sItem.ItemId)) {
                
                MaxStakcItems = getMaxStakcItems(_sItem, _sInfoItem, selectArrayName, selectIndex);
                
                if (MaxStakcItems == -1) {
                    returnItemToSource();
                    itemNoUse(26);
                    return;
                }
            }
            
            if (MaxStakcItems > 0) {
                if (_hItem.ItemId === _sItem.ItemId || _hItem.ItemId === 0) {
                    executeClient("client.gamemenu.inventory.move.stack", selectArrayName, selectIndex, hoverArrayName, hoverIndex, MaxStakcItems);
                    if (_hItem.ItemId === _sItem.ItemId) {
                        _hItem.Count += MaxStakcItems;
                        _sItem.Count -= MaxStakcItems;
                        setItem(hoverIndex, hoverArrayName, _hItem);
                        setItem(selectIndex, selectArrayName, _sItem);
                        executeClient("sounds.playInterface", "inventory/drag_drop", 0.05);
                    } else {
                        _sItem.Count -= MaxStakcItems;
                        setItem(selectIndex, selectArrayName, _sItem);
                        _hItem = {..._sItem};
                        _hItem.Count = MaxStakcItems;
                        setItem(hoverIndex, hoverArrayName, _hItem);
                        executeClient("sounds.playInterface", "inventory/drag_drop", 0.05);
                    }
                } else {
                    window.notificationAdd(4, 9, `${translateText('player1', 'Нет места для')} ${_sInfoItem.Name}, ${translateText('player1', 'максимум можно иметь при себе')} - ${_sInfoItem.Stack} ${translateText('player1', 'шт.')}`, 3000);
                    returnItemToSource();
                }
                itemNoUse(27);
                return;
            }

            MaxStakcItems = 0;
            if ((selectArrayName !== "other" && selectArrayName !== "backpack") && 
                (hoverArrayName === "other" || hoverArrayName === "backpack") && 
                ![0, 237, 238, 239, 240, 241, 242, 245, 246, 247].includes(_hItem.ItemId) && 
                _hItem.ItemId != _sItem.ItemId) {
                
                MaxStakcItems = getMaxStakcItems(_hItem, _hInfoItem, hoverArrayName, hoverIndex);
                
                if (MaxStakcItems == -1) {
                    returnItemToSource();
                    itemNoUse(28);
                    return;
                }
            }

            if (MaxStakcItems > 0) {
                if (_hItem.ItemId === _sItem.ItemId) {
                    executeClient("client.gamemenu.inventory.move.stack", selectArrayName, selectIndex, hoverArrayName, hoverIndex, MaxStakcItems);
                    _sItem.Count += MaxStakcItems;
                    _hItem.Count -= MaxStakcItems;
                    setItem(hoverIndex, hoverArrayName, _hItem);
                    setItem(selectIndex, selectArrayName, _sItem);
                    executeClient("sounds.playInterface", "inventory/drag_drop", 0.05);
                } else {
                    window.notificationAdd(4, 9, `${translateText('player1', 'Нет места для')} ${_sInfoItem.Name}, ${translateText('player1', 'максимум можно иметь при себе')} - ${_sInfoItem.Stack} ${translateText('player1', 'шт.')}`, 3000);
                    returnItemToSource();
                }
                itemNoUse(29);
                return;
            }
if (hoverArrayName === "other" && showModificationsModal && modificationsWeaponItem) {
                if (! canPlaceModification(selectItem, hoverIndex)) {
                    window.notificationAdd(4, 9, 'Эта модификация не подходит для данного слота!', 3000);
                    returnItemToSource();
                    itemNoUse(48);
                    return;
                }
            }
            // ✅ МОЖНО ПОЛОЖИТЬ - ОТПРАВЛЯЕМ НА СЕРВЕР
            executeClient("client.gamemenu.inventory.move", 
                selectArrayName, selectIndex, 
                hoverArrayName, hoverIndex,
                selectItem.isTurn || false
            );

            // ✅ РАЗМЕЩАЕМ ЛОКАЛЬНО (НЕ ЖДЁМ ОТВЕТА СЕРВЕРА!)
            console.log(`[DRAG END] Placing item at hoverIndex ${hoverIndex}`);
            
            // ✅ СОЗДАЁМ ЗАГЛУШКИ ДЛЯ НОВОГО МЕСТА
            if (hoverArrayName === "inventory" || hoverArrayName === "backpack") {
                const hoverItemConfig = itemsInfo[_sItem.ItemId] || {};
                const hoverItemWidth = selectItem.isTurn ? (hoverItemConfig.Height || 1) : (hoverItemConfig.Width || 1);
                const hoverItemHeight = selectItem.isTurn ? (hoverItemConfig.Width || 1) : (hoverItemConfig.Height || 1);
                
                if (hoverItemWidth > 1 || hoverItemHeight > 1) {
                    const maxCols = 6;
                    const hoverStartX = hoverIndex % maxCols;
                    const hoverStartY = Math.floor(hoverIndex / maxCols);
                    
                    for (let dy = 0; dy < hoverItemHeight; dy++) {
                        for (let dx = 0; dx < hoverItemWidth; dx++) {
                            if (dx === 0 && dy === 0) continue;
                            
                            const placeholderIndex = (hoverStartY + dy) * maxCols + (hoverStartX + dx);
                            if (placeholderIndex < ItemsData[hoverArrayName].length) {
                                ItemsData[hoverArrayName][placeholderIndex] = {
                                    ...clearSlot,
                                    Data: `placeholder_${hoverIndex}`
                                };
                            }
                        }
                    }
                }
            }

            // ✅ РАЗМЕЩАЕМ ПРЕДМЕТ В НОВОМ МЕСТЕ
            setItem(hoverIndex, hoverArrayName, {
                ..._sItem,
                Index: hoverIndex,
                isTurn: selectItem.isTurn,
                isDragging: false
            });

            // ✅ ЕСЛИ БЫЛ ПРЕДМЕТ В ЦЕЛЕВОМ СЛОТЕ - РАЗМЕЩАЕМ ЕГО В ИСХОДНОМ
            if (_hItem.ItemId !== 0) {
                setItem(selectIndex, selectArrayName, {
                    ..._hItem,
                    Index: selectIndex,
                    isDragging: false
                });
            }

            executeClient("sounds.playInterface", "inventory/drag_drop", 0.05);
            itemNoUse(30, true);
        } else {
            // ✅ НЕ НАВЕЛИ НА ДРУГОЙ СЛОТ - ВОЗВРАЩАЕМ
            returnItemToSource();
            itemNoUse(30, true);
        }
    }
}

const handleGlobalMouseDown = (event) => {
    if (!visible) return;
    else if (event.which !== 1) return;
    else if (selectItem.tent && clickTime >= new Date().getTime()) return;
    else if (selectItem.use === stageItem.useItem && !useInventoryArea) {
        itemNoUse (31);
    }
}

const getItemToIndex = (index, arrayName) => {
    try {
        // ✅ ПРОВЕРКА НА СУЩЕСТВОВАНИЕ МАССИВА
        if (!ItemsData[arrayName]) {
            console.warn(`[getItemToIndex] Array "${arrayName}" does not exist!`);
            return { ...clearSlot };
        }
        
        // ✅ ПРОВЕРКА НА СУЩЕСТВОВАНИЕ ЭЛЕМЕНТА
        if (!ItemsData[arrayName][index]) {
            console.warn(`[getItemToIndex] Item at index ${index} in "${arrayName}" is undefined!`);
            return { ...clearSlot };
        }
        
        return ItemsData[arrayName][index];
    } catch (e) {
        console.error(`[getItemToIndex] Exception: ${e.toString()}`);
        return { ...clearSlot };
    }
}

const unHoverAll = () => {
    for (let arrayName in ItemsData) {
        ItemsData[arrayName].forEach((item, index) => {
            if (item.hover) updateItem(index, arrayName, "hover");                 
        })
    }
}

const isNumber = (value) => {
    return /^[-]?\d+$/.test(value);
}

const updateItem = (index, arrayName, name, value = null) => {
    if (!arrayName && !index) return;
    if (ItemsData[arrayName][index].ItemId != 0) {
        value = (value === null) ? !ItemsData[arrayName][index][name] : value;
        ItemsData[arrayName][index][name] = value;
        return value;
    }
    return -9;
}
// ✅ ОБРАБОТЧИКИ ДЛЯ СЛОТОВ МОДИФИКАЦИЙ
const handleModSlotMouseEnter = (event, slotIndex) => {
    // ✅ УСТАНАВЛИВАЕМ hoverItem ДЛЯ СЛОТОВ МОДИФИКАЦИЙ! 
    if (selectItem. use === stageItem.move) {
        hoverItem = {
            index: slotIndex,
            arrayName: "weaponMods" // ✅ ВАЖНО! 
        };
    }
    
    if (selectItem.use === stageItem. move && selectItem.ItemId) {
        const slot = event.currentTarget;
        const highlight = slot.querySelector('.highlight');
        if (! highlight) return;
        
        const canPlace = canPlaceModification(selectItem, slotIndex);
        
        if (canPlace) {
            highlight.style.backgroundColor = "rgba(105, 240, 108, 0.3)";
        } else {
            highlight.style.backgroundColor = "rgba(247, 20, 43, 0.3)";
        }
        
        const slotRect = slot.getBoundingClientRect();
        highlight.style.width = `${slotRect.width}px`;
        highlight. style.height = `${slotRect. height}px`;
    }
    
    // ✅ ПОКАЗЫВАЕМ TOOLTIP
    const item = weaponModifications[slotIndex];
    if (item && item.ItemId && item.ItemId !== 0 && selectItem.use !== stageItem.move) {
        const target = event.target. getBoundingClientRect();
        coords.set({ x: (target.x + target.width/2), y: target.y });
        infoItem = {
            ... item,
            index: slotIndex,
            arrayName: "weaponMods"
        };
    }
}

const handleModSlotMouseDown = (event, slotIndex) => {
    if (event.which !== 1) return;
    
    const item = weaponModifications[slotIndex];
    console.log(`[MOD SLOT] MouseDown on slot ${slotIndex}:`, item);
    
    if (!item || !item.ItemId || item.ItemId === 0) {
        console.log(`[MOD SLOT] Slot ${slotIndex} is empty`);
        return;
    }
    
    executeClient("sounds. playInterface", "inventory/keys", 0.005);
    
    const target = event.currentTarget. getBoundingClientRect(); // ✅ ИСПРАВЛЕНО: currentTarget вместо target
    const offsetInElementX = (target.width - (target.right - event.clientX));
    const offsetInElementY = (target.height - (target.bottom - event. clientY));
    
    coords.set({ x: event.clientX, y: event.clientY });
    clickTime = new Date().getTime() + 1000;
    
    // ✅ ОЧИЩАЕМ СЛОТ В weaponModifications
    weaponModifications[slotIndex] = { ...clearSlot };
    
    isDragging = true; // ✅ ДОБАВЛЕНО! 
    
    selectItem = {
        ... item,
        use: stageItem.move, // ✅ СРАЗУ stageItem.move, НЕ stageItem.get! 
        width: target.width,
        height: target.height,
        offsetInElementX: offsetInElementX,
        offsetInElementY: offsetInElementY,
        clientX: event.clientX,
        clientY: event.clientY,
        index: slotIndex,
        arrayName: "weaponMods", // ✅ ОТДЕЛЬНЫЙ ТИП! 
        originalIndex: slotIndex,
        originalArrayName: "weaponMods"
    };
    
    console.log(`[MOD SLOT] Started dragging modification:`, selectItem);
    
    event.preventDefault();
    event.stopPropagation();
}

const handleModSlotMouseUp = () => {
    // ✅ ОБРАБОТКА DROP В СЛОТ МОДИФИКАЦИИ
    if (selectItem. use === stageItem.move && hoverItem !== defaulHoverItem) {
        // Логика перемещения модификации
    }
}
// Обновление полного массива item`а
const setItem = (index, arrayName, item) => {
    if (item.active) item.active = false;
    
    ItemsData[arrayName][index] = {
        ...item,
        Index: index,
        index: index,
        isTurn: item.isTurn || false,  // ✅ СОХРАНЯЕМ isTurn!
        IsTurn: item.isTurn || false   // ✅ И IsTurn тоже!
    };
    
    // ✅ ПЕРЕСЧИТЫВАЕМ ВЕС
    if (arrayName === "inventory" || arrayName === "backpack") {
        recalculateWeight(arrayName);
    }
}

const itemNoUse = (hash, toggled = false) => {
    let hoverIndex = -1,
        hoverArrayName = -1;
        
    // ✅ ИСПРАВЛЕНО: ПРОВЕРЯЕМ, ЧТО arrayName СУЩЕСТВУЕТ В ItemsData! 
    if (selectItem !== defaulSelectItem && 
        selectItem. arrayName && 
        selectItem.arrayName !== "weaponMods" && // ✅ ИГНОРИРУЕМ weaponMods! 
        selectItem. index !== undefined &&
        ItemsData[selectItem.arrayName] && // ✅ ПРОВЕРЯЕМ СУЩЕСТВОВАНИЕ! 
        ItemsData[selectItem.arrayName][selectItem. index]) { // ✅ ПРОВЕРЯЕМ СУЩЕСТВОВАНИЕ ЭЛЕМЕНТА!
        
        ItemsData[selectItem.arrayName][selectItem.index] = {
            ... ItemsData[selectItem.arrayName][selectItem.index],
            isDragging: false // ✅ ВОЗВРАЩАЕМ ВИДИМОСТЬ! 
        };
        updateItem(selectItem.index, selectItem.arrayName, "hover", false);
    }

    clickTime = 0;
    selectItem = defaulSelectItem;
    isDragging = false;
    isDraggingModal = false;
    modalPositionX = 0;
    modalPositionY = 0;
    modalWasDragged = false;

    if (hoverItem !== defaulHoverItem) {
        hoverIndex = hoverItem.index;
        hoverArrayName = hoverItem.arrayName;
    }
    
    // ✅ ИСПРАВЛЕНО: ПРОВЕРЯЕМ, ЧТО arrayName НЕ weaponMods! 
    if (hoverIndex === -1 && hoverArrayName === -1) {
        infoItem = defaulHoverItem;
    } else if (hoverArrayName !== "weaponMods" && 
               ItemsData[hoverArrayName] && 
               ItemsData[hoverArrayName][hoverIndex]) {
        const _Item = getItemToIndex(hoverIndex, hoverArrayName);
        if (_Item. ItemId != 0) {
            infoItem = {
                ..._Item,
                index: hoverIndex,
                arrayName: hoverArrayName
            };
        } else {
            infoItem = defaulHoverItem;
        }
    } else {
        infoItem = defaulHoverItem;
    }

    ItemStack = -1;
    StackValue = 1;
    
    // ✅ УБИРАЕМ ВСЕ HIGHLIGHTS
    document.querySelectorAll('.highlight').forEach(el => {
        el.style.backgroundColor = "";
        el.style.width = "0";
        el.style.height = "0";
    });
}

const fixOutToX = (coordsX, element) => {
    if (!element) return coordsX;
    else if (document.querySelector('.box-inventory')) {
        let mainWidth = document.querySelector('.box-inventory').getBoundingClientRect().width;
        let elementWidth = element.getBoundingClientRect().width;
        if ((elementWidth + coordsX) >= mainWidth) return coordsX - elementWidth;
        return coordsX;
    }
    return coordsX;
}

const fixOutToY = (coordsY, element) => {
    if (!element) return coordsY;
    else if (document.querySelector('.box-inventory')) {
        let mainHeight = document.querySelector('.box-inventory').getBoundingClientRect().height;
        let elementHeight = element.getBoundingClientRect().height;
        if ((coordsY - elementHeight) < 0) return coordsY;
        return coordsY - elementHeight;
    }
    return coordsY;
}

const fixOutToCenter = (coordsX, element) => {
    if (!element) return coordsX;
    let elementWidth = element.getBoundingClientRect().width / 2;
    return coordsX - elementWidth;
}

const fixOutToTop = (coordsY, element) => {
    if (!element) return coordsY;
    let elementHeight = element.getBoundingClientRect().height;
    return coordsY - (elementHeight + (elementHeight * 0.2));
}

const animItems = (index, arrayName) => {
    ItemsData[arrayName][index] = {
        ...ItemsData[arrayName][index],
        anim: false
    };
    
    setTimeout(() => {
        ItemsData[arrayName][index] = {
            ...ItemsData[arrayName][index],
            anim: true
        };
    }, 0);
}

/* Трейд */
const TradeCancel = () => {
    if (tradeInfo.YourStatus) {
        tradeInfo.YourStatus = false;
        tradeInfo.YourStatusChange = false;
        ItemsData.trade.forEach((item, index) => {
            ItemsData.trade[index] = {
                ...ItemsData.trade[index],
                use: true
            };
        });
        executeClient ("client.gamemenu.inventory.trade", 0);
    }
}

const TradeSelect = () => {   
    if (!tradeInfo.YourStatus) {
        let seccuss = false;
        ItemsData.trade.forEach((item, index) => {
            if (selectItem.ItemId !== 0) {
                seccuss = true;
            }
        });

        if (tradeInfo.WithStatus && !seccuss) {// Проверяем, если у нас пустые слоты и второй игрок подтвердил трейд то
            
            ItemsData.with_trade.forEach((item, index) => {
                if (selectItem.ItemId !== 0) {
                    seccuss = true;
                }
            });
            if (!seccuss) { //Если у второго тоже пустые слото то выдаем ошибку
                window.notificationAdd(4, 9, translateText('player1', 'Для начала выберите предмет!'), 3000);
                return;
            }
        }         
        executeClient ("client.gamemenu.inventory.trade", 1);
    } else if (tradeInfo.YourStatus && tradeInfo.WithStatus && !tradeInfo.YourStatusChange) {
        executeClient ("client.gamemenu.inventory.trade", 2);
    }
}

const TradeUpdate = (status) => {
    if (status == -1) {
        tradeInfo.YourStatus = true;
        ItemsData.trade.forEach((item, index) => {
            ItemsData.trade[index] = {
                ...ItemsData.trade[index],
                use: false
            };
        });   
        return;
    }
    if (status == -2) {
        tradeInfo.YourStatusChange = true;
        window.notificationAdd(4, 9, translateText('player1', 'Вы подтвердили свою готовность!'), 3000);
        return;
    }
    let lastWithStatus = tradeInfo.WithStatus;
    if (!status) {
        tradeInfo.WithStatus = false;
        tradeInfo.WithStatusChange = false;
        tradeInfo.YourStatusChange = false;
    }
    else if (status === 1) tradeInfo.WithStatus = true;
    else if (status === 2) tradeInfo.WithStatusChange = true;

    ItemsData.with_trade.forEach((item, index) => {
        ItemsData.with_trade[index] = {
            ...ItemsData.with_trade[index],
            use: tradeInfo.WithStatus ? false : true
        };
    });

    if (lastWithStatus !== tradeInfo.WithStatus) {            
        selectItem = defaulSelectItem;
        isMoveBlock = false;
        if (!tradeInfo.YourStatus) {
            ItemsData.trade.forEach((item, index) => {
                ItemsData.trade[index] = {
                    ...ItemsData.trade[index],
                    use: tradeInfo.WithStatus ? false : true
                };
            });
        }
    }
    if (status === 1 && !tradeInfo.YourStatus) {
        window.notificationAdd(4, 9, `${tradeInfo.WithName} ${translateText('player1', 'выбрал предметы для трейда')}!`, 3000);
    } else if (status === 1 && tradeInfo.YourStatus) {
        window.notificationAdd(4, 9, `${translateText('player1', 'Вы и')} ${tradeInfo.WithName} ${translateText('player1', 'выбрали предметы для трейда')}`, 3000);
    } else if (status === 0) {
        window.notificationAdd(4, 9, `${tradeInfo.WithName} ${translateText('player1', 'выбирает предметы')}`, 3000);
    } else if (status === 2) {
        window.notificationAdd(4, 9, `${tradeInfo.WithName} ${translateText('player1', 'готов к обмену')}`, 3000);
    }
}
let isDraggingModal = false;
let modalDragOffsetX = 0;
let modalDragOffsetY = 0;
let modalPositionX = 0;
let modalPositionY = 0;
let modalWasDragged = false; // ✅ НОВАЯ ПЕРЕМЕННАЯ - БЫЛО ЛИ ПЕРЕТАСКИВАНИЕ

// ✅ НАЧАЛО ПЕРЕТАСКИВАНИЯ ОКНА
// ✅ НАЧАЛО ПЕРЕТАСКИВАНИЯ ОКНА (С ОТЛАДКОЙ)
// ✅ НАЧАЛО ПЕРЕТАСКИВАНИЯ ОКНА (УПРОЩЁННАЯ ВЕРСИЯ)
const handleModalMouseDown = (event) => {
    console.log('[MODAL DRAG] handleModalMouseDown called!', event.target);
    
    // Игнорируем клики на интерактивные элементы
    const isCloseButton = event.target.closest('.close-blockinv');
    const isInput = event.target.closest('.rangeinv__input');
    const isSlider = event.target.closest('.rangeinv-cover');
    const isBody = event.target.closest('.modal-body');
    const isButton = event.target.closest('.modal-body__button');
    
    if (isCloseButton || isInput || isSlider || isButton || isBody) {
        console.log('[MODAL DRAG] Clicked on interactive element - ignoring');
        return;
    }
    
    console.log('[MODAL DRAG] Starting drag!');
    
    isDraggingModal = true;
    
    // ✅ ИЩЕМ РОДИТЕЛЬСКОЕ ОКНО
    const modalElement = event.target.closest('.modal-window');
    if (!modalElement) {
        console.error('[MODAL DRAG] Modal window not found!');
        return;
    }
    
    const rect = modalElement.getBoundingClientRect();
    
    // ✅ ЕСЛИ ОКНО ЕЩЁ НЕ ДВИГАЛИ - ИСПОЛЬЗУЕМ $coords
    if (!modalWasDragged) {
        modalPositionX = rect.left;
        modalPositionY = rect.top;
    }
    
    modalDragOffsetX = event.clientX - rect.left;
    modalDragOffsetY = event.clientY - rect.top;
    
    console.log('[MODAL DRAG] Offsets:', { modalDragOffsetX, modalDragOffsetY });
    console.log('[MODAL DRAG] Current position:', { modalPositionX, modalPositionY });
    
    event.preventDefault();
    event.stopPropagation();
};

// ✅ ПЕРЕМЕЩЕНИЕ ОКНА
const handleModalMouseMove = (event) => {
    if (!isDraggingModal) return;
    
    modalWasDragged = true; // ✅ ПОМЕЧАЕМ, ЧТО ОКНО ДВИГАЛИ
    
    modalPositionX = event.clientX - modalDragOffsetX;
    modalPositionY = event.clientY - modalDragOffsetY;
    
    // Ограничиваем выход за пределы экрана
    const modalWidth = 400;
    const modalHeight = 250;
    
    modalPositionX = Math.max(0, Math.min(modalPositionX, window.innerWidth - modalWidth));
    modalPositionY = Math.max(0, Math.min(modalPositionY, window.innerHeight - modalHeight));
    
    console.log('[MODAL DRAG] Moving to:', { modalPositionX, modalPositionY });
};

// ✅ КОНЕЦ ПЕРЕТАСКИВАНИЯ
const handleModalMouseUp = () => {
    if (isDraggingModal) {
        console.log('[MODAL DRAG] Ended at:', { modalPositionX, modalPositionY });
    }
    isDraggingModal = false;
};
const rangeslidercreate = (max) => {
    StackValue = Math.round(max / 2);
    
    setTimeout(() => {
        const sliderContainer = document.getElementById("stack");
        
        if (!sliderContainer) {
            console.error('[SLIDER] Element #stack not found!');
            return;
        }
        
        // ✅ СОЗДАЁМ СТРУКТУРУ СЛАЙДЕРА
        sliderContainer.innerHTML = `
            <div class="vue-slider-rail">
                <div class="vue-slider-process" style="height: 100%; top: 0px; left: 0%; width: ${(StackValue / max) * 100}%; transition-property: width, left; transition-duration: 0.5s;"></div>
                <div class="vue-slider-dot" 
                     role="slider" 
                     aria-valuenow="${StackValue}" 
                     aria-valuemin="1" 
                     aria-valuemax="${max}" 
                     aria-orientation="horizontal" 
                     tabindex="0"
                     style="width: 14px; height: 14px; transform: translate(-50%, -50%); top: 50%; left: ${(StackValue / max) * 100}%; transition: left 0.5s;">
                    <div class="vue-slider-dot-handle"></div>
                </div>
            </div>
        `;
        
        const rail = sliderContainer.querySelector('.vue-slider-rail');
        const dot = sliderContainer.querySelector('.vue-slider-dot');
        const process = sliderContainer.querySelector('.vue-slider-process');
        
        let isDragging = false;
        
        // ✅ ОБНОВЛЕНИЕ ЗНАЧЕНИЯ
        const updateValue = (newValue) => {
            StackValue = Math.max(0, Math.min(max, Math.round(newValue)));
            const percent = (StackValue / max) * 100;
            
            dot.style.left = `${percent}%`;
            process.style.width = `${percent}%`;
            dot.setAttribute('aria-valuenow', StackValue);
        };
        
        // ✅ КЛИК ПО RAIL
        const handleRailClick = (e) => {
            const rect = rail.getBoundingClientRect();
            const x = e.clientX - rect.left;
            const percent = x / rect.width;
            const newValue = Math.round(percent * max);
            updateValue(newValue);
        };
        
        // ✅ DRAG СЛАЙДЕРА
        const handleMouseDown = (e) => {
            isDragging = true;
            e.preventDefault();
        };
        
        const handleMouseMove = (e) => {
            if (!isDragging) return;
            
            const rect = rail.getBoundingClientRect();
            const x = e.clientX - rect.left;
            const percent = Math.max(0, Math.min(1, x / rect.width));
            const newValue = Math.round(percent * max);
            updateValue(newValue);
        };
        
        const handleMouseUp = () => {
            isDragging = false;
        };
        
        // ✅ СОБЫТИЯ
        rail.addEventListener('click', handleRailClick);
        dot.addEventListener('mousedown', handleMouseDown);
        document.addEventListener('mousemove', handleMouseMove);
        document.addEventListener('mouseup', handleMouseUp);
        
        // ✅ КЛАВИАТУРА
        dot.addEventListener('keydown', (e) => {
            if (e.key === 'ArrowLeft' || e.key === 'ArrowDown') {
                e.preventDefault();
                updateValue(StackValue - 1);
            } else if (e.key === 'ArrowRight' || e.key === 'ArrowUp') {
                e.preventDefault();
                updateValue(StackValue + 1);
            }
        });
        
        console.log('[SLIDER] Custom slider created with max:', max);
    }, 100);
}

const createRangeSlider = () => {
    rangeslider.create(document.getElementById("invOpacity"), {min: 0, max: 1, value: invOpacity, step: 0.1, onSlide: (value, percent, position) => {
        invOpacity = value;
    }});
}
const entityMap = {
    '&': '&amp;',
    '<': '&lt;',
    '>': '&gt;',
    '"': '&quot;',
    "'": '&#39;',
    '/': '&#x2F;',
    '`': '&#x60;',
    '=': '&#x3D;'
};

const escapeHtml = (str) => {
    return String(str).replace(/[&<>"'`=\/]/g, function (s) {
        return entityMap[s];
    });
}
const getName = (Item, arrayName) => {
    const _infoItem = window.getItem (Item.ItemId);
    let name = escapeHtml (_infoItem.Name);
    if (Item.ItemId == -9 && arrayName !== "accessories") {
        name += `<span>Состояние: ${Item.Data}</span>`;
    }
    else if (Item.ItemId == 19 && Item.Data.split("_")) {
        name += `<span>${Item.Data.split("_")[0]} | ${Item.Data.split("_")[1]}</span>`;
    } 
    else if (_infoItem.Stack > 1 && Item.Count > 1) {
        name += ` | ${Item.Count} шт.`;
    } else if (_infoItem.functionType === ItemType.Modification && Item.Data.split("_").length) {
        let dataParse = Item.Data.split("_");
        if (WeaponHashToItem [dataParse[0]]) name += `<span>Weapon: ${window.getItem (WeaponHashToItem [dataParse[0]]).Name}</span>`;
    } else if (Item.ItemId == 220) {
        name += `<span>${Item.Data}</span>`;
    }
    else if (Item.ItemId == 234 || Item.ItemId == 235 || Item.ItemId == 236 || Item.ItemId == 244
        || Item.ItemId == 225 || Item.ItemId == 229 || Item.ItemId == 249) {
        let ItemValue = 300;

        switch (Item.ItemId) {
            case 234:
                ItemValue = 300;
                break;
            case 235:
                ItemValue = 1248;
                break;
            case 236:
                ItemValue = 2250;
                break;
            case 244:
                ItemValue = 1250;
                break;
            case 225:
                ItemValue = 100;
                break;
            case 229:
                ItemValue = 420;
                break;
            case 249:
                ItemValue = 1000;
                break;
        }
        
        name += `<span>${translateText('player1', 'Состояние')}: ${weaponCondition (Item.Data, wMaxHP [Item.ItemId])}%</span>`;
    }
    else if (Item.ItemId == ItemId.SimCard || Item.ItemId == ItemId.VehicleNumber)
        name += `<span>${Item.Data}</span>`;
    return name;
}

const getNameToData = (Item, arrayName) => {
    const _infoItem = window.getItem (Item.ItemId);
    let name = "";
    if (_infoItem.functionType === ItemType.Weapons) {
        return Item.Data;
    }
    return name;
}

const getDropItem  = (arrayName, ItemId) => {
    if (arrayName === "fastSlots") return false;
    switch (ItemId) {
       // case 2: return false;
        //case 243: return false;
    }
    return true;
}


const getItemsUse  = (Item) => {
    if (Item.arrayName !== "inventory" && Item.arrayName !== "accessories" && Item.arrayName !== "fastSlots") return false;
    else if (Item.arrayName === "accessories" && (Item.ItemId === 12 || Item.ItemId === 15)) return false;
    const _infoItem = window.getItem (Item.ItemId);
    if (_infoItem.functionType === ItemType.Clothes || _infoItem.functionType === ItemType.Weapons || _infoItem.functionType === ItemType.MeleeWeapons || _infoItem.functionType === ItemType.Alco) return true;
    else if (Item.ItemId == 41 && ((OtherInfo.Id !== otherType.None && OtherInfo.Id !== otherType.Key) || tradeInfo.Active)) return false;
    switch (Item.ItemId) {
        case 220:
        case 4:
        case 41:
        case 19:
        case 13:
        case 6:
        case 14:
        case 9:
        case 2:
        case 1:
        case 280:
        case 7:
        case 11:
        case 16:
        case 5:
        case 8:
        case 10:
        case 3:
        case 40:
        case 12:
        case 225:
        case 15:
        case 248:
        case 229:
        case ItemId.SimCard:return true;
        //case 249: return true;
    }
    return false;
}

const getItemsClickInfo = (info) => {
    let _infoItem = window.getItem (info.ItemId);
    if (info.arrayName === "accessories") return `${translateText('player1', 'снять')}`;
    else if (_infoItem.functionType === ItemType.Clothes) return `${translateText('player1', 'надеть')}`;
    else if (info.arrayName !== "fastSlots" && (_infoItem.functionType === ItemType.Weapons || _infoItem.functionType === ItemType.MeleeWeapons) && ItemToWeaponHash [info.ItemId] && wComponents [ItemToWeaponHash [info.ItemId]] && wComponents [ItemToWeaponHash [info.ItemId]].Components) return `${translateText('player1', 'модификации')}`;
    else if (info.arrayName !== "fastSlots" && (_infoItem.functionType === ItemType.Weapons || _infoItem.functionType === ItemType.MeleeWeapons)) return `${translateText('player1', 'взять')}`;
    else if (_infoItem.functionType === ItemType.Alco || _infoItem.functionType === ItemType.Water) return `${translateText('player1', 'выпить')}`;
    else if (_infoItem.functionType === ItemType.Eat) return `${translateText('player1', 'съесть')}`;
    else if (info.ItemId == 41 && OtherInfo.Id === otherType.None) return `${translateText('player1', 'открыть')}`;
    else if ((OtherInfo.Id === otherType.wComponents || OtherInfo.Id === otherType.Key) && OtherSqlId && Number (OtherSqlId) === Number (info.SqlId)) return `${translateText('player1', 'закрыть')}`;
    else if ((info.ItemId == 225 || info.ItemId == 229) && OtherInfo.Id === otherType.None) return `${translateText('player1', 'курить')}`;
    else if (_infoItem.functionType === ItemType.Cases) return `${translateText('player1', 'открыть')}`;
    else if (info.ItemId == ItemId.SimCard) return `${translateText('player1', 'Вставить')}`;
    return `${translateText('player1', 'использовать')}`;
}

const onItemsHands = () => {
    if (selectItem.use === stageItem.useItem) {
        const selectIndex = selectItem.index;
        const selectArrayName = selectItem.arrayName;

        executeClient ("client.gamemenu.inventory.hands", selectArrayName, selectIndex);
        itemNoUse (32);
    }
}

const getToPut = (ItemId) => {
    switch (ItemId) {
        case 234:
        case 235:
        case 236:
        case 237:
        case 238:
        case 239:
        case 240:
        case 241:
        case 242:
        case 248:
        case 229: return false;
        //case 249: return false;
    }
    return true;
}

const onToPut = () => {
    if (selectItem.use === stageItem.useItem) {
        const selectIndex = selectItem.index;
        const selectArrayName = selectItem.arrayName;

        executeClient ("client.gamemenu.inventory.toput", selectArrayName, selectIndex);
        itemNoUse (33);
    }
}

const handleMouseDownBlock = (event, eClass, arrayName) => {
    /*if (event.which == 1) {
        if (selectItem.use === stageItem.none) {
            itemNoUse (34);

            const target = document.querySelector('.box-inventory .' + eClass).getBoundingClientRect();

            const offsetInElementX = (target.width - (target.right - event.clientX));
            const offsetInElementY = (target.height - (target.bottom - event.clientY));

            selectItem = {
                use: stageItem.moveBlock,
                width: target.width,
                height: target.height,
                offsetInElementX: offsetInElementX,
                offsetInElementY: offsetInElementY,
            } 
            moveBlock[arrayName] = [
                event.clientY - offsetInElementY,
                event.clientX - offsetInElementX,
                //event.clientX - offsetInElementX
            ]

            isMoveBlock = arrayName;

        }
    }*/
}

let boxItemInfo;
let boxInfoLeft = 0;
let boxInfoTop = 0;
    let weaponModifications = Array(5).fill(null). map(() => ({ ... clearSlot }));
    let nearbyItems = Array(maxSlots. other).fill(null). map(() => ({ ... clearSlot }));

$: if (boxItemInfo) {
    boxInfoLeft = fixOutToCenter ($coords.x, boxItemInfo);
    boxInfoTop = fixOutToTop ($coords.y, boxItemInfo);
}

let boxPopup;

const weaponCondition = (hp, maxHp) => {
    const condition = Math.floor((hp / maxHp) * 100);
    if (!condition || isNaN(condition)) {
        return 0;
    }
    return condition;
}

let cursorX = 0;
let cursorY = 0;

function handleMouseMove(event) {
    cursorX = event.clientX;
    cursorY = event.clientY;
}

const onExit = () => {
    executeClient ("client.inventory.Close");
}

const onOpenBattlePass = () => {
    executeClient ("client.battlepass.open");
}
// ✅ ПЕРЕМЕННЫЕ ДЛЯ ПЕРЕТАСКИВАНИЯ ОКНА МОДИФИКАЦИЙ
// ✅ ПЕРЕМЕННЫЕ ДЛЯ ПЕРЕТАСКИВАНИЯ ОКНА МОДИФИКАЦИЙ
let isDraggingModModal = false;
let modModalDragOffsetX = 0;
let modModalDragOffsetY = 0;

// ✅ НАЧАЛО ПЕРЕТАСКИВАНИЯ
const handleModModalMouseDown = (event) => {
    // ✅ Игнорируем клики на слоты и кнопку закрытия
    if (event.target.closest('.mod-slot') || event.target. closest('.close-blockinv')) {
        return;
    }
    
    isDraggingModModal = true;
    modModalDragOffsetX = event.clientX - modModalX;
    modModalDragOffsetY = event.clientY - modModalY;
    
    event.preventDefault();
    event.stopPropagation();
};


let activeWeapon = null; // ✅ АКТИВНОЕ ОРУЖИЕ В РУКАХ
$: {
    const weaponSlot = ItemsData["accessories"]?.[19];
    
    if (weaponSlot && weaponSlot.ItemId && weaponSlot.ItemId !== 0) {
        activeWeapon = weaponSlot;
    } else {
        activeWeapon = null;
    }
}
    

const CounterUpdate = (args, value) => {
    if (userData["timerId" + args])
        clearTimeout (userData["timerId" + args]);
    userData["change" + args] = userData[args] > value ? (0 - (userData[args] - value)) : (value - userData[args]);
    userData[args] = value;
    userData["timerId" + args] = setTimeout (() => {
        userData["timerId" + args] = 0;
        userData["change" + args] = 0;
        if (!userData["target" + args]) {
            userData["target" + args] = new CountUp("target" + args, value);
            //userData["target" + args].start();
            //userData["target" + args].update(value);
        }
        else
            userData["target" + args].update(value);
    }, !userData["target" + args] ? 0 : 5000)
}
let opened = true;
let hp = 100;
let eat = 100;
let water = 100;
let inventoryWeight = 0;
let maxInventoryWeight = 40;
let backpackWeight = 0;
let maxBackpackWeight = 25;

// ✅ РЕАКТИВНЫЕ ВЫЧИСЛЕНИЯ ПРОЦЕНТОВ
$: inventoryPercent = (inventoryWeight / trunkMaxWeight) * 100;
$: backpackPercent = maxBackpackWeight > 0 ? (backpackWeight / maxBackpackWeight) * 100 : 0;

// ✅ ЦВЕТА ИНДИКАТОРОВ
$: inventoryColor = inventoryPercent > 90 ? '#ff0000' : inventoryPercent > 70 ? '#ffaa00' : '#00ff00';
$: backpackColor = backpackPercent > 90 ? '#ff0000' : backpackPercent > 70 ? '#ffaa00' : '#00ff00';

const open = () => opened = true;
const close = () => opened = false;
const updateHealth = (val) => hp = val;
const updateEat = (val) => eat = val;
const updateWater = (val) => water = val;

// После функции updateWater
const updateActiveWeapon = (itemId, sqlId) => {
    if (itemId && itemId !== 0) {
        // ✅ НАХОДИМ ПРЕДМЕТ В ИНВЕНТАРЕ ИЛИ БЫСТРЫХ СЛОТАХ
        let foundItem = null;
        
        for (let arrayName of ["inventory", "fastSlots", "accessories"]) {
            foundItem = ItemsData[arrayName].find(item => 
                item && item.ItemId === itemId && item.SqlId === sqlId
            );
            if (foundItem) break;
        }
        
        activeWeapon = foundItem || null;
    } else {
        activeWeapon = null;
    }
}

    import TrunkInventory from './TrunkInventory.svelte'

// ✅ ПЕРЕМЕННЫЕ ДЛЯ ПЕРЕТАСКИВАНИЯ ОКНА РАЗДЕЛЕНИ
</script>

<svelte:window 
    on:mousemove={handleGlobalMouseMove} 
    on:mousemove={handleMouseMove} 
    on:mousemove={handleModalMouseMove}
    on:mouseup={handleGlobalMouseUp} 
    on:mouseup={handleModalMouseUp}
    on:mousedown={handleGlobalMouseDown} 
    on:keyup={onKeyUp} 
    on:keydown={onKeyDown} 
/>
{#if visible}
{#if OtherInfo.Id === 1 || OtherInfo.Id === 2 || OtherInfo.Id === 3 || OtherInfo.Id === 4 || OtherInfo.Id === 5 || OtherInfo.Id === 6 || OtherInfo.Id === 10 || OtherInfo.Id === 12 || OtherInfo.Id === 13}
        <TrunkInventory
            {OtherInfo}
            {ItemsData}
            bind:searchText
            {onExit}
            {handleMouseDown}
            {handleSlotMouseUp}
            {handleSlotMouseEnter}
            {handleSlotMouseLeave}
            bind:mainInventoryArea
            {getItemSize}
            {cdn}
            maxWeight={trunkMaxWeight}
        />
    {:else}
    <div class="inventory-interface full-width full-height" data-v-29f6b6db>
        
        <!-- Click Menu (Context Menu) -->
        

        <!-- Hover Block -->
        <!-- ✅ HOVER BLOCK (УЛУЧШЕННЫЙ) -->
{#if (infoItem !== defaulHoverItem && 
      selectItem.use !== stageItem.move && 
      !infoItem.Data?.startsWith("placeholder_"))}
    {@const _infoItem = window.getItem(infoItem.ItemId)}
    {@const isWeapon = _infoItem.functionType === ItemType.Weapons || _infoItem.functionType === ItemType.MeleeWeapons}
    {@const isClothes = _infoItem.functionType === ItemType.Clothes}
    {@const hasCondition = infoItem.ItemId === -9 || infoItem.ItemId === 234 || infoItem.ItemId === 235 || infoItem.ItemId === 236 || infoItem.ItemId === 244 || infoItem.ItemId === 225 || infoItem.ItemId === 229 || infoItem.ItemId === 249}
    
    <div class="hover-block" data-v-29f6b6db 
        style="opacity: 1; left: {fixOutToX(cursorX + 20)}px; top: {fixOutToY(cursorY)}px;">
        <div class="hover-block-body column-block" data-v-29f6b6db>
            <!-- ✅ НАЗВАНИЕ И ОПИСАНИЕ -->
            <div class="title" data-v-29f6b6db>
                <div data-v-29f6b6db>{@html getName(infoItem, infoItem.arrayName)}</div>
                <div class="hover-block-body__text-description" data-v-29f6b6db>
                    {_infoItem.Description}
                </div>
            </div>
            
       <!-- ✅ МОДИФИКАЦИИ (ТОЛЬКО ДЛЯ ОРУЖИЯ) -->
{#if isWeapon && ItemToWeaponHash[infoItem.ItemId] && wComponents[ItemToWeaponHash[infoItem.ItemId]]}
    {@const weaponHash = ItemToWeaponHash[infoItem.ItemId]}
    {@const weaponData = infoItem.Data ?  infoItem.Data. split('_') : []}
    {@const installedModHashes = weaponData.slice(1)}
    {@const weaponComponents = wComponents[weaponHash]?. Components || {}}
    
    <!-- ✅ МАППИНГ: ТИП КОМПОНЕНТА → ИНДЕКС СЛОТА -->
    {@const typeToSlot = {
        0: 0,  // Магазин → Слот 0
        4: 1,  // Прицел (SCOPE) → Слот 1
        8: 2,  // Рукоятка (GRIP) → Слот 2
        7: 3,  // Фонарик (FLASH) → Слот 3
        3: 4   // Глушитель (SUPP) → Слот 4
    }}
    
    <!-- ✅ МАППИНГ: ТИП КОМПОНЕНТА → ItemId ДЛЯ ИКОНКИ -->
    {@const typeToItemId = {
        0: 207,  // Магазин → 207. png
        0: 751,
        4: 209,  // Прицел → 209.png
        4: 750,
        8: 213,  // Рукоятка → 213.png
        7: 212,  // Фонарик → 212.png
        3: 208   // Глушитель → 208.png
    }}
    
    <!-- ✅ МАППИНГ: ИНДЕКС СЛОТА → ТИП КОМПОНЕНТА -->
    {@const slotToType = {
        0: 0,  // Слот 0 → Магазин
        1: 4,  // Слот 1 → Прицел
        2: 8,  // Слот 2 → Рукоятка
        3: 7,  // Слот 3 → Фонарик
        4: 3   // Слот 4 → Глушитель
    }}
    
    <!-- ✅ СОБИРАЕМ УСТАНОВЛЕННЫЕ МОДИФИКАЦИИ ПО СЛОТАМ -->
    {@const installedBySlot = (() => {
        const result = {};
        for (const modHash of installedModHashes) {
            if (! modHash) continue;
            const compInfo = weaponComponents[modHash];
            if (compInfo && typeToSlot[compInfo.Type] !== undefined) {
                result[typeToSlot[compInfo. Type]] = {
                    hash: modHash,
                    info: compInfo,
                    itemId: typeToItemId[compInfo.Type]
                };
            }
        }
        return result;
    })()}
    
    <div data-v-29f6b6db class="modifications column-block full-width">
        <span data-v-29f6b6db class="modifications__title">Модификации:</span>
        <div data-v-29f6b6db class="modifications-list row-block full-width">
            <!-- ✅ ВСЕГДА 5 СЛОТОВ -->
            {#each [0, 1, 2, 3, 4] as slotIndex}
                {@const mod = installedBySlot[slotIndex]}
                {@const hasModification = !!mod}
                {@const slotItemId = mod?.itemId || typeToItemId[slotToType[slotIndex]]}
                
                <div data-v-29f6b6db 
                     class="modification {hasModification ? 'installed' : 'empty'}" 
                     title={mod?.info?.Name || ''}>
                    
                    {#if hasModification}
                        <!-- ✅ КАРТИНКА УСТАНОВЛЕННОЙ МОДИФИКАЦИИ -->
                        <div class="modification-icon" data-v-29f6b6db
                             style="background-image: url(http://cdn.piecerp.ru/cloud/inventoryItems/items/{slotItemId}.png);
                                    background-size: contain;
                                    background-position: center;
                                    background-repeat: no-repeat;
                                    width: 100%;
                                    height: 100%;">
                        </div>
                    {:else}
                        <!-- ✅ ПУСТОЙ СЛОТ - ПОКАЗЫВАЕМ SVG ИКОНКУ ТИПА СЛОТА -->
                        {#if slotIndex === 0}
                            <!-- Магазин -->
                            <svg data-v-29f6b6db class="modification__icon" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="22" height="22" viewBox="0 0 22 22">
    <path d="M47.587,19.558c.009-.9,0-1.8,0-2.7,0-.937,0-1.879-.009-2.807,0-.438,0-.438-.438-.455-.053,0-.133.009-.141-.053-.04-.274-.243-.239-.429-.243-.831-.018-1.662-.009-2.493-.022-.88-.018-1.76-.128-2.639-.08h-.1a.582.582,0,0,1-.274-.049,1.924,1.924,0,0,0-1.105-.08.33.33,0,0,0-.225.124c-.071.115-.164.111-.27.106a1.24,1.24,0,0,0-.181,0c-.19.009-.252.093-.225.274a1.015,1.015,0,0,1,.009.164c0,.561-.044,1.119-.062,1.68l-.066,1.83c-.027.685-.049,1.375-.066,2.06s-.049,1.375-.1,2.06c-.044.61-.119,1.22-.2,1.83-.115.875-.292,1.737-.491,2.6-.354,1.521-.867,3-1.322,4.487-.2.672-.416,1.344-.615,2.016-.093.309-.062.345.239.447.1.035.225.018.279.119a.249.249,0,0,0,.177.128c.128.04.252.08.38.115.044.013.1.035.137-.013.057-.066.119-.035.181-.018,1.189.345,2.378.685,3.563,1.039.15.044.367.027.411.261,0,.018.044.027.071.035.332.1.663.2.995.3.053.018.1.027.137-.022.057-.08.124-.044.19-.022.305.1.606.2.911.3a.548.548,0,0,0,.27.049,2.231,2.231,0,0,0,.663-.23.23.23,0,0,0,.141-.234A1.171,1.171,0,0,1,44.979,34c.093-.248.168-.5.243-.752.389-1.344.827-2.675,1.194-4.027.23-.853.433-1.715.6-2.582.181-.951.323-1.9.416-2.865.066-.668.124-1.335.155-2,.027-.508,0-1.012.035-1.521C47.636,20.023,47.587,19.788,47.587,19.558Z" transform="translate(-30.863 -13.029)"/>
</svg>
                        {:else if slotIndex === 1}
                            <!-- Прицел -->
                            <svg data-v-29f6b6db class="modification__icon" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="22" height="22" viewBox="0 0 22 22">
    <g transform="translate(0 2.088)">
      <path d="M18.2,21.618c0-.283.009-.5,0-.717-.018-.34-.012-.681-.024-1.024,0-.094,0-.185.006-.279a.235.235,0,0,1,.191-.246c.295-.094.589-.191.887-.279a.144.144,0,0,0,.112-.134c0-.049-.07-.052-.115-.07l-.82-.328-.043-.018c-.2-.085-.216-.115-.188-.331.076-.562.152-1.121.228-1.683a.246.246,0,0,1,.131-.21.6.6,0,0,0,.3-.516,1.374,1.374,0,0,0-.112-.687,5.439,5.439,0,0,1-.346-1.358,8.777,8.777,0,0,1-.085-1.322,8.825,8.825,0,0,1,.106-1.094,8.018,8.018,0,0,1,.3-1.255,5.679,5.679,0,0,1,.547-1.215,4.6,4.6,0,0,1,.69-.881,1.123,1.123,0,0,1,.674-.331c.267-.039.538-.082.805-.128a.919.919,0,0,1,.444.073c.331.106.659.222.99.328l1.118.365,1.756.571c.155.052.307.112.462.152a.51.51,0,0,1,.355.289A9.248,9.248,0,0,1,27,10.356a8.817,8.817,0,0,1,.322,1.343,6.275,6.275,0,0,1,.079.787,2.415,2.415,0,0,1,.012.611c-.012.052.039.076.091.079a4.5,4.5,0,0,1,.829.112c.122.03.216-.055.328-.07a.4.4,0,0,0,.307-.213,9.784,9.784,0,0,1,.65-.814c.112-.137.225-.276.343-.407a.929.929,0,0,1,.362-.2c.377-.158.753-.313,1.13-.471a15.037,15.037,0,0,1,1.938-.7c.665-.173,1.328-.358,1.984-.562.283-.088.568-.161.851-.237a.5.5,0,0,1,.258,0c.325.085.647.179.969.276q.766.232,1.531.456a.938.938,0,0,1,.483.313c.182.216.377.425.568.635a.4.4,0,0,1,.109.289c0,.377.006.753,0,1.127-.006.334.046.668.027,1-.006.128-.015.255-.03.386a.358.358,0,0,1-.2.249c-.532.358-1.069.714-1.6,1.069-.161.106-.319.216-.477.328a.444.444,0,0,0-.194.45,3.851,3.851,0,0,1,.058.7.807.807,0,0,0,.021.225.3.3,0,0,1-.128.27,2.218,2.218,0,0,1-1.045.516,3.364,3.364,0,0,1-.541.115.128.128,0,0,0-.131.158c.006.064,0,.128,0,.194.018.857-.033,1.713-.024,2.573a.218.218,0,0,1-.164.237c-.477.158-.951.325-1.425.489l-1.191.41-1.318.456c-.556.191-1.112.386-1.671.577-.544.185-1.088.368-1.628.556-.441.152-.878.307-1.315.459q-.816.283-1.628.559c-.592.2-1.188.4-1.78.611a.461.461,0,0,1-.371-.015c-.289-.131-.577-.261-.866-.4a.217.217,0,0,1-.082-.362s0-.006.006-.009c.349-.231.228-.589.243-.911a.08.08,0,0,0-.073-.088,2.105,2.105,0,0,1-.626-.307,3.053,3.053,0,0,0-.55-.234,1.425,1.425,0,0,1-.261-.106.416.416,0,0,0-.4-.012c-.5.167-1.006.322-1.51.483a.376.376,0,0,1-.264-.018c-.146-.061-.295-.122-.444-.173a.465.465,0,0,1-.331-.437C18.188,22.326,18.2,21.937,18.2,21.618Zm3.67-6.5a.148.148,0,0,0,0-.134,1.159,1.159,0,0,1-.027-.383c0-.052.012-.112.088-.067a1.85,1.85,0,0,0,.295.115c.419.152.838.3,1.261.447.322.109.647.207.966.319.082.027.115.015.146-.058.061-.146.125-.292.185-.437a6.939,6.939,0,0,0,.407-1.713,8.8,8.8,0,0,0,.021-1.167,3.086,3.086,0,0,0-.052-.529,6.251,6.251,0,0,0-.337-1.279.584.584,0,0,0-.425-.407c-.34-.079-.674-.191-1.012-.295-.358-.109-.714-.222-1.069-.337q-.611-.2-1.221-.419c-.07-.024-.1-.018-.131.049a6.39,6.39,0,0,0-.3.781,6.438,6.438,0,0,0-.225,1.337c-.012.249-.006.5,0,.747a6.9,6.9,0,0,0,.155,1.2,6.7,6.7,0,0,0,.422,1.358.758.758,0,0,0,.51.516.241.241,0,0,1,.213.191A.444.444,0,0,0,21.867,15.114Z" transform="translate(-18.169 -7.506)"/>
    </g>
</svg>
                        {:else if slotIndex === 2}
                            <!-- Рукоятка -->
                            <svg data-v-29f6b6db class="modification__icon" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="22" height="22" viewBox="0 0 22 22">
    <g transform="translate(0 6.237)">
      <path d="M86.969,52.9a1.847,1.847,0,0,1-.078.593,1.057,1.057,0,0,1-.35.5c-.049.039-.088.088-.136.126-.068.058-.107.107-.01.185s.01.156-.058.2a1.178,1.178,0,0,1-.651.272,2.657,2.657,0,0,1-.622.01,1.078,1.078,0,0,1-.788-.476,8.645,8.645,0,0,1-.807-1.313.609.609,0,0,0-.311-.292,1.469,1.469,0,0,1-.632-.758.682.682,0,0,1,.029-.681.6.6,0,0,0,.049-.117.636.636,0,0,1,.622-.457c.34-.049.671-.1,1.011-.156s.661-.107.992-.156l.535-.088a.088.088,0,0,0,.078-.1c0-.146-.058-.292-.049-.438a.567.567,0,0,1,.058-.253c.1-.175.194-.35.292-.535a.242.242,0,0,1,.156-.117c.253-.068.515-.1.778-.146.311-.058.622-.1.933-.156.457-.078.924-.146,1.381-.224.438-.068.865-.156,1.3-.214s.846-.165,1.274-.185a1.573,1.573,0,0,1,.729.165c.2.1.389.214.593.321.369.185.729.389,1.089.593a1.415,1.415,0,0,0,1.031.175c.35-.058.7-.107,1.05-.156l1.089-.146c.428-.058.865-.117,1.293-.194.34-.058.69-.1,1.031-.146.408-.058.817-.126,1.225-.185.476-.068.953-.126,1.439-.194.36-.049.72-.117,1.079-.156.185-.019.36-.049.535-.078a.11.11,0,0,1,.107.029.49.49,0,0,1,.185.428,3.585,3.585,0,0,1-.486,1.215,2.306,2.306,0,0,1-.476.5c-.058.049-.126.078-.126.194a1.094,1.094,0,0,1-.107.331c-.535,1.459-1.07,2.907-1.6,4.366a2.89,2.89,0,0,1-.953,1.254c-.194.156-.36.34-.554.515a4.062,4.062,0,0,1-.72.486l-.2.117a.5.5,0,0,1-.69-.117,4.669,4.669,0,0,1-.506-.807,1,1,0,0,0-.438-.389c-.379-.194-.749-.4-1.138-.574-.292-.136-.574-.292-.865-.418s-.574-.3-.865-.438c-.272-.126-.545-.272-.827-.389a2.826,2.826,0,0,0-.5-.214.212.212,0,0,0-.214.039,3.108,3.108,0,0,1-.554.311.392.392,0,0,1-.593-.272,4.035,4.035,0,0,0-.107-.467,1.252,1.252,0,0,0-.69-.7c-.408-.185-.807-.379-1.206-.583a1.371,1.371,0,0,0-.895-.136c-.282.049-.564.088-.846.136-.263.049-.535.1-.8.136-.126.019-.253.019-.379.029-.088,0-.126.029-.126.126C86.979,52.7,86.969,52.78,86.969,52.9ZM99.4,50.8c-.107.019-.263.039-.408.058-.379.049-.749.107-1.128.156-.438.058-.875.107-1.313.165-.039.01-.078.019-.088.068a.4.4,0,0,0,.165.36,4.687,4.687,0,0,1,.486.263c.71.4,1.42.807,2.13,1.206.126.068.136.068.175-.078.136-.525.272-1.05.408-1.585C99.931,51.04,99.766,50.8,99.4,50.8Z" transform="translate(-82.448 -47.925)"/>
    </g>
</svg>
                        {:else if slotIndex === 3}
                            <!-- Фонарик -->
                            <svg data-v-29f6b6db class="modification__icon" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="22" height="22" viewBox="0 0 22 22">
        <path d="M11,0C8.957,0,5.5.386,5.5,1.833V4.39a4.112,4.112,0,0,0,.693,2.288l.756,1.133a2.289,2.289,0,0,1,.385,1.271V20.625C7.333,21.962,10.625,22,11,22s3.667-.038,3.667-1.375V9.083a2.289,2.289,0,0,1,.385-1.271l.756-1.133A4.112,4.112,0,0,0,16.5,4.39V1.833C16.5.386,13.043,0,11,0Zm.917,16.958a.458.458,0,0,1-.458.458h-.917a.458.458,0,0,1-.458-.458v-2.75a.458.458,0,0,1,.458-.458h.917a.458.458,0,0,1,.458.458ZM11,12.833a.917.917,0,1,1,.917-.917A.918.918,0,0,1,11,12.833ZM15.556,1.854c-.238.334-1.892.9-4.556.9a9.392,9.392,0,0,1-4.486-.8.487.487,0,0,1-.09-.111C6.581,1.513,8.258.917,11,.917c2.71,0,4.383.583,4.575.914C15.571,1.837,15.56,1.847,15.556,1.854Z"/>
</svg>
                        {:else if slotIndex === 4}
                            <!-- Глушитель -->
                            <svg data-v-29f6b6db class="modification__icon" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="22" height="22" viewBox="0 0 22 22">
    <g transform="translate(0 0.995)">
      <path d="M12.172,7.018a2.988,2.988,0,0,1,.3-1.259,4.93,4.93,0,0,1,.66-1.082,5.006,5.006,0,0,1,1.145-1.072,3.034,3.034,0,0,1,1.285-.538,1.606,1.606,0,0,1,1.069.15,2.983,2.983,0,0,1,.325.252c.293.235.575.483.863.723q.491.415.982.825c.138.116.281.23.412.356a.744.744,0,0,0,.315.192.616.616,0,0,1,.281.182,1.176,1.176,0,0,0,.323.228,1.141,1.141,0,0,1,.565.737,2.28,2.28,0,0,1,.044.6c0,.092-.022.162-.141.141a.065.065,0,0,0-.078.044c-.01.032.022.039.041.049a2.541,2.541,0,0,1,.58.432c.342.281.679.57,1.019.854s.672.57,1.006.854c.369.313.74.626,1.108.936q.6.5,1.193,1c.463.388.939.764,1.392,1.164.415.366.859.7,1.266,1.072.276.255.582.473.866.72.25.218.5.429.757.643.347.289.7.575,1.04.866.454.381.9.766,1.361,1.147.337.281.674.563,1.016.837.2.165.359.381.57.534a1.951,1.951,0,0,0,.162.126.559.559,0,0,1,.274.5,2.806,2.806,0,0,1-.192.832,3.747,3.747,0,0,1-.369.786,3.4,3.4,0,0,1-.381.529c-.187.218-.373.434-.577.635a3.678,3.678,0,0,1-.788.621,4.914,4.914,0,0,1-.81.376.721.721,0,0,1-.8-.238,3.211,3.211,0,0,0-.468-.393c-.281-.243-.553-.492-.834-.735s-.589-.514-.878-.776c-.364-.33-.735-.652-1.1-.985s-.769-.677-1.152-1.016c-.347-.308-.689-.621-1.036-.929-.366-.323-.735-.643-1.1-.965-.259-.23-.514-.466-.776-.694-.33-.286-.648-.582-.977-.868-.3-.262-.6-.531-.9-.791-.534-.456-1.036-.946-1.572-1.4-.4-.34-.781-.7-1.174-1.045-.252-.226-.507-.449-.764-.667a1.813,1.813,0,0,1-.2-.213c-.024-.027-.046-.051-.08-.022s-.022.053,0,.082c.068.073,0,.107-.051.119a2.013,2.013,0,0,1-.681.078,1.25,1.25,0,0,1-.912-.57,1.407,1.407,0,0,0-.243-.252,1,1,0,0,1-.247-.306.3.3,0,0,0-.063-.073c-.3-.255-.592-.524-.88-.791-.272-.252-.548-.5-.825-.749-.235-.213-.473-.424-.708-.638A1.405,1.405,0,0,1,12.172,7.018Z" transform="translate(-12.171 -3.042)"/>
    </g>
</svg>
                        {/if}
                    {/if}
                </div>
            {/each}
        </div>
    </div>
{/if}
            
            <!-- ✅ НИЖНЯЯ ПАНЕЛЬ: ВЕС, СОСТОЯНИЕ, ПОЛ -->
            <div class="row-block full-width align-center justify-between" data-v-29f6b6db>
<!-- ✅ ВЕС (БЕЗ ДЕЛЕНИЯ НА 1000, Т.К. УЖЕ В КГ) -->
    <div class="weight row-block align-center" data-v-29f6b6db>
        {#if _infoItem && typeof _infoItem.Weight === 'number'}

        {@const itemWeightKg = _infoItem.Weight} <!-- УЖЕ В КГ! -->
        {@const itemCount = infoItem.Count && typeof infoItem.Count === 'number' ? infoItem.Count : 1}
        {@const totalWeightKg = itemWeightKg * itemCount}
        
        <span class="weight__value" data-v-29f6b6db>
            {totalWeightKg < 0.01 && totalWeightKg > 0 ? '< 0.01' : totalWeightKg.toFixed(2)}
        </span>
        <span class="weight__icon" data-v-29f6b6db>kg</span>
    
{:else}

    <div class="weight row-block align-center" data-v-29f6b6db>
        <span class="weight__value" data-v-29f6b6db>0.00</span>
        <span class="weight__icon" data-v-29f6b6db>kg</span>
    </div>
{/if}    
</div>   
                <!-- ✅ СОСТОЯНИЕ (ДЛЯ БРОНИ, ОРУЖИЯ С ИЗНОСОМ) -->
                {#if hasCondition && infoItem.Data}
                    {@const maxHP = wMaxHP[infoItem.ItemId] || 100}
                    {@const currentHP = parseInt(infoItem.Data) || 0}
                    {@const condition = Math.floor((currentHP / maxHP) * 100)}
                    {@const conditionTag = condition >= 70 ? 'green-tag' : 
                                            condition >= 30 ? 'yellow-tag' : 
                                            condition >= 10 ? 'orange-tag' : 'red-tag'}
                    
                    <div data-v-29f6b6db class="deterioration row-block">
                        <span data-v-29f6b6db class="deterioration__title">Состояние:</span>
                        <div data-v-29f6b6db class="deterioration__value {conditionTag}">
                            {condition}%
                        </div>
                    </div>
                {/if}
                
                <!-- ✅ ПОЛ (ТОЛЬКО ДЛЯ ОДЕЖДЫ) -->
                {#if isClothes}
    {@const itemImageUrl = getPng(infoItem, _infoItem)}
    {@const isFemaleFromUrl = itemImageUrl.includes('/female/')}
    {@const isMaleFromUrl = itemImageUrl.includes('/male/')}
    
    {#if isFemaleFromUrl || isMaleFromUrl}
        <div data-v-29f6b6db class="item-properties align-center __gendered">
            <span data-v-29f6b6db class="gender-text">
                {isFemaleFromUrl ? 'Для женщин' : 'Для мужчин'}
            </span>
            <img data-v-29f6b6db 
                 src="https://cdn.majestic-files.com/public/master/static/img/inventory/{isFemaleFromUrl ? 'female' : 'male'}.svg" 
                 class="gender-icon" 
                 alt="">
        </div>
    {/if}
{/if}
            </div>
        </div>
    </div>
{/if}

<!-- ✅ ОКНО МОДИФИКАЦИЙ ОРУЖИЯ -->
<!-- ✅ ОКНО МОДИФИКАЦИЙ ОРУЖИЯ -->
<!-- ✅ ОКНО МОДИФИКАЦИЙ ОРУЖИЯ (ИСПОЛЬЗУЕТ weaponModifications!) -->
{#if showModificationsModal && modificationsWeaponItem}
    <div data-v-29f6b6db 
         class="modal-window column-block modifications-modal"
         style="left: {modModalX}px; top: {modModalY}px;"
         on:mousedown={handleModModalMouseDown}
         on:mouseenter={() => useInventoryArea = true}
         on:mouseleave={() => useInventoryArea = false}>
        
        <!-- ✅ ЗАГОЛОВОК -->
        <div data-v-29f6b6db class="modal-header full-width align-center"
             style="cursor: {isDraggingModModal ? 'grabbing' : 'grab'};">
            <span data-v-29f6b6db class="modal-header__title">Редактировать модификацию</span>
        </div>
        
        <!-- ✅ КНОПКА ЗАКРЫТИЯ -->
        <div data-v-29f6b6db class="close-blockinv flex-block" 
             on:click|stopPropagation={() => {
                 showModificationsModal = false;
                 modificationsWeaponItem = null;
                 // ✅ ОЧИЩАЕМ МОДИФИКАЦИИ
                  weaponModifications = Array(5).fill(null). map(() => ({ ... clearSlot }));
                 executeClient("client.inventory.OtherClose");
             }}
             on:keypress={() => {}}>
            <svg data-v-29f6b6db xmlns="http://www.w3.org/2000/svg" width="17.122" height="17.121" viewBox="0 0 17.122 17.121">
            <g transform="translate(1.061 1.061)" stroke="#fff">
                <path d="M0,0,15,15" fill="none" stroke-linecap="square" stroke-miterlimit="10" stroke-width="2"/>
                <path d="M6.929,0l-15,15" transform="translate(8.071)" stroke-linecap="square" stroke-miterlimit="10" stroke-width="2"/>
            </g>
            </svg>
        </div>
        
        <!-- ✅ ТЕЛО МОДАЛЬНОГО ОКНА -->
        <div data-v-29f6b6db class="modal-body row-block full-height full-width modifications">
            <div data-v-29f6b6db class="wallet-container column-block">
                <div data-v-29f6b6db class="line">
                    
                    {#each [0, 1, 2, 3, 4] as slotIndex}
                        {@const item = weaponModifications[slotIndex]}
                        {@const hasItem = item && item.ItemId && item.ItemId !== 0}
                        
                        <div data-v-29f6b6db class="slot mod-slot" 
                             data-mod-type={slotIndex}
                             on:mouseenter={(event) => handleModSlotMouseEnter(event, slotIndex)}
                             on:mouseleave={handleModSlotMouseLeave}
                             on:mousedown|stopPropagation={(event) => handleModSlotMouseDown(event, slotIndex)}
                             on:mouseup={handleModSlotMouseUp}>
                            
                            {#if hasItem}
                                <div class="picture-handler" data-v-29f6b6db>
                                    <div class="picture-handler__picture" data-v-29f6b6db
                                        style="background-image: url({getPng(item, itemsInfo[item.ItemId])})">
                                    </div>
                                </div>
                            {:else}
                                <!-- ✅ ИКОНКИ ДЛЯ ПУСТЫХ СЛОТОВ -->
                                {#if slotIndex === 0}
                                <svg data-v-29f6b6db class="modification__icon" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="22" height="22" viewBox="0 0 22 22">
                                    <path d="M47.587,19.558c.009-.9,0-1.8,0-2.7,0-.937,0-1.879-.009-2.807,0-.438,0-.438-.438-.455-.053,0-.133.009-.141-.053-.04-.274-.243-.239-.429-.243-.831-.018-1.662-.009-2.493-.022-.88-.018-1.76-.128-2.639-.08h-.1a.582.582,0,0,1-.274-.049,1.924,1.924,0,0,0-1.105-.08.33.33,0,0,0-.225.124c-.071.115-.164.111-.27.106a1.24,1.24,0,0,0-.181,0c-.19.009-.252.093-.225.274a1.015,1.015,0,0,1,.009.164c0,.561-.044,1.119-.062,1.68l-.066,1.83c-.027.685-.049,1.375-.066,2.06s-.049,1.375-.1,2.06c-.044.61-.119,1.22-.2,1.83-.115.875-.292,1.737-.491,2.6-.354,1.521-.867,3-1.322,4.487-.2.672-.416,1.344-.615,2.016-.093.309-.062.345.239.447.1.035.225.018.279.119a.249.249,0,0,0,.177.128c.128.04.252.08.38.115.044.013.1.035.137-.013.057-.066.119-.035.181-.018,1.189.345,2.378.685,3.563,1.039.15.044.367.027.411.261,0,.018.044.027.071.035.332.1.663.2.995.3.053.018.1.027.137-.022.057-.08.124-.044.19-.022.305.1.606.2.911.3a.548.548,0,0,0,.27.049,2.231,2.231,0,0,0,.663-.23.23.23,0,0,0,.141-.234A1.171,1.171,0,0,1,44.979,34c.093-.248.168-.5.243-.752.389-1.344.827-2.675,1.194-4.027.23-.853.433-1.715.6-2.582.181-.951.323-1.9.416-2.865.066-.668.124-1.335.155-2,.027-.508,0-1.012.035-1.521C47.636,20.023,47.587,19.788,47.587,19.558Z" transform="translate(-30.863 -13.029)"/>
                                </svg>
                                {:else if slotIndex === 1}
                                    <svg data-v-29f6b6db class="modification__icon" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="22" height="22" viewBox="0 0 22 22">
                                        <g transform="translate(0 2.088)">
                                        <path d="M18.2,21.618c0-.283.009-.5,0-.717-.018-.34-.012-.681-.024-1.024,0-.094,0-.185.006-.279a.235.235,0,0,1,.191-.246c.295-.094.589-.191.887-.279a.144.144,0,0,0,.112-.134c0-.049-.07-.052-.115-.07l-.82-.328-.043-.018c-.2-.085-.216-.115-.188-.331.076-.562.152-1.121.228-1.683a.246.246,0,0,1,.131-.21.6.6,0,0,0,.3-.516,1.374,1.374,0,0,0-.112-.687,5.439,5.439,0,0,1-.346-1.358,8.777,8.777,0,0,1-.085-1.322,8.825,8.825,0,0,1,.106-1.094,8.018,8.018,0,0,1,.3-1.255,5.679,5.679,0,0,1,.547-1.215,4.6,4.6,0,0,1,.69-.881,1.123,1.123,0,0,1,.674-.331c.267-.039.538-.082.805-.128a.919.919,0,0,1,.444.073c.331.106.659.222.99.328l1.118.365,1.756.571c.155.052.307.112.462.152a.51.51,0,0,1,.355.289A9.248,9.248,0,0,1,27,10.356a8.817,8.817,0,0,1,.322,1.343,6.275,6.275,0,0,1,.079.787,2.415,2.415,0,0,1,.012.611c-.012.052.039.076.091.079a4.5,4.5,0,0,1,.829.112c.122.03.216-.055.328-.07a.4.4,0,0,0,.307-.213,9.784,9.784,0,0,1,.65-.814c.112-.137.225-.276.343-.407a.929.929,0,0,1,.362-.2c.377-.158.753-.313,1.13-.471a15.037,15.037,0,0,1,1.938-.7c.665-.173,1.328-.358,1.984-.562.283-.088.568-.161.851-.237a.5.5,0,0,1,.258,0c.325.085.647.179.969.276q.766.232,1.531.456a.938.938,0,0,1,.483.313c.182.216.377.425.568.635a.4.4,0,0,1,.109.289c0,.377.006.753,0,1.127-.006.334.046.668.027,1-.006.128-.015.255-.03.386a.358.358,0,0,1-.2.249c-.532.358-1.069.714-1.6,1.069-.161.106-.319.216-.477.328a.444.444,0,0,0-.194.45,3.851,3.851,0,0,1,.058.7.807.807,0,0,0,.021.225.3.3,0,0,1-.128.27,2.218,2.218,0,0,1-1.045.516,3.364,3.364,0,0,1-.541.115.128.128,0,0,0-.131.158c.006.064,0,.128,0,.194.018.857-.033,1.713-.024,2.573a.218.218,0,0,1-.164.237c-.477.158-.951.325-1.425.489l-1.191.41-1.318.456c-.556.191-1.112.386-1.671.577-.544.185-1.088.368-1.628.556-.441.152-.878.307-1.315.459q-.816.283-1.628.559c-.592.2-1.188.4-1.78.611a.461.461,0,0,1-.371-.015c-.289-.131-.577-.261-.866-.4a.217.217,0,0,1-.082-.362s0-.006.006-.009c.349-.231.228-.589.243-.911a.08.08,0,0,0-.073-.088,2.105,2.105,0,0,1-.626-.307,3.053,3.053,0,0,0-.55-.234,1.425,1.425,0,0,1-.261-.106.416.416,0,0,0-.4-.012c-.5.167-1.006.322-1.51.483a.376.376,0,0,1-.264-.018c-.146-.061-.295-.122-.444-.173a.465.465,0,0,1-.331-.437C18.188,22.326,18.2,21.937,18.2,21.618Zm3.67-6.5a.148.148,0,0,0,0-.134,1.159,1.159,0,0,1-.027-.383c0-.052.012-.112.088-.067a1.85,1.85,0,0,0,.295.115c.419.152.838.3,1.261.447.322.109.647.207.966.319.082.027.115.015.146-.058.061-.146.125-.292.185-.437a6.939,6.939,0,0,0,.407-1.713,8.8,8.8,0,0,0,.021-1.167,3.086,3.086,0,0,0-.052-.529,6.251,6.251,0,0,0-.337-1.279.584.584,0,0,0-.425-.407c-.34-.079-.674-.191-1.012-.295-.358-.109-.714-.222-1.069-.337q-.611-.2-1.221-.419c-.07-.024-.1-.018-.131.049a6.39,6.39,0,0,0-.3.781,6.438,6.438,0,0,0-.225,1.337c-.012.249-.006.5,0,.747a6.9,6.9,0,0,0,.155,1.2,6.7,6.7,0,0,0,.422,1.358.758.758,0,0,0,.51.516.241.241,0,0,1,.213.191A.444.444,0,0,0,21.867,15.114Z" transform="translate(-18.169 -7.506)"/>
                                        </g>
                                    </svg>
                                {:else if slotIndex === 2}
                                    <svg data-v-29f6b6db class="modification__icon" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="22" height="22" viewBox="0 0 22 22">
                                        <g transform="translate(0 6.237)">
                                        <path d="M86.969,52.9a1.847,1.847,0,0,1-.078.593,1.057,1.057,0,0,1-.35.5c-.049.039-.088.088-.136.126-.068.058-.107.107-.01.185s.01.156-.058.2a1.178,1.178,0,0,1-.651.272,2.657,2.657,0,0,1-.622.01,1.078,1.078,0,0,1-.788-.476,8.645,8.645,0,0,1-.807-1.313.609.609,0,0,0-.311-.292,1.469,1.469,0,0,1-.632-.758.682.682,0,0,1,.029-.681.6.6,0,0,0,.049-.117.636.636,0,0,1,.622-.457c.34-.049.671-.1,1.011-.156s.661-.107.992-.156l.535-.088a.088.088,0,0,0,.078-.1c0-.146-.058-.292-.049-.438a.567.567,0,0,1,.058-.253c.1-.175.194-.35.292-.535a.242.242,0,0,1,.156-.117c.253-.068.515-.1.778-.146.311-.058.622-.1.933-.156.457-.078.924-.146,1.381-.224.438-.068.865-.156,1.3-.214s.846-.165,1.274-.185a1.573,1.573,0,0,1,.729.165c.2.1.389.214.593.321.369.185.729.389,1.089.593a1.415,1.415,0,0,0,1.031.175c.35-.058.7-.107,1.05-.156l1.089-.146c.428-.058.865-.117,1.293-.194.34-.058.69-.1,1.031-.146.408-.058.817-.126,1.225-.185.476-.068.953-.126,1.439-.194.36-.049.72-.117,1.079-.156.185-.019.36-.049.535-.078a.11.11,0,0,1,.107.029.49.49,0,0,1,.185.428,3.585,3.585,0,0,1-.486,1.215,2.306,2.306,0,0,1-.476.5c-.058.049-.126.078-.126.194a1.094,1.094,0,0,1-.107.331c-.535,1.459-1.07,2.907-1.6,4.366a2.89,2.89,0,0,1-.953,1.254c-.194.156-.36.34-.554.515a4.062,4.062,0,0,1-.72.486l-.2.117a.5.5,0,0,1-.69-.117,4.669,4.669,0,0,1-.506-.807,1,1,0,0,0-.438-.389c-.379-.194-.749-.4-1.138-.574-.292-.136-.574-.292-.865-.418s-.574-.3-.865-.438c-.272-.126-.545-.272-.827-.389a2.826,2.826,0,0,0-.5-.214.212.212,0,0,0-.214.039,3.108,3.108,0,0,1-.554.311.392.392,0,0,1-.593-.272,4.035,4.035,0,0,0-.107-.467,1.252,1.252,0,0,0-.69-.7c-.408-.185-.807-.379-1.206-.583a1.371,1.371,0,0,0-.895-.136c-.282.049-.564.088-.846.136-.263.049-.535.1-.8.136-.126.019-.253.019-.379.029-.088,0-.126.029-.126.126C86.979,52.7,86.969,52.78,86.969,52.9ZM99.4,50.8c-.107.019-.263.039-.408.058-.379.049-.749.107-1.128.156-.438.058-.875.107-1.313.165-.039.01-.078.019-.088.068a.4.4,0,0,0,.165.36,4.687,4.687,0,0,1,.486.263c.71.4,1.42.807,2.13,1.206.126.068.136.068.175-.078.136-.525.272-1.05.408-1.585C99.931,51.04,99.766,50.8,99.4,50.8Z" transform="translate(-82.448 -47.925)"/>
                                        </g>
                                    </svg>
                                {:else if slotIndex === 3}
                                    <svg data-v-29f6b6db class="modification__icon" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="22" height="22" viewBox="0 0 22 22">
                                        <path d="M11,0C8.957,0,5.5.386,5.5,1.833V4.39a4.112,4.112,0,0,0,.693,2.288l.756,1.133a2.289,2.289,0,0,1,.385,1.271V20.625C7.333,21.962,10.625,22,11,22s3.667-.038,3.667-1.375V9.083a2.289,2.289,0,0,1,.385-1.271l.756-1.133A4.112,4.112,0,0,0,16.5,4.39V1.833C16.5.386,13.043,0,11,0Zm.917,16.958a.458.458,0,0,1-.458.458h-.917a.458.458,0,0,1-.458-.458v-2.75a.458.458,0,0,1,.458-.458h.917a.458.458,0,0,1,.458.458ZM11,12.833a.917.917,0,1,1,.917-.917A.918.918,0,0,1,11,12.833ZM15.556,1.854c-.238.334-1.892.9-4.556.9a9.392,9.392,0,0,1-4.486-.8.487.487,0,0,1-.09-.111C6.581,1.513,8.258.917,11,.917c2.71,0,4.383.583,4.575.914C15.571,1.837,15.56,1.847,15.556,1.854Z"/>
                                </svg>
                                {:else if slotIndex === 4}
                                    <svg data-v-29f6b6db class="modification__icon" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="22" height="22" viewBox="0 0 22 22">
                                    <g transform="translate(0 0.995)">
                                    <path d="M12.172,7.018a2.988,2.988,0,0,1,.3-1.259,4.93,4.93,0,0,1,.66-1.082,5.006,5.006,0,0,1,1.145-1.072,3.034,3.034,0,0,1,1.285-.538,1.606,1.606,0,0,1,1.069.15,2.983,2.983,0,0,1,.325.252c.293.235.575.483.863.723q.491.415.982.825c.138.116.281.23.412.356a.744.744,0,0,0,.315.192.616.616,0,0,1,.281.182,1.176,1.176,0,0,0,.323.228,1.141,1.141,0,0,1,.565.737,2.28,2.28,0,0,1,.044.6c0,.092-.022.162-.141.141a.065.065,0,0,0-.078.044c-.01.032.022.039.041.049a2.541,2.541,0,0,1,.58.432c.342.281.679.57,1.019.854s.672.57,1.006.854c.369.313.74.626,1.108.936q.6.5,1.193,1c.463.388.939.764,1.392,1.164.415.366.859.7,1.266,1.072.276.255.582.473.866.72.25.218.5.429.757.643.347.289.7.575,1.04.866.454.381.9.766,1.361,1.147.337.281.674.563,1.016.837.2.165.359.381.57.534a1.951,1.951,0,0,0,.162.126.559.559,0,0,1,.274.5,2.806,2.806,0,0,1-.192.832,3.747,3.747,0,0,1-.369.786,3.4,3.4,0,0,1-.381.529c-.187.218-.373.434-.577.635a3.678,3.678,0,0,1-.788.621,4.914,4.914,0,0,1-.81.376.721.721,0,0,1-.8-.238,3.211,3.211,0,0,0-.468-.393c-.281-.243-.553-.492-.834-.735s-.589-.514-.878-.776c-.364-.33-.735-.652-1.1-.985s-.769-.677-1.152-1.016c-.347-.308-.689-.621-1.036-.929-.366-.323-.735-.643-1.1-.965-.259-.23-.514-.466-.776-.694-.33-.286-.648-.582-.977-.868-.3-.262-.6-.531-.9-.791-.534-.456-1.036-.946-1.572-1.4-.4-.34-.781-.7-1.174-1.045-.252-.226-.507-.449-.764-.667a1.813,1.813,0,0,1-.2-.213c-.024-.027-.046-.051-.08-.022s-.022.053,0,.082c.068.073,0,.107-.051.119a2.013,2.013,0,0,1-.681.078,1.25,1.25,0,0,1-.912-.57,1.407,1.407,0,0,0-.243-.252,1,1,0,0,1-.247-.306.3.3,0,0,0-.063-.073c-.3-.255-.592-.524-.88-.791-.272-.252-.548-.5-.825-.749-.235-.213-.473-.424-.708-.638A1.405,1.405,0,0,1,12.172,7.018Z" transform="translate(-12.171 -3.042)"/>
                                    </g>
                                </svg>
                                {/if}
                            {/if}
                            
                            <div data-v-29f6b6db class="highlight"></div>
                        </div>
                    {/each}
                    
                </div>
            </div>
        </div>
    </div>
{/if}
<!-- ✅ КОНТЕКСТНОЕ МЕНЮ (РАСШИРЕННОЕ, БЕЗ translateText) -->
<!-- ✅ КОНТЕКСТНОЕ МЕНЮ (РАСШИРЕННОЕ, БЕЗ translateText) -->
{#if selectItem.use === stageItem.useItem && ItemStack === -1}
<div bind:this={boxPopup} class="click-block" data-v-29f6b6db 
    style="top: {fixOutToY ($coords.y)}px; left: {fixOutToX ($coords.x + 10)}px;"
    on:mouseenter={e => useInventoryArea = true} 
    on:mouseleave={e => useInventoryArea = false}>
    
    {#if OtherInfo.Id == otherType.Tent && OtherInfo.IsMyTent && selectItem.arrayName === "other"}
        <!-- ✅ ОСОБЫЙ СЛУЧАЙ: ПАЛАТКА -->
        <div class="button" data-v-29f6b6db on:keypress={() => {}} on:click={onTransfer}>
            Забрать
        </div>
    {:else}
        {@const _infoItem = window.getItem(selectItem.ItemId)}
        
        <!-- ✅ 1. ИСПОЛЬЗОВАТЬ / НАДЕТЬ / СНЯТЬ / ВЗЯТЬ -->
        {#if getItemsUse(selectItem) !== false}
            <div class="button" data-v-29f6b6db on:keypress={() => {}} on:click={onUseItem}>
                {#if selectItem.arrayName === "accessories"}
                    Снять
                {:else if _infoItem.functionType === ItemType.Clothes}
                    Надеть
                {:else if selectItem.arrayName !== "fastSlots" && (_infoItem.functionType === ItemType.Weapons || _infoItem.functionType === ItemType.MeleeWeapons) && ItemToWeaponHash[selectItem.ItemId] && wComponents[ItemToWeaponHash[selectItem.ItemId]] && wComponents[ItemToWeaponHash[selectItem.ItemId]].Components}
                    Модификации
                {:else if selectItem.arrayName !== "fastSlots" && (_infoItem.functionType === ItemType.Weapons || _infoItem.functionType === ItemType.MeleeWeapons)}
                    Взять
                {:else if _infoItem.functionType === ItemType.Alco || _infoItem.functionType === ItemType.Water}
                    Выпить
                {:else if _infoItem.functionType === ItemType.Eat}
                    Съесть
                {:else if selectItem.ItemId == 41 && OtherInfo.Id === otherType.None}
                    Открыть
                {:else if (OtherInfo.Id === otherType.wComponents || OtherInfo.Id === otherType.Key) && OtherSqlId && Number(OtherSqlId) === Number(selectItem.SqlId)}
                    Закрыть
                {:else if (selectItem.ItemId == 225 || selectItem.ItemId == 229) && OtherInfo.Id === otherType.None}
                    Курить
                {:else if _infoItem.functionType === ItemType.Cases}
                    Открыть
                {:else if selectItem.ItemId == ItemId.SimCard}
                    Вставить
                {:else}
                    Использовать
                {/if}
            </div>
        {/if}
        
        <!-- ✅ 2. ДОСТАТЬ В РУКИ (ТОЛЬКО ДЛЯ ОРУЖИЯ В ИНВЕНТАРЕ) -->
        {#if selectItem.arrayName === "inventory" && 
             (_infoItem.functionType === ItemType.Weapons || _infoItem.functionType === ItemType.MeleeWeapons)}
            <div class="button" data-v-29f6b6db on:keypress={() => {}} on:click={() => {
                executeClient("client.gamemenu.inventory.takehands", selectItem.arrayName, selectItem.index);
                itemNoUse(39);
            }}>
                Достать
            </div>
        {/if}
        
        <!-- ✅ 3. В БЫСТРЫЙ СЛОТ / ИЗ БЫСТРОГО СЛОТА (ТОЛЬКО ДЛЯ ИНВЕНТАРЯ!) -->
        <!-- ✅ 3. В БЫСТРЫЙ СЛОТ / ИЗ БЫСТРОГО СЛОТА (ТОЛЬКО ДЛЯ ИНВЕНТАРЯ!) -->
{#if selectItem.arrayName === "inventory" && 
     (_infoItem.functionType === ItemType.Weapons || 
      _infoItem.functionType === ItemType.MeleeWeapons ||
      selectItem.ItemId === 6 || selectItem.ItemId === 7 || selectItem.ItemId === 8 ||
      selectItem.ItemId === 3 || selectItem.ItemId === 5 || selectItem.ItemId === 9 ||
      selectItem.ItemId === 10 || selectItem.ItemId === 1 || selectItem.ItemId === 280 ||
      selectItem.ItemId === 225 || selectItem.ItemId === 226 || selectItem.ItemId === 227 ||
      selectItem.ItemId === 228 || selectItem.ItemId === 229 || selectItem.ItemId === 230 ||
      selectItem.ItemId === 231 || selectItem.ItemId === 232 || selectItem.ItemId === 233 ||
      selectItem.ItemId === 388 || selectItem.ItemId === 389 || selectItem.ItemId === ItemId.VehicleNumber)}
    
    <!-- ✅ ПРОВЕРЯЕМ: УЖЕ В БЫСТРЫХ СЛОТАХ? -->
    {@const fastSlotIndex = (() => {
        console.log(`[FAST SLOT CHECK] Looking for SqlId=${selectItem.SqlId}, ItemId=${selectItem.ItemId}`);
        
        for (let i = 0; i < ItemsData["fastSlots"].length; i++) {
            const slot = ItemsData["fastSlots"][i];
            if (!slot || !slot.ItemId || slot.ItemId === 0) continue;
            
            console.log(`[FAST SLOT CHECK] Slot ${i}:`, slot);
            
            // ✅ ПРОВЕРЯЕМ ПО SqlId (УНИКАЛЬНО ДЛЯ КАЖДОГО ПРЕДМЕТА)
            if (slot.SqlId === selectItem.SqlId && slot.ItemId === selectItem.ItemId) {
                console.log(`[FAST SLOT CHECK] Found in slot ${i}!`);
                return i;
            }
        }
        
        console.log(`[FAST SLOT CHECK] Not found in fast slots`);
        return -1;
    })()}
    
    <!-- ✅ ЕСЛИ ПРЕДМЕТ УЖЕ В БЫСТРЫХ СЛОТАХ - ПОКАЗЫВАЕМ "ИЗ БЫСТРОГО СЛОТА" -->
    {#if fastSlotIndex !== -1}
        <div class="button" data-v-29f6b6db on:keypress={() => {}} on:click={() => {
            console.log(`[FAST SLOT] Removing item from slot ${fastSlotIndex}`);
            
            // ✅ ОТПРАВЛЯЕМ СЕРВЕРУ КОМАНДУ УДАЛЕНИЯ
            executeClient("client.gamemenu.inventory.fastslot.remove", fastSlotIndex);
            
            // ✅ ОЧИЩАЕМ СЛОТ ЛОКАЛЬНО
            ItemsData["fastSlots"][fastSlotIndex] = { ...clearSlot };
            
            window.notificationAdd(4, 9, "Предмет убран из быстрых слотов", 3000);
            itemNoUse(43);
        }}>
            Из быстрого слота
        </div>
    {:else}
        <!-- ✅ ЕСЛИ ПРЕДМЕТА НЕТ В БЫСТРЫХ СЛОТАХ - ПОКАЗЫВАЕМ "В БЫСТРЫЙ СЛОТ" -->
        <div class="button" data-v-29f6b6db on:keypress={() => {}} on:click={() => {
            // ✅ ИЩЕМ СВОБОДНЫЙ СЛОТ
            let freeSlot = -1;
            for (let i = 0; i < 3; i++) {
                if (!ItemsData["fastSlots"][i] || !ItemsData["fastSlots"][i].ItemId || ItemsData["fastSlots"][i].ItemId === 0) {
                    freeSlot = i;
                    break;
                }
            }
            
            if (freeSlot === -1) {
                window.notificationAdd(4, 9, "Все быстрые слоты заняты!", 3000);
                itemNoUse(41);
                return;
            }
            
            // ✅ ОТПРАВЛЯЕМ НА СЕРВЕР
            executeClient("client.gamemenu.inventory.move", "inventory", selectItem.index, "fastSlots", freeSlot);
            
            // ✅ СОХРАНЯЕМ ЛОКАЛЬНО (НЕ ЖДЁМ ОТВЕТА СЕРВЕРА!)
            const item = ItemsData["inventory"][selectItem.index];
            ItemsData["fastSlots"][freeSlot] = {
                ...item,
                SqlId: item.SqlId,      // ✅ ВАЖНО!
                ItemId: item.ItemId,
                Index: selectItem.index, // Исходный индекс в инвентаре
                Count: item.Count,
                Data: item.Data,
                use: true
            };
            
            console.log(`[FAST SLOT ADD] Added item with SqlId=${item.SqlId}:`, ItemsData["fastSlots"][freeSlot]);
            
            window.notificationAdd(4, 9, "Предмет добавлен в быстрый слот!", 3000);
            itemNoUse(38);
        }}>
            В быстрый слот
        </div>
    {/if}
{/if}
        
        <!-- ✅ ОСТАЛЬНЫЕ КНОПКИ БЕЗ ИЗМЕНЕНИЙ... -->
        
        <!-- ✅ 4. ПОСТАВИТЬ -->
       
        
        <!-- ✅ 5. ПОЛОЖИТЬ В РЮКЗАК / ПЕРЕДАТЬ / ВЗЯТЬ -->
        {#if OtherInfo.Id != otherType.Tent && 
             (OtherInfo.Id > otherType.None || 
              (maxSlotBackpack > 0 && ItemsData["backpack"].length) || 
              tradeInfo.Active === true) && 
             selectItem.arrayName !== "fastSlots" && 
             selectItem.arrayName !== "accessories"}
            <div class="button" data-v-29f6b6db on:keypress={() => {}} on:click={onTransfer}>
                {#if selectItem.arrayName === "inventory" && maxSlotBackpack > 0 && ItemsData["backpack"].length && OtherInfo.Id === otherType.None && !tradeInfo.Active}
                    Положить в рюкзак
                {:else if selectItem.arrayName === "other" || selectItem.arrayName === "backpack" || selectItem.arrayName === "trade"}
                    Взять
                {:else}
                    Передать
                {/if}
            </div>
        {:else if OtherInfo.Id == otherType.Tent && OtherInfo.IsMyTent && 
                  (selectItem.arrayName === "inventory" || selectItem.arrayName === "backpack")}
            <div class="button" data-v-29f6b6db on:keypress={() => {}} on:click={onTransfer}>
                Продать
            </div>
        {/if}
        
        <!-- ✅ 6. РАЗДЕЛИТЬ -->
        {#if selectItem.Count > 1 && selectItem.arrayName !== "fastSlots"}
            <div class="button" data-v-29f6b6db on:keypress={() => {}} on:click={e => {
                ItemStack = 0; 
                rangeslidercreate(selectItem.Count - 1);
            }}>
                Разделить
            </div>
        {/if}
        
        <!-- ✅ 7. ВЫБРОСИТЬ -->
        {#if getDropItem(selectItem.arrayName, selectItem.ItemId) !== false}
            <div class="button" data-v-29f6b6db on:keypress={() => {}} on:click={onDropItem}>
                Выбросить
            </div>
        {/if}
    {/if}
</div>
{/if}
<!-- ✅ ОКНО РАЗДЕЛЕНИЯ ПРЕДМЕТА -->
<!-- ✅ ОКНО РАЗДЕЛЕНИЯ ПРЕДМЕТА -->
{#if ItemStack !== -1 && selectItem.use === stageItem.useItem}
<div data-v-29f6b6db 
     class="modal-window column-block split"
     style="position: fixed; 
            left: {modalWasDragged ? modalPositionX : $coords.x}px; 
            top: {modalWasDragged ? modalPositionY : $coords.y}px; 
            cursor: {isDraggingModal ? 'grabbing' : 'default'}; 
            z-index: 10000;"
     on:mouseenter={() => useInventoryArea = true}
     on:mouseleave={() => useInventoryArea = false}
     on:mousedown={handleModalMouseDown}>  <!-- ✅ ПЕРЕНОСИМ СЮДА! -->
    
    <!-- ✅ ЗАГОЛОВОК (БЕЗ on:mousedown) -->
    <div data-v-29f6b6db 
         class="modal-header full-width align-center">
        <span data-v-29f6b6db class="modal-header__title">
            {#if ItemStack === 0}
                Разделитель
            {:else if ItemStack === 1}
                Выбросить
            {:else if ItemStack === 2}
                Передать
            {/if}
        </span>
    </div>
    
    <!-- ✅ КНОПКА ЗАКРЫТИЯ -->
    <div data-v-29f6b6db class="close-blockinv flex-block" 
         on:click={() => itemNoUse(36)}
         on:keypress={() => {}}>
        <svg data-v-29f6b6db xmlns="http://www.w3.org/2000/svg" width="17.122" height="17.121" viewBox="0 0 17.122 17.121">
            <g transform="translate(1.061 1.061)" stroke="#fff">
                <path d="M0,0,15,15" fill="none" stroke-linecap="square" stroke-miterlimit="10" stroke-width="2"/>
                <path d="M6.929,0l-15,15" transform="translate(8.071)" stroke-linecap="square" stroke-miterlimit="10" stroke-width="2"/>
            </g>
        </svg>
    </div>
    
    <!-- ✅ ТЕЛО ФОРМЫ -->
    <form data-v-29f6b6db class="modal-body full-height full-width column-block"
          on:submit|preventDefault={() => {}}>
        
        <!-- ✅ СЛАЙДЕР -->
        <div data-v-29f6b6db class="rangeinv align-center full-width row-block">
            <input data-v-29f6b6db
                   type="text"
                   maxlength="4"
                   class="rangeinv__input"
                   bind:value={StackValue}
                   on:input={(e) => handleInputStackChange(e.target.value)}
                   on:blur={onBlurStack}>
            
            <div data-v-29f6b6db class="rangeinv-cover">
                <div data-v-29f6b6db
                     id="stack"
                     class="vue-slider vue-slider-ltr rangeinv__slider rangeinv__slider"
                     style="padding: 7px; width: 18.5vh; height: 4px;">
                </div>
            </div>
        </div>
        
        <!-- ✅ КНОПКА ПРИНЯТЬ -->
        <input data-v-29f6b6db
               type="button"
               class="modal-body__button full-width"
               value="Принять"
               on:click={() => {
                   if (ItemStack === 0 || ItemStack === 1 || ItemStack === 2) {
                       onStack();
                   }
               }}>
    </form>
</div>
{/if}
<!-- Добавьте этот код после секции "ОКНО РАЗДЕЛЕНИЯ ПРЕДМЕТА" -->

        <!-- Drag & Drop Handler -->
        <!-- Drag & Drop Handler -->
{#if (selectItem.use === stageItem.move)}
    {@const itemConfig = itemsInfo[selectItem.ItemId] || {}}
    {@const baseWidth = itemConfig.Width || 1}
    {@const baseHeight = itemConfig.Height || 1}
    {@const itemWidth = selectItem.isTurn ? baseHeight : baseWidth}
    {@const itemHeight = selectItem.isTurn ? baseWidth : baseHeight}
    
    <div class="handler" data-v-29f6b6db 
        style="width: {5.02778 * itemWidth}vh; 
               height: {5.02778 * itemHeight}vh; 
               left: {$coords.x - selectItem.offsetInElementX}px; 
               top: {$coords.y - selectItem.offsetInElementY}px;">
        <div class="handler_static" data-v-29f6b6db>
            <div class="picture-handler" data-v-29f6b6db 
                style="width: {5.02778 * itemWidth}vh; 
                       height: {5.02778 * itemHeight}vh;
                       position: relative;
                       overflow: hidden;">
                
                <div class="picture-handler__picture" data-v-29f6b6db
                    style="background-image: url({getPng(selectItem, window.getItem(selectItem.ItemId))});
                           width: {5.02778 * baseWidth}vh; 
                           height: {5.02778 * baseHeight}vh;
                           position: absolute;
                           top: 50%;
                           left: 50%;
                           transform-origin: center center;
                           {selectItem.isTurn ? 'transform: translate(-50%, -50%) rotate(90deg);' : 'transform: translate(-50%, -50%);'}">
                </div>
            </div>
        </div>
    </div>
{/if}

        <!-- Main Inventory -->
       
    <div class="main-inventory full-width full-height" data-v-29f6b6db>
        <div class="inventory row-block align-center" data-v-29f6b6db>
            <div style="position: fixed; top: 10px; right: 10px; background: rgba(0,0,0,0.8); color: white; padding: 15px; z-index: 9999; font-size: 12px;">
        <h3>DEBUG INFO</h3>
        <p>InventoryWeight: {inventoryWeight}</p>
        <p>MaxInventoryWeight: {maxInventoryWeight}</p>
        <p>BackpackWeight: {backpackWeight}</p>
        <p>MaxBackpackWeight: {maxBackpackWeight}</p>
        <hr>
        <p>charData.InventoryWeight: {$charData?.InventoryWeight}</p>
        <p>charData.MaxInventoryWeight: {$charData?.MaxInventoryWeight}</p>
    </div>
            <!-- ========================================= -->
            <!-- LEFT COLUMN: ОКРУЖЕНИЕ + РЮКЗАК -->
            <!-- ========================================= -->
            <div class="column-block" data-v-29f6b6db>
                
                <!-- ОКРУЖЕНИЕ (19 линий x 5 слотов = 95 слотов) -->
                <div class="inventory-col environment column-block {maxSlotBackpack > 0 ? 'environment-with-backpack' : ''}" data-v-29f6b6db>
                    <div class="border-block full-width full-height" data-v-29f6b6db></div>
                    <div class="general-title align-center justify-between" data-v-29f6b6db>
                        <span data-v-29f6b6db>Окружение</span>
                        <div class="row-block align-center" data-v-29f6b6db>
                            <img data-v-29f6b6db src="https://cdn.majestic-files.com/public/master/static/img/inventory/accepted.png" alt="">
                        </div>
                    </div>
                    <div class="scrollable-wrapper" data-v-29f6b6db id="vs-7-0" 
                         on:mouseenter={e => mainInventoryArea = true} 
                         on:mouseleave={e => mainInventoryArea = false}>
                        <div class="scroll-up" data-v-29f6b6db></div>
                        <div class="scroll-down" data-v-29f6b6db></div>
                        <div class="container full-width" data-v-29f6b6db>
                            <div class="inv-block" data-v-29f6b6db>
                                {#each Array(19) as _, lineIndex}
                                    <div class="line" data-v-29f6b6db>
                                        {#each Array(6) as _, slotIndex}
                                            {@const index = lineIndex * 6 + slotIndex}
                                            {@const item = ItemsData["other"][index]}
                                            
                                            <div class="slot" data-v-29f6b6db
                                                track-by="$index"
                                                data-position="7" 
                                                data-id="0" 
                                                data-x={slotIndex} 
                                                data-y={lineIndex}
                                                on:mousedown={(event) => handleMouseDown(event, index, "other")}
                                                on:mouseup={handleSlotMouseUp}
                                                on:mouseenter={(event) => handleSlotMouseEnter(event, index, "other")}
                                                on:mouseleave={handleSlotMouseLeave}>
                                                
                                                <!-- ✅ FILL - ПРЕДМЕТ -->
                                                {#if item && item.ItemId != 0 && item.Index === index}

                                                    <div class="fill active" data-v-29f6b6db
                                                         style="{getItemSize(item)}">
                                                         
                                                        <div class="item-properties row-block" data-v-29f6b6db>
                                                            {#if item.fraction}
                                                                <img src="{cdn}/img/inventory/job.png" alt="" class="fraction-icon" />
                                                            {:else if item.gender != null}
                                                                <img src="{cdn}/img/inventory/{item.gender ? 'female' : 'male'}.svg" alt="" class="fraction-icon" />
                                                            {/if}
                                                        </div>
                                                <div class="picture-handler" data-v-29f6b6db
             style="{getItemSize(item)}; position: relative; overflow: hidden;">
            
            <!-- ✅ КАРТИНКА: РАЗМЕРЫ 4x2, ПОВЕРНУТА НА 90° -->
            <div class="picture-handler__picture" data-v-29f6b6db
                data-picture="true"
                style="background-image: url({getPng(item, itemsInfo[item.ItemId])}); 
                       {getItemSize(item, true)}
                       position: absolute; 
                       top: 50%; 
                       left: 50%; 
                       transform-origin: center center;
                       {item.isTurn || item.IsTurn ? 'transform: translate(-50%, -50%) rotate(90deg);' : 'transform: translate(-50%, -50%);'}">
            </div>
        </div>
                                                                                                        
                                                        {#if item.Count > 1}
                                                            <div class="amount" data-v-29f6b6db>{item.Count}</div>
                                                        {/if}
                                                        
                                                        <div class="fill-border" data-v-29f6b6db></div>
                                                    </div>
                                                {/if}
                                                
                                                <!-- ✅ HIGHLIGHT И BORDER - ВСЕГДА -->
                                                <div class="highlight" data-v-29f6b6db></div>
                                                <div class="border" data-v-29f6b6db></div>
                                            </div>
                                        {/each}
                                    </div>
                                {/each}
                            </div>
                        </div>
                    </div>
                </div>

                <!-- РЮКЗАК (6 линий x 5 слотов = 30 слотов) -->
                {#if maxSlotBackpack > 0 && ItemsData["backpack"].length}
                    <div class="inventory-col backpack" data-v-29f6b6db>
                        <div class="general-title row-block align-center justify-between" data-v-29f6b6db style="padding-top: 0px;">
                            <div class="row-block full-width align-center" data-v-29f6b6db>
                                <span data-v-29f6b6db>Рюкзак</span>
                            
                            <div class="weight row-block align-center" data-v-29f6b6db>
        <span data-v-29f6b6db class="weight__text-current" style="color: {backpackColor}">
            {backpackWeight.toFixed(1)}
        </span>
        <span data-v-29f6b6db class="weight__text-max align-center">
            &nbsp;/ {maxBackpackWeight} <span data-v-29f6b6db class="kg">kg</span>
        </span>
    </div></div>
                            <div data-v-29f6b6db="">
                               <div data-v-29f6b6db="" class="take-off align-center" 
                     on:click={() => {
                         // ✅ ПРОВЕРЯЕМ, ЕСТЬ ЛИ РЮКЗАК НА ПЕРСОНАЖЕ
                         const bagItem = ItemsData["accessories"][clothes.Bags.slotId];
                         
                         if (!bagItem || !bagItem.ItemId || bagItem.ItemId === 0) {
                             window.notificationAdd(4, 9, "На вас нет рюкзака!", 3000);
                             return;
                         }
                         
                         // ✅ ПРОВЕРЯЕМ, ПУСТОЙ ЛИ РЮКЗАК
                         const hasItemsInBackpack = ItemsData["backpack"].some(item => 
                             item && item.ItemId && item.ItemId !== 0
                         );
                         
                         if (hasItemsInBackpack) {
                             window.notificationAdd(4, 9, "Сначала освободите рюкзак!", 3000);
                             return;
                         }
                         
                         // ✅ ИЩЕМ СВОБОДНОЕ МЕСТО В ИНВЕНТАРЕ
                         const freeSlot = findFreeSlot("inventory", bagItem);
                         
                         if (freeSlot === -1) {
                             window.notificationAdd(4, 9, "Недостаточно места в инвентаре!", 3000);
                             return;
                         }
                         
                         // ✅ СНИМАЕМ РЮКЗАК (ПЕРЕМЕЩАЕМ В ИНВЕНТАРЬ)
                         executeClient("client.gamemenu.inventory.move", "accessories", clothes.Bags.slotId, "inventory", freeSlot);
                     }}>
                                   <span data-v-29f6b6db="" class="take-off__title">Снять</span>
                                </div>
                            </div>
                        </div>
                        <div class="scrollable-wrapper" data-v-29f6b6db id="vs-18" 
                             on:mouseenter={e => mainInventoryArea = true} 
                             on:mouseleave={e => mainInventoryArea = false}>
                            <div class="scroll-up" data-v-29f6b6db></div>
                            <div class="scroll-down" data-v-29f6b6db></div>
                            <div class="container full-width" data-v-29f6b6db>
                                <div class="inv-block" data-v-29f6b6db>
                                    {#each Array(8) as _, lineIndex}
                                        <div class="line" data-v-29f6b6db>
                                            {#each Array(6) as _, slotIndex}
                                                {@const index = lineIndex * 6 + slotIndex}
                                                {@const item = ItemsData["backpack"][index]}
                                                
                                                <div class="slot" data-v-29f6b6db
                                                    track-by="$index"
                                                    data-position="18" 
                                                    data-id="132" 
                                                    data-x={slotIndex} 
                                                    data-y={lineIndex}
                                                    on:mousedown={(event) => handleMouseDown(event, index, "backpack")}
                                                    on:mouseup={handleSlotMouseUp}
                                                    on:mouseenter={(event) => handleSlotMouseEnter(event, index, "backpack")}
                                                    on:mouseleave={handleSlotMouseLeave}>
                                                    
                                                    <!-- ✅ FILL - ПРЕДМЕТ -->
                                                    {#if item && item.ItemId != 0 && !item.isDragging && !item.Data?.startsWith("placeholder_")}

                                                        <div class="fill active" data-v-29f6b6db
                                                             style="{getItemSize(item)}">
                                                             
                                                            <div class="item-properties row-block" data-v-29f6b6db>
                                                                {#if item.fraction}
                                                                    <img src="{cdn}/img/inventory/job.png" alt="" class="fraction-icon" />
                                                                {:else if item.gender != null}
                                                                    <img src="{cdn}/img/inventory/{item.gender ? 'female' : 'male'}.svg" alt="" class="fraction-icon" />
                                                                {/if}
                                                            </div>

                                                            <div class="picture-handler" data-v-29f6b6db
                                                                 style="{getItemSize(item, true)}">
                                                                <div class="picture-handler__picture" data-v-29f6b6db
                                                                    data-picture="true"
                                                                    style="background-image: url({getPng(item, itemsInfo[item.ItemId])})">
                                                                </div>
                                                            </div>
                                                            
                                                            {#if item.Count > 1}
                                                                <div class="amount" data-v-29f6b6db>{item.Count}</div>
                                                            {/if}
                                                            
                                                            <div class="fill-border" data-v-29f6b6db></div>
                                                        </div>
                                                    {/if}
                                                    
                                                    <div class="highlight" data-v-29f6b6db></div>
                                                    <div class="border" data-v-29f6b6db></div>
                                                </div>
                                            {/each}
                                        </div>
                                    {/each}
                                </div>
                            </div>
                        </div>
                    </div>
                {/if}
            </div>

            <!-- ========================================= -->
            <!-- CENTER COLUMN: ПЕРСОНАЖ -->
            <!-- ========================================= -->
            <div class="inventory-col character full-height" data-v-29f6b6db>
                <div class="name full-width" data-v-29f6b6db>{selectCharData.Name}</div>
                
                <div class="player-data column-block" data-v-29f6b6db>
                    <div class="row-block full-width justify-between" data-v-29f6b6db>
                        <!-- Left Clothes -->
                        <!-- Left Clothes -->
<div class="objects column-block" data-v-29f6b6db>
    {#each [
        {type: "head", slotId: clothes.Hats.slotId},
        {type: "tops", slotId: clothes.Tops.slotId},
        {type: "decals", slotId: clothes.Accessories.slotId},
        {type: "legs", slotId: clothes.Legs.slotId}
    ] as cloth}
        {@const hasItem = ItemsData["accessories"][cloth.slotId] && ItemsData["accessories"][cloth.slotId].ItemId != 0}
        
        <div class="cloth" data-v-29f6b6db
            track-by="$index"
            data-position="9" 
            data-tag={cloth.type}
            style="{hasItem ? '' : `background-image: url('https://cdn.majestic-files.com/public/master/static/img/inventory/clothes/v2/${cloth.type}.svg');`}"
            on:mousedown={(event) => handleMouseDown(event, cloth.slotId, "accessories")}
            on:mouseup={handleSlotMouseUp}
            on:mouseenter={(event) => handleSlotMouseEnter(event, cloth.slotId, "accessories")}
            on:mouseleave={handleSlotMouseLeave}>
            
            {#if hasItem}
                <div class="picture-handler" data-v-29f6b6db>
                    <div class="picture-handler__picture" data-v-29f6b6db
                        style="background-image: url({getPng(ItemsData['accessories'][cloth.slotId], itemsInfo[ItemsData['accessories'][cloth.slotId].ItemId])})">
                    </div>
                </div>
            {/if}
            
            <div class="highlight" data-v-29f6b6db></div>
            <div class="border" data-v-29f6b6db></div>
        </div>
    {/each}
</div>

<!-- Gender Image -->
<div class="gender" data-v-29f6b6db>
    <img data-v-29f6b6db
        class="full-width full-height" 
        src="https://cdn.majestic-files.com/public/master/static/img/inventory/{$charGender ? 'female' : 'male'}.png" 
        alt="">
</div>

<!-- Right Clothes -->
<div class="objects column-block" data-v-29f6b6db>
    {#each [
        {type: "glasses", slotId: clothes.Glasses.slotId},
        {type: "undershirts", slotId: clothes.Undershirts.slotId},
        {type: "shoes", slotId: clothes.Shoes.slotId},
        {type: "gloves", slotId: clothes.Torsos.slotId}
    ] as cloth}
        {@const hasItem = ItemsData["accessories"][cloth.slotId] && ItemsData["accessories"][cloth.slotId].ItemId != 0}
        
        <div class="cloth" data-v-29f6b6db
            track-by="$index"
            data-position="9" 
            data-tag={cloth.type}
            style="{hasItem ? '' : `background-image: url('https://cdn.majestic-files.com/public/master/static/img/inventory/clothes/v2/${cloth.type}.svg');`}"
            on:mousedown={(event) => handleMouseDown(event, cloth.slotId, "accessories")}
            on:mouseup={handleSlotMouseUp}
            on:mouseenter={(event) => handleSlotMouseEnter(event, cloth.slotId, "accessories")}
            on:mouseleave={handleSlotMouseLeave}>
            
            {#if hasItem}
                <div class="picture-handler" data-v-29f6b6db>
                    <div class="picture-handler__picture" data-v-29f6b6db
                        style="background-image: url({getPng(ItemsData['accessories'][cloth.slotId], itemsInfo[ItemsData['accessories'][cloth.slotId].ItemId])})">
                    </div>
                </div>
            {/if}
            
            <div class="highlight" data-v-29f6b6db></div>
            <div class="border" data-v-29f6b6db></div>
        </div>
    {/each}
</div>
                    </div>

                    <!-- Bottom Accessories -->
                    <div class="objects bottom row-block" data-v-29f6b6db>
    {#each [
        {type: "watches", slotId: clothes.Watches.slotId},
        {type: "masks", slotId: clothes.Masks.slotId},
        {type: "ears", slotId: clothes.Ears.slotId},
        {type: "bracelets", slotId: clothes.Bracelets.slotId},
        {type: "accessories", slotId: clothes.Suit.slotId}
    ] as cloth}
        {@const hasItem = ItemsData["accessories"][cloth.slotId] && ItemsData["accessories"][cloth.slotId].ItemId != 0}
        
        <div class="slot cloth" data-v-29f6b6db
            track-by="$index"
            data-position="9" 
            data-tag={cloth.type}
            style="{hasItem ? '' : `background-image: url('https://cdn.majestic-files.com/public/master/static/img/inventory/clothes/v2/${cloth.type}.svg');`}"
            on:mousedown={(event) => handleMouseDown(event, cloth.slotId, "accessories")}
            on:mouseup={handleSlotMouseUp}
            on:mouseenter={(event) => handleSlotMouseEnter(event, cloth.slotId, "accessories")}
            on:mouseleave={handleSlotMouseLeave}>
            
            {#if hasItem}
                <div class="picture-handler" data-v-29f6b6db>
                    <div class="picture-handler__picture" data-v-29f6b6db
                        style="background-image: url({getPng(ItemsData['accessories'][cloth.slotId], itemsInfo[ItemsData['accessories'][cloth.slotId].ItemId])})">
                    </div>
                </div>
            {/if}
            
            <div class="highlight" data-v-29f6b6db></div>
            <div class="border" data-v-29f6b6db></div>
        </div>
    {/each}
</div>

                    <!-- Indicators -->
                    <div class="indicators column-block" data-v-29f6b6db>
                        <div class="state-block align-center hunger" data-v-29f6b6db>
                            <img data-v-29f6b6db class="state-block__picture" src="https://cdn.majestic-files.com/public/master/static/img/inventory/indicators/v2/hunger.svg" alt="">
                            <progress data-v-29f6b6db class="state-block__progress" max="100" min="0" value={eat}></progress>
                            <div class="state-block__value" data-v-29f6b6db>{eat}</div>
                        </div>
                        <div class="state-block align-center water" data-v-29f6b6db>
                            <img data-v-29f6b6db class="state-block__picture" src="https://cdn.majestic-files.com/public/master/static/img/inventory/indicators/v2/water.svg" alt="">
                            <progress data-v-29f6b6db class="state-block__progress" max="100" min="0" value={water}></progress>
                            <div class="state-block__value" data-v-29f6b6db>{water}</div>
                        </div>
                        <div class="state-block align-center health" data-v-29f6b6db>
                            <img data-v-29f6b6db class="state-block__picture" src="https://cdn.majestic-files.com/public/master/static/img/inventory/indicators/v2/health.svg" alt="">
                            <progress data-v-29f6b6db class="state-block__progress" max="100" min="0" value={hp}></progress>
                            <div class="state-block__value" data-v-29f6b6db>{hp}</div>
                        </div>
                    </div>
                </div>

<!-- Weapon Slot -->
<div class="weapon" data-v-29f6b6db>
    <div class="slot cloth" data-v-29f6b6db 
         data-position="19"
         on:mouseenter={(event) => handleSlotMouseEnter(event, 19, "accessories")}
         on:mouseleave={handleSlotMouseLeave}
         on:mousedown={(event) => {
             // ✅ РАЗРЕШАЕМ ПЕРЕТАСКИВАНИЕ ОРУЖИЯ ИЗ ЭТОГО СЛОТА
             if (activeWeapon && activeWeapon.ItemId && activeWeapon.ItemId !== 0) {
                 handleMouseDown(event, activeWeapon.Index, "inventory");
             }
         }}
         on:mouseup={handleSlotMouseUp}>
        {#if activeWeapon && activeWeapon.ItemId && activeWeapon.ItemId !== 0}
            {@const weaponInfo = itemsInfo[activeWeapon.ItemId]}
            
            <!-- ✅ РАСЧЁТ СОСТОЯНИЯ ОРУЖИЯ -->
            {@const weaponCondition = activeWeapon.Data && wMaxHP[activeWeapon.ItemId] 
                ? Math.floor((parseInt(activeWeapon.Data) / wMaxHP[activeWeapon.ItemId]) * 100) 
                : 100}
            {@const weaponTag = weaponCondition >= 70 ? 'green-tag' : 
                               weaponCondition >= 30 ? 'yellow-tag' : 
                               weaponCondition >= 10 ? 'orange-tag' : 'red-tag'}
            
            <!-- ✅ ВЕС ОРУЖИЯ -->
            {@const weaponWeight = weaponInfo ? ((weaponInfo.Weight || 0) / 1000).toFixed(1) : 0}
            
            <div class="picture-handler" data-v-29f6b6db>
                <div class="picture-handler__picture" data-v-29f6b6db
                    style="background-image: url({getPng(activeWeapon, weaponInfo)})">
                </div>
                
                <!-- ✅ СОСТОЯНИЕ ОРУЖИЯ -->
                {#if wMaxHP[activeWeapon.ItemId]}
                    <div data-v-29f6b6db class="item-state {weaponTag}">{weaponCondition}%</div>
                {/if}
                
                <!-- ✅ ВЕС ОРУЖИЯ -->
                <div data-v-29f6b6db class="weight row-block align-center">
                    <div data-v-29f6b6db class="weight__value">{weaponWeight}</div>
                    <div data-v-29f6b6db class="kg">kg</div>
                </div>
                
                <!-- ✅ КНОПКИ УПРАВЛЕНИЯ -->
                <div data-v-29f6b6db class="control-buttons full-width full-height row-block">
                    <!-- ✅ КНОПКА 2: ВЕРНУТЬ В ИНВЕНТАРЬ -->
                    <div data-v-29f6b6db class="control-button"
     on:click|stopPropagation={() => {
         if (!activeWeapon || !activeWeapon.ItemId) {
             console.error('[WEAPON RETURN] No active weapon!');
             return;
         }
         
         console.log('[WEAPON RETURN] Returning weapon to inventory');
         
         // ✅ ИСПОЛЬЗУЕМ КОМАНДУ "УБРАТЬ ОРУЖИЕ"
         executeClient("client.weapon.takeweapon");
     }}>
                        <svg data-v-29f6b6db xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 20 20">
                            <path d="M10.593,4.742A11.181,11.181,0,0,0,.007,15.859V20l1.481-3.449a10.788,10.788,0,0,1,9.1-5.95V15.34l9.4-7.684L10.593,0Z"/>
                        </svg>
                    </div>
                </div>
            </div>
        {:else}
            <!-- ✅ ПУСТОЙ СЛОТ -->
            <div class="empty-item full-width full-height column-block" data-v-29f6b6db>
                <img data-v-29f6b6db class="empty-item__picture" src="https://cdn.majestic-files.com/public/master/static/img/inventory/empty/weapon.svg" alt="">
                <span data-v-29f6b6db class="empty-item__title">Активные предметы</span>
            </div>
        {/if}
        
        <div class="highlight" data-v-29f6b6db></div>
        <div class="border" data-v-29f6b6db></div>
    </div>
</div>

<!-- Armor & Bag -->
<div class="wear-items row-block full-width justify-between" data-v-29f6b6db>
    <!-- БРОНЕЖИЛЕТ -->
    <div class="wear-item" data-v-29f6b6db>
        {#if ItemsData["accessories"][clothes.Armors.slotId]}
            {@const armorItem = ItemsData["accessories"][clothes.Armors.slotId]}
            {@const hasArmor = armorItem && armorItem.ItemId != 0}
            
            <!-- ✅ РАСЧЁТ СОСТОЯНИЯ БРОНИ -->
            {@const armorCondition = hasArmor && armorItem.Data ? parseInt(armorItem.Data) : 0}
            {@const armorTag = armorCondition >= 70 ? 'green-tag' : 
                               armorCondition >= 30 ? 'yellow-tag' : 
                               armorCondition >= 10 ? 'orange-tag' : 'red-tag'}
            
            <!-- ✅ ВЕС БРОНИ (В КГ) -->
            {@const armorWeight = hasArmor ? ((itemsInfo[armorItem.ItemId]?.Weight || 0) / 1000).toFixed(1) : 0}
            
            <div class="slot cloth" data-v-29f6b6db 
                data-position="9" 
                data-tag="armor"
                on:mousedown={(event) => handleMouseDown(event, clothes.Armors.slotId, "accessories")}
                on:mouseup={handleSlotMouseUp}
                on:mouseenter={(event) => handleSlotMouseEnter(event, clothes.Armors.slotId, "accessories")}
                on:mouseleave={handleSlotMouseLeave}>
                
                {#if hasArmor}
                    <div class="picture-handler" data-v-29f6b6db>
                        <div class="picture-handler__picture" data-v-29f6b6db
                            style="background-image: url({getPng(armorItem, itemsInfo[armorItem.ItemId])})">
                        </div>
                        
                        <!-- ✅ СОСТОЯНИЕ БРОНИ -->
                        <div data-v-29f6b6db class="item-state {armorTag}">{armorCondition}%</div>
                        
                        <!-- ✅ ВЕС БРОНИ -->
                        <div data-v-29f6b6db class="weight row-block align-center">
                            <div data-v-29f6b6db class="weight__value">{armorWeight}</div>
                            <div data-v-29f6b6db class="kg">kg</div>
                        </div>
                        
                        <!-- ✅ КНОПКИ УПРАВЛЕНИЯ -->
                        <div data-v-29f6b6db class="control-buttons full-width full-height row-block">
                            <!-- ✅ КНОПКА 1: ВЫБРОСИТЬ БРОНЮ -->
                            <div data-v-29f6b6db class="control-button" 
                                 on:click|stopPropagation={() => {
                                     executeClient("client.gamemenu.inventory.drop", "accessories", clothes.Armors.slotId);
                                 }}>
                                
                                <svg data-v-29f6b6db xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 20 20">
                                    <path d="M16.563,2.5H13.125V1.875A1.875,1.875,0,0,0,11.25,0H8.75A1.875,1.875,0,0,0,6.875,1.875V2.5H3.438A1.563,1.563,0,0,0,1.875,4.063v1.25a.625.625,0,0,0,.625.625h15a.625.625,0,0,0,.625-.625V4.063A1.563,1.563,0,0,0,16.563,2.5ZM8.125,1.875A.626.626,0,0,1,8.75,1.25h2.5a.626.626,0,0,1,.625.625V2.5H8.125Z"/>
                                    <path d="M3.061,7.188a.2.2,0,0,0-.2.2l.516,10.822A1.873,1.873,0,0,0,5.254,20h9.491a1.873,1.873,0,0,0,1.873-1.786l.516-10.822a.2.2,0,0,0-.2-.2ZM12.5,8.75a.625.625,0,0,1,1.25,0v8.125a.625.625,0,0,1-1.25,0Zm-3.125,0a.625.625,0,0,1,1.25,0v8.125a.625.625,0,0,1-1.25,0Zm-3.125,0a.625.625,0,0,1,1.25,0v8.125a.625.625,0,0,1-1.25,0Z"/>
                                </svg>
                            </div>
                            
                            <!-- ✅ КНОПКА 2: ВЕРНУТЬ В ИНВЕНТАРЬ -->
                            <div data-v-29f6b6db class="control-button"
                                 on:click|stopPropagation={() => {
                                     const freeSlot = findFreeSlot("inventory", armorItem);
                                     
                                     if (freeSlot === -1) {
                                         window.notificationAdd(4, 9, "Недостаточно места в инвентаре!", 3000);
                                         return;
                                     }
                                     
                                     executeClient("client.gamemenu.inventory.move", "accessories", clothes.Armors.slotId, "inventory", freeSlot);
                                 }}>
                                <svg data-v-29f6b6db xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 20 20">
                                    <path d="M10.593,4.742A11.181,11.181,0,0,0,.007,15.859V20l1.481-3.449a10.788,10.788,0,0,1,9.1-5.95V15.34l9.4-7.684L10.593,0Z"/>
                                </svg>
                            </div>
                        </div>
                    </div>
                {:else}
                    <div class="empty-item full-width full-height column-block" data-v-29f6b6db>
                        <img data-v-29f6b6db class="empty-item__picture" src="https://cdn.majestic-files.com/public/master/static/img/inventory/empty/armour.svg" alt="">
                        <span data-v-29f6b6db class="empty-item__title">Бронежилет</span>
                    </div>
                {/if}
                
                <div class="highlight" data-v-29f6b6db></div>
                <div class="border" data-v-29f6b6db></div>
            </div>
        {/if}
    </div>
    
    <!-- РЮКЗАК -->
    <div class="wear-item" data-v-29f6b6db>
        {#if ItemsData["accessories"][clothes.Bags.slotId]}
            {@const bagItem = ItemsData["accessories"][clothes.Bags.slotId]}
            {@const hasBag = bagItem && bagItem.ItemId != 0}
            
            <div class="slot cloth" data-v-29f6b6db 
                data-position="9" 
                data-tag="bags"
                on:mousedown={(event) => handleMouseDown(event, clothes.Bags.slotId, "accessories")}
                on:mouseup={handleSlotMouseUp}
                on:mouseenter={(event) => handleSlotMouseEnter(event, clothes.Bags.slotId, "accessories")}
                on:mouseleave={handleSlotMouseLeave}>
                
                {#if hasBag}
                    <div class="picture-handler" data-v-29f6b6db>
                        <div class="picture-handler__picture" data-v-29f6b6db
                            style="background-image: url({getPng(bagItem, itemsInfo[bagItem.ItemId])})">
                        </div>
                        
                        <!-- ✅ КНОПКИ УПРАВЛЕНИЯ РЮКЗАКОМ -->
                        <div data-v-29f6b6db class="control-buttons full-width full-height row-block">
                            <!-- ✅ КНОПКА 1: ВЫБРОСИТЬ РЮКЗАК -->
                            <div data-v-29f6b6db class="control-button"
                                 on:click|stopPropagation={() => {
                                     executeClient("client.gamemenu.inventory.drop", "accessories", clothes.Bags.slotId);
                                 }}>
                                <svg data-v-29f6b6db xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 20 20">
                                    <path d="M16.563,2.5H13.125V1.875A1.875,1.875,0,0,0,11.25,0H8.75A1.875,1.875,0,0,0,6.875,1.875V2.5H3.438A1.563,1.563,0,0,0,1.875,4.063v1.25a.625.625,0,0,0,.625.625h15a.625.625,0,0,0,.625-.625V4.063A1.563,1.563,0,0,0,16.563,2.5ZM8.125,1.875A.626.626,0,0,1,8.75,1.25h2.5a.626.626,0,0,1,.625.625V2.5H8.125Z"/>
                                    <path d="M3.061,7.188a.2.2,0,0,0-.2.2l.516,10.822A1.873,1.873,0,0,0,5.254,20h9.491a1.873,1.873,0,0,0,1.873-1.786l.516-10.822a.2.2,0,0,0-.2-.2ZM12.5,8.75a.625.625,0,0,1,1.25,0v8.125a.625.625,0,0,1-1.25,0Zm-3.125,0a.625.625,0,0,1,1.25,0v8.125a.625.625,0,0,1-1.25,0Zm-3.125,0a.625.625,0,0,1,1.25,0v8.125a.625.625,0,0,1-1.25,0Z"/>
                                </svg>
                            </div>
                            
                            <!-- ✅ КНОПКА 2: ВЕРНУТЬ В ИНВЕНТАРЬ -->
                            <div data-v-29f6b6db class="control-button"
                                 on:click|stopPropagation={() => {
                                     const freeSlot = findFreeSlot("inventory", bagItem);
                                     
                                     if (freeSlot === -1) {
                                         window.notificationAdd(4, 9, "Недостаточно места в инвентаре!", 3000);
                                         return;
                                     }
                                     
                                     executeClient("client.gamemenu.inventory.move", "accessories", clothes.Bags.slotId, "inventory", freeSlot);
                                 }}>
                                
                                <svg data-v-29f6b6db xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 20 20">
                                    <path d="M10.593,4.742A11.181,11.181,0,0,0,.007,15.859V20l1.481-3.449a10.788,10.788,0,0,1,9.1-5.95V15.34l9.4-7.684L10.593,0Z"/>
                                </svg>
                            </div>
                        </div>
                    </div>
                {:else}
                    <div class="empty-item full-width full-height column-block" data-v-29f6b6db>
                        <img data-v-29f6b6db class="empty-item__picture" src="https://cdn.majestic-files.com/public/master/static/img/inventory/empty/backpack.svg" alt="">
                        <span data-v-29f6b6db class="empty-item__title">Рюкзак</span>
                    </div>
                {/if}
                
                <div class="highlight" data-v-29f6b6db></div>
                <div class="border" data-v-29f6b6db></div>
            </div>
        {/if}
    </div>
</div>
                <!-- Fast Slots -->
                <div class="fast-slots row-block justify-between" data-v-29f6b6db>
    {#each ItemsData["fastSlots"] as item, index}
        <div class="fast-slot" data-v-29f6b6db
            track-by="$index"
            data-position="20"
            on:mouseenter={(event) => handleSlotMouseEnter(event, index, "fastSlots")}
            on:mouseleave={handleSlotMouseLeave}>
            
            {#if item && item.ItemId != 0 && !item.isDragging && !item.Data?.startsWith("placeholder_")}

                <div class="picture-handler" data-v-29f6b6db>
                    <div class="picture-handler__picture" data-v-29f6b6db
                        style="background-image: url({getPng(item, itemsInfo[item.ItemId])})">
                    </div>
                    
                    <!-- ✅ КНОПКА УПРАВЛЕНИЯ (КАК У АКТИВНОГО ОРУЖИЯ) -->
                    <div data-v-29f6b6db class="control-buttons full-width full-height row-block">
    <div data-v-29f6b6db class="control-button"
         on:click|stopPropagation={() => {
             console.log(`[FAST SLOT] Removing item from slot ${index}`);
             
             // ✅ ВАРИАНТ 1: ПРОСТО УДАЛЯЕМ (БЕЗ ПЕРЕМЕЩЕНИЯ В ИНВЕНТАРЬ)
             executeClient("client.gamemenu.inventory.fastslot.remove", index);
             
             // ✅ ОЧИЩАЕМ СЛОТ ЛОКАЛЬНО
             ItemsData["fastSlots"][index] = { ...clearSlot };
             
             window.notificationAdd(4, 9, "Предмет убран из быстрых слотов", 3000);
             itemNoUse(43);
         }}>
        <!-- ✅ ИКОНКА "УДАЛИТЬ" -->
        <svg data-v-29f6b6db xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 20 20">
            <path d="M10.593,4.742A11.181,11.181,0,0,0,.007,15.859V20l1.481-3.449a10.788,10.788,0,0,1,9.1-5.95V15.34l9.4-7.684L10.593,0Z"/>
        </svg>
    </div>
</div>
                </div>
            {/if}
            
            <div class="fast-slot__text-index" data-v-29f6b6db>{index + 1}</div>
            <div class="border" data-v-29f6b6db></div>
        </div>
    {/each}
</div>
            </div>

            <!-- ========================================= -->
            <!-- RIGHT COLUMN: ИНВЕНТАРЬ -->
            <!-- ========================================= -->
            <div class="inventory-col" data-v-29f6b6db>
                <div class="general-title row-block align-center justify-between" data-v-29f6b6db>
                    <span data-v-29f6b6db>Инвентарь</span>
                    <div class="row-block align-center" data-v-29f6b6db>
                        <div class="weight row-block align-center" data-v-29f6b6db>
            <span data-v-29f6b6db class="weight__text-current" style="color: {inventoryColor}">
                {inventoryWeight.toFixed(1)}
            </span>
            <span data-v-29f6b6db class="weight__text-max align-center">
                &nbsp;/ {trunkMaxWeight} <span data-v-29f6b6db class="kg">kg</span>
            </span>
        </div>
        <div class="close-blockinv flex-block" data-v-29f6b6db on:click={onExit}>
            <svg data-v-29f6b6db xmlns="http://www.w3.org/2000/svg" width="17.122" height="17.121" viewBox="0 0 17.122 17.121">
  <g transform="translate(1.061 1.061)" stroke="#fff">
    <path d="M0,0,15,15" fill="none" stroke-linecap="square" stroke-miterlimit="10" stroke-width="2"/>
    <path d="M6.929,0l-15,15" transform="translate(8.071)" stroke-linecap="square" stroke-miterlimit="10" stroke-width="2"/>
  </g>
</svg>
        </div>
    </div>
                </div>
                
                <div class="scrollable-wrapper" data-v-29f6b6db id="vs-1" 
                     on:mouseenter={e => mainInventoryArea = true} 
                     on:mouseleave={e => mainInventoryArea = false}>
                    <div class="scroll-up" data-v-29f6b6db></div>
                    <div class="scroll-down" data-v-29f6b6db></div>
                    <div class="container full-width" data-v-29f6b6db>
                        <div class="inv-block" data-v-29f6b6db>
                            {#each Array(17) as _, lineIndex}
                                <div class="line" data-v-29f6b6db>
                                    {#each Array(6) as _, slotIndex}
                                        {@const index = lineIndex * 6 + slotIndex}
                                        {@const item = ItemsData["inventory"][index]}
                                        
                                        <div class="slot" data-v-29f6b6db
                                            track-by="$index"
                                            data-position="1" 
                                            data-x={slotIndex} 
    data-y={lineIndex}
    on:mousedown={(event) => handleMouseDown(event, index, "inventory")}
    on:mouseup={handleSlotMouseUp}
    on:mouseenter={(event) => handleSlotMouseEnter(event, index, "inventory")}
    on:mouseleave={handleSlotMouseLeave}>
                                            
                                            <!-- ✅ FILL - ПРЕДМЕТ -->
                                            {#if item && item.ItemId != 0 && !item.isDragging && !item.Data?.startsWith("placeholder_")}

                                                <div class="fill active" data-v-29f6b6db
                                                     style="{getItemSize(item)}">
                                                     
                                                    <div class="item-properties row-block" data-v-29f6b6db>
                                                        {#if item.fraction}
                                                            <img src="{cdn}/img/inventory/job.png" alt="" class="fraction-icon" />
                                                        {:else if item.gender != null}
                                                            <img src="{cdn}/img/inventory/{item.gender ? 'female' : 'male'}.svg" alt="" class="fraction-icon" />
                                                        {/if}
                                                    </div>

                                                    <div class="picture-handler" data-v-29f6b6db
                                                         style="{getItemSize(item, true)}">
                                                        <div class="picture-handler__picture" data-v-29f6b6db
                                                            data-picture="true"
                                                            style="background-image: url({getPng(item, itemsInfo[item.ItemId])})">
                                                        </div>
                                                    </div>
                                                    
                                                    {#if item.Count > 1}
                                                        <div class="amount" data-v-29f6b6db>{item.Count}</div>
                                                    {/if}
                                                    
                                                    <div class="fill-border" data-v-29f6b6db></div>
                                                </div>
                                            {/if}
                                            
                                            <!-- ✅ HIGHLIGHT И BORDER - ВСЕГДА -->
                                            <div class="highlight" data-v-29f6b6db></div>
                                            <div class="border" data-v-29f6b6db></div>
                                        </div>
                                    {/each}
                                </div>
                            {/each}
                        </div>
                    </div>
                </div>
            </div>

        </div>
    </div>

    </div>
    {/if}
    {/if}