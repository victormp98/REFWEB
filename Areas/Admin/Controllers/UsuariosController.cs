using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace RefWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsuariosController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsuariosController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Admin/Usuarios
        public async Task<IActionResult> Index()
        {
            var usuarios = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
            var lista = new List<(IdentityUser Usuario, string Rol, bool Bloqueado)>();

            foreach (var u in usuarios)
            {
                var roles    = await _userManager.GetRolesAsync(u);
                var bloqueado = u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow;
                lista.Add((u, roles.FirstOrDefault() ?? "Sin rol", bloqueado));
            }

            ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name).OrderBy(r => r).ToListAsync();
            return View(lista);
        }

        // POST: Admin/Usuarios/CambiarRol
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarRol(string userId, string nuevoRol)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            if (user.Id == currentUserId && nuevoRol != "Admin")
            {
                TempData["Error"] = "No puedes cambiar tu propio rol de Administrador.";
                return RedirectToAction(nameof(Index));
            }

            var rolesActuales = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, rolesActuales);

            if (!string.IsNullOrEmpty(nuevoRol) && await _roleManager.RoleExistsAsync(nuevoRol))
                await _userManager.AddToRoleAsync(user, nuevoRol);

            TempData["Success"] = $"Rol de {user.Email} actualizado a '{nuevoRol}'.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Usuarios/ToggleLock
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            if (user.Id == currentUserId)
            {
                TempData["Error"] = "No puedes bloquearte a ti mismo.";
                return RedirectToAction(nameof(Index));
            }

            var bloqueado = user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow;
            if (bloqueado)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                TempData["Success"] = $"Usuario {user.Email} desbloqueado.";
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                TempData["Success"] = $"Usuario {user.Email} bloqueado.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Usuarios/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string userId, string nuevaPassword)
        {
            if (string.IsNullOrWhiteSpace(nuevaPassword) || nuevaPassword.Length < 6)
            {
                TempData["Error"] = "La contraseña debe tener al menos 6 caracteres.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var token  = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, nuevaPassword);

            if (result.Succeeded)
                TempData["Success"] = $"Contraseña de {user.Email} restablecida correctamente.";
            else
                TempData["Error"] = "Error: " + string.Join(", ", result.Errors.Select(e => e.Description));

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Usuarios/ConfirmarEmail
        // Permite al Admin confirmar manualmente el correo de un usuario
        // (útil cuando el email de confirmación no llegó por configuración de Resend)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarEmail(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            if (user.EmailConfirmed)
            {
                TempData["Success"] = $"El correo de {user.Email} ya estaba confirmado.";
                return RedirectToAction(nameof(Index));
            }

            var token  = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (result.Succeeded)
                TempData["Success"] = $"✅ Correo de {user.Email} confirmado manualmente.";
            else
                TempData["Error"] = "Error al confirmar: " + string.Join(", ", result.Errors.Select(e => e.Description));

            return RedirectToAction(nameof(Index));
        }
    }
}
