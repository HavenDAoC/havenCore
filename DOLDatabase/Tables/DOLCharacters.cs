/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */

using System;
using DOL.Database;
using DOL.Database.Attributes;

namespace DOL.Database;

/// <summary>
/// The player character. Stores GamePlayer in DB
/// </summary>
[DataTable(TableName = "DOLCharacters")]
public class DOLCharacters : DataObject
{
    private string m_accountName;
    private int m_accountSlot;

    private DateTime m_creationDate;
    private DateTime m_lastPlayed;

    //The following structure was directly taken
    //from the 0x57/0x55 Packets
    //
    private string m_name; //24 Bytes

    //0x00					//24 bytes empty
    //Locationstring		//24 bytes empty when sent
    private string m_guildid; //not sent in 0x55/0x57
    private string m_lastName; //not sent in 0x55/0x57
    private int m_race;
    private int m_gender;
    private int m_level; //01 byte
    private int m_class; //01 byte
    private int m_realm; //01 byte
    private int m_creationModel; //02 byte
    private int m_region; //01 byte
    private int m_maxEndurance;
    private int m_health;
    private int m_mana;
    private int m_concentration;
    private int m_endurance;
    private long m_exp;
    private long m_bntyPts;
    private long m_realmPts;

    private int m_realmLevel;
    //0x00					//01 byte
    //int mUnk2;			//04 byte
    //int mStr;				//01 byte
    //int mDex;				//01 byte
    //int mCon;				//01 byte
    //int mQui;				//01 byte
    //int mInt;				//01 byte
    //int mPie;				//01 byte
    //int mEmp;				//01 byte
    //int mChr;				//01 byte
    //0x00					//44 bytes for inventory and stuff

    private byte m_activeWeaponSlot;
    private bool m_isCloakHoodUp;
    private bool m_isCloakInvisible;
    private bool m_isHelmInvisible;
    private bool m_spellQueue;
    private int m_copper;
    private int m_silver;
    private int m_gold;
    private int m_platinum;
    private int m_mithril;
    private int m_currentModel;

    private int m_constitution = 0;
    private int m_dexterity = 0;
    private int m_strength = 0;
    private int m_quickness = 0;
    private int m_intelligence = 0;
    private int m_piety = 0;
    private int m_empathy = 0;
    private int m_charisma = 0;

    //This needs to be uint and ushort!
    private int m_xpos;
    private int m_ypos;
    private int m_zpos;

    //bind position
    private int m_bindxpos;
    private int m_bindypos;
    private int m_bindzpos;
    private int m_bindregion;

    private int m_bindheading;

    //bind house
    private int m_bindhousexpos;
    private int m_bindhouseypos;
    private int m_bindhousezpos;
    private int m_bindhouseregion;
    private int m_bindhouseheading;

    private byte m_deathCount;
    private int m_conLostAtDeath;

    private bool m_hasGravestone;
    private int m_gravestoneRegion;

    private int m_direction;
    private int m_maxSpeed;

    private bool m_isLevelSecondStage;
    private bool m_usedLevelCommand;

    // here are skills stored. loading and saving skills of player is done automatically and
    // these fields should NOT manipulated or used otherwise
    // instead use the skill access methods on GamePlayer
    private string
        m_abilities =
            string.Empty; // comma separated string of ability keynames and pipe'd levels eg "sprint|0,evade|1"

    private string
        m_specs = string
            .Empty; // comma separated string of spec keynames and pipe'd levels like "earth_magic|5,slash|10"

    private string
        m_realmAbilities =
            string.Empty; // comma separated string of realm ability keynames and pipe'd levels eg "purge|1,ignore pain|2"

    private string m_craftingSkills = string.Empty; // crafting skills
    private string m_disabledSpells = string.Empty;
    private string m_disabledAbilities = string.Empty;

    private string m_friendList = string.Empty; //comma seperated string of friends
    private string m_ignoreList = string.Empty; //comma seperated string of ignored Players
    private string m_playerTitleType = string.Empty;

    private bool m_flagClassName = true;
    private ushort m_guildRank;

    private long m_playedTime; // character /played in seconds.
    private long m_deathTime; // character /played death time

    private int m_respecAmountAllSkill; // full Respecs.
    private int m_respecAmountSingleSkill; // Single-Line Respecs
    private int m_respecAmountRealmSkill; //realm respecs
    private int m_respecAmountDOL; // Patch 1.84 /respec Mythic
    private int m_respecAmountChampionSkill; // CL Respecs
    private bool m_isLevelRespecUsed;
    private int m_respecBought; // /respec buy
    private bool m_safetyFlag;
    private int m_craftingPrimarySkill = 0;
    private bool m_cancelStyle;
    private bool m_isAnonymous;

