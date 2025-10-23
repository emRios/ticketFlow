using FluentAssertions;
using TicketFlow.Domain.Entities;
using Xunit;

namespace TicketFlow.Domain.Tests.Entities;

/// <summary>
/// Tests unitarios para la entidad User
/// </summary>
public class UserTests
{
    [Fact]
    public void Create_DebeCrearUsuarioConValoresPorDefecto()
    {
        // Arrange
        var email = "agente@ticketflow.com";
        var name = "Juan Pérez";

        // Act
        var user = User.Create(email, name);

        // Assert
        user.Should().NotBeNull();
        user.Id.Should().NotBeEmpty();
        user.Email.Should().Be(email);
        user.Name.Should().Be(name);
        user.Role.Should().Be("AGENT");
        user.IsActive.Should().BeTrue();
        user.LastAssignedAt.Should().BeNull();
    }

    [Fact]
    public void Create_DebeCrearUsuarioConRolPersonalizado()
    {
        // Arrange
        var email = "admin@ticketflow.com";
        var name = "Admin Principal";
        var role = "ADMIN";

        // Act
        var user = User.Create(email, name, role);

        // Assert
        user.Role.Should().Be("ADMIN");
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("ADMIN")]
    [InlineData("Admin")]
    public void Create_DebeNormalizarRolAMayusculas(string inputRole)
    {
        // Arrange
        var email = "test@test.com";
        var name = "Test User";

        // Act
        var user = User.Create(email, name, inputRole);

        // Assert
        user.Role.Should().Be("ADMIN");
    }

    [Theory]
    [InlineData("ADMIN")]
    [InlineData("AGENT")]
    [InlineData("CLIENT")]
    public void Create_DebeAceptarRolesValidos(string role)
    {
        // Arrange
        var email = "user@test.com";
        var name = "Test User";

        // Act
        var user = User.Create(email, name, role);

        // Assert
        user.Role.Should().Be(role);
    }

    [Theory]
    [InlineData("SUPERADMIN")]
    [InlineData("GUEST")]
    [InlineData("MANAGER")]
    [InlineData("")]
    [InlineData("INVALID")]
    public void Create_DebeLanzarExcepcionConRolInvalido(string invalidRole)
    {
        // Arrange
        var email = "user@test.com";
        var name = "Test User";

        // Act
        Action act = () => User.Create(email, name, invalidRole);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage($"Rol inválido: {invalidRole}*");
    }

    [Fact]
    public void Update_DebeActualizarNombreSiSeProvee()
    {
        // Arrange
        var user = User.Create("test@test.com", "Nombre Original", "AGENT");
        var nuevoNombre = "Nombre Actualizado";

        // Act
        user.Update(name: nuevoNombre);

        // Assert
        user.Name.Should().Be(nuevoNombre);
        user.Email.Should().Be("test@test.com"); // No debe cambiar
        user.Role.Should().Be("AGENT"); // No debe cambiar
    }

    [Fact]
    public void Update_DebeActualizarEmailSiSeProvee()
    {
        // Arrange
        var user = User.Create("original@test.com", "Test User", "AGENT");
        var nuevoEmail = "nuevo@test.com";

        // Act
        user.Update(email: nuevoEmail);

        // Assert
        user.Email.Should().Be(nuevoEmail);
        user.Name.Should().Be("Test User"); // No debe cambiar
        user.Role.Should().Be("AGENT"); // No debe cambiar
    }

    [Fact]
    public void Update_DebeActualizarRolSiSeProvee()
    {
        // Arrange
        var user = User.Create("test@test.com", "Test User", "AGENT");
        var nuevoRol = "ADMIN";

        // Act
        user.Update(role: nuevoRol);

        // Assert
        user.Role.Should().Be("ADMIN");
        user.Name.Should().Be("Test User"); // No debe cambiar
        user.Email.Should().Be("test@test.com"); // No debe cambiar
    }

    [Fact]
    public void Update_DebeActualizarMultiplesCamposSimultaneamente()
    {
        // Arrange
        var user = User.Create("original@test.com", "Nombre Original", "AGENT");

        // Act
        user.Update(
            name: "Nuevo Nombre",
            email: "nuevo@test.com",
            role: "ADMIN"
        );

        // Assert
        user.Name.Should().Be("Nuevo Nombre");
        user.Email.Should().Be("nuevo@test.com");
        user.Role.Should().Be("ADMIN");
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("agent")]
    [InlineData("client")]
    public void Update_DebeNormalizarRolAMayusculasAlActualizar(string inputRole)
    {
        // Arrange
        var user = User.Create("test@test.com", "Test User", "AGENT");

        // Act
        user.Update(role: inputRole);

        // Assert
        user.Role.Should().Be(inputRole.ToUpperInvariant());
    }

    [Theory]
    [InlineData("SUPERADMIN")]
    [InlineData("INVALID")]
    [InlineData("MANAGER")]
    public void Update_DebeLanzarExcepcionConRolInvalidoAlActualizar(string invalidRole)
    {
        // Arrange
        var user = User.Create("test@test.com", "Test User", "AGENT");

        // Act
        Action act = () => user.Update(role: invalidRole);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage($"Rol inválido: {invalidRole}*");
    }

    [Fact]
    public void Update_NoDebeActualizarSiTodosLosParametrosSonNull()
    {
        // Arrange
        var originalEmail = "test@test.com";
        var originalName = "Test User";
        var originalRole = "AGENT";
        var user = User.Create(originalEmail, originalName, originalRole);

        // Act
        user.Update(name: null, email: null, role: null);

        // Assert
        user.Email.Should().Be(originalEmail);
        user.Name.Should().Be(originalName);
        user.Role.Should().Be(originalRole);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_NoDebeActualizarConValoresVacios(string emptyValue)
    {
        // Arrange
        var originalEmail = "test@test.com";
        var originalName = "Test User";
        var user = User.Create(originalEmail, originalName, "AGENT");

        // Act
        user.Update(name: emptyValue, email: emptyValue);

        // Assert
        user.Email.Should().Be(originalEmail);
        user.Name.Should().Be(originalName);
    }

    [Fact]
    public void FlujoCompleto_CrearYActualizarUsuario()
    {
        // Arrange & Act - Crear usuario como agente
        var user = User.Create("agente@ticketflow.com", "Juan Pérez", "AGENT");

        // Assert - Verificar creación inicial
        user.Email.Should().Be("agente@ticketflow.com");
        user.Name.Should().Be("Juan Pérez");
        user.Role.Should().Be("AGENT");
        user.IsActive.Should().BeTrue();

        // Act - Promover a admin y actualizar datos
        user.Update(
            name: "Juan Pérez García",
            email: "juan.perez@ticketflow.com",
            role: "ADMIN"
        );

        // Assert - Verificar actualización
        user.Email.Should().Be("juan.perez@ticketflow.com");
        user.Name.Should().Be("Juan Pérez García");
        user.Role.Should().Be("ADMIN");
        user.Id.Should().NotBeEmpty(); // El ID no debe cambiar
    }

    [Fact]
    public void MultipleUpdates_DebeMantenerEstadoConsistente()
    {
        // Arrange
        var user = User.Create("test@test.com", "Original Name", "AGENT");

        // Act - Múltiples actualizaciones
        user.Update(name: "Primera Actualización");
        user.Update(email: "nuevo@test.com");
        user.Update(role: "ADMIN");
        user.Update(name: "Última Actualización");

        // Assert
        user.Name.Should().Be("Última Actualización");
        user.Email.Should().Be("nuevo@test.com");
        user.Role.Should().Be("ADMIN");
    }
}
