using BackendGestionUsuarios.API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BackendGestionUsuarios.API.Repositories
{
    public interface IUsuarioRepository
    {
        Task<IEnumerable<Usuario>> ObtenerTodosUsuariosAsync();
        Task<Usuario> ObtenerUsuarioPorIdAsync(int id);
        Task<Usuario> ObtenerUsuarioPorCorreoAsync(string correo);
        Task<Usuario> CrearUsuarioAsync(Usuario usuario);
        Task<bool> ActualizarUsuarioAsync(Usuario usuario);
        Task<bool> EliminarUsuarioAsync(int id);
        Task<Usuario> ActualizarFechaAccesoAsync(int id); // Devuelve el usuario actualizado
    }
}
