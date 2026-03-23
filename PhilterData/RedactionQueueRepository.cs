using LiteDB;

namespace PhilterData
{
    /// <summary>
    /// Repository for managing application settings in LiteDB.
    /// </summary>
    public class RedactionQueueRepository : LiteDbRepository<RedactionQueueEntity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsRepository"/> class with a shared database.
        /// </summary>
        /// <param name="database">The shared LiteDatabase instance.</param>
        public RedactionQueueRepository(LiteDatabase database) : base(database, "redaction_queue")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsRepository"/> class with a database path.
        /// </summary>
        /// <param name="databasePath">The path to the LiteDB database file.</param>
        public RedactionQueueRepository(string databasePath) : base(databasePath, "redaction_queue")
        {
        }

    }
}