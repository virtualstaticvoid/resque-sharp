namespace resque
{
  using System;
  using NUnit.Framework;

  [TestFixture]
  class StatTest
  {
    class NUnitConsoleRunner
    {
      [STAThread]
      static void Main(string[] args)
      {
        NUnit.ConsoleRunner.Runner.Main(args);
      }
    }

    [SetUp]
    public void Init()
    {
      Resque.setRedis(ServerHelper.GetRedis());
      Resque.redis().FlushAll();
    }

    [Test]
    public void canCreateAStat()
    {
      Stat.increment("fakeStat");
      int statRetrieveValue = Stat.get("fakeStat");
      Assert.AreEqual(1, statRetrieveValue);
    }

    [Test]
    public void canCreateAndCreateAndIncrementStat()
    {
      Random rand = new Random(System.DateTime.Now.Second);
      int statExpectValue = rand.Next(5, 20);

      for (int i = 0; i < statExpectValue; i++)
      {
        Stat.increment("fakeStat");
      }

      int statRetrieveValue = Stat.get("fakeStat");
      Assert.AreEqual(statExpectValue, statRetrieveValue);
    }

    [Test]
    public void canCreateStatGreaterThanOne()
    {
      Random rand = new Random(System.DateTime.Now.Second);
      int statExpectValue = rand.Next(5, 20);

      Stat.increment("fakeStat", statExpectValue);

      int statRetrieveValue = Stat.get("fakeStat");
      Assert.AreEqual(statExpectValue, statRetrieveValue);
    }

    [Test]
    public void canDecrementAStat()
    {
      Random rand = new Random(System.DateTime.Now.Second);
      int statExpectValue = rand.Next(5, 20);

      for (int i = 0; i < statExpectValue; i++)
      {
        Stat.increment("fakeStat");
      }

      Stat.decrement("fakeStat");
      int statRetrieveValue = Stat.get("fakeStat");
      Assert.AreEqual(statExpectValue - 1, statRetrieveValue);
    }

    [Test]
    public void canClearStats()
    {
      Random rand = new Random(System.DateTime.Now.Second);
      int statExpectValue = rand.Next(5, 20);

      for (int i = 0; i < statExpectValue; i++)
      {
        Stat.increment("fakeStat");
      }

      int statRetrieveValue = Stat.get("fakeStat");
      Assert.AreEqual(statExpectValue, statRetrieveValue);

      Stat.clear("fakeStat");

      Stat.increment("fakeStat");

      statRetrieveValue = Stat.get("fakeStat");
      Assert.AreEqual(1, statRetrieveValue);
    }


  }
}