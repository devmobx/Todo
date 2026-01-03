# 🗂️ API Routes

**Base URL:** `/api/v1/todos`

---

## 🟢 Core CRUD (Basic To-Do Operations)

| Method | Route            | Description                       |
| ------ | ---------------- | --------------------------------- |
| GET    | `/api/todos`     | Get all to-dos                    |
| GET    | `/api/todos/:id` | Get a specific to-do by ID        |
| POST   | `/api/todos`     | Create a new to-do                |
| PUT    | `/api/todos/:id` | Replace a to-do (full update)     |
| PATCH  | `/api/todos/:id` | Update specific fields of a to-do |
| DELETE | `/api/todos/:id` | Delete one to-do                  |
| DELETE | `/api/todos`     | Delete all to-dos                 |

---

## ✅ Completion & Status Handling

| Method | Route                        | Description                |
| ------ | ---------------------------- | -------------------------- |
| PATCH  | `/api/todos/:id/complete`    | Mark a to-do as completed  |
| PATCH  | `/api/todos/:id/uncomplete`  | Mark a to-do as incomplete |
| PATCH  | `/api/todos/:id/toggle`      | Toggle completion status   |
| GET    | `/api/todos?completed=true`  | Get all completed to-dos   |
| GET    | `/api/todos?completed=false` | Get all incomplete to-dos  |

---

## 🕒 Filtering, Sorting, and Searching

| Method | Route                                    | Description                  |
| ------ | ---------------------------------------- | ---------------------------- |
| GET    | `/api/todos?dueBefore=2025-12-01`        | Get to-dos due before a date |
| GET    | `/api/todos?dueAfter=2025-11-01`         | Get to-dos due after a date  |
| GET    | `/api/todos?priority=high`               | Filter by priority level     |
| GET    | `/api/todos/search?q=groceries`          | Search for to-dos by keyword |
| GET    | `/api/todos?sortBy=createdAt&order=desc` | Sort to-dos by creation date |

---

## 🏷️ Tags / Categories

| Method | Route                      | Description                        |
| ------ | -------------------------- | ---------------------------------- |
| GET    | `/api/todos/tags`          | Get all available tags             |
| GET    | `/api/todos?tag=work`      | Get all to-dos with a specific tag |
| POST   | `/api/todos/:id/tags`      | Add one or more tags to a to-do    |
| DELETE | `/api/todos/:id/tags/:tag` | Remove a specific tag from a to-do |

---

## 👥 User-Based Routes

| Method | Route                              | Description                        |
| ------ | ---------------------------------- | ---------------------------------- |
| GET    | `/api/users/:userId/todos`         | Get all to-dos for a specific user |
| POST   | `/api/users/:userId/todos`         | Create a to-do under a user        |
| GET    | `/api/users/:userId/todos/:todoId` | Get one of a user’s to-dos         |
| DELETE | `/api/users/:userId/todos`         | Delete all to-dos for a user       |

---

## 🔔 Reminders / Notifications

| Method | Route                                  | Description                |
| ------ | -------------------------------------- | -------------------------- |
| POST   | `/api/todos/:id/reminders`             | Add a reminder to a to-do  |
| GET    | `/api/todos/:id/reminders`             | Get reminders for a to-do  |
| DELETE | `/api/todos/:id/reminders/:reminderId` | Delete a specific reminder |

---

## 📊 Analytics / Meta

| Method | Route                | Description                           |
| ------ | -------------------- | ------------------------------------- |
| GET    | `/api/todos/stats`   | Get stats (e.g. completed vs pending) |
| GET    | `/api/todos/summary` | Get a short summary of all to-dos     |
| GET    | `/api/todos/recent`  | Get most recently updated to-dos      |
| GET    | `/api/todos/overdue` | Get to-dos past their due date        |

---

## 🧪 Experimental / Advanced Ideas

| Method | Route                           | Description                    |
| ------ | ------------------------------- | ------------------------------ |
| PATCH  | `/api/todos/:id/assign/:userId` | Assign a to-do to a user       |
| PATCH  | `/api/todos/:id/duplicate`      | Duplicate a to-do item         |
| POST   | `/api/todos/bulk`               | Create multiple to-dos at once |
| PATCH  | `/api/todos/bulk/complete`      | Mark multiple to-dos complete  |
| DELETE | `/api/todos/bulk`               | Delete multiple to-dos at once |
| GET    | `/api/todos/export`             | Export all to-dos (CSV/JSON)   |
