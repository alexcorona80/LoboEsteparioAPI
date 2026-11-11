namespace LoboEsteparioAPI.Services;

/// <summary>
/// Categorías de búsqueda "virtuales" de empresas de TI. Overture no tiene un basic_category para ellas:
/// viven en la taxonomía (taxonomy.hierarchy), así que cada clave se traduce a una lista de valores de taxonomía.
/// Para ajustar qué negocios entran en cada categoría basta con editar las listas de este archivo.
/// Valores verificados contra el release 2026-10-21.0 (CDMX, places).
/// </summary>
public static class OvertureTaxonomyCategories
{
    public const string SoftwareServices = "it_software_services";
    public const string HardwareRepair = "it_hardware_repair";
    public const string TelecomInternet = "it_telecom_internet";

    private static readonly Dictionary<string, string[]> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        // Desarrollo de software, consultoría/servicios de TI, diseño y desarrollo web.
        [SoftwareServices] =
        [
            "information_technology_company",
            "software_development",
            "web_designer",
            "it_consultant",
        ],

        // Reparación y venta de equipo de cómputo, celulares y accesorios.
        [HardwareRepair] =
        [
            "it_service_and_computer_repair",
            "computer_store",
            "computer_hardware_company",
            "computer_wholesaler",
            "mobile_phone_store",
            "mobile_phone_accessory_store",
            "mobile_phone_repair",
            "electronics_repair_shop",
        ],

        // Proveedores de internet, telecomunicaciones y hosting.
        [TelecomInternet] =
        [
            "telecommunications_company",
            "telecommunications_service",
            "internet_service_provider",
            "web_hosting_service",
            "telephone_service",
        ],
    };

    /// <summary>Devuelve los valores de taxonomía de la categoría, o false si no es una categoría de TI (se usa basic_category).</summary>
    public static bool TryGetValues(string? category, out IReadOnlyList<string> values)
    {
        if (!string.IsNullOrWhiteSpace(category) && Map.TryGetValue(category.Trim(), out var found))
        {
            values = found;
            return true;
        }

        values = [];
        return false;
    }
}
