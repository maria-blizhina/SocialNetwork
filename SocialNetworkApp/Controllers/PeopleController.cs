using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SocialNetworkApp.Data;
using SocialNetworkApp.Models;
using SocialNetworkApp.Models.ViewModels;

namespace SocialNetworkApp.Controllers
{
    public class PeopleController : Controller
    {
        private readonly ApplicationDbContext _context;
        readonly UserManager<IdentityUser> UserManager;
        readonly RoleManager<IdentityRole> RoleManager;

        public PeopleController(ApplicationDbContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            UserManager = userManager;
            RoleManager = roleManager;
        }

        /// <summary>
        /// Список страниц пользователей. Только для авторизованных пользователей.
        /// </summary>
        /// <param name="search">Строка, по которой осуществляется поиск в списке</param>
        /// <returns>Объект ViewResult для рендеринга представления списка</returns>
        [Authorize]
        public IActionResult Index(string? search = null)
        {
            if (_context == null || _context.Person == null)
                return Problem("Entity set 'ApplicationDbContext.Person' is null.");

            IQueryable<Person> people = _context.Person;
            if (!string.IsNullOrEmpty(search))
            {
                people = people.Where(p => p.Name!.Contains(search) || p.About!.Contains(search));
            }

            return View(new PeopleListViewModel { Search = search, People = people });
        }

        /// <summary>
        /// Действие создание страницы пользователя.
        /// </summary>
        /// <returns>Объект ViewResult для рендеринга представления создания страницы пользователя</returns>
        [Authorize]
        public IActionResult Create()
        {
            var currentUserId = UserManager.GetUserId(User);
            var currentUser = UserManager.Users.Where(u => u.Id == currentUserId).FirstOrDefault();
            if (currentUser == null)
                return NotFound();
            var currentPerson = _context.Person.Where(p => p.UserId == currentUserId).FirstOrDefault();

            //Если страничка уже создана, перенаправить на нее.
            if (currentPerson != null)
                return RedirectToAction(nameof(MyPage));
            var person = new Person(currentUserId, currentUser.UserName, "", currentUser.Email, currentUser.PhoneNumber);
            return View(person);
        }

