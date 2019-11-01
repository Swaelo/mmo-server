// ================================================================================================================================
// File:        Program.cs
// Description: The server programs main entry point
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System;
using System.Xml;
using BepuUtilities;
using ContentLoader;
using ServerUtilities;
using Server.Logic;
using Server.World;
using Server.Database;
using Server.GameItems;
using Server.Networking;
using Server.Misc;
using Server.Logging;

namespace Server
{
    public class Program
    {
        public static Program Application;
        public static Window ApplicationWindow; //Window used to render whats happening in the game server to observe events
        public static GameLoop LogicLoop;       //Main update loop where all the magic happens
        public static GameWorld World;          //Handles Physics simulations, entity AI, ingame interactions etc

        static void Main(string[] args)
        {
            //Fetch the local IPv4 address of the current machine being used to host the server
            string LocalIP = IPAddressGetter.GetLocalIPAddress();

            //Create an instance of Program and use that to start up the whole server application inside it
            Application = new Program();
            if (Application.StartServer(LocalIP))
            {
                Application.RunServer();
                Application.StopServer();
            }

            //Save and close the message output .log file before the application closes completely
            MessageLog.Close();
        }

        private bool StartServer(string ServerIP)
        {
            //Display a message to the console indicating the server is not being started
            MessageLog.Print("Starting server on " + ServerIP);

            //Before connecting to the server we need to load the connection settings from the .xml config file, start by loading the file into memory
            XmlDocument ConnectionSettings = new XmlDocument();
            ConnectionSettings.Load("SQLConnectionSettings.xml");

            //Extract all the required values from the file and store each of their into their own variables
            int NetworkPort = Convert.ToInt32(ConnectionSettings.DocumentElement.SelectSingleNode("/root/NetworkPort").InnerText);
            string WindowsServiceName = ConnectionSettings.DocumentElement.SelectSingleNode("/root/WindowsServiceName").InnerText;
            string Username = ConnectionSettings.DocumentElement.SelectSingleNode("/root/Username").InnerText;
            string Password = ConnectionSettings.DocumentElement.SelectSingleNode("/root/Password").InnerText;

            //Forcing server to remote connect to the database running on the dedicated server PC
            if (!DatabaseManager.InitializeDatabaseConnection("203.221.43.175", 3306, "serverdatabase", "swaelo", "2beardmore"))
                return false;

            ////Now all the required values have been loaded we can use those to establish our connection to the database server
            //if (!DatabaseManager.InitializeDatabaseConnection(ServerIP, NetworkPort, WindowsServiceName, Username, Password))
            //    return false;

            //Load all the existing game items from the exported text file
            ItemInfoDatabase.LoadItemList("Content/MasterItemList.txt");
            ItemManager.InitializeItemManager();

            //Start listening for new network client connections
            ConnectionManager.InitializeManager(ServerIP);

            //Open a new window for rendering so we can see whats going on while the server is up
            ApplicationWindow = new Window("Swaelo Server 2.0", new Int2(1024, 768), WindowMode.Windowed); // new Int2(1700, 100), WindowMode.Windowed);
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

            //Log a message showing the server shutdown correctly
            MessageLog.Print("Server shut down correctly");
        }
    }
}