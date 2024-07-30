using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Imgriff.Data;
using System;
using System.Collections.Generic;
using Imgriff.Data.Entity;
using Microsoft.AspNetCore.Authorization;
using Imgriff.Services.Random;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity.UI.Services;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.ComponentModel.DataAnnotations;
using Imgriff.Data.DTO.List;
using Imgriff.Data.DTO.Gallery;
using Imgriff.Data.DTO.Page;
using Newtonsoft.Json.Linq;
using Imgriff.Data.DTO.Table;
using Imgriff.Services.Hash;
using Imgriff.Services.Kdf;
using Microsoft.AspNetCore.StaticFiles;
using Imgriff;
using Imgriff.Data.DTO.Board;
using System.Reflection.Metadata;
using WebApplication2.Migrations;
using System.Net.Mail;
using System.Net.Mime;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Imgriff.Data.DTO.Other;
using Imgriff.Data.DTO.User;

namespace Imgriff.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly IHashService _hashService;
        private readonly IKdfService _kdfService;
        private readonly DataContext dataContext;
        private readonly ILogger<ValuesController> _logger;
        private readonly string _userFolderPath = AppConfig.UserFolderPath; // Путь к корневой директории пользователей на сервере
        private readonly string _key = AppConfig.Key; // Путь к корневой директории пользователей на сервере
        private readonly IRandomService _randomService;
        private readonly IEmailSender _emailSender; // Сервис отправки электронной почты


        public ValuesController(DataContext dataContext, ILogger<ValuesController> logger, IRandomService randomService, IEmailSender emailSender, IKdfService kdfService, IHashService hashService)
        {
            this.dataContext = dataContext;
            _logger = logger;
            _randomService = randomService;
            _emailSender = emailSender;
            _kdfService = kdfService;
            _hashService = hashService;
        }

        #region Authorization

        [HttpPost("register")]
        [AllowAnonymous]
        public IActionResult Register([FromBody] UserDTO user)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var existingUser = dataContext.Users.FirstOrDefault(u => u.Email == user.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "User with this email already exists");
                    return BadRequest(ModelState);
                }

                var userId = Guid.NewGuid().ToString();

                var userFolderPath = Path.Combine(_userFolderPath, userId);
                Directory.CreateDirectory(userFolderPath);

                var newUser = new User { Id = userId, Email = user.Email, EmailCode = _randomService.ConfirmCode(24) };

                dataContext.Users.Add(newUser);
                dataContext.SaveChanges();

                SendConfirmationCodeByEmail(newUser.Email, newUser.EmailCode);

                return Ok(newUser);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }


        [HttpPost("login")]
        [AllowAnonymous]
        public IActionResult Login([FromBody] UserDTO user)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                ModelState.AddModelError("Email", "Email is required");
                return BadRequest(ModelState);
            }

            var existingUser = dataContext.Users.FirstOrDefault(u => u.Email == user.Email);

            string ConfirmationCode = _randomService.ConfirmCode(8);
            if (existingUser == null)
            {
                var userId = Guid.NewGuid().ToString();

                var userFolderPath = Path.Combine(_userFolderPath, userId);
                Directory.CreateDirectory(userFolderPath);
                var salt = _randomService.RandomString(16);

                existingUser = new User {
                    Id = userId,
                    Email = user.Email,
                    Salt = salt,
                    EmailCode = _kdfService.GetDerivedKey(ConfirmationCode, salt) };
                dataContext.Users.Add(existingUser);
            }
            else
            {
                var salt = _randomService.RandomString(16);

                existingUser.EmailCode = _kdfService.GetDerivedKey(ConfirmationCode, salt);
                existingUser.Salt = salt;
            }
            dataContext.SaveChanges();

            SendConfirmationCodeByEmail(existingUser.Email, ConfirmationCode);

            return Ok(new { status = "Data was accepted" });
        }

        [HttpPost("authorize")]
        [AllowAnonymous]
        public async Task<IActionResult> Authorize([FromBody] LoginDTO login)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var user = dataContext.Users.FirstOrDefault(u => u.Email == login.Email);
            if (user == null)
            {
                ModelState.AddModelError("Email", "User with this email does not exist");
                return BadRequest(ModelState);
            }
            if (user.EmailCode != _kdfService.GetDerivedKey(login.EmailCode, user.Salt))
            {
                ModelState.AddModelError("EmailCode", "Incorrect email code");
                return BadRequest(ModelState);
            }
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_key);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
            new Claim(ClaimTypes.NameIdentifier, user.Id)
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);
            var checkOldToken = dataContext.AuthTokens.FirstOrDefault(t => t.userId == Guid.Parse(user.Id));
            if (checkOldToken == null)
            {
                checkOldToken = new AuthToken
                {
                    Id = Guid.NewGuid(),
                    userId = Guid.Parse(user.Id),
                    Token = tokenString,
                    Used = 0
                };
                dataContext.AuthTokens.Add(checkOldToken);
            }
            else
            {
                checkOldToken.Token = tokenString;
            }
            dataContext.SaveChanges();
            return Ok(new { Token = tokenString });
        }


        #endregion

        #region Notes

        [HttpPost("sendBoard")]
        [Authorize]
        public async Task<IActionResult> SendBoard([FromForm] IFormCollection formData)
        {
            try
            {
                if (!formData.TryGetValue("board", out var boardJson))
                {
                    return BadRequest("Отсутствуют данные 'board'.");
                }
                var board = JsonConvert.DeserializeObject<BoardDTO>(boardJson);
                var user = dataContext.Users.FirstOrDefault(u => u.Email == board.Email);
                if (user == null)
                {
                    return BadRequest("Пользователь не найден.");
                }
                var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (!tokenCheck(user.Id, authHeader))
                {
                    return BadRequest("Invalid token");
                }
                var noteId = Guid.Parse(board.NoteId);
                var note = dataContext.Notes.FirstOrDefault(n => n.Id == noteId);

                if (note == null)
                {
                    note = new Note
                    {
                        Id = noteId,
                        userId = user.Id,
                        isDeleted = false,
                        teamspaceId = Guid.Empty,
                        isFavorite = false,
                        name = board.Title,
                        iconPath = board.iconPath,
                        routerLink = board.currentLink,
                    };
                    dataContext.Notes.Add(note);
                }
                else
                {
                    note.name = board.Title;
                }
                note.noteFileName = $"{note.Id}.json";
                var jsonFilePath = Path.Combine(_userFolderPath, note.userId.ToString(), note.noteFileName);
                Directory.CreateDirectory(Path.GetDirectoryName(jsonFilePath));
                System.IO.File.WriteAllText(jsonFilePath, boardJson.ToString());

                dataContext.SaveChanges();

                return Ok(new { message = "Заметка и опциональный файл успешно сохранены." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Произошла ошибка: {ex.Message}");
            }
        }

        [HttpPost("sendList")]
        [Authorize]
        public async Task<IActionResult> SendList([FromForm] IFormCollection formData)
        {
            try
            {
                // Проверка наличия данных с ключом 'board'
                if (!formData.TryGetValue("list", out var listJson))
                {
                    return BadRequest("Отсутствуют данные 'list'.");
                }

                // Преобразуем JSON в объект BoardDTO
                var list = JsonConvert.DeserializeObject<ListDTO>(listJson);

                // Найдем пользователя по адресу электронной почты
                var user = dataContext.Users.FirstOrDefault(u => u.Email == list.Email);
                if (user == null)
                {
                    return BadRequest("Пользователь не найден.");
                }
                var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (!tokenCheck(user.Id, authHeader))
                {
                    return BadRequest("Invalid token");
                }
                var noteId = Guid.Parse(list.NoteId);
                var note = dataContext.Notes.FirstOrDefault(n => n.Id == noteId);

                if (note == null)
                {
                    // Если заметки нет, создаем новую
                    note = new Note
                    {
                        Id = noteId,
                        userId = user.Id,
                        isDeleted = false,
                        teamspaceId = Guid.Empty,
                        isFavorite = false,
                        name = list.Title,
                        iconPath = list.iconPath,
                        routerLink = list.currentLink,
                    };

                    dataContext.Notes.Add(note); // Добавляем новую заметку в контекст данных
                }
                else
                {
                    // Если заметка уже существует, обновляем ее свойства
                    note.name = list.Title;
                    // note.isFavorite = board.isFavorite;
                    // Другие обновления при необходимости
                }

                // Логируем информацию для отладки
                Console.WriteLine(DateTime.Now.ToShortTimeString());
                Console.WriteLine("Название листа: " + list.Title);

                // Сохраняем JSON-файл с данными
                note.noteFileName = $"{note.Id}.json";
                var jsonFilePath = Path.Combine(_userFolderPath, note.userId.ToString(), note.noteFileName);

                // Проверяем, что папка существует
                Directory.CreateDirectory(Path.GetDirectoryName(jsonFilePath));

                // Сохраняем данные в JSON-файле
                System.IO.File.WriteAllText(jsonFilePath, listJson.ToString());

                // Если переданы файлы, сохраняем их
                if (formData.Files.Count > 0)
                {
                    var file = formData.Files[0]; // Первый файл в списке
                    var fileExtension = Path.GetExtension(file.FileName);
                    var fileSavePath = Path.Combine(_userFolderPath, note.userId.ToString(), $"{note.Id}{fileExtension}");

                    using (var stream = new FileStream(fileSavePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream); // Копируем файл в хранилище
                    }
                }

                dataContext.SaveChanges(); // Сохраняем изменения в базе данных

                return Ok(new { message = "Заметка и опциональный файл успешно сохранены." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Произошла ошибка: {ex.Message}");
            }
        }

        [HttpPost("sendGallery")]
        [Authorize]
        public async Task<IActionResult> SendGallery([FromForm] GalleryDTO gallery)
        {
            try
            {
                if (gallery == null)
                {
                    return BadRequest("Отсутствуют данные.");
                }

                var user = dataContext.Users.FirstOrDefault(u => u.Email == gallery.email);
                if (user == null)
                {
                    return BadRequest("Пользователь не найден.");
                }
                var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (!tokenCheck(user.Id, authHeader))
                {
                    return BadRequest("Invalid token");
                }
                var noteId = Guid.Parse(gallery.noteId);
                var note = dataContext.Notes.FirstOrDefault(n => n.Id == noteId);

                if (note == null)
                {
                    note = new Note
                    {
                        Id = noteId,
                        userId = user.Id,
                        isDeleted = false,
                        teamspaceId = Guid.Empty,
                        isFavorite = false,
                        name = gallery.title,
                        iconPath = gallery.iconPath,
                        routerLink = gallery.currentLink,
                        noteFileName = $"{noteId}.json"
                    };

                    dataContext.Notes.Add(note);
                }
                else
                {
                    note.name = gallery.title;
                }

                var userFolderPath = Path.Combine(AppConfig.UserFolderPath, user.Id.ToString());
                Directory.CreateDirectory(userFolderPath);

                var imagesFolderPath = Path.Combine(userFolderPath, "images");
                Directory.CreateDirectory(imagesFolderPath);

                foreach (var card in gallery.content)
                {
                    if (!string.IsNullOrEmpty(card.base64Image))
                    {
                        var imageBytes = Convert.FromBase64String(card.base64Image);
                        var fileSavePath = Path.Combine(imagesFolderPath, $"{card.Id}.png");

                        await System.IO.File.WriteAllBytesAsync(fileSavePath, imageBytes);

                        card.base64Image = null;
                        card.description = $"{card.Id}.png";
                    }
                }

                var jsonFilePath = Path.Combine(userFolderPath, note.noteFileName);
                Directory.CreateDirectory(Path.GetDirectoryName(jsonFilePath));
                System.IO.File.WriteAllText(jsonFilePath, JsonConvert.SerializeObject(gallery));

                dataContext.SaveChanges();

                return Ok(new { message = "Галерея успешно сохранена." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Произошла ошибка: {ex.Message}");
            }
        }

        [HttpPost("sendTable")]
        [Authorize]
        public async Task<IActionResult> SendTable([FromForm] IFormCollection formData)
        {
            try
            {
                // Проверка наличия данных с ключом 'board'
                if (!formData.TryGetValue("table", out var tableJson))
                {
                    return BadRequest("Отсутствуют данные 'table'.");
                }

                // Преобразуем JSON в объект BoardDTO
                var table = JsonConvert.DeserializeObject<TableDTO>(tableJson);

                // Найдем пользователя по адресу электронной почты
                var user = dataContext.Users.FirstOrDefault(u => u.Email == table.Email);
                if (user == null)
                {
                    return BadRequest("Пользователь не найден.");
                }
                var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (!tokenCheck(user.Id, authHeader))
                {
                    return BadRequest("Invalid token");
                }
                var noteId = Guid.Parse(table.NoteId);
                var note = dataContext.Notes.FirstOrDefault(n => n.Id == noteId);

                if (note == null)
                {
                    // Если заметки нет, создаем новую
                    note = new Note
                    {
                        Id = noteId,
                        userId = user.Id,
                        isDeleted = false,
                        teamspaceId = Guid.Empty,
                        isFavorite = false,
                        name = table.Title,
                        iconPath = table.iconPath,
                        routerLink = table.currentLink,
                    };

                    dataContext.Notes.Add(note); // Добавляем новую заметку в контекст данных
                }
                else
                {
                    // Если заметка уже существует, обновляем ее свойства
                    note.name = table.Title;
                    // note.isFavorite = board.isFavorite;
                    // Другие обновления при необходимости
                }

                // Логируем информацию для отладки
                Console.WriteLine(DateTime.Now.ToShortTimeString());
                Console.WriteLine("Название таблицы: " + table.Title);

                // Сохраняем JSON-файл с данными
                note.noteFileName = $"{note.Id}.json";
                var jsonFilePath = Path.Combine(_userFolderPath, note.userId.ToString(), note.noteFileName);

                // Проверяем, что папка существует
                Directory.CreateDirectory(Path.GetDirectoryName(jsonFilePath));

                // Сохраняем данные в JSON-файле
                System.IO.File.WriteAllText(jsonFilePath, tableJson.ToString());

                // Если переданы файлы, сохраняем их
                if (formData.Files.Count > 0)
                {
                    var file = formData.Files[0]; // Первый файл в списке
                    var fileExtension = Path.GetExtension(file.FileName);
                    var fileSavePath = Path.Combine(_userFolderPath, note.userId.ToString(), $"{note.Id}{fileExtension}");

                    using (var stream = new FileStream(fileSavePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream); // Копируем файл в хранилище
                    }
                }

                dataContext.SaveChanges(); // Сохраняем изменения в базе данных

                return Ok(new { message = "Заметка и опциональный файл успешно сохранены." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Произошла ошибка: {ex.Message}");
            }
        }

        [HttpPost("sendPage")]
        [Authorize]
        public async Task<IActionResult> SendPage([FromForm] IFormCollection formData)
        {
            try
            {
                if (!formData.TryGetValue("page", out var pageJson))
                {
                    return BadRequest("Отсутствуют данные 'page'.");
                }

                var page = JsonConvert.DeserializeObject<PageDTO>(pageJson);

                var user = dataContext.Users.FirstOrDefault(u => u.Email == page.Email);
                if (user == null)
                {
                    return BadRequest("Пользователь не найден.");
                }
                var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (!tokenCheck(user.Id, authHeader))
                {
                    return BadRequest("Invalid token");
                }
                var noteId = Guid.Parse(page.NoteId);
                var note = dataContext.Notes.FirstOrDefault(n => n.Id == noteId);

                if (note == null)
                {
                    note = new Note
                    {
                        Id = noteId,
                        userId = user.Id,
                        isDeleted = false,
                        teamspaceId = Guid.Empty,
                        isFavorite = false,
                        name = page.Title,
                        iconPath = page.iconPath,
                        routerLink = page.currentLink,
                    };

                    dataContext.Notes.Add(note);
                }
                else
                {
                    note.name = page.Title;
                }

                note.noteFileName = $"{note.Id}.json";
                var jsonFilePath = Path.Combine(_userFolderPath, note.userId.ToString(), note.noteFileName);
                Directory.CreateDirectory(Path.GetDirectoryName(jsonFilePath));
                System.IO.File.WriteAllText(jsonFilePath, pageJson.ToString());
                if (formData.Files.Count > 0)
                {
                    var file = formData.Files[0];
                    var fileExtension = Path.GetExtension(file.FileName);
                    var fileSavePath = Path.Combine(_userFolderPath, note.userId.ToString(), $"{note.Id}{fileExtension}");

                    using (var stream = new FileStream(fileSavePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                }

                dataContext.SaveChanges();

                return Ok(new { message = "Заметка и опциональный файл успешно сохранены." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Произошла ошибка: {ex.Message}");
            }
        }

        [HttpPost("getPage")]
        [Authorize]
        public IActionResult GetPage([FromBody] GetNoteDTO request)
        {
            Console.WriteLine("getPage");
            try
            {
                // Поиск пользователя по email
                var user = dataContext.Users.FirstOrDefault(u => u.Email == request.Email);
                if (user == null)
                {
                    return BadRequest("User not found.");
                }

                // Поиск токена для пользователя
                var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if(!tokenCheck(user.Id,authHeader))
                {
                    return BadRequest("Invalid token");
                }

                // Поиск заметки по NoteId
                Console.WriteLine(request.NoteId);
                var note = dataContext.Notes.FirstOrDefault(n => n.Id == Guid.Parse(request.NoteId) && n.isDeleted == false);
                if (note == null)
                {
                    return BadRequest("Note not found or deleted.");
                }

                // Путь к файлу JSON
                var jsonFilePath = Path.Combine(_userFolderPath, user.Id.ToString(), note.noteFileName);

                if (!System.IO.File.Exists(jsonFilePath))
                {
                    return BadRequest("File not found.");
                }

                // Считываем содержимое JSON-файла
                var jsonData = System.IO.File.ReadAllText(jsonFilePath);

                var jsonObject = JObject.Parse(jsonData);

                // Удаляем ненужные поля
                jsonObject.Remove("email");
                jsonObject.Remove("noteId");
                jsonObject.Remove("currentLink");

                var filteredJson = jsonObject.ToString();

                return Content(filteredJson, "application/json");

            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("getGallery")]
        [Authorize]
        public async Task<IActionResult> GetGallery([FromBody] GetNoteDTO request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.NoteId))
                {
                    return BadRequest("Отсутствуют данные.");
                }
                var user = dataContext.Users.FirstOrDefault(u => u.Email == request.Email);
                if (user == null)
                {
                    return NotFound("Пользователь не найден.");
                }
                var parsedNoteId = Guid.Parse(request.NoteId);
                var note = dataContext.Notes.FirstOrDefault(n => n.Id == parsedNoteId);
                if (note == null)
                {
                    return NotFound("Заметка не найдена.");
                }
                var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (!tokenCheck(user.Id, authHeader))
                {
                    return BadRequest("Invalid token");
                }
                var userFolderPath = Path.Combine(AppConfig.UserFolderPath, user.Id.ToString());
                var jsonFilePath = Path.Combine(userFolderPath, $"{note.Id}.json");
                if (!System.IO.File.Exists(jsonFilePath))
                {
                    return NotFound("JSON-файл с данными не найден.");
                }
                using (var streamReader = new StreamReader(jsonFilePath))
                {
                    var jsonData = await streamReader.ReadToEndAsync();
                    var galleryDto = JsonConvert.DeserializeObject<GalleryDTO>(jsonData);

                    var imagesFolderPath = Path.Combine(userFolderPath, "images");
                    foreach (var card in galleryDto.content)
                    {
                        var fileSavePath = Path.Combine(imagesFolderPath, $"{card.Id}.png");
                        if (System.IO.File.Exists(fileSavePath))
                        {
                            var imageBytes = await System.IO.File.ReadAllBytesAsync(fileSavePath);
                            card.base64Image = Convert.ToBase64String(imageBytes);
                        }
                    }

                    return Ok(galleryDto);
                }
            }
            catch (FormatException ex)
            {
                return BadRequest("Неверный формат ID заметки.");
            }
            catch (FileNotFoundException ex)
            {
                return NotFound("Не удалось найти файл.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Произошла ошибка: {ex.Message}");
            }
        }

        [HttpPost("getTemplate")]
        [Authorize]
        public IActionResult GetTemplate([FromBody] GetTemplateDTO request)
        {
            try
            {
                var user = dataContext.Users.FirstOrDefault(u => u.Email == request.Email);
                if (user == null)
                {
                    return BadRequest("User not found.");
                }
                var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (!tokenCheck(user.Id, authHeader))
                {
                    return BadRequest("Invalid token");
                }
                var jsonFilePath = Path.Combine(_userFolderPath, "templates", request.TemplateName + ".json");

                if (!System.IO.File.Exists(jsonFilePath))
                {
                    return BadRequest("File not found.");
                }
                var jsonData = System.IO.File.ReadAllText(jsonFilePath);
                var jsonObject = JObject.Parse(jsonData);
                jsonObject["noteId"] = request.NoteId;
                jsonObject["currentLink"] = request.currentLink;

                var note = dataContext.Notes.FirstOrDefault(n => n.Id == Guid.Parse(request.NoteId));

                if (note == null)
                {
                    note = new Note
                    {
                        Id = Guid.Parse(request.NoteId),
                        userId = user.Id,
                        isDeleted = false,
                        teamspaceId = Guid.Empty,
                        isFavorite = false,
                        name = jsonObject["title"].ToString(),
                        iconPath = jsonObject["iconPath"].ToString(),
                        routerLink = request.currentLink,
                        noteFileName  = $"{request.NoteId}.json"
                    };
                    dataContext.Notes.Add(note);
                }
                dataContext.SaveChanges();
                var saveJsonFilePath = Path.Combine(_userFolderPath, note.userId.ToString(), note.noteFileName);
                Directory.CreateDirectory(Path.GetDirectoryName(jsonFilePath));
                System.IO.File.WriteAllText(saveJsonFilePath, jsonObject.ToString());
                return Ok(new { status = "Data was accepted" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("delPage")]
        [Authorize]
        public IActionResult DelPage([FromBody] GetNoteDTO request)
        {
            try
            {
                var user = dataContext.Users.FirstOrDefault(u => u.Email == request.Email);
                if (user == null)
                {
                    return BadRequest("User not found.");
                }
                var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (!tokenCheck(user.Id, authHeader))
                {
                    return BadRequest("Invalid token");
                }
                Console.WriteLine(request.NoteId);
                var note = dataContext.Notes.FirstOrDefault(n => n.Id == Guid.Parse(request.NoteId));
                if (note == null)
                {
                    return BadRequest("Note not found.");
                }
                note.isDeleted = !note.isDeleted;
                dataContext.SaveChanges();

                return Ok("Deleted status was changed");

            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("fullDelPage")]
        [Authorize]
        public IActionResult FullDelPage([FromBody] GetNoteDTO request)
        {
            Console.WriteLine("delPage");
            try
            {
                // Поиск пользователя по email
                var user = dataContext.Users.FirstOrDefault(u => u.Email == request.Email);
                if (user == null)
                {
                    return BadRequest("User not found.");
                }
                var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (!tokenCheck(user.Id, authHeader))
                {
                    return BadRequest("Invalid token");
                }
                // Поиск заметки по NoteId
                Console.WriteLine(request.NoteId);
                var note = dataContext.Notes.FirstOrDefault(n => n.Id == Guid.Parse(request.NoteId));
                if (note == null)
                {
                    return BadRequest("Note not found.");
                }

                dataContext.Remove(note);
                dataContext.SaveChanges();

                return Ok("Note was deleted");

            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("favPage")]
        [Authorize]
        public IActionResult FavPage([FromBody] GetNoteDTO request)
        {
            try
            {
                var user = dataContext.Users.FirstOrDefault(u => u.Email == request.Email);
                if (user == null)
                {
                    return BadRequest("User not found.");
                }
                var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (!tokenCheck(user.Id, authHeader))
                {
                    return BadRequest("Invalid token");
                }
                var note = dataContext.Notes.FirstOrDefault(n => n.Id == Guid.Parse(request.NoteId));
                if (note == null)
                {
                    return BadRequest("Note not found.");
                }
                note.isFavorite = !note.isFavorite;
                dataContext.SaveChanges();
                return Ok("Favorite status was changed");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }


        [HttpPost("getUserNotes")]
        [Authorize]
        public IActionResult GetUserNotes([FromBody] GetAllNotes obj)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(obj.Email))
                {
                    return BadRequest("Email is required.");
                }

                var user = dataContext.Users.FirstOrDefault(u => u.Email == obj.Email);
                if (user == null)
                {
                    return BadRequest("User not found.");
                }
                var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (!tokenCheck(user.Id, authHeader))
                {
                    return BadRequest("Invalid token");
                }
                var userNotes = dataContext.Notes.Where(n => n.userId == user.Id && n.isDeleted == false).ToList();
                if (userNotes.Count == 0)
                {
                    return NotFound("No notes found for this user.");
                }

                var notesDTO = new List<NoteDTO>();
                foreach (var note in userNotes)
                {
                    var noteDTO = new NoteDTO
                    {
                        id = note.Id.ToString(),
                        name = note.name,
                        isFavorite = note.isFavorite,
                        iconPath = note.iconPath,
                        currentLink = note.routerLink
                    };
                    notesDTO.Add(noteDTO);
                }

                return Ok(notesDTO);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("getDeletedUserNotes")]
        [Authorize]
        public IActionResult GetDeletedUserNotes([FromBody] GetAllNotes obj)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(obj.Email))
                {
                    return BadRequest("Email is required."); // Проверка на пустой email
                }

                Console.WriteLine("Trying to get notes for email:", obj.Email);

                var user = dataContext.Users.FirstOrDefault(u => u.Email == obj.Email);
                if (user == null)
                {
                    return BadRequest("User not found."); // Пользователь не найден
                }
                var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (!tokenCheck(user.Id, authHeader))
                {
                    return BadRequest("Invalid token");
                }
                var userNotes = dataContext.Notes.Where(n => n.userId == user.Id && n.isDeleted == true).ToList();
                if (userNotes.Count == 0) // Проверка на наличие заметок
                {
                    return Ok("No deleted notes found for this user.");
                }

                var notesDTO = new List<NoteDTO>();
                foreach (var note in userNotes)
                {
                    var noteDTO = new NoteDTO
                    {
                        id = note.Id.ToString(),
                        name = note.name,
                        isFavorite = note.isFavorite,
                        iconPath = note.iconPath,
                        currentLink = note.routerLink
                    };
                    notesDTO.Add(noteDTO);
                }

                return Ok(notesDTO); // Успешный ответ
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}"); // Обработка ошибок
            }
        }

        #endregion

        // Отправка кода подтверждения по электронной почте
        private async Task SendConfirmationCodeByEmail(string email, string code)
        {
            string htmlBody = $@"
<html>
<head>
    <style>
        body {{
            font-family: Arial, sans-serif;
            background-color: #f9f9f9;
            color: #333;
            margin: 0;
            padding: 0;
        }}
        .container {{
            width: 100%;
            max-width: 600px;
            margin: 0 auto;
            background-color: #fff;
            padding: 20px;
            border-radius: 10px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }}
        .header {{
            background-color: #434BF3;
            padding: 20px;
            border-top-left-radius: 10px;
            border-top-right-radius: 10px;
        }}
        .header h1 {{
            margin: 0;
            color: #f1f1f1;
            font-size: 24px;
        }}
        .content {{
            padding: 20px;
        }}
        .confirmation-code {{
            font-size: 24px;
            color: #434BF3;
            font-weight: bold;
            margin: 20px 0;
        }}
        .footer {{
            margin-top: 20px;
            padding: 20px;
            background-color: #f1f1f1;
            border-bottom-left-radius: 10px;
            border-bottom-right-radius: 10px;
            text-align: center;
        }}
        .footer p {{
            margin: 5px 0;
        }}

        .img {{
            background-color: #f1f1f1;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Welcome to our community!</h1>
        </div>
        <div class='content'>
            <p>Hello,</p>
            <p>We are excited to welcome you to our community. To access your account, please use the following confirmation code:</p>
            <p class='confirmation-code'>{code}</p>
            <p>If you have any questions, feel free to contact us.</p>
        </div>
        <div class='footer'>
            <p>Best regards,<br/>Your support team</p>
            <img src='cid:logoImage' alt='Logo' width='100' />
        </div>
    </div>
</body>
</html>";


            await _emailSender.SendEmailAsync(email, "Confirmation Code", htmlBody);
        }

        private bool tokenCheck(string userId, string authHeader)
        {
            var token = dataContext.AuthTokens.FirstOrDefault(t => t.userId == Guid.Parse(userId));
            if (token == null)
            {
                return false;
            }

            // Получаем заголовок авторизации
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return false;
            }

            // Извлекаем токен из заголовка
            var incomingToken = authHeader.Substring("Bearer ".Length).Trim();

            // Сравниваем токены
            if (token.Token != incomingToken)
            {
                return false;
            }
            return true;
        }
    }
}