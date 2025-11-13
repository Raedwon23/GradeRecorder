using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace GradesProject
{
    public static class HelperClass
    {
        public static List<Course> courses = new List<Course>();
        public static JSchema schema;

        public static void LoadSchema()
        {
            try
            {
                schema = JSchema.Parse(File.ReadAllText("schema.json"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading schema: {ex.Message}");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }

        public static void LoadCourses()
        {
            if (File.Exists("grades.json"))
            {
                try
                {
                    string jsonText = File.ReadAllText("grades.json");
                    courses = JsonConvert.DeserializeObject<List<Course>>(jsonText) ?? new List<Course>();
                }
                catch (Exception)
                {
                    courses = new List<Course>();
                }
            }
            else
            {
                Console.Write("Grades data file grades.json not found. Create new file (y/n): ");
                if (Console.ReadLine().ToLower() == "y")
                {
                    courses = new List<Course>();
                    SaveCourses();
                    Console.WriteLine("\nNew data set created. Press any key to continue...");
                    Console.ReadKey();
                }
            }
        }

        public static void SaveCourses()
        {
            try
            {
                string json = JsonConvert.SerializeObject(courses, Formatting.Indented);
                File.WriteAllText("grades.json", json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving courses: {ex.Message}");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }

        public static void MainMenu()
        {
            while (true)
            {
                Console.Clear();
                DisplayGradesSummary();

                Console.WriteLine("--------------------------------------------------------------------------------");
                Console.WriteLine(" Press # from the above list to view/edit/delete a specific course.");
                Console.WriteLine(" Press A to add a new course.");
                Console.WriteLine(" Press X to quit.");
                Console.WriteLine("--------------------------------------------------------------------------------");
                Console.Write("Enter a command: ");

                string input = Console.ReadLine()?.ToUpper();

                if (input == "A")
                {
                    AddCourse();
                }
                else if (input == "X")
                {
                    SaveCourses();
                    break;
                }
                else if (int.TryParse(input, out int courseNum) && courseNum >= 1 && courseNum <= courses.Count)
                {
                    CourseMenu(courseNum - 1);
                }
            }
        }

        public static void DisplayGradesSummary()
        {
            Console.WriteLine("                           ~ GRADES TRACKING SYSTEM ~");
            Console.WriteLine();
            Console.WriteLine("+------------------------------------------------------------------------------+");
            Console.WriteLine("|                                Grades Summary                                |");
            Console.WriteLine("+------------------------------------------------------------------------------+");
            Console.WriteLine();

            if (courses.Count == 0)
            {
                Console.WriteLine("There are currently no saved courses.");
                Console.WriteLine();
                return;
            }

            Console.WriteLine(" #. Course       Marks Earned     Out Of    Percent");
            Console.WriteLine();

            for (int i = 0; i < courses.Count; i++)
            {
                var course = courses[i];
                double courseMarksTotal = course.Evaluations.Sum(e => e.EarnedMarks.HasValue ?
                    Math.Round((100 * e.EarnedMarks.Value / e.OutOf) * e.Weight / 100, 1) : 0);
                double weightTotal = course.Evaluations.Where(e => e.EarnedMarks.HasValue).Sum(e => e.Weight);
                double percentTotal = weightTotal > 0 ? Math.Round(100 * courseMarksTotal / weightTotal, 1) : 0.0;

                Console.WriteLine($" {i + 1}. {course.Code,-12}        {courseMarksTotal,8}       {weightTotal,5}       {percentTotal,5}");
            }
            Console.WriteLine();
        }

        public static void AddCourse()
        {
            while (true)
            {
                Console.Write("Enter a course code: ");
                string code = Console.ReadLine()?.Trim();

                if (string.IsNullOrWhiteSpace(code))
                    continue;

                var newCourse = new Course { Code = code };

                if (ValidateCourse(newCourse))
                {
                    courses.Add(newCourse);
                    break;
                }
                else
                {
                    Console.WriteLine("ERROR: Invalid course code.");
                }
            }
        }

        public static void CourseMenu(int courseIndex)
        {
            var course = courses[courseIndex];

            while (true)
            {
                Console.Clear();
                DisplayCourseEvaluations(course);

                Console.WriteLine("--------------------------------------------------------------------------------");
                Console.WriteLine(" Press D to delete this course.");
                Console.WriteLine(" Press A to add an evaluation.");
                Console.WriteLine(" Press # from the above list to edit/delete a specific evaluation.");
                Console.WriteLine(" Press X to return to the main menu.");
                Console.WriteLine("--------------------------------------------------------------------------------");
                Console.Write("Enter a command: ");

                string input = Console.ReadLine()?.Trim().ToUpper();

                if (input == "A")
                {
                    AddEvaluation(course);
                }
                else if (input == "D")
                {
                    Console.Write($"Delete {course.Code}? (y/n): ");
                    if (Console.ReadLine()?.ToLower() == "y")
                    {
                        courses.RemoveAt(courseIndex);
                        break;
                    }
                }
                else if (input == "X")
                {
                    break;
                }
                else if (int.TryParse(input, out int evalNum) && evalNum >= 1 && evalNum <= course.Evaluations.Count)
                {
                    EvaluationMenu(course, evalNum - 1);
                }
            }
        }

        public static void DisplayCourseEvaluations(Course course)
        {
            Console.WriteLine("                           ~ GRADES TRACKING SYSTEM ~");
            Console.WriteLine();
            Console.WriteLine("+------------------------------------------------------------------------------+");
            Console.WriteLine($"|                            {course.Code} Evaluations                             |");
            Console.WriteLine("+------------------------------------------------------------------------------+");
            Console.WriteLine();

            if (course.Evaluations.Count == 0)
            {
                Console.WriteLine($"There are currently no evaluations for {course.Code}.");
                Console.WriteLine();
                return;
            }

            Console.WriteLine(" #. Evaluation         Marks Earned   Out Of  Percent  Course Marks  Weight/100");
            Console.WriteLine();

            for (int i = 0; i < course.Evaluations.Count; i++)
            {
                var eval = course.Evaluations[i];
                string earnedMarks = eval.EarnedMarks.HasValue ? eval.EarnedMarks.Value.ToString("F1") : "";
                double percentEvaluation = eval.EarnedMarks.HasValue ? Math.Round(100 * eval.EarnedMarks.Value / eval.OutOf, 1) : 0.0;
                double courseMarksEvaluation = eval.EarnedMarks.HasValue ? Math.Round(percentEvaluation * eval.Weight / 100, 1) : 0.0;

                Console.WriteLine($" {i + 1}. {eval.Description,-18} {earnedMarks,12} {eval.OutOf,8:F1} {percentEvaluation,8:F1} {courseMarksEvaluation,12:F1} {eval.Weight,11:F1}");
            }
            Console.WriteLine();
        }

        public static void AddEvaluation(Course course)
        {
            while (true)
            {
                Console.Write("Enter a description: ");
                string description = Console.ReadLine()?.Trim();
                if (string.IsNullOrWhiteSpace(description)) continue;

                Console.Write("Enter the 'out of' mark: ");
                if (!double.TryParse(Console.ReadLine(), out double outOf))
                {
                    Console.WriteLine("ERROR: Invalid evaluation.");
                    continue;
                }

                Console.Write("Enter the % weight: ");
                if (!double.TryParse(Console.ReadLine(), out double weight))
                {
                    Console.WriteLine("ERROR: Invalid evaluation.");
                    continue;
                }

                Console.Write("Enter marks earned or press ENTER to skip: ");
                string earnedInput = Console.ReadLine()?.Trim();
                double? earnedMarks = null;

                if (!string.IsNullOrWhiteSpace(earnedInput))
                {
                    if (double.TryParse(earnedInput, out double earned))
                        earnedMarks = earned;
                    else
                    {
                        Console.WriteLine("ERROR: Invalid evaluation.");
                        continue;
                    }
                }

                var evaluation = new Evaluation
                {
                    Description = description,
                    OutOf = (int)outOf,
                    Weight = weight,
                    EarnedMarks = earnedMarks
                };

                // Create temporary course for validation
                var tempCourse = new Course
                {
                    Code = course.Code,
                    Evaluations = new List<Evaluation>(course.Evaluations) { evaluation }
                };

                if (ValidateCourse(tempCourse))
                {
                    course.Evaluations.Add(evaluation);
                    break;
                }
                else
                {
                    Console.WriteLine("ERROR: Invalid evaluation.");
                }
            }
        }

        public static void EvaluationMenu(Course course, int evalIndex)
        {
            var evaluation = course.Evaluations[evalIndex];

            while (true)
            {
                Console.Clear();
                Console.WriteLine("                           ~ GRADES TRACKING SYSTEM ~");
                Console.WriteLine();
                Console.WriteLine("+------------------------------------------------------------------------------+");
                Console.WriteLine($"|                               {course.Code} {evaluation.Description}                               |");
                Console.WriteLine("+------------------------------------------------------------------------------+");
                Console.WriteLine();

                string earnedMarks = evaluation.EarnedMarks.HasValue ? evaluation.EarnedMarks.Value.ToString("F1") : "";
                double percentEvaluation = evaluation.EarnedMarks.HasValue ? Math.Round(100 * evaluation.EarnedMarks.Value / evaluation.OutOf, 1) : 0.0;
                double courseMarksEvaluation = evaluation.EarnedMarks.HasValue ? Math.Round(percentEvaluation * evaluation.Weight / 100, 1) : 0.0;

                Console.WriteLine($"Marks Earned   Out Of  Percent  Course Marks  Weight/100");
                Console.WriteLine();
                Console.WriteLine($"{earnedMarks,12} {evaluation.OutOf,8:F1} {percentEvaluation,8:F1} {courseMarksEvaluation,12:F1} {evaluation.Weight,11:F1}");
                Console.WriteLine();

                Console.WriteLine("--------------------------------------------------------------------------------");
                Console.WriteLine(" Press D to delete this evaluation.");
                Console.WriteLine(" Press E to edit this evaluation.");
                Console.WriteLine(" Press X to return to the previous menu.");
                Console.WriteLine("--------------------------------------------------------------------------------");
                Console.Write("Enter a command: ");

                string input = Console.ReadLine()?.Trim().ToUpper();

                if (input == "D")
                {
                    Console.Write($"Delete {evaluation.Description}? (y/n): ");
                    if (Console.ReadLine()?.ToLower() == "y")
                    {
                        course.Evaluations.RemoveAt(evalIndex);
                        break;
                    }
                }
                else if (input == "E")
                {
                    EditEvaluation(course, evaluation);
                }
                else if (input == "X")
                {
                    break;
                }
            }
        }

        public static void EditEvaluation(Course course, Evaluation evaluation)
        {
            while (true)
            {
                Console.Write($"Enter marks earned out of {evaluation.OutOf}, press ENTER to leave unassigned: ");
                string input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                {
                    evaluation.EarnedMarks = null;
                    break;
                }
                else if (double.TryParse(input, out double earned))
                {
                    var originalEarnedMarks = evaluation.EarnedMarks;
                    evaluation.EarnedMarks = earned;

                    // Validate the course with updated evaluation
                    if (ValidateCourse(course))
                    {
                        break;
                    }
                    else
                    {
                        Console.WriteLine("ERROR: Invalid 'marks earned' value.");
                        evaluation.EarnedMarks = originalEarnedMarks; // Restore original value
                    }
                }
                else
                {
                    Console.WriteLine("ERROR: Invalid 'marks earned' value.");
                }
            }
        }

        public static bool ValidateCourse(Course course)
        {
            if (schema == null) return true;

            try
            {
                var jObject = JObject.FromObject(course);
                return jObject.IsValid(schema);
            }
            catch
            {
                return false;
            }
        }
    }
}