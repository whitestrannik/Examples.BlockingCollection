using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Examples.BlockingCollection.Tests
{
    [TestClass]
    public class SimpleBlockingCollectionFixture
    {
        [TestMethod]
        public void WhenThereIsNoItem_TakeShouldBeBlocked()
        {
            using (var sut = new SimpleBlockingCollection<int>())
            {
                ConcurrentAssert.EnsureThatActionIsNeverCompleted(() =>
                {
                    var a = sut.Take();
                });
            }
        }

        [TestMethod]
        public void WhenThereIsItem_TakeShouldNotBeBlocked()
        {
            using (var sut = new SimpleBlockingCollection<int>())
            {
                sut.Add(10);

                ConcurrentAssert.EnsureThatActionIsCompleted(() =>
                {
                    Assert.AreEqual(sut.Take(), 10);
                });
            }
        }

        [TestMethod]
        public void WhenThereIsWaitedConsumer_ItShouldWakeupAfterAddingNewElement()
        {
            using (var sut = new SimpleBlockingCollection<int>())
            {
                var task = Task.Run(() =>
                {
                    return sut.Take();
                });

                ConcurrentAssert.EnsureThatTaskIsNeverCompleted(task);

                sut.Add(10);

                Assert.AreEqual(task.Result, 10);
            }
        }

        [TestMethod]
        public void WhenItemsCountIsEqualToBound_AddShouldBeBlocked()
        {
            using (var sut = new SimpleBlockingCollection<int>(1))
            {
                sut.Add(10);

                ConcurrentAssert.EnsureThatActionIsNeverCompleted(() =>
                {
                    sut.Add(20);
                });
            }
        }

        [TestMethod]
        public void WhenCollectionIsCompleted_AddingAttemptShouldRaiseException()
        {
            using (var sut = new SimpleBlockingCollection<int>())
            {
                sut.CompleteAdding();

                Assert.ThrowsException<InvalidOperationException>(() => sut.Add(10));
            }
        }

        [TestMethod]
        public void WhenCollectionIsCompletedAndThereAreItems_ConsumerShouldGetAllItems()
        {
            using (var sut = new SimpleBlockingCollection<int>())
            {
                sut.Add(10);
                sut.Add(20);
                sut.Add(30);

                sut.CompleteAdding();

                Assert.AreEqual(sut.Take(), 10);
                Assert.AreEqual(sut.Take(), 20);
                Assert.AreEqual(sut.Take(), 30);
            }
        }

        [TestMethod]
        public void WhenCollectionIsCompletedAndThereAreNoItems_TakingAttemptShouldRaiseException()
        {
            using (var sut = new SimpleBlockingCollection<int>())
            {
                sut.CompleteAdding();

                Assert.ThrowsException<InvalidOperationException>(() => sut.Take());
            }
        }

        [TestMethod]
        public void WhenCollectionIsCompleted_WaitedConsumersShouldBeCancelled()
        {
            using (var sut = new SimpleBlockingCollection<int>())
            {
                var consumer1 = Task.Run(() => sut.Take());
                var consumer2 = Task.Run(() => sut.Take());

                ConcurrentAssert.EnsureThatTaskIsNeverCompleted(Task.WhenAny(consumer1, consumer2));

                sut.CompleteAdding();

                Assert.ThrowsExceptionAsync<OperationCanceledException>(() => consumer1);
                Assert.ThrowsExceptionAsync<OperationCanceledException>(() => consumer2);
            }
        }

        [TestMethod]
        public void WhenCollectionIsCompleted_WaitedProducersShouldBeCancelled()
        {
            using (var sut = new SimpleBlockingCollection<int>(1))
            {
                sut.Add(10);

                var producer1 = Task.Run(() => sut.Add(20));
                var producer2 = Task.Run(() => sut.Add(30));

                ConcurrentAssert.EnsureThatTaskIsNeverCompleted(Task.WhenAny(producer1, producer2));

                sut.CompleteAdding();

                Assert.ThrowsExceptionAsync<OperationCanceledException>(() => producer1);
                Assert.ThrowsExceptionAsync<OperationCanceledException>(() => producer2);
            }
        }

        [TestMethod]
        public void WhenCollectionHasItems_ConsumerEnumerableShouldGetAllItemsAndBlocked()
        {
            using (var sut = new SimpleBlockingCollection<int>())
            {
                sut.Add(10);
                sut.Add(20);

                using (var enumerator = sut.GetConsumingEnumerable().GetEnumerator())
                {
                    enumerator.MoveNext();
                    Assert.AreEqual(enumerator.Current, 10);
                    enumerator.MoveNext();
                    Assert.AreEqual(enumerator.Current, 20);

                    ConcurrentAssert.EnsureThatActionIsNeverCompleted(() => enumerator.MoveNext());
                }
            }
        }

        [TestMethod]
        public void WhenCollectionHasItems_ConsumerEnumerableShouldGetAllItemsEvenIfCompleted()
        {
            using (var sut = new SimpleBlockingCollection<int>())
            {
                sut.Add(10);
                sut.Add(20);

                using (var enumerator = sut.GetConsumingEnumerable().GetEnumerator())
                {
                    enumerator.MoveNext();
                    Assert.AreEqual(enumerator.Current, 10);

                    sut.CompleteAdding();

                    enumerator.MoveNext();
                    Assert.AreEqual(enumerator.Current, 20);

                    Assert.IsFalse(enumerator.MoveNext());
                }
            }
        }
    }
}
