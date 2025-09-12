# SkillLink

SkillLink is a **University Peer-to-Peer Learning Platform** that connects students as learners and tutors.  
It allows students to **offer skills**, **request help**, **schedule sessions**, and **exchange feedback** in a collaborative environment.

## 🎯 Key Features
- Secure registration/login via university email
- CRUD operations for users, skills, requests, and sessions
- Learner & tutor dashboards
- Feedback and ratings system
- Reports: most demanded skills, top tutors, active learners
- Admin panel for moderation & monitoring

## 🛠️ Tech Stack
- **Frontend**: React.js  
- **Backend**: ASP.NET Core Web API (ADO.NET for DB access)  
- **Database**: MySQL (Azure Database for MySQL)  
- **Deployment**: Azure App Service + Docker (optional)  
- **Version Control**: GitHub (branching: main/dev/feature/*)  
- **CI/CD**: GitHub Actions (build → test → deploy)  
- **Testing**: xUnit (.NET), Jest (React), Selenium, JMeter

•⁠  ⁠*Frontend:* React  
•⁠  ⁠*Backend:* ASP.NET (.NET 8 LTS recommended)  
•⁠  ⁠*Package Manager:* npm  
•⁠  ⁠*Database:* Configurable via ASP.NET (SQL Server by default)  

---

## 📂 Repository Structure
⁠ bash

 ⁠
<p style="ext-align: center;"><img width="347" height="854" alt="Screenshot 2025-09-12 at 16 18 28" src="https://github.com/user-attachments/assets/a7190802-6059-4484-8aef-3c8809622721" /><p/>

---

## ✅ Prerequisites

•⁠  ⁠*Node.js* 18+ (LTS) or 20+  
•⁠  ⁠*npm* 9+  
•⁠  ⁠*.NET SDK* 8.0+  
•⁠  ⁠*Git*  

Check versions:
bash
node -v
npm -v
dotnet --version

## ⚡ Quick Start

Get the project running in *5 minutes* 🚀

### 1. Clone the repo
⁠ bash
git clone https://github.com/nipunmeegoda/Resourcely.git
cd Resourcely
 ⁠
### 2. Frontend Setup (React + Vite)
⁠ bash
cd Frontend-Resourcely
npm install
npm run dev
 ⁠
### 3. Backend Setup (ASP.NET)
⁠ bash
cd Backend-Resourcely/Backend-Resourcely
dotnet restore
dotnet build
dotnet run

 ⁠
### 4. Running Both Together
Open Terminal 1 → start frontend:
⁠ bash
cd Frontend-Resourcely
npm run dev
 ⁠
Open Terminal 2 → start frontend:
⁠ bash
cd Backend-Resourcely/Backend-Resourcely
dotnet run




