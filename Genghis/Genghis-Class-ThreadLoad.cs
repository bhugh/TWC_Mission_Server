using System; // Ensure this is at the absolute top of your file
using System.IO;
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
using maddox.steam;
using part;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Text;


public class ThreadLoadMission : AMission
{
    [DllImport("Kernel32", EntryPoint = "GetCurrentThreadId", ExactSpelling = true)]
    public static extern Int32 GetCurrentWin32ThreadId();
    public Mission mainmission;
	//public TWClient twclient;
	//public TWServer twserver;
	private bool _handlerRegistered;
	
	// Keep a static reference so garbage collection doesn't clean them up
	// intercepts console & looks for steam connection errors etc
    //
	private static ConsoleInterceptor _interceptor;
    //private static AsyncConsoleProcessor _processor;

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
		//twclient = new TWClient(msn, this);
		//twserver = new TWServer(msn, this);
		
		//Hopefully handle any program crashes not otherwise handled
		//such as Steam disconnect...
		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		_handlerRegistered = true;
		
		

		
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
	
	

			
	
	
	public override void Inited()
    {
        base.Inited();
        
        try
        {
			initConsoleInterceptor();
			startInputParser();
			
            // Subscribe to the global Steam WClient callback 
            // Note: If WClient is instantiated per-server instance, hook into the active instance
            /*if (WClient.Instance != null)
            {
                WClient.Instance.onSteamShutdown += MyCustomSteamShutdownHandler;
                // Double check if there is an onSteamDisconnect or onConnectionLost variant
            }
			*/

			
			/*
			TextWriter originalOut = Console.Out;
			ConsoleInterceptor interceptor = new ConsoleInterceptor(originalOut);

			// Initialize our background worker
			using (AsyncConsoleProcessor processor = new AsyncConsoleProcessor(mainmission))
			{
				// Direct the event to push messages onto the background queue
				interceptor.OnLineWritten += processor.EnqueueMessage;
				Console.SetOut(interceptor);

				// Test execution
				Console.WriteLine("Message 1: Safe and fast.");
				Console.WriteLine("Message 2: Running off the main thread.");

				// Keep main thread alive briefly to let background thread finish logging
				//System.Threading.Thread.Sleep(500); 

				Console.SetOut(originalOut);
			}
			*/
			
			//_processor = new AsyncConsoleProcessor(mainmission, this);

			// 2. Initialize interceptor with the default Console stream
			//Interceptor = new ConsoleInterceptor(Console.Out, mainmission, this);

			// 3. Link the interceptor event to the async queue
			//_interceptor.OnLineWritten += _processor.EnqueueMessage;

			// 4. Divert the system output globally
			//Console.SetOut(_interceptor);

			// =======================================================
			// Everything below here works exactly as normal. 
			// All Console.Write/WriteLine statements print to the screen 
			// AND get processed safely on the background thread.
			// =======================================================

			Console.WriteLine("ThreadLoad: Console Interception started...");
			
        } catch (Exception ex)
        {
            
            Console.WriteLine("[ThreadLoad INITED ERROR]: " + ex.Message);
        }
    }
	
	

	/*
	    try
        {
            if (WClient.Instance != null)
            {
                WClient.Instance.onSteamShutdown -= MyCustomSteamShutdownHandler;
            }
        }
        catch { }
		*/
	
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
		
		//One way:
		/*
			interceptor.OnLineWritten += (message) =>
			{
				// Do whatever you want with the captured message here
				// (e.g., log to file, send to a UI window, or telemetry)
				//System.Diagnostics.Debug.WriteLine($"[MONITOR CAPTURED]: {message}");
				
				File.AppendAllText(mainmission.CLOD_PATH + mainmission.FILE_PATH + "/launchercrashes/shadow-console.log", message + "\n");
			};
			*/
		
			
			
			//Console.SetOut(TextWriter.Null); //completely mutes the console.....

			// --- Test cases ---
			//Console.WriteLine("Hello World!");
			//Console.WriteLine("Monitoring works in real-time.");
			
