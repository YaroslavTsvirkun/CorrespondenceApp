namespace CorrespondenceApi.Domain;

// Назва класу — TaskState (щоб не конфліктувати з System.Threading.Tasks.TaskStatus);
// у БД таблиця називається TaskStatus, навігаційна властивість — TaskStatus.
public class TaskState
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public bool IsFinal { get; set; }
    public string? Description { get; set; }
}
