using System;
using System.Collections.Generic;

namespace InvisibleManXRay.Foundation
{
    public class Container<T>
    {
        private readonly string tag;
        private readonly Dictionary<Type, T> entities;

        public Container(string tag)
        {
            this.tag = tag;
            this.entities = new Dictionary<Type, T>();
        }

        public void Add(T entity)
        {
            entities.Add(entity.GetType(), entity);
        }

        public TEntity Get<TEntity>() where TEntity : T
        {
            Type type = typeof(TEntity);

            if (IsExists())
                return (TEntity)entities[type];
            throw new Exception($"The {tag} of type '{type}' does not found.");

            bool IsExists() => entities.ContainsKey(type);
        }
    }
}