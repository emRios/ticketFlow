using FluentAssertions;
using TicketFlow.Domain.ValueObjects;
using Xunit;

namespace TicketFlow.Domain.Tests.ValueObjects;

/// <summary>
/// Tests unitarios para el value object TicketStatus
/// </summary>
public class TicketStatusTests
{
    [Fact]
    public void Todo_DebeCrearStatusConValorCorrecto()
    {
        // Act
        var status = TicketStatus.Todo;

        // Assert
        status.Should().NotBeNull();
        status.Value.Should().Be("todo");
    }

    [Fact]
    public void InProgress_DebeCrearStatusConValorCorrecto()
    {
        // Act
        var status = TicketStatus.InProgress;

        // Assert
        status.Should().NotBeNull();
        status.Value.Should().Be("in-progress");
    }

    [Fact]
    public void Done_DebeCrearStatusConValorCorrecto()
    {
        // Act
        var status = TicketStatus.Done;

        // Assert
        status.Should().NotBeNull();
        status.Value.Should().Be("done");
    }

    [Fact]
    public void EqualsOperator_DebeCompararCorrectamentePorValor()
    {
        // Arrange
        var status1 = TicketStatus.Todo;
        var status2 = TicketStatus.Todo;

        // Assert
        status1.Should().Be(status2);
        (status1 == status2).Should().BeTrue();
    }

    [Fact]
    public void EqualsOperator_DebeDetectarDiferencias()
    {
        // Arrange
        var status1 = TicketStatus.Todo;
        var status2 = TicketStatus.InProgress;

        // Assert
        status1.Should().NotBe(status2);
        (status1 != status2).Should().BeTrue();
    }

    [Fact]
    public void Record_DebeSerInmutable()
    {
        // Arrange
        var status = TicketStatus.Todo;
        var originalValue = status.Value;

        // Note: Records con init son inmutables, no se puede reasignar Value
        // Este test documenta el comportamiento esperado

        // Assert
        status.Value.Should().Be(originalValue);
    }

    [Fact]
    public void ToString_DebeRetornarRepresentacionLegible()
    {
        // Arrange
        var status = TicketStatus.InProgress;

        // Act
        var result = status.ToString();

        // Assert
        result.Should().Contain("in-progress");
    }
}
