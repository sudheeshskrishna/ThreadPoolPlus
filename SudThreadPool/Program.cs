using System;
using System.Threading;
using System.Collections.Concurrent;
using Sud.Threading;

namespace ConsoleApp1
{
    class Program
    {
        static int noOfThreads = 0;
        static int noOfThreadsCreated = 0;
        const int minThreads = 4;
        const int maxThreads = 10;
        //static long worksLeft = 100;
        //static long worksLeftPool = 100;
        static BlockingCollection<int> jobs = new BlockingCollection<int>();
        static BlockingCollection<int> poolJobs = new BlockingCollection<int>();


        static void Main(string[] args)
        {
            //System.Globalization.CultureInfo currentCulture=(System.Globalization.CultureInfo) Thread.CurrentThread.CurrentCulture.Clone();
            //currentCulture.DateTimeFormat.ShortDatePattern = "dd-MMM-yy";
            //Thread.CurrentThread.CurrentCulture = currentCulture;

            Console.WriteLine("start");
            Console.WriteLine(DateTime.Now.ToString("dd-MMM-yy hh:mm:ss fff"));
            //Console.ReadKey();

            Thread th = new Thread(testPromise);
            Thread th2 = new Thread(testPool);
            //testPromise();
            //testPool();
            th.Start();
            th2.Start();






        }

        private static void testPromise()
        {
            SudThreadPool2<Message> pool = new SudThreadPool2<Message>();
            for (int i = 0; i < 100; i++)
            {
                Message msg = new Message()
                {
                    msgId = i,
                    msg = string.Empty
                };
                if (i == 30)
                {
                    Thread.Sleep(25000);
                    //SudThreadPool2<Message>.KillAllJobs();
                    pool.KillJobs();
                }
                if (i == 51)
                {
                    Thread.Sleep(25000);
                }
                if (i == 80)
                {
                    Thread.Sleep(15000);
                }
                pool.QueueWork(heavyJob, msg)
                    .then((ob) => Console.WriteLine(ob.msg),
                    (ex)=> Console.WriteLine(ex.Message));
            }
        }

        private static void testPool()
        {
            for (int i = 0; i < 50; i++)
            {
                //jobs.Add(i);
                poolJobs.Add(i);
            }
            ThreadPool.SetMaxThreads(10, 10);
            ThreadPool.SetMinThreads(1, 1);

            //Thread th = new Thread(startWork);
            //Thread thpool = new Thread(addWorktoPool);
            ////th.Start();
            //thpool.Start();
            addWorktoPool();
        }

        private static void startWork()
        {
            while (noOfThreads < minThreads)
            {
                Thread newThread = new Thread(someWork);
                //noOfThreads++;
                noOfThreadsCreated++;
                Interlocked.Increment(ref noOfThreads);
                newThread.Start();
            }
        }

        private static void someWork()
        {
            // Console.WriteLine("thread id :" + Thread.CurrentThread.ManagedThreadId + "start .worksleft:" + worksLeft + ". no of threads : " + noOfThreads);
            jobs.Take();
            //Interlocked.Decrement(ref worksLeft);
            //worksLeft--;          

            Thread.Sleep(1000);
            if (jobs.Count > 0)
            {
                //Console.WriteLine("no of threads : "+ noOfThreads+", work lft : "+ jobs.Count);
                if ((noOfThreads < minThreads) || (noOfThreads < maxThreads && (jobs.Count / noOfThreads) > 1))
                {
                    //noOfThreads++;
                    noOfThreadsCreated++;
                    Interlocked.Increment(ref noOfThreads);
                    Thread newThread = new Thread(someWork);
                    newThread.Priority = ThreadPriority.Highest;
                    newThread.Start();
                }
                if (jobs.Count > minThreads || noOfThreads <= minThreads || ((jobs.Count / (noOfThreads - 1)) >= 1))
                    someWork();
                else
                {
                    Interlocked.Decrement(ref noOfThreads);
                    // Console.WriteLine("thread id :" + Thread.CurrentThread.ManagedThreadId + "exiting.worksleft:" + worksLeft + ". no of threads : " + noOfThreads + ". created : " + noOfThreadsCreated);
                    // Console.WriteLine(DateTime.Now.ToString());
                    // Console.ReadKey();
                }
            }
            else
            {
                //noOfThreads--;
                Interlocked.Decrement(ref noOfThreads);
                if (jobs.Count == 0)
                {
                    //Console.WriteLine("thread id :" + Thread.CurrentThread.ManagedThreadId + "exiting.worksleft:" + worksLeft + ". no of threads : " + noOfThreads + ". created : " + noOfThreadsCreated);
                    Console.WriteLine("exiting.worksleft:" + jobs.Count + ". no of threads : " + noOfThreads + ". created : " + noOfThreadsCreated + ". " + DateTime.Now.ToString("dd-MMM-yy hh:mm:ss fff"));

                    //Console.WriteLine(DateTime.Now.ToString("dd-MMM-yy hh:mm:ss fff"));
                    Console.ReadKey();
                }
            }
        }

        private static void doWorkinPool(object on)
        {
            poolJobs.Take();
            //Interlocked.Decrement(ref worksLeftPool);
            Thread.Sleep(1000);
            if (poolJobs.Count == 0)
            {
                //Console.WriteLine("thread id :" + Thread.CurrentThread.ManagedThreadId + "exiting.worksleftpool:" + worksLeftPool + DateTime.Now.ToString("dd-MMM-yy hh:mm:ss fff"));
                Console.WriteLine("exiting.worksleftpool:" + poolJobs.Count + DateTime.Now.ToString("dd-MMM-yy hh:mm:ss fff"));

                //Console.ReadKey();
            }
        }

        private static void addWorktoPool()
        {
            for (int i = 0; i < poolJobs.Count; i++)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(doWorkinPool));
            }
        }

        private static Message heavyJob(Message ob)
        {
            Console.WriteLine("Heavy Job Start");
            Thread.Sleep(1000);
            ob.msg = ob.msgId + " done.";
            return ob;
        }

        private static void OnHeavyJobSuccess(Message ob)
        {
            Console.WriteLine(ob.msg);
        }

        private static void OnHeavyJobError(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}
