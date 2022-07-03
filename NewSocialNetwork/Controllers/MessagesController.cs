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
using NewSocialNetwork.Data;
using NewSocialNetwork.Models;


namespace NewSocialNetwork.Controllers
{
    public class MessagesController : Controller
    {
        private readonly NewSocialNetworkContext _context;

        public MessagesController(NewSocialNetworkContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Входное действие контроллера. У меня это заглушка.
        /// </summary>
        /// <returns>Перенаправление на страницу входящих сообщений</returns>
        [Authorize]
        public async Task<IActionResult> Index()
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
            Person currentperson = GetCurrentPerson();
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
            Person currentperson = GetCurrentPerson();
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
            var newSocialNetworkContext = _context.Message.Include(m => m.Receiver)
                .Include(m => m.Sender)
                .Select(m => m).Where(m => m.Reported == true);
            return View(await newSocialNetworkContext.ToListAsync());
        }

        /// <summary>
        /// Создание сообщения.
        /// </summary>
        /// <returns>Объект ViewResult для рендеринга представления создания сообщения.
        /// </returns>
        [Authorize]
        public IActionResult Create()
        {
            ViewData["ReceiverId"] = new SelectList(_context.Person, "ID", "Name");
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
            message.Sender = GetCurrentPerson();
            message.Receiver = _context.Person.FirstOrDefault(p => p.ID == message.ReceiverId);
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

            var message =  _context.Message
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
        /// Проверка наличия сообщения.
        /// </summary>
        /// <param name="id">Идентификатор сообщения</param>
        /// <returns>True - если сообщение есть, иначе false.</returns>
        private bool MessageExists(int id)
        {
          return (_context.Message?.Any(e => e.ID == id)).GetValueOrDefault();
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
    }
}
