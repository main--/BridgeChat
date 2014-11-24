using System;
using System.Collections.Generic;

namespace BridgeChat.Core
{
    public class CopyOnWriteArrayList<T> : IList<T>
    {
        private volatile T[] Underlying = new T[0];

        public T this[int index] {
            get { return Underlying[index]; }
            set {
                lock (this) {
                    var newone = new T[Underlying.Length];
                    CopyTo(newone, 0);
                    newone[index] = value;
                    Underlying = newone;
                }
            }
        }

        #region Write operations
        public bool Remove(T item)
        {
            lock (this) {
                var index = IndexOf(item);
                if (index < 0)
                    return false;
                RemoveAt(index);
                return true;
            }
        }

        public void Insert(int index, T item)
        {
            lock (this) {
                var newone = new T[Underlying.Length + 1];
                if (index > 0)
                    Array.Copy(Underlying, 0, newone, 0, index);
                newone[index] = item;
                if ((Underlying.Length - index) > 0)
                    Array.Copy(Underlying, index, newone, index + 1, Underlying.Length - index);
                Underlying = newone;
            }
        }

        public void RemoveAt(int index)
        {
            lock (this) {
                var newone = new T[Underlying.Length - 1];
                if (index > 0)
                    Array.Copy(Underlying, 0, newone, 0, index);
                if ((Underlying.Length - index - 1) > 0)
                    Array.Copy(Underlying, index + 1, newone, index, Underlying.Length - index - 1);
                Underlying = newone;
            }
        }

        public void Add(T item)
        {
            lock (this) {
                var newone = new T[Underlying.Length + 1];
                CopyTo(newone, 0);
                newone[Underlying.Length] = item;
                Underlying = newone;
            }
        }

        public void Clear()
        {
            lock (this)
                Underlying = new T[0];
        }
        #endregion

        #region Read operations
        public int IndexOf(T item)
        {
            return Array.IndexOf(Underlying, item);
        }

        public bool Contains(T item)
        {
            return IndexOf(item) >= 0;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            var mine = Underlying;
            if (mine.Length > 0)
                Array.Copy(mine, 0, array, arrayIndex, mine.Length);
        }

        public int Count { get{ return Underlying.Length; } }
        public bool IsReadOnly { get { return false; } }

        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }
        #endregion

        private class Enumerator : IEnumerator<T>
        {
            private readonly CopyOnWriteArrayList<T> Parent;
            private T[] CurrentArray;
            private int CurrentIndex;

            public Enumerator(CopyOnWriteArrayList<T> parent)
            {
                Parent = parent;
                CurrentArray = null;
                CurrentIndex = 0;
            }

            bool System.Collections.IEnumerator.MoveNext()
            {
                if (CurrentArray == null) {
                    CurrentArray = Parent.Underlying;
                    CurrentIndex = -1;
                }

                return ++CurrentIndex < CurrentArray.Length;
            }

            void System.Collections.IEnumerator.Reset()
            {
                CurrentArray = null;
                CurrentIndex = 0;
            }

            object System.Collections.IEnumerator.Current { get { return Current; } }

            void IDisposable.Dispose()
            {
                CurrentArray = null; // be nice to the GC
            }

            public T Current { get { return CurrentArray[CurrentIndex]; } }
        }
    }
}

