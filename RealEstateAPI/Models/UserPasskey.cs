namespace RealEstateAPI.Models
{
    public class UserPasskey
    {
        public int Id { get; set; }

        public string UserId { get; set; }

        public byte[] UserHandle { get; set; }

        public byte[] CredentialId { get; set; }

        public byte[] PublicKey { get; set; }

        public uint SignCount { get; set; }



        public string DeviceName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
