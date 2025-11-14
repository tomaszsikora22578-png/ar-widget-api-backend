using MySql.Data.MySqlClient;
using Dapper;
using ArWidgetApi.Models; // Pamiętaj o ModelDataDto

public class DataAccessService
{
    private readonly string _connectionString = "..."; // Użyj swojego connection string

    public async Task<IEnumerable<ModelDataDto>> GetAuthorizedModels(string clientToken)
    {
        // Nowe zapytanie wykorzystujące Twoje nazwy kolumn i tabelę łączącą
        const string sql = @"
            SELECT
                p.Name,
                p.ModelUrlGlb,   -- Ścieżka GCS dla GLB (Android)
                p.ModelUrlUsdz   -- Ścieżka GCS dla USDZ (iOS)
            FROM Clients c
            JOIN Client_Product_Access a ON c.Id = a.client_id  -- Nowe połączenie
            JOIN Products p ON a.product_id = p.Id              -- Modele
            WHERE 
                c.ClientToken = @ClientToken  -- Użycie ClientToken
                AND c.SubscriptionStatus IS NOT NULL; -- Prosta weryfikacja statusu";

        using (var connection = new MySqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            
            var models = await connection.QueryAsync<ModelDataDto>(
                sql, 
                new { ClientToken = clientToken }
            );

            return models;
        }
    }
}
