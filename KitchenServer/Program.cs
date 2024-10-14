using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static void Main()
    {
        var server = new Server("127.0.0.1", 7777);
        server.Start();
    }
}

public class Order
{
    public int Id { get; set; }
    public string RestaurantName { get; set; }
    public string OrderDetails { get; set; }
    public DateTime OrderReceiptTime { get; set; }
    public bool OrderStatus { get; set; } = false;
}

public class Server
{
    private TcpListener listener;
    private ConcurrentDictionary<int, Order> orderStatus = new ConcurrentDictionary<int, Order>();
    private int orderIdCounter = 0;

    public Server(string ip, int port)
    {
        listener = new TcpListener(IPAddress.Parse(ip), port);
    }

    public void Start()
    {
        listener.Start();
        Console.WriteLine("Кухня работает!!");
        while (true)
        {
            var client = listener.AcceptTcpClient();
            Task.Run(() => HandleClient(client));
        }
    }

    private async Task HandleClient(TcpClient client)
    {
        try
        {
            using (client)
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                string[] splitData = data.Split(':');

                string command = splitData[0];

                if (command == "ADD_ORDER")
                {
                    string restaurantName = splitData[1];
                    string orderDetails = splitData[2];
                    int orderId = Interlocked.Increment(ref orderIdCounter);
                    Order order = new Order
                    {
                        Id = ++orderIdCounter,
                        RestaurantName = restaurantName,
                        OrderDetails = orderDetails,
                        OrderReceiptTime = DateTime.Now
                    };
                    orderStatus.TryAdd(order.Id, order);

                    string response = $"Ваш заказ принят! Номер заказа: №{order.Id}";
                    byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                    await stream.WriteAsync(responseBytes, 0, responseBytes.Length);

                    await Task.Delay(new Random().Next(10, 20) * 1000);

                    order.OrderStatus = true; 
                }
                else if (command == "CHECK_ORDER")
                {
                    if (int.TryParse(splitData[2], out int id) && orderStatus.TryGetValue(id, out Order order))
                    {
                        string result = order.OrderStatus ? "готов!" : "в процессе приготовления!";
                        string response = $"Ваш заказ №{order.Id}, {result}";
                        byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                        await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                    }
                    else
                    {
                        string response = "Заказ не найден или неверный ID.";
                        byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                        await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                    }
                }
                stream.Close();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при обработке клиента: {ex.Message}");
        }
    }
}
