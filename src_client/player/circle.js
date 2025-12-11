const callRemote = mp.events.callRemote;
const call = mp.events.call;
const callRemoteUnreliable = mp.events.callRemoteUnreliable;
const browsers = mp.browsers;
const _callRemote = mp._events.callRemote;
const _call = mp._events.call;
global.circleOpen = false;

let selectCategory = "";

const categoryData = {
	[translateText("Игрок")]: ["bailRelease", "lawyerFree", "showDocs", "giveMoney", "greet", "createParty", "kickFromParty", "inviteToParty", "showMedLic", "showArmyLic", "showLic", "showParamedicLic", "showDrugsLic", "showDiploma", "showLawyerDocument", "requestDanceBattle", "giftPoster", "giftPresent", "protectionMonument", "requestThrowDice", "moveInApartment", "moveInHouse", "halloweenBite", "halloweenStake", "rescue", "rps_build_start"],
	
	[translateText("Документы")]: ["passport", "licenses", "idcard", "badge", "lspdbadge", "fibbadge"],
	
	[translateText("Машина")]: ["putFromHandToTrunk", "lockDoors", "setupNumber", "takeOffNumber", "hood", "trunk", "trunkItems", "cancelRent", "setGpsTracker", "removeGpsTracker", "setBikeSafetyBelt", "setVehDoorTazer", "setVehSignaling", "setVehAutopilot", "setVehHingedNumberplate", "getInTrunk", "throwFromTrunk", "repairCar", "replaceBattery", "repalceOil", "replaceLock", "pushCar", "vehDuplicateKey", "changeVehLock", "pullPlayers", "fireExtinguisherLoad", "drillDoorLock"],
	
	[translateText("Взаимодействия")]: ["handshake", "tinter", "givemoney", "tradehouse", "tradebiz", "tradecar", "vmuted", "ETERNAL_DICE", "ETERNAL_EXCHANGE-PROPS"],
	
	[translateText("Парные анимации")]: ["kiss", "hug", "five", "slap", "hhands", "kissInCheek"],
	
	[translateText("Вылечить")]: ["heal", "epinephrine"],
	
	[translateText("В машине")]: ["park", "lockDoors", "turnNeon", "seatbelt", "trunk", "toggleHingedNumberplate", "autopilot", "hood", "detachFromTowtruck", "ignition", "hijackIgnition", "occupants", "eject"],
	
	[translateText("Взаимодействие с багажником")]: ["intrunk", "fromtrunk", "newnumber"],
	[translateText("Недвижимость")]: ["sellcar", "sellhouse", "roommate", "invitehouse"],
	[translateText("Фракция")]: [
		[],
		["leadaway", "handsup", "rob", "robguns", "pocket"],
		["leadaway", "handsup", "rob", "robguns", "pocket"],
		["leadaway", "handsup", "rob", "robguns", "pocket"],
		["leadaway", "handsup", "rob", "robguns", "pocket"],
		["leadaway", "handsup", "rob", "robguns", "pocket"],
		["leadaway", "search"],
		["leadaway", "search", "takegun", "takeillegal", "takemask", "ticket"],
		["sellkit", "offerheal"],
		["leadaway", "search", "takegun", "takeillegal", "takemask"],
		["leadaway", "pocket", "handsup", "rob", "robguns"],
		["leadaway", "pocket", "handsup", "rob", "robguns"],
		["leadaway", "pocket", "handsup", "rob", "robguns"],
		["leadaway", "pocket", "handsup", "rob", "robguns"],
		["leadaway", "search", "takegun"],
		[],
		["leadaway", "rob", "robguns", "pocket"],
		["leadaway", "search", "pocket", "takemask"],
		["leadaway", "search", "takegun", "takeillegal", "takemask", "ticket"],
	],
	[translateText("Семья")]: ["handsup", "rob", "robguns", "pocket", "leadaway"],
	[translateText("Кальян")]: ["use_hookah", "take_hookah"],
	[translateText("Лифт 1")]: ["f_lift_0", "f_lift_1", "f_lift_2", "f_lift_3"],
	[translateText("Лифт 2")]: ["s_lift_0", "s_lift_1", "s_lift_2", "s_lift_3", "s_lift_4"],
	[translateText("Лифт")]: ["c_lift_0", "c_lift_1"],
	[translateText("Лифт FIB")]: ["gov_lift_1", "gov_lift_3", "gov_lift_4"],
	[translateText("Лифт News 1")]: ["news_f_lift_1", "news_f_lift_2", "news_f_lift_3"],
	[translateText("Лифт News 2")]: ["news_s_lift_1", "news_s_lift_2", "news_s_lift_3"],
	[translateText("Открыть планшет")]: ["fraction_table", "fraction_news", "org_table", "fraction_mayormenu"],
	[translateText("Покинуть фракцию/семью")]: ["leave_fraction", "leave_org"]
};

