using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using HZ.Crawler.WebUI.Models;
using HZ.Crawler.Data;

namespace HZ.Crawler.WebUI.Controllers
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
            return View();
        }

        public IActionResult Product()
        {
            var productList = this._context.ProductModels.OrderBy(p => p.UpdateDate);
            return View(productList.ToArray());
        }

        public IActionResult Category()
        {
            var categoryList = this._context.CategoryModels.OrderBy(c => c.UpdateDate);
            return View(categoryList.ToArray());
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
