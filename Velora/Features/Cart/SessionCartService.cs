using System.Text.Json;

namespace Velora.Features.Cart;

public sealed class SessionCartService(IHttpContextAccessor accessor) : ICartService
{
    private const string CartKey = "velora-cart";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private ISession Session => accessor.HttpContext?.Session ?? throw new InvalidOperationException("No active HTTP session.");

    public CartViewModel GetCart() => new(Read());

    public void Add(CartItem item)
    {
        var items = Read();
        var existing = items.FindIndex(x => x.VariantId == item.VariantId);
        if (existing >= 0)
            items[existing] = items[existing] with { Quantity = Math.Min(10, items[existing].Quantity + item.Quantity) };
        else
            items.Add(item with { Quantity = Math.Clamp(item.Quantity, 1, 10) });
        Write(items);
    }

    public void Update(Guid variantId, int quantity)
    {
        var items = Read();
        var index = items.FindIndex(x => x.VariantId == variantId);
        if (index < 0) return;
        if (quantity <= 0) items.RemoveAt(index);
        else items[index] = items[index] with { Quantity = Math.Min(10, quantity) };
        Write(items);
    }

    public void Remove(Guid variantId)
    {
        var items = Read();
        items.RemoveAll(x => x.VariantId == variantId);
        Write(items);
    }
    public void Clear() => Session.Remove(CartKey);

    private List<CartItem> Read() => JsonSerializer.Deserialize<List<CartItem>>(Session.GetString(CartKey) ?? "[]", JsonOptions) ?? [];
    private void Write(List<CartItem> items) => Session.SetString(CartKey, JsonSerializer.Serialize(items, JsonOptions));
}
