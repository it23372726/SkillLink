---
title: "SkillLink API Documentation"
author: "SkillLink Team"
date: "2025-11-19"
toc: true
toc-depth: 3
---

# Overview

This document describes the SkillLink HTTP API across all modules: Authentication, Admin, Tutor Posts, Requests, Sessions, Skills, Feed (Reactions/Comments), Friendships, Notifications, Ratings, Feedback, and Reports. Unless otherwise stated, request and response bodies are JSON.

## 0) Base URL & Environments
- Local: http://localhost:5159/api
- Staging: https://staging-api.skilllink.com/api
- Production: https://api.skilllink.com/api

Common headers:
- Content-Type: application/json
- Accept: application/json
- Authorization: Bearer {JWT}

Auth notes:
- JWT required for protected endpoints. Role-based restrictions indicated per endpoint.

## HTTP Status Codes Reference

All endpoints return standard HTTP status codes:

| Code | Status | Description |
|------|--------|-------------|
| 200 | OK | Request succeeded, data returned |
| 201 | Created | Resource successfully created |
| 204 | No Content | Request succeeded, no body to return |
| 400 | Bad Request | Invalid input or malformed request |
| 401 | Unauthorized | Missing or invalid authentication token |
| 403 | Forbidden | Authenticated but insufficient permissions |
| 404 | Not Found | Requested resource does not exist |
| 409 | Conflict | Request conflicts with current state (e.g., duplicate) |
| 422 | Unprocessable Entity | Validation failed on request body |
| 500 | Internal Server Error | Unexpected server error |

Error responses include a `message` field:
```json
{
  "message": "Description of the error"
}
```

## Data Models & Schemas

### User
```json
{
  "id": 1,
  "email": "john@example.com",
  "fullName": "John Doe",
  "role": "Learner",
  "isActive": true,
  "readyToTeach": false,
  "profilePicture": "/uploads/profiles/123.jpg",
  "bio": "Passionate about learning",
  "createdAt": "2025-01-01T10:00:00Z",
  "updatedAt": "2025-01-15T10:00:00Z"
}
```

### TutorPost
```json
{
  "id": 1,
  "tutorId": 5,
  "title": "Algebra Basics",
  "description": "Learn fundamental algebra concepts",
  "maxParticipants": 10,
  "status": "Active",
  "imageUrl": "/uploads/posts/1.jpg",
  "createdAt": "2025-01-01T10:00:00Z"
}
```

### Request
```json
{
  "id": 1,
  "learnerId": 3,
  "skillName": "Algebra",
  "topic": "Quadratic Equations",
  "description": "Need help understanding",
  "preferredTutorId": 5,
  "isPrivate": false,
  "status": "Open",
  "createdAt": "2025-01-01T10:00:00Z"
}
```

### Rating
```json
{
  "id": 1,
  "tutorId": 5,
  "learnerId": 3,
  "acceptedRequestId": 42,
  "rating": 5,
  "comment": "Excellent tutor!",
  "createdAt": "2025-01-15T10:00:00Z"
}
```

### Notification
```json
{
  "id": 1,
  "userId": 3,
  "type": "request_accepted",
  "title": "Request Accepted",
  "body": "Your learning request has been accepted!",
  "link": "/requests/42",
  "isRead": false,
  "createdAt": "2025-01-01T10:00:00Z"
}
```

---
## 1) Authentication (AuthController)
Route prefix: `/api/auth`

### POST /api/auth/register (multipart/form-data)
Body (form fields):
- fullName: string (required)
- email: string (required)
- password: string (required)
- role: Learner|Tutor|Admin (optional; defaults to Learner)
- profilePicture: file (optional)

Responses:
- 200 OK { message }
- 400/409/500 with { message }

### GET /api/auth/verify-email?token=...
Responses: 200 OK { message } | 400 BadRequest { message }

### POST /api/auth/login
Request:
{ "email": "user@example.com", "password": "password123" }

