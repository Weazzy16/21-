const callRemote = mp.events.callRemote;
const call = mp.events.call;
const callRemoteUnreliable = mp.events.callRemoteUnreliable;
const browsers = mp.browsers;
const _callRemote = mp._events.callRemote ;
const _call = mp._events.call;
mp.events.add("loadStupidInfo",(text) => {
    mp.console.logInfo(JSON.stringify(text), true, true);
    mp.gui.emmit(`window.router.setView("HouseCraft", ${JSON.stringify(text)});`);
    global.gamemenu = true;
    global.menuOpen(true);
    mp.gui.cursor.show(true, true);
});

mp.events.add("clien.gta5devcraft.getinfo", (index) => {
    callRemote("server.gta5devcraft.getinfo", index)
});

mp.events.add("loadMoreInfoInfo",(resultitem, ingredient, shans, time) => {

    mp.console.logInfo(`${JSON.stringify(ingredient)} ${JSON.stringify(shans)} ${JSON.stringify(time)} ${JSON.stringify(resultitem)}`, true, true);
    mp.gui.emmit(`window.gta5devcraft(${JSON.stringify(resultitem)}, ${JSON.stringify(ingredient)}, ${shans}, ${time});`);
});

mp.events.add('clien.gta5devcraft.exit', () => {
    global.binderFunctions.GameMenuClose ();
});

var craftList = [];
var currentIndex = 0;
var isCrafting = false;

mp.events.add('clien.gta5devcraft.allowcraft', (itemid, timeforone, value) => {
    for (let i = 0; i < value; i++) {
        var info = { "item": itemid, "count": 1, "time": timeforone }
        craftList.push(info);
    }
    if (!isCrafting) {
        isCrafting = true;
        startCrafting();
    }
});


mp.events.add('clien.gta5devcraft.startcraft', (itemid, value) => {
    if (!isCrafting) {
        callRemote("server.gta5devcraft.startcraft", itemid, value, true);
        call('notify', 2, 9, "Вы начали крафт.", 3000);
    } else {
        call('notify', 4, 9, "Вы не можете начать крафт пока идёт другой крафт.", 3000);
    }
});

function startCrafting() {
    if (currentIndex < craftList.length) {
        var info = craftList[currentIndex];
        setTimeout(() => {
            craftItem(info.item, info.count);
            currentIndex++;
            startCrafting();
        }, info.time * 1000);
    } else {
        isCrafting = false;
        currentIndex = 0;
        craftList = [];
    }
}

function craftItem(itemid, value) {
    callRemote("server.gta5devcraft.startcraft", itemid, value, false);
}