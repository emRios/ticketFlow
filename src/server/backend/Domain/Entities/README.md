# Entidades de Dominio

Las entidades son los objetos centrales del dominio. Tienen una identidad única que persiste a lo largo del tiempo y son responsables de encapsular y proteger sus invariantes de negocio.

### Entidades Principales

- **Ticket**: Es la entidad agregada raíz (Aggregate Root). Representa una solicitud o incidencia. 
  - *Campos conceptuales*: `Id`, `Title`, `Description`, `Priority`, `Status`, `AssigneeId`, `Tags`.
  - *Invariante clave*: El estado (`Ticket.Status`) no puede cambiar arbitrariamente. Solo puede transicionar siguiendo una máquina de estados predefinida (ej. `Open` -> `InProgress`). Cualquier cambio de estado debe ser a través de un método en la entidad (ej. `ticket.startProgress()`).

- **User**: Representa un usuario del sistema (cliente o agente).

- **Tag**: Etiquetas para categorizar tickets (ej. "Facturación", "Soporte Técnico").

- **AuditLog**: Registro de auditoría para cambios importantes en las entidades, especialmente en `Ticket`.
