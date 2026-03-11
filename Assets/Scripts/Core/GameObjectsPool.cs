using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MatchingCards.Core
{
    public abstract class GameObjectsPool<TPo> : MonoBehaviour where TPo : MonoBehaviour
    {
        protected TPo Prefab;
        private Stack<TPo> _pool;
        private int _poolLimit;

        protected void InitPool(TPo prefab, int count)
        {
            Prefab = prefab;
            _poolLimit = count;
            _pool = new Stack<TPo>(count);

            for (int i = 0; i < count; i++)
            {
                var instance = CreateInstance();
                AddItemToPool(instance);
            }
        }

        public TPo CreateInstance()
        {
            TPo instance = Instantiate<TPo>(Prefab, transform);
            return instance;
        }

        public TPo GetItem()
        {
            if (!HasItemInPool) return null;
            
            var item = _pool.Pop();
            item.gameObject.SetActive(true);
            return item;
        }

        public int AddItemToPool(TPo item)
        {
            (item as IPoolItem).ReturnToPool();
            _pool.Push(item);
            return _pool.Count;
        }

        public bool HasItemInPool => _pool.Count > 0;
    }
    
    public interface IPoolItem
    {
        void ReturnToPool();
    }
}