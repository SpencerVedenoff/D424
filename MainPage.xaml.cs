using Plugin.LocalNotification;
using D424.Classes;
using SQLite;
using System.Text;
using System.Collections.ObjectModel;

namespace D424
{
    public partial class MainPage : ContentPage
    {
        public static List<Term> terms = new List<Term>();
        public static Dictionary<Term, List<Course>> courses = new Dictionary<Term, List<Course>>();
        public static Dictionary<int, Course> courseList = new Dictionary<int, Course>();
        public static Dictionary<int, Assessment> assessments = new Dictionary<int, Assessment>();
        public static Dictionary<int, Instructor> instructors = new Dictionary<int, Instructor>();
        public static Dictionary<int, Note> notes = new Dictionary<int, Note>();
        public static Term termSelected;
        public static List<String> statusValues = new List<String>();
        public static List<int> notificationValues = new List<int>();
        public static IList<NotificationRequest> notificationRequestsStatic = new List<NotificationRequest>();
        public static string databasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MyData.db");

        public MainPage()
        {
            InitializeComponent();

            // Check if it's the first launch to initialize dummy data only once
            if (Preferences.Get("IsFirstLaunch", true))
            {

                InitializeDummyData();
                Preferences.Set("IsFirstLaunch", false);

            }

            LoadSavedData();
            ConfigureUI(1);
            PopulateStatusValues();
            PopulateNotificationValues();
        }

        private async void OnGenerateReportClicked(object sender, EventArgs e)
        {
            try
            {
                using var db = new SQLiteConnection(MainPage.databasePath);

                // Ensure a term is selected
                if (termSelected == null)
                {
                    await DisplayAlert("Error", "Please select a term to generate the report.", "OK");
                    return;
                }

                // Query to get courses for the selected term
                var query = "SELECT * FROM Courses WHERE TermId = ?";
                var result = db.Query<Course>(query, termSelected.TermId);

                if (result.Count == 0)
                {
                    await DisplayAlert("Report", $"No courses found for the term '{termSelected.TermName}'.", "OK");
                    return;
                }

                // Format the report
                var report = new StringBuilder($"Course Report for Term: {termSelected.TermName}\n\n");
                foreach (var course in result)
                {
                    report.AppendLine($"Title: {course.CourseName}");
                    report.AppendLine($"Start: {course.Start}");
                    report.AppendLine($"End: {course.End}");
                    report.AppendLine($"Status: {course.Status}\n");
                }

                // Display the report
                await DisplayAlert("Report", report.ToString(), "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to generate report: {ex.Message}", "OK");
            }
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = e.NewTextValue?.ToLower() ?? string.Empty;

            using var db = new SQLiteConnection(MainPage.databasePath);

            // Parameterized query to prevent SQL injection
            var query = "SELECT * FROM Courses WHERE LOWER(CourseName) LIKE ?";

            try
            {
                // Pass the parameter to the query
                var courses = db.Query<Course>(query, $"%{searchText}%");

                // Update the CollectionView
                if (courses.Count > 0)
                {
                    SearchResults.IsVisible = true;
                    SearchResults.ItemsSource = courses;
                }
                else
                {
                    SearchResults.IsVisible = false;
                }
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine($"Error querying database: {ex.Message}");
                SearchResults.IsVisible = false;
            }
        }

        private void OnSearchBarUnfocused(object sender, EventArgs e)
        {
            // Safely handle the unfocus action
            CourseSearchBar.IsVisible = true; // Keep visible if needed
        }

        private void DismissKeyboard()
        {
            CourseSearchBar.Unfocus();
        }

        private void OnSearchButtonPressed(object sender, EventArgs e)
        {
            CourseSearchBar.Unfocus(); // This will trigger the OnSearchBarUnfocused method
        }


        private async void OnCourseSelected(object sender, SelectionChangedEventArgs e)
        {
            var selectedCourse = e.CurrentSelection.FirstOrDefault() as Course;
            if (selectedCourse == null)
                return;

            // Display course details
            await DisplayAlert("Course Details",
                $"Title: {selectedCourse.CourseName}\n" +
                $"Start: {selectedCourse.Start:MM/dd/yyyy}\n" +
                $"End: {selectedCourse.End:MM/dd/yyyy}\n" +
                $"Status: {selectedCourse.Status}",
                "OK");

            // Clear selection
            ((CollectionView)sender).SelectedItem = null;
        }

