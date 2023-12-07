using System.Text.Json;
using System.Collections.Generic;
using System.Data.SQLite;
using MyMLApp; // Make sure to reference your ML model namespace

class RestaurantReview
{
    public int Id { get; set; } // SQLite requires an auto-incrementing integer as the primary key
    public string Review { get; set; }
    public string Author { get; set; }
    public string Restaurant { get; set; }
    public string Sentiment { get; set; }
}

class Program
{
    static List<RestaurantReview> restaurantReviews = new List<RestaurantReview>();
    static string connectionString = "Data Source=RestaurantReviews.db;Version=3;";
    static void InitializeDatabase()
    {
        // Skapar databasen om den inte finns
        if (!File.Exists("RestaurantReviews.db"))
        {
            SQLiteConnection.CreateFile("RestaurantReviews.db");
        }

        using (SQLiteConnection connection = new SQLiteConnection(connectionString))
        {
            // Öppnar koppling mot databasen
            connection.Open();

            // Skapar tabellen i databasen för recensionerna
            using (SQLiteCommand command = new SQLiteCommand(
                "CREATE TABLE IF NOT EXISTS Reviews (" +
                "Id INTEGER PRIMARY KEY AUTOINCREMENT," +
                "Review TEXT NOT NULL," +
                "Author TEXT NOT NULL," +
                "Restaurant TEXT NOT NULL," +
                "Sentiment TEXT NOT NULL)",
                connection))
            {
                command.ExecuteNonQuery();
            }
        }
    }
    static void Main()
    {
        InitializeDatabase();
        LoadReviews();

        bool programRuns = true;

        while (programRuns)
        {
            Console.Clear();
            Console.WriteLine("Restaurant Review System:");
            Console.WriteLine("1. Read all reviews");
            Console.WriteLine("2. Read reviews by restaurant");
            Console.WriteLine("3. Read reviews by chosen author");
            Console.WriteLine("4. Write a new review");
            Console.WriteLine("5. Delete a review");
            Console.WriteLine("6. Exit");

            int choice;
            int.TryParse(Console.ReadLine(), out choice);

            switch (choice)
            {
                case 1:
                    ReadReviews();
                    break;
                case 2:
                    ReadReviewsByRestaurant();
                    break;
                case 3:
                    ReadReviewsByAuthor();
                    break;
                case 4:
                    WriteReview();
                    break;
                case 5:
                    DeleteReview();
                    break;
                case 6:
                    SaveReviews();
                    programRuns = false;
                    break;
                default:
                    break;
            }
        }
    }

