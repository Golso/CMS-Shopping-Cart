using CMSShoppingCart.Models.Data;
using CMSShoppingCart.Models.ViewModels.Account;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

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
        [HttpGet]
        public ActionResult Login()
        {
            //confrim that user is not logged in
            string username = User.Identity.Name;

            if (!string.IsNullOrEmpty(username))
                return RedirectToAction("user-profile");

            //return view
            return View();
        }

        // POST: /account/login
        [HttpPost]
        public ActionResult Login(LoginUserVM model)
        {
            //check model state
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            //check if the user is valid
            bool isValid = false;
            using (Db db = new Db())
            {
                if(db.Users.Any(x=>x.Username.Equals(model.Username) && x.Password.Equals(model.Password)))
                {
                    isValid = true;
                }

                if(!isValid)
                {
                    ModelState.AddModelError("", "Invalid username or password.");
                    return View(model);
                }
                else
                {
                    FormsAuthentication.SetAuthCookie(model.Username, model.RememberMe);
                    return Redirect(FormsAuthentication.GetRedirectUrl(model.Username, model.RememberMe));
                }
            }
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

        // GET: /account/logout
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return Redirect("~/account/login");
        }

        public ActionResult UserNavPartial()
        {
            //get username
            string username = User.Identity.Name;

            //declare model
            UserNavPartialVM model;

            using (Db db = new Db())
            {
                //get the user
                UserDTO dto = db.Users.FirstOrDefault(x => x.Username == username);

                //build the model
                model = new UserNavPartialVM()
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName
                };
            }

            //return partial view with model
            return PartialView(model);
        }

        // GET: /account/user-profile
        [HttpGet]
        [ActionName("user-profile")]
        public ActionResult UserProfile()
        {
            //get username
            string username = User.Identity.Name;

            //declare model
            UserProfileVM model;

            using (Db db = new Db())
            {
                //get user
                UserDTO dto = db.Users.FirstOrDefault(x => x.Username == username);

                //build model
                model = new UserProfileVM(dto);
            }

            //return view with model
            return View("UserProfile", model);
        }

        // POST: /account/user-profile
        [HttpPost]
        [ActionName("user-profile")]
        public ActionResult UserProfile(UserProfileVM model)
        {
            //check model state
            if(!ModelState.IsValid)
            {
                return View("UserProfile", model);
            }

            //check if passwords match if need be
            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                if(!model.Password.Equals(model.ConfirmPassword))
                {
                    ModelState.AddModelError("", "Passwords do not match.");
                    return View("UserProfile", model);
                }
            }

            using (Db db = new Db())
            {
                //get username
                string username = User.Identity.Name;

                //make sure username is unique
                if(db.Users.Where(x => x.Id != model.Id).Any(x => x.Username == username))
                {
                    ModelState.AddModelError("", "Username"+model.Username+" already exists.");
                    model.Username = "";
                    return View("UserProfile", model);
                }

                //edit DTO
                UserDTO dto = db.Users.Find(model.Id);

                dto.FirstName = model.FirstName;
                dto.LastName = model.LastName;
                dto.EmailAddress = model.EmailAddress;
                dto.Username = model.Username;

                if (!string.IsNullOrWhiteSpace(model.Password))
                {
                    dto.Password = model.Password;
                }

                //save
                db.SaveChanges();
            }

            //set TempData message
            TempData["SM"] = "You have edited your profile.";

            //redirect
            return Redirect("~/account/user-profile");
        }
    }
}