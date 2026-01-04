const API_BASE = '/v1';

console.log('✅ main.js loaded');

// Check if user is authenticated
function checkAuthentication() {
  console.log('🔐 Checking authentication...');
  const token = localStorage.getItem('authToken');
  const user = localStorage.getItem('user');

  if (!token || !user) {
    // console.log('❌ Not authenticated - clearing storage');
    // localStorage.removeItem('authToken');
    // localStorage.removeItem('user');
    // window.location.href = '/login';
    return false;
  }

  const userData = JSON.parse(user);
  console.log('✅ User logged in:', userData);
  return true;
}

// Load Tasks
async function loadTasks() {
  try {
    const token = localStorage.getItem('authToken');

    if (!token) {
      // console.error('❌ No token available');
      // showError('Session expired - please login again');
      // localStorage.removeItem('authToken');
      // localStorage.removeItem('user');
      // // Don't redirect - let user click logout
      return;
    }

    console.log('📥 Loading tasks with token');

    const response = await fetch(`${API_BASE}/items`, {
      method: 'GET',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
    });

    console.log('📊 Response Status:', response.status);

    if (response.status === 401) {
      console.error('🔐 Unauthorized - token invalid');
      showError('Session expired - please sign in again');
      localStorage.removeItem('authToken');
      localStorage.removeItem('user');
      // Redirect after delay - gives user time to see error
      setTimeout(() => {
        window.location.href = '/login';
      }, 2000);
      return;
    }

    if (response.status === 429) {
      console.error('⚠️ Rate limited - backing off');
      showError('Too many requests - please wait');
      return;
    }

    if (!response.ok) {
      const errorText = await response.text();
      console.error('❌ API Error:', response.status, errorText);
      showError(`API Error: ${response.status}`);
      return;
    }

    const tasks = await response.json();
    console.log('✅ Tasks loaded:', tasks);
    renderTasks(tasks);
  } catch (error) {
    console.error('❌ Fetch error:', error);
    showError('Failed to load tasks');
  }
}

// Initialize Dashboard
async function initializeDashboard() {
  console.log('🚀 Initializing dashboard...');

  // Check auth first
  if (!checkAuthentication()) {
    return; // Stop if not authenticated
  }

  // Only load tasks if authenticated
  await loadTasks();
}

// Render Tasks
function renderTasks(tasks) {
  const container = document.getElementById('tasksContainer');

  if (!tasks || tasks.length === 0) {
    container.innerHTML = `<div class="empty-state"><div class="empty-icon">✨</div><h2>No tasks yet</h2><p>Create your first task to get started</p><button class="btn btn-primary" onclick="showNewTaskModal()">Create Task</button></div>`;
    document.getElementById('taskCount').textContent = '0 tasks';
    return;
  }

  container.innerHTML = tasks.map((task) => createTaskCard(task)).join('');
  document.getElementById('taskCount').textContent = `${tasks.length} task${
    tasks.length !== 1 ? 's' : ''
  }`;
}

// Create Task Card
function createTaskCard(task) {
  const priorityLabels = { 1: 'Low', 2: 'Medium', 3: 'Normal', 4: 'High', 5: 'Urgent' };
  const dueDate = task.dueDate ? new Date(task.dueDate).toLocaleDateString() : null;
  const tagsHtml =
    task.tags && task.tags.length > 0
      ? task.tags.map((tag) => `<span class="tag">${escapeHtml(tag)}</span>`).join('')
      : '';

  return `
    <div class="task-card ${task.isCompleted ? 'completed' : ''}">
      <div class="task-header">
        <h3 class="task-title">${escapeHtml(task.title)}</h3>
        <span class="task-priority priority-${task.priority}">${
    priorityLabels[task.priority]
  }</span>
      </div>
      <p class="task-description">${escapeHtml(task.description)}</p>
      <div class="task-meta">
        ${dueDate ? `<span>📅 ${dueDate}</span>` : ''}
        <span>${
          task.reminders
            ? `⏰ ${task.reminders.length} reminder${
                task.reminders.length !== 1 ? 's' : ''
              }`
            : ''
        }</span>
      </div>
      ${tagsHtml ? `<div class="task-tags">${tagsHtml}</div>` : ''}
      <div class="task-actions">
        <button class="task-btn" onclick="completeTask('${task.id}')">✅ Complete</button>
        <button class="task-btn" onclick="editTask('${task.id}')">✏️ Edit</button>
        <button class="task-btn" onclick="deleteTask('${task.id}')">🗑️ Delete</button>
      </div>
    </div>
  `;
}

