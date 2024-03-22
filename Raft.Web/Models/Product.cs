namespace Raft.Web.Models;

public class Product
{
  public Product(string name, decimal cost)
  {
    Name = name;
    Cost = cost;
  }

  public string Name { get; set; }
  public decimal Cost { get; set; }
  public int Quantity { get; set; } = 0;
}