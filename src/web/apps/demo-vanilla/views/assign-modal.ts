// Simple modal to reassign a ticket
// Creates a singleton modal in document.body and exposes open/close helpers

export type AssigneeOption = { id: string; name: string };

export type AssignModalTicket = {
  id: string;
  title: string;
  currentAssigneeName?: string; // if undefined or empty -> show "System"
};

export type OpenAssignModalArgs = {
  ticket: AssignModalTicket;
  getAssignees: () => Promise<AssigneeOption[]>;
  onTransfer: (assigneeId: string, note?: string) => Promise<void> | void;
};

let modalRoot: HTMLElement | null = null;
let styleInjected = false;

function ensureStyles() {
  if (styleInjected) return;
  const css = `
  .tf-modal-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.4); display: none; align-items: center; justify-content: center; z-index: 1000; }
  .tf-modal-overlay.show { display: flex; }
  .tf-modal { width: 720px; max-width: 95vw; background: #fff; border-radius: 8px; box-shadow: 0 10px 30px rgba(0,0,0,0.2); overflow: hidden; }
  .tf-modal-header { padding: 12px 16px; border-bottom: 1px solid #eee; font-weight: 600; font-size: 16px; }
  .tf-modal-body { padding: 16px; }
  .tf-form-row { display: flex; gap: 12px; align-items: center; margin-bottom: 12px; }
  .tf-form-row label { width: 110px; color: #444; font-size: 14px; }
  .tf-form-row .tf-text { flex: 1; padding: 8px 10px; border: 1px solid #e3e3e8; border-radius: 6px; background: #f9fafb; color: #333; }
  .tf-form-row select { flex: 1; padding: 8px 10px; border: 1px solid #e3e3e8; border-radius: 6px; background: #fff; }
  .tf-note { width: 100%; min-height: 120px; padding: 10px 12px; border: 1px solid #f0e6a0; background: #fffce8; border-radius: 6px; resize: vertical; }
  .tf-modal-footer { display: flex; justify-content: flex-end; gap: 8px; padding: 12px 16px; border-top: 1px solid #eee; background: #fafafa; }
  .tf-btn { padding: 8px 14px; border-radius: 6px; border: 1px solid #e3e3e8; cursor: pointer; font-weight: 500; }
  .tf-btn.primary { background: #3b82f6; border-color: #3b82f6; color: #fff; }
  .tf-btn.secondary { background: #fff; }
  `;
  const style = document.createElement('style');
  style.textContent = css;
  document.head.appendChild(style);
  styleInjected = true;
}

function ensureModalRoot() {
  if (modalRoot) return modalRoot;
  ensureStyles();
  // Reuse pre-existing root if present
  const existing = document.getElementById('tf-assign-modal-root');
  modalRoot = existing || document.createElement('div');
  modalRoot.className = 'tf-modal-overlay';
  modalRoot.innerHTML = `
    <div class="tf-modal" role="dialog" aria-modal="true" aria-labelledby="tf-assign-title">
      <div class="tf-modal-header" id="tf-assign-title">Reasignar ticket</div>
      <div class="tf-modal-body">
        <div class="tf-form-row">
          <label>Ticket</label>
          <div class="tf-text" id="tf-ticket-title"></div>
        </div>
        <div class="tf-form-row">
          <label>Asignado a</label>
          <div class="tf-text" id="tf-current-assignee"></div>
        </div>
        <div class="tf-form-row">
          <label>Reasignar a</label>
          <select id="tf-assignee-select"><option value="">Seleccionar agente…</option></select>
        </div>
        <div class="tf-form-row" style="align-items: start;">
          <label>Motivo</label>
          <textarea id="tf-assignment-note" class="tf-note" placeholder="Explica por qué transfieres este ticket (visible solo para el agente)"></textarea>
        </div>
      </div>
      <div class="tf-modal-footer">
        <button class="tf-btn secondary" id="tf-cancel">Cancelar</button>
        <button class="tf-btn primary" id="tf-transfer">Transferir</button>
      </div>
    </div>`;
  if (!existing) document.body.appendChild(modalRoot);
  modalRoot.addEventListener('click', (e) => {
    if (e.target === modalRoot) closeAssignModal();
  });
  return modalRoot;
}

export function openAssignModal(args: OpenAssignModalArgs) {
  const root = ensureModalRoot();
  const titleEl = root.querySelector('#tf-ticket-title') as HTMLElement;
  const currentAssigneeEl = root.querySelector('#tf-current-assignee') as HTMLElement;
  const selectEl = root.querySelector('#tf-assignee-select') as HTMLSelectElement;
  const noteEl = root.querySelector('#tf-assignment-note') as HTMLTextAreaElement;
  const cancelBtn = root.querySelector('#tf-cancel') as HTMLButtonElement;
  const transferBtn = root.querySelector('#tf-transfer') as HTMLButtonElement;

  // Fill content
  titleEl.textContent = `#${args.ticket.id} — ${args.ticket.title}`;
  currentAssigneeEl.textContent = args.ticket.currentAssigneeName && args.ticket.currentAssigneeName.trim().length > 0
    ? args.ticket.currentAssigneeName
    : 'System';

  // Reset select and note
  selectEl.innerHTML = '<option value="">Seleccionar agente…</option>';
  noteEl.value = '';

  // Load agents
  selectEl.disabled = true;
  args.getAssignees().then(list => {
    list.forEach(opt => {
      const o = document.createElement('option');
      o.value = opt.id;
      o.textContent = opt.name;
      selectEl.appendChild(o);
    });
  }).finally(() => {
    selectEl.disabled = false;
  });

  // Wire buttons
  cancelBtn.onclick = () => closeAssignModal();
  transferBtn.onclick = async () => {
    const assigneeId = selectEl.value;
    if (!assigneeId) { alert('Selecciona un agente'); return; }
    transferBtn.disabled = true;
    try {
      await args.onTransfer(assigneeId, noteEl.value || undefined);
      closeAssignModal();
    } finally {
      transferBtn.disabled = false;
    }
  };

  root.classList.add('show');
}

export function closeAssignModal() {
  if (!modalRoot) return;
  modalRoot.classList.remove('show');
}
