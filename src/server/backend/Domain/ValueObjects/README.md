# Value Objects (Objetos de Valor)

Los Value Objects son objetos inmutables definidos por sus atributos. Carecen de identidad propia y dos VOs se consideran iguales si todos sus atributos son iguales.

### Value Objects del Dominio

- **TicketId, UserId**: Se utilizan identificadores fuertemente tipados en lugar de tipos primitivos (como `Guid` o `int`). Esto previene errores comunes, como asignar un `UserId` donde se espera un `TicketId`.

- **Priority**: Representa la prioridad de un ticket (ej. `Low`, `Medium`, `High`). Encapsula la lógica de comparación entre prioridades.

- **Status**: Un enumerador (`enum`) que representa los estados válidos de un ticket (`Open`, `InProgress`, `Resolved`, `Closed`).

- **Reglas de Igualdad**: Todos los VOs implementan igualdad estructural. Por ejemplo, dos objetos `Priority` son iguales si ambos representan la misma prioridad (ej. `Priority.High == Priority.High`).
