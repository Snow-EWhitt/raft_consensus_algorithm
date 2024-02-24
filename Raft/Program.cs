namespace Raft;

public class Program
{
  public static void Main()
  {
    List<Node> nodes = [];

    foreach (var file in Directory.GetFiles(Directory.GetCurrentDirectory(), "*.log"))
      File.Delete(file);

    for (int i = 0; i < 3; i++)
    {
      nodes.Add(new Node(nodes));
    }

    foreach (var node in nodes)
    {
      var thread = new Thread(new ThreadStart(node.Initialize));
      thread.Start();
    }
  }
}