const categoryDesc = {
	// Машина - правильная последовательность
	"putFromHandToTrunk": "📦 " + translateText("Положить в багажник"),
	"lockDoors": "🔐 " + translateText("Блокировка дверей"),
	"setupNumber": "🔢 " + translateText("Установка номера"),
	"takeOffNumber": "🔢 " + translateText("Снять номер"),
	"hood": "🚙 " + translateText("Капот"),
	"trunk": "🚙 " + translateText("Багажник"),
	"trunkItems": "🧳 " + translateText("Содержимое багажника"),
	"cancelRent": "❌ " + translateText("Отменить аренду"),
	"setGpsTracker": "📡 " + translateText("Прикрепить GPS трекер"),
	"removeGpsTracker": "📡 " + translateText("Снять GPS трекер"),
	"setBikeSafetyBelt": "⚙️ " + translateText("Установить систему крепления"),
	"setVehDoorTazer": "⚡️ " + translateText("Установить набор дверных шокеров"),
	"setVehSignaling": "🔊 " + translateText("Установить сигнализацию"),
	"setVehAutopilot": "🚗 " + translateText("Установить автопилот"),
	"setVehHingedNumberplate": "🔢 " + translateText("Установить откидную рамку"),
	"getInTrunk": "🚘 " + translateText("Залезть в багажник"),
	"throwFromTrunk": "🚘 " + translateText("Выкинуть из багажника"),
	"repairCar": "🔧 " + translateText("Ремонт транспорта"),
	"replaceBattery": "🔧 " + translateText("Заменить аккумулятор"),
	"repalceOil": "🔧 " + translateText("Заменить масло"),
	"replaceLock": "🔧 " + translateText("Починить дверной замок"),
	"pushCar": "🤚 " + translateText("Толкать транспорт"),
	"vehDuplicateKey": "🗝️ " + translateText("Сделать дубликат ключа"),
	"changeVehLock": "🧰 " + translateText("Сменить замок"),
	"pullPlayers": "✋ " + translateText("Вытащить игрока"),
	"fireExtinguisherLoad": "🧯 " + translateText("Заправить огнетушитель"),
	"drillDoorLock": "🔏 " + translateText("Распилить дверной замок"),

	// Игрок - правильная последовательность
	"bailRelease": "🕵️ " + translateText("Предложить выйти под залог"),
	"lawyerFree": "🆓 " + translateText("Запросить освобождение подзащитного"),
	"showDocs": "🛂 " + translateText("Показать документы"),
	"giveMoney": "💰 " + translateText("Передать деньги"),
	"greet": "👋 " + translateText("Поздороваться"),
	"createParty": "👥 " + translateText("Создать группу"),
	"kickFromParty": "👥 " + translateText("Выгнать из группы"),
	"inviteToParty": "👥 " + translateText("Пригласить в группу"),
	"showMedLic": "🌡️ " + translateText("Показать мед. справку"),
	"showArmyLic": "🎫 " + translateText("Показать сертификат о военной службе"),
	"showLic": "🎫 " + translateText("Показать лицензии"),
	"showParamedicLic": "🎫 " + translateText("Показать лицензию парамедика"),
	"showDrugsLic": "🎫 " + translateText("Показать лицензию на наркотики"),
	"showDiploma": "🎫 " + translateText("Показать диплом"),
	"showLawyerDocument": "🎫 " + translateText("Показать лицензию адвоката"),
	"requestDanceBattle": "🕺 " + translateText("Предложить танцевальный поединок"),
	"giftPoster": "🧧 " + translateText("Подарить приглашение"),
	"giftPresent": "🧧 " + translateText("Подарить открытку"),
	"protectionMonument": "🗿 " + translateText("Защита обелиска"),
	"requestThrowDice": "🎲 " + translateText("Предложить бросить кости"),
	"moveInApartment": "🚪 " + translateText("Подселить в квартиру"),
	"moveInHouse": "🏠 " + translateText("Подселить в дом"),
	"halloweenBite": "🚪 " + translateText("Укусить"),
	"halloweenStake": "🚪 " + translateText("Вбить осиновый кол"),
	"rescue": "💉 " + translateText("Реанимировать"),
	"rps_build_start": "🖖 " + translateText("Камень, ножницы, бумага"),

	// Парные анимации
	"kiss": "💋 " + translateText("Поцеловать"),
	"hug": "🤗 " + translateText("Обнять"),
	"five": "🖐 " + translateText("Дать пять"),
	"slap": "✋ " + translateText("Дать пощечину"),
	"hhands": "🤝 " + translateText("Держаться за руки"),
	"kissInCheek": "💋 " + translateText("Поцеловать в щечку"),

	// В машине
	"park": "🚘 " + translateText("Припарковать"),
	"seatbelt": "💺 " + translateText("Ремень безопасности"),
	"turnNeon": "🚨 " + translateText("Неон"),
	"toggleHingedNumberplate": "🔢 " + translateText("Убрать/Показать номер"),
	"autopilot": "🏎️ " + translateText("Автопилот"),
	"detachFromTowtruck": "🚙 " + translateText("Отцепить транспорт"),
	"ignition": "🔑 " + translateText("Зажигание"),
	"hijackIgnition": "🔏 " + translateText("Взломать зажигание"),
	"occupants": "🧑 " + translateText("Пассажиры"),
	"eject": "🦵 " + translateText("Высадить"),

	// Взаимодействия
	"handshake": "👋 " + translateText("Пожать руку"),
	"tinter": "🔁 " + translateText("Повторить анимацию"),
	"givemoney": "💰 " + translateText("Передать деньги"),
	"tradehouse": "🏠 " + translateText("Обмен недвижимостью"),
	"tradebiz": "📋 " + translateText("Обмен бизнесами"),
	"tradecar": "🚗 " + translateText("Обмен машинами"),
	"vmuted": "🔇 " + translateText("Заглушить микрофон"),
	"ETERNAL_DICE": "🎲 Кости",
	"ETERNAL_EXCHANGE-PROPS": "🔄 Обмен имуществом",

	// Документы
	"passport": "🛂 " + translateText("Показать паспорт"),
	"licenses": "🎫 " + translateText("Показать лицензии"),
	"idcard": "📑 " + translateText("Показать ID-карту"),
	"badge": "🆔 " + translateText("Показать удостоверение"),
	"lspdbadge": "🔰 " + translateText("Посмотреть значок"),
	"fibbadge": "🎖️ " + translateText("Посмотреть бейджик"),

	// Вылечить
	"heal": "💊 " + translateText("Аптечкой"),
	"epinephrine": "💉 " + translateText("Адреналином"),
};

