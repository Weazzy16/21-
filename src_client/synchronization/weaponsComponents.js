const callRemote = mp.events.callRemote;
const call = mp.events.call;
const callRemoteUnreliable = mp.events. callRemoteUnreliable;
const browsers = mp.browsers;
const _callRemote = mp._events.callRemote;
const _call = mp._events.call;

// ✅ ИСПРАВЛЕНИЕ: Делаем функцию ГЛОБАЛЬНОЙ
global.setComponentsToPlayer = (entity, data) => {
	try 
	{
		if (entity && mp.players.exists(entity) && entity.type === 'player' && entity.handle !== 0) {
			if (!data) data = entity.getVariable("weaponComponents");
			if (data && data != "undefined" && data != "null" && data.split("|")) {
				let [weaponHash, indexData] = data.split("|");
				weaponHash = parseInt(weaponHash);
				
				if (!global.ComponentsData[weaponHash]) {
					mp.console.logInfo(`[WEAPON CLIENT] ❌ No ComponentsData for weapon ${weaponHash}`);
					return;
				}
				if (!global.ComponentsData[weaponHash].Components) {
					mp.console.logInfo(`[WEAPON CLIENT] ❌ No Components data for weapon ${weaponHash}`);
					return;
				}
				
				entity.__weaponHash = weaponHash;
		
				indexData = (indexData && indexData.length > 0) ? JSON.parse(indexData) : [];
				
				mp.console.logInfo(`[WEAPON CLIENT] 📋 Applying ${indexData.length} components to weapon ${weaponHash}`);
				mp.console.logInfo(`[WEAPON CLIENT] 📋 Components list: ${indexData.join(', ')}`);
			
				// ✅ Для других игроков выдаём оружие
				if (entity. handle != global.localplayer. handle) {
					entity.giveWeapon(weaponHash, -1, true);
				}

				// ✅ ПРИМЕНЯЕМ КОМПОНЕНТЫ
				for (let index of indexData) {
					global.addComponentToPlayer(entity, weaponHash, index);
					mp.console.logInfo(`[WEAPON CLIENT] ✅ Applied component:  ${index}`);
				}
				
				mp.game.invoke("0xADF692B254977C0C", entity.handle, weaponHash >> 0, true);
				
				mp.console.logInfo(`[WEAPON CLIENT] ✅ All components applied successfully`);
			} else {
				global.removeAllComponentFromPlayer(entity);
			}
		}
	}
	catch (e) 
	{
		mp. console.logError(`[WEAPON CLIENT] setComponentsToPlayer Exception: ${e.toString()}`);
		if(new Date().getTime() - global.trycatchtime["synchronization/weaponsComponents"] < 5000) return;
		global.trycatchtime["synchronization/weaponsComponents"] = new Date().getTime();
		callRemote("client_trycatch", "synchronization/weaponsComponents", "setComponentsToPlayer", e.toString());
	}
}

// ✅ ИСПРАВЛЕНИЕ: Делаем функцию ГЛОБАЛЬНОЙ
global.addComponentToPlayer = (player, weaponHash, componentHash) => {
	try
	{
		if (player && mp.players.exists(player) && player.type === 'player') 
		{
			if (!player.hasOwnProperty("__weaponComponentData")) player.__weaponComponentData = new Set();

			player.__weaponComponentData.add(componentHash);
			mp.game.invoke("0xD966D51AA5B28BB9", player.handle, weaponHash >> 0, componentHash >> 0);
		}
	}
	catch (e) 
	{
		mp.console.logError(`[WEAPON CLIENT] addComponentToPlayer Exception: ${e.toString()}`);
		callRemote("client_trycatch", "synchronization/weaponsComponents", "addComponentToPlayer", e.toString());
	}
}

// ✅ ИСПРАВЛЕНИЕ:  Делаем функцию ГЛОБАЛЬНОЙ
global.removeAllComponentFromPlayer = (player) => {
	try
	{
		if (player && mp. players.exists(player) && player.type === 'player') 
		{
			if (! player.hasOwnProperty("__weaponHash")) return;
			if (!player.hasOwnProperty("__weaponComponentData")) return;

			for (let component of player.__weaponComponentData) {
				mp.game.invoke("0x1E8BE90C74FB4C09", player.handle, player.__weaponHash >> 0, component >> 0);
			}
			player.__weaponComponentData = new Set();
			delete player.__weaponComponentData;
		}
	}
	catch (e) 
	{
		mp.console.logError(`[WEAPON CLIENT] removeAllComponentFromPlayer Exception: ${e.toString()}`);
		callRemote("client_trycatch", "synchronization/weaponsComponents", "removeAllComponentFromPlayer", e. toString());
	}
}

// ✅ Обработчик SharedData
mp.events.addDataHandler("weaponComponents", (entity, value, oldValue) => {
	global.setComponentsToPlayer(entity, value);
});

// ✅ Обработчик стрима
gm.events.add("playerStreamIn", (entity) => {
	global.setComponentsToPlayer(entity, null);
});

gm.events.add("playerStreamOut", (entity) => {
	if (entity && entity.hasOwnProperty("__weaponComponentData")) {
		entity.__weaponComponentData = new Set();
		delete entity.__weaponComponentData;
	}
});