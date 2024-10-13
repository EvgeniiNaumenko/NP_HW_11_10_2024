using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        Console.WriteLine("Введите имя ресторана!");
        string restaurantName = Console.ReadLine();
        Client client = new Client(restaurantName, "127.0.0.1", 7777);
        bool exit = false;

        try
        {
            while (!exit)
            {
                Console.WriteLine("\nРесторан");
                Console.WriteLine("1. Оформить заказ");
                Console.WriteLine("2. Посмотреть заказы");
                Console.WriteLine("3. Проверить статус заказа");
                Console.WriteLine("4. Выход");
                Console.Write("Ваш выбор: ");

                string choice = Console.ReadLine();
                switch (choice)
                {
                    case "1":
                        Console.WriteLine("Введите заказ");
                        string orderDetails = Console.ReadLine();
                        client.PlaceOrder(orderDetails);
                        break;
                    case "2":
                        client.ShowMyOrders();
                        break;
                    case "3":
                        Console.WriteLine("Введите Id заказа");
                        if (int.TryParse(Console.ReadLine(), out int orderId))
                            client.CheckOrderStatus(orderId);
                        else
                            Console.WriteLine("Неверный ввод!");
                        break;
                    case "4":
                        exit = true;
                        Console.WriteLine("Выход");
                        break;
                    default:
                        Console.WriteLine("Неверный выбор");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
        finally
        {
            client.Close();
        }
    }
}

public class Client
{
    private TcpClient client;
    private NetworkStream stream;
    private string restaurantName;
    private Dictionary<int, string> myOrders = new Dictionary<int, string>();

    public Client(string name, string ip, int port)
    {
        restaurantName = name;
        client = new TcpClient(ip, port);
        stream = client.GetStream();
    }

    public void PlaceOrder(string orderDetails)
    {
        try
        {
            string message = $"ADD_ORDER:{restaurantName}:{orderDetails}";
            byte[] data = Encoding.UTF8.GetBytes(message);
            stream.Write(data, 0, data.Length);

            byte[] buffer = new byte[1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine(response);
            SaveOrder(orderDetails, response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при оформлении заказа: {ex.Message}");
        }
    }

    public void CheckOrderStatus(int orderId)
    {
        if (myOrders.ContainsKey(orderId))
        {
            try
            {
                string message = $"CHECK_ORDER:-:{orderId}";
                byte[] data = Encoding.UTF8.GetBytes(message);
                stream.Write(data, 0, data.Length);

                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при проверке статуса заказа: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("Заказ с таким ID не найден.");
        }
    }

    public void Close()
    {
        stream.Close();
        client.Close();
    }

    private void SaveOrder(string orderDetails, string text)
    {
        string pattern = @"№(\d+)";
        Match match = Regex.Match(text, pattern);
        if (match.Success)
        {
            string numberString = match.Groups[1].Value;
            if (int.TryParse(numberString, out int id))
            {
                myOrders.Add(id, orderDetails);
            }
        }
    }

    public void ShowMyOrders()
    {
        if (myOrders.Count > 0)
        {
            Console.WriteLine("Мои заказы:");
            foreach (var order in myOrders)
            {
                Console.WriteLine($"Номер заказа {order.Key}, Заказ: {order.Value}");
            }
        }
        else
        {
            Console.WriteLine("Заказов еще нет!");
        }
    }
}
