
-- --------------------------------------------------
-- Entity Designer DDL Script for SQL Server 2005, 2008, 2012 and Azure
-- --------------------------------------------------
-- Date Created: 05/23/2022 21:51:02
-- Generated from EDMX file: C:\Users\TbpT\Desktop\ipl\S2\TS\proj\Projeto_TS_2211850\App_cliente\database.edmx
-- --------------------------------------------------

SET QUOTED_IDENTIFIER OFF;
GO
USE [ts_proj];
GO
IF SCHEMA_ID(N'dbo') IS NULL EXECUTE(N'CREATE SCHEMA [dbo]');
GO

-- --------------------------------------------------
-- Dropping existing FOREIGN KEY constraints
-- --------------------------------------------------


-- --------------------------------------------------
-- Dropping existing tables
-- --------------------------------------------------


-- --------------------------------------------------
-- Creating all tables
-- --------------------------------------------------

-- Creating table 'users'
CREATE TABLE [dbo].[users] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [username] nvarchar(max)  NOT NULL,
    [password] nvarchar(max)  NOT NULL,
    [salt] nvarchar(max)  NOT NULL
);
GO

-- Creating table 'messages'
CREATE TABLE [dbo].[messages] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [text] nvarchar(max)  NOT NULL,
    [userId] int  NOT NULL,
    [userId1] int  NOT NULL
);
GO

-- --------------------------------------------------
-- Creating all PRIMARY KEY constraints
-- --------------------------------------------------

-- Creating primary key on [Id] in table 'users'
ALTER TABLE [dbo].[users]
ADD CONSTRAINT [PK_users]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'messages'
ALTER TABLE [dbo].[messages]
ADD CONSTRAINT [PK_messages]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- --------------------------------------------------
-- Creating all FOREIGN KEY constraints
-- --------------------------------------------------

-- Creating foreign key on [userId] in table 'messages'
ALTER TABLE [dbo].[messages]
ADD CONSTRAINT [FK_usermessage_sender]
    FOREIGN KEY ([userId])
    REFERENCES [dbo].[users]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_usermessage_sender'
CREATE INDEX [IX_FK_usermessage_sender]
ON [dbo].[messages]
    ([userId]);
GO

-- Creating foreign key on [userId1] in table 'messages'
ALTER TABLE [dbo].[messages]
ADD CONSTRAINT [FK_usermessage_receiver]
    FOREIGN KEY ([userId1])
    REFERENCES [dbo].[users]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_usermessage_receiver'
CREATE INDEX [IX_FK_usermessage_receiver]
ON [dbo].[messages]
    ([userId1]);
GO

-- --------------------------------------------------
-- Script has ended
-- --------------------------------------------------