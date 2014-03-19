using System.Collections;
using System.Collections.Generic;

namespace SteamMobile
{
    // http://stackoverflow.com/a/17853085/1056845
    public class OrderedSet<T> : ICollection<T>
    {
        private readonly object _sync = new object();
        private readonly IDictionary<T, LinkedListNode<T>> _dictionary;
        private readonly LinkedList<T> _linkedList;

        public OrderedSet()
            : this(EqualityComparer<T>.Default)
        {
        }

        public OrderedSet(IEqualityComparer<T> comparer)
        {
            _dictionary = new Dictionary<T, LinkedListNode<T>>(comparer);
            _linkedList = new LinkedList<T>();
        }

        public OrderedSet(IEnumerable<T> items)
            : this()
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }

        public int Count
        {
            get
            {
                lock (_sync)
                    return _dictionary.Count;
            }
        }

        public virtual bool IsReadOnly
        {
            get
            {
                lock (_sync)
                    return _dictionary.IsReadOnly;
            }
        }

        void ICollection<T>.Add(T item)
        {
            Add(item);
        }

        public bool Add(T item)
        {
            lock (_sync)
            {
                if (_dictionary.ContainsKey(item))
                    return false;
                LinkedListNode<T> node = _linkedList.AddLast(item);
                _dictionary.Add(item, node);
                return true;
            }
        }

        public void Clear()
        {
            lock (_sync)
            {
                _linkedList.Clear();
                _dictionary.Clear();
            }
        }

        public bool Remove(T item)
        {
            lock (_sync)
            {
                LinkedListNode<T> node;
                bool found = _dictionary.TryGetValue(item, out node);
                if (!found)
                    return false;
                _dictionary.Remove(item);
                _linkedList.Remove(node);
                return true;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _linkedList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Contains(T item)
        {
            lock (_sync)
                return _dictionary.ContainsKey(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (_sync)
                _linkedList.CopyTo(array, arrayIndex);
        }
    }
}
