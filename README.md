# Overview  
This is one half of my MMO project that I have been developing in my free time over the last year or so.  
It's still very early on in development, but my main idea for the project was wanting to create a 3d open world mmorpg project that can be played in your web browser.  
This project is the server application which you can run to host your own server for the mmo, its written in C#, uses TCP/WebSockets for communication with game clients.  
Although the game client uses the Unity engine I have chosen to implement the open source physics engine BepuPhysics2 in the server project to handle all server side game logic, as this engine is also coded in C# and because its open source.  
All you will need to compile and run the server is Visual Studio 2019 with the .NET Core 3.0 SDK installed, you will need to open port 5500 on your router to allow outside connections to be able to connect to the server.  
After you have the server running, take at look at the other half of the project [mmo-client](https://github.com/Swaelo/mmo-client) to get a game client running to connect and start playing.

# Project Requirements 
### Windows 10
I've always used Windows 10 while working on this project, I suspect you may be able to get it running in Windows 8 or 8.1 but that’s totally up to you.  Any issues caused by trying this are left up to you to figure out on your own.  
### Visual Studio Professional 2019
Just like with the Windows 10 requirement, this is what I have always used so I know it works.  You can very likely get it running in older versions (I'm certain 2017 will work with just a few changes), but again that’s totally up to you.
### .NET Core 3.0  
For compiling and debugging the application you will need to install the SDK through the Visual Studio Installer  
To run standalone release builds you will need the [runtimes](https://dotnet.microsoft.com/download/dotnet-core/3.0)
### MySQL Server
The server application maintains a MySQL database to store many important things such as user account information, the locations of players inside the game world, the items in their inventory, etc.  
If no database exists or it’s unable to connect then the server is unable to function correctly.  
### MySQL Workbench  
Very useful application to assist in making manual changes to the database or checking what values are currently being stored which is often needed during development and troubleshooting.  
Initially required to help set up all the correct required table structures/database layout which are needed before the server can interact with it correctly.  

# Setup Instructions

### Database Initialisation  
1. Download and run the official [MySQL Installer](https://dev.mysql.com/downloads/installer/)  
1. If you want to run the server on a different network port other than the default 3306, change this option during the **Type and Networking** section
1. Make sure to memorize or take note of your MySQL Root Password when setting that up as it is extremely important that you don’t lose this later on  
1. In the same section where you set up your Root Password, make sure to create a new MySQL User Account and take note of its username and password as these will be used later  
1. The last thing you should take note of is the ability to run the server under a windows service, and being able to give that service a custom name. Do this and take note of the name you give to the service for later
1. Once you are happy with everything click **Next** and **Execute** through all the menus until the installation process is completed  
1. As the windows service will be set to start automatically whenever your computer is turned on you should do a quick restart now to make sure that happens so we can be certain the server is now running

### Database Setup  
1. Clone my [mmo-server-database-files](https://github.com/Swaelo/mmo-server-database-files) repo onto your computer, this includes all of the .sql files we will execute to quickly and easily set up all the correct table structures that are required for the game server to interact with the database correctly
1. Run the MySQL Workbench application, this is useful for managing the active database during development such as when you may need to make manual changes to the database or check the value or something while testing and debugging later on
1. Click the + button next to **MySQL Connections** to set up a new connection for connecting to and managing the server that we set up earlier  
1. Put whatever you want into the Connection Name, under Hostname you want to put in your computers local IPv4 address (found by entering **ipconfig** in the command window), then enter in the username and password that you created during the database initialization earlier
1. Click Test Connection to make sure you entered everything correctly if it works then you can click OK to finish setting up the connection.  
1. The connection should now appear in the list, you can double click on it to connect to and start managing the database  
1. Click **File**, **Run SQL Script...**, navigate to the mmo-server-database-files repo on your computer, select the **accounts.sql** file, under **Default Schema Name** select **sys**, then click **Run** and then **Close** to import the default accounts table structures into the database.  
1. Repeat the previous step for all of the remaining files **actionbars.sql**, **characters.sql**, **equipments.sql**, **globals.sql** and **inventories.sql** to finish setting up all of the other required table structures.  
1. Now you can close the Workbench application as we have finished setting up the database and its ready to be used by the server application

### Visual Studio Project Setup  
1. Start by cloning this repo to your computer  
2. Inside the projects root directory, double-click the **mmo-server-2.0.sln** file to open the project in Visual Studio  
>note: if visual studio says it needs to perform a **one-way upgrade** to the project the first time you open it, its likely your visual studio is setup incorrectly, double check to make sure you are opening it with Visual Studio 2019 and that you have .NET Core 3.0 workloads installed.  
3. In your **Solution Explorer** ensure **Server** is the default project by right-clicking it and selecting **Set as startup project**  
4. Inside the **Server** project, open the **DatabaseSettings.xml** file and update all the values so they match what you set during SQL Database setup
5. Now if you compile and run the solution it should work
