using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using HZ.Crawler.WebUITest.Models;
using HZ.Crawler.Data;

namespace HZ.Crawler.WebUITest.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ShiweijiaContext _context;

        public HomeController(ILogger<HomeController> logger, ShiweijiaContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            _context.BrandModels.Add(new Model.Shiweijia.BrandModel { Id = 11, Logo = "", Name = "品牌" });
            _context.SaveChanges();
            var brand = this._context.BrandModels.Where(b => b.Id == 11);
            string result = System.Text.Json.JsonSerializer.Serialize(brand);
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
