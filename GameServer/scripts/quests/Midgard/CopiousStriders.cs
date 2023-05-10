using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using log4net;
using System;
using System.Reflection;
using DOL.GS.Trainer;

namespace DOL.GS.Quests.Midgard
{
    public class CopiousStriders : BaseQuest
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        protected const int MIN_LEVEL = 10;
        protected const int MAX_LEVEL = 50;
        protected const string QUEST_TITLE = "Copious Striders";
        protected const string QUEST_GIVER_NAME = "Alomali";
        protected const string QUEST_FINISH_NAME = "Alomali";
        private static GameNPC _alomali;
        private static ItemTemplate _striderLeg;
        private static ItemTemplate _reedBracer;

        public override string Name
        {
            get { return QUEST_TITLE; }
        }

        public override string Description
        {
            get
            {
                switch (Step)
                {
                    case 1:
                        return "Seek out and slay a water strider.";
                    case 2:
                        return "Return the strider leg to Alomali.";
                }
                return base.Description;
            }
        }
        public CopiousStriders() : base() { }
        public CopiousStriders(GamePlayer questingPlayer) : base(questingPlayer) { }
        public CopiousStriders(GamePlayer questingPlayer, int step) : base(questingPlayer, step) { }
        public CopiousStriders(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest) { }

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            if (!ServerProperties.Properties.LOAD_QUESTS)
                return;

            #region Initialize Quest Giver NPC
            GameNPC[] npcs = WorldMgr.GetNPCsByName(QUEST_GIVER_NAME, eRealm.Midgard);

            if (npcs.Length > 0)
                foreach (GameNPC npc in npcs)
                    if (npc.CurrentRegionID == 181 && npc.X == 424066 && npc.Y == 446316)
                    {
                        _alomali = (ForesterTrainer)npc;
                        break;
                    }

            if (_alomali == null)
            {
                if (log.IsWarnEnabled)
                    log.Warn("Could not find Alomali, creating entity now...");
                
                _alomali = new GameNPC();
                _alomali.Model = 169;
                _alomali.Name = "Alomali";
                _alomali.LoadEquipmentTemplateFromDatabase("97f36f25-af3d-4909-bfd0-70293ae26d18");
                _alomali.GuildName = "";
                _alomali.Realm = eRealm.Midgard;
                _alomali.CurrentRegionID = 100;
                _alomali.Size = 50;
                _alomali.Level = 47;
                _alomali.X = 773244;
                _alomali.Y = 756471;
                _alomali.Z = 4600;
                _alomali.Heading = 2820;
                _alomali.AddToWorld();
                if (SAVE_INTO_DATABASE)
                    _alomali.SaveIntoDatabase();
                
            }
            #endregion Initialize Quest Giver NPC

            #region Initialize Quest Items
            _reedBracer = GameServer.Database.FindObjectByKey<ItemTemplate>("qi_reedBracer");
            
            if (_reedBracer == null)
            {
                _reedBracer = new ItemTemplate();
                _reedBracer.Name = "Reed Bracer";
                
                if (log.IsWarnEnabled)
                    log.Warn("Could not find " + _reedBracer.Name + ", creating item now...");

                _reedBracer.Weight = 1;
                _reedBracer.Model = 598;
                _reedBracer.Id_nb = "qi_reedBracer";
                _reedBracer.IsPickable = true;
                _reedBracer.IsDropable = true;
                _reedBracer.IsTradable = true;
                _reedBracer.Quality = 100;
                _reedBracer.Condition = 50000;
                _reedBracer.MaxCondition = 50000;
                _reedBracer.Durability = 50000;
                _reedBracer.MaxDurability = 50000;
                _reedBracer.Bonus = 1;
                _reedBracer.BonusLevel = 7;
                _reedBracer.LevelRequirement = 10;
                _reedBracer.Level = 12;
                _reedBracer.Object_Type = 41;
                _reedBracer.Item_Type = 33;
                _reedBracer.Bonus1Type = 10;
                _reedBracer.Bonus1 = 16;
                _reedBracer.Bonus2Type = 12;
                _reedBracer.Bonus2 = 4;
            }
            
