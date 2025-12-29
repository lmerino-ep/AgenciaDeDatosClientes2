namespace Utils.Secretos
{
    public interface ISecretos
    {
        Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default);
        Task<T> GetSecretAsync<T>(string secretName, CancellationToken cancellationToken = default) where T : class;
        string GetSecretValue(string secretString, string key);
    }
}
