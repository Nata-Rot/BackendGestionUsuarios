
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
                {
                    Console.WriteLine("Usuario no encontrado");
                    return null;
                }

                Console.WriteLine($"Usuario encontrado: {usuario.Nombre}");
                Console.WriteLine($"Contraseña proporcionada: {loginDto.Contrasena}");

                if (usuario.Contrasena == null)
                {
                    Console.WriteLine("La contraseña almacenada es null");
                    return null;
                }

                Console.WriteLine($"Longitud de contraseña almacenada: {usuario.Contrasena.Length} bytes");

                // Crear directamente el hash aquí para comparar
                using (var sha256 = SHA256.Create())
                {
                    byte[] hashEntrada = sha256.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Contrasena));
                    string hashEntradaHex = BitConverter.ToString(hashEntrada).Replace("-", "");
                    string hashAlmacenadoHex = BitConverter.ToString(usuario.Contrasena).Replace("-", "");

                    Console.WriteLine($"Hash de entrada: {hashEntradaHex}");
                    Console.WriteLine($"Hash almacenado: {hashAlmacenadoHex}");
                    Console.WriteLine($"¿Coinciden? {hashEntradaHex.Equals(hashAlmacenadoHex, StringComparison.OrdinalIgnoreCase)}");

                    if (hashEntradaHex.Equals(hashAlmacenadoHex, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("Autenticación exitosa mediante comparación directa");
                        await _repository.ActualizarFechaAccesoAsync(usuario.Id);

                        var jwtToken = GenerateJwtToken(usuario);

                        return new LoginResponseDTO
                        {
                            Id = usuario.Id,
                            Nombre = usuario.Nombre,
                            Apellidos = usuario.Apellidos,
                            Token = jwtToken
                        };
                    }
                    else
                    {
                        Console.WriteLine("Las contraseñas no coinciden");
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en login: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
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
            else if (horasTranscurridas <= 168) 
                return "Explorador";
            else
                return "Olvidado";
        }

        private int CalcularPuntaje(string nombre, string apellidos, string correo)
        {
            int puntaje = 0;
            string nombreCompleto = $"{nombre} {apellidos}";

            if (nombreCompleto.Length > 10)
                puntaje += 20;
            else if (nombreCompleto.Length >= 5)
                puntaje += 10;

            string dominio = correo.Split('@').Last().ToLower();
            if (dominio == "gmail.com")
                puntaje += 40;
            else if (dominio == "hotmail.com")
                puntaje += 20;
            else
                puntaje += 10;

            return puntaje;
        }

        public byte[] HashPassword(string password)
        {
            try
            {
                using (var sha256 = SHA256.Create())
                {
                    // Asegúrate de usar una codificación consistente
                    byte[] bytes = Encoding.UTF8.GetBytes(password);
                    byte[] hash = sha256.ComputeHash(bytes);

                    // Para depuración, muestra el hash generado
                    Console.WriteLine($"HashPassword generó: {BitConverter.ToString(hash)}");

                    return hash;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en HashPassword: {ex.Message}");
                throw;
            }
        }


        // Modificación para VerifyPassword
        private bool VerifyPassword(string passwordInput, byte[] storedPassword)
        {
            if (storedPassword == null || storedPassword.Length == 0)
            {
                Console.WriteLine("El hash almacenado es null o vacío");
                return false;
            }

            try
            {
                using (var sha256 = SHA256.Create())
                {
                    byte[] hashIngresado = sha256.ComputeHash(Encoding.UTF8.GetBytes(passwordInput));

                    // Usar un método más directo para comparar
                    return storedPassword.SequenceEqual(hashIngresado);

                    // O alternativamente:
                    // return Convert.ToBase64String(storedPassword) == Convert.ToBase64String(hashIngresado);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en VerifyPassword: {ex.Message}");
                return false;
            }
        }
        private bool CryptographicEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;

            int result = 0;
            for (int i = 0; i < a.Length; i++)
            {
                result |= a[i] ^ b[i];
            }

            return result == 0;
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