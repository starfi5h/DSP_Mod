﻿using System;
using System.Reflection;
using System.Threading;
using UnityEngine;

namespace SampleAndHoldSim
{
    public class Threading
    {
        private static readonly ManualResetEvent completeEvent = new ManualResetEvent(false);

        // Modify from https://github.com/BepInEx/BepInEx/blob/0894d3552e4fdd96899d5209313ebb551ed17b62/BepInEx.Unity/ThreadingHelper.cs
        public static void ForEachParallel(Action<int> work, int dataCount, int workerCount = -1)
        {
            if (workerCount < 0)
                workerCount = Mathf.Max(2, Environment.ProcessorCount);
            else if (workerCount == 0)
                throw new ArgumentException("Need at least 1 worker", nameof(workerCount));

            var currentIndex = dataCount;

            completeEvent.Reset();
            var runningCount = workerCount;
            Exception exceptionThrown = null;

            void DoWork(object _)
            {
                try
                {
                    while (true)
                    {
                        if (exceptionThrown != null)
                            return;

                        var decrementedIndex = Interlocked.Decrement(ref currentIndex);
                        if (decrementedIndex < 0)
                            return;

                        work(decrementedIndex);
                    }
                }
                catch (Exception ex)
                {
                    exceptionThrown = ex;
                }
                finally
                {
                    var decCount = Interlocked.Decrement(ref runningCount);
                    if (decCount <= 0)
                        completeEvent.Set();
                }
            }

            // Start threads to process the data
            for (var i = 0; i < workerCount - 1; i++)
                ThreadPool.QueueUserWorkItem(DoWork);

            DoWork(null);

            completeEvent.WaitOne();

            if (exceptionThrown != null)
                throw new TargetInvocationException("An exception was thrown inside one of the threads", exceptionThrown);
        }
    }
}
