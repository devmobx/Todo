# 📘 Data Model Documentation

This document describes the data models used by the **To-Do API** (`/api/v1/todos`).

---

## 🗂️ TodoItem

Represents an individual to-do task.

| Field         | Type         | Description                                          |
| ------------- | ------------ | ---------------------------------------------------- |
| `id`          | `string`     | Unique identifier for the to-do item.                |
| `title`       | `string`     | Short, descriptive name of the task.                 |
| `description` | `string`     | Optional detailed notes about the task.              |
| `isComplete`  | `boolean`    | Indicates whether the task is completed.             |
| `createdAt`   | `datetime`   | Timestamp when the task was created.                 |
| `updatedAt`   | `datetime`   | Timestamp of the most recent update.                 |
| `dueDate`     | `datetime`   | Optional date/time the task is due.                  |
| `priority`    | `string`     | Priority level (`low`, `medium`, `high`).            |
| `tags`        | `Tag[]`      | List of tags or categories associated with the task. |
| `reminders`   | `Reminder[]` | List of reminders linked to this task.               |
| `userId`      | `string`     | Reference to the user who owns this task.            |

---

## 🏷️ Tag

Used to categorize or label to-do items.

| Field  | Type     | Description                                    |
| ------ | -------- | ---------------------------------------------- |
| `id`   | `string` | Unique identifier for the tag.                 |
| `name` | `string` | Tag label (e.g. `work`, `personal`, `urgent`). |

---

## 👥 User

Represents a person who owns or manages one or more to-do items.

| Field      | Type         | Description                            |
| ---------- | ------------ | -------------------------------------- |
| `id`       | `string`     | Unique identifier for the user.        |
| `username` | `string`     | Display name or handle for the user.   |
| `email`    | `string`     | Optional contact email.                |
| `todos`    | `TodoItem[]` | List of to-dos belonging to this user. |

---

## 🔔 Reminder

Represents a notification or scheduled alert for a to-do item.

| Field          | Type       | Description                               |
| -------------- | ---------- | ----------------------------------------- |
| `id`           | `string`   | Unique identifier for the reminder.       |
| `reminderTime` | `datetime` | When the reminder should trigger.         |
| `message`      | `string`   | Optional custom message for the reminder. |
| `todoId`       | `string`   | Reference to the related to-do item.      |

---

## 📊 Stats / Summary (Computed)

Computed dynamically for analytics endpoints (not stored in the database).

| Field           | Type         | Description                           |
| --------------- | ------------ | ------------------------------------- |
| `totalTodos`    | `number`     | Total count of all to-dos.            |
| `completed`     | `number`     | Number of completed to-dos.           |
| `pending`       | `number`     | Number of incomplete to-dos.          |
| `overdue`       | `number`     | Number of to-dos past their due date. |
| `recentUpdated` | `TodoItem[]` | List of recently updated items.       |

---

## ⚙️ Relationships Overview

| Entity     | Related To | Relationship Type | Description                          |
| ---------- | ---------- | ----------------- | ------------------------------------ |
| `User`     | `TodoItem` | One-to-Many       | Each user can have many to-dos.      |
| `TodoItem` | `Tag`      | Many-to-Many      | A to-do can have multiple tags.      |
| `TodoItem` | `Reminder` | One-to-Many       | A to-do can have multiple reminders. |

---

## 🧩 Example JSON

### TodoItem

```json
{
  "id": 1,
  "title": "Buy groceries",
  "description": "Milk, eggs, bread",
  "isComplete": false,
  "priority": "medium",
  "createdAt": "2025-11-10T10:00:00Z",
  "dueDate": "2025-11-11T18:00:00Z",
  "tags": [
    { "id": 1, "name": "personal" },
    { "id": 2, "name": "shopping" }
  ],
  "reminders": [
    { "id": 1, "reminderTime": "2025-11-11T09:00:00Z", "message": "Buy before noon" }
  ],
  "userId": 1
}
```
