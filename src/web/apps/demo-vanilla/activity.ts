import { apiFetch } from './api/apiClient';

function qs(sel: string) { return document.querySelector(sel) as HTMLElement; }

function getParam(name: string): string | null {
  const url = new URL(location.href);
  return url.searchParams.get(name);
}

type TicketResponse = {
  id: string;
  title: string;
  description: string;
  status: string;
  priority: string;
  assignedTo?: string | null;
  assignedToName?: string | null;
  createdAt: string;
  updatedAt?: string | null;
};

type ActivityItem = {
  action: string;
  occurredAt: string;
  performedBy?: string | null;
  performedByName?: string | null;
  correlationId?: string | null;
  title?: string | null;
  assigneeId?: string | null;
  reason?: string | null;
  oldStatus?: string | null;
  newStatus?: string | null;
  comment?: string | null;
};

async function loadTicket(id: string): Promise<TicketResponse> {
  return await apiFetch<TicketResponse>(`/api/tickets/${id}`, { method: 'GET' });
}

async function loadActivity(id: string): Promise<ActivityItem[]> {
  return await apiFetch<ActivityItem[]>(`/api/tickets/${id}/activity`, { method: 'GET' });
}

function getActionIcon(action: string): { icon: string; class: string } {
  switch (action) {
    case 'TicketCreated':
      return { icon: '🎫', class: 'created' };
    case 'TicketAssigned':
      return { icon: '👤', class: 'assigned' };
    case 'TicketStatusChanged':
      return { icon: '🔄', class: 'status' };
    default:
      return { icon: '📝', class: 'created' };
  }
}

function getActionLabel(action: string): string {
  switch (action) {
    case 'TicketCreated':
      return 'Ticket Creado';
    case 'TicketAssigned':
      return 'Ticket Asignado';
    case 'TicketStatusChanged':
      return 'Estado Cambiado';
    default:
      return action;
  }
}

function renderActivityDetails(activity: ActivityItem): { details: string; comment: string } {
  const details: string[] = [];
  let comment = '<span style="color: #9ca3af;">Sin comentarios</span>';

  // Para creación de ticket
  if (activity.action === 'TicketCreated' && activity.title) {
    details.push(`<div class="detail-item"><span class="detail-label">TÍTULO:</span> <span class="detail-value">${escapeHtml(activity.title)}</span></div>`);
    comment = '<span style="color: #9ca3af;">-</span>';
  }

  // Para asignación
  if (activity.action === 'TicketAssigned') {
    if (activity.assigneeId) {
      details.push(`<div class="detail-item"><span class="detail-label">Asignado a:</span> <span class="detail-value">${escapeHtml(activity.assigneeId)}</span></div>`);
    }
    if (activity.reason) {
      details.push(`<div class="detail-item"><span class="detail-label">Motivo:</span> <span class="detail-value">${escapeHtml(activity.reason)}</span></div>`);
    }
    // Comentario automático
    comment = `<div style="background: #f0f9ff; padding: 8px; border-radius: 4px; border-left: 3px solid #3b82f6;">
      <span class="detail-label">ACCIÓN AUTOMÁTICA:</span><br/>
      <span style="font-style: italic;">"Asignación de ticket, inicio de atención"</span>
    </div>`;
  }

  // Para cambios de estado - SEPARAR TRANSICIÓN (detalles) Y COMENTARIO
  if (activity.action === 'TicketStatusChanged') {
    const oldStatus = activity.oldStatus || 'Sin estado';
    const newStatus = activity.newStatus || 'Sin estado';
    
    // DETALLES: Mostrar solo la transición
    details.push(`<div class="detail-item">
      <span class="detail-label">TRANSICIÓN:</span><br/>
      <span class="detail-value" style="font-weight: 600; font-size: 13px;">
        ${escapeHtml(oldStatus)} → ${escapeHtml(newStatus)}
      </span>
    </div>`);

    // COMENTARIO: Determinar si es manual o automático
    if (activity.comment && activity.comment.trim() !== '') {
      // Comentario manual del usuario
      comment = `<div style="background: #f9fafb; padding: 8px; border-radius: 4px;">
        <span class="detail-label">COMENTARIO:</span><br/>
        <span style="font-style: italic;">"${escapeHtml(activity.comment)}"</span>
      </div>`;
    } else {
      // Comentario automático según la transición
      let autoComment = '';
      const oldLower = oldStatus.toLowerCase();
      const newLower = newStatus.toLowerCase();
      
      if (oldLower === 'nuevo' && newLower === 'en-proceso') {
        autoComment = 'Asignación de ticket, inicio de atención';
      } else if (newLower === 'en-espera') {
        autoComment = 'Ticket puesto en espera';
      } else if (newLower === 'resuelto') {
        autoComment = 'Ticket marcado como resuelto';
      } else if (newLower === 'cerrado') {
        autoComment = 'Ticket cerrado';
      } else {
        autoComment = `Cambio de estado: ${oldStatus} → ${newStatus}`;
      }
      
      comment = `<div style="background: #f0f9ff; padding: 8px; border-radius: 4px; border-left: 3px solid #3b82f6;">
        <span class="detail-label">ACCIÓN AUTOMÁTICA:</span><br/>
        <span style="font-style: italic;">"${escapeHtml(autoComment)}"</span>
      </div>`;
    }
  }

  return {
    details: details.length > 0 ? details.join('') : '<span style="color: #9ca3af;">-</span>',
    comment: comment
  };
}

