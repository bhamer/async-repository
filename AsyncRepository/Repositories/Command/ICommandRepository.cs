using System.Collections.Generic;

namespace AsyncRepository.Repositories.Command
{
    public interface ICommandRepository<T> where T : class
    {
        /// <summary>
        /// Finds an entity with the given primary key values.
        /// </summary>
        /// <param name="keyValues">The values of the primary key for the entity to be found.</param>
        /// <returns>The entity found, or null.</returns>
        T Find(params object[] keyValues);

        /// <summary>
        /// Adds the entity to the underlying data set.
        /// </summary>
        void Add(T entity);

        /// <summary>
        /// Adds the entities to the underlying data set.
        /// </summary>
        void AddRange(IEnumerable<T> entities);

        /// <summary>
        /// Removes the entity from the underlying data set.
        /// </summary>
        void Remove(T entity);

        /// <summary>
        /// Removes the entities from the underlying data set.
        /// </summary>
        void RemoveRange(IEnumerable<T> entities);

        /// <summary>
        /// Updates the entity in the underlying data set.
        /// </summary>
        void Update(T entity);

        /// <summary>
        /// Updates the entities in the underlying data set.
        /// </summary>
        void UpdateRange(IEnumerable<T> entities);
    }
}
