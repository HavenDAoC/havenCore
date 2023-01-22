using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DOL.Database;
using DOL.GS.Quests;
using ECS.Debug;

namespace DOL.GS;

public class WeeklyQuestService
{
    private static DateTime lastWeeklyRollover;

    private const string ServiceName = "WeeklyQuestService";
    private const string WeeklyIntervalKey = "WEEKLY";

    static WeeklyQuestService()
    {
        EntityManager.AddService(typeof(WeeklyQuestService));

        var loadQuestsProp = GameServer.Database.SelectAllObjects<TaskRefreshIntervals>();

        foreach (var interval in loadQuestsProp)
            if (interval.RolloverInterval.Equals(WeeklyIntervalKey))
                lastWeeklyRollover = interval.LastRollover;

        if (lastWeeklyRollover == null)
            lastWeeklyRollover = DateTime.UnixEpoch;
    }

    public static void Tick(long tick)
    {
        Diagnostics.StartPerfCounter(ServiceName);
        //.WriteLine($"daily:{lastDailyRollover.Date.DayOfYear} weekly:{lastWeeklyRollover.Date.DayOfYear+7} now:{DateTime.Now.Date.DayOfYear}");

        //this is where the weekly check will go once testing is finished
        if (lastWeeklyRollover.Date.DayOfYear + 7 < DateTime.Now.Date.DayOfYear ||
            lastWeeklyRollover.Year < DateTime.Now.Year)
        {
            lastWeeklyRollover = DateTime.Now;

            //update db
            var loadQuestsProp =
                GameServer.Database.SelectObject<TaskRefreshIntervals>(DB.Column("RolloverInterval")
                    .IsEqualTo(WeeklyIntervalKey));
            //update the one we've got, otherwise...
            if (loadQuestsProp != null)
            {
                loadQuestsProp.LastRollover = DateTime.Now;
                GameServer.Database.SaveObject(loadQuestsProp);
            }
            else
            {
                //make a new one
                var newTime = new TaskRefreshIntervals();
                newTime.LastRollover = DateTime.Now;
                newTime.RolloverInterval = WeeklyIntervalKey;
                GameServer.Database.AddObject(newTime);
            }

            foreach (var player in EntityManager.GetAllPlayers())
            {
                var questsToRemove = new List<AbstractQuest>();
                foreach (var quest in player.QuestListFinished)
                    if (quest is Quests.WeeklyQuest)
                    {
                        quest.AbortQuest();
                        questsToRemove.Add(quest);
                    }

                foreach (var quest in questsToRemove)
                {
                    player.QuestList.Remove(quest);
                    player.QuestListFinished.Remove(quest);
                }
            }

            var existingWeeklyQuests =
                GameServer.Database.SelectObjects<DBQuest>(DB.Column("Name").IsLike("%WeeklyQuest%"));

            foreach (var existingWeeklyQuest in existingWeeklyQuests)
                if (existingWeeklyQuest.Step <= -1)
                    GameServer.Database.DeleteObject(existingWeeklyQuest);
        }

        Diagnostics.StopPerfCounter(ServiceName);
    }
}