const getCircleName = (func, title) => {
	if (func === "fraction_table" && global.fractionId === 0)
		return false;

	if (func === "fraction_news" && global.fractionId !== 15)
		return false;

	if (func === "fraction_mayormenu" && !(global.fractionId === 6 && global.isLeader))
		return false;

	if (func === "org_table" && global.organizationId === 0)
		return false;

	if (func === "belt")
		return isBelt ? "💺 " + translateText("Отстегнуть ремень") : "💺 " + translateText("Пристегнуть ремень");

	if (func === "lockDoors" && selectEntity !== null && selectEntity.doesExist())
		return selectEntity.getVariable("vLock") ? "🔐 " + translateText("Блокировка дверей") : "🔐 " + translateText("Блокировка дверей");

	if (func === "fraction" && (global.fractionId == 0 || global.fractionId == 15))
		return false;

	if (func === "family" && global.organizationId == 0)
		return false;

	if (func === "handshake" && selectEntity !== null && global.friends[selectEntity.name] != undefined && global.friends[selectEntity.name] == true)
		return false;

	if (func === "take_hookah" && (selectEntity === null || !selectEntity.doesExist() || selectEntity['dropData'].pId !== global.localplayer.remoteId))
		return false;
if (func === "cancelRent") {
    if (selectEntity !== null && selectEntity.doesExist()) {
        // Проверяем переменную isRent
        const isRentedVehicle = selectEntity.getVariable("isRent");
        
        // ✅ ВАЖНО: Проверяем что isRent === true (не undefined, не null)
        if (isRentedVehicle !== true) {
            return false; // Скрываем кнопку если машина НЕ арендована
        }
        
        // ✅ Показываем кнопку только если isRent === true
        return categoryDesc[func];
    } else {
        return false;
		}
	}
	if (func === "setupNumber" || func === "takeOffNumber") {
		if (selectEntity !== null && selectEntity.doesExist()) {
			// Берём номер из SharedData (как в C# - vehicleLocalData.NumberPlate)
			const currentNumber = selectEntity.getVariable("NumberPlate") || selectEntity.numberPlate || "";
			
			mp.gui.chat.push(`[DEBUG] NumberPlate: "${currentNumber}", Type: ${typeof currentNumber}`);
			
			// Проверяем есть ли номер
			// В circle.js - проверка наличия номера
const hasNumber = currentNumber !== "" && 
                  !currentNumber.startsWith("NO") && // ← ДОБАВЛЕНО
                  currentNumber !== null &&
                  currentNumber !== undefined;
			
			mp.gui.chat.push(`[DEBUG] hasNumber: ${hasNumber}, func: ${func}`);
			
			if (func === "setupNumber") {
				if (hasNumber) {
					mp.gui.chat.push(`[DEBUG] Hiding setupNumber - number exists: ${currentNumber}`);
					return false; // Скрываем "Установить" если номер есть
				}
				return categoryDesc[func];
			}
			
			if (func === "takeOffNumber") {
				if (!hasNumber) {
					mp.gui.chat.push(`[DEBUG] Hiding takeOffNumber - no number`);
					return false; // Скрываем "Снять" если номера нет
				}
				return categoryDesc[func];
			}
		}
	}

	// НОВОЕ: Проверка багажника
	if (func === "putFromHandToTrunk") {
		// Временно скрываем (как вы просили)
		return false;
	}

	if (func === "trunkItems") {
		if (selectEntity !== null && selectEntity.doesExist()) {
			// Проверяем переменную vDoor (из vehiclesync.js)
			const doorData = selectEntity.getVariable("vDoor");
			if (doorData && global.IsJsonString(doorData)) {
				try {
					const doors = JSON.parse(doorData);
					// doors[5] это багажник (DoorId.DoorTrunk)
					// 0 = закрыт, 1 = открыт, 2 = сломан
					if (doors[5] !== 1) {
						return false; // Скрываем если багажник НЕ открыт
					}
				} catch (e) {
					return false;
				}
			} else {
				// Если нет данных о дверях - скрываем
				return false;
			}
		}
	}

	return categoryDesc[func];
};

