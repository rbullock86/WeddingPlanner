using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

using WeddingPlanner.Models;

namespace WeddingPlanner.Controllers
{
    public class WeddingPlannerController : Controller
    {
        // DataBase Connections
        private WeddingPlannerContext dbContext;
        public WeddingPlannerController(WeddingPlannerContext context)
        {
            dbContext = context;
        }

        [HttpGet]
        [Route("")]
        public IActionResult Index()
        {
            Console.WriteLine("Hitting Index");
            return View("Index");
        }

        [HttpGet]
        [Route("/Register")]
        public IActionResult Register()
        {
            Console.WriteLine("Hitting Register");
            return View("Register");
        }

        [HttpGet]
        [Route("/Login")]
        public IActionResult Login()
        {
            Console.WriteLine("Hitting Login");
            return View("Login");
        }

        [HttpGet]
        [Route("/Logout")]
        public IActionResult Logout()
        {
            Console.WriteLine("Hitting Logout");
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        [HttpGet]
        [Route("/NonRegisterdLogin/{UId}")]
        public IActionResult NonRegisteredLogin(int UId)
        {
            Console.WriteLine("Hitting NonRegisteredLogin");
            // apply fields from tempdata to hidden inputs on the form
            ViewBag.UId = UId;
            ViewBag.FirstName = TempData["FirstName"];
            ViewBag.LastName = TempData["LastName"];
            ViewBag.Email = TempData["Email"];
            return View("NonRegisteredLogin");
        }

        [HttpGet]
        [Route("/Wedding/{WedId}")]
        public IActionResult WeddingShowOne(int WedId)
        {
            Console.WriteLine($"Hitting Wedding for {WedId}");
            if(!SessionCheck())
            {
                return RedirectToAction("Index");
            }

            // Need to grab guests too!
            Wedding CurrentWedding = dbContext.Weddings.SingleOrDefault(w => w.WeddingId == WedId);
            var GuestsOfWedding = dbContext.Guests.Include(g => g.ThisWedding).ToList();

            ViewBag.WeddingName = CurrentWedding.WeddingName;
            ViewBag.Date = CurrentWedding.Date;
            ViewBag.Location = CurrentWedding.Location;
            ViewBag.WeddingId = CurrentWedding.WeddingId;
            ViewBag.AddGuestError = TempData["AddGuestError"];
            ViewBag.GuestsOfWedding = GuestsOfWedding;


            User CurrentUser = dbContext.Users.SingleOrDefault(u => u.UserId == HttpContext.Session.GetInt32("UId"));
            if(CurrentWedding.WedderOneId != CurrentUser.UserId)
            {
                if(CurrentWedding.WedderTwoId != null)
                {
                    if(CurrentWedding.WedderTwoId != CurrentUser.UserId)
                    {
                        Console.WriteLine("Current User is not part of the wedding.");
                        return RedirectToAction("Index");
                    }
                }
            }
            // Hooray no probs, now show stuff
            return View("WeddingShowOne");
        }

        [HttpGet]
        [Route("/Wedding")]
        public IActionResult WeddingShowAll()
        {
            Console.WriteLine("Hitting Show All Weddings");
            return View("WeddingShowAll");
        }

        [HttpGet]
        [Route("/Wedding/New")]
        public IActionResult WeddingNew()
        {
            Console.WriteLine("Hitting NewWedding");
            if(!SessionCheck())
            {
                return RedirectToAction("Index");
            }
            int _UserId = HttpContext.Session.GetInt32("UId").GetValueOrDefault();
            ViewBag.UserId = HttpContext.Session.GetInt32("UId");
            ViewBag.UserEmail = dbContext.Users.FirstOrDefault(u => u.UserId == _UserId).Email;
            return View("WeddingNew");
        }

        [HttpGet]
        [Route("/Wedding/{WedId}/GuestList")]
        public IActionResult GuestList(int WedId)
        {
            Console.WriteLine($"Showing GuestList for {WedId}");
            return View("GuestList");
        }

        [HttpGet]
        [Route("/UserHome/{UId}")]
        public IActionResult UserHome(int UId)
        {
            // Needs Session Functionality
            Console.WriteLine($"Hitting UserHome for {UId}");
            if(!SessionCheck())
            {
                return RedirectToAction("Index");
            }
            var UserWedding = dbContext.Weddings.FirstOrDefault(w => w.WedderOneId == UId);
            if(UserWedding == null)
            {
                UserWedding = dbContext.Weddings.FirstOrDefault(w => w.WedderTwoId == UId);
            }
            if(UserWedding != null)
            {             
                ViewBag.WeddingName = UserWedding.WeddingName;
                ViewBag.WeddingId = UserWedding.WeddingId;
                Console.WriteLine($"*************User is in {UserWedding.WeddingName}*************");
            }
            else
            {
                ViewBag.WeddingName = null;
            }
            var GuestOfs = dbContext.Guests.Where(g => g.UserId == UId)
                                            .Include(g => g.ThisWedding)
                                            .ToList();
            return View("UserHome", GuestOfs);
        }

        [HttpGet]
        [Route("/UpdateRSVP/{GId}/{status}")]
        public IActionResult UpdateRSVP(int GId, bool status)
        {
            Console.WriteLine($"Hitting UpdateRSVP for {GId}, {status}");
            if(!SessionCheck())
            {
                return RedirectToAction("Index");
            }
            int _UId = HttpContext.Session.GetInt32("UId").GetValueOrDefault();
            var GuestToUpdate = dbContext.Guests.FirstOrDefault(g => g.GuestId == GId);
            GuestToUpdate.ReceivedInvite = true;
            GuestToUpdate.HasRSVP = status;
            GuestToUpdate.UpdatedAt = DateTime.Now;
            dbContext.SaveChanges();
            return RedirectToAction("UserHome", new { UId = _UId});
        }

        [HttpPost]
        [Route("/CreateUser")]
        public IActionResult CreateUser(User NewUser)
        {
            if(ModelState.IsValid)
            {
                if(dbContext.Users.Any(u => u.Email == NewUser.Email))
                {
                    User tempUser = dbContext.Users.FirstOrDefault(u => u.Email == NewUser.Email);
                    if(tempUser.HasRegistered == false)
                    {
                        TempData["FirstName"] = NewUser.FirstName;
                        TempData["LastName"] = NewUser.LastName;
                        TempData["Email"] = NewUser.Email;
                        return RedirectToAction("NonRegisteredLogin", new { UId = tempUser.UserId });
                    }
                    else
                    {
                        Console.WriteLine("Email already taken");
                        ModelState.AddModelError("Email", "Email already taken.");
                        return View("Register");
                    }
                }
                Console.WriteLine("Email Not Used");
                PasswordHasher<User> Hasher = new PasswordHasher<User>();
                NewUser.Password = Hasher.HashPassword(NewUser, NewUser.Password);
                NewUser.HasRegistered = true;
                dbContext.Add(NewUser);
                dbContext.SaveChanges();
                HttpContext.Session.SetInt32("UId", NewUser.UserId);
                return RedirectToAction("UserHome", new { UId = NewUser.UserId});
            }
            return View("Register");
        }

        [HttpPost]
        [Route("/User/Update/{UId}")]
        public IActionResult UpdateUser(int UId, User _UpdateUser)
        {
            Console.WriteLine($"Hitting Update User for {UId}");
            HttpContext.Session.SetInt32("UId", UId);
            User UserToUpdate = dbContext.Users.FirstOrDefault(u => u.UserId == UId);
            UserToUpdate.FirstName = _UpdateUser.FirstName;
            UserToUpdate.LastName = _UpdateUser.LastName;
            UserToUpdate.HasRegistered = true;
            UserToUpdate.UpdatedAt = DateTime.Now;
            dbContext.SaveChanges();
            
            return RedirectToAction("UserHome", new { UId = UId });
        }

        [HttpPost]
        [Route("/ProcessLogin")]
        public IActionResult ProcessLogin(UserLogin LoginAttempt)
        {
            if(ModelState.IsValid)
            {
                var userInDb = dbContext.Users.FirstOrDefault(u => u.Email == LoginAttempt.Email);
                if(userInDb == null)
                {
                    ModelState.AddModelError("Email", "Invalid Email/Password");
                    return View("Login");
                }
                var hasher = new PasswordHasher<UserLogin>();
                var result = hasher.VerifyHashedPassword(LoginAttempt, userInDb.Password, LoginAttempt.Password);
                if(result == 0)
                {
                    ModelState.AddModelError("Email", "Invalid Email/Password");
                    return View("Login");
                }
                HttpContext.Session.SetInt32("UId", userInDb.UserId);
                return RedirectToAction("UserHome", new {Uid = userInDb.UserId});
            }
            else
            {
                return View("Login");
            }
        }
        
        [HttpPost]
        [Route("/Wedding/{WedId}/AddGuest")]
        public IActionResult AddGuest(Guest NewGuest, int WedId)
        {
            Console.WriteLine("Hitting AddGuest");
            int _WedId = WedId;
            if(ModelState.IsValid)
            {
                Console.WriteLine("Guest Model is valid.");
                var PotentialUserInDb = dbContext.Users.FirstOrDefault(u => u.Email == NewGuest.Email);
                if(PotentialUserInDb == null)
                {
                    Console.WriteLine("No Matching User in DataBase. Making an unregistered User.");
                    int _UserId = CreateUnregisteredUser(NewGuest.Email, NewGuest.FirstName, NewGuest.LastName);
                    NewGuest.UserId = _UserId;
                }
                else
                {
                    NewGuest.UserId = PotentialUserInDb.UserId;
                }
                dbContext.Add(NewGuest);
                dbContext.SaveChanges();
            }
            TempData["AddGuestError"] = "Invalid Email Address";
            return RedirectToAction("WeddingShowOne", new {WedId = _WedId});
        }

        [HttpPost]
        [Route("/Wedding/Create")]
        public IActionResult WeddingCreate(Wedding NewWedding)
        {
            Console.WriteLine("Hitting Create Wedding");
            if(!SessionCheck())
            {
                return RedirectToAction("Index");
            }
            int _UId = HttpContext.Session.GetInt32("UId").GetValueOrDefault();
            NewWedding.WedderOneId = _UId;
            NewWedding.WedderOneEmail = dbContext.Users.SingleOrDefault(u => u.UserId == _UId).Email;
            if(NewWedding.WedderTwoEmail != null)
            {
                if(ModelState.IsValid)
                {
                    Console.WriteLine("Valid Wedding!!!!!");
                    NewWedding.WedderTwoId = CreateUnregisteredUser(NewWedding.WedderTwoEmail);
                    dbContext.Add(NewWedding);
                    dbContext.SaveChanges();
                    return RedirectToAction("UserHome", new { UId =  _UId});
                }
            }

            ModelState.AddModelError("WedderTwoEmail", "Thing is still Invalid :(");
            return View("WeddingNew");
        }

        private bool SessionCheck()
        {
            if(HttpContext.Session.GetInt32("UId") == null)
            {
                Console.WriteLine("No user currently in sessionon");
                HttpContext.Session.Clear();
                return false;
            }
            return true;
        }

        public int CreateUnregisteredUser(string NewEmail, string FirstName = "temp", string LastName = "temp")
        {
            Console.WriteLine("Creating an Unregistered User");
            PasswordHasher<User> Hasher = new PasswordHasher<User>();
            User NewUser = new User();
            NewUser.Email = NewEmail;
            NewUser.FirstName = FirstName;
            NewUser.LastName = LastName;
            NewUser.Password = Hasher.HashPassword(NewUser, "12345678");
            NewUser.HasRegistered = false;
            NewUser.CreatedAt = DateTime.Now;
            NewUser.UpdatedAt = DateTime.Now;
            dbContext.Add(NewUser);
            dbContext.SaveChanges();
            return NewUser.UserId;
        }
    }
}