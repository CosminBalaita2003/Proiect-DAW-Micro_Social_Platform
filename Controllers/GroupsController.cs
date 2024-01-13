using Micro_Social_Platform.Data;
using Micro_Social_Platform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Micro_Social_Platform.Controllers
{
    public class GroupsController : Controller
    {
        private readonly ApplicationDbContext db;

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public GroupsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }
        //httpGet implicit
        [Authorize(Roles = "User,Admin")]
        public IActionResult Index()
        {

            //afisare grupuri pentru utilizatori sa le vada
            var groups = db.Groups.Include(g => g.UserGroups).ToList();
            ViewBag.Groups = groups;

            //aici luam useru curent cu _usermanager
            //ca sa l transmitem in View ca sa afisam butonu de adaugare pt userii care nu sunt in grup
            var currentUser = _userManager.GetUserId(User);
            ViewBag.CurrentUser = currentUser;


            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }


            return View();

        }

        //HttpGet implicit
        //adaugare grupuri
        [Authorize(Roles = "User,Admin")]
        public IActionResult New()
        {
            Group group = new Group();
            return View(group);
        }

        [HttpPost]
        [Authorize(Roles = "User,Admin")] //oricine poate crea grup nou
        //dar salvam userid ca sa l trecem in bd, el avand drept de crud asupra grupului
        public IActionResult New(Group newGroup)
        {
            //salvam userul care creaza grupul in UserId
            newGroup.UserId = _userManager.GetUserId(User);

            if (ModelState.IsValid)
            {
                db.Groups.Add(newGroup);
                db.SaveChanges();

                //salvam userul curent intr o variabila ca sa cream o inregistrare
                //in UserGroup adaugand userul care a fct grupul si id-ul grupului
                //ptc in realitate, adminul grupului e automat parte din grup
                var currentUser = _userManager.GetUserId(User);
                var userGroup = new UserGroup
                {
                    UserId = currentUser,
                    GroupId = newGroup.Id
                };

                db.UserGroups.Add(userGroup);
                db.SaveChanges();


                TempData["message"] = "Group successfully created!";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Index");
            }

            else
            {
                return View(newGroup);
            }
        }
        //HttpGet implicit
        [Authorize(Roles = "User,Admin")]
        public IActionResult Show(int id)
        {

            //orice user poate vedea grupurile cu show dar doar daca fac parte din ele

            if (User.IsInRole("Admin") || IsUserInGroup(id, _userManager.GetUserId(User)))
            {
                var group = db.Groups.Include("Messages")
                                     .Include("Messages.User")
                                     .Where(grp => grp.Id == id)
                                     .First();

                return View(group);


            }
            else //inseamna ca ori nu e admin ori userul nu e parte din grup deci nu l lasam sa vada mesajele
            {
                TempData["message"] = "You are not part of this group!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

        }
        ///Mesaj
        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public IActionResult Show(int id, [FromForm] Message message)
        {

            if (User.IsInRole("Admin") || IsUserInGroup(id, _userManager.GetUserId(User)))
            {
                message.Date = DateTime.Now; //momentul curent
                message.UserId = _userManager.GetUserId(User); //userul curent
                message.GroupId = id;

                if (ModelState.IsValid)
                {
                    db.Messages.Add(message);
                    db.SaveChanges();
                    return Redirect("/Groups/Show/" + message.GroupId);
                }

                else
                {
                    Group grp = db.Groups.Include("Messages")
                                     .Include("Messages.User")
                                     .Where(grp => grp.Id == id)
                                     .First();
                    return View(grp);
                }
            }
            else
            {
                TempData["message"] = "You are not part of this group!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }
        }


        //ediatare grup existent in baza de date
        //autorizam orice user(rol) sa ajunga aici, insa verificam
        //sa fie admin sau group creator(care poate fi admin sau user)

        //HtppGet implicit
        [Authorize(Roles = "User,Admin")]
        public IActionResult Edit(int id)
        {
            Group group = db.Groups.Where(grp => grp.Id == id)
                                   .First();

            //daca useru curent e group creator sau admin
            if (group.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                return View(group); //ii returnam view cu informatiile grupului
            }

            else
            {
                TempData["message"] = "You don't have permission to modify a group that is not yours!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }
        }

        //adaugam grupul editat in bd 
        //din nou, din cauza ca group creator poate avea orice rol, dam authorize la toate
        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public IActionResult Edit(int id, Group requestGroup)
        {
            Group group = db.Groups.Find(id);

            if (ModelState.IsValid)
            {
                //doar daca e group creator sau admin
                if (group.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
                {
                    group.GroupName = requestGroup.GroupName;
                    group.Description = requestGroup.Description;
                    TempData["message"] = "Group successfully modified!";
                    TempData["messageType"] = "alert-success";
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["message"] = "You don't have permission to modify a group that is not yours!";
                    TempData["messageType"] = "alert-danger";
                    return RedirectToAction("Index");
                }
            }
            else
            {
                return View(requestGroup);
            }
        }


        //stergere grup
        //doar adminul si group creator pot sterge grupuri
        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public IActionResult Delete(int id)
        {
            Group group = db.Groups.Include("Messages") //includem in grup si mesajele pentru ca trebuie sterse automat si ele
                                   .Where(grp => grp.Id == id)
                                   .First();

            //daca userul curent e group creator ul acestuia sau este admin
            if (group.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                db.Groups.Remove(group); //il lasam sa stearga grupul
                db.SaveChanges(); //salvam modificarile
                TempData["message"] = "Group successfully deleted!";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Index");
            }

            else
            {
                TempData["message"] = "You don't have permission to modify a group that is not yours!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }
        }
        //metoda pentru alaturare grup
        [Authorize(Roles = "User,Admin")]
        public IActionResult Join(int id)
        {
            //verificam daca userul curent e deja in grup folosind functia IsUserInGroup definita mai jos
            if (IsUserInGroup(id, _userManager.GetUserId(User)))
            {
                TempData["message"] = "You are already part of this group!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }
            else
            {
                //daca nu e in grup, il adaugam
                var currentUser = _userManager.GetUserId(User);
                var userGroup = new UserGroup //facem o noua inregistrare in tabela UserGroup
                {
                    UserId = currentUser,
                    GroupId = id
                };

                db.UserGroups.Add(userGroup);
                db.SaveChanges();//salvam modificarile

                TempData["message"] = "You have been added to this group!";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Index");
            }

        }

        //query pt baza de date ca sa verificam daca userul curent este in grup
        private bool IsUserInGroup(int groupId, string userId)
        {
            //verificam daca e in grupu respectiv
            return db.UserGroups.Any(ug => ug.GroupId == groupId && ug.UserId == userId);
        }


    }
}