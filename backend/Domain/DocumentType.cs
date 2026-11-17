namespace CorrespondenceApi.Domain;

public class DocumentType
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    // Короткий код типу — використовується у патерні реєстраційного номера ({type})
    public string? Code { get; set; }
    public string? Description { get; set; }
}
