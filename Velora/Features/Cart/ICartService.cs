namespace Velora.Features.Cart;

public interface ICartService
{
    CartViewModel GetCart();
    void Add(CartItem item);
    void Update(Guid variantId, int quantity);
    void Remove(Guid variantId);
    void Clear();
}
