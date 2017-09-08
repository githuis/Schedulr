using System;
using System.IO;
using RedHttpServerCore;
using RedHttpServerCore.Plugins;
using RedHttpServerCore.Plugins.Interfaces;
using RedHttpServerCore.Response;

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

            // We log to terminal here
            var logger = new TerminalLogging();
            server.Plugins.Register<ILogging, TerminalLogging>(logger);

            server.Get("/hey", async (req, res) =>
            {
                db.NewUser("hashboi");
                var sess = db.GetUsersSessions("hashboi");
                
                Console.WriteLine(sess.Count);
                

                foreach (var sesh in sess)
                {
                    Console.WriteLine(sesh.ToString());
                }


                await res.SendString("Hi my dude");
            });

            server.Get("/newses", async (req, res) =>
            {
                Session s = new Session()
                {
                    Start = DateTime.Now,
                    End = DateTime.Today,
                    Wage = 100
                };
                db.AddSession(s, "hashboi");
                
                await res.SendString("Added session ");
            });

            server.Get("/register", async (req, res) =>
            {
                var files = Directory.GetFiles(".");
                foreach (var file in files)
                {
                    Console.WriteLine(file.ToString());
                }
                await res.SendFile("Frontend/newuser.html");
            });

            server.Post("/registered", async (req, res) =>
            {
                var x = await req.GetFormDataAsync();
                
                await res.SendString($"You registered as {x.TryGetValue("key").ToString()}");
            });


            // URL param demo
            server.Get("/:param1/:paramtwo/:somethingthird", async (req, res) =>
            {
                await res.SendString($"URL: {req.Params["param1"]} / {req.Params["paramtwo"]} / {req.Params["somethingthird"]}");
            });

            // Redirect to page on same host
            server.Get("/redirect", async (req, res) =>
            {
                await res.Redirect("/redirect/test/here");
            });

            // Save uploaded file from request body 
            Directory.CreateDirectory("./uploads");
            server.Post("/upload", async (req, res) =>
            {
                if (await req.SaveBodyToFile("./uploads"))
                {
                    await res.SendString("OK");
                    // We can use logger reference directly
                    logger.Log("UPL", "File uploaded");
                }
                else
                    await res.SendString("Error", status: 413);
            });

            server.Get("/file", async (req, res) =>
            {
                await res.SendFile("testimg.jpeg");
            });

            // Using url queries to generate an answer
            server.Get("/hello", async (req, res) =>
            {
                var queries = req.Queries;
                var firstname = queries["firstname"];
                var lastname = queries["lastname"];
                await res.SendString($"Hello {firstname} {lastname}, have a nice day");
            });

            // Rendering a page for dynamic content
            server.Get("/serverstatus", async (req, res) =>
            {
                await res.RenderPage("./pages/statuspage.ecs", new RenderParams
                {
                    { "uptime", DateTime.UtcNow.Subtract(startTime).TotalHours },
                    { "versiom", RedHttpServer.Version }
                });
            });
            
            

            server.Start();
            
            while (true)
            {
                Console.Read();
            }
        }
    }
}