        /// <summary>
        /// Post-запрос действия создании страницы пользователя.
        /// </summary>
        /// <param name="person">Созданный пользователь</param>
        /// <returns>Объект ViewResult для рендеринга представления создания страницы пользователя</returns>
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,UserId,Name,About,Email,Phone")] Person person)
        {           
            if (ModelState.IsValid && _context != null && _context.Person != null)
            {
                _context.Person.Add(person);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(person);
        }

        /// <summary>
        /// Моя страница или страница текущего пользователя.
        /// </summary>
        /// <returns>Объект ViewResult для рендеринга представления, в который передается объект класса Person.
        /// Если это первый вход и страницы еще нет, то сразу переход к созданию.
        /// </returns>
        public IActionResult MyPage()
        {
            var currentPerson = GetCurrentPerson();

            //Если странички нет, то перенаправить и
            //создать ее
            if (currentPerson == null && User.Identity != null && User.Identity.IsAuthenticated)
             return Redirect("~/People/Create");
            return View(currentPerson);
        }

        /// <summary>
        /// Страница пользователя.
        /// </summary>
        /// <param name="id">Идентификатор страницы пользователя</param>
        /// <returns>Объект ViewResult для рендеринга представления страницы пользователя</returns>
        [Authorize]
        public async Task<IActionResult> PersonPage(int? id)
        {
            if (id == null)
                return Redirect("~/People/");
            if (_context == null || _context.Person == null)
                return Problem("Entity set 'ApplicationDbContext.Person' is null.");

            var person = await _context.Person
                .FirstOrDefaultAsync(p => p.ID == id);

            if (person == null)
                return NotFound();

            //Определить, подписан ли текущий пользователь на страничку
            var existingFollowerRerord = GetExistedFollowing(id);
            var currentPerson = GetCurrentPerson();

            //Если текущий пользователь зашел к себе же на страничку,
            //то у него не должно быть видимой возможности подписаться или отписаться.         
            if (currentPerson != null && currentPerson.ID == id)
                return View(person);

            //Передаем информацию о том подписан или нет, для того, чтобы дать возможность подписаться или отписаться.
            ViewBag.AlreadyFollowed = existingFollowerRerord != null ? true : false;

            return View(person);

        }

        /// <summary>
        /// Действия редактирования страницы пользователя.
        /// </summary>
        /// <returns>Объект ViewResult для рендеринга представления страницы редактирования</returns>
        [Authorize]
        public IActionResult Edit()
        {
            if (_context == null || _context.Person == null)
                return Problem("Entity set 'ApplicationDbContext.Person' is null.");

            //Редактировать можно только свою страницу.
            var person = GetCurrentPerson();
            if (person == null)
            {
                //Если странички нет, ее надо создать.
                return Redirect("~/People/Create");
            }
            return View(person);
        }

        /// <summary>
        /// Post запрос редактирования страницы пользователя. 
        /// </summary>
        /// <param name="person">Отредактированный пользователь</param>
        /// <returns>Объект ViewResult для рендеринга представления</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, [Bind("ID,UserId,Name,About,Email,Phone")] Person person)
        {
            if (id != person.ID)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(person);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PersonExists(person.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(person);
        }

        /// <summary>
        /// Действия удаления страницы пользователя.
        /// </summary>
        /// <param name="id">Идентификатор пользователя для удаления</param>
        /// <returns>Объект ViewResult для рендеринга представления удаления пользователя</returns>
        [Authorize(Roles = "moderator")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Person == null)
                return NotFound();

            var person = await _context.Person
                .FirstOrDefaultAsync(m => m.ID == id);
            if (person == null)
                return NotFound();

            return View(person);
        }

        /// <summary>
        /// Post запрос удаления страницы пользователя. 
        /// </summary>
        /// <param name="id">Идентификатор удаляемого пользователя</param>
        /// <returns>Перенаправление на действие Index (Список пользователей)</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "moderator")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Person == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Person'  is null.");
            }
            var person = await _context.Person.FindAsync(id);
            if (person != null)
            {
                _context.Person.Remove(person);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        
        /// <summary>
        /// Действие подписки на пользователя.
        /// </summary>
        /// <param name="id">Идентификатор пользователя</param>
        /// <returns>Возвращает на страницу пользователя</returns>
        [Authorize]
        public IActionResult Follow(int id)
        {
            if (_context == null || _context.Person == null || _context.FollowerRecord == null)
                return Problem("Entity set 'ApplicationDbContext' is null.");

            //Страничка, на которую пользователь хочет подписаться.
            var following = _context.Person
                .FirstOrDefault(p => p.ID == id);
            var currentPerson = GetCurrentPerson();
            if (currentPerson == null || following == null || following == currentPerson)
                return NotFound();
            
            FollowerRecord followerRecord = new FollowerRecord(following, following.ID, currentPerson, currentPerson.ID);
            //Найти запись о том, что текущий пользователь уже подписан на страничку,
            //если уже подписан, то повторно этого делать не стоит.
            if (_context.FollowerRecord.Any(fl => fl.FollowedPersonId == followerRecord.FollowedPersonId && 
                                                    fl.FollowerPersonId == followerRecord.FollowerPersonId))
                //Попытка вернуться на страницу, с которой была произведена подписка.
                try
                {
                    return Redirect(Request.Headers["Referer"]);
                }
                catch (Exception)
                {
                    return Redirect("~/People/Index");
                }

            if (ModelState.IsValid)
            {
                _context.FollowerRecord.Add(followerRecord);
                _context.SaveChanges();
            }
            
            try
            {
                return Redirect(Request.Headers["Referer"]);
            }     
            catch (Exception)
            {
                return Redirect("~/People/Index");
            }              

        }

        /// <summary>
        /// Действие отписки от пользователя.
        /// </summary>
        /// <param name="id">Идентификатор пользователя</param>
        /// <returns>Возвращает на страницу пользователя</returns>
        [Authorize]
        public IActionResult UnFollow(int? id)
        {
            if (_context == null || _context.Person == null || _context.FollowerRecord == null)
                return Problem("Entity set 'ApplicationDbContext' is null.");

            var currentPerson = GetCurrentPerson();

            //Страничка, от которой пользователь хочет отписаться.
            var noMoreFollowing = _context.Person.FirstOrDefault(p => p.ID == id);
            if (currentPerson == null || noMoreFollowing == null)
                return NotFound();

            var noMoreFollowingRecord = _context.FollowerRecord.FirstOrDefault(p =>
                p.FollowedPersonId == noMoreFollowing.ID && p.FollowerPersonId == currentPerson.ID);
            if (noMoreFollowingRecord == null)
                return NotFound();

            if (noMoreFollowingRecord != null)
            {
                _context.FollowerRecord.Remove(noMoreFollowingRecord);
                _context.SaveChanges();
            }
            return Redirect(Request.Headers["Referer"]);
        }

            /// <summary>
            /// Список друзей пользователя. Только для авторизованных пользователей.
            /// </summary>
            /// <returns>Объект ViewResult для рендеринга представления списка друзей</returns>
            [Authorize]
        public async Task<IActionResult> FriendsList()
        {
            if (_context == null || _context.Person == null || _context.FollowerRecord == null)
                Problem("Entity set is null.");

            Person? current = GetCurrentPerson();
            if (current == null)
                return NotFound();
            
            //Те, кто подписан на меня.
            var myFollowersRecords = _context.FollowerRecord
                .Select(f => f)
                .Where(f => f.FollowedPersonId == current.ID)
                .Select(fol => fol.FollowerPersonId);

            //На кого я подписан, отфильтровать с учетом предыдущего списка.
            var friendsRecords = await _context.FollowerRecord
                .Select(f => f)
                .Where(f => f.FollowerPersonId == current.ID && myFollowersRecords.Any(fr => fr == f.FollowedPersonId))
                .Select(r => r.FollowedPersonId).ToListAsync();

            //Сформировать список друзей.
            List<Person> friends = new List<Person>();
            foreach (int friendrecord in friendsRecords)
            {
                var friend = _context.Person.FirstOrDefault(f => f.ID == friendrecord);
                if (friend != null)
                    friends.Add(friend);
            }
            return View(friends);
        }

        /// <summary>
        /// Метод проверки существования пользователя.
        /// </summary>
        /// <param name="id">Идентификатор пользователя</param>
        /// <returns>True, если пользователь есть, иначе false</returns>
        private bool PersonExists(int id)
        {
          return (_context.Person?.Any(e => e.ID == id)).GetValueOrDefault();
        }

        /// <summary>
        /// Метод для получения записи подписчика.
        /// </summary>
        /// <param name="personId">Идентификатор пользователя, на которого подписан (или нет) текущий</param>
        /// <returns>Запись FollowerRecord о подписке, либо null, если таковой нет</returns>
        private FollowerRecord? GetExistedFollowing(int? personId)
        {
            var currentPerson = GetCurrentPerson();
            if (personId == null || _context.FollowerRecord == null || currentPerson == null)
                return null;
            var follower = _context.FollowerRecord
                .FirstOrDefault(f => f.FollowedPersonId == personId && f.FollowerPersonId == currentPerson.ID);
            return follower;
        }

        /// <summary>
        /// Метод для получения текущего пользователя.
        /// </summary>
        /// <returns>Текущего пользователя</returns>
        protected Person? GetCurrentPerson()
        {
            if (_context == null || _context.Person == null)
                return null;
            var currentPerson = _context.Person
                .Where(p => p.UserId == UserManager.GetUserId(User))
                .FirstOrDefault();
            return currentPerson;
        }
    }
}
