/* ========================================================================
   SkillLink — Database Bootstrap (SQL Server)
   - Safe drop in dependency order
   - Tables with explicit constraints & defaults
   - Status CHECK constraints aligned with API/frontend
   - Helpful indexes
   ======================================================================== */

SET NOCOUNT ON;
GO

/* ======================= DROP IN DEPENDENCY ORDER ======================= */
IF OBJECT_ID('dbo.TutorPostParticipants','U') IS NOT NULL DROP TABLE dbo.TutorPostParticipants;
IF OBJECT_ID('dbo.PostComments','U')          IS NOT NULL DROP TABLE dbo.PostComments;
IF OBJECT_ID('dbo.PostReactions','U')         IS NOT NULL DROP TABLE dbo.PostReactions;
IF OBJECT_ID('dbo.Friendships','U')           IS NOT NULL DROP TABLE dbo.Friendships;
IF OBJECT_ID('dbo.Ratings','U')               IS NOT NULL DROP TABLE dbo.Ratings;
IF OBJECT_ID('dbo.AcceptedRequests','U')      IS NOT NULL DROP TABLE dbo.AcceptedRequests;
IF OBJECT_ID('dbo.Sessions','U')              IS NOT NULL DROP TABLE dbo.Sessions;
IF OBJECT_ID('dbo.UserSkills','U')            IS NOT NULL DROP TABLE dbo.UserSkills;
IF OBJECT_ID('dbo.Skills','U')                IS NOT NULL DROP TABLE dbo.Skills;
IF OBJECT_ID('dbo.TutorPosts','U')            IS NOT NULL DROP TABLE dbo.TutorPosts;
IF OBJECT_ID('dbo.Notifications','U')         IS NOT NULL DROP TABLE dbo.Notifications;
IF OBJECT_ID('dbo.Feedback','U')              IS NOT NULL DROP TABLE dbo.Feedback;
IF OBJECT_ID('dbo.Requests','U')              IS NOT NULL DROP TABLE dbo.Requests;
IF OBJECT_ID('dbo.Users','U')                 IS NOT NULL DROP TABLE dbo.Users;
GO

/* ======================= USERS ======================= */
CREATE TABLE dbo.Users (
  UserId                   INT IDENTITY(1,1) PRIMARY KEY,
  FullName                 NVARCHAR(255) NOT NULL,
  Email                    NVARCHAR(255) NOT NULL UNIQUE,
  PasswordHash             NVARCHAR(255) NULL,
  Role                     NVARCHAR(50)  NOT NULL
                           CONSTRAINT DF_Users_Role DEFAULT ('Learner'),
  CreatedAt                DATETIME2     NOT NULL
                           CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),
  Bio                      NVARCHAR(MAX) NULL,
  Location                 NVARCHAR(255) NULL,
  ProfilePicture           NVARCHAR(500) NULL,
  ReadyToTeach             BIT           NOT NULL
                           CONSTRAINT DF_Users_ReadyToTeach DEFAULT (0),
  IsActive                 BIT           NOT NULL
                           CONSTRAINT DF_Users_IsActive DEFAULT (1),
  EmailVerified            BIT           NOT NULL
                           CONSTRAINT DF_Users_EmailVerified DEFAULT (0),
  EmailVerificationToken   NVARCHAR(255) NULL,
  EmailVerificationExpires DATETIME2     NULL,
  CONSTRAINT CK_Users_Role
    CHECK (Role IN (N'Learner', N'Tutor', N'Admin'))
);
GO

/* ======================= REQUESTS ======================= */
CREATE TABLE dbo.Requests (
  RequestId        INT IDENTITY(1,1) PRIMARY KEY,
  LearnerId        INT NOT NULL,
  SkillName        NVARCHAR(255) NOT NULL,
  Topic            NVARCHAR(255) NULL,
  Description      NVARCHAR(MAX) NULL,
  PreferredTutorId INT NULL,
  IsPrivate        BIT NOT NULL
                   CONSTRAINT DF_Requests_IsPrivate DEFAULT (0),
  Status           NVARCHAR(50) NOT NULL
                   CONSTRAINT DF_Requests_Status DEFAULT (N'PENDING'),
  CreatedAt        DATETIME2 NOT NULL
                   CONSTRAINT DF_Requests_CreatedAt DEFAULT (SYSUTCDATETIME()),
  CONSTRAINT FK_Requests_Learner
    FOREIGN KEY (LearnerId)        REFERENCES dbo.Users(UserId) ON DELETE CASCADE,
  CONSTRAINT FK_Requests_PreferredTutor
    FOREIGN KEY (PreferredTutorId) REFERENCES dbo.Users(UserId) ON DELETE NO ACTION,
  CONSTRAINT CK_Requests_Status
    CHECK (Status IN (N'PENDING', N'SCHEDULED', N'COMPLETED', N'CANCELLED'))
);
GO

