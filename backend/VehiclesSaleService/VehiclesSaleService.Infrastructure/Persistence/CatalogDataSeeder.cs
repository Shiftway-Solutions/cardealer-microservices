using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VehiclesSaleService.Domain.Entities;

namespace VehiclesSaleService.Infrastructure.Persistence;

/// <summary>
/// Seeds the vehicle catalog (makes, models) if the tables are empty.
/// Runs once at startup — idempotent (checks before inserting).
/// </summary>
public static class CatalogDataSeeder
{
    // Popular makes in Dominican Republic market
    private static readonly (string Name, string Country, bool IsPopular)[] Makes =
    [
        ("Toyota",        "Japan",   true),
        ("Honda",         "Japan",   true),
        ("Hyundai",       "Korea",   true),
        ("Nissan",        "Japan",   true),
        ("Kia",           "Korea",   true),
        ("Mazda",         "Japan",   true),
        ("Ford",          "USA",     true),
        ("Chevrolet",     "USA",     true),
        ("Jeep",          "USA",     true),
        ("BMW",           "Germany", false),
        ("Mercedes-Benz", "Germany", false),
        ("Audi",          "Germany", false),
        ("Volkswagen",    "Germany", false),
        ("Lexus",         "Japan",   false),
        ("Mitsubishi",    "Japan",   false),
        ("Suzuki",        "Japan",   false),
        ("Subaru",        "Japan",   false),
        ("Dodge",         "USA",     false),
        ("Ram",           "USA",     false),
        ("Volvo",         "Sweden",  false),
        ("Land Rover",    "UK",      false),
        ("Porsche",       "Germany", false),
        ("Infiniti",      "Japan",   false),
        ("Acura",         "Japan",   false),
        ("Buick",         "USA",     false),
        ("GMC",           "USA",     false),
        ("Lincoln",       "USA",     false),
        ("Chrysler",      "USA",     false),
        ("Cadillac",      "USA",     false),
        ("Tesla",         "USA",     false),
        ("BYD",           "China",   false),
        ("Chery",         "China",   false),
        ("JAC",           "China",   false),
        ("MG",            "UK",      false),
    ];

