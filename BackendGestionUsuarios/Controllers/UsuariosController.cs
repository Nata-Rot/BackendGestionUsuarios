
// Controllers/UsuariosController.cs
using BackendGestionUsuarios.API.Models;
using GestionUsuarios.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BackendGestionUsuarios.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;

        public UsuariosController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        // GET: api/Usuarios
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<UsuarioDTO>>> GetUsuarios()
        {
            var usuarios = await _usuarioService.ObtenerTodosUsuariosAsync();
            return Ok(usuarios);
        }

        // GET: api/Usuarios/5
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<UsuarioDTO>> GetUsuario(int id)
        {
            var usuario = await _usuarioService.ObtenerUsuarioPorIdAsync(id);

            if (usuario == null)
                return NotFound(new { Message = "Usuario no encontrado" });

            return Ok(usuario);
        }

        // POST: api/Usuarios
        [HttpPost]
        public async Task<ActionResult<UsuarioDTO>> CreateUsuario(CrearUsuarioDTO usuarioDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuario = await _usuarioService.CrearUsuarioAsync(usuarioDto);

            if (usuario == null)
                return Conflict(new { Message = "El correo electrónico ya está registrado" });

            return CreatedAtAction(nameof(GetUsuario), new { id = usuario.Id }, usuario);
        }

        // PUT: api/Usuarios/5
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateUsuario(int id, ActualizarUsuarioDTO usuarioDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _usuarioService.ActualizarUsuarioAsync(id, usuarioDto);

            if (!result)
                return NotFound(new { Message = "Usuario no encontrado" });

            return NoContent();
        }

        // DELETE: api/Usuarios/5
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            var result = await _usuarioService.EliminarUsuarioAsync(id);

            if (!result)
                return NotFound(new { Message = "Usuario no encontrado" });

            return NoContent();
        }

        // POST: api/Usuarios/login
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDTO>> Login(LoginDTO loginDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var response = await _usuarioService.LoginAsync(loginDto);

                if (response == null)
                    return Unauthorized(new { Message = "Credenciales inválidas" });

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error interno", Details = ex.Message });
            }
        }

    }
}