const getCategory = (title, id) => {
	selectCategory = title;

	let useCategory = [];

	if (typeof categoryData[title][0] === "string")
		useCategory = categoryData[title];
	else
		useCategory = categoryData[title][id];

	let data = [];
	let displayIndex = 0; // Счетчик для отображаемых элементов

	useCategory.forEach((func, originalIndex) => {  // originalIndex - это индекс из categoryData
		const name = getCircleName(func, title);

		if (name) {
			data.push({
				name: name,
				func: func,
				index: originalIndex,  // ← ИСПРАВЛЕНО: используем оригинальный индекс
				displayIndex: displayIndex  // Для отладки
			});
			displayIndex++;
		}
	});

	return data;
};

let selectEntity = null;
let circleSelect = [];

global.OpenCircle = (title, id, entity = null) => {
	try {
		if (global.menuCheck() && !global.circleOpen) return;

		if (entity !== -1)
			selectEntity = entity;

		const useCategory = getCategory(title, id);

		if (useCategory.length === 0) {
			selectEntity = null;
			return;
		}

		const isUpdateEntity = !!((entity !== -1 && selectEntity !== entity) || title === translateText("В машине"));
		if (!global.circleOpen || isUpdateEntity) {
			circleSelect = [];
			mp.gui.emmit(`window.router.setPopUp("CircleMenu", '${JSON.stringify(useCategory)}')`);
		} else {
			mp.gui.emmit(`window.events.callEvent("cef.circle.updateCategory", '${JSON.stringify(useCategory)}');`);
		}

		circleSelect.push({
			title: title,
			id: id
		});

		if (!global.circleOpen) {
			global.circleOpen = true;
			global.isPopup = true;
			global.menuOpen(true);

			mp.events.add("render", OnRenderCircle);

			mp.game.graphics.transitionToBlurred(50);
		}
	}
	catch (e) {
		callRemote("client_trycatch", "player/circle", "global.OpenCircle", e.toString());
	}
};

global.UpdateCircle = (entity) => {
	if (!global.circleOpen)
		return;

	if (selectCategory === translateText("В машине") && selectEntity === global.localplayer.vehicle)
		return;

	if ([translateText("Кальян"), translateText("Лифт 1"), translateText("Лифт 2"), translateText("Лифт"), translateText("Лифт FIB"), translateText("Лифт News 1"), translateText("Лифт News 2"), translateText("Открыть планшет"), translateText("Покинуть фракцию/семью")].includes(selectCategory))
		return;

	if ([translateText("Взаимодействие с багажником"), translateText("Машина")].includes(selectCategory) && global.localplayer.vehicle)
		return global.OpenCircle(translateText("В машине"), 0, global.localplayer.vehicle);

	if (entity === null && selectEntity === null)
		return;

	if (entity == null) {
		selectEntity = null;
		return;
	}

	const isUpdateEntity = !!(selectEntity !== entity || (selectCategory === translateText("В машине") && !global.localplayer.vehicle));

	selectEntity = entity;

	if ([translateText("Игрок"), translateText("Документы"), translateText("Взаимодействия"), translateText("Парные анимации"), translateText("Машина"), translateText("Взаимодействие с багажником"), translateText("В машине"), translateText("Фракция"), translateText("Семья"), translateText("Недвижимость")].includes(selectCategory)) {

		const ePosition = entity.position;
		const pPosition = global.localplayer.position;

		if (mp.game.gameplay.getDistanceBetweenCoords(ePosition.x, ePosition.y, ePosition.z, pPosition.x, pPosition.y, pPosition.z, true) > 8) {
			selectEntity = null;
			return;
		}

		if (isUpdateEntity) {
			switch (entity.type) {
				case "player":
					global.OpenCircle(translateText("Игрок"), 0, entity);
					break;
				case "vehicle":
					if (translateText("Взаимодействие с багажником") === selectCategory)
						global.OpenCircle(selectCategory, 0, entity);
					else
						global.OpenCircle(translateText("Машина"), 0, entity);
					break;
				default:
					selectEntity = null;
					break;
			}
		}
	}
};

let isInitCircle = false;

