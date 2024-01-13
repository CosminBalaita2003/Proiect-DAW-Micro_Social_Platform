using Micro_Social_Platform.Data;
using Micro_Social_Platform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Micro_Social_Platform.Controllers
{
    public class MessagesController : Controller
    {
        private readonly ApplicationDbContext db;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly RoleManager<IdentityRole> _roleManager;

        public MessagesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager
            )
        {
            db = context;

            _userManager = userManager;

            _roleManager = roleManager;
        }

        //stergere mesaj
        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public IActionResult Delete(int id)
        {
            Message mess = db.Messages.Find(id); //cautam mesajul dupa id
            //doar daca mesajul apartine userului curent sau userul curent este admin il lasam sa stearga
            if (mess.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                db.Messages.Remove(mess);
                db.SaveChanges();
                TempData["message"] = "Message successfully deleted!";
                TempData["messageType"] = "alert-success";
                return Redirect("/Groups/Show/" + mess.GroupId);
            }
            else
            {
                TempData["message"] = "You don't have permission to delete this message!";
                TempData["messageType"] = "alert-danger";
                return Redirect("/Groups/Show/" + mess.GroupId); ;
            }

        }
        [Authorize(Roles = "User,Admin")]
        public IActionResult Edit(int id)
        {
            Message mess = db.Messages.Find(id);
            //la fel ca mai sus, doar userul care a postat mesajul sau adminul le pot edita
            if (mess.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                return View(mess);
            }
            else
            {
                TempData["message"] = "You don't have permission to edit this message!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", "Groups");
            }
        }
        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public IActionResult Edit(int id, Message requestMess)
        {
            Message mess = db.Messages.Find(id);
            if (ModelState.IsValid)
            {
                if (mess.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
                {
                    mess.Content = requestMess.Content;
                    db.SaveChanges();
                    return Redirect("/Groups/Show/" + mess.GroupId);
                }
                else
                {
                    TempData["message"] = "You don't have permission to edit this message";
                    TempData["messageType"] = "alert-danger";
                    return RedirectToAction("Show", "Groups");
                }
            }
            else
            {
                return View(requestMess);
            }
        }
    }
}