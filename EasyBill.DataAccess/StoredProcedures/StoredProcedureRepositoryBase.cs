using AOne.DataAccess.Data;
using EasyBill.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Data.Common;
using System.Security.Claims;

namespace EasyBill.DataAccess.StoredProcedures
{
    public abstract class StoredProcedureRepositoryBase
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITenantAccessor _tenantAccessor;

        protected StoredProcedureRepositoryBase(
            ApplicationDbContext dbContext,
            IHttpContextAccessor httpContextAccessor,
            ITenantAccessor tenantAccessor)
        {
            DbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
            _tenantAccessor = tenantAccessor;
        }

        protected ApplicationDbContext DbContext { get; }

        protected string GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";
        }

        protected string? GetCurrentTenantId()
        {
            return _tenantAccessor.GetCurrentTenantId();
        }

        protected string GetCurrentFilterMode()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null)
            {
                return "All";
            }

            if (user.IsInRole("SuperAdmin"))
            {
                return "All";
            }

            if (user.IsInRole("Admin") || user.IsInRole("Doctor"))
            {
                return string.IsNullOrWhiteSpace(GetCurrentTenantId()) ? "All" : "Tenant";
            }

            return "User";
        }

        protected async Task<T> WithStoredProcedureCommandAsync<T>(
            string storedProcedureName,
            Func<DbCommand, Task<T>> action,
            CancellationToken cancellationToken = default)
        {
            var connection = DbContext.Database.GetDbConnection();
            var shouldClose = connection.State != ConnectionState.Open;

            if (shouldClose)
            {
                await connection.OpenAsync(cancellationToken);
            }

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = storedProcedureName;
                command.CommandType = CommandType.StoredProcedure;

                var currentTransaction = DbContext.Database.CurrentTransaction;
                if (currentTransaction != null)
                {
                    command.Transaction = currentTransaction.GetDbTransaction();
                }

                return await action(command);
            }
            finally
            {
                if (shouldClose && DbContext.Database.CurrentTransaction == null)
                {
                    await connection.CloseAsync();
                }
            }
        }

        protected Task WithStoredProcedureCommandAsync(
            string storedProcedureName,
            Func<DbCommand, Task> action,
            CancellationToken cancellationToken = default)
        {
            return WithStoredProcedureCommandAsync<object?>(
                storedProcedureName,
                async command =>
                {
                    await action(command);
                    return null;
                },
                cancellationToken);
        }

        protected void AddFilterParameters(DbCommand command)
        {
            AddParameter(command, "@FilterMode", GetCurrentFilterMode());
            AddParameter(command, "@TenantId", GetCurrentTenantId());
            AddParameter(command, "@UserId", GetCurrentUserId());
        }

        protected static void AddParameter(DbCommand command, string name, object? value, DbType? dbType = null)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;

            if (dbType.HasValue)
            {
                parameter.DbType = dbType.Value;
            }

            command.Parameters.Add(parameter);
        }

        protected static void AddStructuredParameter(DbCommand command, string name, string typeName, DataTable value)
        {
            if (command is not SqlCommand sqlCommand)
            {
                throw new InvalidOperationException("Structured parameters require SqlCommand.");
            }

            sqlCommand.Parameters.Add(new SqlParameter(name, SqlDbType.Structured)
            {
                TypeName = typeName,
                Value = value
            });
        }
    }
}