        private void InitializeDummyData()
        {
            // Call the method to add dummy data
            AddDummyData();
        }

        private void LoadSavedData()
        {
            // Load saved data into the application
            Load_Data();
        }

        private void ConfigureUI(int id)
        {
            // Configure the user interface with the given ID
            Load_Interface(id);
        }
        private void PopulateStatusValues()
        {
            // Add the various status values
            statusValues.AddRange(new List<string> { "In Progress", "Completed", "Dropped", "Plan to Take" });
        }

        private void PopulateNotificationValues()
        {
            // Add the various notification values
            notificationValues.AddRange(new List<int> { 0, 1, 2, 3, 5, 7, 14 });
        }

        public async void NotifyPrompt()
        {
            await UpdatePendingNotifications();
            if (await NotificationsNotEnabled())
            {
                await RequestNotificationPermission();
            }

            List<NotificationRequest> requests = new List<NotificationRequest>();
            List<int> cancelledRequests = new List<int>();
            DateTime currentTime = DateTime.Now;

            foreach (List<Course> courseList in courses.Values)
            {
                foreach (Course course in courseList)
                {
                    HandleCourseNotification(course, currentTime, requests, cancelledRequests);
                }
            }

            foreach (Assessment assessment in assessments.Values)
            {
                HandleAssessmentNotification(assessment, currentTime, requests, cancelledRequests);
            }

            CancelNotifications(cancelledRequests);
            await ShowNotifications(requests);
        }

        public async Task UpdatePendingNotifications()
        {
            notificationRequestsStatic = await LocalNotificationCenter.Current.GetPendingNotificationList();
        }

        private async Task<bool> NotificationsNotEnabled()
        {
            return await LocalNotificationCenter.Current.AreNotificationsEnabled() == false;
        }

        private async Task RequestNotificationPermission()
        {
            await LocalNotificationCenter.Current.RequestNotificationPermission();
        }

        public void HandleCourseNotification(Course course, DateTime currentTime, List<NotificationRequest> requests, List<int> cancelledRequests)
        {
            HandleStartNotification(course.CourseId, course.CourseName, course.Start, course.StartNotification, currentTime, requests, cancelledRequests, 1000);
            HandleEndNotification(course.CourseId, course.CourseName, course.End, course.EndNotification, currentTime, requests, cancelledRequests, 2000);
        }

        private void HandleAssessmentNotification(Assessment assessment, DateTime currentTime, List<NotificationRequest> requests, List<int> cancelledRequests)
        {
            HandleStartNotification(assessment.AssessmentId, assessment.AssessmentName, assessment.Start, assessment.StartNotification, currentTime, requests, cancelledRequests, 3000);
            HandleEndNotification(assessment.AssessmentId, assessment.AssessmentName, assessment.End, assessment.EndNotification, currentTime, requests, cancelledRequests, 4000);
        }

        public async void HandleStartNotification(int id, string name, DateTime startDate, int startNotif, DateTime currentTime, List<NotificationRequest> requests, List<int> cancelledRequests, int notificationOffset)
        {
            if (startNotif == 0)
            {
                cancelledRequests.Add(id + notificationOffset);
            }
            else
            {
                // Add the notification request
                requests.Add(CreateNotificationRequest(id + notificationOffset, $"{name} Starting soon", $"{name} is starting soon", startDate, startNotif, currentTime));

                // Check if the current date matches the notification trigger date
                if (currentTime.Date == startDate.AddDays(-startNotif).Date)
                {
                    // Show alert only if it's the correct day for the notification
                    await Application.Current.MainPage.DisplayAlert(
                        $"{name} Starting soon",
                        $"{name} is starting soon",
                        "OK");
                }
            }
        }

        public async void HandleEndNotification(int id, string name, DateTime endDate, int endNotif, DateTime currentTime, List<NotificationRequest> requests, List<int> cancelledRequests, int notificationOffset)
        {
            if (endNotif == 0)
            {
                cancelledRequests.Add(id + notificationOffset);
            }
            else
            {
                requests.Add(CreateNotificationRequest(id + notificationOffset, $"{name} Ending soon", $"{name} is ending soon", endDate, endNotif, currentTime));

                // Check if the current date matches the notification trigger date
                if (currentTime.Date == endDate.AddDays(-endNotif).Date)
                {
                    // Show alert only if it's the correct day for the notification
                    await Application.Current.MainPage.DisplayAlert(
                        $"{name} Ending soon",
                        $"{name} is ending soon",
                        "OK");
                }
            }
        }

