namespace RealEstateAPI.Models
{

    public class AgentFeedbackDTO
    {
        
        public int UserId { get; set; }

        public string Feedback { get; set; } = null!;

        public string UserName { get; set; }= string.Empty;

        public string UserEmail { get; set; } = null!;


        public double StarRating { get; set; } = 0.0;


    }

    public class AgentFeedbackDTOResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Feedback { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string UserEmail { get; set; } = null!;
        public string IpAddress { get; set; } = null!;
        public double StarRating { get; set; } = 0.0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; } = null;
    }


    public class AgentFeedback
    {
        public int Id { get; set; }

        public User User { get; set; } = null!;

        public string Feedback { get; set; } = null!;

        public string UserName { get; set; } = null!;

        public string UserEmail { get; set; } = null!;

        public string IpAddress { get; set; } = null!;

        public double StarRating { get; set; } = 0.0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; } = null;

    }
}
