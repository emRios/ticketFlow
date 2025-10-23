using FluentAssertions;
using TicketFlow.Domain.Entities;
using TicketFlow.Domain.Events;
using Xunit;

namespace TicketFlow.Domain.Tests.Entities;

/// <summary>
/// Tests unitarios para la entidad Ticket
/// </summary>
public class TicketTests
{
    [Fact]
    public void Create_DebeCrearTicketConValoresPorDefecto()
    {
        // Arrange
        var title = "Problema con login";
        var description = "El usuario no puede iniciar sesión";

        // Act
        var ticket = Ticket.Create(title, description);

        // Assert
        ticket.Should().NotBeNull();
        ticket.Id.Should().NotBeEmpty();
        ticket.Title.Should().Be(title);
        ticket.Description.Should().Be(description);
        ticket.Status.Should().Be("nuevo");
        ticket.Priority.Should().Be("MEDIUM");
        ticket.AssignedTo.Should().BeNull();
        ticket.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        ticket.UpdatedAt.Should().BeNull();
        ticket.Version.Should().Be(0);
    }

    [Fact]
    public void Create_DebeCrearTicketConPrioridadPersonalizada()
    {
        // Arrange
        var title = "Error crítico";
        var description = "Sistema caído";
        var priority = "HIGH";

        // Act
        var ticket = Ticket.Create(title, description, priority);

        // Assert
        ticket.Priority.Should().Be("HIGH");
    }

    [Fact]
    public void Create_DebeNormalizarPrioridadAMayusculas()
    {
        // Arrange
        var title = "Test";
        var description = "Test description";
        var priority = "low";

        // Act
        var ticket = Ticket.Create(title, description, priority);

        // Assert
        ticket.Priority.Should().Be("LOW");
    }

    [Fact]
    public void Create_DebeGenerarEventoTicketCreated()
    {
        // Arrange
        var title = "Nuevo ticket";
        var description = "Descripción";
        var createdBy = "user-123";

        // Act
        var ticket = Ticket.Create(title, description, createdBy: createdBy);

        // Assert
        ticket.DomainEvents.Should().HaveCount(1);
        var evt = ticket.DomainEvents.First() as TicketCreatedEvent;
        evt.Should().NotBeNull();
        evt!.TicketId.Should().Be(ticket.Id);
        evt.Title.Should().Be(title);
        evt.PerformedBy.Should().Be(createdBy);
    }

