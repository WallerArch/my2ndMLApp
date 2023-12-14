using System.Data.SQLite;
using MyMLApp; // Hämtar in min MLs namespace så den kan användas till bedömningar

class RestaurantReview
{
    public int Id { get; set; } // Id för varje recension som också skall vara primär-nyckel i databasen
    public string Review { get; set; }
    public string Author { get; set; }
    public string Restaurant { get; set; }
    public string Sentiment { get; set; }
}

class Program
{
    //Lista som alla recenserioner hanteras i
    static List<RestaurantReview> restaurantReviews = new List<RestaurantReview>();
    //Variabel för databasen
    static string connectionString = "Data Source=RestaurantReviews.db;Version=3;";
    
    //Funktion som startar databasen
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
                "CREATE TABLE IF NOT EXISTS Reviews (" + // Detta körs endast som tabellen inte redan finns
                "Id INTEGER PRIMARY KEY AUTOINCREMENT," + // Primärnyckel som automatiskt för räknar upp för varje ny som skapas
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
        //Startar databasen
        InitializeDatabase();

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

            //Skapar variabel för valet samt konverterar den input-strängen som en integer
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
                    SaveReviews(); //När programmet stängs ned sparas aktiv lista till databasen
                    programRuns = false;
                    break;
                default:
                    break;
            }
        }
    }

    //Funktion som sparar alla recensionerna till databasen
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

    //Funktion som läser ut alla recensionerna från databasen
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
                        Console.WriteLine("\nNo reviews available."); //Om databasen är tom skrivs detta ut
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
    
    //Funktion som hämtar ut recensioner för en specifik restaurang
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
                        Console.WriteLine("\nNo restaurants available."); //Felmeddelande om inga recensioner finns än
                    }
                    else
                    {
                        //Lista som hanterar restaurangerna
                        List<string> uniqueRestaurants = new List<string>();

                        while (reader.Read())
                        {
                            //Lägger till vardera restaurang som finns i databasen till listan
                            uniqueRestaurants.Add(reader["Restaurant"].ToString());
                        }

                        // Skriver ut numrerad lista med alla restauranger som recenserats
                        for (int i = 0; i < uniqueRestaurants.Count; i++)
                        {
                            Console.WriteLine($"{i + 1}. {uniqueRestaurants[i]}");
                        }

                        // Användaren får här välja en av restaurangerna
                        Console.Write("\nEnter the number of the restaurant to view reviews (or press 'B' to go back): ");
                        string userInput = Console.ReadLine();

                        //Läser input som integer
                        if (int.TryParse(userInput, out int choice) && choice >= 1 && choice <= uniqueRestaurants.Count)
                        {
                            //Eftersom listan räknar som array måste vi köra -1 så det blir rätt val
                            string chosenRestaurant = uniqueRestaurants[choice - 1];

                            // Hämtar ut restaurangens recenserioner
                            string reviewsQuery = $"SELECT * FROM Reviews WHERE Restaurant = @Restaurant";
                            using (SQLiteCommand reviewsCommand = new SQLiteCommand(reviewsQuery, connection))
                            {
                                reviewsCommand.Parameters.AddWithValue("@Restaurant", chosenRestaurant); //Anger just den valda restaurangen till SQL-frågan

                                using (SQLiteDataReader reviewsReader = reviewsCommand.ExecuteReader())
                                {
                                    Console.Clear();
                                    Console.WriteLine($"Reviews for {chosenRestaurant}:");

                                    while (reviewsReader.Read())
                                    {
                                        //Skriver ut recensioner för vald restaurang
                                        Console.WriteLine($"{reviewsReader["Author"]} says: {reviewsReader["Review"]} - Sentiment: {reviewsReader["Sentiment"]}");
                                    }
                                }
                            }
                        }
                        else if (userInput.Equals("B", StringComparison.OrdinalIgnoreCase)) //Om användaren anger B skickas den bakåt i programmet
                        {
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

    //Funktion för att hämta ut specifik skribents recensioner
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
                        Console.WriteLine("\nNo authors available."); //Felmeddelande om ingen recension skrivits än
                    }
                    else
                    {
                        List<string> uniqueAuthors = new List<string>(); //Lista med alla skribenter

                        while (reader.Read())
                        {
                            uniqueAuthors.Add(reader["Author"].ToString()); //Lägger till skribenterna i listan
                        }

                        // Skriver ut lista med alla skribenter
                        for (int i = 0; i < uniqueAuthors.Count; i++)
                        {
                            Console.WriteLine($"{i + 1}. {uniqueAuthors[i]}");
                        }

                        Console.Write("\nEnter the number of the author to view reviews (or press 'B' to go back): ");
                        string userInput = Console.ReadLine();

                        if (int.TryParse(userInput, out int choice) && choice >= 1 && choice <= uniqueAuthors.Count)
                        {
                            string chosenAuthor = uniqueAuthors[choice - 1];

                            // Hämtar ut skribentens recensioner
                            string reviewsQuery = $"SELECT * FROM Reviews WHERE Author = @Author";
                            using (SQLiteCommand reviewsCommand = new SQLiteCommand(reviewsQuery, connection))
                            {
                                reviewsCommand.Parameters.AddWithValue("@Author", chosenAuthor); // Anger just den valda skribenten till SQL-frågan

                                using (SQLiteDataReader reviewsReader = reviewsCommand.ExecuteReader())
                                {
                                    Console.Clear();
                                    Console.WriteLine($"Reviews by {chosenAuthor}:");

                                    while (reviewsReader.Read()) // Skriver ut skribentens recensioner
                                    {
                                        Console.WriteLine($"{reviewsReader["Author"]} was at {reviewsReader["Restaurant"]} and says: {reviewsReader["Review"]} - Sentiment: {reviewsReader["Sentiment"]}");
                                    }
                                }
                            }
                        }
                        else if (userInput.Equals("B", StringComparison.OrdinalIgnoreCase))
                        {
                            return; //Om användaren anger B skickas den bakåt i programmet
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

    // Funktion för att skriva en ny recension
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

            var result = SentimentModel.Predict(sampleData); // Skickar skribentens recension till min ML för bedömning
            var sentiment = result.PredictedLabel == 1 ? "Positive" : "Negative"; // Om det är positivt blir svaret "Positive", annars "Negative"

            Console.WriteLine($"\nSentiment Analysis Result: {sentiment}"); // Skriver ut resultatet till skärmen

            Console.WriteLine("Your name:");
            string userName = Console.ReadLine();

            Console.WriteLine("Restaurant you visited:");
            string reviewedRestaurant = Console.ReadLine();

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                // Skickar in det skribenten skrev till databasen
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

    // Funktion för att ta bort en vald recension
    static void DeleteReview()
    {
        using (SQLiteConnection connection = new SQLiteConnection(connectionString))
        {
            connection.Open();

            // Hämtar ut alla recensioner från databasen
            string selectQuery = "SELECT * FROM Reviews";
            using (SQLiteCommand selectCommand = new SQLiteCommand(selectQuery, connection))
            {
                using (SQLiteDataReader reader = selectCommand.ExecuteReader())
                {
                    if (!reader.HasRows)
                    {
                        // Om databasen är tom skrivs detta ut
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

                List<RestaurantReview> reviewsFromDatabase = new List<RestaurantReview>(); // Lista för att hantera recensionerna
                using (SQLiteCommand selectCommand = new SQLiteCommand(selectQuery, connection))
                {
                    using (SQLiteDataReader reader = selectCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            reviewsFromDatabase.Add(new RestaurantReview // Lägger alla recensioner i listan
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
                    // Skriver ut alla recensionerna till en numrerad lista
                    Console.WriteLine($"{i + 1}. {reviewsFromDatabase[i].Restaurant}, {reviewsFromDatabase[i].Review} - By: {reviewsFromDatabase[i].Author}");
                }

                Console.Write("Enter the number of the review you want to delete:\n");

                // Konverterar input från användaren till integer och kollar så att den är minst 1 och som högst den största lagrade id-siffran i databasen
                if (int.TryParse(Console.ReadLine(), out int reviewNumber) && reviewNumber >= 1 && reviewNumber <= reviewsFromDatabase.Count)
                {
                    int reviewIdToDelete = reviewsFromDatabase[reviewNumber - 1].Id; // Eftersom listan räknar som en array måste vi lägga till -1 för att få rätt val från användaren

                    string deleteQuery = "DELETE FROM Reviews WHERE Id = @ReviewId";
                    using (SQLiteCommand deleteCommand = new SQLiteCommand(deleteQuery, connection))
                    {
                        // Utför SQL frågan med valt recensions-id att radera från databasen
                        deleteCommand.Parameters.AddWithValue("@ReviewId", reviewIdToDelete);
                        deleteCommand.ExecuteNonQuery();
                    }

                    Console.WriteLine("\nReview deleted successfully.\n");
                }
                else
                {
                    // Felmeddelande om id-t inte finns i databasen
                    Console.WriteLine("Invalid choice. Please enter a valid number.\n");
                }

                Console.WriteLine("Press any key to enter a number again or press backspace to return to the main menu.\n");
            } while (Console.ReadKey(true).Key != ConsoleKey.Backspace);
        }
    }
}