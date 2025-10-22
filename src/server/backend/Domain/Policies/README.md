# Políticas de Dominio

Las Políticas de Dominio encapsulan reglas de negocio complejas o procesos que no pertenecen de forma natural a una única entidad o agregado. Suelen orquestar acciones entre varias entidades.

### Políticas Implementadas

- **Política de Auto-Asignación v1**: 
  Esta política define la estrategia para asignar automáticamente un ticket a un agente disponible en el momento de la creación del ticket. La lógica puede basarse en la carga de trabajo actual de los agentes, sus habilidades, o un sistema de round-robin.

  La lógica detallada, los criterios y las reglas de negocio de esta estrategia se encuentran documentados en: `docs/assignment-strategy.md`.
