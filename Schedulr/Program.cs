using System;
using System.Data;
using System.Globalization;
using System.IO;
using Microsoft.AspNetCore.Http;
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

            server.Get("/register", async (req, res) =>
            {
                await res.SendFile("Frontend/newuser.html");
            });

            server.Post("/register", async (req, res) =>
            {
                var x = await req.GetFormDataAsync();

                var username = x["username"][0];
                var hash = BCrypt.Net.BCrypt.HashPassword(x["password"][0]);

                var usr = db.NewUser(username, hash);

                if(usr == null)
                {
                    await res.SendString("Oh boy, somebody already used this key!", status:400);
                }
                else
                {
                    await res.SendString("Welcome to Schedulr!");
                }

            });
            
            server.Post("/submitnewjob", async (req, res) =>
            {
                if (!sessionManager.TryAuthenticateRequest(req, res, out SessionData sd, false))
                {
                    await res.SendString("FAIL");
                    return;
                }

                var x = await req.GetFormDataAsync();

                if(!db.AddJob(x, sd))
                {
                    await res.SendString("FAIL");
                    return;
                }
                    

            });
            
            server.Get("/user", async (req, res) =>
            {
                if (sessionManager.TryAuthenticateRequest(req, res, out SessionData sd, false))
                {
                    await res.SendJson(db.GetUser(sd.Username));
                }
                else
                {
                    await res.SendString("Please login first", status: 401);
                }
            });

            server.Post("/login", async (req, res) =>
            {
                var x = await req.GetFormDataAsync();


                if(db.CorrectPassword(x["username"][0], x["password"][0]))
                {
                    var cookie = sessionManager.OpenSession(new SessionData(x["username"][0]));
                    res.AddHeader("Set-Cookie", cookie);

                    await res.Redirect("/");
                }
                else
                {
                    await res.SendString("No user found with that username or password, sorry!", status: 401);
                }
            });
            
            
            server.Post("/submittime", async (req, res) =>
            {

                if (sessionManager.TryAuthenticateRequest(req, res, out SessionData sd, false))
                {
                    var x = await req.GetFormDataAsync();

                    if (!DateTime.TryParse(x["start-time"][0], out var date) ||
                        !double.TryParse(x["duration"][0], NumberStyles.Number, CultureInfo.InvariantCulture,
                            out double duration) || !int.TryParse(x["wage"][0], out int wage))
                    {
                        await res.SendString("FAIL");
                        return;
                    }
                    
                    var session = new Session
                    {
                        Id = Guid.NewGuid().ToString("N").Substring(8),
                        Username = sd.Username,
                        Start = date,
                        End = date.AddHours(duration),
                        Earned = wage
                    };
                    
                    await res.SendJson(db.AddSession(session));
                }

                await res.SendString("FAIL");
            });


            

            server.Start();
            Console.Read();
            //while (true)
            //{
            //    //Console.Read();
            //}
        }
    }
}
