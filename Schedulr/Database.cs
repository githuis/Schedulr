using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using LiteDB;

namespace Schedulr
{
    public class Database
    {
        private static string databaseName, target;

        private readonly LiteCollection<User> _users;

        public Database(string dbname)
        {
            var db = new LiteDatabase(databaseName);
            _users = db.GetCollection<User>();
        }
        
        public bool UserExists(string key)
        {
           return _users.Exists(u => u.key == key);
        }

        public List<Session> GetUsersSessions(string key)
        {
            if (UserExists(key))
            {
                User x = _users.FindOne(u => u.key == key);

                return x.sessions;
            }
            
            return new List<Session>();
        }

        public User GetUser(string key)
        {
            if (UserExists(key))
            {
                return _users.FindOne(u => u.key == key);
            }
            else
                return null; //Throw error?
        }

        public User NewUser()
        {
            
        }
    }
    
    public class User
    {
        public string key { get; set; }
        public List<Job> jobs { get; set; }
        public List<Session> sessions { get; set; }


    }

    public class Job
    {
        public decimal hourly { get; set; }
        public string name { get; set; }
    }

    public class Session
    {
        public DateTime start { get; set; }
        public DateTime end { get; set; }
        public decimal wage;
    }
    
}