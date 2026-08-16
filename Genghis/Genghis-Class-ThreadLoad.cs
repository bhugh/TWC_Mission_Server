using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections;
using System.Globalization;
using maddox.game;
using maddox.game.world;
using maddox.GP;
using maddox.game.page;
using part;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Text;

public class ThreadLoadMission : AMission
{
    [DllImport("Kernel32", EntryPoint = "GetCurrentThreadId", ExactSpelling = true)]
    public static extern Int32 GetCurrentWin32ThreadId();
    private Mission mainmission;

    public double recentCPUPercent = 0; //%, ie 0-100 not 0-1
    public double allTimeCPUPercent = 0;//%, ie 0-100 not 0-1
    public double timeSinceStart_s = 0; // in seconds
	
	public TimeSpan oneIntervalTickLength; //
    public TimeSpan allTimeTickLength;//
	public TimeSpan allTimeMaxTickLength;//
	public TimeSpan currentTickLength;
	private long initTick;
	private long lastSingleTick;
	private long lastOneIntervalTick;
	public TimeSpan lastSingleTickTime;
	public TimeSpan lastOneIntTickTime;

	
	public double oneIntAveTick_ms; //************* average tick length over past interval (10 seconds)
			
	public double allTimeAveTick_ms; //************ average tick length since server start
	

    public CircularArray<double> CPUPercent_stack = new CircularArray<double>(6); //saves the last 6 values/last 1 minute
    public double rollingAverageCPUPercent; //rolling average over the past 1 minute, in % ie 0-100 NOT 0-1					
			
	public CircularArray<double> TickTime_stack = new CircularArray<double>(6); //saves the last 6 values/last 1 minute
    public double rollingAverageTickTime_ms; //************ average tick length over past minute 

    public ThreadLoadMission(Mission msn)
    {
        mainmission = msn;
    }

    private System.Threading.Timer threadLoadTimer;
    public Stopwatch threadLoadStopWatch = new Stopwatch();
	public Stopwatch threadLoadTickStopWatch = new Stopwatch();
    private TimeSpan initCPUTime;
    private TimeSpan lastTime;
    private TimeSpan lastCPUTime;
	
    public int threadID;
    public int win32ThreadID;
    public Thread ourThread;
    public ProcessThread ourProcessThread;

    public void threadLoadTimer_recurs()
    {
        try
        {
            Console.WriteLine("threadLoad: Starting timer! " + DateTime.UtcNow.ToString("T"));
            threadLoadTimer = new System.Threading.Timer(
                new TimerCallback(threadLoad_callback),
                null,
                dueTime: 10000, //wait time @ startup
                period: 10000); //periodically call the callback at this interval, every 4-6 minutes say

            initThreadLoad();
        }
        catch (Exception ex) { Console.WriteLine("threadLoad_recurs error: " + ex.Message); }
    }

    public void threadLoadTimer_dispose()
    {
        try
        {
            if (threadLoadTimer != null) threadLoadTimer.Dispose();
        }
        catch (Exception ex) { }
    }

    public void initThreadLoad()
    {
        try
        {
            threadLoadStopWatch.Reset();
            threadLoadStopWatch.Start();

            ourThread = Thread.CurrentThread;

            threadID = ourThread.ManagedThreadId;
            win32ThreadID = GetCurrentWin32ThreadId();
            ourProcessThread = getThreadByID(win32ThreadID);

            //initCPUTime = ourThread.TotalProcessorTime;
            initCPUTime = ourProcessThread.TotalProcessorTime;
            lastCPUTime = initCPUTime;
            lastTime = threadLoadStopWatch.Elapsed;		
			lastSingleTickTime = lastTime;
			lastOneIntTickTime = lastTime;			
			
			initTick = Time.tickCounter();
			lastSingleTick = initTick;
			lastOneIntervalTick = initTick;			

            Console.WriteLine("threadLoad: Initializing!  Thread ID: {0}", win32ThreadID);
        }
        catch (Exception ex) { Console.WriteLine("threadLoad_callback error: " + ex.Message); }
    }

