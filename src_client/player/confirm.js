const callRemote = mp.events.callRemote;
const call = mp.events.call;
const callRemoteUnreliable = mp.events.callRemoteUnreliable;
const browsers = mp.browsers;
const _callRemote = mp._events.callRemote ;
const _call = mp._events.call;
var text = "";
var canback = "";

let isInterface = false;
gm.events.add('debugDeathPopup', () => {
  console.log('[confirm.js] debugDeathPopup handler is loaded');
});

global.openDialog = () => {
    mp.gui.emmit(`window.router.setPopUp("PopupConfirm", {title: "${translateText("Подтверждение")}", text: "${text}"});`);
    mp.gui.cursor.visible = true;
    isInterface = false;    
    if (!global.menuOpened) {
        global.menuOpen(true);
        isInterface = true;
        global.isPopup = true;
    }
    //call('startScreenEffect', "MenuMGHeistIn", 1, true);
}

global.closeDialog = () => {
    mp.gui.emmit('window.router.setPopUp()');

    if (isInterface)
        global.menuClose();
    else if (isOpenPopupList)
        callRemote('popup.list.callback', null);

    isInterface = false;
    global.isPopup = false;
    isOpenPopupList = false;
    //call('stopScreenEffect', "MenuMGHeistIn");
}

gm.events.add('openDialog', (cback, ctext) => {
    // Добавить title!!!
    canback = cback;
    text = ctext;
    global.openDialog();
})

gm.events.add('client:OnDialogCallback', (state) => {
    if (canback == 'tuningbuy') call('client.custom.sbuy', state);
    else callRemote('dialogCallback', canback, state);
    global.closeDialog();
})

//

gm.events.add('openHospitalDialog', (ctext) => {
    mp.gui.emmit(`window.router.setPopUp("PopupDeath", {title: "${translateText("Подтверждение")}", text: "${ctext}"});`);
    mp.gui.cursor.visible = true;
    isInterface = false;    
    if (!global.menuOpened) {
        global.menuOpen(true);
        gm.discord('Общается с врачами в больнице');
        isInterface = true;
        global.isPopup = true;
    }
})

gm.events.add('client:OnHospitalDialogCallback', (state) => {
    callRemote('server:OnHospitalDialogCallback', state);
    global.closeDialog();
})

// Добавьте в confirm.js (рядом с существующими gm.events.add handlers)
gm.events.add('openHospitalDialogData', (jsonPayload) => {
  try {
    // payload может быть строкой JSON или уже объектом
    const payload = (typeof jsonPayload === 'string') ? JSON.parse(jsonPayload) : jsonPayload;

    // Нормализация: гарантируем нужные поля
    const normalized = {
      isShown: payload.isShown ?? true,
      places: payload.places ?? [],
      deathScreenData: payload.deathScreenData ?? {},
      title: payload.title ?? (payload.title || ''),
      text: payload.text ?? (payload.text || '')
    };

    // Передаём в router/CEF: PopusContainer ожидает popupData
    // Используем mp.gui.emmit как в проекте (сохраняем стиль), если у вас другой - поменяйте на mp.gui.emit
    mp.gui.emmit(`window.router.setPopUp("PopupDeath", ${JSON.stringify(normalized)});`);

    // Показываем курсор и меню, как в других попапах
    mp.gui.cursor.visible = true;
    if (!global.menuOpened) {
      global.menuOpen(true);
      global.isPopup = true;
    }
  } catch (e) {
    console.error('openHospitalDialogData error', e, jsonPayload);
    // На случай ошибки можно fallback'нуть на старый текстовый popup (если есть payload.text)
    try {
      if (typeof jsonPayload === 'string') {
        mp.gui.emmit(`window.router.setPopUp("PopupDeath", { isShown: true, places: [], deathScreenData: {}, title: '', text: ${JSON.stringify(jsonPayload)} });`);
        mp.gui.cursor.visible = true;
      }
    } catch (inner) {
      console.error('fallback openHospitalDialogData failed', inner);
    }
  }
});


// Обработка CEF->RAGE команды deathScreen.deathButton(action, placeType?)
// Добавьте/замените этот обработчик в confirm.js
gm.events.add('deathScreen.deathButton', (action, placeType) => {
  try {
    console.log('[confirm.js] deathScreen.deathButton received:', action, placeType);

    // Если передан placeType — отправляем action+placeType на сервер (реализуйте RemoteEvent на сервере)
    if (placeType) {
      callRemote('deathScreen.deathButton', action, placeType);
    } else {
      // fallback: если сервер ожидает numeric state через server:OnHospitalDialogCallback
      const actionToState = {
        waitMedics: 1,
        callMedics: 2,
        loseConsciousness: 3,
        toHospital: 3,
        selfRevive: 4
      };
      const state = actionToState[action] || 0;
      if (state) {
        callRemote('server:OnHospitalDialogCallback', state);
      } else {
        // если неизвестное действие — логируем
        console.warn('[confirm.js] Unknown action in deathScreen.deathButton:', action);
      }
    }

    // ----------- Закрываем popup и убираем курсор/меню -------------
    try {
      // закрыть popup (вызов window.router.setPopUp() без аргументов)
      mp.gui.emmit('window.router.setPopUp()');

      // скрыть курсор
      mp.gui.cursor.visible = false;

      // если у вас есть глобальная функция menuClose, вызывать её через глобальные переменные
      // (в проекте часто используется global.menuClose())
      try {
        if (global && typeof global.menuClose === 'function') {
          global.menuClose();
        }
      } catch (e) {
        // ignore if not present
      }

      // снять флаг isPopup, menuOpened как у вас в проекте
      try {
        global.isPopup = false;
      } catch (e) {}

      console.log('[confirm.js] deathScreen: popup closed client-side');
    } catch (uiErr) {
      console.warn('[confirm.js] Failed to close popup UI:', uiErr);
    }
    // ---------------------------------------------------------------

  } catch (err) {
    console.error('[confirm.js] deathScreen.deathButton handler error:', err);
  }
});
//list

let isOpenPopupList = false;

gm.events.add('popup.list.open', (header, list) => {
    mp.gui.emmit(`window.router.setPopUp("PopupSelect", {title: '${header}', elements: '${list}'});`);
    mp.gui.cursor.visible = true;
    isInterface = false;
    isOpenPopupList = true;
    if (!global.menuOpened) {
        global.menuOpen(true);
        isInterface = true;
        global.isPopup = true;
    }
})

gm.events.add('popup.list.selected', (listItem) => {
    callRemote('popup.list.callback', listItem);
    isOpenPopupList = false;
    global.closeDialog ();
})