			// 4. (Optional) Restore the original console behavior when done
			//Console.SetOut(originalOut);
			
			

    }

    public override void OnBattleStoped()
    {
        base.OnBattleStoped();
		
		// 5. set console output back to normal
		_interceptor.localDispose();
		Console.SetOut(originalOut);
		

        threadLoadTimer_dispose();
		_keepRunningInputThread = false;
		_keepRunningMinuteThread = false;
		
	    if (_handlerRegistered)
	    {
			AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
			_handlerRegistered = false;
		}

    }
	
	public DateTime lastCrash = DateTime.Now;
	public DateTime lastFileSave = DateTime.Now.AddHours(-1);
	
	//Returns true of we've had a crash (which includes Steam disconnection) within past 60 seconds
	public bool SoftExit (){
		double timeSinceLastCrash_s = DateTime.Now.Subtract(lastCrash).TotalSeconds;
		if (timeSinceLastCrash_s < 60) return true;
		return false;
	}

	
	public void OnUnhandledException(string sender, string st)
	{
		Exception ex = new Exception(st);
		UnhandledExceptionEventArgs e = new UnhandledExceptionEventArgs(exception: ex, isTerminating: true); 
		OnUnhandledException(sender: (object)sender, e: e);
				
	}
	
	//should handle most ANY error not otherwise captured; we'll see
	//Needs: AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
	//on initialization
	public void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		try 
		{
			File.WriteAllText(mainmission.CLOD_PATH + mainmission.FILE_PATH + "/launchercrashes/launcher-crashes-errors-semaphore.log", "1"); //Do a very quick simple write first; if all else fails, file timestamp indicates when the crash happened
			
			DateTime now = DateTime.Now;
			
			double timeSinceLastCrash_s = now.Subtract(lastCrash).TotalSeconds;
			lastCrash = now;
			double timeSinceLastFileSave_s = now.Subtract(lastFileSave).TotalSeconds;			
			
			var ex = new Exception("Unknown error!"); 
			if (e.ExceptionObject is Exception) ex = (Exception)e.ExceptionObject;
			string term = "(not terminating)";
			if (e.IsTerminating) term = "(TERMINATING!)";
			
			File.AppendAllText(mainmission.CLOD_PATH + mainmission.FILE_PATH + "/launchercrashes/launcher-crashes-errors.log", string.Format("\n{0:u} - UNHANDLED EXCEPTION ERROR! {2} Could be an error or crash ANYWHERE in the sim:\n\n {1}\n\n", now, ex, term));
			GamePlay.gpLogServer(null,">>>>>SERVER CLOSING (ERROR or CONNECTION LOST) - No planes or lives lost due to sudden exit - Stats & campaign state are saved", new object[] { });
			if (timeSinceLastFileSave_s > 30) {
				//Console.WriteLine("UNHANDLED EXCEPTION ERROR!  Could be an error or crash ANYWHERE in the sim: " + ex.ToString());
				mainmission.SaveMapState("");
				Console.WriteLine("Handling UNHANDLED EXCEPTION ERROR! 1");
				mainmission.MO_WriteMissionObjects(wait: true);
				Console.WriteLine("Handling UNHANDLED EXCEPTION ERROR! 2");

				
				
				if (mainmission.tacviewimplmission != null) mainmission.tacviewimplmission.StopRecorder(); //our last priority, but this safely stops & saves the tacview recording of the session.  We COULD do this only if .isTerminating is TRUE, bec. no recording will  take place after this even if the mission continues...  But safer just to stop now and save at least some/most of the Tacview that has been recorded so far.  If it cuts off unexpectedly it might be corrupted (though likely just a part line, could be edited  easily...).
				//Console.WriteLine("UNHANDLED EXCEPTION ERROR! 3B");
			
				lastFileSave = now;
				Console.WriteLine("Handling UNHANDLED EXCEPTION ERROR! 3");
				File.AppendAllText(mainmission.CLOD_PATH + mainmission.FILE_PATH + "/launchercrashes/launcher-crashes-errors.log", string.Format("{0:u} - Crash saves almost complete - all but .stats\n\n", DateTime.Now));
				
				//stb_StatRecorder.StbSr_FinishWaitingTasks() seems to horch things up badly so it just exits there
				//So this should be the LAST thing as we might not get past it...
				mainmission.statsmission.OnBattleStoped_doWork(false);
				
				
				//File.AppendAllText(mainmission.CLOD_PATH + mainmission.FILE_PATH + "/launchercrashes/launcher-crashes-errors.log", string.Format("{0:u} - UNHANDLED EXCEPTION ERROR!  Could be an error or crash ANYWHERE in the sim:\n\n {1}\n", DateTime.Now, ex));
				//SaveCrashReport(exception);
				//try to do a nice exit now
				//GamePlay.gpBattleStop();
				
				
				Console.WriteLine("Handling UNHANDLED EXCEPTION ERROR! 4A - Crash saves successfully completed - good exit");
				File.AppendAllText(mainmission.CLOD_PATH + mainmission.FILE_PATH + "/launchercrashes/launcher-crashes-errors.log", string.Format("Handling UNHANDLED EXCEPTION ERROR {0:u} - Crash saves successfully completed - good exit\n\n", DateTime.Now));
			} else {
				Console.WriteLine("Handling UNHANDLED EXCEPTION ERROR! 4B  - Good exit (but no saves requested)\n\n");
				File.AppendAllText(mainmission.CLOD_PATH + mainmission.FILE_PATH + "/launchercrashes/launcher-crashes-errors.log", string.Format("Handling UNHANDLED EXCEPTION ERROR {0:u} - Good exit (but no saves requested)\n\n", now));
			}
	    }
		
		catch
		{
			// Nothing further can safely be done
		}
	}
	
	TextWriter originalOut = Console.Out;
	private static volatile bool _keepRunningMinuteThread = true;
	
	private void initConsoleInterceptor () {
		// 1. Save the original console output stream
			//try to flush the buffer for log.txt
			Console.WriteLine("Battle begins!.............................................................................");
		    for (int i = 0; i < 100 ; i++) Console.WriteLine("...........................................................................................");
			Console.WriteLine("Battle begins!.............................................................................");
			Console.WriteLine("===========================================================================================");
			Console.WriteLine(">>>>>>     Console is MUTED! All text in logfile @ ClodDir/log_twc_Week_XXXX.txt    <<<<<<");
			Console.WriteLine(">>>>>>         CONSOLE ON & CONSOLE OFF to re-start or stop the console text        <<<<<<");
			Console.WriteLine(">>>>>>   (May need to repeat these & CLOD Commands if they do not work first time)  <<<<<<");
			Console.WriteLine("===========================================================================================");
			
			TextWriter originalOut = Console.Out;

			// 2. Create our interceptor and hook into the event
			_interceptor = new ConsoleInterceptor(originalOut, mainmission, this);
			Console.SetOut(_interceptor);  
			Console.WriteLine("");
			Console.WriteLine("");
			
			
			// Default configuration starts muted for extreme performance
			_interceptor.ConsoleDisplayActive = false; 
						
						
			AppDomain.CurrentDomain.ProcessExit += new EventHandler((sender, e) => 
			{
				// Explicitly trigger your interceptor's cleanup
				_interceptor.Dispose();
			});
			
			_keepRunningMinuteThread = true;
			//Write . to screen every 1 minute to show activity
			Task.Run(() => {
				int minuteCount = 0;
				while (_keepRunningMinuteThread && !_interceptor.ConsoleDisplayActive)
				{
					Thread.Sleep(60000);
					
					if (minuteCount %60 == 0) {
						int hr = minuteCount/60;
						if (hr > 0) originalOut.WriteLine("");
						originalOut.Write("Hour {0:00}:", hr);
						
					} else {
						originalOut.Write(".");
					}
					originalOut.Flush();
					minuteCount ++;
				}
			});
			
			/*
			// 3. Start a non-blocking background thread to watch for console key input commands
			Thread inputThread = new Thread(new ThreadStart(delegate
			{
				while (true)
				{
					string input = Console.ReadLine();
					if (string.IsNullOrEmpty(input)) continue;

					if (input.Equals("Console On", StringComparison.OrdinalIgnoreCase))
					{
						_interceptor.ConsoleDisplayActive = true;
						Console.Error.WriteLine("\n>>> VISIBLE CONSOLE OUTPUT ENABLED = [SYSTEM] OFF to turn it off <<<\n");
					}
					else if (input.Equals("Console Off", StringComparison.OrdinalIgnoreCase))
					{
						_interceptor.ConsoleDisplayActive = false;
						Console.Error.WriteLine("\n>>> VISIBLE CONSOLE OUTPUT DISABLED (LOGGING CONTINUES) - [SYSTEM] ON to turn it back on <<<\n");
					}
				}
			}));
			
			inputThread.IsBackground = true;
			inputThread.Start();
			*/
	}
	
	private static volatile bool _keepRunningInputThread = true;
	
	private void startInputParser() {
	
	     /*
		 //this doesn't work FOR SOME REASON
		Thread passiveInputThread = new Thread(new ThreadStart(delegate
		{
			StringBuilder inputBuffer = new StringBuilder();

			while (true)
			{
				// Thread sleep prevents this loop from thrashing a CPU core while idle
				//Thread.Sleep(15); 

				// Check if a physical key has been pressed without blocking the thread
				if (Console.KeyAvailable)
				{
					//Console.Error.WriteLine("\nGot one!\n");
					// intercept intercepted character (true parameter hides duplicate echo)
					ConsoleKeyInfo keyInfo = Console.ReadKey(true);
					
					Console.Error.WriteLine("\nGot one! {0}\n", keyInfo.KeyChar);

					// Send the key press right back to the original console stream 
					// so Cliffs of Dover still sees it natively on the exact first try!
					Console.Write(keyInfo.KeyChar);

					if (keyInfo.Key == ConsoleKey.Enter)
					{
						string commandLine = inputBuffer.ToString().Trim();
						inputBuffer.Clear();

						// Evaluate commands locally on the input side
						if (commandLine.IndexOf("Console On", StringComparison.OrdinalIgnoreCase) >= 0)
						{
							_interceptor.ConsoleDisplayActive = true;
							Console.Error.WriteLine("\n>>> VISIBLE CONSOLE OUTPUT ENABLED <<<\n");
						}
						else if (commandLine.IndexOf("Console Off", StringComparison.OrdinalIgnoreCase) >= 0)
						{
							_interceptor.ConsoleDisplayActive = false;
							Console.Error.WriteLine("\n>>> VISIBLE CONSOLE OUTPUT DISABLED (LOGGING CONTINUES) <<<\n");
						}
					}
					else if (keyInfo.Key == ConsoleKey.Backspace)
					{
						if (inputBuffer.Length > 0)
						{
							inputBuffer.Remove(inputBuffer.Length - 1, 1);
						}
					}
					else
					{
						// Add standard character inputs to our command analyzer
						if (keyInfo.KeyChar != '\0')
						{
							inputBuffer.Append(keyInfo.KeyChar);
						}
					}
				}
			}
		}));

		passiveInputThread.IsBackground = true;
		passiveInputThread.Start();
		*/
		_interceptor.ConsoleDisplayActive = false; 

		Thread inputThread = new Thread(new ThreadStart(delegate
		{
			_keepRunningInputThread = true;
			while (_keepRunningInputThread)
			{
				string input = Console.ReadLine();
				
		        if (string.IsNullOrEmpty(input)) 
				{
					Thread.Sleep(50); //prevents constant CPU thrashing.  Hopefully.
					continue;
				}

				if (input.Equals("Console On", StringComparison.OrdinalIgnoreCase))
				{
					_interceptor.ConsoleDisplayActive = true;
					Console.Error.WriteLine("\n>>> VISIBLE CONSOLE OUTPUT ENABLED - CONSOLE OFF to turn it off again<<<\n");
				}
				else if (input.Equals("Console Off", StringComparison.OrdinalIgnoreCase))
				{
					_interceptor.ConsoleDisplayActive = false;
					Console.Error.WriteLine("\n>>> VISIBLE CONSOLE OUTPUT DISABLED (LOGGING TO FILE CONTINUES) - CONSOLE ON to turn on again<<<\n");
				}
			}
		}));
    
    inputThread.IsBackground = true;
    inputThread.Start();

    // Mission logic continues down here...


	}
}






