using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using SaboresDeMiTierra.Models;

namespace SaboresDeMiTierra.Controllers
{
    public class LoginController : Controller
    {
        private readonly IConfiguration _configuration;

        public LoginController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (HttpContext.Session.GetInt32("UsuarioId") != null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Entrar(string login, string senha)
        {
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(senha))
            {
                ViewBag.Erro = "Informe o login e a senha.";
                return View("Index");
            }

            string conexao = _configuration.GetConnectionString("DefaultConnection")!;

            using var conn = new MySqlConnection(conexao);
            conn.Open();

            const string sql = @"
                SELECT Id, Nome, Login, Senha, Cargo
                FROM Usuarios
                WHERE Login = @Login
                LIMIT 1";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Login", login.Trim());

            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
            {
                ViewBag.Erro = "Login ou senha inválidos.";
                return View("Index");
            }

            var usuario = new Usuario
            {
                Id = Convert.ToInt32(reader["Id"]),
                Nome = reader["Nome"]?.ToString() ?? "",
                Login = reader["Login"]?.ToString() ?? "",
                Senha = reader["Senha"]?.ToString() ?? "",
                Cargo = reader["Cargo"]?.ToString() ?? ""
            };

            var hasher = new PasswordHasher<Usuario>();

            PasswordVerificationResult resultado = hasher.VerifyHashedPassword(
                usuario,
                usuario.Senha,
                senha
            );

            if (resultado == PasswordVerificationResult.Failed)
            {
                ViewBag.Erro = "Login ou senha inválidos.";
                return View("Index");
            }

            HttpContext.Session.SetInt32("UsuarioId", usuario.Id);
            HttpContext.Session.SetString("UsuarioNome", usuario.Nome);
            HttpContext.Session.SetString("UsuarioLogin", usuario.Login);
            HttpContext.Session.SetString("UsuarioCargo", usuario.Cargo);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Sair()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Login");
        }
    }
}
