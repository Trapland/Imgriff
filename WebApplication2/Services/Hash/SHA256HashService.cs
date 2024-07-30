namespace Imgriff.Services.Hash
{
    public class SHA256HashService : IHashService
    {
        public string Hash(string text)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            return Convert.ToHexString(sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text)));
        }
    }
}
