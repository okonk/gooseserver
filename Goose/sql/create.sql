DROP DATABASE IllutiaGoose;

CREATE DATABASE IllutiaGoose;
go

USE IllutiaGoose;
go

CREATE LOGIN GooseServer WITH password='password1';
go
CREATE USER GooseServer FOR LOGIN GooseServer;
go
GRANT ALTER TO GooseServer;
go
GRANT CONTROL TO GooseServer;

go