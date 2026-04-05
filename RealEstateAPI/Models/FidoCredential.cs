namespace RealEstateAPI.Models
{
    public class FidoCredential
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public byte[] CredentialId { get; set; } = null!;
        public byte[] PublicKey { get; set; } = null!;
        public uint SignCount { get; set; }

        public string CredType { get; set; } = "public-key";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