        public NotificationRequest CreateNotificationRequest(int notificationId, string title, string description, DateTime targetDate, int notifyDaysBefore, DateTime currentTime)
        {
            return new NotificationRequest
            {
                NotificationId = notificationId,
                Title = title,
                Description = description,
                Schedule = new NotificationRequestSchedule
                {
                    NotifyTime = targetDate.AddDays(-notifyDaysBefore).AddHours(currentTime.Hour).AddMinutes(currentTime.Minute),
                    RepeatType = NotificationRepeat.Daily
                }
            };
        }

        public static void CancelNotifications(List<int> cancelledRequests)
        {
            foreach (int notificationId in cancelledRequests)
            {
                var notification = notificationRequestsStatic.FirstOrDefault(n => n.NotificationId == notificationId);
                notification?.Cancel();
            }
        }

        public static async Task ShowNotifications(List<NotificationRequest> requests)
        {
            foreach (NotificationRequest request in requests)
            {
                await LocalNotificationCenter.Current.Show(request);
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            RefreshUIAndNotify();
        }

        private void RefreshUIAndNotify()
        {
            Load_Interface(termSelected.TermId);
            NotifyPrompt();
        }

        public void AddDummyData()
        {
            // Ensure the directory for the database exists
            string folderPath = Path.GetDirectoryName(databasePath);

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // Delete the existing database file only if it exists
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }

            // Create the tables after the file has been deleted
            DB.Generate_Tbl();

            using (var db = new SQLiteConnection(databasePath))
            {
                // Add terms
                var terms = new List<Term>
                {
                    new Term("Term 1", DateTime.Now, DateTime.Now.AddMonths(6)),
                    new Term("Term 2", DateTime.Now, DateTime.Now.AddMonths(6))
                };

                foreach (var term in terms)
                {
                    DB.Add_Term(db, term);
                }

                // Add courses for Term 1
                var term1Courses = new List<Course>
                {
                    new Course(1, 1, "Python Programming", DateTime.Now, DateTime.Now.AddMonths(4), "In Progress", "Enter Course Details Here:", 1, 2),
                    new Course(1, 1, "Software 1", DateTime.Now, DateTime.Now.AddMonths(4), "In Progress", "Enter Course Details Here:", 3, 4),
                    new Course(1, 1, "Software 2", DateTime.Now, DateTime.Now.AddMonths(4), "In Progress", "Enter Course Details Here:", 1, 1),
                    new Course(1, 1, "Mobile App Development", DateTime.Now, DateTime.Now.AddMonths(4), "In Progress", "Enter Course Details Here:", 1, 1),
                    new Course(1, 1, "Data Foundations", DateTime.Now, DateTime.Now.AddMonths(4), "In Progress", "Enter Course Details Here:", 1, 1),
                    new Course(1, 1, "Data Applications", DateTime.Now, DateTime.Now.AddMonths(4), "In Progress", "Enter Course Details Here:", 1, 1)
                };

                foreach (var course in term1Courses)
                {
                    // Create a new instructor for each course
                    var instructor = new Instructor("Anika Patel", "555-123-4567", "anika.patel@strimeuniversity.edu");
                    DB.Add_Instructor(db, instructor);

                    // Assign the new InstructorId to the course
                    course.InstructorId = instructor.InstructorId;

                    // Add course with the unique instructor to the database
                    DB.Add_Course(db, course);
                }

                // Add courses for Term 2
                var term2Courses = new List<Course>
                {
                    new Course(2, 1, "Advanced Data", DateTime.Now, DateTime.Now.AddMonths(4), "Plan to Take", "Enter Course Details Here:", 1, 2),
                    new Course(2, 1, "C++ Programming", DateTime.Now, DateTime.Now.AddMonths(4), "Plan to Take", "Enter Course Details Here:", 1, 2),
                    new Course(2, 1, "Cloud Foundations", DateTime.Now, DateTime.Now.AddMonths(4), "Plan to Take", "Enter Course Details Here:", 1, 2),
                    new Course(2, 1, "IT Project Management", DateTime.Now, DateTime.Now.AddMonths(4), "Plan to Take", "Enter Course Details Here:", 1, 2),
                    new Course(2, 1, "Networking Fundamentals", DateTime.Now, DateTime.Now.AddMonths(4), "Plan to Take", "Enter Course Details Here:", 1, 2),
                    new Course(2, 1, "Programming Capstone", DateTime.Now, DateTime.Now.AddMonths(4), "Plan to Take", "Enter Course Details Here:", 1, 2)
                };

                foreach (var course in term2Courses)
                {
                    // Create a new instructor for each course
                    var instructor = new Instructor("Anika Patel", "555-123-4567", "anika.patel@strimeuniversity.edu");
                    DB.Add_Instructor(db, instructor);

                    // Assign the new InstructorId to the course
                    course.InstructorId = instructor.InstructorId;

                    // Add course with the unique instructor to the database
                    DB.Add_Course(db, course);
                }

                // Add assessments for courses
                var assessments = new List<Assessment>
                {
                    new Assessment(1, "Performance Assessment #1", DateTime.Now, DateTime.Now.AddMonths(3), "Enter details about assessment here:", 1),
                    new Assessment(0, "Objective Assessment #1", DateTime.Now, DateTime.Now.AddMonths(3), "Enter details about assessment here:", 1),
                    new Assessment(1, "Performance Assessment #1", DateTime.Now, DateTime.Now.AddMonths(3), "Enter details about assessment here:", 2),
                    new Assessment(0, "Objective Assessment #1", DateTime.Now, DateTime.Now.AddMonths(3), "Enter details about assessment here:", 2),
                    new Assessment(1, "Performance Assessment #1", DateTime.Now, DateTime.Now.AddMonths(3), "Enter details about assessment here:", 3),
                    new Assessment(0, "Objective Assessment #1", DateTime.Now, DateTime.Now.AddMonths(3), "Enter details about assessment here:", 3),
                    new Assessment(1, "Performance Assessment #1", DateTime.Now, DateTime.Now.AddMonths(3), "Enter details about assessment here:", 4),
                    new Assessment(0, "Objective Assessment #1", DateTime.Now, DateTime.Now.AddMonths(3), "Enter details about assessment here:", 4),
                    new Assessment(1, "Performance Assessment #1", DateTime.Now, DateTime.Now.AddMonths(3), "Enter details about assessment here:", 5),
                    new Assessment(0, "Objective Assessment #1", DateTime.Now, DateTime.Now.AddMonths(3), "Enter details about assessment here:", 5),
                    new Assessment(1, "Performance Assessment #1", DateTime.Now, DateTime.Now.AddMonths(3), "Enter details about assessment here:", 6),
                    new Assessment(0, "Objective Assessment #1", DateTime.Now, DateTime.Now.AddMonths(3), "Enter details about assessment here:", 6)
                };

                foreach (var assessment in assessments)
                {
                    DB.Add_Assessment(db, assessment);
                }

                // Add notes
                var notes = new List<Note>
                {
                    new Note(1, "Note 1"),
                    new Note(1, "Note 2")
                };

                foreach (var note in notes)
                {
                    DB.Add_Note(db, note);
                }
            }
        }

