const AUTH_API_BASE = '/api/auth';

document.addEventListener('DOMContentLoaded', () => {
  const loginForm = document.getElementById('loginForm');
  if (loginForm) {
    loginForm.addEventListener('submit', handleLogin);
  }

  const registerForm = document.getElementById('registerForm');
  if (registerForm) {
    registerForm.addEventListener('submit', handleRegister);
  }

  const requestResetForm = document.getElementById('requestResetForm');
  if (requestResetForm) {
    requestResetForm.addEventListener('submit', handleRequestReset);
  }

  const resetPasswordForm = document.getElementById('resetPasswordForm');
  if (resetPasswordForm) {
    resetPasswordForm.addEventListener('submit', handleResetPassword);
  }
});

async function handleLogin(e) {
  e.preventDefault();

  const email = document.getElementById('email').value.trim();
  const password = document.getElementById('password').value;
  const rememberMe = document.getElementById('remember')?.checked || false;

  try {
    const response = await fetch(`${AUTH_API_BASE}/login`, {
      method: 'POST',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ email, password, rememberMe }),
    });

    if (!response.ok) {
      const error = await response.json();
      showError(error.message || 'Login failed');
      return;
    }

    const data = await response.json();
    localStorage.setItem('token', data.token);
    localStorage.setItem('user', JSON.stringify(data.user));

    showSuccess('Signed in successfully!');
    setTimeout(() => {
      window.location.href = '/';
    }, 1500);
  } catch (error) {
    showError('Error signing in: ' + error.message);
  }
}

async function handleRegister(e) {
  e.preventDefault();

  const firstName = document.getElementById('firstName').value.trim();
  const lastName = document.getElementById('lastName').value.trim();
  const email = document.getElementById('email').value.trim();
  const password = document.getElementById('password').value;
  const confirmPassword = document.getElementById('confirmPassword').value;
  const birthDate = document.getElementById('birthDate').value;

  if (password !== confirmPassword) {
    showError('Passwords do not match');
    return;
  }

  try {
    const response = await fetch(`${AUTH_API_BASE}/register`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        firstName,
        lastName,
        email,
        password,
        birthDate,
      }),
    });

    if (!response.ok) {
      const error = await response.json();
      showError(error.message || 'Registration failed');
      return;
    }

    showSuccess('Account created! Redirecting to sign in...');
    setTimeout(() => {
      window.location.href = '/login';
    }, 1500);
  } catch (error) {
    showError('Error creating account: ' + error.message);
  }
}

async function handleRequestReset(e) {
  e.preventDefault();

  const email = document.getElementById('email').value.trim();

  try {
    const response = await fetch(`${AUTH_API_BASE}/forgot-password`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ email }),
    });

    if (!response.ok) {
      const error = await response.json();
      showError(error.message || 'Failed to process request');
      return;
    }

    document.getElementById('requestResetStep').style.display = 'none';
    document.getElementById('confirmationStep').style.display = 'block';
    document.getElementById('emailDisplay').textContent = email;
    showSuccess('Reset link sent to your email!');
  } catch (error) {
    showError('Error: ' + error.message);
  }
}

async function handleResetPassword(e) {
  e.preventDefault();

  const newPassword = document.getElementById('newPassword').value;
  const confirmPassword = document.getElementById('confirmPassword').value;
  const resetToken = document.getElementById('resetToken').value;

  if (newPassword !== confirmPassword) {
    showError('Passwords do not match');
    return;
  }

  try {
    const response = await fetch(`${AUTH_API_BASE}/reset-password`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        token: resetToken,
        newPassword,
      }),
    });

    if (!response.ok) {
      const error = await response.json();
      showError(error.message || 'Failed to reset password');
      return;
    }

    document.getElementById('resetPasswordStep').style.display = 'none';
    document.getElementById('successStep').style.display = 'block';
    showSuccess('Password reset successfully!');
  } catch (error) {
    showError('Error: ' + error.message);
  }
}

function showError(message) {
  const errorDiv = document.createElement('div');
  errorDiv.className = 'error';
  errorDiv.textContent = message;
  const mainContent = document.querySelector('.main-content');
  mainContent?.insertBefore(errorDiv, mainContent.firstChild);
  setTimeout(() => errorDiv.remove(), 5000);
}

function showSuccess(message) {
  const successDiv = document.createElement('div');
  successDiv.className = 'success';
  successDiv.textContent = message;
  const mainContent = document.querySelector('.main-content');
  mainContent?.insertBefore(successDiv, mainContent.firstChild);
  setTimeout(() => successDiv.remove(), 3000);
}
