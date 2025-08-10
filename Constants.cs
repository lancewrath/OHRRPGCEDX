using System;

namespace OHRRPGCEDX
{
    /// <summary>
    /// Core constants for the OHRRPGCE engine
    /// Based on the FreeBasic const.bi file
    /// </summary>
    public static class Constants
    {
        // Version constants
        public const int CURRENT_RPG_VERSION = 23;
        public const int CURRENT_RGFX_VERSION = 1;
        public const int CURRENT_RSAV_VERSION = 7;
        public const int CURRENT_TESTING_IPC_VERSION = 4;
        public const int CURRENT_HSZ_VERSION = 3;
        public const int CURRENT_HSP_VERSION = 1;
        public const string RECOMMENDED_HSPEAK_VERSION = "3S ";

        // General game data (.GEN) constants
        public const int genMaxMap = 0;
        public const int genTitle = 1;
        public const int genTitleMus = 2;
        public const int genVictMus = 3;
        public const int genBatMus = 4;
        public const int genPassVersion = 5;
        public const int genPW3Rot = 6;
        public const int genMaxHeroPic = 26;
        public const int genMaxEnemy1Pic = 27;
        public const int genMaxEnemy2Pic = 28;
        public const int genMaxEnemy3Pic = 29;
        public const int genMaxNPCPic = 30;
        public const int genMaxWeaponPic = 31;
        public const int genMaxAttackPic = 32;
        public const int genMaxTile = 33;
        public const int genMaxAttack = 34;
        public const int genMaxHero = 35;
        public const int genMaxEnemy = 36;
        public const int genMaxFormation = 37;
        public const int genMaxPal = 38;
        public const int genMaxTextbox = 39;
        public const int genNumPlotscripts = 40;
        public const int genNewGameScript = 41;

        // Additional gen constants from old engine
        public const int genGameoverScript = 42;
        public const int genMaxRegularScript = 43;
        public const int genSuspendBits = 44;
        public const int genCameraMode = 45;
        public const int genCameraArg1 = 46;
        public const int genCameraArg2 = 47;
        public const int genCameraArg3 = 48;
        public const int genCameraArg4 = 49;
        public const int genScrBackdrop = 50;
        public const int genDays = 51;
        public const int genHours = 52;
        public const int genMinutes = 53;
        public const int genSeconds = 54;
        public const int genMaxVehicle = 55;
        public const int genMaxTagname = 56;
        public const int genLoadGameScript = 57;
        public const int genTextboxBackdrop = 58;
        public const int genEnemyDissolve = 59;
        public const int genJoy = 60;
        public const int genPoisonChar = 61;
        public const int genStunChar = 62;
        public const int genDamageCap = 63;
        public const int genMuteChar = 64;
        public const int genStatCap = 65;
        public const int genMaxSFX = 77;
        public const int genMasterPal = 78;
        public const int genMaxMasterPal = 79;
        public const int genMaxMenu = 80;
        public const int genMaxMenuItem = 81;
        public const int genMaxItem = 82;
        public const int genMaxBoxBorder = 83;
        public const int genMaxPortrait = 84;
        public const int genMaxInventory = 85;
        public const int genErrorLevel = 86;
        public const int genLevelCap = 87;
        public const int genEquipMergeFormula = 88;
        public const int genNumElements = 89;
        public const int genUnlockedReserveXP = 90;
        public const int genLockedReserveXP = 91;
        public const int genPW4Hash = 92;
        public const int genPW2Offset = 93;
        public const int genPW2Length = 94;
        public const int genVersion = 95;
        public const int genStartMoney = 96;
        public const int genMaxShop = 97;
        public const int genPW1Offset = 98;
        public const int genPW1Length = 99;
        public const int genNumBackdrops = 100;
        public const int genBits = 101;
        public const int genStartX = 102;
        public const int genStartY = 103;
        public const int genStartMap = 104;
        public const int genOneTimeNPC = 105;
        public const int genOneTimeNPCBits = 106;
        public const int genDefaultDeathSFX = 171;
        public const int genMaxSong = 172;
        public const int genAcceptSFX = 173;
        public const int genCancelSFX = 174;
        public const int genCursorSFX = 175;
        public const int genTextboxLine = 176;
        public const int genBits2 = 177;
        public const int genItemLearnSFX = 179;
        public const int genCantLearnSFX = 180;
        public const int genBuySFX = 181;
        public const int genHireSFX = 182;
        public const int genSellSFX = 183;
        public const int genCantBuySFX = 184;
        public const int genCantSellSFX = 185;
        public const int genDamageDisplayTicks = 186;
        public const int genDamageDisplayRise = 187;
        public const int genHeroWeakHP = 188;
        public const int genEnemyWeakHP = 189;
        public const int genAutosortScheme = 190;
        public const int genMaxLevel = 191;
        public const int genBattleMode = 192;
        public const int genItemStackSize = 193;
        public const int genResolutionX = 194;
        public const int genResolutionY = 195;
        public const int genEscMenuScript = 196;
        public const int genSaveSlotCount = 197;
        public const int genMillisecPerFrame = 198;
        public const int genStealSuccessSFX = 199;
        public const int genStealFailSFX = 200;
        public const int genStealNoItemSFX = 201;
        public const int genRegenChar = 202;
        public const int genDefaultScale = 203;
        public const int genDebugMode = 204;
        public const int genCurrentDebugMode = 205;
        public const int genStartHero = 206;
        public const int genStartTextbox = 207;
        public const int genWindowSize = 208;
        public const int genLivePreviewWindowSize = 209;
        public const int genFullscreen = 210;
        public const int genMusicVolume = 211;
        public const int genSFXVolume = 212;
        public const int genRungameFullscreenIndependent = 213;
        public const int genSkipBattleRewardsTicks = 214;
        public const int genDefOnkeypressScript = 215;
        public const int genDefEachStepScript = 216;
        public const int genDefAfterBattleScript = 217;
        public const int genDefInsteadOfBattleScript = 218;
        public const int genDefMapAutorunScript = 219;
        public const int genMaxEnemyPic = 220;
        public const int genMinimapAlgorithm = 221;
        public const int genBits3 = 222;
        public const int genDefCounterProvoke = 226;
        public const int genInventSlotx1Display = 227;
        public const int genCameraOnWalkaboutFocus = 228;
        public const int genTicksPerWalkFrame = 229;
        public const int genAddHeroScript = 230;
        public const int genRemoveHeroScript = 231;
        public const int genMoveHeroScript = 232;
        public const int gen8bitBlendAlgo = 233;
        public const int genPreviewBackdrop = 234;
        public const int gen32bitMode = 235;
        public const int genDefaultBattleMenu = 236;

