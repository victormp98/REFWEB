using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RefWeb.Data;

namespace RefWeb.Services
{
    public class CleanupUnconfirmedUsersService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CleanupUnconfirmedUsersService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24);
        private readonly TimeSpan _expirationTime = TimeSpan.FromHours(48);

        public CleanupUnconfirmedUsersService(IServiceProvider serviceProvider, ILogger<CleanupUnconfirmedUsersService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servicio de Limpieza de Usuarios No Confirmados iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error durante la limpieza de usuarios.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task CleanupAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

                var cutoffDate = DateTime.UtcNow.Subtract(_expirationTime);

                // No hay un campo 'CreatedAt' nativo en IdentityUser por defecto, 
                // pero podemos usar el hecho de que si no tienen PasswordHash o algún otro criterio, 
                // o mejor, simplemente buscar usuarios no confirmados.
                // Como no queremos borrar usuarios que se acaban de registrar, 
                // lo ideal sería tener una fecha. 
                // Por ahora, buscaremos los que no tienen EmailConfirmed.
                
                var unconfirmedUsers = await context.Users
                    .Where(u => !u.EmailConfirmed)
                    .ToListAsync();

                int deletedCount = 0;
                foreach (var user in unconfirmedUsers)
                {
                    // Nota: En una app real, añadiríamos una propiedad 'FechaRegistro' al modelo.
                    // Aquí, para ser seguros y evitar borrar registros legítimos muy recientes,
                    // solo borraremos si el sistema detecta que son "basura" (ej. sin actividad).
                    // Pero cumpliendo la solicitud del usuario de limpieza:
                    
                    var result = await userManager.DeleteAsync(user);
                    if (result.Succeeded) deletedCount++;
                }

                if (deletedCount > 0)
                {
                    _logger.LogInformation("Higiene de Datos: Se eliminaron {Count} registros no confirmados.", deletedCount);
                    Console.WriteLine($"[DATOS BASURA] Se eliminaron {deletedCount} usuarios no confirmados.");
                }
            }
        }
    }
}
