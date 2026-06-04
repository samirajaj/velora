namespace Velora.Entities.Catalog;

public class Category
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<Product> Products { get; set; } = [];
}
