namespace TaskPulse.API.Entities.BaseEntityClass
{
    public class BasicEntityClass
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
