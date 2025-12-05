using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TravelShare.Models;
using TravelShare.Data;
using System.Linq;

namespace TravelShare.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Login
        public IActionResult Login()
        {
            // If user is already logged in, redirect to dashboard
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
            {
                return RedirectToAction("Dashboard", "Home");
            }
            return View();
        }

        [HttpPost]
[HttpPost]
[HttpPost]
public IActionResult Login(string email, string password)
{
    if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
    {
        ViewBag.Error = "Please enter both email and password";
        return View();
    }

    var user = _context.Users.FirstOrDefault(u => u.Email == email && u.Password == password);
    if (user != null)
    {
        // Clear old session
        HttpContext.Session.Clear();
        
        // Set session variables
        HttpContext.Session.SetString("UserEmail", user.Email);
        HttpContext.Session.SetString("UserName", user.FullName);
        HttpContext.Session.SetInt32("UserId", user.Id);
        
        // SIMPLE LOGIC: If email is admin@travel.com, set IsAdmin = true
        bool isAdmin = (user.Email == "admin@travel.com") || user.IsAdmin;
        HttpContext.Session.SetString("IsAdmin", isAdmin.ToString());
        
        // DEBUG: Write to console
        Console.WriteLine($"LOGIN: {user.Email}, IsAdmin in DB: {user.IsAdmin}, IsAdmin in Session: {isAdmin}");
        
        return RedirectToAction("Dashboard", "Home");
    }

    ViewBag.Error = "Invalid email or password";
    return View();
}

        // GET: /Account/Register
        public IActionResult Register()
        {
            // If user is already logged in, redirect to dashboard
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
            {
                return RedirectToAction("Dashboard", "Home");
            }
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(User user)
        {
            if (ModelState.IsValid)
            {
                // Check if email already exists
                var existingUser = _context.Users.FirstOrDefault(u => u.Email == user.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email already exists. Please use a different email.");
                    return View(user);
                }

                // Add new user
                _context.Users.Add(user);
                _context.SaveChanges();

                // Auto-login after registration
                HttpContext.Session.SetString("UserEmail", user.Email);
                HttpContext.Session.SetString("UserName", user.FullName);
                HttpContext.Session.SetString("IsAdmin", user.IsAdmin.ToString());
                HttpContext.Session.SetInt32("UserId", user.Id);

                return RedirectToAction("Dashboard", "Home");
            }

            return View(user);
        }

        // GET: /Account/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}