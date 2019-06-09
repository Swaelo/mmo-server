// ================================================================================================================================
// File:        Program.cs
// Description: The server programs main entry point
// ================================================================================================================================

using System;
using BepuUtilities;
using ContentLoader;
using ServerUtilities;
using Server.Logic;
using Server.Scenes;
using Server.Database;
using Server.GameItems;
using Server.Networking;

namespace Server
{
    public class Program
    {
        public static Program Application;
        public static Window ApplicationWindow; //Window used to render whats happening in the game server to observe events
        public static GameLoop LogicLoop;       //Main update loop where all the magic happens
        //public static SceneHarness SceneManager;    //Used to change between game scenes on the fly
        public static GameWorld World;

        static void Main(string[] args)
        {
            string LocalIP = IPAddressGetter.GetLocalIPAddress();

            Application = new Program();
            if(Application.StartServer(LocalIP))
            {
                Application.RunServer();
                Application.StopServer();
            }
        }

        private bool StartServer(string ServerIP)
        {
            Console.WriteLine("Starting server on " + ServerIP);

            //Connect to the sql database server
            if (!DatabaseManager.InitializeDatabaseConnection(ServerIP, 3306, "serverdatabase", "harleylaurie", "Fuckyahoo420"))
                return false;

            //Load all the existing game items from the exported text file
            ItemInfoDatabase.LoadItemList("Content/MasterItemList.txt");
            ItemManager.InitializeItemManager();

            //Start listening for new network client connections
            WebSocketConnectionManager.InitializeManager(ServerIP);
            //ConnectionManager.InitializeManager();

            //Open a new window for rendering so we can see whats going on while the server is up
            ApplicationWindow = new Window("Swaelo Server 2.0", new Int2(800, 600), WindowMode.Windowed);// new Int2(-1500, 100), WindowMode.Windowed);
            LogicLoop = new GameLoop(ApplicationWindow);

            //Load in the contents needed for the scene to run
            ContentArchive Content;
            using (var Stream = typeof(Program).Assembly.GetManifestResourceStream("Server.Content.ServerContents.contentarchive"))
                Content = ContentArchive.Load(Stream);

            //Initialize the game world simulation
            World = new GameWorld(LogicLoop, Content);

            return true;
        }

        private void RunServer()
        {
            //Run the game loop until the server is prompted to shut down
            LogicLoop.Run(World);
        }

        private void StopServer()
        {
            //Free any allocated memory, clean up everything and shut down
            LogicLoop.Dispose();
            ApplicationWindow.Dispose();
        }
    }
}