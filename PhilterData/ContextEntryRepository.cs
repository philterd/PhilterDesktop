using LiteDB;

namespace PhilterData
{
    /// <summary>
    /// Repository for managing <see cref="ContextEntryEntity"/> instances in LiteDB.
    /// Includes indexes on Context field for efficient filtering.
    /// </summary>
    public sealed class ContextEntryRepository : LiteDbRepository<ContextEntryEntity>
    {
        public ContextEntryRepository(LiteDatabase database) 
            : base(database, "contextentries")
        {
        }

        protected override void ConfigureIndexes()
        {
            // Index on Context for fast lookup by context name
            EnsureIndex(x => x.Context, unique: false);
            
            // Index on Token for fast lookup
            EnsureIndex(x => x.Token, unique: false);
        }

        /// <summary>
        /// Finds all context entries for a given context name.
        /// </summary>
        public IEnumerable<ContextEntryEntity> FindByContext(string contextName)
        {
            ArgumentException.ThrowIfNullOrEmpty(contextName);
            return Find(x => x.Context == contextName);
        }

        /// <summary>
        /// Deletes all context entries for a given context name.
        /// </summary>
        /// <param name="contextName">The name of the context to empty.</param>
        /// <returns>The number of entries deleted.</returns>
        public int DeleteAllByContext(string contextName)
        {
            ArgumentException.ThrowIfNullOrEmpty(contextName);
            return DeleteWhere(x => x.Context == contextName);
        }

        /// <summary>
        /// Counts the number of entries for a given context name.
        /// </summary>
        public int CountByContext(string contextName)
        {
            ArgumentException.ThrowIfNullOrEmpty(contextName);
            return Find(x => x.Context == contextName).Count();
        }
    }
}