Response 200:
{ "token": "eyJhbGc..." }
Errors: 401 { message: "Invalid credentials" }

### GET /api/auth/me (JWT)
Returns current user claims: { userId, fullName, email, role, ... }

### GET /api/auth/profile (JWT)
Profile object for current user.

### PUT /api/auth/profile (JWT)
Body: { fullName?, bio?, readyToTeach?, ... }
Returns: 200 OK { message }

### PUT /api/auth/profile/photo (JWT, multipart/form-data)
Uploads profile photo.
Returns: 200 OK { profilePicture: "/uploads/profiles/{file}" }

### DELETE /api/auth/profile/photo (JWT)
Removes photo. 200 OK { message }

### PUT /api/auth/teach-mode (JWT)
Body: { readyToTeach: boolean }
Response: 200 OK { message, readyToTeach }

### PUT /api/auth/active (JWT)
Body: { isActive: boolean }
Response: { message, isActive }

### DELETE /api/auth/users/{id} (JWT, Admin)
Deletes user (with safety constraints).
Responses: 204 NoContent | 400/401/403/404/409/500 { message }

#### Examples

Curl (login):
```bash
curl -s -X POST http://localhost:5159/api/auth/login \
 -H "Content-Type: application/json" \
 -d '{"email":"admin@skilllink.local","password":"Admin@123"}'
```

HTTPie (me):
```bash
http GET :5159/api/auth/me "Authorization:Bearer $TOKEN"
```

---
## 2) Admin (AdminController)
Route: `/api/admin` (JWT, Admin)

### GET /api/admin/users?q=...
List/filter users.

### PUT /api/admin/users/{id}/active
Body: { isActive: boolean }
Response: 200 { message }

### PUT /api/admin/users/{id}/role
Body: { role: "Learner"|"Tutor"|"Admin" }
Response: 200 { message }

---
## 3) Tutor Posts (TutorPostsController)
Route: `/api/tutor-posts` (JWT unless noted)

### POST /api/tutor-posts (JWT)
Body: { title: string, description?: string, maxParticipants: number }
Creates post for current tutor (tutorId inferred from JWT).
Response: 200 { message, postId }

### POST /api/tutor-posts/{postId}/image (multipart/form-data)
Body: file (IFormFile)
Response: 200 { url }

### GET /api/tutor-posts (Anonymous)
List all tutor posts.

### GET /api/tutor-posts/{postId} (Anonymous)
Get post detail.

### GET /api/tutor-posts/{postId}/accepted-status (JWT)
Returns { hasAccepted: bool } for current user.

### GET /api/tutor-posts/accepted-status?ids=1,2,3 (JWT)
Batch map of { postId: bool } for current user.

### POST /api/tutor-posts/{postId}/accept (JWT)
Accepts a post. Errors: 404/400 with { message }.

### PUT /api/tutor-posts/{postId}/schedule (JWT)
Body: { scheduleDate: ISO, meetingType?: string, meetingLink?: string }
Response: 200 { message }

### PUT /api/tutor-posts/{postId} (JWT)
Body: { title?, description?, maxParticipants?, status? }
Only owner tutor can update. 200 { message } | 403/404/400

### DELETE /api/tutor-posts/{postId} (JWT)
Only owner tutor can delete. 200 { message } | 403/404

#### Examples

List all (anonymous):
```bash
curl -s :5159/api/tutor-posts | jq '.[:3]'
```

Create (JWT):
```bash
http POST :5159/api/tutor-posts \
  Authorization:"Bearer $TOKEN" \
  title="Algebra Basics" description="Level 1" maxParticipants:=5
```

Accept (JWT):
```bash
curl -s -X POST :5159/api/tutor-posts/123/accept -H "Authorization: Bearer $TOKEN"
```

---
## 4) Requests (RequestsController)
Route: `/api/requests`

### GET /api/requests (Anonymous)
List public requests (enriched by current user if JWT present).

