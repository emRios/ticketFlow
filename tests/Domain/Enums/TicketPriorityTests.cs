using FluentAssertions;
using TicketFlow.Domain.Enums;
using Xunit;

namespace TicketFlow.Domain.Tests.Enums;

/// <summary>
/// Tests unitarios para el enum TicketPriority
/// </summary>
public class TicketPriorityTests
{
    [Fact]
    public void Low_DebeSerValor1()
    {
        // Assert
        TicketPriority.Low.Should().HaveValue(1);
        ((int)TicketPriority.Low).Should().Be(1);
    }

    [Fact]
    public void Medium_DebeSerValor2()
    {
        // Assert
        TicketPriority.Medium.Should().HaveValue(2);
        ((int)TicketPriority.Medium).Should().Be(2);
    }

    [Fact]
    public void High_DebeSerValor3()
    {
        // Assert
        TicketPriority.High.Should().HaveValue(3);
        ((int)TicketPriority.High).Should().Be(3);
    }

    [Fact]
    public void Critical_DebeSerValor4()
    {
        // Assert
        TicketPriority.Critical.Should().HaveValue(4);
        ((int)TicketPriority.Critical).Should().Be(4);
    }

    [Theory]
    [InlineData(TicketPriority.Low, TicketPriority.Medium, true)]
    [InlineData(TicketPriority.Medium, TicketPriority.High, true)]
    [InlineData(TicketPriority.High, TicketPriority.Critical, true)]
    [InlineData(TicketPriority.Critical, TicketPriority.Low, false)]
    [InlineData(TicketPriority.Low, TicketPriority.Low, false)]
    public void Comparacion_DebeFuncionarCorrectamente(
        TicketPriority first, 
        TicketPriority second, 
        bool expectedLessThan)
    {
        // Act & Assert
        (first < second).Should().Be(expectedLessThan);
    }

    [Fact]
    public void ToString_DebeRetornarNombreDelEnum()
    {
        // Assert
        TicketPriority.Low.ToString().Should().Be("Low");
        TicketPriority.Medium.ToString().Should().Be("Medium");
        TicketPriority.High.ToString().Should().Be("High");
        TicketPriority.Critical.ToString().Should().Be("Critical");
    }

    [Theory]
    [InlineData(1, TicketPriority.Low)]
    [InlineData(2, TicketPriority.Medium)]
    [InlineData(3, TicketPriority.High)]
    [InlineData(4, TicketPriority.Critical)]
    public void Cast_DebeFuncionarDesdeEntero(int value, TicketPriority expected)
    {
        // Act
        var priority = (TicketPriority)value;

        // Assert
        priority.Should().Be(expected);
    }

    [Fact]
    public void Orden_DebeMantenerJerarquiaDePrioridad()
    {
        // Arrange
        var priorities = new[]
        {
            TicketPriority.Critical,
            TicketPriority.Low,
            TicketPriority.High,
            TicketPriority.Medium
        };

        // Act
        var sorted = priorities.OrderByDescending(p => p).ToArray();

        // Assert
        sorted[0].Should().Be(TicketPriority.Critical);
        sorted[1].Should().Be(TicketPriority.High);
        sorted[2].Should().Be(TicketPriority.Medium);
        sorted[3].Should().Be(TicketPriority.Low);
    }

    [Fact]
    public void Parse_DebeFuncionarConNombres()
    {
        // Act & Assert
        Enum.Parse<TicketPriority>("Low").Should().Be(TicketPriority.Low);
        Enum.Parse<TicketPriority>("Medium").Should().Be(TicketPriority.Medium);
        Enum.Parse<TicketPriority>("High").Should().Be(TicketPriority.High);
        Enum.Parse<TicketPriority>("Critical").Should().Be(TicketPriority.Critical);
    }

    [Fact]
    public void Parse_DebeSerCaseSensitivePorDefecto()
    {
        // Act
        Action act = () => Enum.Parse<TicketPriority>("low");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Parse_DebeFuncionarIgnorandoCase()
    {
        // Act & Assert
        Enum.Parse<TicketPriority>("low", ignoreCase: true).Should().Be(TicketPriority.Low);
        Enum.Parse<TicketPriority>("CRITICAL", ignoreCase: true).Should().Be(TicketPriority.Critical);
    }

    [Fact]
    public void GetValues_DebeRetornarTodosLosValores()
    {
        // Act
        var values = Enum.GetValues<TicketPriority>();

        // Assert
        values.Should().HaveCount(4);
        values.Should().Contain(TicketPriority.Low);
        values.Should().Contain(TicketPriority.Medium);
        values.Should().Contain(TicketPriority.High);
        values.Should().Contain(TicketPriority.Critical);
    }
}
