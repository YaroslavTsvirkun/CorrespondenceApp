namespace CorrespondenceApi.Domain;

public class DeliveryChannel
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}
