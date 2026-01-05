using System;
using System.Collections.Generic;

namespace Project.Core.Services
{
    public interface IServiceRegistry
    {
        void Register<T>(T instance) where T : class;
        T Resolve<T>() where T : class;
        bool TryResolve<T>(out T instance) where T : class;
        void Clear();
    }

    public sealed class ServiceRegistry : IServiceRegistry
    {
        private readonly Dictionary<Type, object> _services = new();

        public void Register<T>(T instance) where T : class
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            _services[typeof(T)] = instance;
        }

        public T Resolve<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var obj) && obj is T typed)
                return typed;

            throw new InvalidOperationException($"Service not registered: {typeof(T).Name}");
        }

        public bool TryResolve<T>(out T instance) where T : class
        {
            if (_services.TryGetValue(typeof(T), out var obj) && obj is T typed)
            {
                instance = typed;
                return true;
            }

            instance = null;
            return false;
        }

        public void Clear() => _services.Clear();
    }
}