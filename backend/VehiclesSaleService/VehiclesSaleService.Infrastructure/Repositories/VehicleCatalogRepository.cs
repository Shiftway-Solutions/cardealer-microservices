using Microsoft.EntityFrameworkCore;
using VehiclesSaleService.Domain.Entities;
using VehiclesSaleService.Domain.Interfaces;
using VehiclesSaleService.Infrastructure.Persistence;

namespace VehiclesSaleService.Infrastructure.Repositories;

/// <summary>
/// Implementación del repositorio del catálogo maestro de vehículos.
/// </summary>
public class VehicleCatalogRepository : IVehicleCatalogRepository
{
    private readonly ApplicationDbContext _context;

    public VehicleCatalogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // ========================================
    // MAKES (Marcas)
    // ========================================

    public async Task<IEnumerable<VehicleMake>> GetAllMakesAsync(bool includeInactive = false)
    {
        var query = _context.VehicleMakes.AsNoTracking().AsQueryable();

        if (!includeInactive)
            query = query.Where(m => m.IsActive);

        return await query
            .OrderByDescending(m => m.IsPopular)
            .ThenBy(m => m.SortOrder)
            .ThenBy(m => m.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<VehicleMake>> GetPopularMakesAsync(int take = 20)
    {
        return await _context.VehicleMakes
            .AsNoTracking()
            .Where(m => m.IsActive && m.IsPopular)
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.Name)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<VehicleMake>> SearchMakesAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetAllMakesAsync();

        var term = searchTerm.ToLower();
        return await _context.VehicleMakes
            .AsNoTracking()
            .Where(m => m.IsActive && m.Name.ToLower().Contains(term))
            .OrderByDescending(m => m.Name.ToLower().StartsWith(term))
            .ThenBy(m => m.Name)
            .Take(50)
            .ToListAsync();
    }

    public async Task<VehicleMake?> GetMakeByIdAsync(Guid id)
    {
        return await _context.VehicleMakes
            .AsNoTracking()
            .Include(m => m.Models.Where(model => model.IsActive))
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<VehicleMake?> GetMakeBySlugAsync(string slug)
    {
        return await _context.VehicleMakes
            .AsNoTracking()
            .Include(m => m.Models.Where(model => model.IsActive))
            .FirstOrDefaultAsync(m => m.Slug == slug.ToLower());
    }

    // ========================================
    // MODELS (Modelos)
    // ========================================

    public async Task<IEnumerable<VehicleModel>> GetModelsByMakeIdAsync(Guid makeId, bool includeInactive = false)
    {
        var query = _context.VehicleModels
            .AsNoTracking()
            .Include(m => m.Make)
            .Where(m => m.MakeId == makeId);

        if (!includeInactive)
            query = query.Where(m => m.IsActive);

        return await query
            .OrderByDescending(m => m.IsPopular)
            .ThenBy(m => m.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<VehicleModel>> GetModelsByMakeSlugAsync(string makeSlug, bool includeInactive = false)
    {
        var make = await _context.VehicleMakes
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Slug == makeSlug.ToLower());

        if (make == null)
            return Enumerable.Empty<VehicleModel>();

        return await GetModelsByMakeIdAsync(make.Id, includeInactive);
    }

    public async Task<IEnumerable<VehicleModel>> SearchModelsAsync(Guid makeId, string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetModelsByMakeIdAsync(makeId);

        var term = searchTerm.ToLower();
        return await _context.VehicleModels
            .AsNoTracking()
            .Include(m => m.Make)
            .Where(m => m.MakeId == makeId && m.IsActive && m.Name.ToLower().Contains(term))
            .OrderByDescending(m => m.Name.ToLower().StartsWith(term))
            .ThenBy(m => m.Name)
            .Take(50)
            .ToListAsync();
    }

    public async Task<VehicleModel?> GetModelByIdAsync(Guid id)
    {
        return await _context.VehicleModels
            .AsNoTracking()
            .Include(m => m.Make)
            .Include(m => m.Trims.Where(t => t.IsActive))
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<VehicleModel?> GetModelBySlugAsync(string makeSlug, string modelSlug)
    {
        return await _context.VehicleModels
            .AsNoTracking()
            .Include(m => m.Make)
            .Include(m => m.Trims.Where(t => t.IsActive))
            .FirstOrDefaultAsync(m =>
                m.Make != null &&
                m.Make.Slug == makeSlug.ToLower() &&
                m.Slug == modelSlug.ToLower());
    }

    // ========================================
    // TRIMS (Versiones)
    // ========================================

    public async Task<IEnumerable<VehicleTrim>> GetTrimsByModelAndYearAsync(Guid modelId, int year)
    {
        return await _context.VehicleTrims
            .AsNoTracking()
            .Include(t => t.Model)
            .ThenInclude(m => m!.Make)
            .Where(t => t.ModelId == modelId && t.Year == year && t.IsActive)
            .OrderBy(t => t.BaseMSRP)
            .ThenBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<int>> GetAvailableYearsAsync(Guid modelId)
    {
        return await _context.VehicleTrims
            .AsNoTracking()
            .Where(t => t.ModelId == modelId && t.IsActive)
            .Select(t => t.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync();
    }

    public async Task<VehicleTrim?> GetTrimByIdAsync(Guid id)
    {
        return await _context.VehicleTrims
            .AsNoTracking()
            .Include(t => t.Model)
            .ThenInclude(m => m!.Make)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<VehicleTrim?> GetTrimSpecsAsync(Guid modelId, int year, string trimName)
    {
        return await _context.VehicleTrims
            .AsNoTracking()
            .Include(t => t.Model)
            .ThenInclude(m => m!.Make)
            .FirstOrDefaultAsync(t =>
                t.ModelId == modelId &&
                t.Year == year &&
                t.Name.ToLower() == trimName.ToLower() &&
                t.IsActive);
    }

    // ========================================
    // SEED / ADMIN
    // ========================================

    public async Task<VehicleMake> UpsertMakeAsync(VehicleMake make)
    {
        var existing = await _context.VehicleMakes
            .FirstOrDefaultAsync(m => m.Slug == make.Slug);

        if (existing == null)
        {
            make.Id = Guid.NewGuid();
            make.CreatedAt = DateTime.UtcNow;
            make.UpdatedAt = DateTime.UtcNow;
            _context.VehicleMakes.Add(make);
        }
        else
        {
            existing.Name = make.Name;
            existing.LogoUrl = make.LogoUrl;
            existing.Country = make.Country;
            existing.IsActive = make.IsActive;
            existing.IsPopular = make.IsPopular;
            existing.SortOrder = make.SortOrder;
            existing.UpdatedAt = DateTime.UtcNow;
            make = existing;
        }

        await _context.SaveChangesAsync();
        return make;
    }

    public async Task<VehicleModel> UpsertModelAsync(VehicleModel model)
    {
        var existing = await _context.VehicleModels
            .FirstOrDefaultAsync(m => m.MakeId == model.MakeId && m.Slug == model.Slug);

        if (existing == null)
        {
            model.Id = Guid.NewGuid();
            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;
            _context.VehicleModels.Add(model);
        }
        else
        {
            existing.Name = model.Name;
            existing.VehicleType = model.VehicleType;
            existing.DefaultBodyStyle = model.DefaultBodyStyle;
            existing.StartYear = model.StartYear;
            existing.EndYear = model.EndYear;
            existing.IsActive = model.IsActive;
            existing.IsPopular = model.IsPopular;
            existing.UpdatedAt = DateTime.UtcNow;
            model = existing;
        }

        await _context.SaveChangesAsync();
        return model;
    }

    public async Task<VehicleTrim> UpsertTrimAsync(VehicleTrim trim)
    {
        var existing = await _context.VehicleTrims
            .FirstOrDefaultAsync(t =>
                t.ModelId == trim.ModelId &&
                t.Year == trim.Year &&
                t.Slug == trim.Slug);

        if (existing == null)
        {
            trim.Id = Guid.NewGuid();
            trim.CreatedAt = DateTime.UtcNow;
            trim.UpdatedAt = DateTime.UtcNow;
            _context.VehicleTrims.Add(trim);
        }
        else
        {
            existing.Name = trim.Name;
            existing.BaseMSRP = trim.BaseMSRP;
            existing.EngineSize = trim.EngineSize;
            existing.Horsepower = trim.Horsepower;
            existing.Torque = trim.Torque;
            existing.FuelType = trim.FuelType;
            existing.Transmission = trim.Transmission;
            existing.DriveType = trim.DriveType;
            existing.MpgCity = trim.MpgCity;
            existing.MpgHighway = trim.MpgHighway;
            existing.MpgCombined = trim.MpgCombined;
            existing.IsActive = trim.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
            trim = existing;
        }

        await _context.SaveChangesAsync();
        return trim;
    }

    public async Task<int> BulkImportAsync(IEnumerable<VehicleMake> makes)
    {
        int count = 0;

        foreach (var make in makes)
        {
            var savedMake = await UpsertMakeAsync(make);
            count++;

            foreach (var model in make.Models)
            {
                model.MakeId = savedMake.Id;
                var savedModel = await UpsertModelAsync(model);
                count++;

                foreach (var trim in model.Trims)
                {
                    trim.ModelId = savedModel.Id;
                    await UpsertTrimAsync(trim);
                    count++;
                }
            }
        }

        return count;
    }

    public async Task<CatalogStats> GetStatsAsync()
    {
        var makesCount = await _context.VehicleMakes.CountAsync(m => m.IsActive);
        var modelsCount = await _context.VehicleModels.CountAsync(m => m.IsActive);
        var trimsCount = await _context.VehicleTrims.CountAsync(t => t.IsActive);

        var years = await _context.VehicleTrims
            .Where(t => t.IsActive)
            .Select(t => t.Year)
            .Distinct()
            .ToListAsync();

        var lastUpdated = await _context.VehicleMakes
            .MaxAsync(m => (DateTime?)m.UpdatedAt) ?? DateTime.UtcNow;

        return new CatalogStats
        {
            TotalMakes = makesCount,
            TotalModels = modelsCount,
            TotalTrims = trimsCount,
            MinYear = years.Any() ? years.Min() : DateTime.UtcNow.Year,
            MaxYear = years.Any() ? years.Max() : DateTime.UtcNow.Year,
            LastUpdated = lastUpdated
        };
    }
}
