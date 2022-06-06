using ComputerUtils.Logging;
using ComputerUtils.RandomExtensions;
using ComputerUtils.Updating;
using ComputerUtils.Webserver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Cards_against_humanity
{
    public class Server
    {
        public HttpServer server = new HttpServer();
        public Config config
        {
            get
            {
                return CAHEnvironment.config;
            }
        }
        public Dictionary<string, string> replace = new Dictionary<string, string>
        {
            {"{meta}", "<meta name=\"theme-color\" content=\"#63fac3\">\n<meta property=\"og:site_name\" content=\"Cards agains ...\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n<link rel=\"stylesheet\" href=\"/style.css\"><link href=\"https://fonts.googleapis.com/css?family=Open+Sans:400,400italic,700,700italic\" rel=\"stylesheet\" type=\"text/css\">" }
        };

        public List<CardSet> cards = new List<CardSet>();

        public string GetToken(ServerRequest request, bool send403 = true)
        {
            string token = request.context.Request.Headers["token"];
            if (token == null)
            {
                token = request.queryString["token"];
                if (token == null)
                {
                    token = request.cookies["token"] == null ? "" : request.cookies["token"].Value;
                    if (token == null)
                    {
                        if (send403) request.Send403();
                        return "";
                    }
                }
            }
            return token;
        }

        public bool IsUserAdmin(ServerRequest request, bool send403 = true)
        {
            return GetToken(request, send403) == config.masterToken;
        }

        public void StartServer()
        {
            if(!File.Exists("cards.json")) 
            server = new HttpServer();
            string frontend = "frontend" + Path.DirectorySeparatorChar;
            MongoDBInteractor.Init();
            server.DefaultCacheValidityInSeconds = 0;
            server.AddRouteFile("/", frontend + "index.html", replace);
            server.AddRouteFile("/style.css", frontend + "style.css", replace);
            server.AddRouteFile("/play", frontend + "play.html", replace);
            server.AddRouteFile("/room", frontend + "room.html", replace);
            server.AddRouteFile("/create", frontend + "create.html", replace);
            server.AddRouteFile("/script.js", frontend + "script.js", replace);
            server.AddRouteFile("/login", frontend + "login.html", replace);
            server.AddWSRoute("/", new Action<SocketServerRequest>(request =>
            {
                List<string> args = request.bodyString.Split('|').ToList();
                GameManager.HandleWSConnection(request, MongoDBInteractor.GetUserByToken(args[0]), args.GetRange(1, args.Count - 1));
            }));
            server.AddRoute("GET", "/admin", new Func<ServerRequest, bool>(request =>
            {
                if (!IsUserAdmin(request)) return true;
                request.SendFile(frontend + "admin.html", replace);
                return true;
            }), true, true, true, true);
            server.AddRoute("POST", "/api/updateserver/", new Func<ServerRequest, bool>(request =>
            {
                if (!IsUserAdmin(request)) return true;
                config.Save();
                request.SendString("Starting update");
                Updater.StartUpdateNetApp(request.bodyBytes, Path.GetFileName(Assembly.GetExecutingAssembly().Location), CAHEnvironment.workingDir);
                return true;
            }));
            server.AddRoute("GET", "/randomuserId", new Func<ServerRequest, bool>(request =>
            {
                request.SendString(RandomExtension.CreateRandom(6, "0123456789ABCDEF"));
                return true;
            }));
            server.AddRoute("GET", "/randompassword", new Func<ServerRequest, bool>(request =>
            {
                request.SendString(RandomExtension.CreateRandom(20, "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!?.-"));
                return true;
            }));

            // Sets
            server.AddRoute("GET", "/api/v1/allsets", new Func<ServerRequest, bool>(request =>
            {
                User u = MongoDBInteractor.GetUserByToken(GetToken(request));
                if(u == null)
                {
                    request.Send403();
                    return true;
                }
                request.SendString(JsonSerializer.Serialize(MongoDBInteractor.GetAllSets(u)));
                return true;
            }));
            server.AddRoute("POST", "/api/v1/creategame", new Func<ServerRequest, bool>(request =>
            {
                User u = MongoDBInteractor.GetUserByToken(GetToken(request));
                if(u == null)
                {
                    request.SendString(JsonSerializer.Serialize(new CreateRoomResponse
                    {
                        success = false,
                        msg = "You are not logged in. Please login"
                    }));
                    return true;
                }
                request.SendString(JsonSerializer.Serialize(GameManager.CreateRoom(request.bodyString)));
                return true;
            }));
            server.AddRoute("POST", "/api/v1/createset", new Func<ServerRequest, bool>(request =>
            {
                User u = MongoDBInteractor.GetUserByToken(GetToken(request));
                CardSet set = JsonSerializer.Deserialize<CardSet>(request.bodyString);
                set.editors = new List<User>();
                set.editors.Add(u);
                set.owner = u;
                MongoDBInteractor.AddSet(set);
                request.SendString("created");
                return true;
            }));
            server.AddRoute("POST", "/api/v1/updateeditors", new Func<ServerRequest, bool>(request =>
            {
                User u = MongoDBInteractor.GetUserByToken(GetToken(request));
                CardSet set = JsonSerializer.Deserialize<CardSet>(request.bodyString);
                CardSet toUpdate = MongoDBInteractor.GetCardSet(set.name, u);
                if (toUpdate == null)
                {
                    request.SendString("set does not exist", "text/plain", 404);
                    return true;
                }
                if (toUpdate.owner.nickname != u.nickname)
                {
                    request.Send403();
                    return true;
                }
                toUpdate.editors = set.editors;
                toUpdate.players = set.players;
                toUpdate.isPrivate = set.isPrivate;
                MongoDBInteractor.UpdateSet(toUpdate);
                request.SendString(JsonSerializer.Serialize(toUpdate));
                return true;
            }));
            server.AddRoute("POST", "/api/v1/removefromset", new Func<ServerRequest, bool>(request =>
            {
                User u = MongoDBInteractor.GetUserByToken(GetToken(request));
                CardSet set = JsonSerializer.Deserialize<CardSet>(request.bodyString);
                CardSet toUpdate = MongoDBInteractor.GetCardSet(set.name, u);
                if (toUpdate == null)
                {
                    request.SendString("set does not exist", "text/plain", 404);
                    return true;
                }
                if (!toUpdate.editors.Where(x => x.nickname == u.nickname).Any())
                {
                    request.Send403();
                    return true;
                }
                foreach (Card card in set.white)
                {
                    toUpdate.white.Remove(toUpdate.white.FirstOrDefault(x => x.content == card.content));
                }
                foreach (Card card in set.black)
                {
                    toUpdate.black.Remove(toUpdate.black.FirstOrDefault(x => x.content == card.content));
                }
                MongoDBInteractor.UpdateSet(toUpdate);
                request.SendString(JsonSerializer.Serialize(toUpdate));
                return true;
            }));
            server.AddRoute("POST", "/api/v1/addtoset", new Func<ServerRequest, bool>(request =>
            {
                User u = MongoDBInteractor.GetUserByToken(GetToken(request));
                CardSet set = JsonSerializer.Deserialize<CardSet>(request.bodyString);
                CardSet toUpdate = MongoDBInteractor.GetCardSet(set.name, u);
                if(toUpdate == null)
                {
                    request.SendString("set does not exist", "text/plain", 404);
                    return true;
                }
                if (!toUpdate.editors.Where(x => x.nickname == u.nickname).Any())
                {
                    request.Send403();
                    return true;
                }
                foreach(Card card in set.white)
                {
                    toUpdate.white.Add(card);
                }
                foreach (Card card in set.black)
                {
                    toUpdate.black.Add(card);
                }
                MongoDBInteractor.UpdateSet(toUpdate);
                request.SendString(JsonSerializer.Serialize(toUpdate));
                return true;
            }));
            server.AddRoute("GET", "/api/v1/mysets", new Func<ServerRequest, bool>(request =>
            {
                request.SendString(JsonSerializer.Serialize(MongoDBInteractor.GetMyCardSets(GetToken(request))));
                return true;
            }));
            server.AddRoute("GET", "/api/v1/set/", new Func<ServerRequest, bool>(request =>
            {
                User u = MongoDBInteractor.GetUserByToken(GetToken(request));
                if (u == null)
                {
                    request.Send403();
                    return true;
                }
                Logger.Log(request.pathDiff);
                request.SendString(JsonSerializer.Serialize(MongoDBInteractor.GetCardSet(request.pathDiff, u)));
                return true;
            }), true);

            // Users
            server.AddRoute("GET", "/api/v1/me", new Func<ServerRequest, bool>(request =>
            {
                request.SendString(JsonSerializer.Serialize(MongoDBInteractor.GetUserByToken(GetToken(request))));
                return true;
            }));
            server.AddRoute("POST", "/api/v1/createuser", new Func<ServerRequest, bool>(request =>
            {
                request.SendString(JsonSerializer.Serialize(UserSystem.ProcessCreateUser(JsonSerializer.Deserialize<LoginRequest>(request.bodyString))));
                return true;
            }));
            server.AddRoute("POST", "/api/v1/login", new Func<ServerRequest, bool>(request =>
            {
                request.SendString(JsonSerializer.Serialize(UserSystem.ProcessLogin(JsonSerializer.Deserialize<LoginRequest>(request.bodyString))));
                return true;
            }));
            server.StartServer(config.port);
        }
    }
}
