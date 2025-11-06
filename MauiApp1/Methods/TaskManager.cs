using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Anti_Bunda_Mole.Methods
{
    internal class TaskManager
    {
        private readonly SQLiteAsyncConnection _database;
        public List<TaskItem> TasksInMemory { get; private set; } = new List<TaskItem>();

        public TaskManager()
        {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var appFolder = Path.Combine(documentsPath, "AntiBundaMole");
            if (!Directory.Exists(appFolder))
                Directory.CreateDirectory(appFolder);

            var dbPath = Path.Combine(appFolder, "tasks.db3");
            _database = new SQLiteAsyncConnection(dbPath);
            _database.CreateTableAsync<TaskItem>().Wait();
        }

        public Task<List<TaskItem>> GetAllTasksAsync()
        {
            return _database.Table<TaskItem>().ToListAsync();
        }

        public Task<List<TaskItem>> GetPendingTasksAsync()
        {
            return _database.Table<TaskItem>().Where(t => !t.IsCompleted).ToListAsync();
        }

        public Task<TaskItem> GetTaskByIdAsync(int id)
        {
            return _database.Table<TaskItem>().Where(t => t.Id == id).FirstOrDefaultAsync();
        }

        public async Task SaveTaskAsync(TaskItem task)
        {
            if (task.Id != 0)
                await _database.UpdateAsync(task);
            else
                await _database.InsertAsync(task);

            await LoadTasksIntoMemoryAsync(); 
        }

        public async Task DeleteTaskAsync(TaskItem task)
        {
            await _database.DeleteAsync(task);
            await LoadTasksIntoMemoryAsync(); 
        }
        public async Task LoadTasksIntoMemoryAsync()
        {
            TasksInMemory = await _database.Table<TaskItem>().ToListAsync();
        }

    }
}
