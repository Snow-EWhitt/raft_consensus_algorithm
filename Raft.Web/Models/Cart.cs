namespace Raft.Web.Models;

public class Cart
{
  public string Username { get; set; } = "";
  public List<Product> Products { get; set; } = [];
}