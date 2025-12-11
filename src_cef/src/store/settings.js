import { writable } from 'svelte/store';

window.settingsStore = {};

export const storeSettings = writable({
    // Старые поля (с большой буквы для совместимости)
    Timestamp: false,
    ChatShadow: true,
    Fontsize: 16,
    ChatOpacity: 100,
    Chatalpha: 100,
    Pagesize: 10,
    Widthsize: 50,
    Transition: 0,
    WalkStyle: 0,
    FacialEmotion: 0,
    Deaf: false,
    TagsHead: true,
    HudToggled: true,
    HudStats: true,
    HudSpeed: true,
    HudOnline: true,
    HudLocation: true,
    HudKey: true,
    HudMap: true,
    VolumeInterface: 100,
    VolumeQuest: 50,
    VolumeAmbient: 80,
    VolumePhoneRadio: 50,
    VolumeVoice: 100,
    VolumeRadio: 70,
    VolumeBoombox: 70,
    FirstMute: false,
    DistancePlayer: 50,
    DistanceVehicle: 50,
    cPToggled: false,
    cPWidth: 2,
    cPGap: 2,
    cPDot: true,
    cPThickness: 0,
    cPColorR: 255,
    cPColorG: 255,
    cPColorB: 255,
    cPOpacity: 9,
    cPCheck: true,
    APunishments: false,
    CircleVehicle: false,
    cEfValue: 0,
    notifCount: 2,
    hitPoint: false,
 // ← ДОБАВЬ (если нет)
    schat: true,
    hud: true,
    radar: true,
    ids: true,
    compass: true,
    displayChatMsg: true,
    schatPunish: true,
        speedoType: "Minimalistic_2",

    // ← ТОЛЬКО С МАЛЕНЬКОЙ БУКВЫ (убрали дубликаты)
    chatSettings: "standard",
    chatTheme: "Clean",
    chatFont: "ProximaNova",
    chatFontStyle: "Bold",
    chatBorderType: "Stroke",
    chatTimestamp: "DateTime",
    chatFontSize: 14,
    chatFontShadow: 0,
    chatFontShadowAlpha: 0,
    chatLinesMargin: 0,
    chatLineHeight: 0,
    chatWidth: 500,
    chatHeight: 500,
    
    chatCmdsSettings: "standard",
    chatCmdsFont: "ProximaNova",
    chatCmdsFontStyle: "Regular",
    chatCmdsFontSize: 12,
    chatCmdsOrder: "/report,/b,/me,/do,/try,/s,/w,/m,/f,/fb,/c,/cb,/dep,/mark,/mark2,/gnews,/wnews,/adv,/vmute,/pay"
});

window.settingsStore.init = (json) => {
    try {
        const parsed = JSON.parse(json);
        console.log("🟢 settingsStore.init called with:", parsed);
        
        storeSettings.update(current => {
            const updated = { ...current };
            
            for (const key in parsed) {
                if (parsed.hasOwnProperty(key)) {
                    // Поддержка обоих форматов
                    if (key in updated) {
                        updated[key] = parsed[key];
                        console.log(`  ✅ Loaded ${key}:`, parsed[key]);
                    }
                }
            }
            
            return updated;
        });
        
        console.log("✅ Settings loaded successfully");
    } catch (e) {
        console.error("❌ settingsStore.init error:", e);
    }
}