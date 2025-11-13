using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GradesProject
{
    public static class ValidationHelper
    {
        // Centralized validation with detailed error reporting
        public static ValidationResult ValidateObject<T>(T obj, JSchema schema, string objectName = "Object")
        {
            try
            {
                var jObject = JObject.FromObject(obj);
                bool isValid = jObject.IsValid(schema, out IList<string> errors);

                return new ValidationResult
                {
                    IsValid = isValid,
                    Errors = errors?.ToList() ?? new List<string>(),
                    ObjectName = objectName,
                    ValidatedObject = jObject
                };
            }
            catch (Exception ex)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Errors = new List<string> { $"Serialization error: {ex.Message}" },
                    ObjectName = objectName
                };
            }
        }

        // Validate entire course list
        public static ValidationResult ValidateCourses(List<Course> courses, JSchema schema)
        {
            var allErrors = new List<string>();
            var validCourses = new List<Course>();
            var invalidCourses = new List<string>();

            foreach (var course in courses)
            {
                var result = ValidateObject(course, schema, $"Course {course.Code}");

                if (result.IsValid)
                {
                    validCourses.Add(course);
                }
                else
                {
                    invalidCourses.Add(course.Code);
                    allErrors.AddRange(result.Errors.Select(e => $"{course.Code}: {e}"));
                }
            }

            return new ValidationResult
            {
                IsValid = allErrors.Count == 0,
                Errors = allErrors,
                ObjectName = "Course Collection",
                AdditionalInfo = new Dictionary<string, object>
                {
                    ["ValidCourses"] = validCourses.Count,
                    ["InvalidCourses"] = invalidCourses,
                    ["TotalCourses"] = courses.Count
                }
            };
        }

        // Safe input parsing with validation
        public static T SafeParse<T>(string input, Func<string, T> parser, T defaultValue, string fieldName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(input))
                    return defaultValue;

                return parser(input);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Invalid {fieldName}: {ex.Message}. Using default value: {defaultValue}");
                return defaultValue;
            }
        }

        // Display validation errors with better formatting
        public static void DisplayValidationErrors(ValidationResult result)
        {
            if (result.IsValid)
            {
                Console.WriteLine($"✓ {result.ObjectName} is valid");
                return;
            }

            Console.WriteLine($"✗ {result.ObjectName} has validation errors:");
            Console.WriteLine(new string('-', 50));

            foreach (var error in result.Errors)
            {
                Console.WriteLine($"  • {error}");
            }

            if (result.AdditionalInfo != null)
            {
                Console.WriteLine("\nAdditional Info:");
                foreach (var kvp in result.AdditionalInfo)
                {
                    Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
                }
            }
            Console.WriteLine(new string('-', 50));
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public string ObjectName { get; set; }
        public JObject ValidatedObject { get; set; }
        public Dictionary<string, object> AdditionalInfo { get; set; }
    }

    // Enhanced error handling extensions for your existing methods
    public static class HelperClassExtensions
    {
        // Replace your existing SaveCourses method with this
        public static bool SaveCoursesWithValidation(this List<Course> courses, JSchema schema)
        {
            var validationResult = ValidationHelper.ValidateCourses(courses, schema);

            ValidationHelper.DisplayValidationErrors(validationResult);

            if (validationResult.IsValid)
            {
                try
                {
                    File.WriteAllText("grades.json", JsonConvert.SerializeObject(courses, Formatting.Indented));
                    Console.WriteLine("✓ Courses saved successfully.");
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error saving file: {ex.Message}");
                    return false;
                }
            }
            else
            {
                Console.WriteLine("✗ Courses not saved due to validation errors.");
                Console.WriteLine("Fix the errors above and try again.");
                return false;
            }
        }

        // Safe course addition with validation
        public static bool AddCourseWithValidation(this List<Course> courses, JSchema schema)
        {
            Console.Write("Enter course code (format UUUU-####): ");
            var code = Console.ReadLine()?.Trim().ToUpper();

            var newCourse = new Course { Code = code };
            var validationResult = ValidationHelper.ValidateObject(newCourse, schema, $"New Course {code}");

            if (validationResult.IsValid)
            {
                courses.Add(newCourse);
                Console.WriteLine("✓ Course added successfully.");
                return true;
            }
            else
            {
                ValidationHelper.DisplayValidationErrors(validationResult);
                Console.WriteLine("✗ Course not added due to validation errors.");
                return false;
            }
        }

        // Safe evaluation addition with validation
        public static bool AddEvaluationWithValidation(this Course course, JSchema schema)
        {
            Console.Write("Enter evaluation description: ");
            var description = Console.ReadLine()?.Trim();

            Console.Write("Enter OutOf (max marks): ");
            var outOf = ValidationHelper.SafeParse(Console.ReadLine(), int.Parse, 100, "OutOf");

            Console.Write("Enter Weight %: ");
            var weight = ValidationHelper.SafeParse(Console.ReadLine(), double.Parse, 0.0, "Weight");

            Console.Write("Enter Earned Marks (or leave blank for unassigned): ");
            var input = Console.ReadLine()?.Trim();
            double? earnedMarks = string.IsNullOrWhiteSpace(input)
      ? null
      : ValidationHelper.SafeParse(input, double.Parse, 0.0, "Earned Marks");
            var eval = new Evaluation
            {
                Description = description,
                OutOf = outOf,
                Weight = weight,
                EarnedMarks = earnedMarks
            };

            // Create temporary course copy for validation
            var tempCourse = new Course
            {
                Code = course.Code,
                Evaluations = new List<Evaluation>(course.Evaluations) { eval }
            };

            var validationResult = ValidationHelper.ValidateObject(tempCourse, schema, $"Course {course.Code} with new evaluation");

            if (validationResult.IsValid)
            {
                course.Evaluations.Add(eval);
                Console.WriteLine("✓ Evaluation added successfully.");
                return true;
            }
            else
            {
                ValidationHelper.DisplayValidationErrors(validationResult);
                Console.WriteLine("✗ Evaluation not added due to validation errors.");
                return false;
            }
        }
    }
}