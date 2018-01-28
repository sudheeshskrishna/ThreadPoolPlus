using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Threading;

namespace Sud.Threading
{
    public class ThreadPoolLite<TParam> : ThreadPoolLite<TParam, TParam>
        where TParam : IComparable
    {
        public ThreadPoolLite() : base()
        {

        }
    }

    public class ThreadPoolLite<TParam, TReturn>
        where TParam : IComparable
        where TReturn : IComparable
    {
        public static int DEFAULT_POOL_TIME_OUT = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["DEFAULT_MIN_THREADS"]);
        public static int DEFAULT_MIN_THREADS = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["DEFAULT_MIN_THREADS"]);
        public static int DEFAULT_MAX_THREADS = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["DEFAULT_MAX_THREADS"]);
        public static int DEFAULT_TIME_OUT_WAIT_PERIOD = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["DEFAULT_TIME_OUT_WAIT_PERIOD"]);
        int noOfThreads = 0;
        volatile int noOfThreadsCreated = 0;
        int minThreads = DEFAULT_MIN_THREADS > 0 ? DEFAULT_MIN_THREADS : 1;
        int maxThreads = DEFAULT_MAX_THREADS > 0 ? DEFAULT_MAX_THREADS : 4;
        internal volatile static bool _continueRunning;
        volatile bool _isRunning;
        volatile bool _isExplicitlyStopped;
        volatile Timer poolTimer;
        int _poolTimeOut = DEFAULT_POOL_TIME_OUT > 0 ? DEFAULT_POOL_TIME_OUT : 60000;
        //static long worksLeft = 100;
        //static long worksLeftPool = 100;

        public ThreadPoolLite()
        {
            _continueRunning = true;
            _isRunning = true;
            startTimer();
            startWork();
        }

        public ThreadPoolLite(int MinThreads, int MaxThreads)
        {
            _continueRunning = true;
            _isRunning = true;
            this.minThreads = MinThreads;
            this.maxThreads = MaxThreads;
            startTimer();
            startWork();
        }

        public ThreadPoolLite(int MinThreads, int MaxThreads, int poolTimeOut)
        {
            _continueRunning = true;
            _isRunning = true;
            this.minThreads = MinThreads;
            this.maxThreads = MaxThreads;
            this._poolTimeOut = poolTimeOut;
            startTimer();
            startWork();
        }

        public BlockingCollection<BaseJob<TParam>> basejobs = new BlockingCollection<BaseJob<TParam>>();

        public virtual void QueueWork(BaseWorkItem<TParam> cb, TParam ob)
        {
            if (cb != null)
            {
                if (!_isRunning)
                {
                    _isRunning = true;
                    startTimer();
                    startWork();
                }
                BaseJob<TParam> newJob = new BaseJob<TParam>(cb, ob);
                basejobs.Add(newJob);
                //return newJob.PromiseHandler;
            }
            //else
            // return null;
        }

        private void startWork()
        {
            if (_continueRunning && !_isExplicitlyStopped && _isRunning)
            {
                while (noOfThreads < minThreads)
                {
                    Thread newThread = new Thread(takeUpWork);
                    //noOfThreads++;
                    noOfThreadsCreated++;
                    Interlocked.Increment(ref noOfThreads);
                    newThread.Start();
                }
            }
        }

        public virtual void takeUpWork()
        {
            ////Console.WriteLine("thread id :" + Thread.CurrentThread.ManagedThreadId + "base pool start .worksleft:" + basejobs.Count + ". no of threads : " + noOfThreads);


            while (_continueRunning && !_isExplicitlyStopped && _isRunning && takeUpWorkIfRequried(basejobs, this.takeUpWork, minThreads, maxThreads, ref noOfThreads))
            {
                ////Console.WriteLine("sud pool 2 takeupwork : true. worksleft:" + jobs.Count + ". no of threads : " + noOfThreads + ". created : " + noOfThreadsCreated + ". " + DateTime.Now.ToString("dd-MMM-yy hh:mm:ss fff"));

                doWork();

            }

            Interlocked.Decrement(ref noOfThreads);
            ////Console.WriteLine("base pool exiting. worksleft:" + basejobs.Count + ". no of threads : " + noOfThreads + ". created : " + noOfThreadsCreated + ". " + DateTime.Now.ToString("dd-MMM-yy hh:mm:ss fff"));
        }

        public virtual void doWork()
        {
            BaseJob<TParam> jobTobeDone = basejobs.Take();
            try
            {
                jobTobeDone.WorkItem(jobTobeDone.param);
            }
            catch (Exception ex)
            {
                //jobTobeDone.promise.Reject(ex);
            }
        }

        protected bool takeUpWorkIfRequried(int minThreads, int maxThreads, ref int noOfThreads)
        {
            if (basejobs.Count > 0 || noOfThreads <= minThreads)
            {
                ////Console.WriteLine("no of threads : "+ noOfThreads+", work lft : "+ basejobs.Count);
                if ((noOfThreads < minThreads) || (noOfThreads < maxThreads && (basejobs.Count / noOfThreads) > 1))
                {
                    //noOfThreads++;
                    noOfThreadsCreated++;
                    Interlocked.Increment(ref noOfThreads);
                    Thread newThread = new Thread(takeUpWork);
                    //newThread.Priority = ThreadPriority.Highest;
                    newThread.Start();
                }
                if (basejobs.Count > minThreads || noOfThreads <= minThreads || ((basejobs.Count / (noOfThreads - 1)) >= 1))
                    return true;
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private void createThreadIfRequired(ref int noOfThreads)
        {
            if ((noOfThreads < minThreads) || (noOfThreads < maxThreads && (basejobs.Count / noOfThreads) > 1))
            {
                //noOfThreads++;
                noOfThreadsCreated++;
                Interlocked.Increment(ref noOfThreads);
                Thread newThread = new Thread(takeUpWork);
                //newThread.Priority = ThreadPriority.Highest;
                newThread.Start();
            }
        }

        private void startTimer()
        {
            if (poolTimer == null)
                poolTimer = new Timer(checkRunningStatus, null, 5000, _poolTimeOut);
            else
                poolTimer.Change(5000, _poolTimeOut);
            //poolTimer.InitializeLifetimeService();

        }

        private void checkRunningStatus(object ob)
        {
            if (basejobs.Count == 0)
            {
                BaseJob<TParam> job = null;
                basejobs.TryTake(out job, DEFAULT_TIME_OUT_WAIT_PERIOD);
                if (job != null)
                {
                    if (!_isRunning)
                    {
                        _isRunning = true;
                        startWork();
                    }
                    basejobs.Add(job);
                }
                else
                {
                    if (!_isRunning)
                    {
                        //Console.WriteLine("base pool : Kill timer. isRunning : " + _isRunning);
                        poolTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    }
                    else
                    {
                        _isRunning = false;
                        //Console.WriteLine("base pool timeout. isRunning : " + _isRunning);
                        for (int i = 0; i < minThreads; i++)
                        {
                            basejobs.Add(new BaseJob<TParam>(dummyJob, default(TParam)));
                        }
                    }
                }

            }



        }

        private void dummyJob(TParam param)
        {
            //Console.WriteLine("base pool : do dummy work");
            Thread.Sleep(500);
            //return default(TParam);
        }

        protected bool takeUpWorkIfRequried(ICollection jobsQueue, ThreadStart threadWork, int minThreads, int maxThreads, ref int noOfThreads)
        {
            if (jobsQueue.Count > 0 || noOfThreads <= minThreads)
            {
                ////Console.WriteLine("no of threads : "+ noOfThreads+", work lft : "+ basejobs.Count);
                if ((noOfThreads < minThreads) || (noOfThreads < maxThreads && (jobsQueue.Count / noOfThreads) > 1))
                {
                    //noOfThreads++;
                    noOfThreadsCreated++;
                    Interlocked.Increment(ref noOfThreads);
                    Thread newThread = new Thread(threadWork);
                    //newThread.Priority = ThreadPriority.Highest;
                    newThread.Start();
                }
                if (jobsQueue.Count > minThreads || noOfThreads <= minThreads || ((jobsQueue.Count / (noOfThreads - 1)) >= 1))
                    return true;
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public static void KillAllJobs()
        {
            _continueRunning = false;
        }

        public void KillJobs()
        {
            _isRunning = false;
            _isExplicitlyStopped = true;
            //Console.WriteLine("basepool : kill jobs");
        }

    }
}
