using Fido2NetLib;

namespace Backend.DTOs.Passkey
{
    public class PasskeyLoginBeginResponseDto
    {
        public AssertionOptions? Options { get; set; }
        public string? ChallengeId { get; set; }
    }
}