        private void Load_Data()
        {
            using (var db = new SQLiteConnection(databasePath))
            {
                // Load Terms
                var loadedTerms = db.Query<Term>("SELECT * FROM Terms");
                terms.AddRange(loadedTerms);

                // Load Courses for each Term
                foreach (var term in terms)
                {
                    var termCourses = db.Query<Course>($"SELECT * FROM Courses WHERE termId={term.TermId}");
                    var courseListForTerm = new List<Course>();

                    foreach (var course in termCourses)
                    {
                        courseListForTerm.Add(course);
                        courseList.Add(course.CourseId, course); // Assuming courseList is the correct Dictionary<int, Course>
                    }

                    courses.Add(term, courseListForTerm);
                }

                // Load Assessments
                var loadedAssessments = db.Query<Assessment>("SELECT * FROM Assessments");
                foreach (var assessment in loadedAssessments)
                {
                    assessments.Add(assessment.AssessmentId, assessment);
                }

                // Load Instructors
                var loadedInstructors = db.Query<Instructor>("SELECT * FROM Instructors");
                foreach (var instructor in loadedInstructors)
                {
                    instructors.Add(instructor.InstructorId, instructor);
                }

                // Load Notes
                var loadedNotes = db.Query<Note>("SELECT * FROM Notes");
                foreach (var note in loadedNotes)
                {
                    notes.Add(note.NoteId, note);
                }
            }
        }

