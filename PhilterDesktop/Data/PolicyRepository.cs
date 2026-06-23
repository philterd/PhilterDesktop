using LiteDB;

namespace PhilterData
{
    /// <summary>
    /// Repository for managing <see cref="PolicyEntity"/> instances in LiteDB.
    /// Includes indexes on Name and CreatedAt fields.
    /// </summary>
    public sealed class PolicyRepository : LiteDbRepository<PolicyEntity>
    {
        public PolicyRepository(LiteDatabase database) 
            : base(database, "policies")
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
        /// Finds a policy by its exact name.
        /// </summary>
        public PolicyEntity? FindByName(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            return FindOne(x => x.Name == name);
        }

        /// <summary>
        /// Returns all policies ordered by creation date (newest first).
        /// </summary>
        public IEnumerable<PolicyEntity> GetAllOrderedByDate()
        {
            return GetAll().OrderByDescending(x => x.CreatedAt);
        }
    }
}