// ================================================================================================================================
// File:        Program.cs
// Description: The server programs main entry point
// ================================================================================================================================

using BepuUtilities;
using ContentLoader;
using ServerUtilities;
using Server.Logic;
using Server.World;
using Server.Database;
using Server.GameItems;
using Server.Networking;
using Server.Scenes;

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
            Application = new Program();
            Application.StartServer();
            Application.RunServer();
            Application.StopServer();
        }

        private void StartServer()
        {
            //Connect to the sql database server
            DatabaseManager.InitializeDatabaseConnection("localhost", "3306");

            //Load all the existing game items from the exported text file
            ItemInfoDatabase.LoadItemList();
            ItemManager.InitializeItemManager();

            //Start listening for new network client connections
            ConnectionManager.InitializeManager();

            //Open a new window for rendering so we can see whats going on while the server is up
            ApplicationWindow = new Window("Swaelo Server 2.0", new Int2(1280, 720), new Int2(-1500, 100), WindowMode.Windowed);
            LogicLoop = new GameLoop(ApplicationWindow);

            //Load in the contents needed for the scene to run
            ContentArchive Content;
            using (var Stream = typeof(Program).Assembly.GetManifestResourceStream("Server.Content.ServerContents.contentarchive"))
                Content = ContentArchive.Load(Stream);

            //Start up the game world simulation
            World = new GameWorld(LogicLoop, Content);
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