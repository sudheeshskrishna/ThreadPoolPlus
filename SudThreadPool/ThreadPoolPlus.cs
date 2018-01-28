using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Threading;

namespace Sud.Threading
{
    public class ThreadPoolPlus<TParam> : ThreadPoolPlus<TParam, TParam>
        where TParam : IComparable
    {
        public ThreadPoolPlus() : base()
        {

        }

        public ThreadPoolPlus(int MinThreads, int MaxThreads):base(MinThreads, MaxThreads)
        {
                   
        }

        public ThreadPoolPlus(int MinThreads, int MaxThreads, int minBaseThreads, int maxBaseThreads, int timeOut) : base(MinThreads, MaxThreads, minBaseThreads, maxBaseThreads, timeOut)
        {
            
        }
    }

    public class ThreadPoolPlus<TParam, TReturn> : ThreadPoolLite<TReturn, TReturn>
        where TParam : IComparable
        where TReturn : IComparable
    {
        //static int DEFAULT_POOL_TIME_OUT = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["DEFAULT_MIN_THREADS"]);
        //static int DEFAULT_MIN_THREADS = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["DEFAULT_MIN_THREADS"]);
        //static int DEFAULT_MAX_THREADS = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["DEFAULT_MAX_THREADS"]);
        //static int DEFAULT_TIME_OUT_WAIT_PERIOD = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["DEFAULT_TIME_OUT_WAIT_PERIOD"]);
        int noOfThreads = 0;
        volatile int noOfThreadsCreated = 0;
        int minThreads = DEFAULT_MIN_THREADS>0? DEFAULT_MIN_THREADS:1;
        int maxThreads = DEFAULT_MAX_THREADS > 0 ? DEFAULT_MAX_THREADS : 4;
        //static long worksLeft = 100;
        //static long worksLeftPool = 100;

        volatile bool _isRunning;
        volatile bool _isExplicitlyStopped;
        volatile Timer poolTimer;
        int _poolTimeOut = DEFAULT_POOL_TIME_OUT > 0 ? DEFAULT_POOL_TIME_OUT : 60000;

        public ThreadPoolPlus() : base()
        {
            _isRunning = true;
            startTimer();
            startWork();
        }

        public ThreadPoolPlus(int MinThreads, int MaxThreads) : base()
        {
            _isRunning = true;
            this.minThreads = MinThreads;
            this.maxThreads = MaxThreads;
            startTimer();
            startWork();
        }

        public ThreadPoolPlus(int MinThreads, int MaxThreads,int minBaseThreads, int maxBaseThreads, int timeOut) : base(minBaseThreads, maxBaseThreads, timeOut+1000)
        {
            _isRunning = true;
            this.minThreads = MinThreads;
            this.maxThreads = MaxThreads;
            this._poolTimeOut = timeOut;
            startTimer();
            startWork();
        }

        public BlockingCollection<Jobs<TParam, TReturn>> jobs = new BlockingCollection<Jobs<TParam, TReturn>>();

        public PromiseHandler<TReturn> QueueWork(PoolWorkItem<TParam, TReturn> cb, TParam ob)
        {
            if (cb != null)
            {
                if (!_isExplicitlyStopped)
                {
                    if (!_isRunning)
                    {
                        _isRunning = true;
                        startTimer();
                        startWork();
                    }
                    Jobs<TParam, TReturn> newJob = new Jobs<TParam, TReturn>(cb, ob);
                    jobs.Add(newJob);
                    return newJob.PromiseHandler;
                }
                else
                    return new PromiseHandler<TReturn>(new Promise<TReturn>());
            }
            else
                return null;
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
                    //Console.WriteLine("sud pool 2 start new thread id : " + newThread.ManagedThreadId + ". worksleft:" + jobs.Count + ". no of threads : "
                     //      + noOfThreads + ". created : " + noOfThreadsCreated + ". " + DateTime.Now.ToString("dd-MMM-yy hh:mm:ss fff"));
                    newThread.Start();
                }
            }
        }

        public new void takeUpWork()
        {
          
            while (_continueRunning && !_isExplicitlyStopped && _isRunning && takeUpWorkIfRequried(jobs,this.takeUpWork, minThreads, maxThreads, ref noOfThreads))
            {
                ////Console.WriteLine("sud pool 2 takeupwork : true. worksleft:" + jobs.Count + ". no of threads : " + noOfThreads + ". created : " + noOfThreadsCreated + ". " + DateTime.Now.ToString("dd-MMM-yy hh:mm:ss fff"));
               
                doWork();
                
            }

            Interlocked.Decrement(ref noOfThreads);          
            
        }

        public new void doWork()
        {
            ////Console.WriteLine("thread id :" + Thread.CurrentThread.ManagedThreadId + "sud pool start .worksleft:" + jobs.Count + ". no of threads : " + noOfThreads);
            Jobs<TParam, TReturn> jobTobeDone = jobs.Take();
            ////Console.WriteLine("thread id :" + Thread.CurrentThread.ManagedThreadId + "sud pool took job .worksleft:" + jobs.Count + ". no of threads : " + noOfThreads);
            try
            {
                base.QueueWork(jobTobeDone.promise.Resolve, jobTobeDone.WorkItem(jobTobeDone.param));                
            }
            catch (Exception ex)
            {
                jobTobeDone.promise.Reject(ex);
            }
        }        

        //protected bool TakeUpWorkIfRequried(int minThreads, int maxThreads, ref int noOfThreads)
        //{
        //    if (jobs.Count > 0 || noOfThreads <= minThreads)
        //    {
        //        ////Console.WriteLine("no of threads : "+ noOfThreads+", work lft : "+ jobs.Count);
        //        if ((noOfThreads < minThreads) || (noOfThreads < maxThreads && (jobs.Count / noOfThreads) > 1))
        //        {
        //            //noOfThreads++;
        //            noOfThreadsCreated++;
        //            Interlocked.Increment(ref noOfThreads);
        //            Thread newThread = new Thread(takeUpWork);
        //            //Console.WriteLine("sud pool 2 start new thread id : " + newThread.ManagedThreadId + ". worksleft:" + jobs.Count + ". no of threads : "
        //                + noOfThreads + ". created : " + noOfThreadsCreated + ". " + DateTime.Now.ToString("dd-MMM-yy hh:mm:ss fff"));
        //            //newThread.Priority = ThreadPriority.Highest;
        //            newThread.Start();
        //        }
        //        if (jobs.Count > minThreads || noOfThreads <= minThreads || ((jobs.Count / (noOfThreads - 1)) >= 1))
        //        {                   
        //            return true;
        //        }
        //        else
        //        {
        //            return false;                   
        //        }
        //    }
        //    else
        //    {
        //        return false;
        //    }
        //}

        private void createThreadIfRequired(ref int noOfThreads)
        {
            if ((noOfThreads < minThreads) || (noOfThreads < maxThreads && (jobs.Count / noOfThreads) > 1))
            {               
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
            if (jobs.Count == 0)
            {               
                Jobs<TParam,TReturn> job = null;
                jobs.TryTake(out job, DEFAULT_TIME_OUT_WAIT_PERIOD);
                if (job != null)
                {
                    if (!_isRunning)
                    {
                        _isRunning = true;
                        startWork();
                    }
                    jobs.Add(job);
                }
                else
                {
                    if (!_isRunning)
                    {
                        //Console.WriteLine("sud pool : Kill timer. isRunning : " + _isRunning);
                        poolTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    }
                    else
                    {
                        _isRunning = false;
                        //Console.WriteLine("sud pool timeout. isRunning : " + _isRunning);
                        for (int i = 0; i < minThreads; i++)
                        {
                            jobs.Add(new Jobs<TParam,TReturn>(dummyJob, default(TParam)));
                        }
                    }
                }

            }



        }

        private TReturn dummyJob(TParam param)
        {
            //Console.WriteLine("sud pool : do dummy work");
            Thread.Sleep(500);
            return default(TReturn);
        }

        public void KillJobs()
        {
            _isRunning = false;
            _isExplicitlyStopped = true;
            //Console.WriteLine("sudpool : kill jobs");
        }

    }
}
