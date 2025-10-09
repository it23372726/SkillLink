-- db_test/init/1_schema.sql (MSSQL)
USE [skilllink_test];
GO

/* ======================= USERS ======================= */
IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users (
        UserId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        FullName NVARCHAR(255) NOT NULL,
        Email NVARCHAR(255) NOT NULL,
        PasswordHash NVARCHAR(255) NULL,
        Role NVARCHAR(50) NOT NULL CONSTRAINT DF_Users_Role DEFAULT ('Learner'),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),
        Bio NVARCHAR(MAX) NULL,
        Location NVARCHAR(255) NULL,
        ProfilePicture NVARCHAR(500) NULL,
        ReadyToTeach BIT NOT NULL CONSTRAINT DF_Users_ReadyToTeach DEFAULT (0),
        IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
        EmailVerified BIT NOT NULL CONSTRAINT DF_Users_EmailVerified DEFAULT (0),
        EmailVerificationToken NVARCHAR(255) NULL,
        EmailVerificationExpires DATETIME2 NULL
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_Email' AND object_id = OBJECT_ID('dbo.Users'))
    CREATE UNIQUE INDEX IX_Users_Email ON dbo.Users(Email);
GO


/* ======================= REQUESTS ======================= */
IF OBJECT_ID('dbo.Requests', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Requests (
        RequestId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        LearnerId INT NOT NULL,
        SkillName NVARCHAR(255) NOT NULL,
        Topic NVARCHAR(255) NULL,
        Description NVARCHAR(MAX) NULL,
        PreferredTutorId INT NULL,
        IsPrivate BIT NOT NULL CONSTRAINT DF_Requests_IsPrivate DEFAULT (0),
        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_Requests_Status DEFAULT ('PENDING'),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Requests_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_Requests_Learner FOREIGN KEY (LearnerId)
            REFERENCES dbo.Users(UserId) ON DELETE CASCADE,
        CONSTRAINT FK_Requests_PreferredTutor FOREIGN KEY (PreferredTutorId)
            REFERENCES dbo.Users(UserId)
    );
END
ELSE
BEGIN
    -- Add PreferredTutorId if missing
    IF COL_LENGTH('dbo.Requests','PreferredTutorId') IS NULL
        ALTER TABLE dbo.Requests ADD PreferredTutorId INT NULL;

    -- Add IsPrivate if missing (NOT NULL requires default + WITH VALUES)
    IF COL_LENGTH('dbo.Requests','IsPrivate') IS NULL
        ALTER TABLE dbo.Requests ADD IsPrivate BIT NOT NULL CONSTRAINT DF_Requests_IsPrivate DEFAULT(0) WITH VALUES;

    -- Ensure FK to PreferredTutor exists
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Requests_PreferredTutor' AND parent_object_id = OBJECT_ID('dbo.Requests'))
        ALTER TABLE dbo.Requests WITH CHECK ADD CONSTRAINT FK_Requests_PreferredTutor FOREIGN KEY(PreferredTutorId) REFERENCES dbo.Users(UserId);

    -- Ensure Status default is 'PENDING'
    DECLARE @dfReqStatus NVARCHAR(128);
    SELECT @dfReqStatus = dc.name
    FROM sys.default_constraints dc
    JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
    WHERE dc.parent_object_id = OBJECT_ID('dbo.Requests') AND c.name = 'Status';
    IF @dfReqStatus IS NOT NULL
        EXEC('ALTER TABLE dbo.Requests DROP CONSTRAINT ' + QUOTENAME(@dfReqStatus));
    IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE parent_object_id = OBJECT_ID('dbo.Requests'))
        ALTER TABLE dbo.Requests ADD CONSTRAINT DF_Requests_Status DEFAULT('PENDING') FOR Status;
END;

