namespace Imgriff.Services.Random
{
    public class RandomService : IRandomService
    {
        private readonly String _codeChars = "abcdefghijklmnopqrstuvwxyz0123456789";
        private readonly String _safeChars = new String(Enumerable.Range(20, 107).Select(x => (char)x).ToArray());
        private readonly System.Random _random = new();

        public string ConfirmCode(int length)
        {
            char[] chars = new char[length];
            for (int i = 0; i < length; i++)
            {
                chars[i] = _codeChars[_random.Next(_codeChars.Length)];
            }
            return new String(chars);
        }

        public string RandomString(int length)
        {
            char[] chars = new char[length];
            for (int i = 0; i < length; i++)
            {
                chars[i] = _safeChars[_random.Next(_safeChars.Length)];
            }
            return new String(chars);
        }
    }
}