### GET /api/requests/search?q=...
Search requests by text (Anonymous).

### GET /api/requests/by-requestId/{id}
Get a single request. Private requests visible only to owner or targeted tutor.

### GET /api/requests/by-learnerId/{id}
All requests created by learner.

### POST /api/requests (JWT)
Body: Request model
- LearnerId enforced from JWT
- If PreferredTutorId is set, IsPrivate is set true and targeted tutor is notified
Response: 200 { message }

### PUT /api/requests/{id} (JWT)
Update request fields. 200 { message } | 404

### PATCH /api/requests/{id} (JWT)
Body: "status" (string)
Update status. 200 { message } | 404

### DELETE /api/requests/{id} (JWT)
Delete request. 200 { message } | 404

### POST /api/requests/{id}/accept (JWT)
Accept a request. For private requests, only targeted tutor can accept.
Response: 200 { message } | 400/404

### GET /api/requests/accepted (JWT)
Accepted requests for current user (as tutor or requester, depending on service semantics).

### GET /api/requests/{id}/accepted-status (JWT)
Returns { hasAccepted: bool } for current user.

### POST /api/requests/accepted/{id}/schedule (JWT)
Body: { scheduleDate: ISO, meetingType?: string, meetingLink?: string }
Only the accepting tutor can schedule. 200 { message } | 403/404/400

### GET /api/requests/accepted/requester (JWT)
For the current user, returns the requests they asked for with accepted meta.

### POST /api/requests/{id}/decline-directed (JWT)
Targeted tutor declines a private request (cancels targeting & notifies requester). 200 { message }

### POST /api/requests/{id}/decline (JWT)
Alias of decline-directed.

### POST /api/requests/accepted/{id}/complete (JWT)
Only accepting tutor can complete after scheduled time has passed. 200 { message } | 400/403/404

#### Examples

Search:
```bash
http GET :5159/api/requests/search q=="algebra"
```

Create (JWT):
```bash
curl -s -X POST :5159/api/requests \
 -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
 -d '{"skillName":"Algebra","description":"Need help"}'
```

Accept (JWT):
```bash
http POST :5159/api/requests/42/accept Authorization:"Bearer $TOKEN"
```

Schedule accepted (JWT):
```bash
curl -s -X POST :5159/api/requests/accepted/42/schedule \
 -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
 -d '{"scheduleDate":"2025-12-01T15:00:00Z","meetingType":"Online","meetingLink":"https://meet.example.com/abc"}'
```

---
## 5) Sessions (SessionsController)
Route: `/api/sessions`

### GET /api/sessions
List all sessions.

### GET /api/sessions/by-sessionId/{id}
Get one session.

### GET /api/sessions/by-tutorId/{id}
List sessions for tutor.

### POST /api/sessions
Body: Session model
Response: 200 { message }

### PATCH /api/sessions/{id}
Body: "status" (string). Response: 200 { message }

### DELETE /api/sessions/{id}
200 { message }

---
## 6) Skills (SkillsController)
Route: `/api/skills`

### POST /api/skills/add
Body: { userId, skillName, level? }
Response: 200 { message }

### DELETE /api/skills/{userId}/{skillId}
Response: 200 { message }

### GET /api/skills/user/{userId}
List skills for user.

### GET /api/skills/suggest?q=...
Autocomplete skill names.

### GET /api/skills/filter?skill=...
Users who have this skill.

#### Examples

Suggest:
```bash
http GET :5159/api/skills/suggest q=="alg"
```

---
## 7) Feed, Reactions, Comments (FeedController)
Route: `/api/feed` (JWT)

### GET /api/feed?page=1&pageSize=10&q=...
Get feed for current user; supports paging and search.

### POST /api/feed/sort
Body: [ { postType, id, title, ... } ]
Response: sorted list for current user context.

### POST /api/feed/{postType}/{postId}/like | /dislike
Set reaction for current user. 200 { message }

