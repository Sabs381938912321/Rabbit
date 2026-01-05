CREATE DATABASE RabbitDB;

CREATE TABLE Usuarios (
    Id BIGINT PRIMARY KEY,
    Nombre VARCHAR(100)
);


select * from Usuarios;
truncate table Usuarios
truncate table CargaArchivos;

CREATE TABLE CargaArchivos (
    IdCarga INT IDENTITY(1,1) PRIMARY KEY,
    NombreArchivo NVARCHAR(200),
    TotalRegistros INT,
    RegistrosExitosos INT,
    RegistrosError INT,
    Estado NVARCHAR(20), -- Exitoso / ConErrores / Fallido
    FechaCarga DATETIME DEFAULT GETDATE()
);
 select * from CargaArchivos;
