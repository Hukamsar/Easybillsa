using AOne.DataAccess.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace EasyBill.DataAccess.StoredProcedures
{
    public static class PurchaseModuleStoredProcedureInstaller
    {
        private const string ResourceSuffix = "Sql.PurchaseModule.StoredProcedures.sql";

        public static async Task EnsureInstalledAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
        {
            if (!context.Database.IsSqlServer())
            {
                return;
            }

            var assembly = typeof(PurchaseModuleStoredProcedureInstaller).Assembly;
            var resourceName = assembly
                .GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith(ResourceSuffix, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(resourceName))
            {
                throw new InvalidOperationException("Embedded SQL resource for purchase stored procedures was not found.");
            }

            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Unable to open embedded SQL resource '{resourceName}'.");
            using var reader = new StreamReader(stream);

            var script = await reader.ReadToEndAsync(cancellationToken);
            var batches = Regex.Split(script, @"^\s*GO(?:\s+\d+)?\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)
                .Where(batch => !string.IsNullOrWhiteSpace(batch))
                .ToList();

            var connection = context.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;

            if (shouldClose)
            {
                await connection.OpenAsync(cancellationToken);
            }

            try
            {
                foreach (var batch in batches)
                {
                    await using var command = connection.CreateCommand();
                    command.CommandText = batch;
                    command.CommandType = System.Data.CommandType.Text;
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }
            }
            finally
            {
                if (shouldClose)
                {
                    await connection.CloseAsync();
                }
            }
        }
    }
}
