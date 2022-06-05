using ComputerUtils.Encryption;
using ComputerUtils.FileManaging;
using ComputerUtils.Logging;
using ComputerUtils.Webserver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Cards_against_humanity
{
    public class MongoDBInteractor
    {
        public static MongoClient mongoClient = null;
        public static IMongoDatabase zwietrachtDatabase = null;
        public static IMongoCollection<UserInfo> userCollection = null;
        public static IMongoCollection<CardSet> setsCollection = null;
        public static Config config
        { 
            get
            {
                return CAHEnvironment.config;
            }
        }

        public static void Init()
        {
            RemoveIdRemap<User>();
            RemoveIdRemap<UserInfo>();

            mongoClient = new MongoClient(CAHEnvironment.config.mongoDBUrl);
            zwietrachtDatabase = mongoClient.GetDatabase(CAHEnvironment.config.mongoDBName);
            userCollection = zwietrachtDatabase.GetCollection<UserInfo>("users");
            setsCollection = zwietrachtDatabase.GetCollection<CardSet>("cardSets");
        }

        public static bool DoesUserExist(string username)
        {
            return userCollection.Find(x => x.nickname == username).Any();
        }

        public static UserInfo GetUserInfo(string nickname)
        {
            return userCollection.Find(x => x.nickname == nickname).FirstOrDefault();
        }

        public static User GetUser(string nickname)
        {
            return ObjectConverter.ConvertCopy<User, UserInfo>(userCollection.Find(x => x.nickname == nickname).FirstOrDefault());
        }

        public static void UpdateUser(UserInfo u)
        {
            userCollection.ReplaceOne(x => x.idLong == u.idLong, u);
        }

        public static UserInfo GetUserInfoById(string id)
        {
            Logger.Log(id, LoggingType.Important);
            return userCollection.Find(x => x.id == id).FirstOrDefault();
        }

        public static User GetUserByToken(string token)
        {
            string sha256 = Hasher.GetSHA256OfString(token);
            return ObjectConverter.ConvertCopy<User, UserInfo>(userCollection.Find(x => x.currentTokenSHA256 == sha256).FirstOrDefault());
        }

        public static void AddUser(UserInfo user)
        {
            userCollection.InsertOne(user);
        }

        public static List<CardSet> GetSetsOfUser(string nickname)
        {
            return setsCollection.Find(new BsonDocument("editors", new BsonDocument("$elemMatch", new BsonDocument("nickname", nickname)))).ToList();
        }

        public static List<CardSet> GetMyCardSets(string token)
        {
            User u = GetUserByToken(token);
            if (u == null) return new List<CardSet>();
            return GetSetsOfUser(u.nickname);
        }

        public static List<CardSet> GetAllSets()
        {
            return setsCollection.Find(x => true).ToList();
        }

        public static void UpdateSet(CardSet set)
        {
            setsCollection.ReplaceOne(x => x.name == set.name, set);
        }

        public static CardSet GetCardSet(string name)
        {
            return setsCollection.Find(x => x.name.ToLower() == name.ToLower()).FirstOrDefault();
        }

        public static void RemoveIdRemap<T>()
        {
            BsonClassMap.RegisterClassMap<T>(cm =>
            {
                cm.AutoMap();
                if (typeof(T).GetMember("id").Length > 0)
                {
                    Logger.Log("Unmapping reassignment for " + typeof(T).Name + " id -> _id");
                    cm.UnmapProperty("id");
                    cm.MapMember(typeof(T).GetMember("id")[0])
                        .SetElementName("id")
                        .SetOrder(0) //specific to your needs
                        .SetIsRequired(true); // again specific to your needs
                }
            });
        }

        public static void AddSet(CardSet set)
        {
            if (setsCollection.Find(x => x.name.ToLower() == set.name.ToLower()).Any()) return;
            setsCollection.InsertOne(set);
        }
    }
}
