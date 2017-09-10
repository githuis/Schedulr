using System;
using System.IO;
using RedHttpServerCore;
using RedHttpServerCore.Plugins;
using RedHttpServerCore.Plugins.Interfaces;
using RedHttpServerCore.Response;
using StockManager;

namespace Schedulr
{
    class Program
    {
        
        static void Main(string[] args)
        {
            // We serve static files, such as index.html from the 'public' directory
            var server = new RedHttpServer(5000, "Frontend");
            var startTime = DateTime.UtcNow;
            var db = new Database("WorkTimeDatabaseHashboiii");
            var sessionManager = new SessionManager<SessionData>(new TimeSpan(12, 0, 0), "localhost", secure: false) { TokenName = "key-token" };

            // We log to terminal here
            var logger = new TerminalLogging();
            server.Plugins.Register<ILogging, TerminalLogging>(logger);

            server.Get("/home", async (req, res) =>
            {
                if(sessionManager.TryAuthenticateRequest(req, res, out SessionData sd, false))
                {
                    RenderParams rp = new RenderParams();
                    rp.Add("userkey", sd.Key);

                    string allSessionStrings = "";
                    foreach(var sesh in db.GetUsersSessions(sd.Key))
                    {
                        allSessionStrings += sesh.ToHtmlTableRow();
                    }

                    rp.Add("usersessions", allSessionStrings);
                    
                    await res.RenderPage("Frontend/home.ecs", rp);
                }
            });

            server.Get("/register", async (req, res) =>
            {
                await res.SendFile("Frontend/newuser.html");
            });

            server.Get("/login", async (req, res) =>
            {
                SessionData sd;
                if (sessionManager.TryAuthenticateRequest(req, res, out sd, false))
                {
                    Console.WriteLine("User is logged in as: " + sd.Key);
                    await res.Redirect("/home");
                    return;
                }
                else
                    Console.WriteLine("User is not logged in already - showing login page");

                await res.SendFile("Frontend/login.html");
            });

            server.Post("/registered", async (req, res) =>
            {
                var x = await req.GetFormDataAsync();

                var userkey = x["key"][0];

                var usr = db.NewUser(userkey);

                if(usr == null)
                {
                    await res.SendString("Oh boy, somebody already used this key!");
                }
                else
                {
                    await res.SendString("Welcome to Schedulr!");
                }

            });

            server.Post("/loggedin", async (req, res) =>
            {
                var x = await req.GetFormDataAsync();

                var userkey = x["key"][0];

                if(db.UserExists(userkey))
                {
                    var cookie = sessionManager.OpenSession(new SessionData(userkey));
                    res.AddHeader("Set-Cookie", cookie);

                    await res.Redirect("/home");

                    //await res.SendString($"Welcome {userkey}, added a cookie for you!");
                }
                else
                {
                    await res.SendString("No user found with that key, sorry!");
                }
            });


            

            server.Start();
            
            while (true)
            {
                //Console.Read();
            }
        }
    }
}