gm.events.add('client.circle.initCircle', (percentWidth, percentHeight) => {
	isInitCircle = [
		percentWidth,
		percentHeight
	];
});

let isBack = false;

gm.events.add('client.circle.isBack', (_isBack) => {
	isBack = _isBack;
});

const OnRenderCircle = () => {
	try {
		if (!isInitCircle)
			return;

		const [cursorX, cursorY] = mp.gui.cursor.position;
		const ratio = mp.game.graphics.getScreenAspectRatio(true);

		const res = mp.game.graphics.getScreenActiveResolution(0, 0);
		const centerX = cursorX - res.x / 2;
		const centerY = cursorY - res.y / 2;
		let heading = Math.atan2(centerY, centerX) * (180 / Math.PI);
		if (heading < 0)
			heading = Math.abs(heading);
		else if (heading > 0)
			heading = heading - heading - heading;

		mp.game.graphics.drawSprite("redage_textures_001", isBack ? "noCircleMenu" : "circleMenu", 0.5, 0.5, 0.175 * isInitCircle[0], 0.175 * isInitCircle[0] * ratio, 90 - heading, 255, 255, 255, 255);
	}
	catch (e) {
		callRemote("client_trycatch", "player/circle", "OnRenderCircle", e.toString());
	}
};

mp.game.graphics.transitionFromBlurred(0);

global.CloseCircle = () => {
	try {
		mp.gui.emmit(`window.router.setPopUp();`);
		global.circleOpen = false;
		global.isPopup = false;
		selectCategory = "";
		selectEntity = null;
		circleSelect = [];
		global.menuClose();

		mp.events.remove("render", OnRenderCircle);

		mp.game.graphics.transitionFromBlurred(250);
	}
	catch (e) {
		callRemote("client_trycatch", "player/circle", "global.CloseCircle", e.toString());
	}
};

// Остальной код без изменений...
gm.events.add('client.circle.select', (funcName, index) => {
	switch (funcName) {
		case 'back':
			index = circleSelect.length - 2;
			if (circleSelect.length > 0 && circleSelect[index]) {
				const data = circleSelect[index];
				circleSelect.splice(index, 2);
				global.OpenCircle(data.title, data.id, -1);
			} else {
				global.CloseCircle();
			}
			break;
		case 'trunkAction':
			global.OpenCircle(translateText("Взаимодействие с багажником"), 0, -1);
			break;
		case 'sell':
			call("client.circle.events", Number(index));
			global.OpenCircle(translateText("Взаимодействия"), 0, -1);
			break;
		case 'paired_animations':
			global.OpenCircle(translateText("Парные анимации"), 0, -1);
			break;
		case 'fraction':
			if (global.fractionId == 0 || global.fractionId == 15) return;
			global.OpenCircle(translateText("Фракция"), global.fractionId, -1);
			break;
		case 'family':
			if (global.organizationId == 0) return;
			global.OpenCircle(translateText("Семья"), 0, -1);
			break;
		case 'documents':
			global.OpenCircle(translateText("Документы"), 0, -1);
			break;
		case 'house':
			global.OpenCircle(translateText("Недвижимость"), 0, -1);
			break;
		case 'healMenu':
			global.OpenCircle(translateText("Вылечить"), 0, -1);
			break;
		default:
			call("client.circle.events", funcName, Number(index));
			break;
	}
});


