using Raft;

namespace Raft.Tests;

public class Tests
{
  [SetUp]
  public void Setup()
  {
  
  }

  [Test]
  public void LeaderIsElectedIfTwoOfThreeNodesAreHealthy()
  {
    // Arrange
    List<Node> nodes = [];
    var testTimeProvider = new TestTimeProvider(DateTime.UtcNow);

    nodes.Add(new Node(nodes, testTimeProvider, true));
    nodes.Add(new Node(nodes, testTimeProvider, true));
    nodes.Add(new Node(nodes, testTimeProvider, false));

    testTimeProvider.UtcNow = testTimeProvider.UtcNow.AddMilliseconds(300);
    
    // Act
    foreach (Node node in nodes)
    {
      node.Act();
    }

    // Assert
    int leaderCount = nodes.Count(n => n.State == NodeState.Leader);

    Assert.That(leaderCount, Is.EqualTo(1));
  }

  [Test]
  public void LeaderIsElectedIfThreeOfFiveNodesAreHealthy()
  {
    // Arrange
    List<Node> nodes = [];
    var testTimeProvider = new TestTimeProvider(DateTime.UtcNow);

    nodes.Add(new Node(nodes, testTimeProvider, true));
    nodes.Add(new Node(nodes, testTimeProvider, true));
    nodes.Add(new Node(nodes, testTimeProvider, true));
    nodes.Add(new Node(nodes, testTimeProvider, false));
    nodes.Add(new Node(nodes, testTimeProvider, false));

    testTimeProvider.UtcNow = testTimeProvider.UtcNow.AddMilliseconds(300);
    
    // Act
    foreach (Node node in nodes)
    {
      node.Act();
    }

    // Assert
    int leaderCount = nodes.Count(n => n.State == NodeState.Leader);

    Assert.That(leaderCount, Is.EqualTo(1));
  }

  [Test]
  public void LeaderIsNotElectedIfThreeOfFiveNodesAreUnhealthy()
  {
    // Arrange
    List<Node> nodes = [];
    var testTimeProvider = new TestTimeProvider(DateTime.UtcNow);

    nodes.Add(new Node(nodes, testTimeProvider, true));
    nodes.Add(new Node(nodes, testTimeProvider, true));
    nodes.Add(new Node(nodes, testTimeProvider, false));
    nodes.Add(new Node(nodes, testTimeProvider, false));
    nodes.Add(new Node(nodes, testTimeProvider, false));

    testTimeProvider.UtcNow = testTimeProvider.UtcNow.AddMilliseconds(300);
    // Act
    foreach (Node node in nodes)
    {
      node.Act();
    }

    // Assert
    int leaderCount = nodes.Count(n => n.State == NodeState.Leader);

    Assert.That(leaderCount, Is.EqualTo(0));
  }
  
  [Test]
  public void NodeContinuesAsLeaderIfAllNodesStayHealthy()
  {
    // Arrange
    List<Node> nodes = [];
    var testTimeProvider = new TestTimeProvider(DateTime.UtcNow);

    nodes.Add(new Node(nodes, testTimeProvider, true));
    nodes.Add(new Node(nodes, testTimeProvider, true));
    nodes.Add(new Node(nodes, testTimeProvider, true));

    testTimeProvider.UtcNow = testTimeProvider.UtcNow.AddMilliseconds(300);

    // Act
    foreach (Node node in nodes)
    {
      node.Act();
    }

    Node initialLeader = nodes.First(n => n.State == NodeState.Leader);
    initialLeader.Act();

    // Assert
    int leaderCount = nodes.Count(n => n.State == NodeState.Leader);

    Assert.Multiple(() =>
    {
      Assert.That(initialLeader.State, Is.EqualTo(NodeState.Leader));
      Assert.That(nodes.Count(n => n.State == NodeState.Leader), Is.EqualTo(1));
    });
  }

