const callRemote = mp.events.callRemote;

let compassVisible = true;
let currentHeading = 0;

// ✅ Включение/выключение
// ✅ Включение/выключение
gm.events.add('client.compass', (visible) => {
    try {
        console.log('🧭 [compass.js] client.compass called:', visible); // ✅ ДОБАВЬ ЛОГ
        compassVisible = visible;
        mp.gui.emmit(`window.compassStore.setVisible(${visible})`);
    } catch (e) {
        callRemote("client_trycatch", "shamancode/compass", "client.compass", e.toString());
    }
});

// ✅ Обновление направления (по камере!)
gm.events.add(global.renderName["125ms"], () => {
    try {
        if (!global.loggedin || !compassVisible) return;
        
        // ✅ Получаем rotation камеры (как в оригинале comps.js)
        const camRot = mp.game.cam.getGameplayCamRot(2);
        const heading = Math.round(360 - (camRot.z + 360) % 360);
        
        if (heading !== currentHeading) {
            currentHeading = heading;
            mp.gui.emmit(`window.compassStore.setDegree(${heading})`);
        }
    } catch (e) {
        callRemote("client_trycatch", "shamancode/compass", "renderName125ms", e.toString());
    }
});