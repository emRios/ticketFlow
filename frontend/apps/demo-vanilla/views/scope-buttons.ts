import { BoardScope } from '../api/board';
import { BoardData } from '../state/board-loader';

/**
 * Callback que se ejecuta cuando el scope cambia
 */
export type ScopeChangeCallback = (
  scope: BoardScope, 
  data: BoardData
) => void;

/**
 * Función para cargar datos del tablero
 */
export type LoadBoardFunction = (
  scope: BoardScope
) => Promise<BoardData>;

/**
 * Configura los botones de filtro por scope (assigned/team/all)
 * 
 * @param initialScope - Scope inicial seleccionado
 * @param onScopeChange - Callback cuando cambia el scope
 * @param loadBoardData - Función para cargar datos del tablero
 */
export function setupScopeButtons(
  initialScope: BoardScope,
  onScopeChange: ScopeChangeCallback,
  loadBoardData: LoadBoardFunction
): void {
  const buttons = document.querySelectorAll('.scope-btn');
  let currentScope = initialScope;
  
  buttons.forEach(btn => {
    btn.addEventListener('click', async (e) => {
      const button = e.target as HTMLButtonElement;
      const scope = button.getAttribute('data-scope') as BoardScope;
      
      // No hacer nada si ya está seleccionado
      if (scope === currentScope) return;
      
      // UI: Deshabilitar botones mientras carga
      setButtonsState(buttons, false);
      
      try {
        // Cargar datos del nuevo scope
        const data = await loadBoardData(scope);
        currentScope = scope;
        
        // Notificar cambio al componente padre
        onScopeChange(scope, data);
        
        // UI: Actualizar botón activo
        updateActiveButton(buttons, button);
        
      } catch (error) {
        console.error('[scope] Error al cambiar scope:', error);
        alert('Error al cargar tickets. Por favor intenta nuevamente.');
      } finally {
        // UI: Rehabilitar botones
        setButtonsState(buttons, true);
      }
    });
  });
}

/**
 * Helper: Habilita/deshabilita todos los botones
 */
function setButtonsState(buttons: NodeListOf<Element>, enabled: boolean): void {
  buttons.forEach(b => {
    (b as HTMLButtonElement).disabled = !enabled;
  });
}

/**
 * Helper: Actualiza qué botón está activo visualmente
 */
function updateActiveButton(
  buttons: NodeListOf<Element>, 
  activeButton: HTMLButtonElement
): void {
  buttons.forEach(b => b.classList.remove('active'));
  activeButton.classList.add('active');
}
