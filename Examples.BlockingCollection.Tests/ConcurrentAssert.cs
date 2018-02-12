using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Examples.BlockingCollection.Tests
{
    public static class ConcurrentAssert
    {
        public static void EnsureThatTaskIsNeverCompleted(Task task)
        {
            EnsureThatTaskIsNotCompletedIn(task, TimeSpan.FromMilliseconds(1500));
        }

        public static void EnsureThatTaskIsNotCompletedIn(Task task, TimeSpan timeSpan)
        {
            var threadPoolTask = Task.Run(async () => await task);
            var firstTask = Task.WhenAny(threadPoolTask, Task.Delay(timeSpan)).Result;

            if (firstTask == threadPoolTask)
            {
                throw new Exception("Neverending task is unexpectedly complited");
            }
        }

        public static void EnsureThatActionIsNeverCompleted(Action action)
        {
            EnsureThatActionIsNeverCompleted(action, TimeSpan.FromMilliseconds(1500));
        }

        public static void EnsureThatActionIsNeverCompleted(Action action, TimeSpan timeSpan)
        {
            var task = Task.Run(action);
            var firstTask = Task.WhenAny(task, Task.Delay(timeSpan)).Result;

            if (firstTask == task)
            {
                throw new Exception("Neverending action is unexpectedly complited");
            }
        }

        public static void EnsureThatTaskIsCompleted(Task task)
        {
            EnsureThatTaskIsCompleted(task, TimeSpan.FromMilliseconds(1500));
        }

        public static void EnsureThatTaskIsCompleted(Task task, TimeSpan timeSpan)
        {
            var threadPoolTask = Task.Run(async () => await task);
            var firstTask = Task.WhenAny(threadPoolTask, Task.Delay(timeSpan)).Result;

            if (firstTask != threadPoolTask)
            {
                throw new Exception("Task isn't complited in required time");
            }
        }

        public static void EnsureThatActionIsCompleted(Action action)
        {
            EnsureThatActionIsCompleted(action, TimeSpan.FromMilliseconds(1500));
        }

        public static void EnsureThatActionIsCompleted(Action action, TimeSpan timeSpan)
        {
            var task = Task.Run(action);
            var firstTask = Task.WhenAny(task, Task.Delay(timeSpan)).Result;

            if (firstTask != task)
            {
                throw new Exception("Action isn't complited in required time");
            }
        }
    }
}
