using System.Security.Authentication;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace DataPersistence.NoSql
{
    /// <summary>
    /// Helper class for easier single MongoAtlas collection operations. (Although you can work with other collections
    /// by _mongoAtlasService.Database.GetCollection("other collection name"), it is not recommended.);
    /// </summary>
    /// <typeparam name="T">The type you map the deserialized data to.</typeparam>
    public class MongoAtlasService<T>
    {
        protected const string BaseConnStr = "mongodb+srv://{0}:{1}@{2}/{3}?retryWrites=true&w=majority";
        protected readonly MongoClient MongoClient;
        protected readonly string DatabaseName;
        protected readonly string CollectionName;

        public MongoAtlasService(string endpoint, string user, string password, string database, string collection)
        {
            DatabaseName = database;
            CollectionName = collection;
            MongoClient = new MongoClient(GetMongoSetting(GetMongoConnStr(endpoint, user, password, database)));
        }

        public IMongoDatabase Database => MongoClient.GetDatabase(DatabaseName);
        /// <summary>
        /// HealthCheck() should be called first to ensure it works properly
        /// </summary>
        public IMongoCollection<T> Collection => Database.GetCollection<T>(CollectionName);

        /// <summary>
        /// Check whether the default collection of this service instance should initialize.
        ///
        /// Return values meaning:
        /// 0 - Database exists & collection exists & collection is not empty, cannot init.
        /// 1 - Database not exists, can init.
        /// 2 - Collection not exists, can init.
        /// 3 - Collection is empty, can init.
        /// </summary>
        /// <returns>
        /// </returns>
        public async Task<byte> CollectionInitCheck()
        {
            var dbExists = await DatabaseExists(DatabaseName);

            if (!dbExists) return 1;

            var db = MongoClient.GetDatabase(DatabaseName);
            var collectionExists = await CollectionExists(db, CollectionName);

            if (!collectionExists)
                return 2;

            var collection = db.GetCollection<T>(CollectionName);
            var count = await collection.CountDocumentsAsync(_ => true);

            return count == 0 ? (byte) 3 : (byte) 0;
        }

        private static string GetMongoConnStr(string endpoint, string user, string password, string database)
        {
            return string.Format(BaseConnStr, user, UrlEncoder.Create().Encode(password), endpoint, database);
        }

        protected static MongoClientSettings GetMongoSetting(string connStr)
        {
            var settings = MongoClientSettings.FromUrl(new MongoUrl(connStr));
            settings.SslSettings = new SslSettings() {EnabledSslProtocols = SslProtocols.Tls12};

            return settings;
        }

        public async Task<bool> DatabaseExists(string databaseName)
        {
            var cursor = await MongoClient.ListDatabaseNamesAsync();
            var names = await cursor.ToListAsync();

            return names.Contains(databaseName);
        }
        public async Task<bool> CollectionExists(IMongoDatabase database, string collectionName)
        {
            var cursor = await database.ListCollectionNamesAsync();
            var names = await cursor.ToListAsync();

            return names.Contains(collectionName);
        }
    }
}
