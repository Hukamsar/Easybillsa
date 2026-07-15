using AOne.DataAccess.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;

namespace EasyBill.UI.Controllers
{
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class BackupController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BackupController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            try
            {
                var connectionString = _context.Database.GetDbConnection().ConnectionString;
                var connStringBuilder = new SqlConnectionStringBuilder(connectionString);
                ViewBag.DatabaseName = connStringBuilder.InitialCatalog;
                ViewBag.ServerName = connStringBuilder.DataSource;
            }
            catch (Exception)
            {
                ViewBag.DatabaseName = "EasyBillSuperAdminNew";
                ViewBag.ServerName = "LocalServer";
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RunBackup()
        {
            try
            {
                var connectionString = _context.Database.GetDbConnection().ConnectionString;
                var connStringBuilder = new SqlConnectionStringBuilder(connectionString);
                var databaseName = connStringBuilder.InitialCatalog;

                // 1. Collect candidate directories
                var candidates = new List<string>();

                // Candidate A: SQL Server registry backup directory
                try
                {
                    var query = @"
                        DECLARE @BackupDirectory NVARCHAR(4000);
                        EXEC master.dbo.xp_instance_regread
                            N'HKEY_LOCAL_MACHINE',
                            N'Software\Microsoft\MSSQLServer\MSSQLServer',
                            N'BackupDirectory',
                            @BackupDirectory OUTPUT;
                        SELECT @BackupDirectory;";
                    
                    var connection = _context.Database.GetDbConnection();
                    var wasClosed = connection.State == System.Data.ConnectionState.Closed;
                    if (wasClosed) await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = query;
                        var result = await command.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            candidates.Add(result.ToString());
                        }
                    }
                    if (wasClosed) await connection.CloseAsync();
                }
                catch { }

                // Candidate B: Common Shared Folder (Users/Public)
                candidates.Add(@"C:\Users\Public");

                // Candidate C: System Temp Folder
                candidates.Add(@"C:\Windows\Temp");

                // Candidate D: AppData Common Folder
                try
                {
                    candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EasyBillBackups"));
                }
                catch { }

                // Candidate E: ASP.NET Core process temp folder
                try
                {
                    candidates.Add(Path.GetTempPath());
                }
                catch { }

                // Candidate F: App local folder
                try
                {
                    candidates.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups"));
                }
                catch { }

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFileName = $"EasyBill_Backup_{timestamp}.bak";
                
                string finalBackupFilePath = null;
                Exception lastException = null;

                // 2. Loop through candidates to perform the backup and verify readability
                foreach (var folder in candidates)
                {
                    if (string.IsNullOrEmpty(folder)) continue;

                    // A. Check if the web app can create the directory and write to it
                    try
                    {
                        if (!Directory.Exists(folder))
                        {
                            Directory.CreateDirectory(folder);
                        }

                        // Test write and delete
                        var testFile = Path.Combine(folder, $"EasyBill_PermTest_{Guid.NewGuid():N}.tmp");
                        System.IO.File.WriteAllText(testFile, "test");
                        System.IO.File.Delete(testFile);
                    }
                    catch
                    {
                        // Web app doesn't have permissions on this folder, skip it
                        continue;
                    }

                    // B. Run BACKUP DATABASE command using SQL Server
                    var backupFilePath = Path.Combine(folder, backupFileName);
                    try
                    {
                        var backupQuery = $"BACKUP DATABASE [{databaseName}] TO DISK = '{backupFilePath}' WITH FORMAT, INIT";
                        _context.Database.SetCommandTimeout(300); // 5 minutes timeout
                        await _context.Database.ExecuteSqlRawAsync(backupQuery);

                        // C. Verify that the file exists and is readable by the web app
                        if (System.IO.File.Exists(backupFilePath))
                        {
                            // Test if web app can open the file for reading
                            using (var fs = System.IO.File.OpenRead(backupFilePath))
                            {
                                // Successfully opened!
                            }
                            finalBackupFilePath = backupFilePath;
                            break; // Success! Exit loop
                        }
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        // Attempt cleanup of the file if partially written
                        try
                        {
                            if (System.IO.File.Exists(backupFilePath))
                            {
                                System.IO.File.Delete(backupFilePath);
                            }
                        }
                        catch { }
                    }
                }

                // 3. Read and stream the backup file
                if (string.IsNullOrEmpty(finalBackupFilePath))
                {
                    var details = lastException != null ? $". Details: {lastException.Message}" : "";
                    TempData["error"] = $"Backup failed. SQL Server backup directory could not be accessed or shared with the web application{details}";
                    return RedirectToAction("Index");
                }

                try
                {
                    // Open with DeleteOnClose so the OS deletes the file as soon as streaming completes
                    var stream = new FileStream(finalBackupFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.DeleteOnClose);
                    return File(stream, "application/octet-stream", backupFileName);
                }
                catch (Exception)
                {
                    // Fallback: Read bytes, delete file, then serve
                    var fileBytes = await System.IO.File.ReadAllBytesAsync(finalBackupFilePath);
                    try
                    {
                        System.IO.File.Delete(finalBackupFilePath);
                    }
                    catch { }
                    return File(fileBytes, "application/octet-stream", backupFileName);
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = $"Backup failed: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
    }
}

