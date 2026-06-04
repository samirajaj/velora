using Microsoft.EntityFrameworkCore;
using Velora.Data;
using Velora.Features.Products.ViewModels;

namespace Velora.Features.Products.Services;

public class ProductService(ApplicationDbContext context)
{
    private readonly ApplicationDbContext _context = context;

    public async Task<List<ProductListItemVm>> GetAllAsync()
    {
        return await _context.Products
            .AsNoTracking()
            .Select(x => new ProductListItemVm
            {
                Id = x.Id,
                Name = x.Name,
                Price = x.Price,
                ImageUrl = x.ImageUrl
            })
            .ToListAsync();
    }

    public async Task<ProductDetailsVm?> GetByIdAsync(Guid id)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ProductDetailsVm
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                Price = x.Price,
                ImageUrl = x.ImageUrl,
                CategoryName = x.Category.Name
            })
            .FirstOrDefaultAsync();
    }
}