gm.events.add('client.circle.events', (func, index) => {
	try {
		const category = selectCategory;
		
		// ИСПРАВЛЕНИЕ: Если func это строка, ищем элемент в popupData по func
		let actualIndex = index;
		if (typeof func === 'string') {
			// Находим элемент в оригинальном массиве categoryData
			const categoryArray = typeof categoryData[category][0] === "string" 
				? categoryData[category] 
				: categoryData[category][global.fractionId || 0];
			
			actualIndex = categoryArray.indexOf(func);
            mp.gui.chat.push(`Category: ${category}, Func: '${func}', Original Index: ${index}, Actual Index: ${actualIndex}`);
		}
		
		if (category !== translateText("Игрок") || (category === translateText("Игрок") && (actualIndex === 1 || actualIndex === 4))) 
			global.CloseCircle(false);

		switch (category) {
			case translateText("В машине"):
				const veh = global.localplayer.vehicle;
				if (!veh || global.localplayer.isInAnyPlane()) return;
				switch (actualIndex) {
					case 0:
						let vehclass = veh.getClass();
						if (vehclass == 8 || vehclass == 13 || vehclass == 14) {
							call('notify', 4, 9, translateText("В этом типе транспортных средств нет ремней безопасности."), 3000);
							return;
						}
						if (!isBelt) global.localplayer.setConfigFlag(32, false);
						else global.localplayer.setConfigFlag(32, true);
						callRemote('beltSelected', isBelt);
						isBelt = !isBelt;
						mp.gui.emmit(`window.vehicleState.belt (${isBelt})`);
						break;
					case 1:
					case 2:
					case 3:
						if (veh.getPedInSeat(-1) != global.localplayer.handle) {
							call('notify', 4, 9, translateText("Вы должны быть на водительском месте"), 3000);
							return;
						}
						callRemote('vehicleSelected', veh, actualIndex - 1);
						return;
					case 4:
						if (veh.getPedInSeat(-1) != global.localplayer.handle) {
							call('notify', 4, 9, translateText("Вы должны быть на водительском месте"), 3000);
							return;
						}
						callRemote('server.streetrace.open');
						return;
				}
				return;
			case translateText("Взаимодействие с багажником"):
				if (global.entity == null) return;
				switch (actualIndex) {
					case 0:
					case 1:
						const vehclass = global.entity.getClass();
						if (vehclass != 1 && vehclass != 2 && vehclass != 3 && vehclass != 4 && vehclass != 5 && vehclass != 6) {
							call('notify', 4, 9, translateText("В багажник этого т/с нельзя залезть."), 3000);
							return;
						}
						callRemote('vehicleSelected', global.entity, actualIndex + 10);
						return;
					case 2:
						callRemote('vehicleSelected', global.entity, actualIndex + 10);
						return;
				}
				return;
			case translateText("Машина"):
    if (global.entity == null) return;
     if (func === "cancelRent") {
        callRemote('vehicleSelected', global.entity, 7);
        return;
    }
    // Используем actualIndex из начала функции
    callRemote('vehicleSelected', global.entity, actualIndex);
    return;
	
			case translateText("Вылечить"):
				if (global.entity == null) return;
				callRemote('pSelected', global.entity, func);
				return;
			case translateText("Игрок"):
				if (global.entity == null) return;
				switch (actualIndex) {
					case 1:
						callRemote('pSelected', global.entity, "offer");
						return;
					case 4:
						callRemote('pSelected', global.entity, "heal");
						return;
				}
				return;
			case translateText("Документы"):
				if (global.entity == null) return;
				switch (actualIndex) {
					case 0:
						callRemote('passport', global.entity);
						return;
					case 1:
						callRemote('licenses', global.entity);
						return;
					case 2:
						callRemote('idcard', global.entity);
						return;
					case 3:
						callRemote('certificate', global.entity);
						return;
					case 4:
						if (new Date().getTime() - circleEventRefresh[0] < 15000) {
							call('notify', 4, 9, translateText("Попробуйте через 15 секунд"), 3000);
							return;
						}
						circleEventRefresh[0] = new Date().getTime();
						callRemote('viewBadge', global.entity, translateText("Посмотреть значок"));
						return;
					case 5:
						if (new Date().getTime() - circleEventRefresh[1] < 15000) {
							call('notify', 4, 9, translateText("Попробуйте через 15 секунд"), 3000);
							return;
						}
						circleEventRefresh[1] = new Date().getTime();
						callRemote('viewBadge', global.entity, translateText("Посмотреть бейджик"));
						return;
				}
				return;
			case translateText("Недвижимость"):
				switch (index) {
					case 0:
						callRemote('pSelected', global.entity, "sellcar");
						return;
					case 1:
						callRemote('pSelected', global.entity, "sellhouse");
						return;
					case 2:
						callRemote('pSelected', global.entity, "roommate");
						return;
					case 3:
						callRemote('pSelected', global.entity, "invitehouse");
						return;
				}
				return;
			case translateText("Взаимодействия"):
				if (global.entity == null) return;
				if (func == "ETERNAL_DICE")
					return callRemote('pSelected', global.entity, "ETERNAL_DICE");
				if (func == "ETERNAL_EXCHANGE-PROPS")
					return callRemote('pSelected', global.entity, "ETERNAL_EXCHANGE-PROPS");
				switch (index) {
					case 0:
						callRemote('pSelected', global.entity, "handshake");
						return;
					case 1:
						callRemote('pSelected', global.entity, "tinter");
						return;
					case 2:
						callRemote('pSelected', global.entity, "givemoney");
						return;
					case 3:
						callRemote('server.character.trade', global.entity, "house");
						return;
					case 4:
						callRemote('server.character.trade', global.entity, "business");
						return;
					case 5:
						callRemote('server.character.trade', global.entity, "vehicle");
						return;
					case 6:
						if (global.pplMuted.length >= 10) {
							call('notify', 4, 9, translateText("За одну сессию можно отключить микрофон только 10 игрокам."), 3000);
							return;
						}
						callRemote('pSelected', global.entity, "vmuted");
						return;
				}
				return;
			case translateText("Парные анимации"):
				if (global.entity == null) return;
				switch (index) {
					case 0:
						callRemote('pairedAnimations', global.entity, "PAIRED_EMBRACE");
						return;
					case 1:
						callRemote('pairedAnimations', global.entity, "PAIRED_KISS");
						return;
					case 2:
						callRemote('pairedAnimations', global.entity, "PAIRED_FIVE");
						return;
					case 3:
						callRemote('pairedAnimations', global.entity, "PAIRED_SLAP");
						return;
					case 4:
					case 5:
					case 6:
					case 7:
						callRemote('carryAnimations', global.entity, (index - 4));
						break;
				}
				return;
			case translateText("Фракция"):
				if (global.entity == null) return;
				if (categoryData[translateText("Фракция")][global.fractionId] == undefined) return;
				callRemote('pSelected', global.entity, categoryData[translateText("Фракция")][global.fractionId][index]);
				return;
			case translateText("Семья"):
				if (global.entity == null) return;
				callRemote('pOrgSelected', global.entity, index);
				return;
			case translateText("Кальян"):
				switch (index) {
					case 0:
						callRemote('server.hookahManage', global.entity);
						break;
					case 1:
						callRemote('server.raise', global.entity);
						break;
				}
				return;
			case translateText("Лифт 1"):
				callRemote('server.useCityhallLift', 1, index);
				return;
			case translateText("Лифт 2"):
				callRemote('server.useCityhallLift', 2, index);
				return;
			case translateText("Лифт"):
				callRemote('server.useCityhallLift', 3, index);
				return;
			case translateText("Лифт FIB"):
				callRemote('server.useCityhallLift', 4, index);
				return;
			case translateText("Лифт News 1"):
				callRemote('server.useNewsLift', 1, index);
				return;
			case translateText("Лифт News 2"):
				callRemote('server.useNewsLift', 2, index);
				return;
			case translateText("Открыть планшет"):
				switch (func) {
					case "fraction_table":
						if (global.fractionId !== 0) {
							mp.gui.emmit(`window.gameMenuView ("Fractions");`);
							if (!global.gamemenu)
								global.binderFunctions.GameMenuOpen();
						}
						break;
					case "org_table":
						if (global.organizationId !== 0) {
							mp.gui.emmit(`window.gameMenuView ("Organization");`);
							if (!global.gamemenu)
								global.binderFunctions.GameMenuOpen();
						}
						break;
					case "fraction_news":
						call('client.advert.open');
						break;
					case "fraction_mayormenu":
						callRemote('server.mayormenu.open');
						break;
				}
				return;
			case translateText("Покинуть фракцию/семью"):
				callRemote('server.LeaveFractionOrg', index);
				return;
		}
	}
	catch (e) {
		callRemote("client_trycatch", "player/circle", "client.circle.events", e.toString());
	}
});

