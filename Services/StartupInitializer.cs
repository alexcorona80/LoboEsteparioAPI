using System.Reflection;
using LoboEsteparioAPI.Models.Mongo;

namespace LoboEsteparioAPI.Services;

/// <summary>
/// Tareas de arranque sobre MongoDB: índices, colección AIConfig con sus documentos iniciales y la plantilla HTML de VETA Supplier.
/// Corre en segundo plano y nunca detiene la API: si Mongo no responde, solo registra una advertencia.
/// </summary>
public sealed class StartupInitializer(IServiceScopeFactory scopeFactory, ILogger<StartupInitializer> logger) : BackgroundService
{
    private const string VetaTemplateName = "VETA Supplier · Presentación (HTML)";
    private const string VetaTemplateSubject = "Herramientas, refacciones y control de acceso para {Empresa}";
    private const string VetaTemplateResource = "veta-supplier-presentacion.html";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var sentEmails = scope.ServiceProvider.GetRequiredService<ISentEmailRepository>();
            var aiConfig = scope.ServiceProvider.GetRequiredService<IAIConfigRepository>();
            var templates = scope.ServiceProvider.GetRequiredService<IEmailTemplateRepository>();

            await sentEmails.EnsureIndexesAsync(stoppingToken);
            await aiConfig.EnsureSeedAsync(stoppingToken);

            // Primero las plantillas por defecto: SeedDefaultsIfEmpty solo corre con la colección vacía.
            await templates.SeedDefaultsIfEmptyAsync(stoppingToken);
            var html = ReadEmbedded(VetaTemplateResource);
            if (html is not null)
            {
                var created = await templates.EnsureAsync(
                    new EmailTemplateDocument { Name = VetaTemplateName, Subject = VetaTemplateSubject, Body = html },
                    stoppingToken);
                if (created)
                {
                    logger.LogInformation("Plantilla '{Name}' creada en EmailTemplates.", VetaTemplateName);
                }
            }
            else
            {
                logger.LogWarning("No se encontró el recurso embebido {Resource}.", VetaTemplateResource);
            }

            logger.LogInformation("Inicialización de MongoDB completada (AIConfig, índices y plantillas).");
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Apagado de la aplicación.
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo completar la inicialización de MongoDB; se reintentará al reiniciar.");
        }
    }

    private static string? ReadEmbedded(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var name = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
        if (name is null)
        {
            return null;
        }

        using var stream = assembly.GetManifestResourceStream(name);
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