    public void threadLoad_callback(object o)
    {
        try
        {
            //err, this will be an entirely new thread called by the timer, so this
            //part wn't work at all - we are interested in the one main launcher64.exe
            //thread that all the missions run on
            /*int currentWin32ThreadID = GetCurrentWin32ThreadId();
            if (win32ThreadID != currentWin32ThreadID)
            {
                Console.WriteLine("threadLoad: New thread!  Was {0}, now {1}. Restarting stats...", win32ThreadID, currentWin32ThreadID);
                initThreadLoad();
                return;
            }
            */

            TimeSpan thisCPUTime = ourProcessThread.TotalProcessorTime;

            TimeSpan thisTime = threadLoadStopWatch.Elapsed;
            TimeSpan deltaCPU = thisCPUTime.Subtract(lastCPUTime);
            TimeSpan deltaTime = thisTime.Subtract(lastTime);
            TimeSpan elapsedCPU = thisCPUTime.Subtract(initCPUTime);
            //TimeSpan elapsedTime = thisTime.Subtract(Stopwatch.)

            recentCPUPercent = safeDivide(deltaCPU.TotalMilliseconds, deltaTime.TotalMilliseconds) * 100.0;
            CPUPercent_stack.Push(recentCPUPercent);
            rollingAverageCPUPercent = rollingAverage(CPUPercent_stack.Array);
            

            allTimeCPUPercent = safeDivide(elapsedCPU.TotalMilliseconds, threadLoadStopWatch.ElapsedMilliseconds) * 100.0;
            timeSinceStart_s = threadLoadStopWatch.ElapsedMilliseconds / 1000.0;

            if (timeSinceStart_s < 240) rollingAverageCPUPercent = 0; //don't push anything here until 4 minutes in...
			
			
			//TimeSpan tickOneIntLength = thisTime.Subtract(lastOneIntTickTime);
			long deltaTicks = Time.tickCounter() - lastOneIntervalTick;
			
			oneIntAveTick_ms = safeDivide(deltaTime.TotalMilliseconds, (double)deltaTicks);
			
			allTimeAveTick_ms = safeDivide(thisTime.TotalMilliseconds, (double)Time.tickCounter());
						
			
			TickTime_stack.Push(oneIntAveTick_ms);
            rollingAverageTickTime_ms = rollingAverage(TickTime_stack.Array);

			Console.WriteLine("threadLoad: {0:n1}% (rolling ave. {3:n1}%) - " +
			"{1:n0} ms used of {2:n0} ms since last call", recentCPUPercent, deltaCPU.TotalMilliseconds, deltaTime.TotalMilliseconds, rollingAverageCPUPercent);
            Console.WriteLine("threadLoad: {0:n1}% - " + "{1:n2} s used of {2:n2} s since program start", allTimeCPUPercent, elapsedCPU.TotalSeconds, timeSinceStart_s);
			Console.WriteLine("threadLoad: current tick {0:n0}ms; max tick {1:n0}ms; interval {2:n0}ms; rolling ave {3:n0}ms ; all time {4:n0}ms", currentTickLength.TotalMilliseconds, allTimeMaxTickLength.TotalMilliseconds, oneIntAveTick_ms, rollingAverageTickTime_ms, allTimeAveTick_ms );
			
			allTimeMaxTickLength = currentTickLength; //actually not all time but for this interval

            lastTime = thisTime;
            lastCPUTime = thisCPUTime;
			lastOneIntervalTick = Time.tickCounter();
        }
        catch (Exception ex) { Console.WriteLine("threadLoad_callback error: " + ex.Message); };
    }

    public double safeDivide(double numer, double denom)
    {
        try
        {
            if (denom != 0) return numer / denom;
            if (numer == 0) return 0;
            else return 100000000000;
        }
        catch (Exception ex) { Console.WriteLine("threadLoad safeDivide error: " + ex.Message); return 0; };
    }

    public ProcessThread getThreadByID(int ID)
    {
        try
        {
            Process p = Process.GetCurrentProcess();
            foreach (ProcessThread pt in p.Threads)
            {
                if (pt.Id == ID) return pt;
            }

            return null;
        }
        catch (Exception ex) { Console.WriteLine("threadLoad getThreadByID error: " + ex.Message);
            return null;
        }
    }

    public double rollingAverage(double [] ar) {
        if (ar.Length == 0) return 0;
        double accum = 0;

        for (int i = 0; i < ar.Length; i++ )
        {
            accum += ar[i];
        }
        return accum / ar.Length;
    }
	
	public override void OnTickGame()
    {
        base.OnTickGame();
		
		TimeSpan thisTime = threadLoadStopWatch.Elapsed;
		currentTickLength = thisTime.Subtract(lastSingleTickTime);
		if (TimeSpan.Compare(currentTickLength, allTimeMaxTickLength) == 1 ) allTimeMaxTickLength = currentTickLength; // compare == 1 means 1st greater than 2nd; ==0 means they are equal
		lastSingleTickTime = thisTime;
		
		
	}

    public override void OnBattleStarted()
    {
        base.OnBattleStarted();

        threadLoadTimer_recurs();

    }

    public override void OnBattleStoped()
    {
        base.OnBattleStoped();

        threadLoadTimer_dispose();

    }
}

