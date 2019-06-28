﻿// ================================================================================================================================
// File:        Program.cs
// Description: The server programs main entry point
// Author:      Harley Laurie https://www.github.com/Swaelo/
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
        public static GameWorld World;

        static void Main(string[] args)
        {
            string LocalIP = "192.168.1.102";
            string PublicIP = "203.221.43.175";

            Application = new Program();
            if (Application.StartServer(LocalIP))
            {
                Application.RunServer();
                Application.StopServer();
            }
        }

        private bool StartServer(string ServerIP)
        {
            Console.WriteLine("Starting server on " + ServerIP);

            //Connect to the sql database server
            if (!DatabaseManager.InitializeDatabaseConnection("localhost", 3306, "serverdatabase", "harleylaurie", "Fuckyahoo420"))
                return false;

            //Load all the existing game items from the exported text file
            ItemInfoDatabase.LoadItemList("Content/MasterItemList.txt");
            ItemManager.InitializeItemManager();

            //Start listening for new network client connections
            WebSocketConnectionManager.InitializeManager(ServerIP);

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
        }
    }
}