// Остальной код без изменений
global.attachedtotrunk = false;

global.getVehicleWidth = (vehicle) => {
	try {
		if (vehicle && mp.vehicles.exists(vehicle)) {
			const getModelDimensions = mp.game.gameplay.getModelDimensions(vehicle.model);
			return getModelDimensions.max.y - getModelDimensions.min.y;
		}
		return 1;
	}
	catch (e) {
		callRemote("client_trycatch", "player/circle", "getVehicleWidth", e.toString());
		return 1;
	}
};

const attachPlayerToTrunk = (playerId, vehicleId) => {
	try {
		let player = mp.players.atRemoteId(playerId);
		let vehicle = mp.vehicles.atRemoteId(vehicleId);
		if (!player || !mp.players.exists(player) || "player" !== player.type || !vehicle || !mp.vehicles.exists(vehicle)) return;
		const _getVehicleWidth = global.getVehicleWidth(vehicle);
		player.attachTo(vehicle.handle, -1, 0, -_getVehicleWidth / 2 + 0.5, 0.4, 0, 0, 0, false, false, false, false, 20, true);
		if (player == global.localplayer) global.attachedtotrunk = true;
	}
	catch (e) {
		callRemote("client_trycatch", "player/circle", "attachPlayerToTrunk", e.toString());
	}
};

gm.events.add("client.vehicle.trunk.attachPlayer", attachPlayerToTrunk);

gm.events.add("client.vehicle.trunk.detachPlayer", (playerId, vehicleId, isDeath = true) => {
	try {
		let player = mp.players.atRemoteId(playerId);
		if (!player || "player" !== player.type || !mp.players.exists(player)) return;
		let pos = player.position;
		let vehicle = mp.vehicles.atRemoteId(vehicleId);
		if (vehicle && mp.vehicles.exists(vehicle)) pos = vehicle.getOffsetFromInWorldCoords(0, -3, 0);
		player.detach(true, true);
		if (player == global.localplayer) {
			if (isDeath) {
				player.position = pos;
				player.posX = pos.x;
				player.posY = pos.y;
				player.posZ = pos.z;
			}
			global.attachedtotrunk = false;
		}
	}
	catch (e) {
		callRemote("client_trycatch", "player/circle", "client.vehicle.trunk.detachPlayer", e.toString());
	}
});

