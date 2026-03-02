using LiteDB;

namespace PhilterData
{
    /// <summary>
    /// Repository for managing application settings in LiteDB.
    /// </summary>
    public class SettingsRepository : LiteDbRepository<SettingsEntity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsRepository"/> class with a shared database.
        /// </summary>
        /// <param name="database">The shared LiteDatabase instance.</param>
        public SettingsRepository(LiteDatabase database) : base(database, "settings")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsRepository"/> class with a database path.
        /// </summary>
        /// <param name="databasePath">The path to the LiteDB database file.</param>
        public SettingsRepository(string databasePath) : base(databasePath, "settings")
        {
        }

        /// <summary>
        /// Gets the application settings. If no settings exist, creates and returns default settings.
        /// </summary>
        /// <returns>The application settings entity.</returns>
        public SettingsEntity GetSettings()
        {
            // Settings should always have ID = 1 (singleton pattern)
            var settings = GetById(1);
            
            if (settings == null)
            {
                // Create default settings
                settings = new SettingsEntity
                {
                    Id = 1,
                    OutputToOriginalLocation = true,
                    CustomOutputFolder = string.Empty,
                    LastModified = DateTime.UtcNow
                };
                Insert(settings);
            }

            return settings;
        }

        /// <summary>
        /// Saves the application settings.
        /// </summary>
        /// <param name="settings">The settings entity to save.</param>
        public void SaveSettings(SettingsEntity settings)
        {
            ArgumentNullException.ThrowIfNull(settings);
            
            settings.Id = 1; // Ensure we're always updating the singleton record
            settings.LastModified = DateTime.UtcNow;
            Upsert(settings);
        }
    }
}