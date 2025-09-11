CREATE TABLE Users (
    UserID INT AUTO_INCREMENT PRIMARY KEY,
    Email VARCHAR(100) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    FullName VARCHAR(100) NOT NULL,
    Role ENUM('learner','tutor','admin') NOT NULL,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE Skills (
    SkillID INT AUTO_INCREMENT PRIMARY KEY,
    SkillName VARCHAR(100) NOT NULL UNIQUE,
    Description TEXT
);

CREATE TABLE UserSkills (
    UserID INT NOT NULL,
    SkillID INT NOT NULL,
    Level ENUM('beginner','intermediate','advanced') NOT NULL,
    PRIMARY KEY(UserID, SkillID),
    FOREIGN KEY(UserID) REFERENCES Users(UserID),
    FOREIGN KEY(SkillID) REFERENCES Skills(SkillID)
);

CREATE TABLE Requests (
    RequestID INT AUTO_INCREMENT PRIMARY KEY,
    LearnerID INT NOT NULL,
    SkillID INT NOT NULL,
    Status ENUM('pending','accepted','completed','cancelled') DEFAULT 'pending',
    RequestedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY(LearnerID) REFERENCES Users(UserID),
    FOREIGN KEY(SkillID) REFERENCES Skills(SkillID)
);

CREATE TABLE Sessions (
    SessionID INT AUTO_INCREMENT PRIMARY KEY,
    RequestID INT NOT NULL,
    TutorID INT NOT NULL,
    ScheduledAt DATETIME NOT NULL,
    DurationMinutes INT,
    Status ENUM('scheduled','completed','cancelled') DEFAULT 'scheduled',
    FOREIGN KEY(RequestID) REFERENCES Requests(RequestID),
    FOREIGN KEY(TutorID) REFERENCES Users(UserID)
);

CREATE TABLE Feedback (
    FeedbackID INT AUTO_INCREMENT PRIMARY KEY,
    SessionID INT NOT NULL,
    ReviewerID INT NOT NULL,
    Rating INT CHECK(Rating BETWEEN 1 AND 5),
    Comments TEXT,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY(SessionID) REFERENCES Sessions(SessionID),
    FOREIGN KEY(ReviewerID) REFERENCES Users(UserID)
);