public class ConsoleInterceptor : TextWriter
{
    private readonly TextWriter _originalOutput;
    private readonly StringBuilder _lineBuffer = new StringBuilder();
    private Mission mainmission;
    private ThreadLoadMission threadloadmission;

    private readonly BlockingCollection<string> _logQueue = new BlockingCollection<string>();
    private StreamWriter _fileWriter;
    private readonly Thread _loggingThread;
    private readonly string _baseLogDirectory;
    private string _currentLogPath;
    private int _currentWeekNumber;

    private int _isConsoleDisplayActive = 0; 

    public bool ConsoleDisplayActive
    {
        get 
        { 
            return Interlocked.CompareExchange(ref _isConsoleDisplayActive, 0, 0) == 1; 
        }
        set 
        { 
            Interlocked.Exchange(ref _isConsoleDisplayActive, value ? 1 : 0); 
        }
    }

    public ConsoleInterceptor(TextWriter originalOutput, Mission msn, ThreadLoadMission tlmsn)
    {
        _originalOutput = originalOutput;
        mainmission = msn;
        threadloadmission = tlmsn;

        _baseLogDirectory = mainmission.CLOD_PATH;
        
        ManageOldLogFiles();

        _currentWeekNumber = GetIso8601WeekOfYear(DateTime.Now);
        _currentLogPath = GetWeeklyLogPath(DateTime.Now);
        
        _fileWriter = new StreamWriter(_currentLogPath, true, Encoding.UTF8);
        _fileWriter.AutoFlush = true;
        
        _loggingThread = new Thread(ProcessLogQueue);
        _loggingThread.IsBackground = true;
        _loggingThread.Name = "ConsoleLoggingThread";
        _loggingThread.Start();
		_logQueue.Add("%$#@!%$#@!");
		
    }
	
