using Micro_Social_Platform.Data;
using Micro_Social_Platform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Micro_Social_Platform.Controllers
{
    public class CommentsController : Controller
    {
        private readonly ApplicationDbContext db;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly RoleManager<IdentityRole> _roleManager;

        public CommentsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager
            )
        {
            db = context;

            _userManager = userManager;

            _roleManager = roleManager;
        }


        //stergerea unui comentariu asociat articolului cu id ul id din baza de date
        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public IActionResult Delete(int id)
        {
            Comment comm = db.Comments.Find(id);
            //daca utilizatorul curent este chiar utilizatorul care a postat comentariul sau
            //utilizatorul curent este are rol de admin, il lasam sa stearga comentariul
            if (comm.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                db.Comments.Remove(comm);
                db.SaveChanges();
                TempData["message"] = "Comment successfully deleted!";
                TempData["messageType"] = "alert-success";
                return Redirect("/Posts/Show/" + comm.PostId);
            }
            else //altfel afisam mesaj de avertizare
            {
                TempData["message"] = "You don't have permission to delete this comment!";
                TempData["messageType"] = "alert-danger";
                return Redirect("/Posts/Show/" + comm.PostId);
            }

        }


        //implementarea editarii intr-o pagina View separata
        //editare comm existent

        [Authorize(Roles = "User,Admin")] //orice utilizator are dreptul in teorie sa editeze comentarii
        public IActionResult Edit(int id)
        {
            Comment comm = db.Comments.Find(id);
            //verificam aici ca userul curent sa fie posesrul comentariului sau sa fie admin ca sa il poata edita
            if (comm.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                return View(comm);
            }

            else //altfel afisam mesaj de avertizare
            {
                TempData["message"] = "You don't have permission to edit this comment!";
                TempData["messageType"] = "alert-danger";
                return Redirect("/Posts/Show/" + comm.PostId);
            }
        }

        [HttpPost]
        [Authorize(Roles = "User,Admin")] //edit cu post
        public IActionResult Edit(int id, Comment requestComment)
        {
            Comment comm = db.Comments.Find(id);
            if (comm.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {

                if (ModelState.IsValid)
                {
                    comm.Content = requestComment.Content;
                    db.SaveChanges();
                    return Redirect("/Posts/Show/" + comm.PostId);
                }
                else
                {
                    return View(requestComment);
                }
            }
            else
            {
                TempData["message"] = "You don't have permission to make changes!";
                TempData["messageType"] = "alert-danger";
                return Redirect("/Posts/Show/" + comm.PostId);
            }
        }

    }
}