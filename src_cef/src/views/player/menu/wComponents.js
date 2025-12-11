export default {
    // Ключи — хеши оружия (строки), внутри только разрешённые типы компонентов:
    // Clip (все варианты), Suppressor (3), Scope (4), Scope2 (4 used as holo), Flashlight (7), Grip (8)
        "100416529": { // WEAPON_SNIPERRIFLE
        "Count": 3,
        "Components": {
            // Scopes
            "3159677559": { "Name": "WCT_SCOPE_MAX", "Desc": "WCD_SCOPE_MAX", "Price": 17000, "Type": 4 }, // AT_SCOPE_MAX
            "3527687644": { "Name": "WCT_SCOPE_LRG", "Desc": "WCD_SCOPE_LRG", "Price": 13000, "Type": 4 }, // AT_SCOPE_LARGE
            // Suppressor (if present)
            "2805810788": { "Name": "WCT_SUPP", "Desc": "WCD_AR_SUPP2", "Price": 9000, "Type": 3 } // AT_AR_SUPP_02
        }
    },

    "171789620": { // WEAPON_COMBATPDW
        "Count": 4,
        "Components": {
            // Extended clip (COMBATPDW_CLIP_02)
            "860508675": { "Name": "WCT_CLIP2", "Desc": "WCD_PDW_CLIP2", "Price": 0, "Type": 2 },
            // Grip
            "202788691": { "Name": "WCT_GRIP", "Desc": "WCD_GRIP", "Price": 900, "Type": 8 },
            // Flashlight (AR-style)
            "2076495324": { "Name": "WCT_FLASH", "Desc": "WCD_FLASH", "Price": 1200, "Type": 7 },
            // Small scope
            "2855028148": { "Name": "WCT_SCOPE_SML", "Desc": "WCD_SCOPE_SML", "Price": 1500, "Type": 4 }
        }
    },

    "324215364": { // WEAPON_MICROSMG
        "Count": 4,
        "Components": {
            // Extended clip
            "283556395": { "Name": "WCT_CLIP2", "Desc": "WCDMSMG_CLIP2", "Price": 0, "Type": 2 },
            // Flashlight (pistol-style for this weapon in data)
            "899381934": { "Name": "WCT_FLASH", "Desc": "WCD_FLASH", "Price": 800, "Type": 7 },
            // Small scope (macro)
            "2637152041": { "Name": "WCT_SCOPE_MAC", "Desc": "WCD_SCOPE_MAC", "Price": 1100, "Type": 4 },
            // Suppressor (PDW-style)
            "2805810788": { "Name": "WCT_SUPP", "Desc": "WCD_AR_SUPP2", "Price": 1000, "Type": 3 }
        }
    },

    "453432689": { // WEAPON_PISTOL
        "Count": 3,
        "Components": {
            // Extended clip
            "3978713628": { "Name": "WCT_CLIP2", "Desc": "WCD_P_CLIP2", "Price": 0, "Type": 2 },
            // Flashlight (pistol flashlight hash)
            "899381934": { "Name": "WCT_FLASH", "Desc": "WCD_FLASH", "Price": 400, "Type": 7 }, // COMPONENT_AT_PI_FLSH
            // Suppressor (pistol suppressor)
            "1709866683": { "Name": "WCT_SUPP", "Desc": "WCD_PI_SUPP", "Price": 600, "Type": 3 } // COMPONENT_AT_PI_SUPP_02
        }
    },

    "487013001": { // WEAPON_PUMPSHOTGUN
        "Count": 2,
        "Components": {
            // Flashlight (AR-style)
            "2076495324": { "Name": "WCT_FLASH", "Desc": "WCD_FLASH", "Price": 500, "Type": 7 },
            // Suppressor (shotgun suppressor)
            "3859329886": { "Name": "WCT_SUPP", "Desc": "WCD_SR_SUPP", "Price": 700, "Type": 3 }
            // Магазины у некоторых дробовиков — часто отсутствуют как отдельный "увеличенный" магазин, поэтому не включаем лишние
        }
    },

    "584646201": { // WEAPON_APPISTOL
        "Count": 3,
        "Components": {
            // Extended clip
            "614078421": { "Name": "WCT_CLIP2", "Desc": "WCD_AP_CLIP2", "Price": 0, "Type": 2 },
            // Flashlight (pistol-style)
            "899381934": { "Name": "WCT_FLASH", "Desc": "WCD_FLASH", "Price": 900, "Type": 7 },
            // Suppressor
            "3271853210": { "Name": "WCT_SUPP", "Desc": "WCD_PI_SUPP", "Price": 1100, "Type": 3 }
        }
    },

    "736523883": { // WEAPON_SMG
        "Count": 4,
        "Components": {
            // Extended clip
            "889808635": { "Name": "WCT_CLIP2", "Desc": "WCD_SMG_CLIP2", "Price": 0, "Type": 2 },
            // Small scope (macro)
            "1019656791": { "Name": "WCT_SCOPE_MAC", "Desc": "WCD_SCOPE_MAC", "Price": 1100, "Type": 4 }, // COMPONENT_AT_SCOPE_MACRO_02
            // Flashlight (AR-style)
            "2076495324": { "Name": "WCT_FLASH", "Desc": "WCD_FLASH", "Price": 1200, "Type": 7 },
            // Suppressor (pistol suppressor used on some SMGs)
            "3271853210": { "Name": "WCT_SUPP", "Desc": "WCD_PI_SUPP", "Price": 1400, "Type": 3 }
        }
    },

    "1593441988": { // WEAPON_COMBATPISTOL
        "Count": 3,
        "Components": {
            // Extended clip
            "3598405421": { "Name": "WCT_CLIP2", "Desc": "WCD_CP_CLIP2", "Price": 0, "Type": 2 },
            // Flashlight (pistol-style)
            "899381934": { "Name": "WCT_FLASH", "Desc": "WCD_FLASH", "Price": 400, "Type": 7 },
            // Suppressor
            "3271853210": { "Name": "WCT_SUPP", "Desc": "WCD_PI_SUPP", "Price": 600, "Type": 3 }
        }
    },

    "2132975508": { // WEAPON_BULLPUPRIFLE
        "Count": 4,
        "Components": {
            // Extended clip
            "3009973007": { "Name": "WCT_CLIP2", "Desc": "WCD_BRIF_CLIP2", "Price": 0, "Type": 2 },
            // Grip
            "202788691": { "Name": "WCT_GRIP", "Desc": "WCD_GRIP", "Price": 900, "Type": 8 },
            // Small scope
            "2855028148": { "Name": "WCT_SCOPE_SML", "Desc": "WCD_SCOPE_SML", "Price": 900, "Type": 4 },
            // Suppressor (AR-style)
            "2205435306": { "Name": "WCT_SUPP", "Desc": "WCD_AR_SUPP", "Price": 700, "Type": 3 }
        }
    },

    "3342088282": { // WEAPON_MARKSMANRIFLE (example entry from data)
        "Count": 4,
        "Components": {
            // Extended clip (if present in data)
            "2053798780": { "Name": "WCT_CLIP1", "Desc": "WCD_MKRF_CLIP1", "Price": 0, "Type": 2 },
            // Large scope
            "471997210": { "Name": "WCT_SCOPE_LRG", "Desc": "WCD_SCOPE_LRF", "Price": 2300, "Type": 4 },
            // Flashlight
            "2076495324": { "Name": "WCT_FLASH", "Desc": "WCD_FLASH", "Price": 1300, "Type": 7 },
            // Suppressor
            "2205435306": { "Name": "WCT_SUPP", "Desc": "WCD_AR_SUPP", "Price": 1600, "Type": 3 }
        }
    },

    "2210333304": { // WEAPON_CARBINERIFLE
        "Count": 4,
        "Components": {
            // Extended clip
            "2433783441": { "Name": "WCT_CLIP1", "Desc": "WCD_CR_CLIP1", "Price": 0, "Type": 1 },
            "2433783441": { "Name": "WCT_CLIP2", "Desc": "WCD_CR_CLIP2", "Price": 0, "Type": 2 },
            // Grip
            "202788691": { "Name": "WCT_GRIP", "Desc": "WCD_GRIP", "Price": 1300, "Type": 8 },
            // Large/Medium scope (choose medium/large available)
            "2698550338": { "Name": "WCT_SCOPE_LRG", "Desc": "WCD_SCOPE_LRG", "Price": 1400, "Type": 4 },
            // Flashlight
            "2076495324": { "Name": "WCT_FLASH", "Desc": "WCD_FLASH", "Price": 900, "Type": 7 }
        }
    },
"4208062921": { // WEAPON_CARBINERIFLE_MK2 (правильный хеш)
    "Count": 7,
    "Components": {
        // ✅ ИСПРАВЛЕНО: Используем GetHashKey вместо десятичных
        "3127044405": { "Name": "WCT_CLIP2", "Desc": "WCD_CR_CLIP2", "Price": 0, "Type": 0 }, // COMPONENT_CARBINERIFLE_MK2_CLIP_02
        "626875735": { "Name": "WCT_CLIP_AP", "Desc": "WCD_CLIP_AP", "Price": 0, "Type": 1 }, // COMPONENT_CARBINERIFLE_MK2_CLIP_ARMORPIERCING
        "2205435306": { "Name": "WCT_SUPP", "Desc": "WCD_AR_SUPP", "Price": 1100, "Type": 2 }, // COMPONENT_AT_AR_SUPP
        "2698550338": { "Name": "WCT_SCOPE_MED", "Desc": "WCD_SCOPE_MED", "Price": 1400, "Type":  3 }, // COMPONENT_AT_SCOPE_MEDIUM_MK2
        "1785353629": { "Name": "WCT_HOLO", "Desc": "WCD_HOLO", "Price": 1500, "Type": 4 }, // COMPONENT_AT_SIGHTS
        "2076495324": { "Name": "WCT_FLASH", "Desc": "WCD_FLASH", "Price": 900, "Type": 5 }, // COMPONENT_AT_AR_FLSH
        "202788691": { "Name": "WCT_GRIP", "Desc": "WCD_GRIP", "Price": 1300, "Type": 6 } // COMPONENT_AT_AR_AFGRIP
    }
},
    // MK2 & other special weapons: keep core allowed attachments (single extended clip, scopes, suppressor, flash, grip/holo)
    "2024373456": { // WEAPON_SMG_MK2
        "Count": 5,
        "Components": {
            "1277460590": { "Name": "WCT_CLIP1", "Desc": "WCD_CLIP1", "Price": 0, "Type": 2 }, // default / primary clip (use as single clip option here)
            "2681951826": { "Name": "WCT_HOLO", "Desc": "WCD_HOLO", "Price": 1500, "Type": 4 }, // holographic
            "1038927834": { "Name": "WCT_SCOPE_SML2", "Desc": "WCD_SCOPE_SML", "Price": 1500, "Type": 4 }, // medium scope
            "2076495324": { "Name": "WCT_FLASH", "Desc": "WCD_FLASH", "Price": 1200, "Type": 7 },
            "3271853210": { "Name": "WCT_SUPP", "Desc": "WCD_PI_SUPP", "Price": 1000, "Type": 3 }
        }
    }
}
    // Дополнительные оружия можно добавить по тому же шаблону: 
    //  - указываем только 1 увеличенный магазин (hash для CLIP_02/extended если присутствует),
    //  - если в data есть CLIP_AP / CLIP_FMJ / CLIP_INC — можно добавить отдельным ключом (type 2) чтобы игрок видел бронебойные/специальные патроны,
    //  - указываем точный flashlight hash (pistol vs ar) из weapon data чтобы мод корректно мапился при установке.
