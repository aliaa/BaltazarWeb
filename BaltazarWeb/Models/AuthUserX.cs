using AliaaCommon.Models;
using AliaaCommon.MongoDB;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Models
{
    [AliaaCommon.CollectionOptions(Name = nameof(AuthUser))]
    public class AuthUserX : AuthUser
    {
        [BsonRepresentation(MongoDB.Bson.BsonType.String)]
        public List<Permission> Permissions { get; set; } = new List<Permission>();
    }

    public static class AuthUserXDBExtention
    {
        public static AuthUserX CheckAuthentication(this MongoHelper DB, string username, string password, bool passwordIsHashed = false)
        {
            string hash;
            if (passwordIsHashed)
                hash = password;
            else
                hash = AuthUserDBExtention.GetHash(password);
            return DB.Find<AuthUserX>(u => u.Username == username && u.HashedPassword == hash && u.Disabled != true).FirstOrDefault();
        }
    }
}
