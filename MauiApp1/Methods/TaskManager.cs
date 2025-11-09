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
        public List<TaskModel> TasksInMemory { get; private set; } = new List<TaskModel>();

        public TaskManager()
        {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var appFolder = Path.Combine(documentsPath, "AntiBundaMole");
            if (!Directory.Exists(appFolder))
                Directory.CreateDirectory(appFolder);

            var dbPath = Path.Combine(appFolder, "tasks.db3");
            _database = new SQLiteAsyncConnection(dbPath);
            _database.CreateTableAsync<TaskModel>().Wait();
        }

        public Task<List<TaskModel>> GetAllTasksAsync()
        {
            return _database.Table<TaskModel>().ToListAsync();
        }

        public Task<List<TaskModel>> GetPendingTasksAsync()
        {
            return _database.Table< TaskModel   >().Where(t => !t.IsCompleted).ToListAsync();
        }

        public Task<TaskModel> GetTaskByIdAsync(int id)
        {
            return _database.Table< TaskModel>().Where(t => t.Id == id).FirstOrDefaultAsync();
        }

        public async Task SaveTaskAsync(TaskModel task)
        {
            if (task.Id != 0)
                await _database.UpdateAsync(task);
            else
                await _database.InsertAsync(task);

            await LoadTasksIntoMemoryAsync(); 
        }

        public async Task DeleteTaskAsync(TaskModel task)
        {
            await _database.DeleteAsync(task);
            await LoadTasksIntoMemoryAsync(); 
        }
        public async Task LoadTasksIntoMemoryAsync()
        {
            TasksInMemory = await _database.Table<TaskModel>().ToListAsync();
        }

    }
}
