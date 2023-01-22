using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DOL.Database;
using DOL.GS.Quests;
using ECS.Debug;
using log4net;

namespace DOL.GS;

public class TimerService
{
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    private const string ServiceName = "Timer Service";

    private static HashSet<ECSGameTimer> ActiveTimers;
    private static Stack<ECSGameTimer> TimerToRemove;
    private static Stack<ECSGameTimer> TimerToAdd;

    private static long debugTick = 0;

    //debugTimer is for outputing Timer count/callback info for debug purposes
    public static bool debugTimer = false;

    //Number of ticks to debug the Timer
    public static int debugTimerTickCount = 0;


    static TimerService()
    {
        EntityManager.AddService(typeof(TimerService));
        ActiveTimers = new HashSet<ECSGameTimer>();
        TimerToAdd = new Stack<ECSGameTimer>();
        TimerToRemove = new Stack<ECSGameTimer>();
    }

    public static void Tick(long tick)
    {
        Diagnostics.StartPerfCounter(ServiceName);

        //debug variables
        Dictionary<string, int> TimerToRemoveCallbacks = null;
        Dictionary<string, int> TimerToAddCallbacks = null;
        var TimerToRemoveCount = 0;
        var TimerToAddCount = 0;

        //check if need to debug, then setup vars.
        if (debugTimer && debugTimerTickCount > 0)
        {
            TimerToRemoveCount = TimerToRemove.Count;
            TimerToAddCount = TimerToAdd.Count;
            TimerToRemoveCallbacks = new Dictionary<string, int>();
            TimerToAddCallbacks = new Dictionary<string, int>();
        }

        var addRemoveStartTick = GameTimer.GetTickCount();
        lock (_removeTimerLockObject)
        {
            while (TimerToRemove.Count > 0)
            {
                if (debugTimer && TimerToRemoveCallbacks != null && TimerToRemove.Peek() != null &&
                    TimerToRemove.Peek().Callback != null)
                {
                    var callbackMethodName = TimerToRemove.Peek().Callback.Method.DeclaringType + "." +
                                             TimerToRemove.Peek().Callback.Method.Name;
                    if (TimerToRemoveCallbacks.ContainsKey(callbackMethodName))
                        TimerToRemoveCallbacks[callbackMethodName]++;
                    else
                        TimerToRemoveCallbacks.Add(callbackMethodName, 1);
                }

                if (ActiveTimers.Contains(TimerToRemove.Peek()))
                    ActiveTimers.Remove(TimerToRemove.Pop());
                else
                    TimerToRemove.Pop();
            }
        }

        var addRemoveStopTick = GameTimer.GetTickCount();
        if (addRemoveStopTick - addRemoveStartTick > 25)
            log.Warn($"Long TimerService Remove Timers Time: {addRemoveStopTick - addRemoveStartTick}ms");

        addRemoveStartTick = GameTimer.GetTickCount();
        lock (_addTimerLockObject)
        {
            while (TimerToAdd.Count > 0)
            {
                if (debugTimer && TimerToAddCallbacks != null && TimerToAdd.Peek() != null &&
                    TimerToAdd.Peek().Callback != null)
                {
                    var callbackMethodName = TimerToAdd.Peek().Callback.Method.DeclaringType + "." +
                                             TimerToAdd.Peek().Callback.Method.Name;
                    if (TimerToAddCallbacks.ContainsKey(callbackMethodName))
                        TimerToAddCallbacks[callbackMethodName]++;
                    else
                        TimerToAddCallbacks.Add(callbackMethodName, 1);
                }

                if (!ActiveTimers.Contains(TimerToAdd.Peek()))
                    ActiveTimers.Add(TimerToAdd.Pop());
                else
                    TimerToAdd.Pop();
            }
        }

        addRemoveStopTick = GameTimer.GetTickCount();
        if (addRemoveStopTick - addRemoveStartTick > 25)
            log.Warn($"Long TimerService Add Timers Time: {addRemoveStopTick - addRemoveStartTick}ms");

        //Console.WriteLine($"timer size {ActiveTimers.Count}");
        /*
        if (debugTick + 1000 < tick)
        {
            Console.WriteLine($"timer size {ActiveTimers.Count}");
            debugTick = tick;
        }*/

        Parallel.ForEach(ActiveTimers.ToArray(), timer =>
        {
            try
            {
                if (timer != null && timer.NextTick < GameLoop.GameLoopTime)
                {
                    var startTick = GameTimer.GetTickCount();
                    timer.Tick();
                    var stopTick = GameTimer.GetTickCount();
                    if (stopTick - startTick > 25)
                        log.Warn(
                            $"Long TimerService.Tick for Timer Callback: {timer.Callback?.Method?.DeclaringType}:{timer.Callback?.Method?.Name}  Owner: {timer.TimerOwner?.Name} Time: {stopTick - startTick}ms");
                }
            }
            catch (Exception e)
            {
                log.Error($"Critical error encountered in Timer Service: {e}");
            }
        });


        //Output Debug info
        if (debugTimer && TimerToRemoveCallbacks != null && TimerToAddCallbacks != null)
        {
            log.Debug($"==== TimerService Debug - Total ActiveTimers: {ActiveTimers.Count} ====");

            log.Debug(
                $"==== TimerService RemoveTimer Top 10 Callback Methods. Total TimerToRemove Count: {TimerToRemoveCount} ====");

            foreach (var callbacks in TimerToRemoveCallbacks.OrderByDescending(callback => callback.Value).Take(10))
                log.Debug($"Callback Name: {callbacks.Key} Occurences: {callbacks.Value}");

            log.Debug(
                $"==== TimerService AddTimer Top 10 Callback Methods. Total TimerToAdd Count: {TimerToAddCount} ====");
            foreach (var callbacks in TimerToAddCallbacks.OrderByDescending(callback => callback.Value).Take(10))
                log.Debug($"Callback Name: {callbacks.Key} Occurences: {callbacks.Value}");

            log.Debug("---------------------------------------------------------------------------");

            if (debugTimerTickCount > 1)
            {
                debugTimerTickCount--;
            }
            else
            {
                debugTimer = false;
                debugTimerTickCount = 0;
            }
        }

        Diagnostics.StopPerfCounter(ServiceName);
    }

