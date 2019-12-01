using CMSShoppingCart.Models.Data;
using CMSShoppingCart.Models.ViewModels.Account;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CMSShoppingCart.Controllers
{
    public class AccountController : Controller
    {
        // GET: Account
        public ActionResult Index()
        {
            return Redirect("~/account/login");
        }

        // GET: /account/login
        public ActionResult Login()
        {
            //confrim that user is not logged in
            string username = User.Identity.Name;

            if (!string.IsNullOrEmpty(username))
                return RedirectToAction("user-profile");

            //return view
            return View();
        }

        // GET: /account/create-account
        [HttpGet]
        [ActionName("create-account")]
        public ActionResult CreateAccount()
        {

            return View("CreateAccount");
        }

        // POST: /account/create-account
        [HttpPost]
        [ActionName("create-account")]
        public ActionResult CreateAccount(UserVM model)
        {
            //check model state
            if (!ModelState.IsValid)
            {
                return View("CreateAccount", model);
            }

            //check if passwords match
            if (!model.Password.Equals(model.ConfirmPassword))
            {
                ModelState.AddModelError("", "Passwords do not match.");
                return View("CreateAccount", model);

            }

            using (Db db = new Db())
            {
                //make sure username is unique
                if(db.Users.Any(x => x.Username.Equals(model.Username)))
                {
                    ModelState.AddModelError("", "Username"+model.Username+"is taken.");
                    model.Username = "";
                    return View("CreateAccount", model);
                }

                //create userDTO
                UserDTO userDTO = new UserDTO()
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    EmailAddress = model.EmailAddress,
                    Username = model.Username,
                    Password = model.Password
                };

                //add the DTO
                db.Users.Add(userDTO);

                //Save
                db.SaveChanges();

                //Add to userRolesDTO
                int id = userDTO.Id;

                UserRoleDTO userRolesDTO = new UserRoleDTO()
                {
                    UserId = id,
                    RoleId = 2
                };

                db.UserRoles.Add(userRolesDTO);
                db.SaveChanges();
            }

            //create a TempData message
            TempData["SM"] = "You are now registered and can login";

            //Redirect
            return Redirect("~/account/login");
        }
    }
}