using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using LiteDB;

namespace Schedulr
{
    public class Database
    {
        private static string databaseName, target = "Users";

        public Database(string dbname)
        {
            databaseName = dbname;
        }

        public bool UserExists(string key)
        {
            using (var db = new LiteDatabase(databaseName))
            {
                var users = db.GetCollection<User>(target);
                bool result = users.Exists(u => u.Key == key);

                //Console.WriteLine($"User {key} exists? {result}");

                return result;
            }
        }

        public List<Session> GetUsersSessions(string key)
        {
            using (var db = new LiteDatabase(databaseName))
            {
                var users = db.GetCollection<User>(target);

                if (UserExists(key))
                {
                    User x = users.FindOne(u => u.Key == key);

                    return x.Sessions;
                }
            }

            Console.WriteLine($"Error: User {key} not found");
            return new List<Session>();
        }

        public User GetUser(string key)
        {
            using (var db = new LiteDatabase(databaseName))
            {
                var users = db.GetCollection<User>(target);
                if (UserExists(key))
                {
                    return users.FindOne(u => u.Key == key);
                }
                else
                    return null; //Throw error?
            }
        }

        public User NewUser(string newkey)
        {
            using (var db = new LiteDatabase(databaseName))
            {
                var users = db.GetCollection<User>(target);

                if(UserExists(newkey))
                {
                    Console.WriteLine($"User with key {newkey} already exists!");
                    return null;
                }

                User u = new User()
                {
                    Key = newkey,
                    Jobs = new List<Job>(),
                    Sessions = new List<Session>()
                };

                users.Insert(u);

                return u;
            }

        }

        public void AddSession(Session s, string key)
        {
            using (var db = new LiteDatabase(databaseName))
            {
                var users = db.GetCollection<User>(target);

                if (UserExists(key))
                {
                    var u = users.FindOne(x => x.Key == key);

                    if (u.Sessions.Contains(s))
                        throw new Exception("User already has this session!");

                    u.Sessions.Add(s);

                    users.Update(u);


                }
                else
                    return;

            }
        }
    }

    public class User
    {
        [BsonId]
        public string Key { get; set; }
        public List<Job> Jobs { get; set; }
        public List<Session> Sessions { get; set; }

        public override bool Equals(object obj) => ((User)obj).Key == Key;

        public override int GetHashCode()
        {
            return 990326508 + EqualityComparer<string>.Default.GetHashCode(Key);
        }
    }

    public class Job
    {
        public decimal Hourly { get; set; }
        public string Name { get; set; }
    }

    public class Session
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public decimal Wage { get; set; }
        public string Description { get; set; }

        

        public override string ToString()
        {
            return $"Work session {Start} - {End} at {Wage}. Description: {Description}.";
        }

        public string ToHtmlTableRow()
        {
            return $"<tr><td> {HoursWorked()}</td>" +
                $"<td> {Start.ToShortDateString()}</td>" +
                $"<td> {End.ToShortDateString()}</td>" +
                $"<td> {Wage} </td>" +
                $" <td><i class='fa fa-cog' aria-hidden='true'></i><i class='fa fa-times' aria-hidden='true'></i></td></tr>";
        }
        
        private double HoursWorked()
        {
            return (Start - End).TotalHours;
        }
    }

}