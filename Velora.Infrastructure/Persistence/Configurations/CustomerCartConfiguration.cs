using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Velora.Domain.Customers;

namespace Velora.Infrastructure.Persistence.Configurations;

internal sealed class CustomerCartConfiguration : IEntityTypeConfiguration<CustomerCart>
{
    public void Configure(EntityTypeBuilder<CustomerCart> builder)
    {
        builder.HasKey(cart => cart.Id);
        builder.HasIndex(cart => cart.CustomerId).IsUnique();
    }
}