        // Direction constants
        public const int dirUp = 0;
        public const int dirRight = 1;
        public const int dirDown = 2;
        public const int dirLeft = 3;

        // Boolean constants
        public const bool YES = true;
        public const bool NO = false;

        // Maximum values
        public const int maxElements = 8;
        public const int maxMPLevel = 7;  // Max level of FF1-style level-MP, 0-based
        public const int maxDoorsPerMap = 100;
        public const int inventoryMax = 1000;
        public const int maplayerMax = 10;

        // Graphics constants
        public const int CURRENT_GFX_API_VERSION = 2;

        // File extension constants
        public const string RPG_EXTENSION = ".rpg";
        public const string GEN_EXTENSION = ".gen";
        public const string RGFX_EXTENSION = ".rgfx";
        public const string RSAV_EXTENSION = ".rsav";
        public const string HSZ_EXTENSION = ".hsz";
        public const string HSP_EXTENSION = ".hsp";

        // Working directory constants
        public const string DANGER_TMP_FILE = "__danger.tmp";
        public const string SESSION_INFO_FILE = "session_info.txt.tmp";
        public const string WORKING_DIR_PREFIX = "ohrrpgce_working_";

        // Process constants
        public const int INVALID_PID = 0;
        public const int MAX_PROCESS_WAIT_TIME = 5000; // 5 seconds

        // Menu constants
        public const int MENU_QUIT = 0;
        public const int MENU_MAIN_EDITOR = 1;
        public const int MENU_GRAPHICS_EDITOR = 2;
        public const int MENU_SCRIPT_EDITOR = 3;
        public const int MENU_MAP_EDITOR = 4;
        public const int MENU_BATTLE_EDITOR = 5;
        public const int MENU_ITEM_EDITOR = 6;
        public const int MENU_ENEMY_EDITOR = 7;
        public const int MENU_HERO_EDITOR = 8;
        public const int MENU_PLOTSCRIPT_EDITOR = 9;
        public const int MENU_IMPORT_EXPORT = 10;
        public const int MENU_DISTRIBUTION = 11;
        public const int MENU_OPTIONS = 12;
        public const int MENU_HELP = 13;