            _striderLeg = GameServer.Database.FindObjectByKey<ItemTemplate>("qi_striderLeg");
            
            if (_striderLeg == null)
            {
                _striderLeg = new ItemTemplate();
                _striderLeg.Name = "Strider Leg";
                
                if (log.IsWarnEnabled)
                    log.Warn("Could not find " + _striderLeg.Name + ", creating quest item now...");

                _striderLeg.Weight = 1;
                _striderLeg.Model = 626;
                _striderLeg.Id_nb = "qi_striderLeg";
                _striderLeg.IsPickable = true;
                _striderLeg.IsDropable = true;
                _striderLeg.IsTradable = false;
                _striderLeg.Quality = 100;
                _striderLeg.Condition = 1000;
                _striderLeg.MaxCondition = 1000;
                _striderLeg.Durability = 1000;
                _striderLeg.MaxDurability = 1000;
            }
            #endregion Initialize Quest Items
            
            GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
            GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

            GameEventMgr.AddHandler(_alomali, GameObjectEvent.Interact, new DOLEventHandler(TalkToAlomali));
            GameEventMgr.AddHandler(_alomali, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToAlomali));

            _alomali.AddQuestToGive(typeof(CopiousStriders));
            if (log.IsInfoEnabled)
            {
                log.Info($"Quest {QUEST_TITLE} initialized");
            }
        }

        [ScriptUnloadedEvent]
        public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
        {
            if (_alomali == null) return;

            GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
            GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));
            
            GameEventMgr.RemoveHandler(_alomali, GameObjectEvent.Interact, new DOLEventHandler(TalkToAlomali));
            GameEventMgr.RemoveHandler(_alomali, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToAlomali));
            
            _alomali.RemoveQuestToGive(typeof(CopiousStriders));
        }

        protected static void SubscribeQuest(DOLEvent e, object sender, EventArgs args)
        {
            QuestEventArgs qargs = args as QuestEventArgs;
            if (qargs == null)
            {
                return;
            }
            if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(CopiousStriders)))
            {
                return;
            }
            if (e == GamePlayerEvent.AcceptQuest)
            {
                CheckPlayerAcceptQuest(qargs.Player, 0x01);
            }
            else if (e == GamePlayerEvent.DeclineQuest)
            {
                CheckPlayerAcceptQuest(qargs.Player, 0x00);
            }
        }

        private static void TalkToAlomali(DOLEvent e, object sender, EventArgs args)
        {
            GamePlayer player = ((SourceEventArgs)args).Source as GamePlayer;
            if (player == null)
                return;
            
            if (_alomali.CanGiveQuest(typeof(CopiousStriders), player) <= 0)
                return;
            
            CopiousStriders quest = player.IsDoingQuest(typeof(CopiousStriders)) as CopiousStriders;
            _alomali.TurnTo(player);
            if (e == GameObjectEvent.Interact)
            {
                if (quest != null)
                {
                    switch (quest.Step)
                    {
                        case 1:
                            _alomali.SayTo(player, $"If you could bring back to me one of these legs, I would be forever in your debt.");
                            break;
                        case 2:
                            _alomali.SayTo(player, $"Hello, {player.CharacterClass.Name}. Find a water strider and bring me one of its legs!");
                            break;
                    }
                }
                else
                {
                    _alomali.SayTo(player, "Hail to thee. I seem to be in a [bit of a snitch].");
                }
            }
            else if (e == GameLivingEvent.WhisperReceive)
            {
                WhisperReceiveEventArgs wArgs = (WhisperReceiveEventArgs)args;
                
                if (quest != null)
                {
                    switch (quest.Step)
                    {
                        case 1:
                            _alomali.SayTo(player, $"If you could bring back to me on of these legs I would be forever in your debt!");
                            break;
                        case 2:
                            _alomali.SayTo(player, $"Ah! Thank you so much! I hope this small amount of coin is enough to compensate you. I may have other tasks in which you could help. Return to me later when you have gained more experience and I will tell you of them.");
                            
                            quest.FinishQuest();
                            break;
                    }
                }
                
                if (quest == null)
                {
                    switch (wArgs.Text)
                    {
                        case "bit of a snitch":
                            _alomali.SayTo(player, "My master has asked of me [a deed] I cannot complete.");
                            break;
                        case "a deed":
                            _alomali.SayTo(player, "Aye! He asks the impossible! He wishes me to travel to [Striders' Isle] and return to him the leg of one of [the beasts] there.");
                            break;
                        case "Striders' Isle":
                            _alomali.SayTo(player, "It is the island behind me. A solitary rune stone stands there.");
                            break;
                        case "the beasts":
                            _alomali.SayTo(player, "There are these spider-like beasts that inhabit the island. Their legs are [of great use] to those in my practice.");
                            break;
                        case "of great use":
                            _alomali.SayTo(player, "If you could bring back to me on of these legs I would be forever in your debt!");
                            player.Out.SendQuestSubscribeCommand(_alomali, QuestMgr.GetIDForQuestType(typeof(CopiousStriders)), "Do you accept the Copious Striders quest?");
                            break;
                    }
                }
                else
                {
                    switch (wArgs.Text)
                    {
                        case "abort":
                            player.Out.SendCustomDialog("Do you really want to abort this quest? \nAll items gained will be lost.", new CustomDialogResponse(CheckPlayerAbortQuest));
                            break;
                        case "of great use":
                            if (quest.Step == 1)
                            {
                                _alomali.SayTo(player, "If you could bring back to me one of these legs I would be forever in your debt!");
                                quest.Step = 2;
                            }
                            break;
                    }
                }
            }
            else if (e == GameObjectEvent.ReceiveItem)
            {
                var rArgs = (ReceiveItemEventArgs) args;
                if (quest == null) return;
                if (rArgs.Item.Id_nb != _striderLeg.Id_nb) return;
                _alomali.SayTo(player, "Ah! Thank you so much! I hope this small amount of coin is enough to compensate you. I may have other tasks in which you could help. Return to me later when you have gained more experience and I will tell you of them.");
                quest.FinishQuest();
            }
        }

        public override void Notify(DOLEvent e, object sender, EventArgs args)
        {
            GamePlayer player = sender as GamePlayer;
            if (player?.IsDoingQuest(typeof(CopiousStriders)) == null)
                return;
            
            if (sender != m_questPlayer)
                return;
            
            if (Step == 1 && e == GameLivingEvent.EnemyKilled)
            {
                EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs)args;
               
                if (gArgs.Target.Name.ToLower() == "water strider")
                {
                    GiveItem(player, _striderLeg);
                    Step = 2;
                }
            }
        }

        private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
        {
            CopiousStriders quest = player.IsDoingQuest(typeof(CopiousStriders)) as CopiousStriders;
            if (quest == null)
            {
                return;
            }
            if (response == 0x00)
            {
                SendSystemMessage(player, "Good, now go out there and finish your work!");
            }
            else
            {
                SendSystemMessage(player, "You have declined the quest " + QUEST_TITLE + ".");
                quest.AbortQuest();
            }
        }

        private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
        {
            if (_alomali.CanGiveQuest(typeof(CopiousStriders), player) <= 0)
                return;

            if (player.IsDoingQuest(typeof(CopiousStriders)) != null)
                return;

            if (response == 0x00)
            {
                SendReply(player, "Oh, well... if you change your mind, please come back!");
            }
            else
            {
                if (!_alomali.GiveQuest(typeof(CopiousStriders), player, 1))
                    return;

                SendReply(player, "If you could bring back to me one of these legs I would be forever in your debt!");
            }
        }

        public override bool CheckQuestQualification(GamePlayer player)
        {
            if (player.IsDoingQuest(typeof(CopiousStriders)) != null)
                return true;
                
            if (player.Level < MIN_LEVEL || player.Level > MAX_LEVEL)
                return false;

            return true;
        }

        public override void FinishQuest()
        {
            RemoveItem(m_questPlayer, _striderLeg);
            m_questPlayer.AddMoney(Money.GetMoney(0, 0, 0, 3, 0), "You are awarded 3 silver!");
            GiveItem(m_questPlayer, _reedBracer);

            base.FinishQuest();
        }
    }
}
