using Microsoft.AspNetCore.Mvc;
using VehiclesSaleService.Domain.Entities;
using VehiclesSaleService.Domain.Interfaces;
using Microsoft.AspNetCore.RateLimiting;

namespace VehiclesSaleService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("VehiclesPolicy")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(
        ICategoryRepository categoryRepository,
        ILogger<CategoriesController> logger)
    {
        _categoryRepository = categoryRepository;
        _logger = logger;
    }

    /// <summary>
    /// Obtener todas las categorías
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Category>>> GetAll()
    {
        var categories = await _categoryRepository.GetAllAsync();
        return Ok(categories);
    }

    /// <summary>
    /// Obtener categorías raíz
    /// </summary>
    [HttpGet("root")]
    public async Task<ActionResult<IEnumerable<Category>>> GetRootCategories()
    {
        var categories = await _categoryRepository.GetRootCategoriesAsync();
        return Ok(categories);
    }

    /// <summary>
    /// Obtener categoría por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Category>> GetById(Guid id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);

        if (category == null)
            return NotFound(new { message = "Categoría no encontrada" });

        return Ok(category);
    }

    /// <summary>
    /// Obtener categoría por slug
    /// </summary>
    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<Category>> GetBySlug(string slug)
    {
        var category = await _categoryRepository.GetBySlugAsync(slug);

        if (category == null)
            return NotFound(new { message = "Categoría no encontrada" });

        return Ok(category);
    }

    /// <summary>
    /// Obtener subcategorías de una categoría
    /// </summary>
    [HttpGet("{id}/children")]
    public async Task<ActionResult<IEnumerable<Category>>> GetChildren(Guid id)
    {
        var children = await _categoryRepository.GetChildrenAsync(id);
        return Ok(children);
    }
}
