using GTANetworkAPI;
using NeptuneEvo.Handles;
using NeptuneEvo.Chars.Models;
using Newtonsoft.Json;
using Redage.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using NeptuneEvo.Character;

namespace NeptuneEvo.Chars
{
    class WeaponComponents : Script
    {
        private static readonly nLog Log = new nLog("Chars.WeaponComponents");

        public static IReadOnlyDictionary<uint, wComponentsData> WeaponsComponents = new Dictionary<uint, wComponentsData>()
        {
			// Sniper Rifle
			{ NAPI.Util.GetHashKey("WEAPON_SNIPERRIFLE"), new wComponentsData(4, new Dictionary<uint, wComponentData>()
                {
                    { NAPI.Util.GetHashKey("COMPONENT_AT_SCOPE_LARGE"), new wComponentData("WCT_SCOPE_LRG", "WCD_SCOPE_LRG", 13000, wComponentsType.Scope) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_SCOPE_MAX"), new wComponentData("WCT_SCOPE_MAX", "WCD_SCOPE_MAX", 17000, wComponentsType.Scope) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_AR_SUPP_02"), new wComponentData("WCT_SUPP", "WCD_AR_SUPP2", 9000, wComponentsType.Suppressor) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_AR_FLSH"), new wComponentData("WCT_FLASH", "WCD_FLASH", 11000, wComponentsType.Flashlight) },
                })
            },

			// PDW / SMG family
			{ NAPI.Util.GetHashKey("WEAPON_COMBATPDW"), new wComponentsData(6, new Dictionary<uint, wComponentData>()
                {
                    { NAPI.Util.GetHashKey("COMPONENT_COMBATPDW_CLIP_01"), new wComponentData("WCT_CLIP1", "WCD_PDW_CLIP1", 0, wComponentsType.Clip) },
                    { NAPI.Util.GetHashKey("COMPONENT_COMBATPDW_CLIP_02"), new wComponentData("WCT_CLIP2", "WCD_PDW_CLIP2", 0, wComponentsType.Clip) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_AR_AFGRIP"), new wComponentData("WCT_GRIP", "WCD_GRIP", 900, wComponentsType.Grip) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_AR_FLSH"), new wComponentData("WCT_FLASH", "WCD_FLASH", 1200, wComponentsType.Flashlight) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_SCOPE_SMALL"), new wComponentData("WCT_SCOPE_SML", "WCD_SCOPE_SML", 1500, wComponentsType.Scope) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_PI_SUPP"), new wComponentData("WCT_SUPP", "WCD_PI_SUPP", 900, wComponentsType.Suppressor) },
                })
            },

