using System.ComponentModel.DataAnnotations;

namespace BackendGestionUsuarios.API.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Apellidos { get; set; }
        public string Cedula { get; set; }
        public string CorreoElectronico { get; set; }
        public DateTime? FechaUltimoAcceso { get; set; }
        public string TipoUsuario { get; set; }
        public int? Puntaje { get; set; }
        public byte[] Contrasena { get; set; }
    }

    public class UsuarioDTO
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public string? Apellidos { get; set; }
        public string? Cedula { get; set; }
        public string? CorreoElectronico { get; set; }
        public DateTime? FechaUltimoAcceso { get; set; }
        public string? TipoUsuario { get; set; }
        public int? Puntaje { get; set; }
    }

    public class CrearUsuarioDTO
    {
        public string? Nombre { get; set; }
        public string? Apellidos { get; set; }
        public string? Cedula { get; set; }
        public string? CorreoElectronico { get; set; }
        public string? Contrasena { get; set; }
    }

    public class ActualizarUsuarioDTO
    {
        public string? Nombre { get; set; }
        public string? Apellidos { get; set; }
        public string? CorreoElectronico { get; set; }
    }

    public class LoginDTO
    {
        [Required]
        public string? CorreoElectronico { get; set; }

        [Required]
        public string? Contrasena { get; set; }
    }

    public class LoginResponseDTO
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public string? Apellidos { get; set; }
        public string ?Token { get; set; }
    }

}