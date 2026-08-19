using Microsoft.EntityFrameworkCore;
using TodoApp.Data;
using TodoApp.Models;

namespace TodoApp.Tests;

public class TodoDbContextTests
{
    private static TodoDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TodoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TodoDbContext(options);
    }

    [Fact]
    public async Task AddingTask_PersistsWithDoneFalse()
    {
        using var db = CreateContext();

        db.TodoItems.Add(new TodoItem { Title = "Write CLAUDE.md" });
        await db.SaveChangesAsync();

        var task = Assert.Single(db.TodoItems);
        Assert.Equal("Write CLAUDE.md", task.Title);
        Assert.False(task.Done);
    }

    [Fact]
    public async Task TogglingTask_FlipsDoneState()
    {
        using var db = CreateContext();
        var task = new TodoItem { Title = "Deploy to k8s" };
        db.TodoItems.Add(task);
        await db.SaveChangesAsync();

        task.Done = !task.Done;
        await db.SaveChangesAsync();

        Assert.True(db.TodoItems.Single().Done);
    }

    [Fact]
    public async Task DeletingTask_RemovesIt()
    {
        using var db = CreateContext();
        var task = new TodoItem { Title = "Temporary" };
        db.TodoItems.Add(task);
        await db.SaveChangesAsync();

        db.TodoItems.Remove(task);
        await db.SaveChangesAsync();

        Assert.Empty(db.TodoItems);
    }
}
