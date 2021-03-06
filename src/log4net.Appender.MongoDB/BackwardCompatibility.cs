﻿using System;
using System.Collections;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using log4net.Core;
using log4net.Util;
using Microsoft.Extensions.Configuration;

namespace log4net.Appender.MongoDB
{
	public class BackwardCompatibility
	{
       
        public static IMongoDatabase GetDatabase(MongoDBAppender appender)
		{
          
            var mongoUrl = MongoUrl.Create(appender.ConnectionString);
            var client = new MongoClient(mongoUrl);
			return client.GetDatabase(mongoUrl.DatabaseName ?? "log4net_mongodb");
        }

		public static BsonDocument BuildBsonDocument(LoggingEvent loggingEvent)
		{
			if(loggingEvent == null)
			{
				return null;
			}

			var toReturn = new BsonDocument();
			toReturn["timestamp"] = loggingEvent.TimeStamp;
			toReturn["level"] = loggingEvent.Level.ToString();
			toReturn["thread"] = loggingEvent.ThreadName;
			toReturn["userName"] = loggingEvent.UserName;
			toReturn["message"] = loggingEvent.RenderedMessage;
			toReturn["loggerName"] = loggingEvent.LoggerName;
			toReturn["domain"] = loggingEvent.Domain;
			toReturn["machineName"] = Environment.MachineName;

			// location information, if available
			if(loggingEvent.LocationInformation != null)
			{
				toReturn["fileName"] = loggingEvent.LocationInformation.FileName;
				toReturn["method"] = loggingEvent.LocationInformation.MethodName;
				toReturn["lineNumber"] = loggingEvent.LocationInformation.LineNumber;
				toReturn["className"] = loggingEvent.LocationInformation.ClassName;
			}

			// exception information
			if(loggingEvent.ExceptionObject != null)
			{
				toReturn["exception"] = BuildExceptionBsonDocument(loggingEvent.ExceptionObject);
			}

			// properties
			PropertiesDictionary compositeProperties = loggingEvent.GetProperties();
			if(compositeProperties != null && compositeProperties.Count > 0)
			{
				var properties = new BsonDocument();
				foreach(DictionaryEntry entry in compositeProperties)
				{
					properties[entry.Key.ToString()] = entry.Value.ToString();
				}

				toReturn["properties"] = properties;
			}

			return toReturn;
		}

		private static BsonDocument BuildExceptionBsonDocument(Exception ex)
		{
			var toReturn = new BsonDocument();
			toReturn["message"] = ex.Message;
			toReturn["source"] = ex.Source ?? string.Empty;
			toReturn["stackTrace"] = ex.StackTrace ?? string.Empty;

			if(ex.InnerException != null)
			{
				toReturn["innerException"] = BuildExceptionBsonDocument(ex.InnerException);
			}

			return toReturn;
		}
	}
}