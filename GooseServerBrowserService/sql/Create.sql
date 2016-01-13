USE GooseServerBrowser;

CREATE TABLE [Server] (
    [Id] INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    [Name] NVARCHAR(MAX) NOT NULL,
    [Address] NVARCHAR(MAX) NOT NULL,
    [Port] INT NOT NULL,
    [PlayerCount] INT NOT NULL,
    [Version] NVARCHAR(MAX) NOT NULL,
    [LastUpdate] DATETIME2 NOT NULL
);