	public void localDispose(){
		Dispose(true);
	}

    public override Encoding Encoding
    {
        get { return _originalOutput.Encoding; }
    }

    public override void Write(char value)
    {
        if (ConsoleDisplayActive)
        {
            _originalOutput.Write(value);
        }
        
        ProcessIncomingChar(value);
    }

    public override void Write(char[] buffer, int index, int count)
    {
        if (ConsoleDisplayActive)
        {
            _originalOutput.Write(buffer, index, count);
        }

        for (int i = index; i < index + count; i++)
        {
            ProcessIncomingChar(buffer[i]);
        }
    }
	int lineCount = 0;

    private void ProcessIncomingChar(char value)
    {

		
        if (value == '\n')
        {
            string completedLine = _lineBuffer.ToString().TrimEnd('\r');
			//var saveLB = _lineBuffer;
			

            _logQueue.Add(completedLine);
			
			/*
			lineCount ++;
			
			if (lineCount % 500 == 0)  {
				_originalOutput.Write($"lineCount.");
				_originalOutput.Flush();
			}
			*/
			
			/*
			if (trimmedLine.Contains("[SYSTEM]")) {
				_originalOutput.WriteLine("\n>>> FOUND [SYSTEM] <<<\n");
			}
			*/
			
			if (BufferContainsKeyword(_lineBuffer, this))
				{
				_lineBuffer.Clear();				
				
				/*
				// Passively catch the toggle commands when they clear the game's buffer
				if (trimmedLine.IndexOf("[SYSTEM] On", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					ConsoleDisplayActive = true;										
					_originalOutput.WriteLine("\n>>> VISIBLE CONSOLE OUTPUT ENABLED <<<\n");
					return; // Exit early so this system command isn't written to the log file
				}
				else if (trimmedLine.IndexOf("[SYSTEM] Off", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					ConsoleDisplayActive = false;										
					_originalOutput.WriteLine("\n>>> VISIBLE CONSOLE OUTPUT DISABLED (LOGGING CONTINUES) <<<\n");
					return; // Exit early
				}
				*/
				
				//string trimmedLine = completedLine;

				if (completedLine.Contains("Server got logged out of Steam") || 
					completedLine.Contains("Got logged out of Steam") || 
					completedLine.Contains("connection to Steam lost"))
				{					
					_originalOutput.WriteLine("================================================================================");
					_originalOutput.WriteLine("Steam Shutting Down! OnSteamShutdown triggered [FMB Sync/FAST Console Intercept]");
					_originalOutput.WriteLine("================================================================================");
					_originalOutput.WriteLine(_lineBuffer.ToString());
					Console.WriteLine("================================================================================");
					Console.WriteLine("Steam Shutting Down! OnSteamShutdown triggered [FMB Sync/FAST Console Intercept]");
					Console.WriteLine("================================================================================");
					Console.WriteLine(_lineBuffer.ToString());
					
					threadloadmission.OnUnhandledException(
						"Steam Shutdown (TWC)", 
						"Steam Shutting Down! OnSteamShutdown triggered [FMB Sync/FAST Console Intercept]"
					);
				}
			}
			_lineBuffer.Clear();			
						
        }		
        else
        {
            _lineBuffer.Append(value);
        }
		
    }
	