/* ======================= NOTIFICATIONS ======================= */
CREATE TABLE dbo.Notifications (
  NotificationId INT IDENTITY(1,1) PRIMARY KEY,
  UserId INT NOT NULL,
  [Type]  NVARCHAR(64)  NOT NULL,
  [Title] NVARCHAR(200) NOT NULL,
  [Body]  NVARCHAR(MAX) NULL,
  [Link]  NVARCHAR(400) NULL,
  IsRead  BIT NOT NULL CONSTRAINT DF_Notifications_IsRead DEFAULT(0),
  CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Notifications_CreatedAt DEFAULT SYSUTCDATETIME(),
  CONSTRAINT FK_Notifications_User FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId) ON DELETE CASCADE
);
GO

/* ======================= TUTOR POSTS ======================= */
CREATE TABLE dbo.TutorPosts (
  PostId          INT IDENTITY(1,1) PRIMARY KEY,
  TutorId         INT NOT NULL,
  Title           NVARCHAR(255) NOT NULL,
  Description     NVARCHAR(MAX) NULL,
  MaxParticipants INT NOT NULL,
  Status          NVARCHAR(50) NOT NULL
                  CONSTRAINT DF_TutorPosts_Status DEFAULT (N'Open'),
  CreatedAt       DATETIME2 NOT NULL
                  CONSTRAINT DF_TutorPosts_CreatedAt DEFAULT (SYSUTCDATETIME()),
  ScheduledAt     DATETIME2 NULL,
  ImageUrl        NVARCHAR(500) NULL,
  MeetingLink     NVARCHAR(512) NULL,
  CONSTRAINT FK_TutorPosts_Users
    FOREIGN KEY (TutorId) REFERENCES dbo.Users(UserId) ON DELETE CASCADE,
  CONSTRAINT CK_TutorPosts_Status
    CHECK (Status IN (N'Open', N'Scheduled', N'Completed', N'Cancelled'))
);
GO

/* ======================= TUTOR POST PARTICIPANTS ======================= */
CREATE TABLE dbo.TutorPostParticipants (
  ParticipantId INT IDENTITY(1,1) PRIMARY KEY,
  PostId        INT NOT NULL,
  UserId        INT NOT NULL,
  AcceptedAt    DATETIME2 NOT NULL CONSTRAINT DF_TPP_AcceptedAt DEFAULT (SYSUTCDATETIME()),
  CONSTRAINT UQ_TPP_Post_User UNIQUE (PostId, UserId),
  CONSTRAINT FK_TPP_Posts FOREIGN KEY (PostId) REFERENCES dbo.TutorPosts(PostId) ON DELETE CASCADE,
  CONSTRAINT FK_TPP_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId) ON DELETE NO ACTION
);
GO

/* ======================= FRIENDSHIPS ======================= */
CREATE TABLE dbo.Friendships (
  Id         INT IDENTITY(1,1) PRIMARY KEY,
  FollowerId INT NOT NULL,
  FollowedId INT NOT NULL,
  CreatedAt  DATETIME2 NOT NULL CONSTRAINT DF_Friendships_CreatedAt DEFAULT (SYSUTCDATETIME()),
  CONSTRAINT UQ_Friendships UNIQUE (FollowerId, FollowedId),
  CONSTRAINT FK_Friendships_Follower FOREIGN KEY (FollowerId) REFERENCES dbo.Users(UserId) ON DELETE CASCADE,
  CONSTRAINT FK_Friendships_Followed FOREIGN KEY (FollowedId) REFERENCES dbo.Users(UserId) ON DELETE NO ACTION
);
GO