        public static void Sync_DB()
        {
            // Reinitialize collections to ensure fresh data is loaded
            terms.Clear();
            courses.Clear();
            courseList.Clear();
            assessments.Clear();
            notes.Clear();

            using (var db = new SQLiteConnection(databasePath))
            {
                // Load Terms
                var loadedTerms = db.Query<Term>("SELECT * FROM Terms");
                terms.AddRange(loadedTerms);

                // Load Courses for each Term
                foreach (var term in terms)
                {
                    var termCourses = db.Query<Course>($"SELECT * FROM Courses WHERE termId={term.TermId}");
                    var courseListForTerm = new List<Course>();

                    foreach (var course in termCourses)
                    {
                        courseListForTerm.Add(course);
                        courseList.Add(course.CourseId, course); // Add course to courseList dictionary
                    }

                    courses.Add(term, courseListForTerm); // Add list of courses to courses dictionary for each term
                }

                // Load Assessments
                var loadedAssessments = db.Query<Assessment>("SELECT * FROM Assessments");
                foreach (var assessment in loadedAssessments)
                {
                    assessments.Add(assessment.AssessmentId, assessment);
                }

                // Load Notes
                var loadedNotes = db.Query<Note>("SELECT * FROM Notes");
                foreach (var note in loadedNotes)
                {
                    notes.Add(note.NoteId, note);
                }
            }
        }

        public static void Sync_CurrentCourse(int courseId)
        {
            using (var db = new SQLiteConnection(databasePath))
            {
                // Retrieve the specific course data and update it in `courseList`
                var updatedCourse = db.Find<Course>(courseId);
                if (updatedCourse != null)
                {
                    courseList[courseId] = updatedCourse;
                }

                // Retrieve the associated instructor for this course
                var updatedInstructor = db.Find<Instructor>(updatedCourse.InstructorId);
                if (updatedInstructor != null)
                {
                    instructors[updatedCourse.InstructorId] = updatedInstructor;
                }
            }
        }

        public void Load_Interface(int termId)
        {
            // Ensure terms are available
            if (terms == null || terms.Count == 0)
            {
                DisplayAlert("No terms available", "There are currently no terms to display.", "OK");
                return;
            }

            // Find the term by TermId safely
            termSelected = terms.FirstOrDefault(t => t.TermId == termId);
            if (termSelected == null)
            {
                DisplayAlert("Invalid Term", "The selected term ID is not valid.", "OK");
                return;
            }

            // Clear existing children from the term and course stacks
            termStack.Children.Clear();
            courseStack.Children.Clear();

            // Add buttons for each term in the terms list
            foreach (var tmpTerm in terms)
            {
                var termButton = new Button
                {
                    Text = tmpTerm.TermName,
                    Padding = 5,
                    BackgroundColor = Microsoft.Maui.Graphics.Colors.LightSkyBlue,
                    TextColor = Microsoft.Maui.Graphics.Colors.Black,
                    CornerRadius = 5
                };

                // Set up click event to reload the UI with the selected term
                termButton.Clicked += (sender, args) => Load_Interface(tmpTerm.TermId);

                // Add the term button to the term stack
                termStack.Children.Add(termButton);
            }

            // Add a button for adding a new term
            var addTermButton = new Button
            {
                Text = "Add Term",
                Padding = 5,
                BackgroundColor = Microsoft.Maui.Graphics.Colors.LightSkyBlue,
                TextColor = Microsoft.Maui.Graphics.Colors.Black,
                CornerRadius = 5
            };
            addTermButton.Clicked += (sender, args) => onNewTerm();
            termStack.Children.Add(addTermButton);

            // Add a course button for each course in the selected term
            if (courses.ContainsKey(termSelected))
            {
                foreach (var course in courses[termSelected])
                {
                    var courseGrid = new Grid
                    {
                        BackgroundColor = Colors.White
                    };

                    var courseButton = new Button
                    {
                        Text = course.CourseName
                    };
                    courseGrid.Add(courseButton);

                    var deleteSwipeItem = new SwipeItem
                    {
                        Text = "Delete",
                        BindingContext = course,
                        BackgroundColor = Colors.LightSkyBlue
                    };
                    deleteSwipeItem.Invoked += onDeleteInvoked;

                    var swipeView = new SwipeView
                    {
                        RightItems = new SwipeItems { deleteSwipeItem },
                        Content = courseGrid
                    };

                    // Set up click event to navigate to the ClassView for the course
                    courseButton.Clicked += async (sender, args) => await Navigation.PushAsync(new ClassView(course.CourseId));

                    // Add the swipe view with the course button to the course stack
                    courseStack.Children.Add(swipeView);
                }

                // Add a button for adding new courses if there are fewer than 6 courses in the selected term
                if (courses[termSelected].Count < 6)
                {
                    var addCourseButton = new Button
                    {
                        Text = "Add Course"
                    };
                    addCourseButton.Clicked += (sender, args) => onNewCourse();
                    courseStack.Children.Add(addCourseButton);
                }

                // Add a delete button for the term if no courses exist
                if (courses[termSelected].Count == 0)
                {
                    var deleteTermButton = new Button
                    {
                        Text = "Delete Term",
                        BackgroundColor = Colors.Red
                    };
                    deleteTermButton.Clicked += (sender, args) => onTermDelete();
                    courseStack.Children.Add(deleteTermButton);
                }
            }

            // Set the selected term's start and end dates and title
            termStart.Date = termSelected.Start;
            termEnd.Date = termSelected.End;
            termTitle.Text = termSelected.TermName;
        }

