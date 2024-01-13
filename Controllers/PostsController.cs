using Ganss.Xss;
using Micro_Social_Platform.Data;
using Micro_Social_Platform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace Micro_Social_Platform.Controllers
{
    //[Authorize]
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext db;

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public PostsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }
        //afisare toate postarile 

        public IActionResult Index()
        {
            //postarile utilizatorilor care nu au cont privat (daca userul curent nu e userul care a facut postarea)
            var posts = db.Posts.Include("User").Where(p => p.User.IsPrivate == false || p.UserId == _userManager.GetUserId(User)).OrderByDescending(p => p.Date).ToList();
            ViewBag.Posts = posts;
            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            return View();
        }

        //afisare postare dupa id
        [Authorize(Roles = "User,Admin")]
        public IActionResult Show(int id)
        {
            //includem in afisarea postarii (show) si comentariile si userul care le-a facut
            var post = db.Posts.Include("Comments").Include("User").Include("Comments.User").Where(p => p.Id == id).First();
            SetAccessRights();
            return View(post);
        }

        //Adaugare postare noua
        //aici orice utilizator (daca are cont) poate adauga o postare
        //deci daca rolul exista fie el user sau admin, o poate face
        [Authorize(Roles = "User,Admin")]
        public IActionResult New()
        {
            Post post = new Post();
            return View(post);
        }
        [Authorize(Roles = "User,Admin")]
        [HttpPost]
        public IActionResult New(Post post)
        {
            var sanitizer = new HtmlSanitizer();
            post.Date = DateTime.Now; //momentul de acum
            post.UserId = _userManager.GetUserId(User); //salvam userul care a facut postarea
            if (ModelState.IsValid)
            {
                post.Content = sanitizer.Sanitize(post.Content);
                db.Posts.Add(post);
                db.SaveChanges();
                TempData["message"] = "Post successfully added!";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Index");
            }
            else
            {
                return View(post);
            }
        }
        [Authorize(Roles = "User,Admin")]
        //edit pentru postari, oricine are acces la alta metoda, verificam mai jos mai multe
        public IActionResult Edit(int id)
        {
            var post = db.Posts.Find(id);//cautam postarea in baza de date dupa id
            //daca utilizatorul care vrea sa editeze postarea este detinatorul ei sau este admin il lasam
            if (post.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                return View(post);
            }
            else //altfel returnam mesaj de avertizare
            {
                TempData["message"] = "You don't have permission to edit this post!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

        }
        [Authorize(Roles = "User,Admin")]
        [HttpPost]
        public IActionResult Edit(int id, Post requestPost)
        {
            var sanitizer = new HtmlSanitizer();
            Post post = db.Posts.Find(id);
            if (ModelState.IsValid)
            {
                //verificam ca postarea sa fie a userului curent sau el sa fie admin
                if (post.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
                {
                    requestPost.Content = sanitizer.Sanitize(requestPost.Content);
                    post.Content = requestPost.Content;
                    TempData["message"] = "Post successfully edited!";
                    TempData["messageType"] = "alert-success";
                    db.SaveChanges();

                    return Redirect("/Posts/Show/" + post.Id);
                }
                else
                {
                    TempData["message"] = "You don't have permission to edit this post!";
                    TempData["messageType"] = "alert-danger";
                    return RedirectToAction("Index");
                }

            }
            else
            {
                return View(requestPost);
            }

        }
        //Stergere postare
        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public IActionResult Delete(int id)
        {
            //includem comentariile pentru ca si ele trebuie sterse odata cu postarea
            Post post = db.Posts.Include("Comments")
                                         .Where(p => p.Id == id)
                                         .First();
            //verificam ca postarea sa fie al userului curent sau sa fie admin, altfel nu lasam sa stearga
            if (post.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                db.Posts.Remove(post);
                db.SaveChanges();
                TempData["message"] = "Post successfully deleted!";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["message"] = "You don't have permission to delete this post!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

        }
        //conditiile de afisare a butoanelor de editare si stergere
        private void SetAccessRights()
        {
            ViewBag.AfisareButoane = false;

            //if (User.IsInRole("Editor") || User.IsInRole("User")) --am eliminat editor pentru ca nu are drepturi in plus
            //fata de user si in minus fata de admin
            if (User.IsInRole("User"))
            {
                ViewBag.AfisareButoane = true;
            }

            ViewBag.EsteAdmin = User.IsInRole("Admin");

            ViewBag.UserCurent = _userManager.GetUserId(User);
        }


        //Adaugare comentariu la postare
        [HttpPost]

        [Authorize(Roles = "User,Admin")]
        public IActionResult Show([FromForm] Comment comment)
        {
            comment.Date = DateTime.Now;

            // preluam id-ul utilizatorului care posteaza comentariul
            comment.UserId = _userManager.GetUserId(User);

            if (ModelState.IsValid)
            {
                db.Comments.Add(comment);
                db.SaveChanges();
                return Redirect("/Posts/Show/" + comment.PostId);
            }

            else
            {
                Post post = db.Posts.Include("User")
                                         .Include("Comments")
                                         .Include("Comments.User")
                                         .Where(post => post.Id == comment.PostId)
                                         .First();


                SetAccessRights();

                return View(post);
            }
        }


    }
}