    private byte m_customisationStep = 1;

    private byte m_eyesize = 0;
    private byte m_lipsize = 0;
    private byte m_eyecolor = 0;
    private byte m_hairColor = 0;
    private byte m_facetype = 0;
    private byte m_hairstyle = 0;
    private byte m_moodtype = 0;

    private bool m_gainXP;
    private bool m_gainRP;
    private bool m_roleplay;
    private bool m_autoloot;
    private int m_lastfreeLevel;
    private DateTime m_lastfreeleveled;
    private bool m_showXFireInfo;
    private bool m_showGuildLogins;

    private string m_guildNote = string.Empty;

    //CLs
    private bool m_cl;
    private long m_clExperience;
    private int m_clLevel;

    // MLs
    private byte m_ml;
    private long m_mlExperience;
    private int m_mlLevel;
    private bool m_mlGranted;

    // Should this player stats be ignored when tabulating statistics?
    private bool m_ignoreStatistics = false;

    // What should the Herald display of this character?
    private byte m_notDisplayedInHerald = 0;

    // Should we hide the detailed specialization of this player in the APIs?
    private bool m_hideSpecializationAPI;

    private byte m_activeSaddleBags = 0;

    private DateTime m_lastLevelUp;

    private long m_playedTimeSinceLevel;

    // Atlas
    private bool m_noHelp; // set to true if player is doing the solo challenge
    private bool m_hardcore; // set to true if player is doing the hardcore challenge
    private bool m_hardcoreCompleted; // set to true if player has reached level 50 as hardcore
    private bool m_receiveROG; // toggle receiving ROGs for the player
    private bool m_boosted; // set to true if player has used a free level/rr NPC

    /// <summary>
    /// Create the character row in table
    /// </summary>
    public DOLCharacters()
    {
        m_creationDate = DateTime.Now;
        m_concentration = 100;
        m_exp = 0;
        m_bntyPts = 0;
        m_realmPts = 0;

        m_lastPlayed = DateTime.Now; // Prevent /played crash.
        m_playedTime = 0; // /played startup
        m_deathTime = long.MinValue;
        m_respecAmountAllSkill = 0;
        m_respecAmountSingleSkill = 0;
        m_respecAmountRealmSkill = 0;
        m_respecAmountDOL = 0;
        m_respecBought = 0;

        m_isLevelRespecUsed = true;
        m_safetyFlag = true;
        m_craftingPrimarySkill = 0;
        m_usedLevelCommand = false;
        m_spellQueue = true;
        m_gainXP = true;
        m_gainRP = true;
        m_autoloot = true;
        m_showXFireInfo = false;
        m_noHelp = false;
        m_showGuildLogins = false;
        m_roleplay = false;
        m_ignoreStatistics = false;
        m_lastLevelUp = DateTime.Now;
        m_playedTimeSinceLevel = 0;
        m_receiveROG = true;
        m_hardcore = false;
        m_hardcoreCompleted = false;
        m_boosted = false;
    }