  [Test]
  public void NodeCallsForElectionIfLeaderTakesTooLong()
  {
    // Arrange
    List<Node> nodes = [];
    var testTimeProvider = new TestTimeProvider(DateTime.UtcNow);

    nodes.Add(new Node(nodes, testTimeProvider, true));
    nodes.Add(new Node(nodes, testTimeProvider, true));
    nodes.Add(new Node(nodes, testTimeProvider, true));

    testTimeProvider.UtcNow = testTimeProvider.UtcNow.AddMilliseconds(500);

    // Act
    foreach (Node node in nodes)
    {
      node.Act();
    }

    Node initialLeader = nodes.First(n => n.State == NodeState.Leader);
    Node follower = nodes.First(n => n != initialLeader);

    follower.Act();

    // Assert
    Assert.That(follower.State, Is.EqualTo(NodeState.Leader));
  }

  [Test]
  public void NodeContinuesAsLeaderEvenIfTwoOfFiveNodesBecomeUnhealthy()
  {
    // Arrange
    List<Node> nodes = [];
    var testTimeProvider = new TestTimeProvider(DateTime.UtcNow);

    nodes.Add(new Node(nodes, testTimeProvider, true));
    nodes.Add(new Node(nodes, testTimeProvider, true));
    nodes.Add(new Node(nodes, testTimeProvider, true));
    nodes.Add(new Node(nodes, testTimeProvider, true));
    nodes.Add(new Node(nodes, testTimeProvider, true));

    testTimeProvider.UtcNow = testTimeProvider.UtcNow.AddMilliseconds(300);

    // Act
    foreach (Node node in nodes)
    {
      node.Act();
    }

    Node initialLeader = nodes.First(n => n.State == NodeState.Leader);
    List<Node> unhealthyNodes = nodes.Where(n => n != initialLeader).Take(2).ToList();

    foreach (Node node in unhealthyNodes)
    {
      node.Stop();
    }

    testTimeProvider.UtcNow = testTimeProvider.UtcNow.AddMilliseconds(-150);

    foreach (Node node in nodes)
    {
      node.Act();
    }

    // Assert
    Assert.Multiple(() =>
    {
      Assert.That(initialLeader.State, Is.EqualTo(NodeState.Leader));
      Assert.That(nodes.Count(n => n.State == NodeState.Leader), Is.EqualTo(1));
    });
  }

  [Test]
  public void AvoidingDoubleVoting()
  {
    // Arrange
    List<Node> nodes = [];
    var testTimeProvider = new TestTimeProvider(DateTime.UtcNow);

    nodes.Add(new Node(nodes, testTimeProvider, true));
    nodes.Add(new Node(nodes, testTimeProvider, true));
    nodes.Add(new Node(nodes, testTimeProvider, true));
    nodes.Add(new Node(nodes, testTimeProvider, true));

    testTimeProvider.UtcNow = testTimeProvider.UtcNow.AddMilliseconds(500);

    // Act
    nodes[0].Act();

    var initialLeader = nodes.First(n => n.State == NodeState.Leader);
    foreach (var node in nodes.Skip(1).Take(3))
    {
        node.Restart();
    }

    nodes.Add(new Node(nodes, testTimeProvider, true));
    nodes[4].Act();

    // Assert
    Assert.Multiple(() =>
    {
      Assert.That(nodes[4].State, Is.Not.EqualTo(NodeState.Leader));
      Assert.That(initialLeader.State == NodeState.Leader, Is.True);
    });
  }

  [Test]
  public void OldLeaderBecomesFollowerIfLeaderWithGreaterTermSendsHeartbeat()
  {
    // Arrange
    List<Node> nodes = [];
    var testTimeProvider = new TestTimeProvider(DateTime.UtcNow);

    nodes.Add(new Node(nodes, testTimeProvider, true));
    nodes.Add(new Node(nodes, testTimeProvider, true));
    nodes.Add(new Node(nodes, testTimeProvider, true));
    nodes.Add(new Node(nodes, testTimeProvider, true));
    nodes.Add(new Node(nodes, testTimeProvider, true));

    testTimeProvider.UtcNow = testTimeProvider.UtcNow.AddMilliseconds(500);

    // Act
    foreach (Node node in nodes)
    {
      node.Act();
    }

    Node initialLeader = nodes.First(n => n.State == NodeState.Leader);
    testTimeProvider.UtcNow = testTimeProvider.UtcNow.AddMilliseconds(500);

    foreach (Node node in nodes.Where(n => n != initialLeader))
    {
      node.Act();
    }

    Node newLeader = nodes.First(n => n != initialLeader && n.State == NodeState.Leader);

    // Assert
    Assert.Multiple(() =>
    {
      Assert.That(initialLeader.State, Is.EqualTo(NodeState.Follower));
      Assert.That(newLeader.State, Is.EqualTo(NodeState.Leader));
    });
  }