	//Superfast way to check for [SYSTEM] since this is in the main thread and runs or every single character output to console
	private static bool BufferContainsKeyword(StringBuilder sb, ConsoleInterceptor ci )
	//private bool BufferContainsKeyword(StringBuilder sb, ConsoleInterceptor ci )
    {
        int len = sb.Length;
        if (len < 20) return false;  //line always has [1823] ERROR [SYSTEM] where the # Vhas at least 1 digit, thus the whole thing is far > 19 length
		//int searchDist = 30.Clamp(0,len);
		//if (len <searchDist) searchDist = len;
		len = len.Clamp(0,18); //the first ERROR Is always within the first 13 chars or so but 18 for safety [12343234] ERROR [SYSTEM] = 16 chars

        for (int i = 0; i < len - 5; i++) //len -5 because "ERROR" has 5 digits
        {
			//ci._originalOutput.WriteLine("\n>>> FOUND [ <<<\n");
			//Console.WriteLine("\n>>> FOUND [ <<<\n");
            char c = sb[i];
            
            // Safe bounds validation before reading offsets
            if (c == 'E') 
            {
				//ci._originalOutput.WriteLine("\n>>> FOUND CHAR <<<\n"); //OK don't do this (print an E to console) as it leads to stack overflow
				//Console.WriteLine("\n>>> FOUND CHAR <<<\n");
                if (sb[i+1] == 'R' && sb[i+2] == 'R' && sb[i+3] == 'O' && sb[i+4] == 'R'
					   //&& sb[i+5] == 'M'
						//&& sb[i+6] == 'M' && sb[i+7] == ']' 
						) {
					//ci._originalOutput.WriteLine("\n>>> FOUND ERROR <<<\n"); //OK don't do this either (print an E to console, or especially not the whole word ERROR as it is the word we are searching for) as it leads to stack overflow
					//Console.WriteLine("\n>>> FOUND ERROR <<<\n");
                    return true;
				}
            }
            /*else if (c == 'l' && (i + 3) < len) 
            {
                if (sb[i+1] == 'o' && sb[i+2] == 's' && sb[i+3] == 't')
                    return true;
            }*/
        }
        return false;
    }