    /// <summary>
    /// Gets/sets if this character has xp in a gravestone
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public bool HasGravestone
    {
        get => m_hasGravestone;
        set
        {
            m_hasGravestone = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Gets/sets the region id where the gravestone of the player is located
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int GravestoneRegion
    {
        get => m_gravestoneRegion;
        set
        {
            m_gravestoneRegion = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Gets/sets character constitution
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int Constitution
    {
        get => m_constitution;
        set
        {
            m_constitution = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Gets/sets character dexterity
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int Dexterity
    {
        get => m_dexterity;
        set
        {
            m_dexterity = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Gets/sets character strength
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int Strength
    {
        get => m_strength;
        set
        {
            m_strength = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Gets/sets character quickness
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int Quickness
    {
        get => m_quickness;
        set
        {
            m_quickness = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Gets/sets character intelligence
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int Intelligence
    {
        get => m_intelligence;
        set
        {
            m_intelligence = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Gets/sets character piety
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int Piety
    {
        get => m_piety;
        set
        {
            m_piety = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Gets/sets character empathy
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int Empathy
    {
        get => m_empathy;
        set
        {
            m_empathy = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Gets/sets character charisma
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int Charisma
    {
        get => m_charisma;
        set
        {
            m_charisma = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Gets/sets chracter bounty points
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public long BountyPoints
    {
        get => m_bntyPts;
        set
        {
            m_bntyPts = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Gets/sets character realm points
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public long RealmPoints
    {
        get => m_realmPts;
        set
        {
            m_realmPts = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Gets/sets realm rank
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int RealmLevel
    {
        get => m_realmLevel;
        set
        {
            m_realmLevel = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Gets/sets experience
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public long Experience
    {
        get => m_exp;
        set
        {
            m_exp = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Gets/sets max endurance
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int MaxEndurance
    {
        get => m_maxEndurance;
        set
        {
            m_maxEndurance = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Gets/sets maximum health
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int Health
    {
        get => m_health;
        set
        {
            m_health = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Gets/sets max mana
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int Mana
    {
        get => m_mana;
        set
        {
            m_mana = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Gets/sets max endurance
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int Endurance
    {
        get => m_endurance;
        set
        {
            m_endurance = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Gets/sets the object concentration
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int Concentration
    {
        get => m_concentration;
        set
        {
            m_concentration = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Account name of account which own this character
    /// </summary>
    [DataElement(AllowDbNull = false, Index = true)]
    public string AccountName
    {
        get => m_accountName;
        set
        {
            Dirty = true;
            m_accountName = value;
        }
    }

    /// <summary>
    /// The slot of character in account
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int AccountSlot
    {
        get => m_accountSlot;
        set
        {
            Dirty = true;
            m_accountSlot = value;
        }
    }

    /// <summary>
    /// The creation date of this character
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public DateTime CreationDate
    {
        get => m_creationDate;
        set
        {
            Dirty = true;
            m_creationDate = value;
        }
    }

    /// <summary>
    /// The last time this character have been played
    /// </summary>
    [DataElement(AllowDbNull = true)]
    public DateTime LastPlayed
    {
        get => m_lastPlayed;
        set
        {
            Dirty = true;
            m_lastPlayed = value;
        }
    }

    /// <summary>
    /// Name of this character. all name of character is unique
    /// </summary>
    [DataElement(AllowDbNull = false, Unique = true)]
    public virtual string Name
    {
        get => m_name;
        set
        {
            Dirty = true;
            m_name = value;
        }
    }

    /// <summary>
    /// Lastname of this character. You can have family ;)
    /// </summary>
    [DataElement(AllowDbNull = true)]
    public string LastName
    {
        get => m_lastName;
        set
        {
            Dirty = true;
            m_lastName = value;
        }
    }

    /// <summary>
    /// ID of the guild this character is in
    /// </summary>
    [DataElement(AllowDbNull = true, Index = true)]
    public string GuildID
    {
        get => m_guildid;
        set
        {
            Dirty = true;
            m_guildid = value;
        }
    }

    /// <summary>
    /// Male or female character
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int Gender
    {
        get => m_gender;
        set
        {
            Dirty = true;
            m_gender = value;
        }
    }

    /// <summary>
    /// Race of character (viking,troll,...)
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int Race
    {
        get => m_race;
        set
        {
            Dirty = true;
            m_race = value;
        }
    }

    /// <summary>
    /// Level of this character
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int Level
    {
        get => m_level;
        set
        {
            Dirty = true;
            m_level = value;
        }
    }

    /// <summary>
    /// class of this character
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int Class
    {
        get => m_class;
        set
        {
            Dirty = true;
            m_class = value;
        }
    }

    /// <summary>
    /// Realm of character
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int Realm
    {
        get => m_realm;
        set
        {
            Dirty = true;
            m_realm = value;
        }
    }

    /// <summary>
    /// The model of character when created
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int CreationModel
    {
        get => m_creationModel;
        set
        {
            Dirty = true;
            m_creationModel = value;
        }
    }

    /// <summary>
    /// The model used for the character's display (usually the same as CreationModel)
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int CurrentModel
    {
        get => m_currentModel;
        set
        {
            Dirty = true;
            m_currentModel = value;
        }
    }

    /// <summary>
    /// The region of character
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int Region
    {
        get => m_region;
        set
        {
            Dirty = true;
            m_region = value;
        }
    }

    /// <summary>
    /// The character's active weapon slot and quiver slot - <see cref="T:DOL.GS.eActiveWeaponSlot" /> ORed with <see cref="T:DOL.GS.GameLiving.eActiveQuiverSlot" />
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public byte ActiveWeaponSlot
    {
        get => m_activeWeaponSlot;
        set
        {
            m_activeWeaponSlot = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// The X position of character
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int Xpos
    {
        get => m_xpos;
        set
        {
            Dirty = true;
            m_xpos = value;
        }
    }

    /// <summary>
    /// The Y position of character
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int Ypos
    {
        get => m_ypos;
        set
        {
            Dirty = true;
            m_ypos = value;
        }
    }

    /// <summary>
    /// The Z position of character
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int Zpos
    {
        get => m_zpos;
        set
        {
            Dirty = true;
            m_zpos = value;
        }
    }

    /// <summary>
    /// The bind X position of character
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int BindXpos
    {
        get => m_bindxpos;
        set
        {
            Dirty = true;
            m_bindxpos = value;
        }
    }

    /// <summary>
    /// The bind Y position of character
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int BindYpos
    {
        get => m_bindypos;
        set
        {
            Dirty = true;
            m_bindypos = value;
        }
    }

    /// <summary>
    /// The bind Z position of character
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int BindZpos
    {
        get => m_bindzpos;
        set
        {
            Dirty = true;
            m_bindzpos = value;
        }
    }

    /// <summary>
    /// The bind region position of character
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int BindRegion
    {
        get => m_bindregion;
        set
        {
            Dirty = true;
            m_bindregion = value;
        }
    }

    /// <summary>
    /// The bind heading position of character
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int BindHeading
    {
        get => m_bindheading;
        set
        {
            Dirty = true;
            m_bindheading = value;
        }
    }

    /// <summary>
    /// The house bind X position of character
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int BindHouseXpos
    {
        get => m_bindhousexpos;
        set
        {
            Dirty = true;
            m_bindhousexpos = value;
        }
    }

    /// <summary>
    /// The bind house Y position of character
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int BindHouseYpos
    {
        get => m_bindhouseypos;
        set
        {
            Dirty = true;
            m_bindhouseypos = value;
        }
    }

    /// <summary>
    /// The bind house Z position of character
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int BindHouseZpos
    {
        get => m_bindhousezpos;
        set
        {
            Dirty = true;
            m_bindhousezpos = value;
        }
    }

    /// <summary>
    /// The bind house region position of character
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int BindHouseRegion
    {
        get => m_bindhouseregion;
        set
        {
            Dirty = true;
            m_bindhouseregion = value;
        }
    }

    /// <summary>
    /// The bind house heading position of character
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int BindHouseHeading
    {
        get => m_bindhouseheading;
        set
        {
            Dirty = true;
            m_bindhouseheading = value;
        }
    }

    /// <summary>
    /// The number of chacter is dead at this level
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public byte DeathCount
    {
        get => m_deathCount;
        set
        {
            Dirty = true;
            m_deathCount = value;
        }
    }

    /// <summary>
    /// Constitution lost at death
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int ConLostAtDeath
    {
        get => m_conLostAtDeath;
        set
        {
            Dirty = true;
            m_conLostAtDeath = value;
        }
    }

    /// <summary>
    /// Heading of character
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int Direction
    {
        get => m_direction;
        set
        {
            Dirty = true;
            m_direction = value;
        }
    }

    /// <summary>
    /// The max speed of character
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int MaxSpeed
    {
        get => m_maxSpeed;
        set
        {
            Dirty = true;
            m_maxSpeed = value;
        }
    }

    /// <summary>
    /// Money copper part player own
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int Copper
    {
        get => m_copper;
        set
        {
            Dirty = true;
            m_copper = value;
        }
    }

    /// <summary>
    /// Money silver part player own
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int Silver
    {
        get => m_silver;
        set
        {
            Dirty = true;
            m_silver = value;
        }
    }

    /// <summary>
    /// Money gold part player own
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int Gold
    {
        get => m_gold;
        set
        {
            Dirty = true;
            m_gold = value;
        }
    }

    /// <summary>
    /// Money platinum part player own
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int Platinum
    {
        get => m_platinum;
        set
        {
            Dirty = true;
            m_platinum = value;
        }
    }

    /// <summary>
    /// Money mithril part player own
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int Mithril
    {
        get => m_mithril;
        set
        {
            Dirty = true;
            m_mithril = value;
        }
    }

    /// <summary>
    /// The crafting skills of character
    /// </summary>
    [DataElement(AllowDbNull = true)]
    public string SerializedCraftingSkills
    {
        get => m_craftingSkills;
        set
        {
            Dirty = true;
            m_craftingSkills = value;
        }
    }

    /// <summary>
    /// The abilities of character
    /// </summary>
    [DataElement(AllowDbNull = true)]
    public string SerializedAbilities
    {
        get => m_abilities;
        set
        {
            Dirty = true;
            m_abilities = value;
        }
    }

    /// <summary>
    /// The specs of character
    /// </summary>
    [DataElement(AllowDbNull = true)]
    public string SerializedSpecs
    {
        get => m_specs;
        set
        {
            Dirty = true;
            m_specs = value;
        }
    }

    /// <summary>
    /// the realm abilities of character
    /// </summary>
    [DataElement(AllowDbNull = true)]
    public string SerializedRealmAbilities
    {
        get => m_realmAbilities;
        set
        {
            Dirty = true;
            m_realmAbilities = value;
        }
    }

    /// <summary>
    /// The spells unallowed to character
    /// </summary>
    [DataElement(AllowDbNull = true)]
    public string DisabledSpells
    {
        get => m_disabledSpells;
        set => m_disabledSpells = value;
    }

    /// <summary>
    /// The abilities unallowed to character
    /// </summary>
    [DataElement(AllowDbNull = true)]
    public string DisabledAbilities
    {
        get => m_disabledAbilities;
        set => m_disabledAbilities = value;
    }

    /// <summary>
    /// The Friend list
    /// </summary>
    [DataElement(AllowDbNull = true)]
    public string SerializedFriendsList
    {
        get => m_friendList;
        set
        {
            Dirty = true;
            m_friendList = value;
        }
    }

    /// <summary>
    /// The Ignore list
    /// </summary>
    [DataElement(AllowDbNull = true)]
    public string SerializedIgnoreList
    {
        get => m_ignoreList;
        set
        {
            Dirty = true;
            m_ignoreList = value;
        }
    }

    /// <summary>
    /// Is cloak hood up
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public bool IsCloakHoodUp
    {
        get => m_isCloakHoodUp;
        set
        {
            m_isCloakHoodUp = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Is cloak hood up
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public bool IsCloakInvisible
    {
        get => m_isCloakInvisible;
        set
        {
            m_isCloakInvisible = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Is cloak hood up
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public bool IsHelmInvisible
    {
        get => m_isHelmInvisible;
        set
        {
            m_isHelmInvisible = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Spell queue flag
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public bool SpellQueue
    {
        get => m_spellQueue;
        set
        {
            m_spellQueue = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Gets/sets half-level flag
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public bool IsLevelSecondStage
    {
        get => m_isLevelSecondStage;
        set
        {
            m_isLevelSecondStage = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Gets/sets guildname flag to print guildname or crafting title
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public bool FlagClassName
    {
        get => m_flagClassName;
        set
        {
            m_flagClassName = value;
            Dirty = true;
        }
    }

    private bool m_advisor = false;

    /// <summary>
    /// Is the character an advisor
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public bool Advisor
    {
        get => m_advisor;
        set
        {
            Dirty = true;
            m_advisor = value;
        }
    }

    /// <summary>
    /// Gets/sets guild rank in the guild
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public ushort GuildRank
    {
        get => m_guildRank;
        set
        {
            m_guildRank = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Gets/sets the characters /played time
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public long PlayedTime
    {
        get => m_playedTime;
        set
        {
            Dirty = true;
            m_playedTime = value;
        }
    }

    /// <summary>
    /// Gets/sets the characters death /played time
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public long DeathTime
    {
        get => m_deathTime;
        set
        {
            Dirty = true;
            m_deathTime = value;
        }
    }

    /// <summary>
    /// Gets/sets the characters full skill respecs available
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int RespecAmountAllSkill
    {
        get => m_respecAmountAllSkill;
        set
        {
            Dirty = true;
            m_respecAmountAllSkill = value;
        }
    }

    /// <summary>
    /// Gets/sets the characters single-line respecs available
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int RespecAmountSingleSkill
    {
        get => m_respecAmountSingleSkill;
        set
        {
            Dirty = true;
            m_respecAmountSingleSkill = value;
        }
    }

    /// <summary>
    /// Gets/Sets the characters realm respecs available
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int RespecAmountRealmSkill
    {
        get => m_respecAmountRealmSkill;
        set
        {
            Dirty = true;
            m_respecAmountRealmSkill = value;
        }
    }

    /// <summary>
    /// Gets/Sets the characters DOL respecs available
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int RespecAmountDOL
    {
        get => m_respecAmountDOL;
        set
        {
            Dirty = true;
            m_respecAmountDOL = value;
        }
    }

    /// <summary>
    /// Gets/sets the characters single-line respecs available
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int RespecAmountChampionSkill
    {
        get => m_respecAmountChampionSkill;
        set
        {
            Dirty = true;
            m_respecAmountChampionSkill = value;
        }
    }

    /// <summary>
    /// Gets/Sets level respec flag
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public bool IsLevelRespecUsed
    {
        get => m_isLevelRespecUsed;
        set
        {
            Dirty = true;
            m_isLevelRespecUsed = value;
        }
    }

    /// <summary>
    /// Gets/Sets level respec bought
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int RespecBought
    {
        get => m_respecBought;
        set
        {
            Dirty = true;
            m_respecBought = value;
        }
    }

    /// <summary>
    /// Gets/sets the characters safety flag
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public bool SafetyFlag
    {
        get => m_safetyFlag;
        set
        {
            Dirty = true;
            m_safetyFlag = value;
        }
    }

    /// <summary>
    /// Gets/sets the characters safety flag
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int CraftingPrimarySkill
    {
        get => m_craftingPrimarySkill;
        set
        {
            Dirty = true;
            m_craftingPrimarySkill = value;
        }
    }

    /// <summary>
    /// the cancel style
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public bool CancelStyle
    {
        get => m_cancelStyle;
        set
        {
            Dirty = true;
            m_cancelStyle = value;
        }
    }

    /// <summary>
    /// is anonymous( can not seen him in /who and some other things
    /// /anon to toggle
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public bool IsAnonymous
    {
        get => m_isAnonymous;
        set
        {
            Dirty = true;
            m_isAnonymous = value;
        }
    }

    /// <summary>
    /// the face customisation step
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public byte CustomisationStep
    {
        get => m_customisationStep;
        set
        {
            Dirty = true;
            m_customisationStep = value;
        }
    }

    /// <summary>
    /// Gets/sets character EyeSize
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public byte EyeSize
    {
        get => m_eyesize;
        set
        {
            m_eyesize = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Gets/sets character LipSize
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public byte LipSize
    {
        get => m_lipsize;
        set
        {
            m_lipsize = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Gets/sets character EyeColor
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public byte EyeColor
    {
        get => m_eyecolor;
        set
        {
            m_eyecolor = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Gets/sets character HairColor
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public byte HairColor
    {
        get => m_hairColor;
        set
        {
            m_hairColor = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Gets/sets character FaceType
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public byte FaceType
    {
        get => m_facetype;
        set
        {
            m_facetype = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Gets/sets character HairStyle
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public byte HairStyle
    {
        get => m_hairstyle;
        set
        {
            m_hairstyle = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Gets/sets character MoodType
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public byte MoodType
    {
        get => m_moodtype;
        set
        {
            m_moodtype = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Gets/sets weather a character has used /level
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public bool UsedLevelCommand
    {
        get => m_usedLevelCommand;
        set
        {
            m_usedLevelCommand = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Gets/sets selected player title type
    /// </summary>
    [DataElement(AllowDbNull = true)]
    public string CurrentTitleType
    {
        get => m_playerTitleType;
        set
        {
            m_playerTitleType = value;
            Dirty = true;
        }
    }

    #region Statistics

    private int m_killsAlbionPlayers;
    private int m_killsMidgardPlayers;
    private int m_killsHiberniaPlayers;
    private int m_killsAlbionDeathBlows;
    private int m_killsMidgardDeathBlows;
    private int m_killsHiberniaDeathBlows;
    private int m_killsAlbionSolo;
    private int m_killsMidgardSolo;
    private int m_killsHiberniaSolo;
    private int m_capturedKeeps;
    private int m_capturedTowers;
    private int m_capturedRelics;
    private int m_killsDragon;
    private int m_deathsPvP;

    /// <summary>
    /// Amount of Albion Players Killed
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int KillsAlbionPlayers
    {
        get => m_killsAlbionPlayers;
        set
        {
            m_killsAlbionPlayers = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Amount of Midgard Players Killed
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int KillsMidgardPlayers
    {
        get => m_killsMidgardPlayers;
        set
        {
            m_killsMidgardPlayers = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Amount of Hibernia Players Killed
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int KillsHiberniaPlayers
    {
        get => m_killsHiberniaPlayers;
        set
        {
            m_killsHiberniaPlayers = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Amount of Death Blows on Albion Players
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int KillsAlbionDeathBlows
    {
        get => m_killsAlbionDeathBlows;
        set
        {
            m_killsAlbionDeathBlows = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Amount of Death Blows on Midgard Players
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int KillsMidgardDeathBlows
    {
        get => m_killsMidgardDeathBlows;
        set
        {
            m_killsMidgardDeathBlows = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Amount of Death Blows on Hibernia Players
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int KillsHiberniaDeathBlows
    {
        get => m_killsHiberniaDeathBlows;
        set
        {
            m_killsHiberniaDeathBlows = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Amount of Solo Albion Kills
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int KillsAlbionSolo
    {
        get => m_killsAlbionSolo;
        set
        {
            m_killsAlbionSolo = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Amount of Solo Midgard Kills
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int KillsMidgardSolo
    {
        get => m_killsMidgardSolo;
        set
        {
            m_killsMidgardSolo = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Amount of Solo Hibernia Kills
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int KillsHiberniaSolo
    {
        get => m_killsHiberniaSolo;
        set
        {
            m_killsHiberniaSolo = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Amount of Keeps Captured
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int CapturedKeeps
    {
        get => m_capturedKeeps;
        set
        {
            m_capturedKeeps = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Amount of Towers Captured
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int CapturedTowers
    {
        get => m_capturedTowers;
        set
        {
            m_capturedTowers = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Amount of Relics Captured
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int CapturedRelics
    {
        get => m_capturedRelics;
        set
        {
            m_capturedRelics = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Amount of Dragons Killed
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int KillsDragon
    {
        get => m_killsDragon;
        set
        {
            m_killsDragon = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Amount of PvP deaths
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int DeathsPvP
    {
        get => m_deathsPvP;
        set
        {
            m_deathsPvP = value;
            Dirty = true;
        }
    }

    private int m_killsLegion;
    private int m_killsEpicBoss;


    /// <summary>
    /// Amount of killed Legions
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int KillsLegion
    {
        get => m_killsLegion;
        set
        {
            m_killsLegion = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Amount of killed EpicDungeon Boss
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int KillsEpicBoss
    {
        get => m_killsEpicBoss;
        set
        {
            m_killsEpicBoss = value;
            Dirty = true;
        }
    }

    #endregion

    /// <summary>
    /// can gain experience points
    /// /xp to toggle
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public bool GainXP
    {
        get => m_gainXP;
        set
        {
            Dirty = true;
            m_gainXP = value;
        }
    }

    /// <summary>
    /// can gain realm points
    /// /rp to toggle
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public bool GainRP
    {
        get => m_gainRP;
        set
        {
            Dirty = true;
            m_gainRP = value;
        }
    }

    /// <summary>
    /// autoloot
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public bool Autoloot
    {
        get => m_autoloot;
        set
        {
            Dirty = true;
            m_autoloot = value;
        }
    }

    /// <summary>
    /// Last Date for FreeLevel
    /// </summary>
    [DataElement(AllowDbNull = true)]
    public DateTime LastFreeLeveled
    {
        get
        {
            if (m_lastfreeleveled == DateTime.MinValue)
                m_lastfreeleveled = DateTime.Now;
            return m_lastfreeleveled;
        }
        set
        {
            Dirty = true;
            m_lastfreeleveled = value;
        }
    }

    /// <summary>
    /// Last Level for FreeLevel
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int LastFreeLevel
    {
        get => m_lastfreeLevel;
        set
        {
            Dirty = true;
            m_lastfreeLevel = value;
        }
    }

    [DataElement(AllowDbNull = true)]
    public string GuildNote
    {
        get => m_guildNote;
        set
        {
            Dirty = true;
            m_guildNote = value;
        }
    }

    [DataElement(AllowDbNull = false)]
    public bool ShowXFireInfo
    {
        get => m_showXFireInfo;
        set
        {
            Dirty = true;
            m_showXFireInfo = value;
        }
    }

    [DataElement(AllowDbNull = false)]
    public bool NoHelp
    {
        get => m_noHelp;
        set
        {
            Dirty = true;
            m_noHelp = value;
        }
    }

    [DataElement(AllowDbNull = false)]
    public bool ShowGuildLogins
    {
        get => m_showGuildLogins;
        set
        {
            Dirty = true;
            m_showGuildLogins = value;
        }
    }

    /// <summary>
    /// Is Champion level activated
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public bool Champion
    {
        get => m_cl;
        set
        {
            m_cl = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Champion level
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int ChampionLevel
    {
        get => m_clLevel;
        set
        {
            m_clLevel = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// Champion Experience
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public long ChampionExperience
    {
        get => m_clExperience;
        set
        {
            m_clExperience = value;
            Dirty = true;
        }
    }

    /// <summary>
    /// ML Line
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public byte ML
    {
        get => m_ml;
        set
        {
            Dirty = true;
            m_ml = value;
        }
    }

    /// <summary>
    /// ML Experience of this character
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public long MLExperience
    {
        get => m_mlExperience;
        set
        {
            Dirty = true;
            m_mlExperience = value;
        }
    }

    /// <summary>
    /// ML Level of this character
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public int MLLevel
    {
        get => m_mlLevel;
        set
        {
            Dirty = true;
            m_mlLevel = value;
        }
    }

    /// <summary>
    /// ML can be validated to next level
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public bool MLGranted
    {
        get => m_mlGranted;
        set
        {
            Dirty = true;
            m_mlGranted = value;
        }
    }

    /// <summary>
    /// is the player a roleplayer
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public bool RPFlag
    {
        get => m_roleplay;
        set
        {
            Dirty = true;
            m_roleplay = value;
        }
    }

    /// <summary>
    /// is the player flagged hardcore
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public bool HCFlag
    {
        get => m_hardcore;
        set
        {
            Dirty = true;
            m_hardcore = value;
        }
    }

    /// <summary>
    /// has the player reached 50 in hardcore mode
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public bool HCCompleted
    {
        get => m_hardcoreCompleted;
        set
        {
            Dirty = true;
            m_hardcoreCompleted = value;
        }
    }

    /// <summary>
    /// has the player used any free level/rr npc?
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public bool isBoosted
    {
        get => m_boosted;
        set
        {
            Dirty = true;
            m_boosted = value;
        }
    }

    /// <summary>
    /// Do we ignore all statistics for this player?
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public bool IgnoreStatistics
    {
        get => m_ignoreStatistics;
        set
        {
            Dirty = true;
            m_ignoreStatistics = value;
        }
    }

    /// <summary>
    /// what should we not display in Herald ?
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public byte NotDisplayedInHerald
    {
        get => m_notDisplayedInHerald;
        set
        {
            Dirty = true;
            m_notDisplayedInHerald = value;
        }
    }

    /// <summary>
    /// Should we hide the detailed specialization of this player in the APIs?
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public bool HideSpecializationAPI
    {
        get => m_hideSpecializationAPI;
        set
        {
            Dirty = true;
            m_hideSpecializationAPI = value;
        }
    }

    [DataElement(AllowDbNull = false)]
    public byte ActiveSaddleBags
    {
        get => m_activeSaddleBags;
        set
        {
            Dirty = true;
            m_activeSaddleBags = value;
        }
    }

    [DataElement(AllowDbNull = false)]
    public DateTime LastLevelUp
    {
        get => m_lastLevelUp;
        set
        {
            Dirty = true;
            m_lastLevelUp = value;
        }
    }

    /// <summary>
    /// Gets/sets the characters /played level time
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public long PlayedTimeSinceLevel
    {
        get => m_playedTimeSinceLevel;
        set
        {
            Dirty = true;
            m_playedTimeSinceLevel = value;
        }
    }

    /// <summary>
    /// Gets/sets the characters option to receive ROGs /eventrog
    /// </summary>
    [DataElement(AllowDbNull = false)]
    public bool ReceiveROG
    {
        get => m_receiveROG;
        set
        {
            Dirty = true;
            m_receiveROG = value;
        }
    }

    /// <summary>
    /// List of Custom Params for this Character
    /// </summary>
    [Relation(LocalField = "DOLCharacters_ID", RemoteField = "DOLCharactersObjectId", AutoLoad = true,
        AutoDelete = true)]
    public DOLCharactersXCustomParam[] CustomParams;

    /// <summary>
    /// List of Random Number Decks for this Character
    /// </summary>
    [Relation(LocalField = "DOLCharacters_ID", RemoteField = "DOLCharactersObjectId", AutoLoad = true,
        AutoDelete = true)]
    public DOLCharactersXDeck[] RandomNumberDecks;
}

/// <summary>
/// DOL Characters (Player) Custom Params linked to Character Entry
/// </summary>
[DataTable(TableName = "DOLCharactersXCustomParam")]
public class DOLCharactersXCustomParam : CustomParam
{
    private string m_dOLCharactersObjectId;

    /// <summary>
    /// DOLCharacters Table ObjectId Reference
    /// </summary>
    [DataElement(AllowDbNull = false, Index = true, Varchar = 255)]
    public string DOLCharactersObjectId
    {
        get => m_dOLCharactersObjectId;
        set
        {
            Dirty = true;
            m_dOLCharactersObjectId = value;
        }
    }

    /// <summary>
    /// Create new instance of <see cref="DOLCharactersXCustomParam"/> linked to Character ObjectId
    /// </summary>
    /// <param name="DOLCharactersObjectId">DOLCharacters ObjectId</param>
    /// <param name="KeyName">Key Name</param>
    /// <param name="Value">Value</param>
    public DOLCharactersXCustomParam(string DOLCharactersObjectId, string KeyName, string Value)
        : base(KeyName, Value)
    {
        this.DOLCharactersObjectId = DOLCharactersObjectId;
    }

    /// <summary>
    /// Create new instance of <see cref="DOLCharactersXCustomParam"/>
    /// </summary>
    public DOLCharactersXCustomParam()
    {
    }
}