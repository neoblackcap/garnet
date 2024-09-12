using StackExchange.Redis;

namespace Extensions.test;

public class Tests
{
    ConnectionMultiplexer rdb;

    /// <summary>
    /// Test custom extension
    /// </summary>
    public Tests()
    {
    }

    [SetUp]
    public void Setup()
    {

        rdb = ConnectionMultiplexer.Connect("localhost");
    }

    [Test]
    public void Test1()
    {
        var db = rdb.GetDatabase(0);
        db.Execute("GRAPH");
        Assert.AreEqual(1, 2);
        Assert.Pass();
    }
}