function escapeHtml(text: string): string {
  const div = document.createElement('div');
  div.textContent = text;
  return div.innerHTML;
}

function formatDateTime(dateStr: string): string {
  const date = new Date(dateStr);
  return date.toLocaleString('es-ES', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit'
  });
}

function calculateDuration(startDate: string, endDate: string | null): string {
  const start = new Date(startDate);
  const end = endDate ? new Date(endDate) : new Date();
  
  const diffMs = end.getTime() - start.getTime();
  const diffSeconds = Math.floor(diffMs / 1000);
  const diffMinutes = Math.floor(diffSeconds / 60);
  const diffHours = Math.floor(diffMinutes / 60);
  const diffDays = Math.floor(diffHours / 24);
  
  if (diffDays > 0) {
    const hours = diffHours % 24;
    return `${diffDays}d ${hours}h`;
  } else if (diffHours > 0) {
    const minutes = diffMinutes % 60;
    return `${diffHours}h ${minutes}m`;
  } else if (diffMinutes > 0) {
    const seconds = diffSeconds % 60;
    return `${diffMinutes}m ${seconds}s`;
  } else {
    return `${diffSeconds}s`;
  }
}

function renderActivities(activities: ActivityItem[]) {
  const tbody = qs('#activity-tbody') as HTMLTableSectionElement;
  const loadingDiv = qs('#activity-loading');
  const emptyDiv = qs('#activity-empty');
  const containerDiv = qs('#activity-container');

  loadingDiv.style.display = 'none';

  if (activities.length === 0) {
    emptyDiv.style.display = 'block';
    containerDiv.style.display = 'none';
    return;
  }

  emptyDiv.style.display = 'none';
  containerDiv.style.display = 'block';

  tbody.innerHTML = activities.map((activity, index) => {
    const { icon, class: iconClass } = getActionIcon(activity.action);
    const actionLabel = getActionLabel(activity.action);
    const performer = activity.performedByName || (activity.performedBy === 'anonymous' ? 'Sistema' : activity.performedBy || 'Sistema');
    const dateTime = formatDateTime(activity.occurredAt);
    const { details, comment } = renderActivityDetails(activity);
    
    // Calcular duración en el estado (tiempo hasta el siguiente cambio de estado o hasta ahora)
    let duration = '-';
    if (activity.action === 'TicketStatusChanged' || activity.action === 'TicketCreated') {
      const nextActivity = activities.find((a, i) => i > index && a.action === 'TicketStatusChanged');
      const nextDate = nextActivity ? nextActivity.occurredAt : null;
      
      duration = calculateDuration(activity.occurredAt, nextDate);
      
      // Si es el último estado, mostrar "Actual"
      if (!nextDate && index === activities.length - 1) {
        duration = `<span style="color: #059669; font-weight: 600;">${duration}</span><br/><small style="color: #10b981;">(Estado actual)</small>`;
      }
    }

    return `
      <tr>
        <td>
          <div class="activity-icon ${iconClass}">${icon}</div>
        </td>
        <td>
          <span class="action-badge ${iconClass}">${actionLabel}</span>
        </td>
        <td style="white-space: nowrap;">${dateTime}</td>
        <td style="text-align: center;">${duration}</td>
        <td>${escapeHtml(performer)}</td>
        <td>${details}</td>
        <td>${comment}</td>
      </tr>
    `;
  }).join('');
}

async function init() {
  const id = getParam('id');
  if (!id) {
    alert('Falta el parámetro id del ticket');
    history.back();
    return;
  }

  // User info in header
  const uName = localStorage.getItem('ticketflow_username') || '';
  const uRole = localStorage.getItem('ticketflow_role') || '';
  const roleLabel = uRole.toUpperCase() === 'ADMIN' ? 'Administrador' : uRole.toUpperCase() === 'AGENT' ? 'Agente' : uRole;
  qs('#user-info').textContent = `${uName} (${roleLabel})`;

  // Back buttons
  (qs('#btn-back') as HTMLButtonElement).onclick = () => {
    location.href = `./ticket.html?id=${id}`;
  };
  
  (qs('#btn-back-board') as HTMLButtonElement).onclick = () => {
    location.href = './index.html';
  };

  try {
    // Load ticket info
    const ticket = await loadTicket(id);
    qs('#ticket-title').textContent = `Historial: ${ticket.title}`;
    qs('#ticket-id').textContent = `Ticket ID: ${ticket.id}`;

    // Load and render activities
    const activities = await loadActivity(id);
    renderActivities(activities);
  } catch (err) {
    console.error('[activity] Failed to load', err);
    qs('#activity-loading').innerHTML = '<p style="color:#ef4444;">❌ Error al cargar el historial</p>';
  }
}

init().catch(err => {
  console.error('[activity] init error', err);
  alert('No se pudo cargar el historial de actividad');
});
