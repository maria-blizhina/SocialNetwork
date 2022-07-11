using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SocialNetworkApp.Data;
using SocialNetworkApp.Models;

namespace SocialNetworkApp.Controllers
{
    public class MessagesController : Controller
    {
        private readonly ApplicationDbContext _context;
        readonly UserManager<IdentityUser> UserManager;

        public MessagesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            UserManager = userManager;
        }

        /// <summary>
        /// Входное действие контроллера. У меня это заглушка.
        /// </summary>
        /// <returns>Перенаправление на страницу входящих сообщений</returns>
        [Authorize]
        public IActionResult Index()
        {
            return Redirect("~/Messages/IncomingMessages");
        }

        /// <summary>
        /// Входящие сообщения.
        /// </summary>
        /// <returns>Объект ViewResult для рендеринга представления списка входящих сообщений.
        /// </returns>
        [Authorize]
        public async Task<IActionResult> IncomingMessages()
        {
            if (_context == null)
                return Problem("Entity set 'ApplicationDbContext' is null.");
            if (_context.Person == null)
                return Problem("Entity set 'ApplicationDbContext.Person' is null.");
            if (_context.Message == null)
                return Problem("Entity set 'ApplicationDbContext.Message' is null.");

            Person? currentperson = GetCurrentPerson();
            if (currentperson == null)
                return NotFound();

            var newSocialNetworkContext = _context.Message.Include(m => m.Receiver)
                .Include(m => m.Sender)
                .Select(m => m).Where(m => m.Receiver == currentperson);
            return View(await newSocialNetworkContext.ToListAsync());
        }

        /// <summary>
        /// Исходящие сообщения.
        /// </summary>
        /// <returns>Объект ViewResult для рендеринга представления списка исходящих сообщений.
        /// </returns>
        [Authorize]
        public async Task<IActionResult> OutgoingMessages()
        {
            if (_context == null)
                return Problem("Entity set 'ApplicationDbContext' is null.");
            if (_context.Person == null)
                return Problem("Entity set 'ApplicationDbContext.Person' is null.");
            if (_context.Message == null)
                return Problem("Entity set 'ApplicationDbContext.Message' is null.");

            Person? currentperson = GetCurrentPerson();
            if (currentperson == null)
                return NotFound();

            var newSocialNetworkContext = _context.Message.Include(m => m.Receiver)
                .Include(m => m.Sender)
                .Select(m => m).Where(m => m.Sender == currentperson);
            return View(await newSocialNetworkContext.ToListAsync());
        }

        /// <summary>
        /// Действие для просмотра сообщений, на которые были жалобы пользователей.
        /// </summary>
        /// <returns>Список сообщений</returns>
        [Authorize(Roles = "moderator")]
        public async Task<IActionResult> ReportedMessages()
        {
            if (_context == null)
                return Problem("Entity set 'ApplicationDbContext' is null.");
            if (_context.Person == null)
                return Problem("Entity set 'ApplicationDbContext.Person' is null.");
            if (_context.Message == null)
                return Problem("Entity set 'ApplicationDbContext.Message' is null.");

            var newSocialNetworkContext = _context.Message.Include(m => m.Receiver)
                .Include(m => m.Sender)
                .Select(m => m).Where(m => m.Reported == true);
            return View(await newSocialNetworkContext.ToListAsync());
        }

        /// <summary>
        /// Создание сообщения.
        /// </summary>
        /// <param name="receiverId">Идентификатор пользователя-получателя</param>
        /// <returns>Объект ViewResult для рендеринга представления создания сообщения.
        /// </returns>
        [Authorize]
        public IActionResult Create(int? id)
        {
            //Если получатель не передан, то отправка сообщения по умолчанию.
            if(id == null)
            {
                ViewData["ReceiverId"] = new SelectList(_context.Person, "ID", "Name");
                return View();
            }

            if (_context == null || _context.Person == null)
                return Problem("Entity set 'ApplicationDbContext.Person' is null.");

            //Если есть получатель, то сразу подставить его.
            var selectList = new SelectList(_context.Person, "ID", "Name");
            var selected = selectList.Where(x => x.Value == id.ToString()).FirstOrDefault();
            if (selected != null)
                selected.Selected = true;

            ViewData["ReceiverId"] = selectList;            
            
            return View();
        }