  [Test]
  public void EventualGetWritesAndReturnsCorrectly()
  {
    // Arrange
    List<Node> nodes = [];
    Gateway gateway = new(nodes);
    var testTimeProvider = new TestTimeProvider(DateTime.UtcNow);

    nodes.Add(new Node(nodes, testTimeProvider, true));
    nodes.Add(new Node(nodes, testTimeProvider, true));
    nodes.Add(new Node(nodes, testTimeProvider, true));
    nodes.Add(new Node(nodes, testTimeProvider, true));
    nodes.Add(new Node(nodes, testTimeProvider, true));

    testTimeProvider.UtcNow = testTimeProvider.UtcNow.AddMilliseconds(500);

    // Act
    foreach (Node node in nodes)
    {
      node.Act();
    }

    Node leader = nodes.First(n => n.State == NodeState.Leader);
    
    gateway.Write("eventualGetKey", 2);
    
    foreach (Node node in nodes)
    {
      node.Act();
    }

    int? value = gateway.EventualGet("eventualGetKey");

    // Assert
    Assert.That(value, Is.EqualTo(2));
  }

  [Test]
  public void StrongGetWritesAndReturnsCorrectly()
  {
    // Arrange
    List<Node> nodes = [];
    Gateway gateway = new(nodes);
    var testTimeProvider = new TestTimeProvider(DateTime.UtcNow);

    nodes.Add(new Node(nodes, testTimeProvider, true));
    nodes.Add(new Node(nodes, testTimeProvider, true));
    nodes.Add(new Node(nodes, testTimeProvider, true));
    nodes.Add(new Node(nodes, testTimeProvider, true));
    nodes.Add(new Node(nodes, testTimeProvider, true));

    testTimeProvider.UtcNow = testTimeProvider.UtcNow.AddMilliseconds(500);

    // Act
    foreach (Node node in nodes)
    {
      node.Act();
    }

    Node leader = nodes.First(n => n.State == NodeState.Leader);
    
    gateway.Write("strongGetKey", 4);
    
    foreach (Node node in nodes)
    {
      node.Act();
    }

    int? value = gateway.StrongGet("strongGetKey");

    // Assert
    Assert.That(value, Is.EqualTo(4));
  }

  [Test]
  public void CompareVersionAndSwapIsSuccessful()
  {
    // Arrange
    List<Node> nodes = [];
    Gateway gateway = new(nodes);
    var testTimeProvider = new TestTimeProvider(DateTime.UtcNow);

    nodes.Add(new Node(nodes, testTimeProvider, true));
    nodes.Add(new Node(nodes, testTimeProvider, true));
    nodes.Add(new Node(nodes, testTimeProvider, true));
    nodes.Add(new Node(nodes, testTimeProvider, true));
    nodes.Add(new Node(nodes, testTimeProvider, true));

    testTimeProvider.UtcNow = testTimeProvider.UtcNow.AddMilliseconds(500);

    // Act
    foreach (Node node in nodes)
    {
      node.Act();
    }

    Node leader = nodes.First(n => n.State == NodeState.Leader);
    
    gateway.Write("CVASKey", 8);
    
    foreach (Node node in nodes)
    {
      node.Act();
    }

    bool result = gateway.CompareVersionAndSwap("CVASKey", 8, 16);

    int? value = gateway.StrongGet("CVASKey");

    // Assert
    Assert.Multiple(() =>
    {
      Assert.That(result, Is.True);
      Assert.That(value, Is.EqualTo(16));
    });
  }
}

public class TestTimeProvider : ITimeProvider
{
    private DateTime currentTime;

    public TestTimeProvider(DateTime initialTime)
    {
        currentTime = initialTime;
    }

    public DateTime UtcNow
    {
        get => currentTime;
        set => currentTime = value;
    }
}
