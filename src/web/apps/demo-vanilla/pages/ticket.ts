import { apiFetch } from '../api/apiClient';
import { listAgents } from '../api/users';
import { updateTicketStatus } from '../api/tickets';

function qs(sel: string) { return document.querySelector(sel) as HTMLElement; }
function qsi(sel: string) { return document.querySelector(sel) as HTMLInputElement; }
function qss(sel: string) { return document.querySelector(sel) as HTMLSelectElement; }

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

async function loadTicket(id: string): Promise<TicketResponse> {
  return await apiFetch<TicketResponse>(`/api/tickets/${id}`, { method: 'GET' });
}

function isAdmin(): boolean { return (localStorage.getItem('ticketflow_role') || '').toUpperCase() === 'ADMIN'; }
function isAgent(): boolean { return (localStorage.getItem('ticketflow_role') || '').toUpperCase() === 'AGENT'; }

async function init() {
  const id = getParam('id');
  if (!id) { alert('Falta el parámetro id'); history.back(); return; }

  // header user info
  const uName = localStorage.getItem('ticketflow_username') || '';
  const uRole = localStorage.getItem('ticketflow_role') || '';
  const roleLabel = uRole.toUpperCase() === 'ADMIN' ? 'Administrador' : uRole.toUpperCase() === 'AGENT' ? 'Agente' : uRole;
  qs('#user-info').textContent = `${uName} (${roleLabel})`;
  (qs('#btn-back-board') as HTMLButtonElement).onclick = () => location.href = './index.html';

  // load ticket
  const t = await loadTicket(id);
  qs('#t-title').textContent = t.title;
  qs('#t-id').textContent = t.id;
  qs('#t-status').textContent = t.status;
  qs('#t-priority').textContent = t.priority;
  qs('#t-assignee').textContent = t.assignedToName || t.assignedTo || 'Sin asignar';
  qs('#t-created').textContent = new Date(t.createdAt).toLocaleString();
  qs('#t-updated').textContent = t.updatedAt ? new Date(t.updatedAt).toLocaleString() : '-';
  qs('#t-desc').textContent = t.description;

  // History button - navigate to dedicated page
  (qs('#btn-history') as HTMLButtonElement).onclick = () => {
    location.href = `./activity.html?id=${id}`;
  };

  // Comments section - available for everyone
  const txtComment = document.querySelector('#txt-comment') as HTMLTextAreaElement;
  (qs('#btn-add-comment') as HTMLButtonElement).onclick = async () => {
    const comment = txtComment.value.trim();
    
    if (!comment) {
      return;
    }
    
    try {
      // Guardar comentario usando el endpoint de cambio de estado
      // pero manteniendo el estado actual (solo agrega el comentario al historial)
      const currentStatus = t.status;
      await updateTicketStatus(id, currentStatus, comment);
      
      txtComment.value = '';
      console.log('✅ Comentario guardado en el historial del ticket');
      
      // Mostrar feedback visual breve
      const btn = qs('#btn-add-comment') as HTMLButtonElement;
      const originalText = btn.textContent;
      btn.textContent = '✓ Guardado';
      btn.disabled = true;
      setTimeout(() => {
        btn.textContent = originalText;
        btn.disabled = false;
      }, 2000);
    } catch (err) {
      console.error('Error al agregar comentario:', err);
      alert('Error al guardar el comentario');
    }
  };

  // ADMIN: show reassign
  if (isAdmin()) {
    const adminCard = qs('#admin-actions');
    adminCard.style.display = 'block';
    const sel = qss('#sel-agent');
    sel.innerHTML = '<option value="">Selecciona agente…</option>';
    const agents = await listAgents();
    agents.forEach(a => { const opt = document.createElement('option'); opt.value = a.id; opt.textContent = a.name; sel.appendChild(opt); });
    (qs('#btn-transfer') as HTMLButtonElement).onclick = async () => {
      const assigneeId = sel.value; if (!assigneeId) { alert('Selecciona un agente'); return; }
      const reason = qsi('#txt-reason').value || undefined;
      await apiFetch<void>(`/api/tickets/${id}/assign`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ assigneeId, reason }) });
      alert('Ticket reasignado'); location.reload();
    };
  }

}

init().catch(err => {
  console.error('[ticket] init error', err);
  alert('No se pudo cargar el ticket');
});