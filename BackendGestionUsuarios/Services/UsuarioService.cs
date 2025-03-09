
using BackendGestionUsuarios.API.Models;
using BackendGestionUsuarios.API.Repositories;
using GestionUsuarios.API.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BackendGestionUsuarios.API.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IUsuarioRepository _repository;
        private readonly IConfiguration _configuration;

        public UsuarioService(IUsuarioRepository repository, IConfiguration configuration)
        {
            _repository = repository;
            _configuration = configuration;
        }

        public async Task<IEnumerable<UsuarioDTO>> ObtenerTodosUsuariosAsync()
        {
            var usuarios = await _repository.ObtenerTodosUsuariosAsync();

            return usuarios.Select(u => new UsuarioDTO
            {
                Id = u.Id,
                Nombre = u.Nombre,
                Apellidos = u.Apellidos,
                Cedula = u.Cedula,
                CorreoElectronico = u.CorreoElectronico,
                FechaUltimoAcceso = u.FechaUltimoAcceso,
                TipoUsuario = CalcularTipoUsuario(u.FechaUltimoAcceso),
                Puntaje = CalcularPuntaje(u.Nombre, u.Apellidos, u.CorreoElectronico)
            });
        }

        public async Task<UsuarioDTO> ObtenerUsuarioPorIdAsync(int id)
        {
            var usuario = await _repository.ObtenerUsuarioPorIdAsync(id);
            if (usuario == null)
                return null;

            return new UsuarioDTO
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Apellidos = usuario.Apellidos,
                Cedula = usuario.Cedula,
                CorreoElectronico = usuario.CorreoElectronico,
                FechaUltimoAcceso = usuario.FechaUltimoAcceso,
                TipoUsuario = CalcularTipoUsuario(usuario.FechaUltimoAcceso),
                Puntaje = CalcularPuntaje(usuario.Nombre, usuario.Apellidos, usuario.CorreoElectronico)
            };
        }

        public async Task<UsuarioDTO> CrearUsuarioAsync(CrearUsuarioDTO usuarioDto)
        {
            // Verificar si el correo ya existe
            var usuarioExistente = await _repository.ObtenerUsuarioPorCorreoAsync(usuarioDto.CorreoElectronico);
            if (usuarioExistente != null)
                return null;

            var usuario = new Usuario
            {
                Nombre = usuarioDto.Nombre,
                Apellidos = usuarioDto.Apellidos,
                Cedula = usuarioDto.Cedula,
                CorreoElectronico = usuarioDto.CorreoElectronico,
                Contrasena = HashPassword(usuarioDto.Contrasena),
                FechaUltimoAcceso = DateTime.Now,
            };

            usuario.TipoUsuario = CalcularTipoUsuario(usuario.FechaUltimoAcceso);
            usuario.Puntaje = CalcularPuntaje(usuario.Nombre, usuario.Apellidos, usuario.CorreoElectronico);

            await _repository.CrearUsuarioAsync(usuario);

            return new UsuarioDTO
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Apellidos = usuario.Apellidos,
                Cedula = usuario.Cedula,
                CorreoElectronico = usuario.CorreoElectronico,
                FechaUltimoAcceso = usuario.FechaUltimoAcceso,
                TipoUsuario = usuario.TipoUsuario,
                Puntaje = usuario.Puntaje
            };
        }

        public async Task<bool> ActualizarUsuarioAsync(int id, ActualizarUsuarioDTO usuarioDto)
        {
            var usuario = await _repository.ObtenerUsuarioPorIdAsync(id);
            if (usuario == null)
                return false;

            usuario.Nombre = usuarioDto.Nombre;
            usuario.Apellidos = usuarioDto.Apellidos;
            usuario.CorreoElectronico = usuarioDto.CorreoElectronico;
            usuario.Puntaje = CalcularPuntaje(usuario.Nombre, usuario.Apellidos, usuario.CorreoElectronico);

            return await _repository.ActualizarUsuarioAsync(usuario);
        }

        public async Task<bool> EliminarUsuarioAsync(int id)
        {
            return await _repository.EliminarUsuarioAsync(id);
        }

        public async Task<LoginResponseDTO> LoginAsync(LoginDTO loginDto)
        {
            try
            {
                var usuario = await _repository.ObtenerUsuarioPorCorreoAsync(loginDto.CorreoElectronico);

                if (usuario == null)
                    return null;

                // Verificar contraseña con el formato correcto
                if (!VerifyPassword(loginDto.Contrasena, usuario.Contrasena))
                    return null;

                // Actualizar fecha de acceso
                await _repository.ActualizarFechaAccesoAsync(usuario.Id);

                // Generar token JWT
                var token = GenerateJwtToken(usuario);

                return new LoginResponseDTO
                {
                    Id = usuario.Id,
                    Nombre = usuario.Nombre,
                    Apellidos = usuario.Apellidos,
                    Token = token
                };
            }
            catch (Exception ex)
            {
                // Agregar logging
                Console.WriteLine($"Error en login: {ex.Message}");
                throw; // Re-lanzar la excepción para que el controlador pueda manejarla
            }
        }

        private string CalcularTipoUsuario(DateTime? ultimoAcceso)
        {
            if (!ultimoAcceso.HasValue)
                return "Olvidado";

            var horasTranscurridas = (DateTime.Now - ultimoAcceso.Value).TotalHours;

            if (horasTranscurridas <= 12)
                return "Hechicero";
            else if (horasTranscurridas <= 48)
                return "Luchador";
            else if (horasTranscurridas <= 168) // 7 días * 24 horas
                return "Explorador";
            else
                return "Olvidado";
        }

        private int CalcularPuntaje(string nombre, string apellidos, string correo)
        {
            int puntaje = 0;
            string nombreCompleto = $"{nombre} {apellidos}";

            // Calcular puntos por longitud del nombre
            if (nombreCompleto.Length > 10)
                puntaje += 20;
            else if (nombreCompleto.Length >= 5)
                puntaje += 10;

            // Calcular puntos por dominio de correo
            string dominio = correo.Split('@').Last().ToLower();
            if (dominio == "gmail.com")
                puntaje += 40;
            else if (dominio == "hotmail.com")
                puntaje += 20;
            else
                puntaje += 10;

            return puntaje;
        }

        // Ejemplo de método para generar hash de contraseña para crear usuarios
        public byte[] HashPassword(string password)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                // El salt es generado automáticamente por HMACSHA512
                var salt = hmac.Key;
                var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));

                // Combinar salt y hash
                var hashBytes = new byte[salt.Length + hash.Length];
                Buffer.BlockCopy(salt, 0, hashBytes, 0, salt.Length);
                Buffer.BlockCopy(hash, 0, hashBytes, salt.Length, hash.Length);

                return hashBytes;
            }
        }

        private bool VerifyPassword(string passwordInput, byte[] storedPassword)
        {
            // Asumiendo que los primeros 16-32 bytes son el salt y el resto es el hash
            // Ajusta esto según tu implementación específica
            const int saltSize = 16; // o 32 dependiendo de tu implementación

            byte[] salt = new byte[saltSize];
            byte[] hash = new byte[storedPassword.Length - saltSize];

            // Extraer salt y hash del password almacenado
            Buffer.BlockCopy(storedPassword, 0, salt, 0, saltSize);
            Buffer.BlockCopy(storedPassword, saltSize, hash, 0, hash.Length);

            // Calcular hash de la contraseña ingresada con el mismo salt
            using (var hmac = new System.Security.Cryptography.HMACSHA512(salt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(passwordInput));

                // Comparar los hashes
                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != hash[i])
                        return false;
                }
            }

            return true;
        }


        private string GenerateJwtToken(Usuario usuario)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, usuario.CorreoElectronico),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(3),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}