### DELETE /api/feed/{postType}/{postId}/reaction
Remove reaction. 200 { message }

### GET /api/feed/{postType}/{postId}/comments
List comments.

### POST /api/feed/{postType}/{postId}/comments
Body: { content: string }
Response: 200 { message }

#### Examples

Get feed (JWT):
```bash
curl -s :5159/api/feed?page=1&pageSize=10 -H "Authorization: Bearer $TOKEN"
```

Like a post:
```bash
http POST :5159/api/feed/tutor-post/123/like Authorization:"Bearer $TOKEN"
```

### DELETE /api/feed/comments/{commentId}
Owner or Admin can delete. 200 { message }

---
## 8) Friendships (FriendshipsController)
Route: `/api/friends` (JWT)

### GET /api/friends/followers
Followers of current user.

### POST /api/friends/{userId}/follow
Follow a user. 200 { message }

### DELETE /api/friends/{userId}/unfollow
Unfollow. 200 { message }

### GET /api/friends/my
Users I follow (friends).

### GET /api/friends/search?q=...
Search users (excluding self).

### GET /api/friends/{id}/followers | /following
Followers/Following for specified user (async endpoints).

#### Examples

Follow:
```bash
curl -s -X POST :5159/api/friends/987/follow -H "Authorization: Bearer $TOKEN"
```

Unfollow:
```bash
http DELETE :5159/api/friends/987/unfollow Authorization:"Bearer $TOKEN"
```

---
## 9) Notifications (NotificationsController)
Route: `/api/notifications` (JWT)

### GET /api/notifications
List my notifications.

### POST /api/notifications/{id}/read
Mark a notification read. 200 { message: "ok" }

### POST /api/notifications/read-all
Mark all my notifications read. 200 { message: "ok" }

#### Examples

List my notifications:
```bash
http GET :5159/api/notifications Authorization:"Bearer $TOKEN"
```

Read all:
```bash
curl -s -X POST :5159/api/notifications/read-all -H "Authorization: Bearer $TOKEN"
```

---
## 10) Ratings (RatingsController)
Route: `/api/ratings` (JWT unless noted)

### POST /api/ratings
Body: { acceptedRequestId: number, rating: 1..5, comment?: string }
Only the requesting learner can rate a completed accepted session.
Responses: 200 { message } | 400/403/404/409

### GET /api/ratings/exists/{acceptedRequestId}
Returns { exists: bool } for current user.

### GET /api/ratings/received (JWT)
For tutor, returns recent received ratings. ?limit=1..50

### GET /api/ratings/given (JWT)
For learner, returns recent given ratings. ?limit=1..50

### GET /api/ratings/summary/{tutorId} (Anonymous)
Aggregated stats for a tutor (avg, count, distribution...).

#### Examples

Create rating:
```bash
http POST :5159/api/ratings \
  Authorization:"Bearer $TOKEN" \
  acceptedRequestId:=42 rating:=5 comment="Great session"
```

Summary:
```bash
curl -s :5159/api/ratings/summary/123
```

---
## 11) Feedback (FeedbackController)
Route: `/api/feedback`

### POST /api/feedback (AllowAnonymous)
Body: { subject?: string, message: string }
Response: 201 Created { feedbackId }

### GET /api/feedback (Admin)
Query: isRead?: bool, limit?: int, offset?: int
Response: list of feedback items.

### PUT /api/feedback/{id}/read?isRead=true (Admin)
Marks feedback read/unread. 204 NoContent

#### Examples

Submit feedback (anon):
```bash
http POST :5159/api/feedback message="Loving the app!"
```

Admin list (JWT):
```bash
curl -s :5159/api/feedback?isRead=false -H "Authorization: Bearer $ADMIN_TOKEN"
```

---
## 12) Reports (AdminReportsController)
Route: `/api/admin/reports` (Admin)

