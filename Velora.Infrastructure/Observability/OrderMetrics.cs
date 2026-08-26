using System.Diagnostics.Metrics;

namespace Velora.Infrastructure.Observability;

public sealed class OrderMetrics
{
    public const string MeterName = "Velora.Metrics";
    public const string OrderActivitySource = "Velora.Orders";

    private readonly Counter<long> _ordersPlaced;
    private readonly Histogram<double> _orderValue;

    public OrderMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _ordersPlaced = meter.CreateCounter<long>("orders.placed", "orders");
        _orderValue = meter.CreateHistogram<double>("orders.value", "USD");
    }

    public void RecordPlaced(double value)
    {
        var paymentMethod = new KeyValuePair<string, object?>("payment.method", "cash_on_delivery");
        _ordersPlaced.Add(1, paymentMethod);
        _orderValue.Record(value, paymentMethod);
    }
}
