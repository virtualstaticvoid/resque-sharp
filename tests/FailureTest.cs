namespace resque
{
  using System;
  using System.Text;
  using NUnit.Framework;

  [TestFixture]
  class FailureTest
  {
    class NUnitConsoleRunner
    {
      [STAThread]
      static void Main(string[] args)
      {
        NUnit.ConsoleRunner.Runner.Main(args);
      }
    }

    private string server;
    private Failure.Redis myRedis;
    private const String testString = "failed";
    private Object payload;

    [SetUp]
    public void Init()
    {
      Resque.setRedis(ServerHelper.GetRedis());
      Resque.redis().FlushAll();

      Exception ex = new Exception(testString);
      Worker worker = new Worker();
      const string queue = testString;
      payload = Encoding.UTF8.GetBytes(testString);

      myRedis = new Failure.Redis(ex, worker, queue, payload);
    }

    [Test]
    public void CanCreateFailure()
    {

      Assert.AreEqual(testString, myRedis.exception.Message);
      Assert.AreEqual(testString, myRedis.queue);

// ReSharper disable RedundantCast
      Object temp = (Byte[])myRedis.payload;
// ReSharper restore RedundantCast

      Assert.AreEqual(temp, payload);

    }
    [Test]
    public void CanGetURL()
    {
      Assert.AreEqual(Resque.failure.url(), server);
    }

    [Test]
    public void CanCheckEmptyQueue()
    {
      Assert.AreEqual(0, Resque.failure.count());
    }

    [Test]
    public void CanSaveOnItemToQueue()
    {
      myRedis.save();

      int count = Resque.failure.count();
      Assert.AreEqual(1, count);

    }

    [Test]
    public void CanSaveRandomNumberOfItemsToQueue()
    {
      int random = new System.Random().Next(5, 20);

      for (int i = 0; i < random; i++)
      {
        myRedis.save();
      }

      Assert.AreEqual(random, Resque.failure.count());
    }

    [Test]
    public void CanClear()
    {
      int randNumOfJobs = new System.Random().Next(5, 20);

      for (int i = 0; i < randNumOfJobs; i++)
      {
        myRedis.save();
      }

      Assert.AreEqual(randNumOfJobs, Resque.failure.count());

      Resque.failure.clear();

      Assert.AreEqual(0, Resque.failure.count());
    }

    [Test]
    public void CanRetrieveAllKeys()
    {
      int randNumOfJobs = new System.Random().Next(5, 20);

      for (int i = 0; i < randNumOfJobs; i++)
      {
        myRedis.save();
      }

      Byte[][] allKeys = Resque.failure.all(0, randNumOfJobs);

      Assert.AreEqual(allKeys.Length, randNumOfJobs);

    }

  }
}