/* ======================= POST REACTIONS ======================= */
CREATE TABLE dbo.PostReactions (
  ReactionId INT IDENTITY(1,1) PRIMARY KEY,
  PostType   NVARCHAR(20) NOT NULL,
  PostId     INT NOT NULL,
  UserId     INT NOT NULL,
  Reaction   NVARCHAR(10) NOT NULL,
  CreatedAt  DATETIME2 NOT NULL CONSTRAINT DF_PostReactions_CreatedAt DEFAULT (SYSUTCDATETIME()),
  CONSTRAINT UQ_PostReactions UNIQUE (PostType, PostId, UserId),
  CONSTRAINT FK_PostReactions_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId) ON DELETE CASCADE
);
GO

/* ======================= POST COMMENTS ======================= */
CREATE TABLE dbo.PostComments (
  CommentId INT IDENTITY(1,1) PRIMARY KEY,
  PostType  NVARCHAR(20) NOT NULL,
  PostId    INT NOT NULL,
  UserId    INT NOT NULL,
  Content   NVARCHAR(MAX) NOT NULL,
  CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_PostComments_CreatedAt DEFAULT (SYSUTCDATETIME()),
  CONSTRAINT FK_PostComments_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId) ON DELETE CASCADE
);
GO

/* ======================= SKILLS / USER SKILLS ======================= */
CREATE TABLE dbo.Skills (
  SkillId      INT IDENTITY(1,1) PRIMARY KEY,
  [Name]       NVARCHAR(255) NOT NULL UNIQUE,
  IsPredefined BIT NOT NULL CONSTRAINT DF_Skills_IsPredefined DEFAULT (0)
);
GO

CREATE TABLE dbo.UserSkills (
  UserSkillId INT IDENTITY(1,1) PRIMARY KEY,
  UserId      INT NOT NULL,
  SkillId     INT NOT NULL,
  [Level]     NVARCHAR(50) NOT NULL CONSTRAINT DF_UserSkills_Level DEFAULT (N'Beginner'),
  CONSTRAINT UQ_UserSkills UNIQUE (UserId, SkillId),
  CONSTRAINT FK_UserSkills_Users  FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId)  ON DELETE CASCADE,
  CONSTRAINT FK_UserSkills_Skills FOREIGN KEY (SkillId) REFERENCES dbo.Skills(SkillId) ON DELETE CASCADE
);
GO

/* ======================= ACCEPTED REQUESTS ======================= */
CREATE TABLE dbo.AcceptedRequests (
  AcceptedRequestId INT IDENTITY(1,1) PRIMARY KEY,
  RequestId    INT NOT NULL,
  AcceptorId   INT NOT NULL,
  AcceptedAt   DATETIME2 NOT NULL CONSTRAINT DF_AR_AcceptedAt DEFAULT (SYSUTCDATETIME()),
  Status       NVARCHAR(50) NOT NULL CONSTRAINT DF_AR_Status DEFAULT (N'PENDING'),
  ScheduleDate DATETIME2 NULL,
  MeetingType  NVARCHAR(100) NULL,
  MeetingLink  NVARCHAR(500) NULL,
  CONSTRAINT UQ_AcceptedRequests UNIQUE (RequestId, AcceptorId),
  CONSTRAINT FK_AR_Requests FOREIGN KEY (RequestId)  REFERENCES dbo.Requests(RequestId) ON DELETE CASCADE,
  CONSTRAINT FK_AR_Users    FOREIGN KEY (AcceptorId) REFERENCES dbo.Users(UserId)    ON DELETE NO ACTION,
  CONSTRAINT CK_AcceptedRequests_Status
    CHECK (Status IN (N'PENDING', N'SCHEDULED', N'COMPLETED', N'CANCELLED'))
);
GO

/* ======================= SESSIONS ======================= */
CREATE TABLE dbo.Sessions (
  SessionId   INT IDENTITY(1,1) PRIMARY KEY,
  RequestId   INT NOT NULL,
  TutorId     INT NOT NULL,
  ScheduledAt DATETIME2 NULL,
  Status      NVARCHAR(50) NOT NULL CONSTRAINT DF_Sessions_Status DEFAULT (N'PENDING'),
  CreatedAt   DATETIME2 NOT NULL CONSTRAINT DF_Sessions_CreatedAt DEFAULT (SYSUTCDATETIME()),
  CONSTRAINT FK_Sessions_Requests FOREIGN KEY (RequestId) REFERENCES dbo.Requests(RequestId) ON DELETE CASCADE,
  CONSTRAINT FK_Sessions_Users    FOREIGN KEY (TutorId)   REFERENCES dbo.Users(UserId)    ON DELETE NO ACTION,
  CONSTRAINT CK_Sessions_Status
    CHECK (Status IN (N'PENDING', N'SCHEDULED', N'COMPLETED', N'CANCELLED'))
);
GO

