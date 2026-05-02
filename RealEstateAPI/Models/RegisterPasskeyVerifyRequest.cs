using Fido2NetLib;

namespace RealEstateAPI.Models
{
    public class RegisterPasskeyVerifyRequest
    {
        public int UserId { get; set; }
        public AuthenticatorAttestationRawResponse Credential { get; set; }
    }
}