        // Graphics editor constants
        public const int GFX_EDIT_SPRITES = 1;
        public const int GFX_EDIT_BACKGROUNDS = 2;
        public const int GFX_EDIT_PALETTES = 3;
        public const int GFX_EDIT_FONTS = 4;
        public const int GFX_EDIT_TILESETS = 5;
        public const int GFX_EDIT_ANIMATIONS = 6;
        public const int GFX_EDIT_BACK_TO_MAIN = 0;

        // File operation constants
        public const int FILE_OPERATION_SUCCESS = 0;
        public const int FILE_OPERATION_FAILED = -1;
        public const int FILE_OPERATION_CANCELLED = -2;

        // Error constants
        public const int ERROR_NONE = 0;
        public const int ERROR_FILE_NOT_FOUND = 1;
        public const int ERROR_ACCESS_DENIED = 2;
        public const int ERROR_DISK_FULL = 3;
        public const int ERROR_INVALID_FORMAT = 4;
        public const int ERROR_CORRUPTED_DATA = 5;

        // Suspend bits (gen(genSuspendBits))
        public const int suspendnpcs = 0;
        public const int suspendplayer = 1;
        public const int suspendobstruction = 2;
        public const int suspendherowalls = 3;
        public const int suspendnpcwalls = 4;
        public const int suspendcaterpillar = 5;
        public const int suspendrandomenemies = 6;
        public const int suspendboxadvance = 7;
        public const int suspendoverlay = 8;
        public const int suspendambientmusic = 9;
        public const int suspenddoors = 10;
        public const int suspendtimers = 11;
        public const int suspendtextboxcontrols = 12;
        public const int suspendwalkabouts = 13;

        // Camera mode constants (gen(genCameraMode))
        public const int herocam = 0;
        public const int npccam = 1;
        public const int pancam = 2;
        public const int focuscam = 3;
        public const int slicecam = 4;
        public const int stopcam = -1;

        // Built in stats
        public const int statHP = 0;
        public const int statMP = 1;
        public const int statAtk = 2;
        public const int statAim = 3;
        public const int statDef = 4;
        public const int statDodge = 5;
        public const int statMagic = 6;
        public const int statWill = 7;
        public const int statSpeed = 8;
        public const int statCtr = 9;
        public const int statFocus = 10;
        public const int statHitX = 11;
        public const int statUser = 12;
        public const int statLast = 11;
        public const int statPoison = 12;
        public const int statRegen = 13;
        public const int statStun = 14;
        public const int statMute = 15;
        public const int statLastRegister = 15;

        // Format fix bits
        public const int fixAttackitems = 0;
        public const int fixWeapPoints = 1;
        public const int fixStunCancelTarg = 2;
        public const int fixDefaultDissolve = 3;
        public const int fixDefaultDissolveEnemy = 4;
        public const int fixPushNPCBugCompat = 5;
        public const int fixDefaultMaxItem = 6;
        public const int fixBlankDoorLinks = 7;
        public const int fixShopSounds = 8;
        public const int fixExtendedNPCs = 9;
        public const int fixHeroPortrait = 10;
        public const int fixTextBoxPortrait = 11;
        public const int fixNPCLocationFormat = 12;
        public const int fixInitDamageDisplay = 13;
        public const int fixDefaultLevelCap = 14;
        public const int fixHeroElementals = 15;
        public const int fixOldElementalFailBit = 16;
        public const int fixAttackElementFails = 17;
        public const int fixEnemyElementals = 18;
        public const int fixItemElementals = 19;
        public const int fixNumElements = 20;
        public const int fixRemoveDamageMP = 21;
        public const int fixDefaultMaxLevel = 22;
        public const int fixUNUSED23 = 23;
        public const int fixWipeGEN = 24;
        public const int fixSetOldAttackPosBit = 25;
        public const int fixWrapCroppedMapsBit = 26;
        public const int fixInitNonElementalSpawning = 27;

        // Additional constants from old engine
        public const int sizeParty = 4;  // Maximum party size
        public const int maxSpellLists = 4;  // Maximum spell lists per hero
        public const int maxSpellsPerList = 24;  // Maximum spells per spell list
        public const int maxEquipSlots = 4;  // Maximum equipment slots
        public const int maxLearnMaskBits = 96;  // (4 * 24 - 1) / 16 = 95, rounded up
    }
}
