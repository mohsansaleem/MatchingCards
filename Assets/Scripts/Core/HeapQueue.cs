using System;
using System.Collections.Generic;

namespace MatchingCards.Core
{
    /// <summary>
    /// HeapQueue provides a queue collection that is always ordered.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class HeapQueue<T> where T : IComparable<T>
    {
        List<T> _items;

        public int Count { get { return _items.Count; } }

        public bool IsEmpty { get { return _items.Count == 0; } }

        public T First { get { return _items[0]; } }

        public void Clear() => _items.Clear();

        public bool Contains(T item) => _items.Contains(item);

        public void Remove(T item)
        {
            int index = _items.IndexOf(item);
            if (index < 0) return;

            int last = _items.Count - 1;
            if (index == last)
            {
                _items.RemoveAt(last);
                return;
            }

            _items[index] = _items[last];
            _items.RemoveAt(last);
            if (index > 0 && Compare(_items[index], _items[(index - 1) >> 1]) < 0)
                SiftDown(0, index);
            else
                SiftUp(index);
        }

        public T Peek() => _items[0];

        public HeapQueue()
        {
            _items = new List<T>();
        }

        public void Push(T item)
        {
            //add item to end of tree to extend the list
            _items.Add(item);
            //find correct position for new item.
            SiftDown(0, _items.Count - 1);
        }

        public T Pop()
        {

            //if there are more than 1 items, returned item will be first in tree.
            //then, add last item to front of tree, shrink the list
            //and find correct index in tree for first item.
            T item;
            var last = _items[_items.Count - 1];
            _items.RemoveAt(_items.Count - 1);
            if (_items.Count > 0)
            {
                item = _items[0];
                _items[0] = last;
                SiftUp();
            }
            else
            {
                item = last;
            }
            return item;
        }


        int Compare(T a, T b) => a.CompareTo(b);

        void SiftDown(int startpos, int pos)
        {
            //preserve the newly added item.
            var newitem = _items[pos];
            while (pos > startpos)
            {
                //find parent index in binary tree
                var parentpos = (pos - 1) >> 1;
                var parent = _items[parentpos];
                //if new item precedes or equal to parent, pos is new item position.
                if (Compare(parent, newitem) <= 0)
                    break;
                //else move parent into pos, then repeat for grand parent.
                _items[pos] = parent;
                pos = parentpos;
            }
            _items[pos] = newitem;
        }

        void SiftUp() => SiftUp(0);

        void SiftUp(int pos)
        {
            var endpos = _items.Count;
            var startpos = pos;
            //preserve the inserted item
            var newitem = _items[pos];
            var childpos = 2 * pos + 1;
            //find child position to insert into binary tree
            while (childpos < endpos)
            {
                //get right branch
                var rightpos = childpos + 1;
                //if right branch should precede left branch, move right branch up the tree
                if (rightpos < endpos && Compare(_items[rightpos], _items[childpos]) <= 0)
                    childpos = rightpos;
                //move child up the tree
                _items[pos] = _items[childpos];
                pos = childpos;
                //move down the tree and repeat.
                childpos = 2 * pos + 1;
            }
            //the child position for the new item.
            _items[pos] = newitem;
            SiftDown(startpos, pos);
        }
    }
}