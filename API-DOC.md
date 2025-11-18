#!/usr/bin/env bash
set -euo pipefail

DOCS_DIR="$(cd "$(dirname "$0")/.." && pwd)/docs"
MD_FILE="$DOCS_DIR/API_DOCUMENTATION.md"
DOCX_FILE="$DOCS_DIR/SkillLink-API-Documentation.docx"

mkdir -p "$DOCS_DIR"

# Ensure pandoc exists (macOS: try to install via Homebrew if missing)
if ! command -v pandoc >/dev/null 2>&1; then
  echo "Pandoc not found. Installing via Homebrew..."
  if ! command -v brew >/dev/null 2>&1; then
    echo "Homebrew is required to auto-install pandoc. Install Homebrew: https://brew.sh"
    exit 1
  fi
  brew install pandoc
fi

cat > "$MD_FILE" <<'EOF'
# SkillLink API Documentation

## Table of Contents
1. Authentication
2. Base URL & Environments
3. API Endpoints
   - Users
   - Courses
   - Enrollments
   - Admin
4. Data Models
5. Error Handling
6. Rate Limiting
7. Examples (cURL/JS)
8. Webhooks (Optional)
9. Versioning
10. Support

---

## 1) Authentication

- Scheme: Bearer JWT
- Header: Authorization: Bearer {accessToken}
- Access Token TTL: 3600s
- Refresh Token TTL: 7 days

Login
POST /api/auth/login
Request:
{
  "email": "user@example.com",
  "password": "password123"
}
Response 200:
{
  "accessToken": "eyJhbGc...",
  "refreshToken": "eyJhbGc...",
  "expiresIn": 3600,
  "user": { "id": "uuid", "email": "user@example.com", "role": "Student|Tutor|Admin", "name": "User Name" }
}

Refresh
POST /api/auth/refresh
Request:
{ "refreshToken": "eyJhbGc..." }
Response 200:
{ "accessToken": "eyJhbGc...", "expiresIn": 3600 }

Logout
POST /api/auth/logout
Body (optional):
{ "refreshToken": "..." }
Response 204

---

## 2) Base URL & Environments

- Local: http://localhost:5159/api
- Staging: https://staging-api.skilllink.com/api
- Production: https://api.skilllink.com/api

Default headers:
- Content-Type: application/json
- Accept: application/json

---

## 3) API Endpoints

### 3.1 Users

Get Me
GET /api/users/me
Auth: Required
200:
{ "id":"uuid","email":"user@skilllink.local","name":"John Doe","role":"Student","isActive":true,"createdAt":"2025-01-15T10:30:00Z","updatedAt":"2025-01-18T14:22:00Z" }

Update User
PUT /api/users/{userId}
Auth: Required
Body:
{ "name":"Jane Doe","bio":"Software Developer","avatar":"https://..." }
200:
{ "id":"uuid","name":"Jane Doe","bio":"Software Developer","avatar":"https://...","updatedAt":"2025-01-18T14:30:00Z" }

List Users (Admin)
GET /api/users?page=1&pageSize=20&role=Tutor&isActive=true&search=expert
Auth: Admin
200:
{
  "data":[{ "id":"uuid","email":"tutor@skilllink.local","name":"Expert Tutor","role":"Tutor","isActive":true,"createdAt":"2025-01-10T09:00:00Z" }],
  "pagination":{ "page":1,"pageSize":20,"totalCount":150,"totalPages":8 }
}

Change Role (Admin)
PATCH /api/users/{userId}/role
Body:
{ "role":"Tutor" }
200:
{ "id":"uuid","email":"user@skilllink.local","role":"Tutor","updatedAt":"2025-01-18T14:35:00Z" }

Set Active Status (Admin)
PATCH /api/users/{userId}/status
Body:
{ "isActive": true }
200:
{ "id":"uuid","email":"user@skilllink.local","isActive": true,"updatedAt":"2025-01-18T14:40:00Z" }

Delete User (Admin)
DELETE /api/users/{userId}
204

---

### 3.2 Courses

List Courses
GET /api/courses?page=1&pageSize=10&category=Web&search=React&difficulty=Beginner&sortBy=rating
200:
{
  "data":[{ "id":"course-id","title":"React Fundamentals","description":"...","tutor":{"id":"tutor-id","name":"Expert Dev","avatar":"https://..."},"category":"Web","difficulty":"Beginner","rating":4.8,"studentCount":245,"price":49.99,"createdAt":"2025-01-01T00:00:00Z" }],
  "pagination":{ "page":1,"pageSize":10,"totalCount":42,"totalPages":5 }
}

Get Course
GET /api/courses/{courseId}
200:
{ "id":"course-id","title":"React Fundamentals","description":"...","tutor":{...},"category":"Web","difficulty":"Beginner","rating":4.8,"studentCount":245,"price":49.99,"modules":[{ "id":"module-id","title":"Getting Started","lessonCount":5 }],"createdAt":"2025-01-01T00:00:00Z" }

Create Course (Tutor)
POST /api/courses
Auth: Tutor
Body:
{ "title":"Advanced React","description":"Master React patterns","category":"Web","difficulty":"Advanced","price":99.99 }
201:
{ "id":"new-course-id","title":"Advanced React","tutorId":"tutor-id","createdAt":"2025-01-18T14:45:00Z" }

