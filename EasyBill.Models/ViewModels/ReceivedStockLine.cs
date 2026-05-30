public sealed class ReceivedStockLine
{
    public int ItemId { get; init; }
    public int? SupplierId { get; init; }
    public string? Batch { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public decimal Mrp { get; init; }
    public decimal Qty { get; init; }
}