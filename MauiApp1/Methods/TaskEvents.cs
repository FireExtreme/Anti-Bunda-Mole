using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anti_Bunda_Mole.Methods
{
    public  class TaskEvents
    {
        public static event Action? TasksUpdated;

        public static void RaiseTasksUpadated()
        {
            TasksUpdated.Invoke();
        }
    }
}