        /// <summary>
        /// Отправка созданного сообщения.
        /// </summary>
        /// <returns>В случае успеха, возвращаемся к списку входящих сообщений.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create([Bind("ID,Text,ReceiverId")] Message message)
        {
            if (_context == null)
                return Problem("Entity set 'ApplicationDbContext' is null.");
            if (_context.Person == null)
                return Problem("Entity set 'ApplicationDbContext.Person' is null.");

            var currentPerson = GetCurrentPerson();
            //Если нет странички, то прежде, чем отправлять сообщения, ее надо создать.
            if (currentPerson == null)
                return Redirect("~/People/Create");

            message.Sender = currentPerson;
            message.Receiver = _context.Person.FirstOrDefault(p => p.ID == message.ReceiverId);
            if (message.Receiver == null)
                return NotFound();

            if (ModelState.IsValid)
            {
                _context.Add(message);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(message);
        }

        /// <summary>
        /// Редактирование сообщения.
        /// </summary>
        /// <param name="id">Идентификатор сообщения</param>
        /// <returns>Объект ViewResult для рендеринга представления формы редактирования сообщения</returns>
        [Authorize(Roles = "moderator")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Message == null)
            {
                return NotFound();
            }

            var message = await _context.Message.FindAsync(id);
            if (message == null)
            {
                return NotFound();
            }
            ViewData["ReceiverId"] = new SelectList(_context.Person, "ID", "ID", message.ReceiverId);
            ViewData["SenderId"] = new SelectList(_context.Person, "ID", "ID", message.SenderId);
            return View(message);
        }

        /// <summary>
        /// Редактирование сообщения.
        /// </summary>
        /// <param name="message">Отредактированное сообщение</param>
        /// <returns>В случае успеха возврат к списку сообщений</returns>
        [Authorize(Roles = "moderator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Text,SenderId,ReceiverId,Created")] Message message)
        {
            if (id != message.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(message);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MessageExists(message.ID))
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
            return View(message);
        }

        /// <summary>
        /// Удаление сообщения.
        /// </summary>
        /// <param name="id">Идентификатор сообщения</param>
        /// <returns>Форма для удаления</returns>
        [Authorize(Roles = "moderator")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Message == null)
            {
                return NotFound();
            }

            var message = await _context.Message
                .Include(m => m.Receiver)
                .Include(m => m.Sender)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (message == null)
            {
                return NotFound();
            }

            return View(message);
        }

        /// <summary>
        /// Удаление сообщения.
        /// </summary>
        /// <param name="id">Идентификатор сообщения</param>
        /// <returns>В случае успеха возврат к списку сообщений.</returns>
        [Authorize(Roles = "moderator")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Message == null)
            {
                return Problem("Entity set 'NewSocialNetworkContext.Message'  is null.");
            }
            var message = await _context.Message.FindAsync(id);
            if (message != null)
            {
                _context.Message.Remove(message);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Пожаловаться на сообщение.
        /// </summary>
        /// <param name="id">Идентификатор сообщения</param>
        /// <returns>Возврат к входящим сообщениям.</returns>
        [Authorize]
        public IActionResult Report(int? id)
        {
            if (id == null || _context.Message == null)
            {
                return NotFound();
            }

            var message = _context.Message
                .Include(m => m.Receiver)
                .Include(m => m.Sender)
                .FirstOrDefault(m => m.ID == id);
            if (message == null)
            {
                return NotFound();
            }

            message.Reported = true;
            _context.SaveChanges();

            return Redirect("~/Messages/IncomingMessages");
        }


        /// <summary>
        /// Метод проверки существования пользователя.
        /// </summary>
        /// <param name="id">Идентификатор пользователя</param>
        /// <returns>True, если пользователь есть, иначе false</returns>
        private bool MessageExists(int id)
        {
          return (_context.Message?.Any(e => e.ID == id)).GetValueOrDefault();
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