gm.events.add("openCityhallLiftMenu", (index) => {
	try {
		if (global.circleOpen) {
			global.CloseCircle();
			return;
		}

		if (index == 10) global.OpenCircle(translateText("Лифт 1"), 0);
		else if (index == 11) global.OpenCircle(translateText("Лифт 2"), 0);
		else if (index == 12) global.OpenCircle(translateText("Лифт"), 0);
		else if (index == 13) global.OpenCircle(translateText("Лифт FIB"), 0);
	}
	catch (e) {
		callRemote("client_trycatch", "player/circle", "openCityhallLiftMenu", e.toString());
	}
});

gm.events.add("openNewsLiftMenu", (index) => {
	try {
		if (global.circleOpen) {
			global.CloseCircle();
			return;
		}

		if (index == 2) global.OpenCircle(translateText("Лифт News 1"), 0);
		else if (index == 3) global.OpenCircle(translateText("Лифт News 2"), 0);
	}
	catch (e) {
		callRemote("client_trycatch", "player/circle", "openCityhallLiftMenu", e.toString());
	}
});

gm.events.add("openSpecialChooseMenu", (index) => {
	try {
		if (global.circleOpen) {
			global.CloseCircle();
			return;
		}

		if (index == 0) global.OpenCircle(translateText("Открыть планшет"), 0);
		else if (index == 1) global.OpenCircle(translateText("Покинуть фракцию/семью"), 0);
	}
	catch (e) {
		callRemote("client_trycatch", "player/circle", "openTableChooseMenu", e.toString());
	}
});

gm.events.add("playerStreamIn", (entity) => {
	let atttoveh = entity.getVariable('AttachToVehicle');
	if (atttoveh) {
		setTimeout(function () {
			if (entity && mp.players.exists(entity))
				attachPlayerToTrunk(entity.remoteId, Number(atttoveh));
		}, 2500);
	}
});

gm.events.add("render", () => {
	try {
		if (!global.loggedin) return;
		if (global.attachedtotrunk == true) {
			mp.game.graphics.drawText(translateText("Нажмите 'F', чтобы вылезти из багажника."), [0.5, 0.8], {
				font: 0,
				color: [255, 255, 255, 185],
				scale: [0.35, 0.35],
				outline: true
			});
			global.ToggleMovementControls();
		}
	}
	catch (e) {
		if (new Date().getTime() - global.trycatchtime["player/circle"] < 60000) return;
		global.trycatchtime["player/circle"] = new Date().getTime();
		callRemote("client_trycatch", "player/circle", "render", e.toString());
	}
});

let isBelt = false;
gm.events.add("playerEnterVehicle", (entity, seat) => {
	try {
		isBelt = false;
		mp.gui.emmit(`window.vehicleState.belt (false)`);
	}
	catch (e) {
		callRemote("client_trycatch", "player/circle", "playerEnterVehicle", e.toString());
	}
});

gm.events.add("playerLeaveVehicle", () => {
	try {
		isBelt = false;
		mp.gui.emmit(`window.vehicleState.belt (false)`);
		global.localplayer.setConfigFlag(32, true);
	}
	catch (e) {
		callRemote("client_trycatch", "player/circle", "playerLeaveVehicle", e.toString());
	}
});

global.binderFunctions.onBelt = () => {
	try {
		const veh = global.localplayer.vehicle;
		if (!veh || global.localplayer.isInAnyPlane()) return;
		let vehclass = veh.getClass();
		if (vehclass == 8 || vehclass == 13 || vehclass == 14) {
			call('notify', 4, 9, translateText("В этом типе транспортных средств нет ремней безопасности."), 3000);
			return;
		}
		if (!isBelt) global.localplayer.setConfigFlag(32, false);
		else global.localplayer.setConfigFlag(32, true);
		callRemote('beltSelected', isBelt);
		isBelt = !isBelt;
		mp.gui.emmit(`window.vehicleState.belt (${isBelt})`);
	}
	catch (e) {
		callRemote("client_trycatch", "player/circle", "global.binderFunctions.onBelt", e.toString());
	}
};

global.pplMuted = [];
gm.events.add('MutePlayer', function (playername) {
	try {
		pplMuted[playername] = true;
	}
	catch (e) {
		callRemote("client_trycatch", "player/circle", "MutePlayer", e.toString());
	}
});

gm.events.add('unMutePlayer', function (playername) {
	try {
		if (pplMuted[playername] === true) delete pplMuted[playername];
	}
	catch (e) {
		callRemote("client_trycatch", "player/circle", "unMutePlayer", e.toString());
	}
});

global.pplMutedMe = [];
gm.events.add('MutedMePlayer', function (playername) {
	try {
		pplMutedMe[playername] = true;
	}
	catch (e) {
		callRemote("client_trycatch", "player/circle", "MutedMePlayer", e.toString());
	}
});

gm.events.add('unMuteMeForPlayer', function (playername) {
	try {
		if (pplMutedMe[playername] === true) delete pplMutedMe[playername];
	}
	catch (e) {
		callRemote("client_trycatch", "player/circle", "unMuteMeForPlayer", e.toString());
	}
});

gm.events.add('test.2', function (name) {
	if (categoryData[name])
		global.OpenCircle(name, 0);
});