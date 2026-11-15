
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using ClienteApp.Core.Models;
using ClienteApp.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ClienteApp.Tests.Api;

public class ClientesApiTests
{
    [Fact]
    public async Task Registrar_ClienteValido_DebeRetornar201()
    {
        // Arrange
        using var factory = new ClienteApiFactory();
        using var client = factory.CreateClient();

        var solicitud = new
        {
            Nombre = "Ana López",
            Correo = $"ana_{Guid.NewGuid():N}@example.com"
        };

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/Clientes",
            solicitud);

        // Assert
        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var cliente = await response.Content
            .ReadFromJsonAsync<ClienteRespuesta>();

        Assert.NotNull(cliente);
        Assert.Equal(solicitud.Nombre, cliente.Nombre);
        Assert.Equal(solicitud.Correo, cliente.Correo);
    }

    [Fact]
    public async Task Registrar_NombreVacio_DebeRetornar400()
    {
        // Arrange
        using var factory = new ClienteApiFactory();
        using var client = factory.CreateClient();

        var solicitud = new
        {
            Nombre = "",
            Correo = $"correo_{Guid.NewGuid():N}@example.com"
        };

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/Clientes",
            solicitud);

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task ObtenerPorId_ClienteExistente_DebeRetornar200()
    {
        using var factory = new ClienteApiFactory();
        using var client = factory.CreateClient();

        Cliente clienteCreado;
        using (var scope = factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ClienteService>();
            clienteCreado = service.Registrar("Ana López", $"ana_{Guid.NewGuid():N}@example.com");
        }

        var response = await client.GetAsync($"/api/Clientes/{clienteCreado.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cliente = await response.Content.ReadFromJsonAsync<ClienteRespuesta>();
        Assert.NotNull(cliente);
        Assert.Equal(clienteCreado.Id, cliente.Id);
    }

    [Fact]
    public async Task ObtenerPorId_ClienteInexistente_DebeRetornar404()
    {
        using var factory = new ClienteApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/Clientes/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record ClienteRespuesta(
        int Id,
        string Nombre,
        string Correo);
}

public sealed class ClienteApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(
        Path.GetTempPath(),
        $"clientes_api_test_{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, configuration) =>
        {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:ClientesDb"] =
                        $"Data Source={_dbPath};Pooling=False"
                });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }   
}