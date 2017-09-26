﻿using System;
using System.Data;
using System.Globalization;
using System.IO;
using Microsoft.AspNetCore.Http;
using RedHttpServerCore;
using RedHttpServerCore.Plugins;
using RedHttpServerCore.Plugins.Interfaces;
using RedHttpServerCore.Response;
//using StockManager;
using Rosenbjerg.SessionManager;
using System.Linq;
using System.Threading.Tasks;

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
            var sessionManager = new SessionManager<SessionData>(new TimeSpan(12, 0, 0), "localhost", secure: false);

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
                var pass1 = x["password1"][0];
                var pass2 = x["password2"][0];

                if (!db.Register(username, pass1, pass2))
                {
                    await res.SendString("Oh boy, somebody already used this key!", status: 400);
                }
                else
                {
                    await res.SendString("Welcome to Schedulr!");
                }

            });

            server.Post("/submitnewjob", async (req, res) =>
            {
                if (!sessionManager.TryAuthenticateToken(req.Cookies["token"], out SessionData sd))
                {
                    await res.SendString("FAIL");
                    return;
                }

                var x = await req.GetFormDataAsync();

                if (!db.AddJob(x, sd))
                {
                    await res.SendString("FAIL");
                    return;
                }
                await res.SendString("OK");

            });

            server.Get("/user", async (req, res) =>
            {
                if (sessionManager.TryAuthenticateToken(req.Cookies["token"], out SessionData sd))
                {
                    await res.SendJson(db.GetUser(sd.Username));
                }
                else
                {
                    await res.SendString("Please login first", status: 401);
                }
            });

            server.Get("/sessions", async (req, res) =>
            {
                if (sessionManager.TryAuthenticateToken(req.Cookies["token"], out SessionData sd))
                {
                    var q = req.Queries;
                    var a = db.GetUsersSessions(sd.Username, q);
                    await res.SendJson(a);
                    return;
                }
                await res.SendString("\"[]\"", contentType: "text/json");
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
                        var cookie = sessionManager.OpenSession(new SessionData(form["username"][0]));
                        res.AddHeader("Set-Cookie", cookie);
                        await res.SendString("Sucess!");
                        return;
                    }
                }
                // Just to annoy people who want to try many passwords fast
                await Task.Delay(350);
                await res.SendString("No user found with that username or password, sorry!", status: 401);
            });

            server.Post("/submittime", async (req, res) =>
            {
                if (sessionManager.TryAuthenticateToken(req.Cookies["token"], out SessionData sd))
                {
                    var form = await req.GetFormDataAsync();

                    //TODO Better input validation please
                    if (!DateTime.TryParse(form["start-time"][0], out var date) || !double.TryParse(form["duration"][0], NumberStyles.Number, CultureInfo.InvariantCulture, out double duration))
                    {
                        await res.SendString("FAIL");
                        return;
                    }
                    var desc = "";
                    if (form.ContainsKey("desc"))
                        desc = form["desc"][0];

                    User u = db.GetUser(sd.Username);
                    Job j = u.Jobs.FirstOrDefault(b => b.Name == form["job"][0]);


                    var session = new Session
                    {
                        Id = Guid.NewGuid().ToString("N").Substring(8),
                        JobId = j.Id,
                        Job = j.Name,
                        Username = sd.Username,
                        StartDate = date,
                        EndDate = date.AddHours(duration),
                    };

                    session.Earned = Database.ProcessSession(session, j);

                    var sess = db.AddSession(session, j);

                    await res.SendJson(sess);
                }
                else
                {
                    await res.SendString("/login", status: 403);
                }
            });

            server.Post("/deletesession", async (req, res) =>
            {
                if (sessionManager.TryAuthenticateToken(req.Cookies["token"], out SessionData sd))
                {
                    var form = await req.GetFormDataAsync();


                    if (form.ContainsKey("deleteTarget") 
                    && db.DeleteSession(form["deleteTarget"], sd.Username))
                    {
                        await res.SendString("Sucess");
                        return;
                    }

                    await res.SendString("Error", status: 403);
                }
                else
                {
                    await res.SendString("Error, user not logged in", status: 401);
                }

            });




            server.Start();
            while (true)
            {
                Console.ReadLine();
            }
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