            { NAPI.Util.GetHashKey("WEAPON_MICROSMG"), new wComponentsData(6, new Dictionary<uint, wComponentData>()
                {
                    { NAPI.Util.GetHashKey("COMPONENT_MICROSMG_CLIP_01"), new wComponentData("WCT_CLIP1", "WCDMSMG_CLIP1", 0, wComponentsType.Clip) },
                    { NAPI.Util.GetHashKey("COMPONENT_MICROSMG_CLIP_02"), new wComponentData("WCT_CLIP2", "WCDMSMG_CLIP2", 0, wComponentsType.Clip) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_PI_FLSH"), new wComponentData("WCT_FLASH", "WCD_FLASH", 800, wComponentsType.Flashlight) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_SCOPE_MACRO"), new wComponentData("WCT_SCOPE_MAC", "WCD_SCOPE_MAC", 1100, wComponentsType.Scope) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_AR_SUPP_02"), new wComponentData("WCT_SUPP", "WCD_AR_SUPP2", 1000, wComponentsType.Suppressor) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_AR_AFGRIP"), new wComponentData("WCT_GRIP", "WCD_GRIP", 900, wComponentsType.Grip) },
                })
            },

			// Pistols
			{ NAPI.Util.GetHashKey("WEAPON_PISTOL"), new wComponentsData(5, new Dictionary<uint, wComponentData>()
                {
                    { NAPI.Util.GetHashKey("COMPONENT_PISTOL_CLIP_01"), new wComponentData("WCT_CLIP1", "WCD_P_CLIP1", 0, wComponentsType.Clip) },
                    { NAPI.Util.GetHashKey("COMPONENT_PISTOL_CLIP_02"), new wComponentData("WCT_CLIP2", "WCD_P_CLIP2", 0, wComponentsType.Clip) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_PI_FLSH"), new wComponentData("WCT_FLASH", "WCD_FLASH", 400, wComponentsType.Flashlight) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_PI_SUPP_02"), new wComponentData("WCT_SUPP", "WCD_PI_SUPP", 600, wComponentsType.Suppressor) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_PI_RAIL"), new wComponentData("WCT_SCOPE_PI", "WCD_SCOPE_PI", 1400, wComponentsType.Scope) }, // small rail-sight as Scope
				})
            },

			// Shotguns
			{ NAPI.Util.GetHashKey("WEAPON_PUMPSHOTGUN"), new wComponentsData(4, new Dictionary<uint, wComponentData>()
                {
                    { NAPI.Util.GetHashKey("COMPONENT_PUMPSHOTGUN_CLIP_01"), new wComponentData("WCT_CLIP1", "WCD_INVALID", 0, wComponentsType.Clip) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_AR_FLSH"), new wComponentData("WCT_FLASH", "WCD_FLASH", 500, wComponentsType.Flashlight) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_SR_SUPP"), new wComponentData("WCT_SUPP", "WCD_SR_SUPP", 700, wComponentsType.Suppressor) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_AR_AFGRIP"), new wComponentData("WCT_GRIP", "WCD_GRIP", 600, wComponentsType.Grip) },
                })
            },

			// AP Pistol / Machine pistols
			{ NAPI.Util.GetHashKey("WEAPON_APPISTOL"), new wComponentsData(4, new Dictionary<uint, wComponentData>()
                {
                    { NAPI.Util.GetHashKey("COMPONENT_APPISTOL_CLIP_01"), new wComponentData("WCT_CLIP1", "WCD_AP_CLIP1", 0, wComponentsType.Clip) },
                    { NAPI.Util.GetHashKey("COMPONENT_APPISTOL_CLIP_02"), new wComponentData("WCT_CLIP2", "WCD_AP_CLIP2", 0, wComponentsType.Clip) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_PI_FLSH"), new wComponentData("WCT_FLASH", "WCD_FLASH", 900, wComponentsType.Flashlight) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_PI_SUPP"), new wComponentData("WCT_SUPP", "WCD_PI_SUPP", 1100, wComponentsType.Suppressor) },
                })
            },

			// SMG
			{ NAPI.Util.GetHashKey("WEAPON_SMG"), new wComponentsData(6, new Dictionary<uint, wComponentData>()
                {
                    { NAPI.Util.GetHashKey("COMPONENT_SMG_CLIP_01"), new wComponentData("WCT_CLIP1", "WCD_SMG_CLIP1", 0, wComponentsType.Clip) },
                    { NAPI.Util.GetHashKey("COMPONENT_SMG_CLIP_02"), new wComponentData("WCT_CLIP2", "WCD_SMG_CLIP2", 0, wComponentsType.Clip) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_AR_AFGRIP"), new wComponentData("WCT_GRIP", "WCD_GRIP", 900, wComponentsType.Grip) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_SCOPE_MACRO_02"), new wComponentData("WCT_SCOPE_MAC", "WCD_SCOPE_MAC", 1100, wComponentsType.Scope) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_AR_FLSH"), new wComponentData("WCT_FLASH", "WCD_FLASH", 1200, wComponentsType.Flashlight) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_PI_SUPP"), new wComponentData("WCT_SUPP", "WCD_PI_SUPP", 1400, wComponentsType.Suppressor) },
                })
            },

			// Combat Pistol
			{ NAPI.Util.GetHashKey("WEAPON_COMBATPISTOL"), new wComponentsData(4, new Dictionary<uint, wComponentData>()
                {
                    { NAPI.Util.GetHashKey("COMPONENT_COMBATPISTOL_CLIP_01"), new wComponentData("WCT_CLIP1", "WCD_CP_CLIP1", 0, wComponentsType.Clip) },
                    { NAPI.Util.GetHashKey("COMPONENT_COMBATPISTOL_CLIP_02"), new wComponentData("WCT_CLIP2", "WCD_CP_CLIP2", 0, wComponentsType.Clip) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_PI_FLSH"), new wComponentData("WCT_FLASH", "WCD_FLASH", 400, wComponentsType.Flashlight) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_PI_SUPP"), new wComponentData("WCT_SUPP", "WCD_PI_SUPP", 600, wComponentsType.Suppressor) },
                })
            },

			// Bullpup Rifle
			{ NAPI.Util.GetHashKey("WEAPON_BULLPUPRIFLE"), new wComponentsData(6, new Dictionary<uint, wComponentData>()
                {
                    { NAPI.Util.GetHashKey("COMPONENT_BULLPUPRIFLE_CLIP_01"), new wComponentData("WCT_CLIP1", "WCD_BRIF_CLIP1", 0, wComponentsType.Clip) },
                    { NAPI.Util.GetHashKey("COMPONENT_BULLPUPRIFLE_CLIP_02"), new wComponentData("WCT_CLIP2", "WCD_BRIF_CLIP2", 0, wComponentsType.Clip) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_AR_AFGRIP"), new wComponentData("WCT_GRIP", "WCD_GRIP", 900, wComponentsType.Grip) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_AR_FLSH"), new wComponentData("WCT_FLASH", "WCD_FLASH", 1200, wComponentsType.Flashlight) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_SCOPE_SMALL"), new wComponentData("WCT_SCOPE_SML", "WCD_SCOPE_SML", 900, wComponentsType.Scope) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_AR_SUPP"), new wComponentData("WCT_SUPP", "WCD_AR_SUPP", 700, wComponentsType.Suppressor) },
                })
            },

			// Heavy Pistol / Revolver / Pistol50
			{ NAPI.Util.GetHashKey("WEAPON_HEAVYPISTOL"), new wComponentsData(4, new Dictionary<uint, wComponentData>()
                {
                    { NAPI.Util.GetHashKey("COMPONENT_HEAVYPISTOL_CLIP_01"), new wComponentData("WCT_CLIP1", "WCD_HPST_CLIP1", 0, wComponentsType.Clip) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_PI_FLSH"), new wComponentData("WCT_FLASH", "WCD_FLASH", 900, wComponentsType.Flashlight) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_PI_SUPP"), new wComponentData("WCT_SUPP", "WCD_PI_SUPP", 1100, wComponentsType.Suppressor) },
                })
            },

            { NAPI.Util.GetHashKey("WEAPON_REVOLVER"), new wComponentsData(3, new Dictionary<uint, wComponentData>()
                {
                    { NAPI.Util.GetHashKey("COMPONENT_REVOLVER_CLIP_01"), new wComponentData("WCT_CLIP1", "WCD_CLIP1", 0, wComponentsType.Clip) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_PI_FLSH"), new wComponentData("WCT_FLASH", "WCD_FLASH", 900, wComponentsType.Flashlight) },
                })
            },

			// Marksman / Sniper family (with grips/scopes)
			{ NAPI.Util.GetHashKey("WEAPON_MARKSMANRIFLE"), new wComponentsData(6, new Dictionary<uint, wComponentData>()
                {
                    { NAPI.Util.GetHashKey("COMPONENT_MARKSMANRIFLE_CLIP_01"), new wComponentData("WCT_CLIP1", "WCD_MKRF_CLIP1", 0, wComponentsType.Clip) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_SCOPE_LARGE_FIXED_ZOOM"), new wComponentData("WCT_SCOPE_LRG", "WCD_SCOPE_LRF", 16000, wComponentsType.Scope) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_SIGHTS"), new wComponentData("WCT_HOLO", "WCD_HOLO", 12000, wComponentsType.Scope2) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_AR_AFGRIP"), new wComponentData("WCT_GRIP", "WCD_GRIP", 9000, wComponentsType.Grip) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_AR_SUPP"), new wComponentData("WCT_SUPP", "WCD_AR_SUPP", 14000, wComponentsType.Suppressor) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_AR_FLSH"), new wComponentData("WCT_FLASH", "WCD_FLASH", 11000, wComponentsType.Flashlight) },
                })
            },

			// Assault / Rifle family
			{ NAPI.Util.GetHashKey("WEAPON_ASSAULTRIFLE"), new wComponentsData(7, new Dictionary<uint, wComponentData>()
                {
                    { NAPI.Util.GetHashKey("COMPONENT_ASSAULTRIFLE_CLIP_01"), new wComponentData("WCT_CLIP1", "WCD_AR_CLIP1", 0, wComponentsType.Clip) },
                    { NAPI.Util.GetHashKey("COMPONENT_ASSAULTRIFLE_CLIP_02"), new wComponentData("WCT_CLIP2", "WCD_AR_CLIP2", 0, wComponentsType.Clip) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_AR_AFGRIP"), new wComponentData("WCT_GRIP", "WCD_GRIP", 1200, wComponentsType.Grip) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_AR_FLSH"), new wComponentData("WCT_FLASH", "WCD_FLASH", 900, wComponentsType.Flashlight) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_SCOPE_MACRO"), new wComponentData("WCT_SCOPE_MAC", "WCD_SCOPE_MAC", 1400, wComponentsType.Scope) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_SIGHTS"), new wComponentData("WCT_HOLO", "WCD_HOLO", 1500, wComponentsType.Scope2) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_AR_SUPP_02"), new wComponentData("WCT_SUPP", "WCD_AR_SUPP2", 1100, wComponentsType.Suppressor) },
                })
            },

            { NAPI.Util.GetHashKey("WEAPON_CARBINERIFLE"), new wComponentsData(6, new Dictionary<uint, wComponentData>()
                {
                    { NAPI.Util.GetHashKey("COMPONENT_CARBINERIFLE_CLIP_01"), new wComponentData("WCT_CLIP1", "WCD_CR_CLIP1", 0, wComponentsType.Clip) },
                    { NAPI.Util.GetHashKey("COMPONENT_CARBINERIFLE_CLIP_02"), new wComponentData("WCT_CLIP2", "WCD_CR_CLIP2", 0, wComponentsType.Clip) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_AR_AFGRIP"), new wComponentData("WCT_GRIP", "WCD_GRIP", 1300, wComponentsType.Grip) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_AR_FLSH"), new wComponentData("WCT_FLASH", "WCD_FLASH", 900, wComponentsType.Flashlight) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_SCOPE_MEDIUM"), new wComponentData("WCT_SCOPE_LRG", "WCD_SCOPE_LRG", 1400, wComponentsType.Scope) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_AR_SUPP"), new wComponentData("WCT_SUPP", "WCD_AR_SUPP", 1100, wComponentsType.Suppressor) },
                })
            },
{ NAPI. Util.GetHashKey("WEAPON_CARBINERIFLE_MK2"), new wComponentsData(7, new Dictionary<uint, wComponentData>()
    {
        // ✅ БАЗОВЫЙ МАГАЗИН (ОБЯЗАТЕЛЕН!)
        { NAPI.Util.GetHashKey("COMPONENT_CARBINERIFLE_MK2_CLIP_01"), new wComponentData("WCT_CLIP1", "WCD_CR_CLIP1", 0, wComponentsType.Clip) },
        
        // ✅ УВЕЛИЧЕННЫЙ МАГАЗИН
        { NAPI.Util.GetHashKey("COMPONENT_CARBINERIFLE_MK2_CLIP_02"), new wComponentData("WCT_CLIP2", "WCD_CR_CLIP2", 0, wComponentsType.Clip) },
        
        // ✅ БРОНЕБОЙНЫЙ МАГАЗИН
        { NAPI. Util.GetHashKey("COMPONENT_CARBINERIFLE_MK2_CLIP_ARMORPIERCING"), new wComponentData("WCT_CLIP_AP", "WCD_CLIP_AP", 0, wComponentsType.Clip2) },

        { NAPI.Util.GetHashKey("COMPONENT_AT_AR_SUPP"), new wComponentData("WCT_SUPP", "WCD_AR_SUPP", 1100, wComponentsType.Suppressor) },
        { NAPI.Util.GetHashKey("COMPONENT_AT_SCOPE_MEDIUM_MK2"), new wComponentData("WCT_SCOPE_MED", "WCD_SCOPE_MED", 1400, wComponentsType.Scope) },
        { NAPI.Util.GetHashKey("COMPONENT_AT_SIGHTS"), new wComponentData("WCT_HOLO", "WCD_HOLO", 1500, wComponentsType.Scope2) },
        { NAPI.Util.GetHashKey("COMPONENT_AT_AR_FLSH"), new wComponentData("WCT_FLASH", "WCD_FLASH", 900, wComponentsType. Flashlight) },
        { NAPI.Util.GetHashKey("COMPONENT_AT_AR_AFGRIP"), new wComponentData("WCT_GRIP", "WCD_GRIP", 1300, wComponentsType.Grip) },
    })
},

            { NAPI.Util.GetHashKey("WEAPON_ADVANCEDRIFLE"), new wComponentsData(5, new Dictionary<uint, wComponentData>()
                {
                    { NAPI.Util.GetHashKey("COMPONENT_ADVANCEDRIFLE_CLIP_01"), new wComponentData("WCT_CLIP1", "WCD_AR_CLIP1", 0, wComponentsType.Clip) },
                    { NAPI.Util.GetHashKey("COMPONENT_ADVANCEDRIFLE_CLIP_02"), new wComponentData("WCT_CLIP2", "WCD_AR_CLIP2", 0, wComponentsType.Clip) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_AR_FLSH"), new wComponentData("WCT_FLASH", "WCD_FLASH", 900, wComponentsType.Flashlight) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_AR_SUPP"), new wComponentData("WCT_SUPP", "WCD_AR_SUPP", 1100, wComponentsType.Suppressor) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_SCOPE_SMALL"), new wComponentData("WCT_SCOPE_SML", "WCD_SCOPE_SML", 1400, wComponentsType.Scope) },
                })
            },

			// Bullpup / Other shotguns handled above
			{ NAPI.Util.GetHashKey("WEAPON_BULLPUPSHOTGUN"), new wComponentsData(5, new Dictionary<uint, wComponentData>()
                {
                    { NAPI.Util.GetHashKey("COMPONENT_BULLPUPSHOTGUN_CLIP_01"), new wComponentData("WCT_CLIP1", "WCD_INVALID", 0, wComponentsType.Clip) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_AR_AFGRIP"), new wComponentData("WCT_GRIP", "WCD_GRIP", 400, wComponentsType.Grip) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_AR_FLSH"), new wComponentData("WCT_FLASH", "WCD_FLASH", 600, wComponentsType.Flashlight) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_AR_SUPP_02"), new wComponentData("WCT_SUPP", "WCD_AR_SUPP2", 800, wComponentsType.Suppressor) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_SIGHTS"), new wComponentData("WCT_HOLO", "WCD_HOLO", 1200, wComponentsType.Scope2) },
                })
            },

			// Misc / MK2 weapons keep scope2/holo entries as Scope2 (where available)
			{ NAPI.Util.GetHashKey("WEAPON_PUMPSHOTGUN_MK2"), new wComponentsData(6, new Dictionary<uint, wComponentData>()
                {
                    { NAPI.Util.GetHashKey("COMPONENT_PUMPSHOTGUN_MK2_CLIP_01"), new wComponentData("WCT_SHELL", "WCD_SHELL", 0, wComponentsType.Clip) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_SIGHTS"), new wComponentData("WCT_HOLO", "WCD_HOLO", 1200, wComponentsType.Scope2) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_SCOPE_MACRO_MK2"), new wComponentData("WCT_SCOPE_MAC2", "WCD_SCOPE_MAC", 1200, wComponentsType.Scope) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_SCOPE_SMALL_MK2"), new wComponentData("WCT_SCOPE_SML2", "WCD_SCOPE_SML", 1200, wComponentsType.Scope) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_AR_FLSH"), new wComponentData("WCT_FLASH", "WCD_FLASH", 600, wComponentsType.Flashlight) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_SR_SUPP_03"), new wComponentData("WCT_SUPP", "WCD_SR_SUPP", 800, wComponentsType.Suppressor) },
                })
            },

            { NAPI.Util.GetHashKey("WEAPON_SMG_MK2"), new wComponentsData(8, new Dictionary<uint, wComponentData>()
                {
                    { NAPI.Util.GetHashKey("COMPONENT_SMG_MK2_CLIP_01"), new wComponentData("WCT_CLIP1", "WCD_CLIP1", 0, wComponentsType.Clip) },
                    { NAPI.Util.GetHashKey("COMPONENT_SMG_MK2_CLIP_02"), new wComponentData("WCT_CLIP2", "WCD_CLIP2", 0, wComponentsType.Clip) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_AR_FLSH"), new wComponentData("WCT_FLASH", "WCD_FLASH", 1200, wComponentsType.Flashlight) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_SIGHTS_SMG"), new wComponentData("WCT_HOLO", "WCD_HOLO", 1500, wComponentsType.Scope2) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_SCOPE_MACRO_02_SMG_MK2"), new wComponentData("WCT_SCOPE_MAC2", "WCD_SCOPE_MAC", 1500, wComponentsType.Scope) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_SCOPE_SMALL_SMG_MK2"), new wComponentData("WCT_SCOPE_SML2", "WCD_SCOPE_SML", 1500, wComponentsType.Scope) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_PI_SUPP"), new wComponentData("WCT_SUPP", "WCD_PI_SUPP", 1000, wComponentsType.Suppressor) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_AR_AFGRIP_02"), new wComponentData("WCT_GRIP2", "WCD_GRIP", 1200, wComponentsType.Grip) },
                })
            },

            { NAPI.Util.GetHashKey("WEAPON_MACHINEPISTOL"), new wComponentsData(3, new Dictionary<uint, wComponentData>()
                {
                    { NAPI.Util.GetHashKey("COMPONENT_MACHINEPISTOL_CLIP_01"), new wComponentData("WCT_CLIP1", "WCD_MCHP_CLIP1", 0, wComponentsType.Clip) },
                    { NAPI.Util.GetHashKey("COMPONENT_MACHINEPISTOL_CLIP_02"), new wComponentData("WCT_CLIP2", "WCD_MCHP_CLIP2", 0, wComponentsType.Clip) },
                    { NAPI.Util.GetHashKey("COMPONENT_AT_PI_SUPP"), new wComponentData("WCT_SUPP", "WCD_PI_SUPP", 900, wComponentsType.Suppressor) },
                })
            },
        };


        private static wComponentsType MapItemIdToType(ItemId itemId)
        {
            switch (itemId)
            {
                case ItemId.cClip:
                    return wComponentsType.Clip; // ✅ Увеличенный магазин

                case ItemId.cClip2:
                    return wComponentsType.Clip2; // ✅ ИСПРАВЛЕНО: Бронебойный магазин

                case ItemId.cSuppressor:
                    return wComponentsType.Suppressor;

                case ItemId.cScope:
                    return wComponentsType.Scope;

                case ItemId.cScope2:
                    return wComponentsType.Scope2;

                case ItemId.cFlashlight:
                    return wComponentsType.Flashlight;

                case ItemId.cGrip:
                    return wComponentsType.Grip;

                default:
                    return wComponentsType.Invalid;
            }
        }

        // Helper: select the "single" clip we will use (prefer extended / drum / box), and optionally include AP
        private static List<uint> SelectPreferredComponents(Dictionary<uint, wComponentData> components)
        {
            var result = new List<uint>();

            // ✅ ИСПРАВЛЕНО: Отдельно собираем Clip и Clip2
            var clips = components.Where(kv => kv.Value.Type == wComponentsType.Clip).ToList();
            var clips2 = components.Where(kv => kv.Value.Type == wComponentsType.Clip2).ToList();

            // ✅ ПРИОРИТЕТ: Если есть Clip2 (бронебойный) — берём его
            var clipAP = clips2.FirstOrDefault(kv =>
                kv.Value.Name != null && (kv.Value.Name.ToUpper().Contains("AP") ||
                                          kv.Value.Name.ToUpper().Contains("ARMORPIERCING") ||
                                          kv.Value.Name.ToUpper().Contains("CLIP_AP"))
            );

            if (clipAP.Equals(default(KeyValuePair<uint, wComponentData>)) && clips2.Count > 0)
            {
                clipAP = clips2.First();
            }

            // ✅ ЕСЛИ ЕСТЬ БРОНЕБОЙНЫЙ — ИСПОЛЬЗУЕМ ТОЛЬКО ЕГО
            if (!clipAP.Equals(default(KeyValuePair<uint, wComponentData>)))
            {
                result.Add(clipAP.Key);
            }
            else
            {
                // ✅ ИНАЧЕ — ИСПОЛЬЗУЕМ ОБЫЧНЫЙ УВЕЛИЧЕННЫЙ МАГАЗИН
                var clipExtended = clips.FirstOrDefault(kv =>
                    kv.Value.Name != null && (kv.Value.Name.ToUpper().Contains("CLIP2") ||
                                              kv.Value.Name.ToUpper().Contains("CLIP_02") ||
                                              kv.Value.Name.ToUpper().Contains("CLIP_DRM") ||
                                              kv.Value.Name.ToUpper().Contains("CLIP_BOX") ||
                                              kv.Value.Name.ToUpper().Contains("DRM") ||
                                              kv.Value.Name.ToUpper().Contains("BOX"))
                );

                if (clipExtended.Equals(default(KeyValuePair<uint, wComponentData>)) && clips.Count > 0)
                {
                    var nonDefault = clips.FirstOrDefault(kv => !(kv.Value.Name != null && kv.Value.Name.ToUpper().Contains("CLIP1")));
                    clipExtended = nonDefault.Equals(default(KeyValuePair<uint, wComponentData>)) ? clips.First() : nonDefault;
                }

                if (!clipExtended.Equals(default(KeyValuePair<uint, wComponentData>)))
                    result.Add(clipExtended.Key);
            }

            // ✅ Scopes:   prefer small and large separately
            var scopes = components.Where(kv => kv.Value.Type == wComponentsType.Scope).ToList();
            uint? smallScope = null, largeScope = null;

            if (scopes.Count > 0)
            {
                var small = scopes.FirstOrDefault(kv =>
                    kv.Value.Name != null && (kv.Value.Name.ToUpper().Contains("SML") ||
                                              kv.Value.Name.ToUpper().Contains("MAC") ||
                                              kv.Value.Name.ToUpper().Contains("MICRO")));

                var large = scopes.FirstOrDefault(kv =>
                    kv.Value.Name != null && (kv.Value.Name.ToUpper().Contains("LRG") ||
                                              kv.Value.Name.ToUpper().Contains("MAX") ||
                                              kv.Value.Name.ToUpper().Contains("MED") ||
                                              kv.Value.Name.ToUpper().Contains("LARGE")));

                if (small.Equals(default(KeyValuePair<uint, wComponentData>)))
                    small = scopes.First();

                if (large.Equals(default(KeyValuePair<uint, wComponentData>)))
                    large = scopes.First();

                if (!small.Equals(default(KeyValuePair<uint, wComponentData>)))
                    smallScope = small.Key;

                if (!large.Equals(default(KeyValuePair<uint, wComponentData>)))
                    largeScope = large.Key;
            }

            if (smallScope.HasValue)
                result.Add(smallScope.Value);

            if (largeScope.HasValue && (!smallScope.HasValue || largeScope.Value != smallScope.Value))
                result.Add(largeScope.Value);

            // ✅ Scope2 (holographic)
            var scope2 = components.FirstOrDefault(kv => kv.Value.Type == wComponentsType.Scope2);
            if (!scope2.Equals(default(KeyValuePair<uint, wComponentData>)))
                result.Add(scope2.Key);

            // ✅ Suppressor
            var supp = components.FirstOrDefault(kv => kv.Value.Type == wComponentsType.Suppressor);
            if (!supp.Equals(default(KeyValuePair<uint, wComponentData>)))
                result.Add(supp.Key);

            // ✅ Flashlight
            var flash = components.FirstOrDefault(kv => kv.Value.Type == wComponentsType.Flashlight);
            if (!flash.Equals(default(KeyValuePair<uint, wComponentData>)))
                result.Add(flash.Key);

            // ✅ Grip
            var grip = components.FirstOrDefault(kv => kv.Value.Type == wComponentsType.Grip);
            if (!grip.Equals(default(KeyValuePair<uint, wComponentData>)))
                result.Add(grip.Key);

            return result.Distinct().ToList();
        }

        public static void Give(ExtPlayer player, uint weaponHash, string locationName, string Location)
        {
            try
            {
                if (!player.IsCharacterData()) return;
                if (!WeaponsComponents.ContainsKey(weaponHash)) return;

                Log.Write($"[WEAPONCOMPONENTS] Loading weapon components for weaponHash={weaponHash}, location={locationName}");

                if (Repository.ItemsData.ContainsKey(locationName) &&
                    Repository.ItemsData[locationName].ContainsKey(Location) &&
                    Repository.ItemsData[locationName][Location].Count > 0)
                {
                    Dictionary<uint, wComponentData> Components = WeaponsComponents[weaponHash].Components;

                    var preferredList = SelectPreferredComponents(Components);
                    var preferredSet = new HashSet<uint>(preferredList);

                    var typeToPreferred = new Dictionary<wComponentsType, uint>();
                    foreach (var h in preferredList)
                    {
                        var t = Components[h].Type;
                        if (!typeToPreferred.ContainsKey(t))
                            typeToPreferred.Add(t, h);
                    }

                    List<uint> _JsonInventoryItemData = new List<uint>();

                    // ✅ СОБИРАЕМ ВСЕ УСТАНОВЛЕННЫЕ МОДИФИКАЦИИ
                    Dictionary<wComponentsType, uint> installedMods = new Dictionary<wComponentsType, uint>();

                    foreach (InventoryItemData item in Repository.ItemsData[locationName][Location].Values)
                    {
                        if (item.ItemId == ItemId.Debug) continue;

                        Log.Write($"[WEAPONCOMPONENTS] Processing mod: ItemId={item.ItemId}, Data={item.Data}");

                        uint componentHash = 0;

                        if (!string.IsNullOrEmpty(item.Data))
                        {
                            string[] parts = item.Data.Split('_');
                            string hashStr = parts.Length > 1 ? parts[1] : item.Data;

                            if (uint.TryParse(hashStr, out componentHash))
                            {
                                Log.Write($"[WEAPONCOMPONENTS] Extracted hash: {componentHash}");

                                if (Components.ContainsKey(componentHash))
                                {
                                    wComponentsType modType = Components[componentHash].Type;

                                    if (!installedMods.ContainsKey(modType))
                                    {
                                        installedMods[modType] = componentHash;
                                        Log.Write($"[WEAPONCOMPONENTS] Registered {modType}:  {componentHash}");
                                    }
                                }
                            }
                        }
                    }

                    // ✅ ШАГ 1: ВСЕГДА ДОБАВЛЯЕМ БАЗОВЫЙ МАГАЗИН ПЕРВЫМ (если есть)
                    uint? baseClipHash = null;

                    // Ищем CLIP_01 (базовый магазин)
                    foreach (var comp in Components)
                    {
                        if (comp.Value.Type == wComponentsType.Clip &&
                            comp.Value.Name != null &&
                            comp.Value.Name.ToUpper().Contains("CLIP1"))
                        {
                            baseClipHash = comp.Key;
                            break;
                        }
                    }

                    if (baseClipHash.HasValue)
                    {
                        _JsonInventoryItemData.Add(baseClipHash.Value);
                        Log.Write($"[WEAPONCOMPONENTS] Added BASE CLIP (CLIP_01): {baseClipHash.Value}");
                    }

                    // ✅ ШАГ 2: ДОБАВЛЯЕМ УСТАНОВЛЕННЫЕ МАГАЗИНЫ
                    if (installedMods.ContainsKey(wComponentsType.Clip2))
                    {
                        _JsonInventoryItemData.Add(installedMods[wComponentsType.Clip2]);
                        Log.Write($"[WEAPONCOMPONENTS] Added Clip2 (AP): {installedMods[wComponentsType.Clip2]}");
                    }
                    else if (installedMods.ContainsKey(wComponentsType.Clip))
                    {
                        // Проверяем, не базовый ли это магазин
                        if (!baseClipHash.HasValue || installedMods[wComponentsType.Clip] != baseClipHash.Value)
                        {
                            _JsonInventoryItemData.Add(installedMods[wComponentsType.Clip]);
                            Log.Write($"[WEAPONCOMPONENTS] Added extended Clip:  {installedMods[wComponentsType.Clip]}");
                        }
                    }

                    // ✅ ШАГ 3: ДОБАВЛЯЕМ ОСТАЛЬНЫЕ МОДИФИКАЦИИ
                    foreach (var kvp in installedMods)
                    {
                        if (kvp.Key != wComponentsType.Clip && kvp.Key != wComponentsType.Clip2)
                        {
                            if (!_JsonInventoryItemData.Contains(kvp.Value))
                            {
                                _JsonInventoryItemData.Add(kvp.Value);
                                Log.Write($"[WEAPONCOMPONENTS] Added {kvp.Key}:  {kvp.Value}");
                            }
                        }
                    }

                    Log.Write($"[WEAPONCOMPONENTS] Final components list: [{string.Join(", ", _JsonInventoryItemData)}]");

                    player.SetSharedData("weaponComponents", $"{weaponHash}|" + JsonConvert.SerializeObject(_JsonInventoryItemData));
                }
                else
                {
                    Log.Write($"[WEAPONCOMPONENTS] No components found for weapon {weaponHash}");
                    player.SetSharedData("weaponComponents", "null");
                }
            }
            catch (Exception e)
            {
                Log.Write($"Give Exception: {e.ToString()}");
            }
        }

        public static void Remove(ExtPlayer player)
        {
            try
            {
                if (!player.IsCharacterData()) return;
                player.SetSharedData("weaponComponents", "null");
            }
            catch (Exception e)
            {
                Log.Write($"Remove Exception: {e.ToString()}");
            }
        }
    }
}