namespace Imgriff.Data.Entity
{
    public class AuthToken
    {
        public Guid Id { get; set; }

        public Guid userId { get; set; }

        public string? Token { get; set; }

        public int Used { get; set; } = 0;
    }
}
