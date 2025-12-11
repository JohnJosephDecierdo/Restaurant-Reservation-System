#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RestaurantReservationSystem
{
    class Program
    {
        static string usersFile = "users.json";
        static string menuFile = "menu.json";
        static string reservationsFile = "reservations.json";
        static string salesFile = "sales.json";
        static string receiptsDir = "Receipts";
        static JsonSerializerOptions jsonOpts = new JsonSerializerOptions { WriteIndented = true };

        static void Main()
        {
            Directory.CreateDirectory(receiptsDir);

            if (!File.Exists(usersFile) || !File.Exists(menuFile))
            {
                Console.WriteLine("Required files missing. Please ensure 'users.json' and 'menu.json' are present in the program folder.");
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
                return;
            }
            if (!File.Exists(reservationsFile)) File.WriteAllText(reservationsFile, "[]");
            if (!File.Exists(salesFile)) File.WriteAllText(salesFile, "[]");

            Console.OutputEncoding = Encoding.UTF8;

            while (true)
            {
                Console.Clear();

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("╔════════════════════════════════════════╗");
                Console.WriteLine("║                                        ║");
                Console.WriteLine("║             BLUE HARBOR CAFE           ║");
                Console.WriteLine("║                                        ║");
                Console.WriteLine("╚════════════════════════════════════════╝");
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("╔════════════════════════════════════════╗");
                Console.WriteLine("║           MAIN MENU                    ║");
                Console.WriteLine("╚════════════════════════════════════════╝");
                Console.WriteLine("╔════════════════════════════════════════╗");
                Console.WriteLine("║ [1] Login                              ║");
                Console.WriteLine("║ [2] Exit                               ║");
                Console.WriteLine("╚════════════════════════════════════════╝");
                Console.ResetColor();

                Console.WriteLine();
                Console.Write("Select an option: ");
                var c = Console.ReadLine();

                if (c == "1") Login();
                else if (c == "2") break;
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Invalid option! Press any key to try again...");
                    Console.ResetColor();
                    Console.ReadKey();
                }
            }
        }

        static void Login()
        {
            const int maxAttempts = 3;
            int attemptsLeft = maxAttempts;

            while (attemptsLeft > 0)
            {
                Console.Clear();
                Console.WriteLine($"Login (attempts left: {attemptsLeft})");
                Console.Write("Username: ");
                string username = Console.ReadLine()?.Trim() ?? "";

                Console.Write("Password: ");
                var passBuilder = new StringBuilder();
                ConsoleKeyInfo key;
                while ((key = Console.ReadKey(true)).Key != ConsoleKey.Enter)
                {
                    if (key.Key == ConsoleKey.Backspace && passBuilder.Length > 0)
                    {
                        passBuilder.Remove(passBuilder.Length - 1, 1);
                        Console.Write("\b \b");
                    }
                    else if (!char.IsControl(key.KeyChar))
                    {
                        passBuilder.Append(key.KeyChar);
                        Console.Write("*");
                    }
                }
                Console.WriteLine();
                string password = passBuilder.ToString();

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Username and Password cannot be empty! Press any key...");
                    Console.ResetColor();
                    Console.ReadKey();
                    attemptsLeft--;
                    continue;
                }

                var accounts = Load<List<Account>>(usersFile) ?? new List<Account>();
                var acc = accounts.FirstOrDefault(a => a.Username == username && a.Password == password);

                if (acc == null)
                {
                    attemptsLeft--;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Invalid username or password! Press any key...");
                    Console.ResetColor();
                    Console.ReadKey();
                    continue;
                }

                if (acc.Role.Equals("owner", StringComparison.OrdinalIgnoreCase)) OwnerMenu(acc);
                else if (acc.Role.Equals("server", StringComparison.OrdinalIgnoreCase) || acc.Role.Equals("foodserver", StringComparison.OrdinalIgnoreCase)) ServerMenu(acc);
                else if (acc.Role.Equals("cashier", StringComparison.OrdinalIgnoreCase)) CashierMenu(acc);

                return;
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Too many failed attempts. Returning to main menu. Press any key...");
            Console.ResetColor();
            Console.ReadKey();
        }

        static void OwnerMenu(Account acc)
        {
            while (true)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("=== OWNER MENU ===");
                Console.WriteLine("[1] View Sales Report");
                Console.WriteLine("[2] Create Employee Account");
                Console.WriteLine("[3] View Employees");
                Console.WriteLine("[4] Manage Menu");
                Console.WriteLine("[5] Logout");
                Console.Write("Select: ");
                var ch = Console.ReadLine();
                if (ch == "1") ViewSalesReport();
                else if (ch == "2") CreateAccount(acc);
                else if (ch == "3") ViewEmployees();
                else if (ch == "4") ManageMenu();
                else if (ch == "5") break;
            }
        }

        static void ServerMenu(Account acc)
        {
            while (true)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("=== FOOD SERVER MENU ===");
                Console.WriteLine("[1] View Menu");
                Console.WriteLine("[2] Register Reservation + Take Order");
                Console.WriteLine("[3] View Reservations (all)");
                Console.WriteLine("[4] Logout");
                Console.Write("Select: ");
                var ch = Console.ReadLine();
                if (ch == "1") DisplayFullMenu();
                else if (ch == "2") ReservationWithOrder(acc.Username);
                else if (ch == "3") ViewReservations(showAll: true);
                else if (ch == "4") break;
            }
        }

        static void CashierMenu(Account acc)
        {
            while (true)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("=== CASHIER MENU ===");
                Console.WriteLine("[1] View Reservations (unpaid)");
                Console.WriteLine("[2] Process Payment");
                Console.WriteLine("[3] View Reservations (all)");
                Console.WriteLine("[4] Logout");
                Console.Write("Select: ");
                var ch = Console.ReadLine();
                if (ch == "1") ViewReservations(showAll: false);
                else if (ch == "2") ProcessPayment(acc.Username);
                else if (ch == "3") ViewReservations(showAll: true);
                else if (ch == "4") break;
            }
        }

        static void DisplayFullMenu()
        {
            Console.Clear();
            var menu = Load<List<MenuItem>>(menuFile) ?? new List<MenuItem>();
            Console.WriteLine("╔═════════════════════════════════════╗");
            Console.WriteLine("║           FULL MENU                 ║");
            Console.WriteLine("╚═════════════════════════════════════╝");
            int i = 1;
            foreach (var m in menu)
            {
                Console.WriteLine($"[{i}] {m.Category} - {m.Name} - ₱{m.Price:F2}");
                i++;
            }
            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }

        static void ReservationWithOrder(string serverName)
        {
            Console.Clear();
            Console.WriteLine("=== RESERVATION + ORDER ===");
            Console.Write("Customer name: ");
            var cname = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(cname))
            {
                Console.WriteLine("Name required.");
                Console.ReadKey();
                return;
            }

            var date = DateTime.Now.ToString("yyyy-MM-dd");

            Console.Write("Table number: ");
            if (!int.TryParse(Console.ReadLine(), out int tableNo))
            {
                Console.WriteLine("Invalid table.");
                Console.ReadKey();
                return;
            }

            var menu = Load<List<MenuItem>>(menuFile) ?? new List<MenuItem>();
            if (menu.Count == 0)
            {
                Console.WriteLine("Menu is empty. Please add menu items first.");
                Console.ReadKey();
                return;
            }

            var orderItems = new List<OrderLine>();
            string[] categories = { "Main", "Beverage", "Appetizer" };

            foreach (var cat in categories)
            {
                Console.Clear();
                Console.WriteLine($"=== {cat} Menu ===");

                var list = menu
                    .Where(m => !string.IsNullOrWhiteSpace(m.Category) && m.Category.ToLower().Contains(cat.ToLower()))
                    .ToList();

                if (list.Count == 0)
                {
                    Console.WriteLine($"No {cat} items available.\n");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    continue;
                }

                for (int i = 0; i < list.Count; i++)
                    Console.WriteLine($"[{i + 1}] {list[i].Name} - ₱{list[i].Price:F2}");

                while (true)
                {
                    Console.Write($"Select {cat} item number (0 to skip): ");
                    var selInput = Console.ReadLine();
                    if (!int.TryParse(selInput, out int sel))
                        continue;
                    if (sel == 0)
                        break;
                    if (sel < 1 || sel > list.Count)
                        continue;

                    var selectedItem = list[sel - 1];
                    Console.Write($"Enter quantity for {selectedItem.Name}: ");
                    if (!int.TryParse(Console.ReadLine(), out int qty) || qty <= 0)
                        continue;

                    var existing = orderItems.FirstOrDefault(x => x.Item == selectedItem.Name);
                    if (existing != null)
                        existing.Quantity += qty;
                    else
                        orderItems.Add(new OrderLine { Item = selectedItem.Name, Quantity = qty, Price = selectedItem.Price });
                }
            }

            if (orderItems.Count == 0)
            {
                Console.WriteLine("No items ordered. Reservation canceled.");
                Console.ReadKey();
                return;
            }

            double total = orderItems.Sum(x => x.Price * x.Quantity);
            var reservations = Load<List<Reservation>>(reservationsFile) ?? new List<Reservation>();
            int id = reservations.Any() ? reservations.Max(r => r.ReservationID) + 1 : 1;

            var newRes = new Reservation
            {
                ReservationID = id,
                CustomerName = cname ?? "",
                Date = date,
                Time = DateTime.Now.ToString("HH:mm"),
                TableNumber = tableNo,
                Orders = orderItems,
                TotalAmount = total,
                CreatedByServer = serverName,
                IsPaid = false
            };

            reservations.Add(newRes);
            Save(reservationsFile, reservations);

            Console.WriteLine("\nReservation and order saved!");
            Console.WriteLine($"Reservation ID: {id}");
            Console.WriteLine($"Customer: {cname}");
            Console.WriteLine($"Table: {tableNo}");
            Console.WriteLine($"Date: {date}");
            Console.WriteLine("\nOrder Details:");
            foreach (var o in orderItems)
                Console.WriteLine($" - {o.Item} x{o.Quantity} = ₱{o.Price * o.Quantity:F2}");
            Console.WriteLine($"Total: ₱{total:F2}");
            Console.WriteLine("\nPress any key to return...");
            Console.ReadKey();
        }

        static void ViewReservations(bool showAll = true)
        {
            Console.Clear();
            var reservations = Load<List<Reservation>>(reservationsFile) ?? new List<Reservation>();
            var list = showAll ? reservations : reservations.Where(r => !r.IsPaid).ToList();

            if (!list.Any())
            {
                Console.WriteLine("No reservations.");
                Console.ReadKey();
                return;
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("==== RESERVATIONS ====\n");
            Console.ResetColor();

            for (int i = 0; i < list.Count; i++)
            {
                var r = list[i];
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[{i + 1}] ID:{r.ReservationID} | {r.CustomerName} | Table:{r.TableNumber} | {r.Date} {r.Time} | Paid:{r.IsPaid}");
                Console.ResetColor();

                foreach (var oi in r.Orders)
                {
                    Console.WriteLine($"   - {oi.Item} x{oi.Quantity} = ₱{oi.Price * oi.Quantity:F2}");
                }
                Console.WriteLine($"   Total: ₱{r.TotalAmount:F2}\n");
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Do you want to delete a reservation? (y/n): ");
            var choice = Console.ReadLine()?.ToLower();
            Console.ResetColor();

            if (choice == "y")
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Enter the number of the reservation to delete: ");
                var numberInput = Console.ReadLine();
                if (int.TryParse(numberInput, out int number) && number >= 1 && number <= list.Count)
                {
                    var resToDelete = list[number - 1];
                    Console.Write($"Are you sure you want to delete reservation ID {resToDelete.ReservationID}? (y/n): ");
                    var confirm = Console.ReadLine()?.ToLower();
                    if (confirm == "y")
                    {
                        reservations.Remove(resToDelete);
                        Save(reservationsFile, reservations);
                        Console.WriteLine("Reservation deleted successfully.");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.WriteLine("Deletion canceled.");
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Invalid number. No reservation deleted.");
                    Console.ResetColor();
                }
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        static void ProcessPayment(string cashierName)
        {
            Console.Clear();
            var reservations = Load<List<Reservation>>(reservationsFile) ?? new List<Reservation>();
            var unpaid = reservations.Where(r => !r.IsPaid).ToList();
            if (!unpaid.Any()) { Console.WriteLine("No unpaid reservations."); Console.ReadKey(); return; }
            Console.WriteLine("Unpaid reservations:");
            foreach (var r in unpaid) Console.WriteLine($"[{r.ReservationID}] {r.CustomerName} - ₱{r.TotalAmount:F2}");
            Console.Write("Enter Reservation ID to pay: ");
            var idInput = Console.ReadLine();
            if (!int.TryParse(idInput, out int id)) return;
            var res = unpaid.FirstOrDefault(r => r.ReservationID == id);
            if (res == null) { Console.WriteLine("Reservation not found."); Console.ReadKey(); return; }
            double tax = Math.Round(res.TotalAmount * 0.12, 2);
            double grand = res.TotalAmount + tax;
            Console.WriteLine($"Subtotal: ₱{res.TotalAmount:F2}");
            Console.WriteLine($"Tax (12%): ₱{tax:F2}");
            Console.WriteLine($"Grand Total: ₱{grand:F2}");
            Console.Write("Cash received: ");
            var cashInput = Console.ReadLine();
            if (!double.TryParse(cashInput, out double cash)) { Console.WriteLine("Invalid."); Console.ReadKey(); return; }
            if (cash < grand) { Console.WriteLine("Insufficient cash."); Console.ReadKey(); return; }
            double change = cash - grand;
            res.IsPaid = true;
            res.PaidAt = DateTime.Now;
            Save(reservationsFile, reservations);

            var sales = Load<List<Sale>>(salesFile) ?? new List<Sale>();
            int sid = sales.Any() ? sales.Max(s => s.Id) + 1 : 1;
            sales.Add(new Sale { Id = sid, ReservationID = res.ReservationID, Date = DateTime.Now, Amount = grand, CashierName = cashierName });
            Save(salesFile, sales);

            var receipt = BuildReceipt(res, tax, grand, cash, change, cashierName);
            var filename = Path.Combine(receiptsDir, $"receipt_{res.ReservationID}_{DateTime.Now:yyyyMMddHHmmss}.txt");
            File.WriteAllText(filename, receipt, Encoding.UTF8);
            Console.WriteLine("\nReceipt:\n");
            Console.WriteLine(receipt);
            Console.WriteLine($"Receipt saved to {filename}");
            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }

        static string BuildReceipt(Reservation r, double tax, double grand, double cash, double change, string cashier)
        {
            var sb = new StringBuilder();
            sb.AppendLine("****************************************");
            sb.AppendLine("            BLUE HARBOR CAFE");
            sb.AppendLine("****************************************");
            sb.AppendLine("Address: 470 Bayfront Place, Naples, FL");
            sb.AppendLine("Tel: (239) 555-0298");
            sb.AppendLine($"Receipt No.: {r.ReservationID}    Date: {DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine($"Customer: {r.CustomerName}    Table: {r.TableNumber}");
            sb.AppendLine($"Cashier: {cashier}");
            sb.AppendLine("----------------------------------------");
            sb.AppendLine($"Item Name               Qty    Total");
            sb.AppendLine("----------------------------------------");
            foreach (var it in r.Orders)
                sb.AppendLine($"{it.Item,-22} {it.Quantity,3}   ₱{it.Price * it.Quantity,8:F2}");
            sb.AppendLine("----------------------------------------");
            sb.AppendLine($"Subtotal                        ₱{r.TotalAmount,8:F2}");
            sb.AppendLine($"Tax                             ₱{tax,8:F2}");
            sb.AppendLine($"Grand Total                     ₱{grand,8:F2}");
            sb.AppendLine("****************************************");
            sb.AppendLine($"Payment: Cash    Amount: ₱{cash,8:F2}");
            sb.AppendLine($"Change:                          ₱{change,8:F2}");
            sb.AppendLine("****************************************");
            sb.AppendLine("      Thank you for dining with us!");
            sb.AppendLine("****************************************");
            return sb.ToString();
        }

        static void ViewSalesReport()
        {
            Console.Clear();
            var sales = Load<List<Sale>>(salesFile) ?? new List<Sale>();
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            double weekly = sales.Where(s => s.Date > DateTime.Now.AddDays(-7)).Sum(s => s.Amount);
            double monthly = sales.Where(s => s.Date > DateTime.Now.AddMonths(-1)).Sum(s => s.Amount);
            double yearly = sales.Where(s => s.Date > DateTime.Now.AddYears(-1)).Sum(s => s.Amount);

            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                           SALES REPORT SUMMARY                        ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════╝\n");

            Console.WriteLine("╔══════════════════════════════════════╦═════════════════════════════════╗");
            Console.WriteLine($"║ Weekly       (last 7 days)           ║   ₱ {weekly,17:F2}            ║");
            Console.WriteLine("╠══════════════════════════════════════╬═════════════════════════════════╣");
            Console.WriteLine($"║ Monthly      (last 30 days)          ║   ₱ {monthly,17:F2}            ║");
            Console.WriteLine("╠══════════════════════════════════════╬═════════════════════════════════╣");
            Console.WriteLine($"║ Yearly       (last 365 days)         ║   ₱ {yearly,17:F2}            ║");
            Console.WriteLine("╚══════════════════════════════════════╩═════════════════════════════════╝");

            Console.WriteLine("\nOptions:");
            Console.WriteLine("[1] Custom date range");
            Console.WriteLine("[2] Last N days/months/years");
            Console.WriteLine("[3] Filter by cashier username");
            Console.WriteLine("[4] Back");
            Console.Write("Choose option: ");
            var opt = (Console.ReadLine() ?? "").Trim();

            if (opt == "4") return;

            if (opt == "1")
            {
                DateTime start, end;
                while (true)
                {
                    Console.Write("Enter start date (e.g. yyyy-MM-dd or MM/dd/yyyy) or 'c' to cancel: ");
                    var sstart = Console.ReadLine()?.Trim();
                    if (string.Equals(sstart, "c", StringComparison.OrdinalIgnoreCase)) return;
                    if (TryParseDateFlexible(sstart, out start)) break;
                    Console.WriteLine("Invalid date format.");
                }
                while (true)
                {
                    Console.Write("Enter end date (e.g. yyyy-MM-dd or MM/dd/yyyy) or 'c' to cancel: ");
                    var send = Console.ReadLine()?.Trim();
                    if (string.Equals(send, "c", StringComparison.OrdinalIgnoreCase)) return;
                    if (TryParseDateFlexible(send, out end)) break;
                    Console.WriteLine("Invalid date format.");
                }
                if (end < start) { Console.WriteLine("End date earlier than start. Press any key..."); Console.ReadKey(); return; }

                var list = sales.Where(s => s.Date.Date >= start.Date && s.Date.Date <= end.Date).ToList();
                double totalCustom = list.Sum(s => s.Amount);

                Console.Clear();
                Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║                        CUSTOM DATE RANGE REPORT                       ║");
                Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════╝\n");
                Console.WriteLine($"Range : {start:yyyy-MM-dd} to {end:yyyy-MM-dd}");
                Console.WriteLine($"Transactions: {list.Count}    Total: ₱{totalCustom:F2}");
                Console.Write("Show individual transactions? (y/n): ");
                var show = (Console.ReadLine() ?? "").Trim().ToLower();
                if (show == "y")
                {
                    Console.WriteLine("ID   | Date                | Amount     | Cashier");
                    Console.WriteLine("-----+---------------------+------------+---------");
                    foreach (var s in list)
                        Console.WriteLine($"{s.Id,4} | {s.Date:yyyy-MM-dd HH:mm} | ₱{s.Amount,10:F2} | {s.CashierName}");
                }
                Console.WriteLine("\nPress any key to return...");
                Console.ReadKey();
                return;
            }
            else if (opt == "2")
            {
                Console.Write("Enter range (examples: 'last 7 days', 'months 2', 'years 1'): ");
                var input = (Console.ReadLine() ?? "").ToLower();
                int n = ExtractNumber(input);
                if (n <= 0) { Console.WriteLine("Invalid number. Press any key..."); Console.ReadKey(); return; }
                DateTime start;
                if (input.Contains("day")) start = DateTime.Now.AddDays(-n);
                else if (input.Contains("month")) start = DateTime.Now.AddMonths(-n);
                else if (input.Contains("year")) start = DateTime.Now.AddYears(-n);
                else { Console.WriteLine("Invalid unit. Press any key..."); Console.ReadKey(); return; }

                var list = sales.Where(s => s.Date >= start && s.Date <= DateTime.Now).ToList();
                Console.Clear();
                Console.WriteLine("╔════════════════════════════════════════════════╗");
                Console.WriteLine($"║     SALES FOR LAST {n} {(input.Contains("day") ? "DAYS" : input.Contains("month") ? "MONTHS" : "YEARS"),-25}║");
                Console.WriteLine("╚════════════════════════════════════════════════╝");
                Console.WriteLine("╔════════════════════════════════════════════════╗");
                Console.WriteLine($"║ Transactions: {list.Count}   ║ Total: ₱{list.Sum(x => x.Amount):F2}            ║");
                Console.WriteLine("╚════════════════════════════════════════════════╝");
                Console.WriteLine("Press any key...");
                Console.ReadKey();
                return;
            }
            else if (opt == "3")
            {
                Console.Write("Enter cashier username to filter: ");
                var cname = (Console.ReadLine() ?? "").Trim().ToLower();
                if (string.IsNullOrEmpty(cname)) { Console.WriteLine("Invalid input. Press any key..."); Console.ReadKey(); return; }
                var list = sales.Where(s => (s.CashierName ?? "").ToLower().Contains(cname)).ToList();
                Console.Clear();
                Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════╗");
                Console.WriteLine($"║    SALES FILTERED BY CASHIER: {cname}                                   ║");
                Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════╝\n");
                Console.WriteLine($"Transactions: {list.Count}    Total: ₱{list.Sum(x => x.Amount):F2}");
                Console.WriteLine("Show list? (y/n): ");
                var ans = (Console.ReadLine() ?? "").Trim().ToLower();
                if (ans == "y")
                {
                    Console.WriteLine("ID   | Date                | Amount     | Cashier");
                    Console.WriteLine("-----+---------------------+------------+---------");
                    foreach (var s in list)
                        Console.WriteLine($"{s.Id,4} | {s.Date:yyyy-MM-dd HH:mm} | ₱{s.Amount,10:F2} | {s.CashierName}");
                }
                Console.WriteLine("\nPress any key to return...");
                Console.ReadKey();
                return;
            }
            else
            {
                Console.WriteLine("Invalid option. Press any key...");
                Console.ReadKey();
                return;
            }
        }

        static void CreateAccount(Account creator)
        {
            if (creator == null || !creator.Role.Equals("owner", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Unauthorized. Only owner can create accounts. Press any key...");
                Console.ReadKey();
                return;
            }

            Console.Clear();

            string u, p, r;

            var users = Load<List<Account>>(usersFile) ?? new List<Account>();

            do
            {
                Console.Write("Username: ");
                u = Console.ReadLine()?.Trim() ?? "";

                if (string.IsNullOrEmpty(u))
                    Console.WriteLine("Username cannot be empty.");
                else if (users.Any(x => x.Username.Equals(u, StringComparison.OrdinalIgnoreCase)))
                {
                    Console.WriteLine("Username already exists. Choose another.");
                    u = "";
                }
            } while (string.IsNullOrEmpty(u));

            do
            {
                Console.Write("Password: ");
                p = Console.ReadLine()?.Trim() ?? "";

                if (string.IsNullOrEmpty(p))
                    Console.WriteLine("Password cannot be empty.");
            } while (string.IsNullOrEmpty(p));

            do
            {
                Console.Write("Role (server/cashier): ");
                r = Console.ReadLine()?.Trim().ToLower() ?? "";

                if (r != "server" && r != "cashier")
                    Console.WriteLine("Invalid role. Please enter: server / cashier");
            } while (r != "server" && r != "cashier");


            users.Add(new Account { Username = u, Password = p, Role = r });
            Save(usersFile, users);

            Console.WriteLine("Account created successfully. Press any key...");
            Console.ReadKey();
        }

        static void ViewEmployees()
        {
            Console.Clear();
            var users = Load<List<Account>>(usersFile) ?? new List<Account>();

            if (users.Count == 0)
            {
                Console.WriteLine("No employees found.");
                Console.WriteLine("Press any key...");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("Employees:");
            for (int i = 0; i < users.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {users[i].Username} - {users[i].Role}");
            }

            Console.Write("\nDo you want to delete an account? (y/n) : ");
            var choice = Console.ReadLine()?.ToLower();

            if (choice == "y")
            {
                Console.Write("Enter the number of the account to delete: ");
                if (int.TryParse(Console.ReadLine(), out int number) && number >= 1 && number <= users.Count)
                {
                    var deletedUser = users[number - 1];

                    Console.Write($"Are you sure you want to delete '{deletedUser.Username}'? (y/n): ");
                    var confirm = Console.ReadLine()?.ToLower();
                    if (confirm == "y")
                    {
                        users.RemoveAt(number - 1);
                        Save(usersFile, users);
                        Console.WriteLine($"Account '{deletedUser.Username}' has been deleted.");
                    }
                    else
                    {
                        Console.WriteLine("Deletion canceled.");
                    }
                }
                else
                {
                    Console.WriteLine("Invalid number. No account deleted.");
                }
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        static void ManageMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== MENU MANAGEMENT ===");
                Console.WriteLine("[1] Add item");
                Console.WriteLine("[2] Remove item");
                Console.WriteLine("[3] Edit item");
                Console.WriteLine("[4] Back");
                Console.Write("Choose: ");
                var ch = Console.ReadLine();
                if (ch == "1") AddMenuItem();
                else if (ch == "2") RemoveMenuItem();
                else if (ch == "3") EditMenuItem();
                else break;
            }
        }

        static void AddMenuItem()
        {
            Console.Clear();
            Console.Write("Category (Main/Beverage/Appetizer): ");
            var cat = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(cat) ||
                !(cat.Equals("Main", StringComparison.OrdinalIgnoreCase) ||
                  cat.Equals("Beverage", StringComparison.OrdinalIgnoreCase) ||
                  cat.Equals("Appetizer", StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine("Invalid category.");
                Console.ReadKey();
                return;
            }

            Console.Write("Name: ");
            var name = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(name) || !Regex.IsMatch(name, @"^[A-Za-z ]+$"))
            {
                Console.WriteLine("Invalid name.");
                Console.ReadKey();
                return;
            }

            Console.Write("Price: ");
            if (!double.TryParse(Console.ReadLine(), out double price) || price <= 0)
            {
                Console.WriteLine("Invalid price.");
                Console.ReadKey();
                return;
            }

            var menu = Load<List<MenuItem>>(menuFile) ?? new List<MenuItem>();
            menu.Add(new MenuItem { Category = cat, Name = name, Price = price });
            Save(menuFile, menu);

            Console.WriteLine("Added. Press any key...");
            Console.ReadKey();
        }

        static void RemoveMenuItem()
        {
            var menu = Load<List<MenuItem>>(menuFile) ?? new List<MenuItem>();

            if (menu.Count == 0)
            {
                Console.WriteLine("Menu is empty.");
                Console.ReadKey();
                return;
            }

            Console.Clear();
            for (int i = 0; i < menu.Count; i++)
                Console.WriteLine($"[{i + 1}] {menu[i].Category} - {menu[i].Name} - ₱{menu[i].Price:F2}");

            Console.Write("Choose number to remove: ");
            var input = Console.ReadLine()?.Trim();

            if (!int.TryParse(input, out int idx) || idx < 1 || idx > menu.Count)
            {
                Console.WriteLine("Invalid selection.");
                Console.ReadKey();
                return;
            }

            menu.RemoveAt(idx - 1);
            Save(menuFile, menu);

            Console.WriteLine("Removed. Press any key...");
            Console.ReadKey();
        }

        static void EditMenuItem()
        {
            var menu = Load<List<MenuItem>>(menuFile) ?? new List<MenuItem>();

            if (menu.Count == 0)
            {
                Console.WriteLine("Menu is empty.");
                Console.ReadKey();
                return;
            }

            Console.Clear();
            Console.WriteLine("=== EDIT MENU ITEM ===\n");

            for (int i = 0; i < menu.Count; i++)
                Console.WriteLine($"[{i + 1}] {menu[i].Category} - {menu[i].Name} - ₱{menu[i].Price:F2}");

            Console.Write("\nChoose item number to edit: ");
            var input = Console.ReadLine()?.Trim();

            if (!int.TryParse(input, out int idx) || idx < 1 || idx > menu.Count)
            {
                Console.WriteLine("Invalid selection.");
                Console.ReadKey();
                return;
            }

            var item = menu[idx - 1];

            Console.WriteLine($"\nEditing: {item.Category} - {item.Name} - ₱{item.Price:F2}\n");

            Console.Write("New Category (Main/Beverages/Appetizers) or Enter to keep: ");
            var newCat = Console.ReadLine()?.Trim();

            if (!string.IsNullOrWhiteSpace(newCat))
            {
                if (!(newCat.Equals("Main", StringComparison.OrdinalIgnoreCase) ||
                      newCat.Equals("Beverages", StringComparison.OrdinalIgnoreCase) ||
                      newCat.Equals("Appetizers", StringComparison.OrdinalIgnoreCase)))
                {
                    Console.WriteLine("Invalid category. Edit canceled.");
                    Console.ReadKey();
                    return;
                }

                item.Category = newCat;
            }

            Console.Write("New Name or Enter to keep: ");
            var newName = Console.ReadLine()?.Trim();

            if (!string.IsNullOrWhiteSpace(newName))
            {
                if (!Regex.IsMatch(newName, @"^[A-Za-z ]+$"))
                {
                    Console.WriteLine("Invalid name. Edit canceled.");
                    Console.ReadKey();
                    return;
                }

                item.Name = newName;
            }

            Console.Write("New Price or Enter to keep: ");
            var priceInput = Console.ReadLine()?.Trim();

            if (!string.IsNullOrWhiteSpace(priceInput))
            {
                if (!double.TryParse(priceInput, out double newPrice) || newPrice <= 0)
                {
                    Console.WriteLine("Invalid price. Edit canceled.");
                    Console.ReadKey();
                    return;
                }

                item.Price = newPrice;
            }

            Save(menuFile, menu);

            Console.WriteLine("\nItem updated successfully!");
            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }

        static bool TryParseDateFlexible(string? input, out DateTime date)
        {
            date = DateTime.MinValue;
            if (string.IsNullOrWhiteSpace(input)) return false;

            if (DateTime.TryParse(input, CultureInfo.CurrentCulture, DateTimeStyles.None, out date))
                return true;

            string[] formats = {
                "yyyy-MM-dd", "MM/dd/yyyy", "M/d/yyyy",
                "dd-MM-yyyy", "d-M-yyyy",
                "MMMM dd yyyy", "MMM dd yyyy", "MMM d yyyy",
                "MMMM d yyyy", "yyyy/MM/dd"
            };

            return DateTime.TryParseExact(input, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }

        static int ExtractNumber(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return -1;
            var digits = new string(input.Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out int n) ? n : -1;
        }

        static T? Load<T>(string file)
        {
            try
            {
                if (!File.Exists(file)) return default;
                var txt = File.ReadAllText(file);
                if (string.IsNullOrWhiteSpace(txt)) return default;
                return JsonSerializer.Deserialize<T>(txt);
            }
            catch
            {
                return default;
            }
        }

        static void Save<T>(string file, T data)
        {
            File.WriteAllText(file, JsonSerializer.Serialize(data, jsonOpts));
        }
    }

    class Account
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string Role { get; set; } = "";
    }

    class MenuItem
    {
        public string Category { get; set; } = "";
        public string Name { get; set; } = "";
        public double Price { get; set; }
    }

    class OrderLine
    {
        public string Item { get; set; } = "";
        public int Quantity { get; set; }
        public double Price { get; set; }
    }

    class Reservation
    {
        public int ReservationID { get; set; }
        public string CustomerName { get; set; } = "";
        public string Date { get; set; } = "";
        public string Time { get; set; } = "";
        public int TableNumber { get; set; }
        public List<OrderLine> Orders { get; set; } = new List<OrderLine>();
        public double TotalAmount { get; set; }
        public string CreatedByServer { get; set; } = "";
        public bool IsPaid { get; set; }
        public DateTime? PaidAt { get; set; }
    }

    class Sale
    {
        public int Id { get; set; }
        public int ReservationID { get; set; }
        public DateTime Date { get; set; }
        public double Amount { get; set; }
        public string CashierName { get; set; } = "";
    }
}