        public void onNewCourse()
        {
            // Add a new course to the selected term using the term ID
            DB.AddNewCourse(termSelected.TermId);

            // Reload the UI with the updated course list for the selected term
            Load_Interface(termSelected.TermId);
        }

        public void onNewTerm()
        {
            DB.Add_NewTerm();
            Load_Interface(termSelected.TermId);
        }

        public void onTermDelete()
        {
            using (var db = new SQLiteConnection(databasePath))
            {
                db.Delete(termSelected);
                Sync_DB();

                var firstTerm = terms.FirstOrDefault(); // Get the first available term after deletion
                if (firstTerm != null)
                {
                    Load_Interface(firstTerm.TermId);
                }
                else
                {
                    // Clear the UI or handle no available terms
                    // ClearTermUI();
                    DisplayAlert("No terms available", "Please add a new term.", "OK");
                }
            }
        }

        private void onDeleteInvoked(object sender, EventArgs e)
        {
            // Cast the sender as a SwipeItem to access the bound course
            if (sender is SwipeItem swipeItem && swipeItem.BindingContext is Course selectedCourse)
            {
                // Delete the selected course from the database
                DB.DeleteCourse(selectedCourse);

                // Synchronize the database and update the UI
                MainPage.Sync_DB();
                Load_Interface(termSelected.TermId);
            }
        }

        public static bool CheckDates(DateTime startDate, DateTime endDate)
        {
            // Return true if end date is greater than or equal to start date, otherwise false
            return endDate >= startDate;
        }

        private void TermTitleChange(object sender, TextChangedEventArgs e)
        {
            // Ensure that the new text value is not null
            if (!string.IsNullOrEmpty(e.NewTextValue))
            {
                // Update the selected term's name with the new text value
                termSelected.TermName = e.NewTextValue;

                // Update the term in the database
                using (var db = new SQLiteConnection(databasePath))
                {
                    DB.Update_Term(db, termSelected);
                }

                // Refresh the UI to reflect the changes
                Load_Interface(termSelected.TermId);
            }
        }


        private void TermEnd_DateSelect(object sender, DateChangedEventArgs e)
        {
            // Check if the new end date is valid compared to the start date
            if (CheckDates(termStart.Date, e.NewDate))
            {
                // Update the selected term's end date with the new date
                termEnd.Date = e.NewDate;
                termSelected.End = e.NewDate;

                // Update the term in the database
                using (var db = new SQLiteConnection(databasePath))
                {
                    DB.Update_Term(db, termSelected);
                }
            }
        }

        private void TermStart_DateSelect(object sender, DateChangedEventArgs e)
        {
            // Check if the new start date is valid compared to the end date
            if (CheckDates(e.NewDate, termEnd.Date))
            {
                // Update the selected term's start date with the new date
                termStart.Date = e.NewDate;
                termSelected.Start = e.NewDate;

                // Update the term in the database
                using (var db = new SQLiteConnection(databasePath))
                {
                    DB.Update_Term(db, termSelected);
                }
            }
        }
    }

}
