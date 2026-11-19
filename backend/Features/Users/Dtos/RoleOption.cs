namespace CorrespondenceApi.Features.Users.Dtos;

/// <summary>Роль у формі користувача; IsSuper — надає право organizations.manage.</summary>
public record RoleOption(int Id, string Name, bool IsSuper);
