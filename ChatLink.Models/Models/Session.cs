namespace ChatLink.Models.Models;

public class Session
{
    public Guid Id { get; set; }

    public required string Title { get; set; }

    public DateTime CreatedDt { get; set; }

    public ICollection<Message> Messages { get; set; } = new List<Message>();
}