using JeeLee.UniSignals.Delegates;
using JeeLee.UniSignals.Domain;
using JeeLee.UniSignals.Pooling;
using JeeLee.UniSignals.Subscriptions;
using System;
using System.Collections.Generic;

namespace JeeLee.UniSignals
{
    /// <summary>
    /// General signals manager class. Implements methods from `ISignalTransmitter` and `ISignalReceiver` to create general signals workflow.
    /// Also adds ISignalMuter to allow for signal handling to be paused.
    /// </summary>
    public sealed class SignalManager : ISignalTransmitter, ISignalReceiver, ISignalMuter
    {
        private readonly Dictionary<Type, IPool> _signalPools;
        private readonly Dictionary<Type, ISubscription> _signalSubscriptions;

        public SignalManager()
        {
            _signalPools = new Dictionary<Type, IPool>();
            _signalSubscriptions = new Dictionary<Type, ISubscription>();
        }

        #region ISignalTransmitter Members

        /// <summary>
        /// Sends a new instance of this signal without setting properties.
        /// </summary>
        /// <typeparam name="TSignal">The type of signal to be send.</typeparam>
        public void Send<TSignal>()
            where TSignal : Signal
        {
            TSignal signal = GetSignal<TSignal>();
            Send(signal);
        }

        /// <summary>
        /// Sends a signal of the given instance. Properties can be set before hand.
        /// </summary>
        /// <typeparam name="TSignal">The type of signal to be send.</typeparam>
        /// <param name="signal">The signal instance to be send.</param>
        public void Send<TSignal>(TSignal signal)
            where TSignal : Signal
        {
            Type type = signal.GetType();

            while (type != null)
            {
                if (typeof(ISignal).IsAssignableFrom(type) && _signalSubscriptions.TryGetValue(type, out var subscription))
                {
                    subscription.Handle(signal);
                }
                
                type = type.BaseType;
            }

            AllocatePool<TSignal>().Release(signal);
        }

        /// <summary>
        /// Allocates a signal instance of the given type.
        /// </summary>
        /// <typeparam name="TSignal">The signal type to allocate.</typeparam>
        /// <returns>An instance of the signal of the requested type.</returns>
        public TSignal GetSignal<TSignal>()
            where TSignal : Signal
        {
            return AllocatePool<TSignal>().Get();
        }

        #endregion

        #region ISignalReceiver Members

        /// <summary>
        /// Subscribe a given method to signals of this type.
        /// </summary>
        /// <typeparam name="TSignal">The signal type to subscribe to.</typeparam>
        /// <param name="handler">The method to be called when this signal is fired.</param>
        public void Subscribe<TSignal>(SignalHandler<TSignal> handler)
            where TSignal : Signal
        {
            AllocateSubscription<TSignal>().AddHandler(handler);
        }

        /// <summary>
        /// Unsubscribe a given method from signals of this type.
        /// </summary>
        /// <typeparam name="TSignal">The signal type to unscubscribe from.</typeparam>
        /// <param name="handler">The method which needs to be unsubscribed.</param>
        public void Unsubscribe<TSignal>(SignalHandler<TSignal> handler)
            where TSignal : Signal
        {
            AllocateSubscription<TSignal>().RemoveHandler(handler);
        }

        #endregion

        #region ISignalMuter Members

        /// <summary>
        /// Mutes all signal handlers from this type and prevents them from being invoked.
        /// </summary>
        /// <typeparam name="TSignal">The signal type to mute.</typeparam>
        public void Mute<TSignal>()
            where TSignal : Signal
        {
            AllocateSubscription<TSignal>().Mute();
        }

        /// <summary>
        /// Unmutes all signal handlers from this type and allows them to be invoked again.
        /// </summary>
        /// <typeparam name="TSignal">The signal type to unmute</typeparam>
        public void Unmute<TSignal>()
            where TSignal : Signal
        {
            AllocateSubscription<TSignal>().Unmute();
        }

        #endregion

        private Pool<TSignal> AllocatePool<TSignal>()
            where TSignal : Signal
        {
            if(!_signalPools.TryGetValue(typeof(TSignal), out var pool))
            {
                _signalPools.Add(typeof(TSignal), pool = new Pool<TSignal>());
            }
            
            return (Pool<TSignal>)pool;
        }

        private Subscription<TSignal> AllocateSubscription<TSignal>()
            where TSignal : Signal
        {
            if (!_signalSubscriptions.TryGetValue(typeof(TSignal), out var subscription))
            {
                _signalSubscriptions.Add(typeof(TSignal), subscription = new Subscription<TSignal>());
            }

            return (Subscription<TSignal>)subscription;
        }
    }
}
