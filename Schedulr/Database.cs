using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography.X509Certificates;
using LiteDB;
using System.Linq;
using BCrypt;
using Microsoft.AspNetCore.Http;
using RedHttpServerCore.Plugins.Interfaces;
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

        public bool Login(string usr, string pwd)
        {
            var user = _users.FindOne(u => u.Username == usr);
            if (user == null)
                return false;
            if (!BCrypt.Net.BCrypt.Verify(pwd, user.Password))
                return false;
            return true;
        }
        public bool Register(string usr, string pwd, string pwd2)
        {
            if (pwd != pwd2)
                return false;
            if (_users.Exists(u => u.Username == usr))
                return false;
            var user = new User
            {
                Username = usr,
                Password = BCrypt.Net.BCrypt.HashPassword(pwd)
            };
            _users.Insert(user);
            return true;

        }

        public bool UserExists(string username)
        {
            return _users.Exists(u => u.Username == username);
        }

        public string GetPassword(string username)
        {

            return _users.FindOne(u => u.Username == username).Password;
        }

        public List<Session> GetUsersSessions(string username, IQueryCollection q)
        {
            var job = q.ContainsKey("job") ? q["job"][0] : "";
            var start = q.ContainsKey("start") ? q["start"][0] : "";
            var end = q.ContainsKey("end") ? q["end"][0] : "";
            var hours = q.ContainsKey("hours") ? q["hours"][0] : "";
            Query query = Query.EQ(nameof(Session.Username), username);
            if (job != "")
            {
                var jobid = _users.FindById(username).Jobs.FirstOrDefault(j => j.Name == job).Id;
                query = Query.And(query, Query.EQ(nameof(Session.JobId), jobid));
            }

            if(start != "")
            {
                query = Query.And(query, Query.GTE(nameof(Session.StartDate), start));
            }

            if(end != "")
            {
                query = Query.And(query, Query.LTE(nameof(Session.EndDate), end));
            }
            
            if(hours != "")
            {
                query = Query.And(query, Query.GTE(nameof(Session.Hours), hours));
            }

            return _sessions.Find(query, limit: 50).ToList();
        }

        public User GetUser(string username)
        {
            return _users.FindById(username);
        }

        public Session AddSession(Session s, Job j)
        {
            _sessions.Insert(s);

            ProcessSession(s , j);

            return s;
        }

        public static decimal ProcessSession(Session session, Job job)
        {
            decimal earned = (decimal) session.Hours * job.Hourly;
            foreach (var rule in job.Rules)
            {
                var timespan = TimeSpacCalculator.GetTimeSpanIntersect(session, rule.Start, rule.End);
                var time = (int) timespan.TotalMinutes / 4;

                switch (rule.RuleType)
                {
                    case Rule.Type.Percentage:
                        earned -= (time * job.Hourly);
                        earned += (time * (job.Hourly / 100) * rule.Value);
                        break;
                    case Rule.Type.Extra:
                       earned += (time * rule.Value/4);
                        break;
                    case Rule.Type.Wage:
                        earned -= (time * job.Hourly);
                        earned += (time * rule.Value);
                        break;
                    default:
                        break;
                }
            }

            return earned;
        }

        /// <summary>
        /// Attempts to delete session. Won't if the username doesn't match the one on the session.
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="username"></param>
        /// <returns>False if session cannot be deleted, true otherwise.</returns>
        public bool DeleteSession(string sessionId, string username)
        {
            var session = _sessions.FindById(sessionId);

            if(session != null && session.Username == username)
            {
                _sessions.Delete(sessionId);
                return true;
            }

            return false;
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

        public void UpdateUser(User u)
        {
            _users.Update(u);
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

    public class Rule : TimeRange
    {
        public Rule(string name, TimeSpan start, TimeSpan end) : base(name, start, end)
        {
            Name = name;
            Start = start;
            End = end;
        }

        public Rule() : base("Hejj :(", DateTime.MinValue.TimeOfDay, DateTime.MinValue.TimeOfDay)
        {

        }

        public enum Type { Percentage, Extra, Wage }
        public Rule.Type RuleType { get; set; }
        public decimal Value { get; set; }
    }

    public class Session : TimeShift
    {
        public Session(DateTime start, DateTime end) : base(start.TimeOfDay, end.TimeOfDay)
        {
            
        }
        
        public Session() : base(DateTime.MinValue.TimeOfDay, DateTime.MinValue.TimeOfDay)
        { }

        [BsonId]
        public string Id { get; set; }
        public decimal Earned { get; set; }
        public string JobId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public double Hours
        {
            get
            {
                return (EndDate - StartDate).TotalHours;
            }
        }

        [JsonIgnore]
        public override TimeSpan Start => StartDate.TimeOfDay;
        [JsonIgnore]
        public override TimeSpan End => EndDate.TimeOfDay;
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

    }

}