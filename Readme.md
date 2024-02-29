# Goose Server v2

Updated Goose Server. New features:
* Supports both Aspereta and Illutia through configuration
* Uses SQLite for storage so no longer requires a separate database server
* C# scripting of items/spells/NPCs
* Easier editing data. Edit via Google Sheets rather than editing SQL directly

## Setting up server

### 1. Install .NET

Download and install the [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

### 2. Copy the data sheet

If using Illutia [click here](https://docs.google.com/spreadsheets//d/1Ig7u4XHc1Vjk4Y1502bwHEVEDba3JTCUcrKwrcOPWyQ/copy).
If using Aspereta [click here](https://docs.google.com/spreadsheets/d/1YB572cYPg43haWySGHxk4v15fS3NSjf_Gj1rLcSE2Zc/copy).

This will prompt you to copy the sheet. Go to your new sheet, share it to make it visible to everyone.

### 3. Edit the settings to configure data id

Open the file `GooseSettings.json`.

By default the settings are configured for Illutia. If you want the server to run for Aspereta you will need to remove/comment out the section under `// Illutia Config`. And uncomment the section under `// Aspereta Config`.

Set up the server to use your data sheet by copying the id out of the URL of your sheet and copying it into the ID in the config `DataLinkId`.

### 4. Run the server

`dotnet "run" --project "Goose/Goose.csproj"`

### 5. Connect client

Server runs on port 2006 by default. So configure your client for that port and play. :)

### 6. Updating server data

When restarting the server you can run it with `updatesql` on the end to update automatically.

`dotnet "run" --project "Goose/Goose.csproj" updatesql`

Otherwise if your character is a GM you can run the `/updatesql` command.

### 7. Connecting to the database

Via command line you can run `sqlite3 Goose/bin/Debug/IllutiaGoose.db` and run SQL commands.

Otherwise you can download a tool such as [SQLite Browser](https://sqlitebrowser.org/) to open the `IllutiaGoose.db` file and edit it.

### 8. Making your character a GM

You can run this SQL with your player name, or update the access_status column for your player and set it to 9.

`UPDATE players SET access_status=9 WHERE player_name='namegoeshere';`