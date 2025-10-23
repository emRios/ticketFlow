// ========== Config ==========
// @ts-ignore - Vite env variables
const API_BASE_URL = import.meta.env?.VITE_API_BASE_URL || 'http://localhost:5076';

// ========== Types ==========
interface User {
  userId: string;
  username: string;
  email: string;
  role: string;
}

interface RegisterUserRequest {
  email: string;
  name: string;
  role: string;
}

interface UpdateUserRequest {
  name?: string;
  email?: string;
  role?: string;
}

// ========== DOM Elements ==========
const userInfoEl = document.getElementById('user-info') as HTMLDivElement;
const btnBack = document.getElementById('btn-back') as HTMLButtonElement;
const formCreateUser = document.getElementById('form-create-user') as HTMLFormElement;
const inputName = document.getElementById('input-name') as HTMLInputElement;
const inputEmail = document.getElementById('input-email') as HTMLInputElement;
const inputRole = document.getElementById('input-role') as HTMLSelectElement;

const usersLoading = document.getElementById('users-loading') as HTMLDivElement;
const usersEmpty = document.getElementById('users-empty') as HTMLDivElement;
const usersContainer = document.getElementById('users-container') as HTMLDivElement;
const usersTbody = document.getElementById('users-tbody') as HTMLTableSectionElement;

// ========== State ==========
let currentUserName = localStorage.getItem('ticketflow_userName') || 'Admin';
let currentUserRole = localStorage.getItem('ticketflow_role') || 'ADMIN';

// ========== UI Setup ==========
userInfoEl.textContent = `👤 ${currentUserName} (${currentUserRole})`;

// ========== Navigation ==========
btnBack.addEventListener('click', () => {
  window.location.href = './index.html';
});

// ========== API Functions ==========
async function loadUsers(): Promise<void> {
  try {
    usersLoading.style.display = 'block';
    usersEmpty.style.display = 'none';
    usersContainer.style.display = 'none';

    const response = await fetch(`${API_BASE_URL}/api/users`, {
      method: 'GET',
      headers: { 'Content-Type': 'application/json' },
    });

    if (!response.ok) {
      throw new Error(`Error al cargar usuarios: ${response.statusText}`);
    }

    const users: User[] = await response.json();

    usersLoading.style.display = 'none';

    if (users.length === 0) {
      usersEmpty.style.display = 'block';
      return;
    }

    renderUsers(users);
    usersContainer.style.display = 'block';
  } catch (error) {
    console.error('Error loading users:', error);
    usersLoading.style.display = 'none';
    usersEmpty.style.display = 'block';
    alert('❌ Error al cargar usuarios. Revisa la consola.');
  }
}

async function createUser(request: RegisterUserRequest): Promise<void> {
  try {
    const response = await fetch(`${API_BASE_URL}/api/users`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`Error ${response.status}: ${errorText}`);
    }

    const newUser: User = await response.json();
    console.log('✅ Usuario creado:', newUser);

    alert(`✅ Usuario creado exitosamente:\n${newUser.username} (${newUser.email})`);
    
    // Reset form
    formCreateUser.reset();
    
    // Reload users list
    await loadUsers();
  } catch (error) {
    console.error('Error creating user:', error);
    alert(`❌ Error al crear usuario: ${error instanceof Error ? error.message : 'Error desconocido'}`);
  }
}

