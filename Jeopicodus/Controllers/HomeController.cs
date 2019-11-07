using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Jeopicodus.Models;
using Jeopicodus.ViewModels;
using System.Threading.Tasks;

namespace Jeopicodus.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if(User.Identity.IsAuthenticated)
            {
                return View();
            }
            else{
                return RedirectToAction("Login", "Account");
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(FillInTheBlankViewModel newQuestion)
        {
            await FillInTheBlankViewModel.PostQuestion(newQuestion);
            return RedirectToAction("Index");
        }
    }
}
