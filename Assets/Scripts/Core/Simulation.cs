using System;
using System.Collections.Generic;
using UnityEngine;


namespace MatchingCards.Core
{
    /// <summary>
    /// The Simulation class implements the discrete event simulator pattern.
    /// Events are pooled, with a default capacity of 4 instances.
    /// </summary>
    public static partial class Simulation
    {

        static HeapQueue<Event> _eventQueue = new HeapQueue<Event>();
        static Dictionary<System.Type, Stack<Event>> _eventPools = new Dictionary<System.Type, Stack<Event>>();

        /// <summary>
        /// Create a new event of type T and return it, but do not schedule it.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        static public T New<T>() where T : Event, new()
        {
            Stack<Event> pool;
            if (!_eventPools.TryGetValue(typeof(T), out pool))
            {
                pool = new Stack<Event>(4);
                pool.Push(new T());
                _eventPools[typeof(T)] = pool;
            }
            if (pool.Count > 0)
                return (T)pool.Pop();
            else
                return new T();
        }

        /// <summary>
        /// Clear all pending events.
        /// </summary>
        public static void Clear()
        {
            _eventQueue.Clear();
        }

        /// <summary>
        /// Schedule an event for a future tick, and return it.
        /// </summary>
        /// <returns>The event.</returns>
        /// <param name="tick">Tick.</param>
        /// <typeparam name="T">The event type parameter.</typeparam>
        static public T Schedule<T>(float tick = 0) where T : Event, new()
        {
            var ev = New<T>();
            ev.Tick = Time.time + tick;
            _eventQueue.Push(ev);
            return ev;
        }

        /// <summary>
        /// Reschedule an existing event for a future tick, and return it.
        /// </summary>
        /// <returns>The event.</returns>
        /// <param name="tick">Tick.</param>
        /// <typeparam name="T">The event type parameter.</typeparam>
        static public T Reschedule<T>(T ev, float tick) where T : Event, new()
        {
            ev.Tick = Time.time + tick;
            _eventQueue.Push(ev);
            return ev;
        }

        /// <summary>
        /// Return the simulation model instance for a class.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        static public T GetModel<T>() where T : class, new()
        {
            return InstanceRegister<T>.Instance;
        }

        /// <summary>
        /// Set a simulation model instance for a class.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        static public void SetModel<T>(T instance) where T : class, new()
        {
            InstanceRegister<T>.Instance = instance;
        }

        /// <summary>
        /// Destroy the simulation model instance for a class.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        static public void DestroyModel<T>() where T : class, new()
        {
            InstanceRegister<T>.Instance = null;
        }

        /// <summary>
        /// Tick the simulation. Returns the count of remaining events.
        /// If remaining events is zero, the simulation is finished unless events are
        /// injected from an external system via a Schedule() call.
        /// </summary>
        /// <returns></returns>
        static public int Tick()
        {
            var time = Time.time;
            var executedEventCount = 0;
            while (_eventQueue.Count > 0 && _eventQueue.Peek().Tick <= time)
            {
                var ev = _eventQueue.Pop();
                var tick = ev.Tick;
                ev.ExecuteEvent();
                if (ev.Tick > tick)
                {
                    //event was rescheduled, so do not return it to the pool.
                }
                else
                {
                    // Debug.Log($"<color=green>{ev.tick} {ev.GetType().Name}</color>");
                    ev.Cleanup();
                    try
                    {
                        _eventPools[ev.GetType()].Push(ev);
                    }
                    catch (KeyNotFoundException)
                    {
                        //This really should never happen inside a production build.
                        Debug.LogError($"No Pool for: {ev.GetType()}");
                    }
                }
                executedEventCount++;
            }
            return _eventQueue.Count;
        }
    }
}