async function updateUser(id: string, updates: UpdateUserRequest): Promise<void> {
  try {
    const response = await fetch(`${API_BASE_URL}/api/users/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(updates),
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`Error ${response.status}: ${errorText}`);
    }

    const updatedUser: User = await response.json();
    console.log('✅ Usuario actualizado:', updatedUser);

    alert(`✅ Usuario actualizado exitosamente`);
    
    // Reload users list
    await loadUsers();
  } catch (error) {
    console.error('Error updating user:', error);
    alert(`❌ Error al actualizar usuario: ${error instanceof Error ? error.message : 'Error desconocido'}`);
  }
}

async function deleteUser(id: string, email: string): Promise<void> {
  if (!confirm(`¿Estás seguro de eliminar el usuario ${email}?\n\nEsta acción no se puede deshacer.`)) {
    return;
  }

  try {
    const response = await fetch(`${API_BASE_URL}/api/users/${id}`, {
      method: 'DELETE',
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`Error ${response.status}: ${errorText}`);
    }

    console.log('✅ Usuario eliminado:', email);
    alert(`✅ Usuario eliminado exitosamente`);
    
    // Reload users list
    await loadUsers();
  } catch (error) {
    console.error('Error deleting user:', error);
    alert(`❌ Error al eliminar usuario: ${error instanceof Error ? error.message : 'Error desconocido'}`);
  }
}

// ========== Render Functions ==========
function renderUsers(users: User[]): void {
  usersTbody.innerHTML = '';

  users.forEach(user => {
    const tr = document.createElement('tr');

    const tdName = document.createElement('td');
    tdName.textContent = user.username;
    tr.appendChild(tdName);

    const tdEmail = document.createElement('td');
    tdEmail.textContent = user.email;
    tr.appendChild(tdEmail);

    const tdRole = document.createElement('td');
    const roleBadge = document.createElement('span');
    roleBadge.className = `badge ${user.role.toLowerCase()}`;
    roleBadge.textContent = user.role;
    tdRole.appendChild(roleBadge);
    tr.appendChild(tdRole);

    const tdId = document.createElement('td');
    tdId.textContent = user.userId;
    tdId.style.fontFamily = 'monospace';
    tdId.style.fontSize = '12px';
    tdId.style.color = '#6b7280';
    tr.appendChild(tdId);

    // Actions column
    const tdActions = document.createElement('td');
    const actionsDiv = document.createElement('div');
    actionsDiv.className = 'action-buttons';

    // Edit button
    const btnEdit = document.createElement('button');
    btnEdit.className = 'btn-icon edit';
    btnEdit.textContent = '✏️ Editar';
    btnEdit.title = 'Editar usuario';
    btnEdit.addEventListener('click', () => handleEditUser(user));
    actionsDiv.appendChild(btnEdit);

    // Delete button (disabled for admin principal)
    const btnDelete = document.createElement('button');
    btnDelete.className = 'btn-icon delete';
    btnDelete.textContent = '🗑️ Eliminar';
    btnDelete.title = 'Eliminar usuario';
    if (user.email === 'admin@ticketflow.com') {
      btnDelete.disabled = true;
      btnDelete.style.opacity = '0.4';
      btnDelete.style.cursor = 'not-allowed';
      btnDelete.title = 'No se puede eliminar el administrador principal';
    } else {
      btnDelete.addEventListener('click', () => deleteUser(user.userId, user.email));
    }
    actionsDiv.appendChild(btnDelete);

    tdActions.appendChild(actionsDiv);
    tr.appendChild(tdActions);

    usersTbody.appendChild(tr);
  });
}

function handleEditUser(user: User): void {
  const newName = prompt('Nuevo nombre:', user.username);
  if (newName === null) return; // Cancelled

  const newEmail = prompt('Nuevo email:', user.email);
  if (newEmail === null) return; // Cancelled

  const newRole = prompt('Nuevo rol (ADMIN, AGENT, CLIENT):', user.role);
  if (newRole === null) return; // Cancelled

  // Validate
  if (!newName.trim() || !newEmail.trim() || !newRole.trim()) {
    alert('⚠️ Todos los campos son requeridos');
    return;
  }

  const validRoles = ['ADMIN', 'AGENT', 'CLIENT'];
  if (!validRoles.includes(newRole.toUpperCase())) {
    alert('⚠️ Rol inválido. Debe ser ADMIN, AGENT o CLIENT');
    return;
  }

  // Email validation
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  if (!emailRegex.test(newEmail)) {
    alert('⚠️ Por favor ingresa un email válido');
    return;
  }

  updateUser(user.userId, {
    name: newName.trim(),
    email: newEmail.trim(),
    role: newRole.toUpperCase()
  });
}

// ========== Event Handlers ==========
formCreateUser.addEventListener('submit', async (e) => {
  e.preventDefault();

  const name = inputName.value.trim();
  const email = inputEmail.value.trim();
  const role = inputRole.value;

  if (!name || !email || !role) {
    alert('⚠️ Por favor completa todos los campos');
    return;
  }

  // Email validation
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  if (!emailRegex.test(email)) {
    alert('⚠️ Por favor ingresa un email válido');
    return;
  }

  const request: RegisterUserRequest = { email, name, role };
  await createUser(request);
});

// ========== Initialize ==========
(async function init() {
  await loadUsers();
})();