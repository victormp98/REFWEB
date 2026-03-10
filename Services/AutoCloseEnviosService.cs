using Microsoft.EntityFrameworkCore;
using RefWeb.Data;
using RefWeb.Models;

namespace RefWeb.Services
{
    /// <summary>
    /// BackgroundService: corre cada 24h y cierra automáticamente los pedidos en estado
    /// "Enviado" que lleven más de 21 días sin confirmación del cliente.
    /// </summary>
    public class AutoCloseEnviosService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AutoCloseEnviosService> _logger;
        private readonly TimeSpan _checkInterval  = TimeSpan.FromHours(24);
        private readonly int      _diasParaCierre = 21;

        public AutoCloseEnviosService(IServiceProvider serviceProvider, ILogger<AutoCloseEnviosService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[AUTO-CLOSE] Servicio de cierre automático de envíos iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CerrarEnviosVencidosAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[AUTO-CLOSE] Error durante el cierre automático de envíos.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task CerrarEnviosVencidosAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var fechaLimite = DateTime.Now.AddDays(-_diasParaCierre);

            var pedidosVencidos = await context.Pedidos
                .Include(p => p.Envio)
                .Where(p => p.EstadoPedido == "Enviado"
                         && p.Envio != null
                         && p.Envio.FechaEnvio != null
                         && p.Envio.FechaEnvio < fechaLimite)
                .ToListAsync();

            if (!pedidosVencidos.Any())
                return;

            foreach (var pedido in pedidosVencidos)
            {
                pedido.EstadoPedido           = "Entregado";
                pedido.Envio!.EstadoEnvio     = "Entregado";
                pedido.Envio.FechaEntrega     = DateTime.Now;

                context.HistorialEnvios.Add(new HistorialEnvio
                {
                    EnvioId     = pedido.Envio.Id,
                    Estado      = "Entregado",
                    Ubicacion   = "Sistema",
                    Descripcion = $"Cierre automático. Sin confirmación del cliente en {_diasParaCierre} días.",
                    Fecha       = DateTime.Now
                });
            }

            await context.SaveChangesAsync();

            _logger.LogInformation(
                "[AUTO-CLOSE] {Count} pedido(s) cerrados automáticamente por inactividad de {Dias} días.",
                pedidosVencidos.Count, _diasParaCierre);
        }
    }
}
