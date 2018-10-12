using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using log4net.Core;
using Microsoft.Extensions.Configuration;

namespace log4net.Appender.MongoDB
{
	public class MongoDBAppender : AppenderSkeleton
    {
        private readonly IConfiguration _configuration;
        public MongoDBAppender()
        { }

        //public MongoDBAppender(IConfiguration configuration)
        //{
        //    this._configuration = configuration;

        //    if (string.IsNullOrEmpty(connectionStringName))
        //    {
        //        this.ConnectionString = _configuration[connectionStringName];
        //    }
        //}

        private readonly List<MongoAppenderFileld> _fields = new List<MongoAppenderFileld>();

		/// <summary>
		/// MongoDB database connection in the format:
		/// mongodb://[username:password@]host1[:port1][,host2[:port2],...[,hostN[:portN]]][/[database][?options]]
		/// See http://www.mongodb.org/display/DOCS/Connections
		/// If no database specified, default to "log4net"
		/// </summary>
		public string ConnectionString { get; set; }


        public string connectionStringName { get; set; }


        /// <summary>
        /// Name of the collection in database
        /// Defaults to "logs"
        /// </summary>
        public string CollectionName { get; set; }

		#region Deprecated

        /// <summary>
        /// Hostname of MongoDB server
		/// Defaults to localhost
        /// </summary>
		[Obsolete("Use ConnectionString")]
		public string Host { get; set; }

        /// <summary>
        /// Port of MongoDB server
		/// Defaults to 27017
        /// </summary>
		[Obsolete("Use ConnectionString")]
		public int Port { get; set; }

        /// <summary>
        /// Name of the database on MongoDB
		/// Defaults to log4net_mongodb
        /// </summary>
		[Obsolete("Use ConnectionString")]
		public string DatabaseName { get; set; }

        /// <summary>
        /// MongoDB database user name
        /// </summary>
		[Obsolete("Use ConnectionString")]
        public string UserName { get; set; }

        /// <summary>
        /// MongoDB database password
        /// </summary>
		[Obsolete("Use ConnectionString")]
		public string Password { get; set; }

		#endregion

		public void AddField(MongoAppenderFileld fileld)
		{
			_fields.Add(fileld);
		}

		protected override void Append(LoggingEvent loggingEvent)
		{
            
            var collection = GetCollection();
			collection.InsertOne(BuildBsonDocument(loggingEvent));
		}

		protected override void Append(LoggingEvent[] loggingEvents)
		{
			var collection = GetCollection();
            collection.InsertMany(loggingEvents.Select(a => BuildBsonDocument(a)));
		}

		private IMongoCollection<BsonDocument> GetCollection()
		{
			var db = GetDatabase();
            var collection = db.GetCollection<BsonDocument>(CollectionName ?? "logs");
			return collection;
		}

		public IMongoDatabase GetDatabase()
		{
			if(string.IsNullOrWhiteSpace(ConnectionString))
			{
				return BackwardCompatibility.GetDatabase(this);
			}
			var mongoUrl = MongoUrl.Create(ConnectionString);
            var client = new MongoClient(mongoUrl);            
			var db = client.GetDatabase(mongoUrl.DatabaseName ?? "log4net");
			return db;
		}

		private BsonDocument BuildBsonDocument(LoggingEvent log)
		{
			if(_fields.Count == 0)
			{
				return BackwardCompatibility.BuildBsonDocument(log);
			}
			var doc = new BsonDocument();
			foreach(MongoAppenderFileld field in _fields)
			{
				object value = field.Layout.Format(log);
				BsonValue bsonValue = BsonValue.Create(value);
				doc.Add(field.Name, bsonValue);
			}
			return doc;
		}
	}
}