    // Models per make (make name → list of model names)
    private static readonly Dictionary<string, string[]> ModelsByMake = new()
    {
        ["Toyota"] = ["Corolla", "Camry", "RAV4", "Hilux", "Fortuner", "Land Cruiser", "Prado", "Yaris", "C-HR", "Highlander", "Tacoma", "Tundra", "4Runner", "Sequoia", "Sienna"],
        ["Honda"] = ["Civic", "Accord", "CR-V", "HR-V", "Pilot", "Odyssey", "Passport", "Ridgeline", "Fit", "Jazz", "BR-V"],
        ["Hyundai"] = ["Elantra", "Sonata", "Tucson", "Santa Fe", "Accent", "Kona", "Palisade", "Creta", "Ioniq 5", "Genesis G80", "Venue"],
        ["Nissan"] = ["Sentra", "Altima", "Maxima", "Pathfinder", "Frontier", "Titan", "Rogue", "Murano", "Armada", "X-Trail", "Kicks", "Versa", "March"],
        ["Kia"] = ["Rio", "Forte", "Stinger", "Sportage", "Sorento", "Telluride", "Seltos", "Soul", "Carnival", "Stonic", "Picanto"],
        ["Mazda"] = ["Mazda3", "Mazda6", "CX-3", "CX-5", "CX-9", "CX-30", "MX-5 Miata", "BT-50"],
        ["Ford"] = ["Mustang", "F-150", "Explorer", "Escape", "Bronco", "Edge", "Expedition", "EcoSport", "Ranger", "Maverick", "Fusion"],
        ["Chevrolet"] = ["Spark", "Cruze", "Malibu", "Camaro", "Silverado", "Colorado", "Equinox", "Blazer", "Traverse", "Tahoe", "Suburban", "Trailblazer", "Captiva"],
        ["Jeep"] = ["Wrangler", "Cherokee", "Grand Cherokee", "Compass", "Renegade", "Gladiator", "Commander", "Wagoneer"],
        ["BMW"] = ["Serie 3", "Serie 5", "Serie 7", "X1", "X3", "X5", "X7", "M3", "M5", "Serie 1"],
        ["Mercedes-Benz"] = ["Clase C", "Clase E", "Clase S", "GLA", "GLC", "GLE", "GLS", "CLA", "AMG GT", "Clase A"],
        ["Audi"] = ["A3", "A4", "A6", "A8", "Q3", "Q5", "Q7", "TT", "R8", "Q8"],
        ["Volkswagen"] = ["Jetta", "Passat", "Golf", "Tiguan", "Atlas", "Taos", "Polo", "T-Cross", "Amarok"],
        ["Lexus"] = ["IS", "ES", "GS", "LS", "NX", "RX", "GX", "LX", "UX"],
        ["Mitsubishi"] = ["Lancer", "Galant", "Eclipse Cross", "Outlander", "ASX", "L200", "Montero", "Xpander"],
        ["Suzuki"] = ["Swift", "Vitara", "S-Cross", "Grand Vitara", "Jimny", "Ertiga", "Ciaz"],
        ["Subaru"] = ["Impreza", "Legacy", "Outback", "Forester", "Crosstrek", "WRX", "BRZ"],
        ["Dodge"] = ["Charger", "Challenger", "Durango", "Journey", "Dart"],
        ["Ram"] = ["1500", "2500", "3500", "ProMaster"],
        ["Volvo"] = ["S60", "S90", "XC40", "XC60", "XC90", "V60"],
        ["Land Rover"] = ["Defender", "Discovery", "Range Rover", "Range Rover Sport", "Range Rover Evoque", "Freelander"],
        ["Porsche"] = ["911", "Cayenne", "Macan", "Panamera", "Taycan"],
        ["Infiniti"] = ["Q50", "Q60", "QX50", "QX60", "QX80"],
        ["Acura"] = ["TLX", "MDX", "RDX", "ILX", "NSX"],
        ["GMC"] = ["Sierra", "Canyon", "Yukon", "Acadia", "Terrain"],
        ["Cadillac"] = ["CT4", "CT5", "XT4", "XT5", "XT6", "Escalade"],
        ["Tesla"] = ["Model 3", "Model Y", "Model S", "Model X", "Cybertruck"],
        ["BYD"] = ["Han", "Tang", "Song Plus", "Seal", "Atto 3"],
        ["Chery"] = ["Tiggo 4", "Tiggo 5x", "Tiggo 7", "Tiggo 8", "Arrizo 5"],
        ["JAC"] = ["S4", "S7", "T8", "J7"],
        ["MG"] = ["MG3", "MG5", "MG ZS", "MG HS", "Cyberster"],
        ["Buick"] = ["Envision", "Enclave", "Encore GX", "LaCrosse"],
        ["Lincoln"] = ["Nautilus", "Corsair", "Aviator", "Navigator"],
        ["Chrysler"] = ["300", "Pacifica", "Voyager"],
        ["Infiniti"] = ["Q50", "Q60", "QX50", "QX60", "QX80"],
        ["Acura"] = ["TLX", "MDX", "RDX", "ILX"],
    };

    public static async Task SeedAsync(ApplicationDbContext context, ILogger? logger = null)
    {
        // Skip if already seeded
        if (await context.VehicleMakes.AnyAsync())
        {
            logger?.LogInformation("CatalogDataSeeder: catalog already seeded, skipping.");
            return;
        }

        logger?.LogInformation("CatalogDataSeeder: seeding vehicle catalog...");

        var makeEntities = new List<VehicleMake>();
        var modelEntities = new List<VehicleModel>();

        foreach (var (makeName, country, isPopular) in Makes)
        {
            var makeId = Guid.NewGuid();
            var makeSlug = makeName.ToLowerInvariant().Replace(" ", "-").Replace(".", "");

            makeEntities.Add(new VehicleMake
            {
                Id = makeId,
                Name = makeName,
                Slug = makeSlug,
                Country = country,
                IsPopular = isPopular,
                IsActive = true,
            });

            if (ModelsByMake.TryGetValue(makeName, out var models))
            {
                foreach (var (modelName, idx) in models.Select((m, i) => (m, i)))
                {
                    var modelSlug = modelName.ToLowerInvariant().Replace(" ", "-").Replace(".", "");
                    modelEntities.Add(new VehicleModel
                    {
                        Id = Guid.NewGuid(),
                        MakeId = makeId,
                        Name = modelName,
                        Slug = $"{makeSlug}-{modelSlug}",
                        IsActive = true,
                        IsPopular = idx < 5, // first 5 models of each make are popular
                    });
                }
            }
        }

        await context.VehicleMakes.AddRangeAsync(makeEntities);
        await context.VehicleModels.AddRangeAsync(modelEntities);
        await context.SaveChangesAsync();

        logger?.LogInformation(
            "CatalogDataSeeder: seeded {Makes} makes and {Models} models.",
            makeEntities.Count, modelEntities.Count);
    }
}
