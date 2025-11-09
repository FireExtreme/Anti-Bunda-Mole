using SQLite;
using System;

namespace Anti_Bunda_Mole.Methods
{
    public class TaskModel
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string Type { get; set; } // "Recorrente" ou "Unica"

        public bool IsCompleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
