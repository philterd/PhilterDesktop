using LiteDB;

namespace PhilterData
{
    /// <summary>
    /// Repository for managing <see cref="ContextEntity"/> instances in LiteDB.
    /// Includes indexes on Name and CreatedAt fields.
    /// </summary>
    public sealed class ContextRepository : LiteDbRepository<ContextEntity>
    {
        public ContextRepository(LiteDatabase database) 
            : base(database, "contexts")
        {
        }

        protected override void ConfigureIndexes()
        {
            // Index on Name for fast lookup by policy name
            EnsureIndex(x => x.Name, unique: false);
            
            // Index on CreatedAt for sorting and date-based queries
            EnsureIndex(x => x.CreatedAt, unique: false);
        }

        /// <summary>
        /// Finds a context by its exact name.
        /// </summary>
        public ContextEntity? FindByName(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            return FindOne(x => x.Name == name);
        }

        /// <summary>
        /// Returns all contexts ordered by creation date (newest first).
        /// </summary>
        public IEnumerable<ContextEntity> GetAllOrderedByDate()
        {
            return GetAll().OrderByDescending(x => x.CreatedAt);
        }
    }
}