    private void ProcessLogQueue()
    {
        foreach (string line in _logQueue.GetConsumingEnumerable())
        {
            try
            {
                DateTime now = DateTime.Now;
                int activeWeek = GetIso8601WeekOfYear(now);

                if (activeWeek != _currentWeekNumber)
                {
                    _currentWeekNumber = activeWeek;
                    if (_fileWriter != null)
                    {
                        _fileWriter.Dispose();
                    }
                    
                    _currentLogPath = GetWeeklyLogPath(now);
                    _fileWriter = new StreamWriter(_currentLogPath, true, Encoding.UTF8);
                    _fileWriter.AutoFlush = true;
                    
                    ManageOldLogFiles();
                }
				if (line == "%$#@!%$#@!") {
					_fileWriter.WriteLine();
					_fileWriter.WriteLine();
					_fileWriter.WriteLine("======================================================================");
					_fileWriter.WriteLine("======================================================================");
					_fileWriter.WriteLine(string.Format("!!!NEW LOG SESSION!!! [{0:yyyy-MM-dd HH:mm:ss}]===========================", now));
					_fileWriter.WriteLine("======================================================================");
					_fileWriter.WriteLine("======================================================================");					
					
				} else {
					_fileWriter.WriteLine(string.Format("[{0:yyyy-MM-dd HH:mm:ss}] {1}", now, line));
				}
            }
            catch { }
        }
    }