/* ======================= RATINGS ======================= */
CREATE TABLE dbo.Ratings
(
  RatingId          INT IDENTITY(1,1) PRIMARY KEY,
  AcceptedRequestId INT NOT NULL,
  TutorId           INT NOT NULL,
  LearnerId         INT NOT NULL,
  Rating            INT NOT NULL CHECK (Rating BETWEEN 1 AND 5),
  Comment           NVARCHAR(2000) NULL,
  CreatedAt         DATETIME2 NOT NULL CONSTRAINT DF_Ratings_CreatedAt DEFAULT SYSUTCDATETIME(),
  CONSTRAINT UX_Ratings_Accepted_Learner UNIQUE (AcceptedRequestId, LearnerId),
  CONSTRAINT FK_Ratings_Accepted FOREIGN KEY (AcceptedRequestId)
      REFERENCES dbo.AcceptedRequests(AcceptedRequestId) ON DELETE CASCADE,
  CONSTRAINT FK_Ratings_Tutor FOREIGN KEY (TutorId)
      REFERENCES dbo.Users(UserId) ON DELETE NO ACTION,
  CONSTRAINT FK_Ratings_Learner FOREIGN KEY (LearnerId)
      REFERENCES dbo.Users(UserId) ON DELETE NO ACTION
);
GO

/* ======================= FEEDBACK (OPTIONAL) ======================= */
CREATE TABLE dbo.Feedback (
  FeedbackId INT IDENTITY(1,1) PRIMARY KEY,
  UserId     INT NULL,
  [Subject]  NVARCHAR(200) NULL,
  [Message]  NVARCHAR(MAX) NOT NULL,
  [Page]     NVARCHAR(256) NULL,
  UserAgent  NVARCHAR(256) NULL,
  CreatedAt  DATETIME2 NOT NULL CONSTRAINT DF_Feedback_CreatedAt DEFAULT SYSUTCDATETIME(),
  IsRead     BIT NOT NULL CONSTRAINT DF_Feedback_IsRead DEFAULT(0),
  CONSTRAINT FK_Feedback_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId)
);
GO

/* ======================= HELPFUL INDEXES ======================= */
-- Requests
CREATE INDEX IX_Requests_CreatedAt        ON dbo.Requests (CreatedAt DESC);
CREATE INDEX IX_Requests_LearnerId        ON dbo.Requests (LearnerId);
CREATE INDEX IX_Requests_PreferredTutorId ON dbo.Requests (PreferredTutorId);
CREATE INDEX IX_Requests_Status           ON dbo.Requests (Status);

-- TutorPosts
CREATE INDEX IX_TutorPosts_CreatedAt      ON dbo.TutorPosts (CreatedAt DESC);
CREATE INDEX IX_TutorPosts_Status         ON dbo.TutorPosts (Status);
CREATE INDEX IX_TutorPosts_ScheduledAt    ON dbo.TutorPosts (ScheduledAt);

-- Post reactions/comments (by post)
CREATE INDEX IX_PostReactions_Post        ON dbo.PostReactions (PostType, PostId);
CREATE INDEX IX_PostComments_Post         ON dbo.PostComments  (PostType, PostId);

-- AcceptedRequests
CREATE INDEX IX_AcceptedRequests_Acceptor ON dbo.AcceptedRequests (AcceptorId, RequestId);
CREATE INDEX IX_AcceptedRequests_Request  ON dbo.AcceptedRequests (RequestId);
CREATE INDEX IX_AcceptedRequests_Status   ON dbo.AcceptedRequests (Status, ScheduleDate);

-- Ratings
CREATE INDEX IX_Ratings_TutorId           ON dbo.Ratings (TutorId);
GO

/* ======================= QUICK INSPECTION QUERIES ======================= */
-- List tables/views
SELECT table_catalog AS [database], table_schema AS [schema], table_name AS [name], table_type AS [type]
FROM INFORMATION_SCHEMA.TABLES
ORDER BY table_schema, table_name;
GO

-- Sanity checks
SELECT TOP 50 * FROM dbo.Users ORDER BY UserId;
SELECT TOP 50 * FROM dbo.Requests ORDER BY RequestId DESC;
SELECT TOP 50 * FROM dbo.Ratings ORDER BY RatingId DESC;
GO