    static void LoadReviews()
    {
        using (SQLiteConnection connection = new SQLiteConnection(connectionString))
        {
            connection.Open();

            string query = "SELECT * FROM Reviews";
            using (SQLiteCommand command = new SQLiteCommand(query, connection))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        RestaurantReview review = new RestaurantReview
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Review = Convert.ToString(reader["Review"]),
                            Author = Convert.ToString(reader["Author"]),
                            Restaurant = Convert.ToString(reader["Restaurant"]),
                            Sentiment = Convert.ToString(reader["Sentiment"])
                        };

                        restaurantReviews.Add(review);
                    }
                }
            }
        }

    }

    static void SaveReviews()
    {
        using (SQLiteConnection connection = new SQLiteConnection(connectionString))
        {
            connection.Open();

            using (var transaction = connection.BeginTransaction())
            {
                foreach (var review in restaurantReviews)
                {
                    using (SQLiteCommand insertCommand = new SQLiteCommand(
                        "INSERT OR REPLACE INTO Reviews (Id, Review, Author, Restaurant, Sentiment) VALUES (@Id, @Review, @Author, @Restaurant, @Sentiment)",
                        connection, transaction))
                    {
                        insertCommand.Parameters.AddWithValue("@Id", review.Id);
                        insertCommand.Parameters.AddWithValue("@Review", review.Review);
                        insertCommand.Parameters.AddWithValue("@Author", review.Author);
                        insertCommand.Parameters.AddWithValue("@Restaurant", review.Restaurant);
                        insertCommand.Parameters.AddWithValue("@Sentiment", review.Sentiment);

                        insertCommand.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
            }
        }
    }



    static void ReadReviews()
    {
        using (SQLiteConnection connection = new SQLiteConnection(connectionString))
        {
            connection.Open();

            string query = "SELECT * FROM Reviews";
            using (SQLiteCommand command = new SQLiteCommand(query, connection))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    Console.Clear();
                    Console.WriteLine("Restaurant Reviews:");

                    if (!reader.HasRows)
                    {
                        Console.WriteLine("\nNo reviews available.");
                    }
                    else
                    {
                        while (reader.Read())
                        {
                            Console.WriteLine($"{reader["Author"]} was at {reader["Restaurant"]} and says: {reader["Review"]} - Sentiment: {reader["Sentiment"]}");
                        }
                    }
                }
            }
        }

        Console.WriteLine("\nPress any key to return.");
        Console.ReadKey();
    }

    static void ReadReviewsByRestaurant()
    {
        using (SQLiteConnection connection = new SQLiteConnection(connectionString))
        {
            connection.Open();

            string query = "SELECT DISTINCT Restaurant FROM Reviews";
            using (SQLiteCommand command = new SQLiteCommand(query, connection))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    Console.Clear();
                    Console.WriteLine("Choose a restaurant to view reviews:");

                    if (!reader.HasRows)
                    {
                        Console.WriteLine("\nNo restaurants available.");
                    }
                    else
                    {
                        List<string> uniqueRestaurants = new List<string>();

                        while (reader.Read())
                        {
                            uniqueRestaurants.Add(reader["Restaurant"].ToString());
                        }

                        // Display numbered list of unique restaurants
                        for (int i = 0; i < uniqueRestaurants.Count; i++)
                        {
                            Console.WriteLine($"{i + 1}. {uniqueRestaurants[i]}");
                        }

                        // Prompt user to choose a restaurant
                        Console.Write("\nEnter the number of the restaurant to view reviews (or press 'B' to go back): ");
                        string userInput = Console.ReadLine();

                        if (int.TryParse(userInput, out int choice) && choice >= 1 && choice <= uniqueRestaurants.Count)
                        {
                            string chosenRestaurant = uniqueRestaurants[choice - 1];

                            // Get reviews for the chosen restaurant
                            string reviewsQuery = $"SELECT * FROM Reviews WHERE Restaurant = @Restaurant";
                            using (SQLiteCommand reviewsCommand = new SQLiteCommand(reviewsQuery, connection))
                            {
                                reviewsCommand.Parameters.AddWithValue("@Restaurant", chosenRestaurant);

                                using (SQLiteDataReader reviewsReader = reviewsCommand.ExecuteReader())
                                {
                                    Console.Clear();
                                    Console.WriteLine($"Reviews for {chosenRestaurant}:");

                                    while (reviewsReader.Read())
                                    {
                                        Console.WriteLine($"{reviewsReader["Author"]} says: {reviewsReader["Review"]} - Sentiment: {reviewsReader["Sentiment"]}");
                                    }
                                }
                            }
                        }
                        else if (userInput.Equals("B", StringComparison.OrdinalIgnoreCase))
                        {
                            // User chose to go back
                            return;
                        }
                        else
                        {
                            Console.WriteLine("Invalid choice. Please enter a valid number.");
                        }
                    }
                }
            }
        }

        Console.WriteLine("\nPress any key to return.");
        Console.ReadKey();
    }

    static void ReadReviewsByAuthor()
    {
        using (SQLiteConnection connection = new SQLiteConnection(connectionString))
        {
            connection.Open();

            string query = "SELECT DISTINCT Author FROM Reviews";
            using (SQLiteCommand command = new SQLiteCommand(query, connection))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    Console.Clear();
                    Console.WriteLine("Choose an author to view reviews:");

                    if (!reader.HasRows)
                    {
                        Console.WriteLine("\nNo authors available.");
                    }
                    else
                    {
                        List<string> uniqueAuthors = new List<string>();

                        while (reader.Read())
                        {
                            uniqueAuthors.Add(reader["Author"].ToString());
                        }

                        // Display numbered list of unique authors
                        for (int i = 0; i < uniqueAuthors.Count; i++)
                        {
                            Console.WriteLine($"{i + 1}. {uniqueAuthors[i]}");
                        }

                        // Prompt user to choose an author
                        Console.Write("\nEnter the number of the author to view reviews (or press 'B' to go back): ");
                        string userInput = Console.ReadLine();

                        if (int.TryParse(userInput, out int choice) && choice >= 1 && choice <= uniqueAuthors.Count)
                        {
                            string chosenAuthor = uniqueAuthors[choice - 1];

                            // Get reviews for the chosen author
                            string reviewsQuery = $"SELECT * FROM Reviews WHERE Author = @Author";
                            using (SQLiteCommand reviewsCommand = new SQLiteCommand(reviewsQuery, connection))
                            {
                                reviewsCommand.Parameters.AddWithValue("@Author", chosenAuthor);

                                using (SQLiteDataReader reviewsReader = reviewsCommand.ExecuteReader())
                                {
                                    Console.Clear();
                                    Console.WriteLine($"Reviews by {chosenAuthor}:");

                                    while (reviewsReader.Read())
                                    {
                                        Console.WriteLine($"{reviewsReader["Author"]} was at {reviewsReader["Restaurant"]} and says: {reviewsReader["Review"]} - Sentiment: {reviewsReader["Sentiment"]}");
                                    }
                                }
                            }
                        }
                        else if (userInput.Equals("B", StringComparison.OrdinalIgnoreCase))
                        {
                            // User chose to go back
                            return;
                        }
                        else
                        {
                            Console.WriteLine("Invalid choice. Please enter a valid number.");
                        }
                    }
                }
            }
        }

        Console.WriteLine("\nPress any key to return.");
        Console.ReadKey();
    }

    static void WriteReview()
    {
        Console.Clear();

        Console.WriteLine("Write a new restaurant review:");

        Console.WriteLine("Your review:");
        string userReview = Console.ReadLine();

        if (!string.IsNullOrWhiteSpace(userReview))
        {
            var sampleData = new SentimentModel.ModelInput()
            {
                Col0 = userReview
            };

            var result = SentimentModel.Predict(sampleData);
            var sentiment = result.PredictedLabel == 1 ? "Positive" : "Negative";

            Console.WriteLine($"\nSentiment Analysis Result: {sentiment}");

            Console.WriteLine("Your name:");
            string userName = Console.ReadLine();

            Console.WriteLine("Restaurant you visited:");
            string reviewedRestaurant = Console.ReadLine();

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string insertQuery = "INSERT INTO Reviews (Review, Author, Restaurant, Sentiment) VALUES (@Review, @Author, @Restaurant, @Sentiment)";
                using (SQLiteCommand insertCommand = new SQLiteCommand(insertQuery, connection))
                {
                    insertCommand.Parameters.AddWithValue("@Review", userReview);
                    insertCommand.Parameters.AddWithValue("@Author", userName);
                    insertCommand.Parameters.AddWithValue("@Restaurant", reviewedRestaurant);
                    insertCommand.Parameters.AddWithValue("@Sentiment", sentiment);

                    insertCommand.ExecuteNonQuery();
                }
            }

            Console.WriteLine("Review added successfully.");
            Console.WriteLine("\nPress any key to write another review or press backspace to return to the main menu.\n");
        }
        else
        {
            Console.WriteLine("Review cannot be empty. Please try again.");
            Console.WriteLine("\nPress any key to write another review or press backspace to return to the main menu.\n");
        }
    }

    static void DeleteReview()
    {
        using (SQLiteConnection connection = new SQLiteConnection(connectionString))
        {
            connection.Open();

            string selectQuery = "SELECT * FROM Reviews";
            using (SQLiteCommand selectCommand = new SQLiteCommand(selectQuery, connection))
            {
                using (SQLiteDataReader reader = selectCommand.ExecuteReader())
                {
                    if (!reader.HasRows)
                    {
                        Console.Clear();
                        Console.WriteLine("No reviews available.\n");
                        Console.WriteLine("Press any key to return.");
                        Console.ReadKey();
                        return;
                    }
                }
            }

            do
            {
                Console.Clear();
                Console.WriteLine("Delete a restaurant review:");

                List<RestaurantReview> reviewsFromDatabase = new List<RestaurantReview>();
                using (SQLiteCommand selectCommand = new SQLiteCommand(selectQuery, connection))
                {
                    using (SQLiteDataReader reader = selectCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            reviewsFromDatabase.Add(new RestaurantReview
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Review = reader["Review"].ToString(),
                                Author = reader["Author"].ToString(),
                                Restaurant = reader["Restaurant"].ToString(),
                                Sentiment = reader["Sentiment"].ToString()
                            });
                        }
                    }
                }

                for (int i = 0; i < reviewsFromDatabase.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {reviewsFromDatabase[i].Restaurant}, {reviewsFromDatabase[i].Review} - By: {reviewsFromDatabase[i].Author}");
                }

                Console.Write("Enter the number of the review you want to delete:\n");

                if (int.TryParse(Console.ReadLine(), out int reviewNumber) && reviewNumber >= 1 && reviewNumber <= reviewsFromDatabase.Count)
                {
                    int reviewIdToDelete = reviewsFromDatabase[reviewNumber - 1].Id;

                    string deleteQuery = "DELETE FROM Reviews WHERE Id = @ReviewId";
                    using (SQLiteCommand deleteCommand = new SQLiteCommand(deleteQuery, connection))
                    {
                        deleteCommand.Parameters.AddWithValue("@ReviewId", reviewIdToDelete);
                        deleteCommand.ExecuteNonQuery();
                    }

                    Console.WriteLine("\nReview deleted successfully.\n");
                }
                else
                {
                    Console.WriteLine("Invalid choice. Please enter a valid number.\n");
                }

                Console.WriteLine("Press any key to enter a number again or press backspace to return to the main menu.\n");
            } while (Console.ReadKey(true).Key != ConsoleKey.Backspace);
        }
    }
}