using System;

namespace GradesProject
{
    class Program
    {
        static void Main(string[] args)
        {
            HelperClass.LoadSchema();
            HelperClass.LoadCourses();
            HelperClass.MainMenu();

        }
    }
}