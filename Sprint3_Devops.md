# **SkillLink DevOps Sprint 3 Overview**

This document outlines the **CI/CD pipeline**, **monitoring**, and **deployment strategy** used in **SkillLink Project Sprint 3**. It automates the testing, building, and deployment of both the frontend and backend, as well as monitoring the system's health and performance.

---

## **1. CI/CD Pipeline Overview**

### **1.1 Unit Test Automation**
- **Unit tests** are triggered **automatically** when a **pull request (PR)** is created to merge into the **main** branch.
- **Technologies**:
  - **Backend**: **.NET Core** (unit tests executed with `dotnet test`)
  - **Frontend**: **React** (using **Jest** for frontend testing)
- **GitHub Actions** is configured to trigger these tests on each PR to ensure code quality before merging into the `main` branch.

### **1.2 Selenium Test Automation**
- **Selenium tests** for end-to-end UI testing are executed automatically when a PR is created for **main**.
- These tests verify the **full functionality** of the application in a **Chrome browser**.

### **1.3 Build & Deploy Automation**
- **Backend Deployment**: The backend (SkillLink API) is deployed through **Azure App Service**.
- **Frontend Deployment**: The frontend (React) is deployed to **Azure Static Web Apps**.
- **GitHub Actions** manages the build and deployment separately for both frontend and backend services.

### **1.4 Monitoring and Performance**
- **Application Insights** is configured to monitor the **backend API** hosted in **Azure App Service**. It tracks:
  - Requests, response times, SQL dependencies, and exceptions.
  - **Live Metrics** provides real-time performance monitoring.

---

## **2. Monitoring and Observability**

### **2.1 Application Insights for Backend Monitoring**
- **Azure Application Insights** is used to monitor the backend API hosted in **Azure App Service**.
- It automatically tracks key telemetry like:
  - Request rates, response times, and dependencies (e.g., SQL database calls).
  - **Live Metrics** to observe system health and request performance.

### **2.2 Frontend and Database Monitoring**
- **Azure Static Web Apps** provides built-in monitoring for page load times, performance, and error tracking for the frontend.
- **Azure SQL Database** monitoring is set up for tracking performance, query latency, and any potential issues.

---

## **3. Deployment Infrastructure**

### **3.1 Azure App Service (Backend)**
- The **SkillLink API** is hosted in **Azure App Service**.
- The API is deployed through **GitHub Actions**, with automatic deployment for every change pushed to `main`.

### **3.2 Azure Static Web Apps (Frontend)**
- The **React frontend** is deployed to **Azure Static Web Apps**, ensuring a fast, scalable delivery model for the front-end application.
- **GitHub Actions** automates deployment for each change to `main`.

### **3.3 Azure SQL Database**
- The **SkillLink database** is hosted on **Azure SQL Database**, storing all the required user and application data.

## **5. Conclusion**

This **DevOps pipeline** automates:
- **Unit and Selenium tests** for every PR.
- **Automated deployment** of both backend and frontend.
- **Real-time monitoring** of the backend using **Azure Application Insights**.

This setup provides full visibility into system health, performance, and allows for automated, consistent deployments.

---

### **Additional Notes:**
- All **configuration** and **secrets** are stored securely in **GitHub Secrets** for pipeline security.
- Future improvements will focus on **scalability**, **performance tuning**, and **advanced monitoring** for a smoother user experience.