Update Course (Tutor)
PUT /api/courses/{courseId}
Auth: Tutor
Body: { "title":"...", "description":"...", "price": 79.99 }
200: { "id":"course-id", "updatedAt":"..." }

Delete Course (Tutor/Admin)
DELETE /api/courses/{courseId}
204

---

### 3.3 Enrollments

Enroll (Student)
POST /api/enrollments
Auth: Student
Body:
{ "courseId":"course-id" }
201:
{ "id":"enrollment-id","studentId":"student-id","courseId":"course-id","enrolledAt":"2025-01-18T14:50:00Z","progress":0 }

My Courses (Student)
GET /api/enrollments/my-courses
Auth: Student
200:
{ "data":[{ "id":"enrollment-id","course":{ "id":"course-id","title":"React Fundamentals" },"progress":45,"enrolledAt":"2025-01-18T14:50:00Z" }] }

Course Students (Tutor/Admin)
GET /api/enrollments/course/{courseId}/students
Auth: Tutor/Admin
200:
{ "data":[{ "studentId":"student-id","studentName":"John Doe","email":"john@example.com","enrolledAt":"2025-01-18T14:50:00Z","progress":45 }] }

---

### 3.4 Admin

Dashboard Stats
GET /api/admin/dashboard/stats
Auth: Admin
200:
{ "totalUsers":1250,"totalTutors":85,"totalStudents":1165,"activeCourses":42,"totalEnrollments":3520,"revenue":125650.50,"avgCourseRating":4.6 }

User Activity Report
GET /api/admin/reports/user-activity?fromDate=2025-01-01&toDate=2025-01-18
Auth: Admin
200:
{ "data":[{ "date":"2025-01-18","newUsers":12,"activeUsers":450,"enrollments":35 }] }

Export Users CSV
GET /api/admin/users/export?format=csv&role=All&status=All
Auth: Admin
200: Binary (text/csv; attachment)

---

## 4) Data Models

User
{ "id":"uuid","email":"user@skilllink.local","name":"John Doe","role":"Student|Tutor|Admin","isActive":true,"avatar":"https://...","bio":"...","createdAt":"2025-01-15T10:30:00Z","updatedAt":"2025-01-18T14:22:00Z" }

Course
{ "id":"uuid","title":"string","description":"string","category":"string","difficulty":"Beginner|Intermediate|Advanced","price":49.99,"tutorId":"uuid","rating":4.8,"studentCount":245,"createdAt":"2025-01-01T00:00:00Z","updatedAt":"2025-01-18T10:00:00Z" }

Enrollment
{ "id":"uuid","studentId":"uuid","courseId":"uuid","progress":45,"enrolledAt":"2025-01-18T14:50:00Z","completedAt":null }

Auth Tokens
{ "accessToken":"string","refreshToken":"string","expiresIn":3600 }

---

## 5) Error Handling

Format
{ "error":{ "code":"UNAUTHORIZED","message":"Missing or invalid authentication token","details":"Token expired" },"timestamp":"2025-01-18T14:55:00Z","traceId":"0HN1GKDC5V0DQ:00000001" }

Common Codes
400 BAD_REQUEST
401 UNAUTHORIZED
403 FORBIDDEN
404 NOT_FOUND
409 CONFLICT
422 UNPROCESSABLE
429 RATE_LIMITED
500 INTERNAL_ERROR

---

## 6) Rate Limiting

- Public: 100 req/min
- Authenticated: 500 req/min
- Admin: 1000 req/min
Headers:
X-RateLimit-Limit, X-RateLimit-Remaining, X-RateLimit-Reset

---

## 7) Examples

cURL
curl -X POST http://localhost:5159/api/auth/login -H "Content-Type: application/json" -d '{"email":"admin@skilllink.local","password":"Admin@123"}'
curl -H "Authorization: Bearer TOKEN" http://localhost:5159/api/users/me

JavaScript (fetch)
const login = await fetch('http://localhost:5159/api/auth/login',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({email:'admin@skilllink.local',password:'Admin@123'})}); const {accessToken}=await login.json();
const me = await fetch('http://localhost:5159/api/users/me',{headers:{Authorization:`Bearer ${accessToken}`}}); const user=await me.json();

---

## 8) Webhooks (Optional)
Events: user.created, enrollment.completed, course.published, payment.processed
Payload:
{ "event":"enrollment.completed","data":{ "enrollmentId":"uuid","studentId":"uuid","courseId":"uuid","completedAt":"2025-01-18T15:00:00Z" },"timestamp":"2025-01-18T15:00:00Z" }

---

## 9) Versioning
- Current: v1
- Pattern: /api/v1/...
- Deprecation: 6 months notice

---

## 10) Support
- Status: https://status.skilllink.com
- Docs: https://docs.skilllink.com
- Email: api-support@skilllink.com

EOF

echo "Generating DOCX..."
pandoc "$MD_FILE" -o "$DOCX_FILE" --from=gfm --toc --highlight-style=tango

echo "Done. File saved at: $DOCX_FILE"
