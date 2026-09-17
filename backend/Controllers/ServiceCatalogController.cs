using ItServiceManagement.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceCatalogModel = ItServiceManagement.API.ServiceCatalog.Models.ServiceCatalog;

namespace ItServiceManagement.API.ServiceCatalog.Controllers;

[ApiController]
[Route("api/servicecatalog")]
public class ServiceCatalogController : ControllerBase
{
    private readonly CatalogDbContext _context;

    private static readonly string[] AllowedCategories =
    {
        "Hardware",
        "Software",
        "Network",
        "Access",
        "Email"
    };

    public ServiceCatalogController(CatalogDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ServiceCatalogModel>>> GetAll()
    {
        var services = await _context.ServiceCatalogs.ToListAsync();

        return Ok(services);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ServiceCatalogModel>> GetById(int id)
    {
        var service = await _context.ServiceCatalogs.FindAsync(id);

        if (service == null)
        {
            return NotFound();
        }

        return Ok(service);
    }

    [HttpPost]
    public async Task<ActionResult<ServiceCatalogModel>> Create(
        ServiceCatalogModel service)
    {
        if (!AllowedCategories.Contains(service.Category))
        {
            return BadRequest(
                "Category must be Hardware, Software, Network, Access, or Email."
            );
        }

        _context.ServiceCatalogs.Add(service);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = service.Id },
            service
        );
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ServiceCatalogModel>> Update(
        int id,
        ServiceCatalogModel updatedService)
    {
        var service = await _context.ServiceCatalogs.FindAsync(id);

        if (service == null)
        {
            return NotFound();
        }

        if (!AllowedCategories.Contains(updatedService.Category))
        {
            return BadRequest(
                "Category must be Hardware, Software, Network, Access, or Email."
            );
        }

        service.ServiceName = updatedService.ServiceName;
        service.Description = updatedService.Description;
        service.Category = updatedService.Category;
        service.RequestType = updatedService.RequestType;
        service.ServiceOwner = updatedService.ServiceOwner;
        service.EstimatedDeliveryTime = updatedService.EstimatedDeliveryTime;
        service.DefaultPriority = updatedService.DefaultPriority;
        service.SLA = updatedService.SLA;
        service.IsActive = updatedService.IsActive;

        await _context.SaveChangesAsync();

        return Ok(service);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var service = await _context.ServiceCatalogs.FindAsync(id);

        if (service == null)
        {
            return NotFound();
        }

        _context.ServiceCatalogs.Remove(service);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}