// Modal Functions
function showNewTaskModal() {
  document.getElementById('taskModal').style.display = 'flex';
  document.getElementById('taskForm').reset();
}

function closeTaskModal() {
  document.getElementById('taskModal').style.display = 'none';
}

// Create Task
async function handleCreateTask(e) {
  e.preventDefault();

  const title = document.getElementById('taskTitle').value.trim();
  const description = document.getElementById('taskDescription').value.trim();
  const priority = parseInt(document.getElementById('taskPriority').value);
  const dueDate = document.getElementById('taskDueDate').value;
  const tags = document
    .getElementById('taskTags')
    .value.split(',')
    .map((t) => t.trim())
    .filter((t) => t);

  if (!title) {
    showError('Task title is required');
    return;
  }

  try {
    const token = localStorage.getItem('authToken');
    const response = await fetch(`${API_BASE}/item`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        title,
        description,
        priority,
        dueDate: dueDate || null,
        tags,
      }),
    });

    if (!response.ok) {
      const error = await response.json();
      showError(error.message || 'Failed to create task');
      return;
    }

    showSuccess('Task created successfully!');
    closeTaskModal();
    await loadTasks();
  } catch (error) {
    showError('Error creating task: ' + error.message);
  }
}

// Complete Task
async function completeTask(taskId) {
  try {
    const token = localStorage.getItem('authToken');
    const response = await fetch(`${API_BASE}/item/${taskId}`, {
      method: 'PUT',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ isCompleted: true }),
    });

    if (!response.ok) {
      showError('Failed to complete task');
      return;
    }

    showSuccess('Task completed!');
    await loadTasks();
  } catch (error) {
    showError('Error: ' + error.message);
  }
}

// Edit Task
async function editTask(taskId) {
  showInfo('Task editing coming soon!');
}

// Delete Task
async function deleteTask(taskId) {
  if (!confirm('Delete this task?')) return;

  try {
    const token = localStorage.getItem('authToken');
    const response = await fetch(`${API_BASE}/item/${taskId}`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${token}` },
    });

    if (!response.ok) {
      showError('Failed to delete task');
      return;
    }

    showSuccess('Task deleted!');
    await loadTasks();
  } catch (error) {
    showError('Error: ' + error.message);
  }
}

// List Modal
function showNewListModal() {
  alert('Create new list - coming soon!');
}

// Logout
function handleLogout() {
  window.location.href = '/MicrosoftIdentity/Account/SignOut';
}

// Alert Functions
function showError(message) {
  const alertContainer = document.getElementById('alertContainer');
  const alertDiv = document.createElement('div');
  alertDiv.className = 'alert alert-error';
  alertDiv.textContent = message;
  alertContainer.insertBefore(alertDiv, alertContainer.firstChild);
  setTimeout(() => alertDiv.remove(), 5000);
}

function showSuccess(message) {
  const alertContainer = document.getElementById('alertContainer');
  const alertDiv = document.createElement('div');
  alertDiv.className = 'alert alert-success';
  alertDiv.textContent = message;
  alertContainer.insertBefore(alertDiv, alertContainer.firstChild);
  setTimeout(() => alertDiv.remove(), 3000);
}

function showInfo(message) {
  const alertContainer = document.getElementById('alertContainer');
  const alertDiv = document.createElement('div');
  alertDiv.className = 'alert alert-info';
  alertDiv.textContent = message;
  alertContainer.insertBefore(alertDiv, alertContainer.firstChild);
  setTimeout(() => alertDiv.remove(), 3000);
}

// Utilities
function escapeHtml(text) {
  const div = document.createElement('div');
  div.textContent = text;
  return div.innerHTML;
}

window.addEventListener('click', (e) => {
  const modal = document.getElementById('taskModal');
  if (e.target === modal) closeTaskModal();
});

// Initialize on page load
document.addEventListener('DOMContentLoaded', function () {
  console.log('Page loaded');
  initializeDashboard();
  const form = document.getElementById('taskForm');
  if (form) {
    form.addEventListener('submit', handleCreateTask);
  }
});
