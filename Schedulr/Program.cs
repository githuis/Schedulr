using System;
using System.Data;
using System.Globalization;
using System.IO;
using Microsoft.AspNetCore.Http;
//using StockManager;
using Rosenbjerg.SessionManager;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Red;
using Red.CookieSessions;

namespace Schedulr
{   
    class Program
    {

        static async Task Main(string[] args)
        {
            // We serve static files, such as index.html from the 'public' directory
            var server = new RedHttpServer(5000, "Frontend");
            var db = new Database("WorkTimeDatabaseHashboiii.db");
            server.Use(new CookieSessions<SessionData>(new CookieSessionSettings(TimeSpan.FromDays(14))
            {
                Secure = false
            }));

            async Task Auth(Request req, Response res)
            {
                if (req.GetSession<SessionData>() == null)
                {
                    await res.SendStatus(HttpStatusCode.Unauthorized);
                }
            }
            
            server.Get("/register", async (req, res) =>
            {
                await res.SendFile("Frontend/newuser.html");
            });

            server.Post("/register", async (req, res) =>
            {
                var x = await req.GetFormDataAsync();
                var username = x["username"][0];
                var pass1 = x["password1"][0];
                var pass2 = x["password2"][0];

                if (!db.Register(username, pass1, pass2))
                {
                    await res.SendString("Oh boy, somebody already used this key!", status: HttpStatusCode.BadRequest);
                }
                else
                {
                    await res.SendString("Welcome to Schedulr!");
                }

            });

            server.Post("/submitnewjob", Auth, async (req, res) =>
            {
                var x = await req.GetFormDataAsync();
                var sd = req.GetSession<SessionData>().Data;

                if (db.AddJob(x, sd) != null)
                {
                    await res.SendString("FAIL");
                    return;
                }

                await res.SendString("OK");

            });

            server.Get("/user", Auth, async (req, res) =>
            {
                var sd = req.GetSession<SessionData>().Data;

                await res.SendJson(db.GetUser(sd.Username));
            });

            server.Get("/sessions", Auth, async (req, res) =>
            {
                var sd = req.GetSession<SessionData>().Data;

                var q = req.Queries;
                var a = db.GetUsersSessions(sd.Username, q);
                await res.SendJson(a);
            });

            server.Post("/login", async (req, res) =>
            {
                var form = await req.GetFormDataAsync();
                if (form.ContainsKey("username") && form.ContainsKey("password"))
                {
                    var username = form["username"][0];
                    var pass = form["password"][0];
                    if (db.Login(username, pass))
                    {
                        req.OpenSession(new SessionData(username));
                        await res.SendStatus(HttpStatusCode.OK);
                    }
                }
                // Just to annoy people who want to try many passwords fast
                await Task.Delay(350);
                await res.SendString("No user found with that username or password, sorry!", status: HttpStatusCode.Unauthorized);
            });

            server.Post("/submittime", Auth, async (req, res) =>
            {
                var sd = req.GetSession<SessionData>().Data;

                var form = await req.GetFormDataAsync();

                //TODO Better input validation please
                if (!ValidateAddSessionForm(form, out var job, out var start, out var end))
                {
                    await res.SendString("Failed", status: HttpStatusCode.BadRequest);
                    return;
                }
                var desc = "";
                if (form.ContainsKey("desc"))
                    desc = form["desc"][0];

                User u = db.GetUser(sd.Username);
                Job j = u.Jobs.FirstOrDefault(b => b.Name == job);
                if (j == null)
                {
                    await res.SendString("Failed", status: HttpStatusCode.BadRequest);
                    return;
                }

                var session = new Session
                {
                    Id = Guid.NewGuid().ToString("N").Substring(8),
                    JobId = j.Id,
                    Description = desc,
                    Job = job,
                    Username = sd.Username,
                    StartDate = start,
                    EndDate = end,
                };
                session.Earned = Database.ProcessSession(session, j);
                

                var sess = db.AddSession(session, j);

                await res.SendJson(sess);
            });

            server.Post("/deletesession", Auth, async (req, res) =>
            {
                var sd = req.GetSession<SessionData>().Data;

                var form = await req.GetFormDataAsync();


                if (form.ContainsKey("deleteTarget") 
                    && db.DeleteSession(form["deleteTarget"], sd.Username))
                {
                    await res.SendStatus(HttpStatusCode.OK);
                }

            });

            server.Post("/deletejob", Auth, async (req, res) =>
            {
                var sd = req.GetSession<SessionData>().Data;

                
                var form = await req.GetFormDataAsync();
                if (CheckFormContains(form, "job"))
                {
                    var user = db.GetUser(sd.Username);
                    var job = user.Jobs.FirstOrDefault(x => x.Name == form["job"][0]);

                    if (job != null)
                    {
                        user.Jobs.Remove(job);
                        db.UpdateUser(user);

                        await res.SendString("Sucess");
                    }
                    else
                    {
                        await res.SendString("Error", status: HttpStatusCode.BadRequest);
                    }
                }
                else
                {
                    await res.SendString("Error in form", status: HttpStatusCode.BadRequest);
                }
                
            });

            server.Post("/changepass", Auth, async (req, res) =>
            {
                
                var sd = req.GetSession<SessionData>().Data;

                Console.Write("Pass change request:  ");
                
                var form = await req.GetFormDataAsync();

                if (!(form.ContainsKey("oldPwd") && form.ContainsKey("newPwd") && form.ContainsKey("confPwd")))
                {
                    Console.WriteLine("Not all keys contained");
                    await res.SendString("Error, not all fields filled out", status: HttpStatusCode.BadRequest);
                    return;
                }

                var oldP = form["oldPwd"][0];
                var newP = form["newPwd"][0];
                var confP = form["confPwd"][0];

                if (!db.Login(sd.Username, oldP))
                {
                    Console.WriteLine("Wrong old password");
                    await res.SendString("Error, wrong pass", status: HttpStatusCode.BadRequest);
                }
                else if (newP != confP)
                {
                    Console.WriteLine("New passwords don't match!");
                    await res.SendString("Error, passwords don't match", status: HttpStatusCode.BadRequest);
                }

                var u = db.GetUser(sd.Username);
                u.Password = BCrypt.Net.BCrypt.HashPassword(newP);
                db.UpdateUser(u);

                await res.SendStatus(HttpStatusCode.OK);

            });
            
            server.Post("/submitmanagedjobform", Auth, async (req, res) =>
            {
                var sd = req.GetSession<SessionData>().Data;

                var form = await req.GetFormDataAsync();
                var id = form["jobid"][0];
                var title = form["title"][0];
                var wage = decimal.Parse(form["wage"][0]);
                var usr = db.GetUser(sd.Username);

                var job = usr.Jobs.First(x => x.Id == id);
                job.Name = title;
                job.Hourly = wage;

                db.UpdateUser(usr);

                await res.SendStatus(HttpStatusCode.OK);
            });
            
            server.Get("/getwage", Auth, async (req, res) =>
            {
                var sd = req.GetSession<SessionData>().Data;

                var jobname = req.Queries["job"][0];

                var job = db.GetUser(sd.Username).Jobs.First(x => x.Name == jobname);

                    

                await res.SendJson(job);
            });


            await server.RunAsync();
        }

        private static bool ValidateAddSessionForm(IFormCollection form, out string job, out DateTime startTime, out DateTime endTime)
        {
            if (CheckFormContains(form, "job") && CheckFormContains(form, "start-time") && DateTime.TryParse(form["start-time"][0], out startTime))
            {
                job = form["job"][0];
                if (CheckFormContains(form, "duration") && double.TryParse(form["duration"][0], out double duration))
                {
                    endTime = startTime.AddHours(duration);
                    return true;
                }
                if (CheckFormContains(form, "end-time") && DateTime.TryParse(form["end-time"][0], out endTime))
                    return true;
                startTime = DateTime.MinValue;
                endTime = DateTime.MinValue;
                return false;

            }
            job = "";
            startTime = DateTime.MinValue;
            endTime = DateTime.MinValue;
            return false;
        }

        private static bool CheckFormContains(IFormCollection form, string field)
        {
            return form.ContainsKey(field) && form[field][0] != "";
        }
    }

    public class SessionData
    {
        public string Username { get; set; }

        public SessionData(string username)
        {
            Username = username;
        }

        public override string ToString()
        {
            return Username;
        }

        public override int GetHashCode()
        {
            return Username.Length;
        }
    }
}