    [Fact]
    public void ChangeStatus_DebeActualizarEstadoCorrectamente()
    {
        // Arrange
        var ticket = Ticket.Create("Test", "Description");
        var newStatus = "en-proceso";
        var performedBy = "agent-1";

        // Act
        ticket.ChangeStatus(newStatus, performedBy);

        // Assert
        ticket.Status.Should().Be("en-proceso");
        ticket.UpdatedAt.Should().NotBeNull();
        ticket.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ChangeStatus_DebeGenerarEventoStatusChanged()
    {
        // Arrange
        var ticket = Ticket.Create("Test", "Description");
        ticket.ClearDomainEvents(); // Limpiar evento de creación
        var oldStatus = ticket.Status;
        var newStatus = "resuelto";
        var performedBy = "agent-1";
        var comment = "Problema solucionado";

        // Act
        ticket.ChangeStatus(newStatus, performedBy, comment);

        // Assert
        ticket.DomainEvents.Should().HaveCount(1);
        var evt = ticket.DomainEvents.First() as TicketStatusChangedEvent;
        evt.Should().NotBeNull();
        evt!.TicketId.Should().Be(ticket.Id);
        evt.OldStatus.Should().Be(oldStatus);
        evt.NewStatus.Should().Be("resuelto");
        evt.PerformedBy.Should().Be(performedBy);
        evt.Comment.Should().Be(comment);
    }

    [Theory]
    [InlineData("nuevo", "nuevo")]
    [InlineData("en-proceso", "en-proceso")]
    [InlineData("enproceso", "en-proceso")]
    [InlineData("en_proceso", "en-proceso")]
    [InlineData("in-progress", "en-proceso")]
    [InlineData("inprogress", "en-proceso")]
    [InlineData("resuelto", "resuelto")]
    [InlineData("resolved", "resuelto")]
    [InlineData("closed", "resuelto")]
    [InlineData("en-espera", "en-espera")]
    [InlineData("enespera", "en-espera")]
    [InlineData("on-hold", "en-espera")]
    [InlineData("onhold", "en-espera")]
    public void ChangeStatus_DebeNormalizarEstadosCorrectamente(string input, string expected)
    {
        // Arrange
        var ticket = Ticket.Create("Test", "Description");

        // Act
        ticket.ChangeStatus(input);

        // Assert
        ticket.Status.Should().Be(expected);
    }

    [Fact]
    public void ChangeStatus_NoDebeHacerNadaConEstadoVacio()
    {
        // Arrange
        var ticket = Ticket.Create("Test", "Description");
        var originalStatus = ticket.Status;
        var originalUpdatedAt = ticket.UpdatedAt;

        // Act
        ticket.ChangeStatus("");

        // Assert
        ticket.Status.Should().Be(originalStatus);
        ticket.UpdatedAt.Should().Be(originalUpdatedAt);
    }

    [Fact]
    public void AssignTo_DebeAsignarTicketAAgente()
    {
        // Arrange
        var ticket = Ticket.Create("Test", "Description");
        var assigneeId = "agent-123";
        var performedBy = "admin-1";
        var reason = "Expertise en el área";

        // Act
        ticket.AssignTo(assigneeId, performedBy, reason);

        // Assert
        ticket.AssignedTo.Should().Be(assigneeId);
        ticket.UpdatedAt.Should().NotBeNull();
        ticket.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void AssignTo_DebeGenerarEventoTicketAssigned()
    {
        // Arrange
        var ticket = Ticket.Create("Test", "Description");
        ticket.ClearDomainEvents();
        var assigneeId = "agent-123";
        var performedBy = "admin-1";
        var reason = "Balanceo de carga";

        // Act
        ticket.AssignTo(assigneeId, performedBy, reason);

        // Assert
        ticket.DomainEvents.Should().HaveCount(1);
        var evt = ticket.DomainEvents.First() as TicketAssignedEvent;
        evt.Should().NotBeNull();
        evt!.TicketId.Should().Be(ticket.Id);
        evt.AssigneeId.Should().Be(assigneeId);
        evt.PerformedBy.Should().Be(performedBy);
        evt.Reason.Should().Be(reason);
    }

    [Fact]
    public void AssignTo_NoDebeHacerNadaConIdVacio()
    {
        // Arrange
        var ticket = Ticket.Create("Test", "Description");
        var originalAssignee = ticket.AssignedTo;
        var originalUpdatedAt = ticket.UpdatedAt;

        // Act
        ticket.AssignTo("");

        // Assert
        ticket.AssignedTo.Should().Be(originalAssignee);
        ticket.UpdatedAt.Should().Be(originalUpdatedAt);
    }

    [Fact]
    public void ClearDomainEvents_DebeEliminarTodosLosEventos()
    {
        // Arrange
        var ticket = Ticket.Create("Test", "Description");
        ticket.ChangeStatus("en-proceso");
        ticket.AssignTo("agent-1");

        // Act
        ticket.ClearDomainEvents();

        // Assert
        ticket.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void FlujoCompleto_CrearAsignarYCambiarEstado()
    {
        // Arrange
        var title = "Configurar servidor";
        var description = "Necesita configuración de firewall";
        var createdBy = "client-1";

        // Act - Crear
        var ticket = Ticket.Create(title, description, "HIGH", createdBy);
        
        // Act - Asignar
        ticket.AssignTo("agent-123", "admin-1", "Experto en redes");
        
        // Act - Cambiar estado
        ticket.ChangeStatus("en-proceso", "agent-123", "Iniciando trabajo");

        // Assert
        ticket.Id.Should().NotBeEmpty();
        ticket.Title.Should().Be(title);
        ticket.Priority.Should().Be("HIGH");
        ticket.AssignedTo.Should().Be("agent-123");
        ticket.Status.Should().Be("en-proceso");
        ticket.DomainEvents.Should().HaveCount(3); // Created + Assigned + StatusChanged
    }
}
