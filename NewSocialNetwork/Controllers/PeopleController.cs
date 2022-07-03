using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NewSocialNetwork.Data;
using NewSocialNetwork.Models;
using NewSocialNetwork.Models.ViewModels;

namespace NewSocialNetwork.Controllers
{
    public class PeopleController : Controller
    {
        private readonly NewSocialNetworkContext _context;
       
        public PeopleController(NewSocialNetworkContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Входное действие контроллера. У меня это заглушка.
        /// </summary>
        /// <returns>Перенаправление на страницу текущего пользователя</returns>
        public IActionResult Index()
        {
            return Redirect("~/People/MyPage");
        }

        /// <summary>
        /// Моя страница или страница текущего пользователя.
        /// </summary>
        /// <returns>Объект ViewResult для рендеринга представления, в который передается объект класса Person.
        /// Если это первый вход и страницы еще нет, то сразу переход к созданию.
        /// </returns>
        public IActionResult MyPage()
        {

            var person = GetCurrentPerson();

            if (person == null && User.Identity.IsAuthenticated)
                return Redirect("~/People/Create");
            return View(person);
        }


        /// <summary>
        /// Список страниц пользователей. Только для авторизованных пользователей.
        /// </summary>
        /// <param name="search">Строка, по которой осуществляется поиск в списке</param>
        /// <returns>Объект ViewResult для рендеринга представления списка</returns>
        [Authorize] 
        public async Task<IActionResult> PeopleList(string? search = null)
        {
            if (_context.Person == null)
                return Problem("Entity set 'NewSocialNetworkContext.Person'  is null.");
             
            IQueryable<Person> people = _context.Person;
            if (!string.IsNullOrEmpty(search))
            {
                people = people.Where(p => p.Name!.Contains(search) || p.About!.Contains(search));
            }

            return View(new PeopleListViewModel { Search = search, People = people });
        }


        /// <summary>
        /// Список друзей пользователя. Только для авторизованных пользователей.
        /// </summary>
        /// <returns>Объект ViewResult для рендеринга представления списка друзей</returns>
        [Authorize]
        public async Task<IActionResult> FriendsList()
        {
            if (_context.Person == null || _context.Follower == null)
                Problem("Entity set is null.");
            Person current = GetCurrentPerson();
            var myFollowersRecords = _context.Follower
                .Select(f => f)
                .Where(f => f.PersonId == current.ID)
                .Select(fol => fol.ID);
            var friendsRecords = await _context.Follower
                .Select(f => f)
                .Where(f => f.FollowerId == current.ID && myFollowersRecords.Any(fr => fr == f.PersonId))
                .Select(r => r.PersonId).ToListAsync();
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
        /// Страница пользователя.
        /// </summary>
        /// <param name="id">Идентификатор страницы пользователя</param>
        /// <returns>Объект ViewResult для рендеринга представления страницы пользователя</returns>
        [Authorize]
        public async Task<IActionResult> PersonPage(int? id)
        {
            //Если не передан id, возвращаемся на список пользователей.
            if (id == null)
               return Redirect("~/People/PeopleList");
                
            var person = await _context.Person
                .FirstOrDefaultAsync(p => p.ID == id);

            if (person == null)
            {
                return Redirect("~/People/PersonNotFound");
            }

            ViewBag.Follower = GetExistedFollowing(id);

            return View(person);
        }

        /// <summary>
        /// Действие создание страницы пользователя.
        /// </summary>
        /// <returns>Объект ViewResult для рендеринга представления создания страницы пользователя</returns>
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Post-запрос действия создании страницы пользователя.
        /// </summary>
        /// <param name="person">Созданный пользователь</param>
        /// <returns>Объект ViewResult для рендеринга представления создания страницы пользователя</returns>
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,Name,About,Email")] Person person)
        {
            person.Email = User.Identity?.Name;
            var errors = ModelState.Values.SelectMany(v => v.Errors);
            if (ModelState.IsValid)
            {
                _context.Person.Add(person);
                var c = _context.Person;
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(person);
        }

        /// <summary>
        /// Действия редактирования страницы пользователя.
        /// </summary>
        /// <param name="id">Идентификатор пользователя для редактирования</param>
        /// <returns>Объект ViewResult для рендеринга представления страницы редактирования</returns>
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Person == null)
            {
                return NotFound();
            }

            var person = await _context.Person.FindAsync(id);
            if (person == null)
            {
                return NotFound();
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
        public async Task<IActionResult> Edit(int id, [Bind("ID,Name,About,Email")] Person person)
        {
            // Так как я связала Пользователя и его страницу по эмейлу, мне приходится костылить его и при редактировании
            person.Email = User.Identity?.Name;
            if (id != person.ID)
            {
                return NotFound();
            }

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
        /// Действие подписки на пользователя.
        /// </summary>
        /// <param name="id">Идентификатор пользователя</param>
        /// <returns>Возвращает на страницу пользователя</returns>
        [Authorize]
        public IActionResult Follow(int id)
        {
            var following = _context.Person
                .FirstOrDefault(p => p.ID == id);
            var me = GetCurrentPerson();
            Follower follower = new Follower ( 0, following.ID, me.ID);
            if (ModelState.IsValid)
            {
                _context.Follower.Add(follower);
                _context.SaveChanges();
            }
            return Redirect("~/People/PersonPage/" + id);
        }

        /// <summary>
        /// Действие отписки от пользователя.
        /// </summary>
        /// <param name="id">Идентификатор пользователя</param>
        /// <returns>Возвращает на страницу пользователя</returns>
        [Authorize]
        public IActionResult UnFollow(int? id)
        {
            Person currentPerson = GetCurrentPerson();
            Person noMoreFollowing = _context.Person.FirstOrDefault(p => p.ID == id);
            Follower noMoreFollowingRecord = _context.Follower.FirstOrDefault(p => 
                p.PersonId == noMoreFollowing.ID && p.FollowerId == currentPerson.ID);
            if (noMoreFollowingRecord != null)
            {
                _context.Follower.Remove(noMoreFollowingRecord);
                _context.SaveChanges();
            }
            return Redirect("~/People/PersonPage/" + id);
        }



        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Person == null)
            {
                return NotFound();
            }

            var person = await _context.Person
                .FirstOrDefaultAsync(m => m.ID == id);
            if (person == null)
            {
                return NotFound();
            }

            return View(person);
        }


        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Person == null)
            {
                return Problem("Entity set 'NewSocialNetworkContext.Person'  is null.");
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
        /// Метод проверки существования пользователя.
        /// </summary>
        /// <param name="id">Идентификатор пользователя</param>
        /// <returns>True, если пользователь есть, иначе false</returns>
        private bool PersonExists(int id)
        {
          return (_context.Person?.Any(e => e.ID == id)).GetValueOrDefault();
        }

        /// <summary>
        /// Метод для получения текущего пользователя.
        /// </summary>
        /// <returns>Текущего пользователя</returns>
        private Person GetCurrentPerson()
        {
            var name = User.Identity?.Name;
            var person = new Person();
            //Так как не получилось разобраться с userManager, я пользуюсь эмейлом учетной записи как уникальным полем.
            //По эмейлу привязываю страницу пользователя, по нему же и нахожу текущего пользователя.
            if (name != null)
                person = _context.Person.ToList().FirstOrDefault(p => Equals(name, p.Email)); 
            return person;
        }

        /// <summary>
        /// Метод для получения записи подписчика.
        /// </summary>
        /// <param name="personId">Идентификатор пользователя, на которого подписан (или нет) текущий</param>
        /// <returns>True, если пользователь есть, иначе false</returns>
        private Follower GetExistedFollowing(int? personId)
        {
            Person currentPerson = GetCurrentPerson();
            Follower follower = _context.Follower.FirstOrDefault(f => f.PersonId == personId && f.FollowerId == currentPerson.ID);
            return follower;
        }

    }
}
