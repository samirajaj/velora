using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Velora.Domain.Customers;

namespace Velora.Infrastructure.Persistence.Configurations;

internal sealed class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.HasKey(address => address.Id);
        builder.Property(address => address.Label).HasMaxLength(50);
        builder.Property(address => address.RecipientName).HasMaxLength(160).IsRequired();
        builder.Property(address => address.PhoneNumber).HasMaxLength(30).IsRequired();
        builder.Property(address => address.Line1).HasMaxLength(200).IsRequired();
        builder.Property(address => address.Line2).HasMaxLength(200);
        builder.Property(address => address.City).HasMaxLength(100).IsRequired();
        builder.Property(address => address.StateOrProvince).HasMaxLength(100);
        builder.Property(address => address.PostalCode).HasMaxLength(30);
        builder.Property(address => address.CountryCode).HasMaxLength(2).IsRequired();
        builder.HasIndex(address => new { address.CustomerId, address.IsArchived });
    }
}
