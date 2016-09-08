using System;
using System.Collections.Generic;
using System.Data.Entity;

namespace AsyncRepository.Repositories.Command
{
    // EF6 implemenation of ICommandRepository
    public class EFCommandRepository<T> : ICommandRepository<T> where T : class
    {
        private readonly DbContext dbContext;
        private readonly DbSet<T> dbSet;

        public EFCommandRepository(DbContext dbContext)
        {
            this.dbContext = dbContext;
            dbSet = dbContext.Set<T>();
        }

        public virtual T Find(params object[] keyValues)
        {
            var entity = dbSet.Find(keyValues);
            if (entity != null)
            {
                // detaching the entity so calling Attach(entity) in the methods below won't throw duplicate key exceptions
                // for example, using Find() to check for the existance of an entity and then calling Update() on the entity returned from Find()
                dbContext.Entry(entity).State = EntityState.Detached;
            }
            return entity;
        }

        public virtual void Add(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            dbSet.Add(entity);
        }

        public virtual void AddRange(IEnumerable<T> entities)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            dbSet.AddRange(entities);
        }

        public virtual void Remove(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            dbSet.Attach(entity);
            dbSet.Remove(entity);
        }

        public virtual void RemoveRange(IEnumerable<T> entities)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            try
            {
                dbContext.Configuration.AutoDetectChangesEnabled = false;
                foreach (var e in entities)
                {
                    Remove(e);
                }
            }
            finally
            {
                dbContext.Configuration.AutoDetectChangesEnabled = true;
            }
        }

        public void Update(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            dbSet.Attach(entity);
            dbContext.Entry(entity).State = EntityState.Modified;
        }

        public void UpdateRange(IEnumerable<T> entities)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            try
            {
                dbContext.Configuration.AutoDetectChangesEnabled = false;
                foreach (var e in entities)
                {
                    Update(e);
                }
            }
            finally
            {
                dbContext.Configuration.AutoDetectChangesEnabled = true;
            }
        }
    }
}
