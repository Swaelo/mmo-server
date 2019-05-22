// ================================================================================================================================
// File:        Program.cs
// Description: The server programs main entry point
// ================================================================================================================================

using BepuUtilities;
using ContentLoader;
using ServerUtilities;
using OpenTK;
using Server.Logic;
using Server.Scenes;
using Server.Database;
using Server.GameItems;
using Server.Networking;

namespace Server
{
    public class Program
    {
        public static Window ApplicationWindow; //Window used to render whats happening in the game server to observe events
        public static GameLoop LogicLoop;       //Main update loop where all the magic happens
        public static SceneHarness SceneManager;    //Used to change between game scenes on the fly

        static void Main(string[] args)
        {
            //Load all the existing game items from the exported text file
            ItemList.InitializeItemList();

            //Connect to the sql database server
            DatabaseManager.InitializeDatabaseConnection("localhost", "3306");

            //Start listening for new network client connections
            ConnectionManager.InitializeManager();

            //Open a new window for rendering so we can see whats going on while the server is up
            ApplicationWindow = new Window("Swaelo Server 2.0", new Int2(1280, 720), new Int2(1700, 50), WindowMode.Windowed);
            LogicLoop = new GameLoop(ApplicationWindow);

            //Load in the contents needed for the scene to run
            ContentArchive Content;
            using (var Stream = typeof(Program).Assembly.GetManifestResourceStream("Server.Content.ServerContents.contentarchive"))
                Content = ContentArchive.Load(Stream);

            //Start the scene manager, loading in the default game world scene first, insert the asset contents into it
            SceneManager = new SceneHarness(LogicLoop, Content);

            //Run the game loop until the server is prompted to shut down
            LogicLoop.Run(SceneManager);

            //Free any allocated memory, clean up everything and shut down
            LogicLoop.Dispose();
            ApplicationWindow.Dispose();
        }
    }
}