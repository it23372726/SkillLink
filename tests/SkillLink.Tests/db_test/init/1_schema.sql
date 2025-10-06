-- ================================
-- SkillLink – Test DB Schema (MSSQL)
-- ================================

-- USERS
IF OBJECT_ID('dbo.Users','U') IS NULL
BEGIN
    CREATE TABLE dbo.Users (
      UserId INT IDENTITY(1,1) PRIMARY KEY,
      FullName NVARCHAR(255) NOT NULL,
      Email NVARCHAR(255) NOT NULL UNIQUE,
      PasswordHash NVARCHAR(255) NULL,
      Role NVARCHAR(50) NOT NULL CONSTRAINT DF_Users_Role DEFAULT('Learner'),
      CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),
      Bio NVARCHAR(MAX) NULL,
      Location NVARCHAR(255) NULL,
      ProfilePicture NVARCHAR(512) NULL,
      ReadyToTeach BIT NOT NULL CONSTRAINT DF_Users_Ready DEFAULT(0),
      IsActive BIT NOT NULL CONSTRAINT DF_Users_Active DEFAULT(1),
      EmailVerified BIT NOT NULL CONSTRAINT DF_Users_Verified DEFAULT(1),
      EmailVerificationToken NVARCHAR(255) NULL,
      EmailVerificationExpires DATETIME2 NULL
    );
END;

-- REQUESTS
IF OBJECT_ID('dbo.Requests','U') IS NULL
BEGIN
    CREATE TABLE dbo.Requests (
      RequestId INT IDENTITY(1,1) PRIMARY KEY,
      LearnerId INT NOT NULL,
      SkillName NVARCHAR(255) NOT NULL,
      Topic NVARCHAR(MAX) NULL,
      Description NVARCHAR(MAX) NULL,
      Status NVARCHAR(50) NOT NULL CONSTRAINT DF_Requests_Status DEFAULT('OPEN'),
      CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Requests_CreatedAt DEFAULT (SYSUTCDATETIME()),
      CONSTRAINT FK_Requests_Users FOREIGN KEY (LearnerId) REFERENCES dbo.Users(UserId)
    );
END;

-- ACCEPTED REQUESTS (for AcceptedRequestService tests)
IF OBJECT_ID('dbo.AcceptedRequests','U') IS NULL
BEGIN
    CREATE TABLE dbo.AcceptedRequests (
      AcceptedRequestId INT IDENTITY(1,1) PRIMARY KEY,
      RequestId INT NOT NULL,
      AcceptorId INT NOT NULL,
      AcceptedAt DATETIME2 NOT NULL CONSTRAINT DF_AcceptedRequests_AcceptedAt DEFAULT (SYSUTCDATETIME()),
      Status NVARCHAR(50) NOT NULL CONSTRAINT DF_AcceptedRequests_Status DEFAULT('PENDING'),
      ScheduleDate DATETIME2 NULL,
      MeetingType NVARCHAR(50) NULL,
      MeetingLink NVARCHAR(512) NULL,
      CONSTRAINT FK_AcceptedRequests_Requests FOREIGN KEY (RequestId) REFERENCES dbo.Requests(RequestId),
      CONSTRAINT FK_AcceptedRequests_Users FOREIGN KEY (AcceptorId) REFERENCES dbo.Users(UserId)
    );
END;

-- TUTOR POSTS
IF OBJECT_ID('dbo.TutorPosts','U') IS NULL
BEGIN
    CREATE TABLE dbo.TutorPosts (
      PostId INT IDENTITY(1,1) PRIMARY KEY,
      TutorId INT NOT NULL,
      Title NVARCHAR(255) NOT NULL,
      Description NVARCHAR(MAX) NULL,
      MaxParticipants INT NOT NULL CONSTRAINT DF_TutorPosts_Max DEFAULT (1),
      Status NVARCHAR(50) NOT NULL CONSTRAINT DF_TutorPosts_Status DEFAULT('Open'),
      CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_TutorPosts_Created DEFAULT (SYSUTCDATETIME()),
      ScheduledAt DATETIME2 NULL,
      ImageUrl NVARCHAR(1024) NULL,
      CONSTRAINT FK_TutorPosts_Users FOREIGN KEY (TutorId) REFERENCES dbo.Users(UserId)
    );
END;

-- TUTOR POST PARTICIPANTS
IF OBJECT_ID('dbo.TutorPostParticipants','U') IS NULL
BEGIN
    CREATE TABLE dbo.TutorPostParticipants (
      ParticipantId INT IDENTITY(1,1) PRIMARY KEY,
      PostId INT NOT NULL,
      UserId INT NOT NULL,
      AcceptedAt DATETIME2 NOT NULL CONSTRAINT DF_TPP_AcceptedAt DEFAULT (SYSUTCDATETIME()),
      CONSTRAINT FK_TPP_Posts FOREIGN KEY (PostId) REFERENCES dbo.TutorPosts(PostId),
      CONSTRAINT FK_TPP_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId)
    );
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='UQ_TPP_Post_User' AND object_id = OBJECT_ID('dbo.TutorPostParticipants'))
        CREATE UNIQUE INDEX UQ_TPP_Post_User ON dbo.TutorPostParticipants(PostId, UserId);
