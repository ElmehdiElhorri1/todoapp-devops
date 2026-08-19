using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApp.Data;
using TodoApp.Models;

namespace TodoApp.Controllers;

public class TodoController(TodoDbContext db) : Controller
{
    [Route("/Error")]
    public IActionResult Error() => View();

    public async Task<IActionResult> Index()
    {
        var tasks = await db.TodoItems.OrderByDescending(t => t.Id).ToListAsync();
        return View(tasks);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(string title)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            db.TodoItems.Add(new TodoItem { Title = title.Trim() });
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var task = await db.TodoItems.FindAsync(id);
        if (task is not null)
        {
            task.Done = !task.Done;
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var task = await db.TodoItems.FindAsync(id);
        if (task is not null)
        {
            db.TodoItems.Remove(task);
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
