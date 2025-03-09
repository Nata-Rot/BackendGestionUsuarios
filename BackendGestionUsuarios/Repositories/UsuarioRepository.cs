using BackendGestionUsuarios.API.Data;
using BackendGestionUsuarios.API.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BackendGestionUsuarios.API.Repositories
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly AppDbContext _context;

        public UsuarioRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Usuario>> ObtenerTodosUsuariosAsync()
        {
            return await _context.Usuarios.ToListAsync();
        }

        public async Task<Usuario> ObtenerUsuarioPorIdAsync(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            return usuario ?? throw new KeyNotFoundException($"Usuario con ID {id} no encontrado.");
        }

        public async Task<Usuario> LoginAsync(LoginDTO loginDto)
        {
            var usuario = await _context.Usuarios
                .Where(u => u.CorreoElectronico == loginDto.CorreoElectronico)
                .FirstOrDefaultAsync();

            if (usuario == null || usuario.Contrasena == null)
                return null;

            using (var sha256 = SHA256.Create())
            {
                byte[] hashIngresado = sha256.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Contrasena));

                if (!usuario.Contrasena.SequenceEqual(hashIngresado))
                    return null;
            }

            return usuario;
        }



        public async Task<Usuario> CrearUsuarioAsync(Usuario usuario)
        {
            await _context.Usuarios.AddAsync(usuario);
            await _context.SaveChangesAsync();
            return usuario;
        }

        public async Task<bool> ActualizarUsuarioAsync(Usuario usuario)
        {
            _context.Usuarios.Update(usuario);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> EliminarUsuarioAsync(int id)
        {
            var usuario = await ObtenerUsuarioPorIdAsync(id);
            if (usuario == null)
                return false;

            _context.Usuarios.Remove(usuario);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<Usuario> ActualizarFechaAccesoAsync(int id)
        {
            var usuario = await ObtenerUsuarioPorIdAsync(id);
            usuario.FechaUltimoAcceso = DateTime.Now;
            _context.Usuarios.Update(usuario);
            await _context.SaveChangesAsync();
            return usuario;
        }
        public async Task<Usuario> ObtenerUsuarioPorCorreoAsync(string correo)
        {
            return await _context.Usuarios
                .Where(u => u.CorreoElectronico == correo)
                .FirstOrDefaultAsync();
        }

    }
}
