using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography.X509Certificates;
using LiteDB;
using System.Linq;
using BCrypt;
using Microsoft.AspNetCore.Http;
using RedHttpServerCore.Plugins.Interfaces;
using StockManager;
using Newtonsoft.Json;

namespace Schedulr
{
    public class Database
    {
        private string databaseName;
        private const string userCollection = "Users", sessionCollection = "Sessions";
        private LiteCollection<User> _users;
        private LiteCollection<Session> _sessions;

        public Database(string dbname)
        {
            databaseName = dbname;
            var litedb = new LiteDatabase(databaseName);
            _users = litedb.GetCollection<User>(userCollection);
            _sessions = litedb.GetCollection<Session>(sessionCollection);
        }

        public bool UserExists(string username)
        {
            return _users.Exists(u => u.Username == username);
        }

        public bool CorrectPassword(string username, string password)
        {
            if (UserExists(username))
            {
                if (BCrypt.Net.BCrypt.Verify(password, GetPassword(username)))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
                return false;
        }

        public string GetPassword(string username)
        {
            if (UserExists(username))
            {
                using (var db = new LiteDatabase(databaseName))
                {
                    return db.GetCollection<User>(userCollection).FindOne(u => u.Username == username).Password;
                }
            }
            return"";
        }

        public List<Session> GetUsersSessions(string username)
        {
            return _sessions.Find(s => s.Username == username).ToList<Session>();
        }

        public User GetUser(string username)
        {
            return _users.FindById(username);
        }

        public User NewUser(string username, string password)
        {
            if (username.Length < 4 || password.Length < 4)
            {
                return null;
            }

            string hash = BCrypt.Net.BCrypt.HashPassword(password);

            var user = GetUser(username);

            if (user == null)
                return null;
            
            
            
            User u = new User()
            {
                Username = username,
                Password = hash,
            };

            _users.Insert(u);
            
            return u;
        }

        public Session AddSession(Session s)
        {
            _sessions.Insert(s);

            ProcessSession(s);

            return s;
        }

        private static void ProcessSession(Session session)
        {
            //session.hou
        }


        public void DeleteSession(int sessionId, string key)
        {
            
        }

        public bool AddJob(IFormCollection form, SessionData sd)
        {
            if (!form.ContainsKey("name") || !form.ContainsKey("wage") || !form.ContainsKey("rules"))
                return false;

            if (string.IsNullOrEmpty(form["name"][0]) || string.IsNullOrEmpty(form["wage"][0]) ||
                string.IsNullOrEmpty(form["rules"][0]))
                return false;
            
            string title = form["name"][0];
            
            

            if (!decimal.TryParse(form["wage"][0], out var wage))
                return false;

            var rules = JsonConvert.DeserializeObject<List<Rule>>(form["rules"][0]);

            if (rules == null)
                return false;



            var job = new Job()
            {
                Name = title,
                Hourly = wage,
                Id = Guid.NewGuid().ToString("N").Substring(8),
                Rules = rules
            };

            var user = GetUser(sd.Username);
            user.Jobs.Add(job);
            _users.Update(user);

            return true;
        }
    }

    public class User
    {
        [BsonId]
        public string Username { get; set; }
        [JsonIgnore]
        public string Password { get; set; }
        public List<Job> Jobs { get; set; } = new List<Job>();
    }

    public class Job
    {
        [BsonId]
        public string Id { get; set; }
        public decimal Hourly { get; set; }
        public string Name { get; set; }
        public List<Rule> Rules { get; set; } = new List<Rule>();
    }

    public class Rule
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public enum Type {Percentage, Extra, Wage}
        public Rule.Type RuleType { get; set; }
        public decimal Value { get; set; }
    }

    public class Session
    {
        [BsonId]
        public string Id { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public decimal Earned { get; set; }

        public double Hours
        {
            get
            {
                return (End - Start).TotalHours;
            }
        }

        [JsonIgnore]
        public string Description { get; set; }
        [JsonIgnore]
        public string Username { get; set; }
        [JsonIgnore]
        public string Job { get; set; }


        public override string ToString()
        {
            return $"Work session {Start} - {End} at {Earned}. Description: {Description}.";
        }

        public Session()
        {
        }
    }

}