    private string GetWeeklyLogPath(DateTime date)
    {
        int week = GetIso8601WeekOfYear(date);
        return Path.Combine(_baseLogDirectory, string.Format("log_twc_Week_{0:yyyy}_{1:D2}.txt", date, week));
    }

    private static int GetIso8601WeekOfYear(DateTime time)
    {
        DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
        if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
        {
            time = time.AddDays(3);
        }
        return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(time, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }

    private void ManageOldLogFiles()
    {
        try
        {
            if (!Directory.Exists(_baseLogDirectory)) return;

            DirectoryInfo dirInfo = new DirectoryInfo(_baseLogDirectory);
            FileInfo[] files = dirInfo.GetFiles("log_twc_*.txt");
            DateTime retentionThreshold = DateTime.Now.AddDays(-30);

            foreach (FileInfo file in files)
            {
                if (file.LastWriteTime < retentionThreshold)
                {
                    file.Delete();
                }
            }
        }
        catch { }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _logQueue.CompleteAdding();
            _loggingThread.Join(2000); 
            if (_fileWriter != null)
            {
                _fileWriter.Dispose();
            }
        }
        base.Dispose(disposing);
    }
}

/*
public class TWServer : WServer
{
	public Mission mainmission;
	public ThreadLoadMission threadloadmission;
	
	public TWServer(){
		Console.WriteLine("TWServer constructed...");
	}
	
	public TWServer(Mission msn, ThreadLoadMission tlmsn )
    {
        mainmission = msn;
		threadloadmission = tlmsn;
		Console.WriteLine("TWServer constructed from TWC...");
		
			// This method intercepts the exact callback frame from the Steam API before the server prints the "[SYSTEM] Server got logged out..." string
	}
	
	//public DateTime lastOnSteamShutdown = DateTime.Now;			
	
    public override void onDisconnected()
    {
        try
        {
			// 1. Force write out simple urgent telemetry file immediately - file date will show last crash if all else fails
            System.IO.File.WriteAllText(mainmission.CLOD_PATH + mainmission.FILE_PATH + "/launchercrashes/launcher-steam-connection-died-semaphore.log", "1");
			
			//double timeSinceLastOnSteamShutdown_s = DateTime.Now.Subtract(lastOnSteamShutdown).TotalSeconds;
			//lastOnSteamShutdown = DateTime.Now;
			
			// 2. Log the issue more precisely; usually the log file is still working on a Steam disconnection so we'll see it
            Console.WriteLine("[FMB INTERCEPT WSERVER] WServer.onDisconnect triggered at {0:u}! Running emergency triage, save, etc ...", DateTime.Now);
			
			//So steam shutdown notices seem to come fast & furious for a second or two before final disconnect
			//So we could wait until say 2 notices before slamming the shutdown, but let's not.
			//if (timeSinceLastOnSteamShutdown_s > 0.5 && timeSinceLastOnSteamShutdown_s < 30) {
			//}
			
			
			// 3. Handle the issue more extensively, save needed files, softExit people rather than killing etc
			threadloadmission.OnUnhandledException(sender: "Steam Shutdown (TWSERVER)", st: "Steam Shutting Down [TWSERVER]! (maddox.steam.WClient OnSteamShutdown triggered)");			                        
            
        }
        catch (Exception ex) { Console.WriteLine("WServer onDisconnected error1: " + ex.Message); }
		try {
			base.onDisconnected();
		}
        catch (Exception ex) { Console.WriteLine("WServer onDisconnected error2: " + ex.Message); }
    }
	
				
		
		
}
*/