using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication3.Models;
using WebApplication3.BusinessLayer;
using System.Threading.Tasks;

namespace WebApplication3.Controllers
{
    public class UserController : Controller
    {
        // GET: User
        public ActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<ActionResult> Index(UserModel user)
        {
            string token = await AzureAuthentication.createToken();
            bool result = AzureAuthentication.DoesUserExistsAsync("ajaychawla@akcorp1.onmicrosoft.com", token);
            await AzureAuthentication.CreateUserAsync(user.userName, token);
            AzureAuthentication.UpdateUser(user.userName, user.password, token);
            AzureAuthentication.DeleteUser(user.userName, token);
            return View();
        }
    }
}