using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin.LocalNotification;
using D424.Classes;
using D424;
using SQLite;

namespace D424.Classes
{
    public static class DB
    {
        
        public static List<Course> GetCourses(SQLiteConnection db, Term term)
        {
            var courses = db.Query<Course>("SELECT * FROM Courses WHERE termId = ?", term.TermId);
            return courses;
        }

        public static void Update_Note(SQLiteConnection db, Note note)
        {
            db.Update(note);
        }

        public static void Add_Note(SQLiteConnection db, Note note)
        {
            db.Insert(note);
        }

        public static void Update_CourseInstructor(SQLiteConnection db, Course course, Instructor instructor)
        {
            // Always create a new instructor entry to avoid cascading updates
            var newInstructor = new Instructor
            {
                InstructorName = instructor.InstructorName,
                InstructorPhone = instructor.InstructorPhone,
                InstructorEmail = instructor.InstructorEmail
            };

            db.Insert(newInstructor);

            // Update the course to point to the new instructor
            course.InstructorId = newInstructor.InstructorId;
            db.Update(course);
        }


        public static void Add_Instructor(SQLiteConnection db, Instructor instructor)
        {
            db.Insert(instructor);
        }

        public static void Update_Assessment(SQLiteConnection db, Assessment assessment)
        {
            db.Update(assessment);
        }

        public static void Add_Assessment(SQLiteConnection db, Assessment assessment)
        {
            db.Insert(assessment);
        }

        public static void Update_Course(SQLiteConnection db, Course course)
        {
            db.Update(course);
        }

        public static void Add_Course(SQLiteConnection db, Course course)
        {
            db.Insert(course);
        }

        public static void Update_Term(SQLiteConnection db, Term term)
        {
            db.Update(term);
        }

        public static void Add_Term(SQLiteConnection db, Term term)
        {
            db.Insert(term);
        }

        public static void Generate_Tbl()
        {
            var databasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MyData.db");

            using (var db = new SQLiteConnection(databasePath))
            {
                db.CreateTable<Term>();
                db.CreateTable<Course>();
                db.CreateTable<Assessment>();
                db.CreateTable<Instructor>();
                db.CreateTable<Note>();
            }
        }

        public static void AddNewCourse(int termId)
        {
            using (var db = new SQLiteConnection(MainPage.databasePath))
            {
                // Create new assessments
                var performanceAssessment = new Assessment(1, "Performance Assessment", DateTime.Now, DateTime.Now.AddMonths(3), "Enter details about assessment:", 1);
                var objectiveAssessment = new Assessment(0, "Objective Assessment", DateTime.Now, DateTime.Now.AddMonths(3), "Enter details about assessment:", 1);

                // Create a new course
                var newCourse = new Course(termId, 1, "NewCourse", DateTime.Now, DateTime.Now.AddMonths(4), "Plan to Take", "Enter Course Details:", 1, 2);
                Add_Course(db, newCourse);

                // Retrieve the new course ID
                var courseResult = db.Query<Course>("SELECT courseId FROM Courses WHERE courseName = ?", "NewCourse");
                var newCourseId = courseResult[0].CourseId;

                // Set courseId for assessments
                performanceAssessment.CourseId = newCourseId;
                objectiveAssessment.CourseId = newCourseId;

                // Insert assessments
                Add_Assessment(db, performanceAssessment);
                Add_Assessment(db, objectiveAssessment);

                // Retrieve assessment IDs
                var assessmentResults = db.Query<Assessment>("SELECT assessmentId, type FROM Assessments WHERE courseId = ?", newCourseId);
                foreach (var assessment in assessmentResults)
                {
                    if (assessment.Type == 1)
                    {
                        newCourse.PerformanceAssessment = assessment.AssessmentId;
                    }
                    else
                    {
                        newCourse.ObjectiveAssessment = assessment.AssessmentId;
                    }
                }

                // Update the course with assessment IDs
                db.Update(newCourse);

                // Sync database
                MainPage.Sync_DB();
            }
        }

        public static void Add_NewTerm()
        {
            using (var db = new SQLiteConnection(MainPage.databasePath))
            {
                // Count the current number of terms and generate a new term name sequentially
                int currentTermCount = db.Table<Term>().Count();
                var newTermId = currentTermCount + 1;  // New term ID based on the number of terms + 1
                var termName = "Term " + newTermId;

                // Check if the term name already exists to avoid duplicates
                while (db.Table<Term>().Any(t => t.TermName == termName))
                {
                    newTermId++;
                    termName = "Term " + newTermId;
                }

                // Create a new term with the incremented name and default dates
                var newTerm = new Term(termName, DateTime.Now, DateTime.Now.AddDays(60));
                db.Insert(newTerm);

                // Sync the database
                MainPage.Sync_DB();
            }
        }

        public static void DeleteCourse(Course course)
        {
            using (var db = new SQLiteConnection(MainPage.databasePath))
            {
                db.Delete(course);
            }
        }

        public static void DeleteNote(Note note)
        {
            using (var db = new SQLiteConnection(MainPage.databasePath))
            {
                db.Delete(note);
            }
        }

    }
}