### GET /api/admin/reports/skill-demand?from=YYYY-MM-DD&to=YYYY-MM-DD&limit=10
Returns top requested skills within window.

#### Example
```bash
http GET :5159/api/admin/reports/skill-demand from==2025-01-01 to==2025-12-31 limit==10 Authorization:"Bearer $ADMIN_TOKEN"
```

---
## 13) Data Models (selected)

User (shape varies across endpoints)
{ id, email, fullName, role, isActive, readyToTeach?, profilePicture?, createdAt?, updatedAt? }

TutorPost
{ id, tutorId, title, description, maxParticipants, status, imageUrl?, createdAt }

Request
{ id, learnerId, skillName, topic?, description?, preferredTutorId?, isPrivate, status, createdAt }

Session
{ id, tutorId, learnerId, topic?, status, createdAt, scheduleDate? }

Rating
{ id, tutorId, learnerId, acceptedRequestId, rating, comment, createdAt }

Notification
{ id, userId, type, title, body, link?, isRead, createdAt }

FeedbackItem
{ id, subject?, message, userId?, isRead, createdAt }

---
## 14) Errors & Status Codes
Standard error body:
{ "message": "..." }

Common statuses: 200 OK | 201 Created | 204 NoContent | 400 BadRequest | 401 Unauthorized | 403 Forbidden | 404 NotFound | 409 Conflict | 500 InternalServerError

---
## 15) Versioning & Support
- Current API version: v1 (implicit)
- Support: api-support@skilllink.com
- Status page: https://status.skilllink.com

---
## 16) Testing Instructions

For comprehensive testing guides, examples, and automation scripts, refer to **docs/API_TESTING_GUIDE.md**.

### Quick Start: Test Authentication

```bash
# 1. Register
curl -X POST http://localhost:5159/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "Test User",
    "email": "test@example.com",
    "password": "SecurePass123!"
  }'

# 2. Login
TOKEN=$(curl -s -X POST http://localhost:5159/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"SecurePass123!"}' \
  | jq -r '.token')

# 3. Get Current User
curl -H "Authorization: Bearer $TOKEN" http://localhost:5159/api/auth/me
```

### Test Tutor Posts

```bash
# List posts (public)
curl http://localhost:5159/api/tutor-posts

# Create post (requires Tutor role + JWT)
curl -X POST http://localhost:5159/api/tutor-posts \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Python Basics",
    "description": "Learn Python",
    "maxParticipants": 5
  }'
```

### Test Learning Requests

```bash
# Search requests
curl "http://localhost:5159/api/requests/search?q=algebra"

# Create request (requires JWT)
curl -X POST http://localhost:5159/api/requests \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "skillName": "Algebra",
    "description": "Need help"
  }'
```

### Test Error Handling

```bash
# Unauthorized (no token)
curl http://localhost:5159/api/auth/me
# Expected: 401 Unauthorized

# Not found
curl http://localhost:5159/api/tutor-posts/99999
# Expected: 404 Not Found

# Bad request (missing required field)
curl -X POST http://localhost:5159/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@test.com"}'
# Expected: 400 Bad Request

# Invalid rating (out of range)
curl -X POST http://localhost:5159/api/ratings \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"acceptedRequestId":1,"rating":10}'
# Expected: 422 Unprocessable Entity
```

### Recommended Tools

- **HTTPie**: Simpler than cURL for JSON APIs
  ```bash
  http POST :5159/api/auth/login email=test@example.com password=SecurePass123!
  ```

- **Postman**: Import `docs/SkillLink.postman_collection.json`
  - Set variables: `baseUrl` and `token`
  - Full request history and debugging

- **Swagger UI**: Available at `http://localhost:5159/swagger`
  - Interactive endpoint explorer
  - Try-it-out feature for testing

### See Also
- **API_TESTING_GUIDE.md**: Complete testing scenarios, automation scripts, and troubleshooting
- **openapi.json**: OpenAPI 3.0 specification (import into Swagger UI or Postman)
