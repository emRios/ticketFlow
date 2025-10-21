// Entities
export type Ticket = { 
  ticketId: string; 
  title: string; 
  columnId: string; 
  order: number; 
  version: number;
  assigneeId?: string;
  tagIds?: string[];
};

export type Column = { id: string; name: string };

export type Tag = { id: string; label: string };

export type Assignee = { id: string; name: string };

// Commands
export type MoveTicketCmd = { ticketId: string; from: string; to: string; newIndex: number; byUserId?: string };
export type ReorderTicketCmd = { ticketId: string; columnId: string; newIndex: number; byUserId?: string };
export type AssignTicketCmd = { ticketId: string; assigneeId: string; byUserId?: string };
export type AddTagToTicketCmd = { ticketId: string; tagId: string; byUserId?: string };
export type RemoveTagFromTicketCmd = { ticketId: string; tagId: string; byUserId?: string };

// Domain Events (con auditoría)
export type DomainEvent =
  | { 
      type: 'TicketMoved'; 
      eventId: string;
      occurredAt: string;
      byUserId?: string;
      ticketId: string; 
      from: string; 
      to: string; 
      newIndex: number; 
      version: number;
    }
  | { 
      type: 'TicketReordered'; 
      eventId: string;
      occurredAt: string;
      byUserId?: string;
      ticketId: string; 
      columnId: string; 
      newIndex: number; 
      version: number;
    }
  | { 
      type: 'TicketAssigned'; 
      eventId: string;
      occurredAt: string;
      byUserId?: string;
      ticketId: string; 
      assigneeId: string; 
      version: number;
    }
  | { 
      type: 'TicketTagAdded'; 
      eventId: string;
      occurredAt: string;
      byUserId?: string;
      ticketId: string; 
      tagId: string; 
      version: number;
    }
  | { 
      type: 'TicketTagRemoved'; 
      eventId: string;
      occurredAt: string;
      byUserId?: string;
      ticketId: string; 
      tagId: string; 
      version: number;
    };