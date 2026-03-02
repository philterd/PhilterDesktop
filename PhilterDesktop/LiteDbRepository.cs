using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace PhilterDesktop
{
    /// <summary>
    /// Generic LiteDB repository providing CRUD operations for any entity type.
    /// The entity must have an <c>Id</c> property (or a property marked with <c>[BsonId]</c>).
    /// </summary>
    /// <typeparam name="T">The entity type stored in the collection.</typeparam>
    public sealed class LiteDbRepository<T> : IDisposable where T : new()
    {
        private readonly LiteDatabase _db;
        private readonly ILiteCollection<T> _collection;
        private bool _disposed;

        /// <param name="databasePath">File path for the LiteDB database (e.g., <c>"philter.db"</c>).</param>
        /// <param name="collectionName">
        /// Optional collection name. Defaults to the lowercase type name of <typeparamref name="T"/>.
        /// </param>
        public LiteDbRepository(string databasePath, string? collectionName = null)
        {
            ArgumentException.ThrowIfNullOrEmpty(databasePath);

            _db = new LiteDatabase(databasePath);
            _collection = _db.GetCollection<T>(collectionName ?? typeof(T).Name.ToLowerInvariant());
        }

        // ── Create ────────────────────────────────────────────────────────────────

        /// <summary>Inserts a single entity and returns its generated <see cref="BsonValue"/> id.</summary>
        public BsonValue Insert(T entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            return _collection.Insert(entity);
        }

        /// <summary>Inserts multiple entities in a single bulk operation.</summary>
        /// <returns>Number of documents inserted.</returns>
        public int InsertBulk(IEnumerable<T> entities)
        {
            ArgumentNullException.ThrowIfNull(entities);
            return _collection.InsertBulk(entities);
        }

        // ── Read ──────────────────────────────────────────────────────────────────

        /// <summary>Returns the entity with the given id, or <c>null</c> if not found.</summary>
        public T? GetById(BsonValue id) => _collection.FindById(id);

        /// <summary>Returns all entities in the collection.</summary>
        public IEnumerable<T> GetAll() => _collection.FindAll();

        /// <summary>Returns entities matching the given predicate.</summary>
        public IEnumerable<T> Find(Expression<Func<T, bool>> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            return _collection.Find(predicate);
        }

        /// <summary>Returns the first entity matching the predicate, or <c>null</c>.</summary>
        public T? FindOne(Expression<Func<T, bool>> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            return _collection.FindOne(predicate);
        }

        /// <summary>Returns the total number of entities in the collection.</summary>
        public int Count() => _collection.Count();

        // ── Update ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Updates an existing entity matched by its id.
        /// </summary>
        /// <returns><c>true</c> if the document was found and updated; otherwise <c>false</c>.</returns>
        public bool Update(T entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            return _collection.Update(entity);
        }

        /// <summary>
        /// Inserts the entity if it does not exist; updates it otherwise.
        /// </summary>
        /// <returns><c>true</c> if inserted; <c>false</c> if updated.</returns>
        public bool Upsert(T entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            return _collection.Upsert(entity);
        }

        // ── Delete ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Deletes the entity with the given id.
        /// </summary>
        /// <returns><c>true</c> if the document was found and deleted; otherwise <c>false</c>.</returns>
        public bool Delete(BsonValue id) => _collection.Delete(id);

        /// <summary>Deletes all entities matching the predicate.</summary>
        /// <returns>Number of documents deleted.</returns>
        public int DeleteWhere(Expression<Func<T, bool>> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            return _collection.DeleteMany(predicate);
        }

        /// <summary>Removes every entity from the collection.</summary>
        /// <returns>Number of documents deleted.</returns>
        public int DeleteAll() => _collection.DeleteAll();

        // ── Indexing ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Ensures an index exists on the specified field, improving query performance.
        /// </summary>
        /// <param name="field">Expression pointing to the field to index (e.g., <c>x => x.Email</c>).</param>
        /// <param name="unique">Whether the index should enforce uniqueness.</param>
        public void EnsureIndex(Expression<Func<T, object>> field, bool unique = false)
        {
            ArgumentNullException.ThrowIfNull(field);
            _collection.EnsureIndex(field, unique);
        }

        // ── IDisposable ───────────────────────────────────────────────────────────

        public void Dispose()
        {
            if (_disposed) return;
            _db.Dispose();
            _disposed = true;
        }
    }
}