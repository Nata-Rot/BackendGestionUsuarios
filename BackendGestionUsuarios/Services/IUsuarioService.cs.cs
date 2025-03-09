
// Services/IUsuarioService.cs
using BackendGestionUsuarios.API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GestionUsuarios.API.Services
{
    public interface IUsuarioService
    {
        Task<IEnumerable<UsuarioDTO>> ObtenerTodosUsuariosAsync();
        Task<UsuarioDTO> ObtenerUsuarioPorIdAsync(int id);
        Task<UsuarioDTO> CrearUsuarioAsync(CrearUsuarioDTO usuarioDto);
        Task<bool> ActualizarUsuarioAsync(int id, ActualizarUsuarioDTO usuarioDto);
        Task<bool> EliminarUsuarioAsync(int id);
        Task<LoginResponseDTO> LoginAsync(LoginDTO loginDto);
    }
}