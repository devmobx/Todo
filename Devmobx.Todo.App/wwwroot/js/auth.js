const API_BASE = '/v1/auth';

async function handleLogin(e) {
  e.preventDefault();

  const email = document.getElementById('email').value.trim();
  const password = document.getElementById('password').value;
  const rememberMe = document.getElementById('remember')?.checked || false;

  if (!validateEmail(email)) {
    showError('Please enter a valid email address');
    return;
  }

  try {
    console.log('🔐 Sending login request to /v1/login');

    const response = await fetch(`${API_BASE}/login`, {
      method: 'POST',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ email, password, rememberMe }),
    });

    console.log('📊 Login Response Status:', response.status);

    const data = await response.json();
    console.log('📋 Full Response Data:', data);

    if (!response.ok) {
      console.error('❌ Login failed:', data.message);
      showError(data.message || 'Login failed');
      return;
    }

    // DEBUG: Check what we got back
    console.log('🔍 Checking response properties:');
    console.log('  - data.token:', data.token ? '✅ Present' : '❌ Missing');
    console.log('  - data.user:', data.user ? '✅ Present' : '❌ Missing');
    console.log('  - data.success:', data.success);

    // Save token
    if (data.token) {
      console.log('💾 Saving token to localStorage');
      localStorage.setItem('authToken', data.token);

      // Verify it was saved
      const savedToken = localStorage.getItem('authToken');
      console.log(
        '✅ Token verification - localStorage now has:',
        savedToken ? '✅ Token Present' : '❌ Still Missing!'
      );
    } else {
      console.error('❌ ERROR: No token in response!');
      showError('Login failed - no token received from server');
      return;
    }

    // Save user
    if (data.user) {
      console.log('💾 Saving user to localStorage');
      localStorage.setItem('user', JSON.stringify(data.user));
    }

    showSuccess('Signed in successfully!');

    console.log('⏳ Waiting 1.5 seconds before redirect...');

    // Redirect after delay
    setTimeout(() => {
      console.log('🔄 Redirecting to dashboard...');
      window.location.href = '/';
    }, 1500);
  } catch (error) {
    console.error('❌ Login fetch error:', error);
    showError('Error signing in: ' + error.message);
  }
}

// Handle Registration
async function handleRegister(e) {
  e.preventDefault();

  const firstName = document.getElementById('firstName').value.trim();
  const lastName = document.getElementById('lastName').value.trim();
  const email = document.getElementById('email').value.trim();
  const password = document.getElementById('password').value;
  const confirmPassword = document.getElementById('confirmPassword').value;
  const birthDate = document.getElementById('birthDate').value;
  const agreeTerms = document.getElementById('agreeTerms').checked;

  // Validation
  if (!firstName || !lastName) {
    showError('First and last name are required');
    return;
  }

  if (!validateEmail(email)) {
    showError('Please enter a valid email address');
    return;
  }

  if (password.length < 8) {
    showError('Password must be at least 8 characters');
    return;
  }

  if (password !== confirmPassword) {
    showError('Passwords do not match');
    return;
  }

  if (!agreeTerms) {
    showError('You must agree to the terms and conditions');
    return;
  }

  try {
    const response = await fetch(`${API_BASE}/register`, {
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

    const data = await response.json();

    if (!response.ok) {
      showError(data.message || 'Registration failed');
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

// Handle Forgot Password Request
async function handleRequestReset(e) {
  e.preventDefault();

  const email = document.getElementById('email').value.trim();

  if (!validateEmail(email)) {
    showError('Please enter a valid email address');
    return;
  }

  try {
    const response = await fetch(`${API_BASE}/forgot-password`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ email }),
    });

    const data = await response.json();

    if (!response.ok) {
      showError(data.message || 'Failed to process request');
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

// Handle Password Reset
async function handleResetPassword(e) {
  e.preventDefault();

  const newPassword = document.getElementById('newPassword').value;
  const confirmPassword = document.getElementById('confirmNewPassword').value;
  const resetToken = document.getElementById('resetToken').value;

  if (newPassword.length < 8) {
    showError('Password must be at least 8 characters');
    return;
  }

  if (newPassword !== confirmPassword) {
    showError('Passwords do not match');
    return;
  }

  if (!resetToken) {
    showError('Invalid reset link');
    return;
  }

  try {
    const response = await fetch(`${API_BASE}/reset-password`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        token: resetToken,
        newPassword,
      }),
    });

    const data = await response.json();

    if (!response.ok) {
      showError(data.message || 'Failed to reset password');
      return;
    }

    document.getElementById('resetPasswordStep').style.display = 'none';
    document.getElementById('successStep').style.display = 'block';
    showSuccess('Password reset successfully!');
  } catch (error) {
    showError('Error: ' + error.message);
  }
}

// Utility Functions

function validateEmail(email) {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  return emailRegex.test(email);
}

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