END;

-- POST REACTIONS
IF OBJECT_ID('dbo.PostReactions','U') IS NULL
BEGIN
    CREATE TABLE dbo.PostReactions (
      Id INT IDENTITY(1,1) PRIMARY KEY,
      PostType NVARCHAR(20) NOT NULL,   -- 'LESSON' or 'REQUEST'
      PostId INT NOT NULL,
      UserId INT NOT NULL,
      Reaction NVARCHAR(10) NOT NULL,  -- 'LIKE' | 'DISLIKE'
      CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_PostReactions_Created DEFAULT (SYSUTCDATETIME())
    );
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='UX_PostReactions' AND object_id = OBJECT_ID('dbo.PostReactions'))
        CREATE UNIQUE INDEX UX_PostReactions ON dbo.PostReactions(PostType, PostId, UserId);
END;

-- POST COMMENTS
IF OBJECT_ID('dbo.PostComments','U') IS NULL
BEGIN
    CREATE TABLE dbo.PostComments (
      CommentId INT IDENTITY(1,1) PRIMARY KEY,
      PostType NVARCHAR(20) NOT NULL,  -- 'LESSON' or 'REQUEST'
      PostId INT NOT NULL,
      UserId INT NOT NULL,
      Content NVARCHAR(MAX) NOT NULL,
      CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_PostComments_Created DEFAULT (SYSUTCDATETIME())
    );
END;

-- FRIENDSHIPS
IF OBJECT_ID('dbo.Friendships','U') IS NULL
BEGIN
    CREATE TABLE dbo.Friendships (
      Id INT IDENTITY(1,1) PRIMARY KEY,
      FollowerId INT NOT NULL,
      FollowedId INT NOT NULL,
      CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Friendships_Created DEFAULT (SYSUTCDATETIME()),
      CONSTRAINT FK_Friendships_Follower FOREIGN KEY (FollowerId) REFERENCES dbo.Users(UserId) ON DELETE CASCADE,
      CONSTRAINT FK_Friendships_Followed FOREIGN KEY (FollowedId) REFERENCES dbo.Users(UserId) ON DELETE CASCADE
    );
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='UX_Friendships_Follow' AND object_id = OBJECT_ID('dbo.Friendships'))
        CREATE UNIQUE INDEX UX_Friendships_Follow ON dbo.Friendships(FollowerId, FollowedId);
END;

-- SESSIONS
IF OBJECT_ID('dbo.Sessions','U') IS NULL
BEGIN
    CREATE TABLE dbo.Sessions (
      SessionId INT IDENTITY(1,1) PRIMARY KEY,
      RequestId INT NOT NULL,
      TutorId INT NOT NULL,
      ScheduledAt DATETIME2 NULL,
      Status NVARCHAR(50) NOT NULL CONSTRAINT DF_Sessions_Status DEFAULT ('PENDING'),
      CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Sessions_Created DEFAULT (SYSUTCDATETIME()),
      CONSTRAINT FK_Sessions_Requests FOREIGN KEY (RequestId) REFERENCES dbo.Requests(RequestId),
      CONSTRAINT FK_Sessions_Users FOREIGN KEY (TutorId) REFERENCES dbo.Users(UserId)
    );
END;

-- SKILLS
IF OBJECT_ID('dbo.Skills','U') IS NULL
BEGIN
    CREATE TABLE dbo.Skills (
      SkillId INT IDENTITY(1,1) PRIMARY KEY,
      Name NVARCHAR(255) NOT NULL UNIQUE,
      IsPredefined BIT NOT NULL CONSTRAINT DF_Skills_Predef DEFAULT 0
    );
END;

-- USER SKILLS
IF OBJECT_ID('dbo.UserSkills','U') IS NULL
BEGIN
    CREATE TABLE dbo.UserSkills (
      UserSkillId INT IDENTITY(1,1) PRIMARY KEY,
      UserId INT NOT NULL,
      SkillId INT NOT NULL,
      Level NVARCHAR(50) NOT NULL,
      CONSTRAINT FK_UserSkills_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId) ON DELETE CASCADE,
      CONSTRAINT FK_UserSkills_Skills FOREIGN KEY (SkillId) REFERENCES dbo.Skills(SkillId) ON DELETE CASCADE
    );
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='UX_UserSkills_User_Skill' AND object_id = OBJECT_ID('dbo.UserSkills'))
        CREATE UNIQUE INDEX UX_UserSkills_User_Skill ON dbo.UserSkills(UserId, SkillId);
END;
