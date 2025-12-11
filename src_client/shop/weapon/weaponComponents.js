const callRemote = mp.events.callRemote;
const call = mp.events.call;
const callRemoteUnreliable = mp.events.callRemoteUnreliable;
const browsers = mp.browsers;
const _callRemote = mp._events.callRemote ;
const _call = mp._events.call;
require('./weaponComponentsData.js');
require('./weaponNameToHash.js');

gm.events.add('client.weaponshop.components', (weaponKey) => {
    try{
        // DEBUG (используем mp.console)
        try { mp.console.logInfo(`[weaponComponents] request received, raw key: ${String(weaponKey)}`); } catch(e){}

        let mapped = null;

        // 1) Прямой lookup
        if (global.WeaponNameToHash && typeof weaponKey === 'string' && global.WeaponNameToHash[weaponKey] !== undefined) {
            mapped = global.WeaponNameToHash[weaponKey];
            try { mp.console.logInfo(`[weaponComponents] mapped by exact name -> ${mapped}`); } catch(e){}
        }

        // 2) Убираем пробелы
        if (mapped === null && typeof weaponKey === 'string') {
            const noSpace = weaponKey.replace(/\s/g,'');
            if (global.WeaponNameToHash && global.WeaponNameToHash[noSpace] !== undefined) {
                mapped = global.WeaponNameToHash[noSpace];
                try { mp.console.logInfo(`[weaponComponents] mapped by no-space name -> ${mapped} (key used: ${noSpace})`); } catch(e){}
            }
        }

        // 3) Case-insensitive поиск
        if (mapped === null && typeof weaponKey === 'string' && global.WeaponNameToHash) {
            const lower = weaponKey.toLowerCase();
            for (let k in global.WeaponNameToHash) {
                if (k.toLowerCase() === lower || k.toLowerCase() === lower.replace(/\s/g,'')) {
                    mapped = global.WeaponNameToHash[k];
                    try { mp.console.logInfo(`[weaponComponents] mapped by case-insensitive match -> ${mapped} (matched key: ${k})`); } catch(e){}
                    break;
                }
            }
        }

        // 4) Numeric
        if (mapped === null) {
            let maybeNum = null;
            if (typeof weaponKey === 'number') maybeNum = weaponKey;
            else if (typeof weaponKey === 'string' && /^\-?\d+$/.test(weaponKey)) maybeNum = Number(weaponKey);

            if (maybeNum !== null) {
                mapped = maybeNum;
                try { mp.console.logInfo(`[weaponComponents] using numeric key directly -> ${mapped}`); } catch(e){}
            }
        }

        // 5) Фallback: пытаемся найти запись в ComponentsData через WeaponNameToHash
        if (mapped === null && typeof weaponKey === 'string' && global.ComponentsData) {
            const candidates = [
                weaponKey,
                weaponKey.replace(/\s/g,''),
                weaponKey.replace(/\s/g,'').toLowerCase(),
                weaponKey.toLowerCase()
            ];
            outerLoop:
            for (let k in global.ComponentsData) {
                for (let wn in (global.WeaponNameToHash || {})) {
                    if (String(global.WeaponNameToHash[wn]) === String(k)) {
                        if (candidates.includes(wn) || candidates.includes(wn.replace(/\s/g,''))) {
                            mapped = Number(k);
                            try { mp.console.logInfo(`[weaponComponents] fallback matched ComponentsData key via WeaponNameToHash entry ${wn} -> ${mapped}`); } catch(e){}
                            break outerLoop;
                        }
                    }
                }
            }
        }

        let dataKey = null;
        if (mapped !== null && mapped !== undefined) dataKey = String(mapped);
        else {
            try { mp.console.logError('[weaponComponents] FAILED to map key. weaponKey: ' + String(weaponKey)); } catch(e){}
            try { mp.console.logInfo('[weaponComponents] WeaponNameToHash sample keys: ' + JSON.stringify(Object.keys(global.WeaponNameToHash || {}).slice(0,40))); } catch(e){}
            try { mp.console.logInfo('[weaponComponents] ComponentsData sample keys: ' + JSON.stringify(Object.keys(global.ComponentsData || {}).slice(0,40))); } catch(e){}
            call('notify', 4, 9, translateText("На данное оружие нет модификаций!") + ` (${weaponKey})`, 3000);
            return;
        }

        try { mp.console.logInfo(`[weaponComponents] final dataKey to use: ${dataKey}`); } catch(e){}

        if (!global.ComponentsData || !global.ComponentsData[dataKey]) {
            try { mp.console.logInfo('[weaponComponents] ComponentsData has no entry for dataKey: ' + dataKey + ' available keys sample: ' + JSON.stringify(Object.keys(global.ComponentsData || {}).slice(0,40))); } catch(e){}
            call('notify', 4, 9, translateText("На данное оружие нет модификаций!") + ` (${dataKey})`, 3000);
            return;
        }

        let componentsData = [];
        let componentsType = [];
        for (let key in global.ComponentsData[dataKey].Components) {
            const cData = global.ComponentsData[dataKey].Components[key];
            if (!componentsType.includes(cData.Type)) componentsType.push(cData.Type);
            componentsData.push({
                Name: global.escapeHtml (mp.game.ui.getLabelText(cData.Name)),
                Desc: global.escapeHtml (mp.game.ui.getLabelText(cData.Desc)),
                Mats: Math.round(cData.Price / 100 * global.weaponComponentPrice),
                type: cData.Type,
                hash: key
            });
        }

        mp.gui.emmit(`window.weaponshopcomponents('${JSON.stringify(componentsData)}','${JSON.stringify(componentsType)}')`);
    }
    catch (e)
    {
        try { mp.console.logError('[weaponComponents] Exception: ' + String(e)); } catch(err){}
        callRemote("client_trycatch", "shop/weapon/weaponComponents", "client.weaponshop.components", e.toString());
    }
});

gm.events.add('client.weaponshop.buyComponent', (category, activeItemID, componentId) => {
    try
    {
        if(new Date().getTime() - global.lastCheck < 50) return;
        global.lastCheck = new Date().getTime();
        callRemote('server.weaponshop.buyComponent', Number (category), Number (activeItemID), componentId.toString());
    }
    catch (e)
    {
        try { mp.console.logError('[weaponComponents] buyComponent Exception: ' + String(e)); } catch(err){}
        callRemote("client_trycatch", "shop/weapon/weaponComponents", "client.weaponshop.buyComponent", e.toString());
    }
});