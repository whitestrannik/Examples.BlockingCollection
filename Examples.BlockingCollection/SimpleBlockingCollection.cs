using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Examples.BlockingCollection
{
    public sealed class SimpleBlockingCollection<T> : IEnumerable<T>, IReadOnlyCollection<T>, IDisposable
    {
        private readonly IProducerConsumerCollection<T> _producerConsumer;
        private readonly int _boundedCapacity;
        private readonly SemaphoreSlim _takeItemSemaphore;
        private readonly SemaphoreSlim _addItemSemaphore;
        private readonly CancellationTokenSource _addItemdCts;
        private readonly CancellationTokenSource _takeItemdCts;

        private volatile bool _isCompleteAdding;

        public SimpleBlockingCollection() : this(new ConcurrentQueue<T>(), 0)
        {
        }

        public SimpleBlockingCollection(int boundedCapacity) : this(new ConcurrentQueue<T>(), boundedCapacity)
        {
        }

        public SimpleBlockingCollection(IProducerConsumerCollection<T> producerConsumer, int boundedCapacity)
        {
            _producerConsumer = producerConsumer;
            _boundedCapacity = boundedCapacity;
    
            _takeItemdCts = new CancellationTokenSource();
            _takeItemSemaphore = new SemaphoreSlim(0);

            if (_boundedCapacity != 0)
            {
                _addItemdCts = new CancellationTokenSource();
                _addItemSemaphore = new SemaphoreSlim(_boundedCapacity, _boundedCapacity);
            }
        }

        public int Count => _producerConsumer.Count;

        public void Add(T item)
        {
            if (_isCompleteAdding)
            {
                throw new InvalidOperationException("Collection is completed.");
            }

            _addItemSemaphore?.Wait(_addItemdCts.Token);

            if (!_producerConsumer.TryAdd(item))
            {
                throw new InvalidOperationException("Can not add item to the collection.");
            }

            _takeItemSemaphore.Release();
        }

        public T Take()
        {
            if (IsCompletedAndEmpty)
            {
                throw new InvalidOperationException("Collection is completed.");
            }

            _takeItemSemaphore.Wait(_takeItemdCts.Token);

            T item;
            if (!_producerConsumer.TryTake(out item))
            {
                throw new InvalidOperationException("Can not take item from the collection.");
            }

            // To free all waited for taking threads 
            if (IsCompletedAndEmpty)
            {
                _takeItemdCts.Cancel();
            }

            _addItemSemaphore?.Release();

            return item;
        }

        public void CompleteAdding()
        {
            if (_isCompleteAdding)
            {
                return;
            }

            _isCompleteAdding = true;
            _addItemdCts?.Cancel();

            if (IsCompletedAndEmpty)
            {
                _takeItemdCts.Cancel();
            }
        }

        public void Dispose()
        {
            _takeItemSemaphore.Dispose();
            _addItemSemaphore?.Dispose();
            _takeItemdCts.Dispose();
            _addItemdCts?.Dispose();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _producerConsumer.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _producerConsumer.GetEnumerator();
        }

        public IEnumerable<T> GetConsumingEnumerable()
        {
            while (!IsCompletedAndEmpty)
            {
                yield return Take();
            }
        }

        private bool IsCompletedAndEmpty => _isCompleteAdding && _producerConsumer.Count == 0;
    }
}
