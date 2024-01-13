using Micro_Social_Platform.Data;
using Micro_Social_Platform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Runtime.Intrinsics.Arm;
using System.Text.RegularExpressions;

namespace Micro_Social_Platform.Controllers
{
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext db;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager
            )
        {
            db = context;

            _userManager = userManager;

            _roleManager = roleManager;
        }

        [Authorize(Roles = "User,Admin")]
        //orice utilizator inregistrat poate vedea lista cu utilizatori
        public IActionResult Index()
        {
            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }
            int _perPage = 3; //afisam cate 3 pe fiecare pagina
            var users = db.Users.ToList();
            ViewBag.Users = users;
            //facem search pentru lista utilizatorilor
            var search = "";
            if (Convert.ToString(HttpContext.Request.Query["search"]) != null)
            {

                //eliminam spatiile libere
                search =
                Convert.ToString(HttpContext.Request.Query["search"]).Trim();
                List<string> userIds = db.Users.Where(u => u.FirstName.Contains(search) || u.LastName.Contains(search)).Select(u => u.Id).ToList();
                users = db.Users.Where(u => userIds.Contains(u.Id)).ToList();
            }
            ViewBag.SearchString = search;

            int totalItems = users.Count();
            var currentPage = Convert.ToInt32(HttpContext.Request.Query["page"]);
            var offset = 0;
            if (!currentPage.Equals(0))
            {
                offset = (currentPage - 1) * _perPage;
            }
            var paginatedUsers = users.Skip(offset).Take(_perPage);
            ViewBag.lastPage = Math.Ceiling((float)totalItems /
(float)_perPage);

            ViewBag.Users = paginatedUsers;
            if (search != "")
            {
                ViewBag.PaginationBaseUrl = "/Users/Index/?search="
                + search + "&page";
            }
            else
            {
                ViewBag.PaginationBaseUrl = "/Users/Index/?page";
            }
            return View();
        }
        [Authorize(Roles = "User,Admin")]
        public async Task<ActionResult> Show(string id) //id-ul utilizatorului la care ne uitam
        {
            ApplicationUser user = db.Users.Find(id); //user la care ne uitam

            bool isFollowing = db.FollowRequests //vedem daca il urmarim sau nu
               .Any(fr => fr.SenderId == _userManager.GetUserId(User) && fr.ReceiverId == id && fr.IsAccepted);
            if (id == _userManager.GetUserId(User)) //daca ne uitam pe profilul nostru
            {

                ViewBag.IsCurrentUser = true;

                ViewBag.Posts = db.Posts.Where(p => p.UserId == id).ToList(); //includem toate postarile userului (ale noastre in cazul asta)

                ViewBag.ShowEditButton = true;
            }
            if (User.IsInRole("Admin")) //daca utilizatorul este admin
            {

                ViewBag.IsAdmin = true; //trimitem asta in view pentru permisiuni
                ViewBag.Posts = db.Posts.Where(p => p.UserId == id).ToList(); //includem toate postarile userului

            }


            else if (!user.IsPrivate || isFollowing) //daca userul are cont public sau il urmarim
            {
                //includem toate postarile 
                ViewBag.Posts = db.Posts
                .Include(p => p.User)
                    .Where(p => p.UserId == id)
                    .Where(p => !p.User.IsPrivate || (isFollowing && p.User.Id == id))
                    .OrderByDescending(p => p.Date)
                    .ToList();
                ViewBag.CanView = true; //trimitem in view ca are acces la postarile lui
            }

            return View(user);
        }

        [Authorize(Roles = "User,Admin")]
        //edit pentru utilizatori
        public async Task<ActionResult> Edit(string id)
        {
            ApplicationUser user = await _userManager.FindByIdAsync(id); //luam id ul utilizatorului pe care vrem sa il editam

            if (user == null)
            {
                return RedirectToAction("Index", "Posts");
            }

            //verificam daca userul curent este detinatorul profilului sau este admin
            var currentUser = await _userManager.GetUserAsync(User);
            if (User.IsInRole("Admin") || user.Id == currentUser.Id)
            {
                ViewBag.IsAdmin = User.IsInRole("Admin");
                return View(user);
            }
            else
            {
                //daca nu este detinatorul profilului sau admin, redirectam catre pagina de index pentru ca nu are permisiuni
                return RedirectToAction("Index", "Posts");
            }
        }

        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public async Task<ActionResult> Edit(ApplicationUser user)
        {

            var currentUser = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Admin") && user.Id != currentUser.Id) //daca nu e admin sau nu e chiar profilul utilizatorului curent
            {
                return RedirectToAction("Index", "Posts"); //redirectionam la index postari pentru ca nu are permisiuni
            }

            if (ModelState.IsValid)
            {
                var usercurent = await _userManager.FindByIdAsync(user.Id); //gasim userul

                if (usercurent == null)
                {
                    TempData["message"] = "This user doesn't exist!";
                    TempData["messageType"] = "alert-danger";
                    return RedirectToAction("Index", "Posts");
                }

                //actualizam toate informatiile acestuia
                usercurent.FirstName = user.FirstName;
                usercurent.LastName = user.LastName;
                usercurent.Pronouns = user.Pronouns;
                usercurent.Bio = user.Bio;
                usercurent.IsPrivate = user.IsPrivate;



                await _userManager.UpdateAsync(usercurent);

                TempData["message"] = "Profile succesfully updated!";
                TempData["messageType"] = "success";
                db.SaveChanges();


                return RedirectToAction("Show", new { id = user.Id });
            }

            return View(user);
        }

        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        //oricine are dreptul la aceasta metoda, verificam mai jos mai multe
        public async Task<ActionResult> Delete(string id)
        {
            //includem toate datele utilizatorului
            var user = db.Users
                .Include(u => u.Posts)
                .Include(u => u.Comments)
                .Include(u => u.Messages)
                .Include(u => u.UserGroups)
                .Include(u => u.SentFollowRequests)
                .Include(u => u.ReceivedFollowRequests)
                .Where(u => u.Id == id)
                .First();


            //sterge comentariile facute de el
            if (user.Comments.Count > 0)
            {
                foreach (var comment in user.Comments)
                {
                    db.Comments.Remove(comment);
                }
            }
            //stergem si postarile lui
            if (user.Posts.Count > 0)
            {
                foreach (var post in user.Posts)
                {
                    db.Posts.Remove(post);
                }
            }
            //stergem mesajele
            if (user.Messages.Count > 0)
            {
                foreach (var message in user.Messages)
                {
                    db.Messages.Remove(message);
                }
            }
            //stergem grupurile
            if (user.UserGroups.Count > 0)
            {
                foreach (var grup in user.UserGroups)
                {
                    db.UserGroups.Remove(grup);
                }
            }
            //sterge cererile de urmarire
            if (user.SentFollowRequests.Count > 0)
            {
                foreach (var request in user.SentFollowRequests)
                {
                    db.FollowRequests.Remove(request);
                }
            }
            if (user.ReceivedFollowRequests.Count > 0)
            {
                foreach (var request in user.ReceivedFollowRequests)
                {
                    db.FollowRequests.Remove(request);
                }
            }


            //apoi stergem userul
            db.ApplicationUsers.Remove(user);

            db.SaveChanges();

            return RedirectToAction("Index", "Users");
        }



        [Authorize(Roles = "User,Admin")]
        //metoda de trimitere cerere de urmarire
        public async Task<IActionResult> SendFollowRequest(string receiverId)
        {
            var sender = _userManager.GetUserId(User);
            //verificam daca deja exista o cerere de urmarire neacceptata
            var existingRequest = await db.FollowRequests
                .FirstOrDefaultAsync(r => r.SenderId == sender && r.ReceiverId == receiverId && !r.IsAccepted);

            if (existingRequest != null)
            {
                //cererea de urmarire deja exista
                TempData["message"] = "Follow request already sent!";
                TempData["messageType"] = "alert-warning";
                return RedirectToAction("Index");
            }

            //altfel o adaugam in tabela
            var request = new FollowRequest
            {
                SenderId = sender,
                ReceiverId = receiverId,
                IsAccepted = false //punem variabila false pentru ca doar am trimis cererea, ramane sa fie acceptata ulterior

            };

            ViewBag.CerereTrimisa = false; //pentru verificari in view
            db.FollowRequests.Add(request);
            db.SaveChanges();

            TempData["message"] = "Follow request successfully sent!";
            TempData["messageType"] = "alert-success";

            return RedirectToAction("Index");
        }
        [Authorize(Roles = "User,Admin")]
        //metoda pentru afisarea listei de cereri de urmarire
        public async Task<IActionResult> FollowRequests()
        {
            var currentUser = await _userManager.GetUserAsync(User); //luam userul curent

            var followRequests = await db.FollowRequests //luam toate cererile de urmarire care inca nu au fost acceptate
                .Include(r => r.Sender)
                .Where(r => r.ReceiverId == currentUser.Id && !r.IsAccepted)
                .ToListAsync();

            return View(followRequests);
        }
        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        //emtoda pentru acceptarea cererilor de urmarire
        public IActionResult AcceptFollowRequest(int requestId)
        {
            var currentUser = _userManager.GetUserId(User); //luam userul curent

            //luam toate cererile neacceptate
            var followRequest = db.FollowRequests
                .Where(r => r.Id == requestId && r.ReceiverId == currentUser && !r.IsAccepted)
                .FirstOrDefault();

            if (followRequest == null)
            {
                return NotFound();
            }

            //variabila devine true deci cererea a fost acceptata
            followRequest.IsAccepted = true;

            db.SaveChanges();

            TempData["message"] = "Follow request accepted!";
            TempData["messageType"] = "alert-success";

            return RedirectToAction("FollowRequests");
        }


        private async Task<bool> IsFollowRequestSent(string senderId, string receiverId)
        {
            //verifica daca exista o cerere de urmarire neacceptata intre senderId si receiverId
            var existingRequest = await db.FollowRequests
                .Where(r => r.SenderId == senderId && r.ReceiverId == receiverId && !r.IsAccepted)
                .FirstOrDefaultAsync();

            return existingRequest != null;
        }


    }
}