-- Helpful indexes for requests
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Requests_CreatedAt' AND object_id = OBJECT_ID('dbo.Requests'))
    CREATE INDEX IX_Requests_CreatedAt        ON dbo.Requests (CreatedAt DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Requests_LearnerId' AND object_id = OBJECT_ID('dbo.Requests'))
    CREATE INDEX IX_Requests_LearnerId        ON dbo.Requests (LearnerId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Requests_PreferredTutorId' AND object_id = OBJECT_ID('dbo.Requests'))
    CREATE INDEX IX_Requests_PreferredTutorId ON dbo.Requests (PreferredTutorId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Requests_Status' AND object_id = OBJECT_ID('dbo.Requests'))
    CREATE INDEX IX_Requests_Status           ON dbo.Requests (Status);
GO


/* ======================= NOTIFICATIONS ======================= */
IF OBJECT_ID('dbo.Notifications','U') IS NULL
BEGIN
    CREATE TABLE dbo.Notifications (
      NotificationId INT IDENTITY(1,1) PRIMARY KEY,
      UserId INT NOT NULL,
      [Type]  NVARCHAR(64)  NOT NULL,
      [Title] NVARCHAR(200) NOT NULL,
      [Body]  NVARCHAR(MAX) NULL,
      [Link]  NVARCHAR(400) NULL,
      IsRead  BIT NOT NULL DEFAULT(0),
      CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
      CONSTRAINT FK_Notifications_User FOREIGN KEY (UserId)
        REFERENCES dbo.Users(UserId) ON DELETE CASCADE
    );
END;
GO


/* ======================= TUTOR POSTS ======================= */
IF OBJECT_ID('dbo.TutorPosts', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TutorPosts (
        PostId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        TutorId INT NOT NULL,
        Title NVARCHAR(255) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        MaxParticipants INT NOT NULL,
        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_TutorPosts_Status DEFAULT ('Open'),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_TutorPosts_CreatedAt DEFAULT (SYSUTCDATETIME()),
        ScheduledAt DATETIME2 NULL,
        ImageUrl NVARCHAR(500) NULL,
        MeetingLink NVARCHAR(512) NULL, -- <-- ADDED HERE
        CONSTRAINT FK_TutorPosts_Users FOREIGN KEY (TutorId)
            REFERENCES dbo.Users(UserId) ON DELETE CASCADE
    );
END;

-- ADDED THIS BLOCK to ensure MeetingLink column exists for backward compatibility
IF COL_LENGTH('dbo.TutorPosts','MeetingLink') IS NULL
    ALTER TABLE dbo.TutorPosts ADD MeetingLink NVARCHAR(512) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TutorPosts_CreatedAt' AND object_id = OBJECT_ID('dbo.TutorPosts'))
    CREATE INDEX IX_TutorPosts_CreatedAt ON dbo.TutorPosts (CreatedAt DESC);
GO


/* ======================= TUTOR POST PARTICIPANTS ======================= */
IF OBJECT_ID('dbo.TutorPostParticipants', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TutorPostParticipants (
        ParticipantId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PostId INT NOT NULL,
        UserId INT NOT NULL,
        AcceptedAt DATETIME2 NOT NULL CONSTRAINT DF_TPP_AcceptedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_TutorPostParticipants_Post_User UNIQUE (PostId, UserId),
        CONSTRAINT FK_TPP_Posts FOREIGN KEY (PostId)
            REFERENCES dbo.TutorPosts(PostId) ON DELETE CASCADE,
        CONSTRAINT FK_TPP_Users FOREIGN KEY (UserId)
            REFERENCES dbo.Users(UserId) -- NO ACTION (default)
    );
END;
GO


/* ======================= FRIENDSHIPS ======================= */
IF OBJECT_ID('dbo.Friendships', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Friendships (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        FollowerId INT NOT NULL,
        FollowedId INT NOT NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Friendships_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_Friendships_Follow UNIQUE (FollowerId, FollowedId),
        CONSTRAINT FK_Friendships_Follower FOREIGN KEY (FollowerId)
            REFERENCES dbo.Users(UserId), -- NO ACTION
        CONSTRAINT FK_Friendships_Followed FOREIGN KEY (FollowedId)
            REFERENCES dbo.Users(UserId)  -- NO ACTION
    );
END;
GO


/* ======================= FEED REACTIONS ======================= */
IF OBJECT_ID('dbo.PostReactions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PostReactions (
        ReactionId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PostType NVARCHAR(20) NOT NULL, -- LESSON or REQUEST
        PostId INT NOT NULL,
        UserId INT NOT NULL,
        Reaction NVARCHAR(10) NOT NULL, -- LIKE or DISLIKE
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_PostReactions_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_PostReactions_Post_User UNIQUE (PostType, PostId, UserId),
        CONSTRAINT FK_PostReactions_Users FOREIGN KEY (UserId)
            REFERENCES dbo.Users(UserId) ON DELETE CASCADE
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PostReactions_Post' AND object_id = OBJECT_ID('dbo.PostReactions'))
    CREATE INDEX IX_PostReactions_Post ON dbo.PostReactions (PostType, PostId);
GO


/* ======================= FEED COMMENTS ======================= */
IF OBJECT_ID('dbo.PostComments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PostComments (
        CommentId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PostType NVARCHAR(20) NOT NULL,  -- LESSON or REQUEST
        PostId INT NOT NULL,
        UserId INT NOT NULL,
        Content NVARCHAR(MAX) NOT NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_PostComments_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_PostComments_Users FOREIGN KEY (UserId)
            REFERENCES dbo.Users(UserId) ON DELETE CASCADE
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PostComments_Post' AND object_id = OBJECT_ID('dbo.PostComments'))
    CREATE INDEX IX_PostComments_Post ON dbo.PostComments (PostType, PostId);
GO


/* ======================= SKILLS ======================= */
IF OBJECT_ID('dbo.Skills', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Skills (
        SkillId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(255) NOT NULL,
        IsPredefined BIT NOT NULL CONSTRAINT DF_Skills_IsPredefined DEFAULT (0)
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Skills_Name' AND object_id = OBJECT_ID('dbo.Skills'))
    CREATE UNIQUE INDEX IX_Skills_Name ON dbo.Skills (Name);
GO

IF OBJECT_ID('dbo.UserSkills', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserSkills (
        UserSkillId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserId INT NOT NULL,
        SkillId INT NOT NULL,
        Level NVARCHAR(50) NOT NULL CONSTRAINT DF_UserSkills_Level DEFAULT ('Beginner'),
        CONSTRAINT UQ_UserSkills_User_Skill UNIQUE (UserId, SkillId),
        CONSTRAINT FK_UserSkills_Users FOREIGN KEY (UserId)
            REFERENCES dbo.Users(UserId) ON DELETE CASCADE,
        CONSTRAINT FK_UserSkills_Skills FOREIGN KEY (SkillId)
            REFERENCES dbo.Skills(SkillId) ON DELETE CASCADE
    );
END;
GO


/* ======================= ACCEPTED REQUESTS ======================= */
IF OBJECT_ID('dbo.AcceptedRequests', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AcceptedRequests (
        AcceptedRequestId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        RequestId INT NOT NULL,
        AcceptorId INT NOT NULL,
        AcceptedAt DATETIME2 NOT NULL CONSTRAINT DF_AcceptedRequests_AcceptedAt DEFAULT (SYSUTCDATETIME()),
        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_AcceptedRequests_Status DEFAULT ('PENDING'),
        ScheduleDate DATETIME2 NULL,
        MeetingType NVARCHAR(100) NULL,
        MeetingLink NVARCHAR(500) NULL,
        CONSTRAINT UQ_AcceptedRequests_Request_Acceptor UNIQUE (RequestId, AcceptorId),
        CONSTRAINT FK_AcceptedRequests_Requests FOREIGN KEY (RequestId)
            REFERENCES dbo.Requests(RequestId) ON DELETE CASCADE,
        CONSTRAINT FK_AcceptedRequests_Users FOREIGN KEY (AcceptorId)
            REFERENCES dbo.Users(UserId) -- NO ACTION
    );
END;
-- helpful indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AcceptedRequests_Acceptor' AND object_id = OBJECT_ID('dbo.AcceptedRequests'))
    CREATE INDEX IX_AcceptedRequests_Acceptor ON dbo.AcceptedRequests (AcceptorId, RequestId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AcceptedRequests_Request' AND object_id = OBJECT_ID('dbo.AcceptedRequests'))
    CREATE INDEX IX_AcceptedRequests_Request ON dbo.AcceptedRequests (RequestId);
GO


/* ======================= SESSIONS ======================= */
IF OBJECT_ID('dbo.Sessions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sessions (
        SessionId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        RequestId INT NOT NULL,
        TutorId INT NOT NULL,
        ScheduledAt DATETIME2 NULL,
        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_Sessions_Status DEFAULT ('PENDING'),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Sessions_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_Sessions_Requests FOREIGN KEY (RequestId)
            REFERENCES dbo.Requests(RequestId) ON DELETE CASCADE,
        CONSTRAINT FK_Sessions_Users FOREIGN KEY (TutorId)
            REFERENCES dbo.Users(UserId) -- NO ACTION
    );
END;
GO