    private static readonly object _addTimerLockObject = new();

    public static void AddTimer(ECSGameTimer newTimer)
    {
        //  if (!ActiveTimers.Contains(newTimer))
        //  {
        lock (_addTimerLockObject)
        {
            TimerToAdd?.Push(newTimer);
        }
        //Console.WriteLine($"added {newTimer.Callback.GetMethodInfo()}");
        //  }
    }

    //Adds timer to the TimerToAdd Stack without checking it already exists. Helpful if the timer is being removed and then added again in same tick.
    //The Tick() method will still check for duplicate timer in ActiveTimers
    public static void AddExistingTimer(ECSGameTimer newTimer)
    {
        lock (_addTimerLockObject)
        {
            TimerToAdd?.Push(newTimer);
        }
    }

    private static readonly object _removeTimerLockObject = new();

    public static void RemoveTimer(ECSGameTimer timerToRemove)
    {
        lock (_removeTimerLockObject)
        {
            // if (ActiveTimers.Contains(timerToRemove))
            // {
            TimerToRemove?.Push(timerToRemove);
            //Console.WriteLine($"removed {timerToRemove.Callback.GetMethodInfo()}");
            // }
        }
    }

    public static bool HasActiveTimer(ECSGameTimer timer)
    {
        var currentTimers = new List<ECSGameTimer>();
        var timerAdds = new List<ECSGameTimer>();
        lock (_addTimerLockObject)
        {
            currentTimers = ActiveTimers.ToList();
            timerAdds = TimerToAdd.ToList();
        }

        return currentTimers.Contains(timer) || timerAdds.Contains(timer);
    }
}

public class ECSGameTimer
{
    /// <summary>
    /// This delegate is the callback function for the ECS Timer
    /// </summary>
    public delegate int ECSTimerCallback(ECSGameTimer timer);

    public ECSTimerCallback Callback;
    public int Interval;
    public long StartTick;
    public long NextTick => StartTick + Interval;

    public GameObject TimerOwner;

    //public GameTimer.TimeManager GameTimeOwner;
    public bool IsAlive => TimerService.HasActiveTimer(this);

    /// <summary>
    /// Holds properties for this region timer
    /// </summary>
    private PropertyCollection m_properties;

    public ECSGameTimer(GameObject target)
    {
        TimerOwner = target;
    }

    public ECSGameTimer(GameObject target, ECSTimerCallback callback, int interval)
    {
        TimerOwner = target;
        Callback = callback;
        Interval = interval;
        Start();
    }

    public ECSGameTimer(GameObject target, ECSTimerCallback callback)
    {
        TimerOwner = target;
        Callback = callback;
    }

    public void Start()
    {
        if (Interval <= 0)
            Start(500); //use half-second intervals by default
        else
            Start((int) Interval);
    }

    public void Start(int interval)
    {
        StartTick = GameLoop.GameLoopTime;
        Interval = interval;
        TimerService.AddTimer(this);
    }

    public void StartExistingTimer(int interval)
    {
        StartTick = GameLoop.GameLoopTime;
        Interval = interval;
        TimerService.AddExistingTimer(this);
    }

    public void Stop()
    {
        TimerService.RemoveTimer(this);
    }

    public void Tick()
    {
        StartTick = GameLoop.GameLoopTime;
        if (Callback != null) Interval = (int) Callback.Invoke(this);

        if (Interval == 0) Stop();
    }
    /*
    /// <summary>
    /// Stores the time where the timer was inserted
    /// </summary>
    private long m_targetTime = -1;
    */

    /// <summary>
    /// Gets the time left until this timer fires, in milliseconds.
    /// </summary>
    public int TimeUntilElapsed => (int) (StartTick + Interval - GameLoop.GameLoopTime);

    /// <summary>
    /// Gets the properties of this timer
    /// </summary>
    public PropertyCollection Properties
    {
        get
        {
            if (m_properties == null)
                lock (this)
                {
                    if (m_properties == null)
                    {
                        var properties = new PropertyCollection();
                        Thread.MemoryBarrier();
                        m_properties = properties;
                    }
                }

            return m_properties;
        }
    }
}