using CMSShoppingCart.Models.Data;
using CMSShoppingCart.Models.ViewModels.Shop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;

namespace CMSShoppingCart.Areas.Admin.Controllers
{
    public class ShopController : Controller
    {
        // GET: Admin/Shop/Categories
        public ActionResult Categories()
        {
            //declare a list of models
            List<CategoryVM> categoryVMList;

            using (Db db = new Db())
            {
                //init the list
                categoryVMList = db.Categories
                                .ToArray()
                                .OrderBy(x => x.Sorting)
                                .Select(x => new CategoryVM(x))
                                .ToList();
            }

            //return view with list
            return View(categoryVMList);
        }

        // POST: Admin/Shop/AddNewCategory
        [HttpPost]
        public string AddNewCategory(string catName)
        {
            //declare id
            string id;

            using (Db db = new Db())
            {
                //check that category name is unique
                if (db.Categories.Any(x => x.Name == catName))
                {
                    return "titletaken";
                }

                //init DTO
                CategoryDTO dto = new CategoryDTO();

                //add to DTO
                dto.Name = catName;
                dto.Slug = catName.Replace(" ", "-").ToLower();
                dto.Sorting = 100;

                //save DTO
                db.Categories.Add(dto);
                db.SaveChanges();

                //get the id
                id = dto.Id.ToString();
            }

            //return id
            return id;
        }

        // POST: Admin/Shop/ReorderCategories
        [HttpPost]
        public void ReorderCategories(int[] id)
        {
            using (Db db = new Db())
            {
                //set initial count
                int count = 1;

                //declare CategoryDTO
                CategoryDTO dto;

                //set sorting for each category
                foreach (var catId in id)
                {
                    dto = db.Categories.Find(catId);
                    dto.Sorting = count;

                    db.SaveChanges();

                    count++;
                }
            }

        }

        // GET: Admin/Shop/DeleteCategory/id
        public ActionResult DeleteCategory(int id)
        {
            using (Db db = new Db())
            {
                //get the category
                CategoryDTO dto = db.Categories.Find(id);

                //remove the category
                db.Categories.Remove(dto);

                //save
                db.SaveChanges();
            }

            //redirect
            return RedirectToAction("Categories");

        }

        // POST: Admin/Shop/RenameCategory
        [HttpPost]
        public string RenameCategory(string newCatName, int id)
        {
            using (Db db = new Db())
            {
                //check category name is unique
                if (db.Categories.Any(x => x.Name == newCatName))
                    return "titletaken";

                //get DTO
                CategoryDTO dto = db.Categories.Find(id);

                //edit DTO
                dto.Name = newCatName;
                dto.Slug = newCatName.Replace(" ", "-").ToLower();

                //save
                db.SaveChanges();
            }

            //return
            return "ok";
        }

        // GET: Admin/Shop/AddProduct
        [HttpGet]
        public ActionResult AddProduct()
        {
            //init model
            ProductVM model = new ProductVM();

            //add select list of categories to model
            using (Db db = new Db())
            {
                model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
            }

            //return view with model
            return View(model);
        }

        // POST: Admin/Shop/AddProduct
        [HttpPost]
        public ActionResult AddProduct(ProductVM model,HttpPostedFileBase file)
        {
            //check model state
            if(! ModelState.IsValid)
            {
                using (Db db = new Db())
                {
                    model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
                    return View(model);
                }
            }

            //make sure product name is unique
            using (Db db = new Db())
            {
                if (db.Products.Any(x => x.Name == model.Name))
                {
                    model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
                    ModelState.AddModelError("", "That product name is taken.");
                    return View(model);
                }
            }

            //declare product id
            int id;

            //init and save productDTO
            using(Db db = new Db())
            {
                ProductDTO product = new ProductDTO();

                product.Name = model.Name;
                product.Slug = model.Name.Replace(" ", "-").ToLower();
                product.Description = model.Description;
                product.Price = model.Price;
                product.CategoryId = model.CategoryId;

                CategoryDTO catDTO = db.Categories.FirstOrDefault(x => x.Id == model.CategoryId);
                product.CategoryName = catDTO.Name;

                db.Products.Add(product);
                db.SaveChanges();

                //get the id
                id = product.Id;
            }

            //set TempData message
            TempData["SM"] = "You have added a product.";

            #region Upload Image

            //create necessary directories
            var originalDirectory = new DirectoryInfo(string.Format("{0}Images\\Uploads",Server.MapPath(@"\")));

            var pathString1 = Path.Combine(originalDirectory.ToString(), "Products");
            var pathString2 = Path.Combine(originalDirectory.ToString(), "Products\\"+id.ToString());
            var pathString3 = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Thumbs");
            var pathString4 = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Gallery");
            var pathString5 = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Gallery\\Thumbs");

            if (!Directory.Exists(pathString1))
                Directory.CreateDirectory(pathString1);

            if (!Directory.Exists(pathString2))
                Directory.CreateDirectory(pathString2);

            if (!Directory.Exists(pathString3))
                Directory.CreateDirectory(pathString3);

            if (!Directory.Exists(pathString4))
                Directory.CreateDirectory(pathString4);

            if (!Directory.Exists(pathString5))
                Directory.CreateDirectory(pathString5);

            //check if a file was uploaded
            if (file != null && file.ContentLength>0)
            {
                //get file extension
                string ext = file.ContentType.ToLower();

                //verify extension
                if (ext != "image/jpg" &&
                    ext != "image/jpeg" &&
                    ext != "image/pjpeg" &&
                    ext != "image/gif" &&
                    ext != "image/x-png" &&
                    ext != "image/png")
                {
                    using (Db db = new Db())
                    {
                        model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
                        ModelState.AddModelError("", "The image was not uploaded - wrong image extension.");
                        return View(model);
                    }
                }

                //init image name
                string imageName = file.FileName;

                //save image name to DTO
                using (Db db = new Db())
                {
                    ProductDTO dto = db.Products.Find(id);
                    dto.ImageName = imageName;

                    db.SaveChanges();
                }

                //set original and thumb image paths
                var path = string.Format("{0}\\{1}", pathString2, imageName);
                var path2 = string.Format("{0}\\{1}", pathString3, imageName);

                //save original
                file.SaveAs(path);

                //create and save thumb
                WebImage img = new WebImage(file.InputStream);
                img.Resize(200, 200);
                img.Save(path2);
            }

            #endregion

            //Redirect
            return RedirectToAction("AddProduct");
        }
    }
}