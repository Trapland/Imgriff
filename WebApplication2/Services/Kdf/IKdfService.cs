namespace Imgriff.Services.Kdf
{
    /// <summary>
    /// Key derivation func (rfc 8018)
    /// </summary>
    public interface IKdfService
    {
        /// <summary>
        /// Mixing password and salt to make a derived key
        /// </summary>
        /// <param name="password"></param>
        /// <param name="salt"></param>
        /// <returns></returns>
        String GetDerivedKey(String password, String salt);
    }
}
