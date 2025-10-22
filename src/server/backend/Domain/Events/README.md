# Eventos de Dominio

Los Eventos de Dominio representan algo significativo que ha ocurrido en el dominio y que es de interés para otras partes del sistema. Se nombran siempre en tiempo pasado.

### Mapeo con Contratos

Estos eventos tienen un **mapeo 1:1 con los eventos definidos en el paquete de `contracts`**. Esto asegura la consistencia entre el backend y otros sistemas (como el frontend o los workers), desacoplando la comunicación sin dejar de ser consistentes.

### Eventos Principales

- **TicketCreated**: Se dispara cuando un nuevo ticket es creado.
  - *Payload conceptual*: `TicketId`, `Title`, `Priority`, `CreatedAt`.

- **TicketAssigned**: Se dispara cuando un ticket es asignado a un agente.
  - *Payload conceptual*: `TicketId`, `AssigneeId`.

- **TicketStatusChanged**: Se dispara cuando el estado de un ticket cambia.
  - *Payload conceptual*: `TicketId`, `OldStatus`, `NewStatus`.
