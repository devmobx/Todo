const API_BASE_URL = '/v1';
let todos = [];
let currentFilter = 'all';

// Initialize on page load
document.addEventListener('DOMContentLoaded', () => {
  loadTodos();
  setupEventListeners();
});

// Setup event listeners
function setupEventListeners() {
  // Create form
  document.getElementById('createTodoForm').addEventListener('submit', handleCreateTodo);

  // Filter buttons
  document.querySelectorAll('.filter-btn').forEach((btn) => {
    btn.addEventListener('click', (e) => {
      document
        .querySelectorAll('.filter-btn')
        .forEach((b) => b.classList.remove('active'));
      e.target.classList.add('active');
      currentFilter = e.target.dataset.filter;
      renderTodos();
    });
  });
}

// Load todos from API
async function loadTodos() {
  try {
    const response = await fetch(`${API_BASE_URL}/items`, {
      method: 'GET',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (response.status === 401) {
      showError('Please log in to view your todos');
      return;
    }

    if (!response.ok) throw new Error('Failed to load todos');

    todos = await response.json();
    renderTodos();
  } catch (error) {
    showError('Error loading todos: ' + error.message);
  }
}

// Handle create todo form submission
async function handleCreateTodo(e) {
  e.preventDefault();

  const title = document.getElementById('title').value.trim();
  const description = document.getElementById('description').value.trim();
  const priority = parseInt(document.getElementById('priority').value);
  const dueDate = document.getElementById('dueDate').value;
  const tagsInput = document.getElementById('tags').value.trim();

  // Parse tags
  const tags = tagsInput
    ? tagsInput
        .split(',')
        .map((t) => t.trim())
        .filter((t) => t)
        .slice(0, 5)
    : [];

  const todoData = {
    title,
    description,
    priority,
    dueDate: dueDate ? new Date(dueDate).toISOString() : null,
    tags: tags.length > 0 ? tags : null,
  };

  try {
    const response = await fetch(`${API_BASE_URL}/item`, {
      method: 'POST',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(todoData),
    });

    if (response.status === 401) {
      showError('Please log in to create todos');
      return;
    }

    if (!response.ok) throw new Error('Failed to create todo');

    const newTodo = await response.json();
    todos.push(newTodo);
    renderTodos();

    // Reset form
    document.getElementById('createTodoForm').reset();
    showSuccess('Todo created successfully!');
  } catch (error) {
    showError('Error creating todo: ' + error.message);
  }
}

// Render todos to DOM
function renderTodos() {
  const todoList = document.getElementById('todoList');

  let filteredTodos = todos;
  if (currentFilter === 'active') {
    filteredTodos = todos.filter((t) => !t.isCompleted);
  } else if (currentFilter === 'completed') {
    filteredTodos = todos.filter((t) => t.isCompleted);
  }

  if (filteredTodos.length === 0) {
    todoList.innerHTML = '<p class="empty">No todos yet. Create one to get started!</p>';
    return;
  }

  // Sort by priority (highest first)
  filteredTodos.sort((a, b) => b.priority - a.priority);

  todoList.innerHTML = filteredTodos.map((todo) => createTodoElement(todo)).join('');
}

// Create todo element HTML
function createTodoElement(todo) {
  const completed = todo.isCompleted ? 'completed' : '';
  const dueDate = todo.dueDate ? new Date(todo.dueDate) : null;
  const isOverdue = dueDate && dueDate < new Date() && !todo.isCompleted;
  const dueDateClass = isOverdue ? 'overdue' : '';
  const dueDateStr = dueDate ? formatDate(dueDate) : '';

  const tagsHtml =
    todo.tags && todo.tags.length > 0
      ? `<div class="tags">${todo.tags
          .map((tag) => `<span class="tag">${escapeHtml(tag)}</span>`)
          .join('')}</div>`
      : '';

  const dueDateHtml = dueDateStr
    ? `<div class="meta-item due-date ${dueDateClass}">📅 ${dueDateStr}</div>`
    : '';

  return `
    <div class="todo-item ${completed}" data-id="${todo.id}">
      <div class="todo-header">
        <div>
          <div class="todo-title">${escapeHtml(todo.title)}</div>
        </div>
        <span class="todo-priority priority-${todo.priority}">Priority: ${
    todo.priority
  }</span>
      </div>
      <div class="todo-description">${escapeHtml(todo.description)}</div>
      ${tagsHtml}
      <div class="todo-meta">
        ${dueDateHtml}
        <div class="meta-item">⏰ ${formatDate(new Date(todo.createdAt))}</div>
      </div>
      <div class="todo-actions">
        ${
          !todo.isCompleted
            ? `<button class="btn btn-success btn-small" onclick="toggleTodo('${todo.id}')">✓ Complete</button>`
            : `<button class="btn btn-secondary btn-small" onclick="toggleTodo('${todo.id}')">Undo</button>`
        }
        <button class="btn btn-danger btn-small" onclick="deleteTodo('${
          todo.id
        }')">Delete</button>
      </div>
    </div>
  `;
}

// Toggle todo completion status
async function toggleTodo(id) {
  const todo = todos.find((t) => t.id === id);
  if (!todo) return;

  try {
    const response = await fetch(`${API_BASE_URL}/item/${id}`, {
      method: 'PUT',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        isCompleted: !todo.isCompleted,
      }),
    });

    if (!response.ok) throw new Error('Failed to update todo');

    todo.isCompleted = !todo.isCompleted;
    renderTodos();
    showSuccess(
      todo.isCompleted ? 'Todo marked as complete!' : 'Todo marked as incomplete!'
    );
  } catch (error) {
    showError('Error updating todo: ' + error.message);
  }
}

// Delete todo
async function deleteTodo(id) {
  if (!confirm('Are you sure you want to delete this todo?')) return;

  try {
    const response = await fetch(`${API_BASE_URL}/item/${id}`, {
      method: 'DELETE',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) throw new Error('Failed to delete todo');

    todos = todos.filter((t) => t.id !== id);
    renderTodos();
    showSuccess('Todo deleted successfully!');
  } catch (error) {
    showError('Error deleting todo: ' + error.message);
  }
}

// Utility: Format date
function formatDate(date) {
  const now = new Date();
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const tomorrow = new Date(today);
  tomorrow.setDate(tomorrow.getDate() + 1);

  const dateObj = new Date(date.getFullYear(), date.getMonth(), date.getDate());

  if (dateObj.getTime() === today.getTime()) {
    return `Today at ${date.toLocaleTimeString('en-US', {
      hour: '2-digit',
      minute: '2-digit',
    })}`;
  } else if (dateObj.getTime() === tomorrow.getTime()) {
    return `Tomorrow at ${date.toLocaleTimeString('en-US', {
      hour: '2-digit',
      minute: '2-digit',
    })}`;
  }

  return date.toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

// Utility: Escape HTML
function escapeHtml(text) {
  const div = document.createElement('div');
  div.textContent = text;
  return div.innerHTML;
}

// Utility: Show error message
function showError(message) {
  const errorDiv = document.createElement('div');
  errorDiv.className = 'error';
  errorDiv.textContent = message;
  document
    .querySelector('.main-content')
    .insertBefore(errorDiv, document.querySelector('.main-content').firstChild);
  setTimeout(() => errorDiv.remove(), 5000);
}

// Utility: Show success message
function showSuccess(message) {
  const successDiv = document.createElement('div');
  successDiv.className = 'success';
  successDiv.textContent = message;
  document
    .querySelector('.main-content')
    .insertBefore(successDiv, document.querySelector('.main-content').firstChild);
  setTimeout(() => successDiv.remove(), 3000);
}
