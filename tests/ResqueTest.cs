﻿namespace resque
{
  using System;
  using System.Collections.Generic;
  using NUnit.Framework;
  using System.Collections;

  class NUnitConsoleRunner
  {
    [STAThread]
    static void Main(string[] args)
    {
      NUnit.ConsoleRunner.Runner.Main(args);
    }
  }

  [TestFixture]
  public class ResqueTest
  {
    [SetUp]
    public void Init()
    {
      ServerHelper.GetRedis().FlushAll();
      Resque.setRedis(ServerHelper.GetRedis());
      Resque.Push("people", new Dictionary<string, string> { { "name", "chris" } });
      Resque.Push("people", new Dictionary<string, string> { { "name", "bob" } });
      Resque.Push("people", new Dictionary<string, string> { { "name", "mark" } });
    }
    [Test]
    public void CanPutJobsOnAQueue()
    {
      Assert.IsTrue(Job.create("jobs", "DummyJob", 20, "/tmp"));
      Assert.IsTrue(Job.create("jobs", "DummyJob", 20, "/tmp"));
    }

    [Test]
    public void CanGrabJobsOffAQueue()
    {
      //Job.create("jobs", "dummy-job", 20, "/tmp"); FIXME NEED TO DEAL WITH THIS
      Job.create("jobs", "resque.DummyJob", 20, "/tmp");
      Job job = Resque.Reserve("jobs");
      Assert.AreEqual("resque.DummyJob", job.PayloadClass().FullName);
      var num = job.args()[0];
      Assert.AreEqual(20, num);
      Assert.That("/tmp", Is.EqualTo(job.args()[1]));
    }

    [Test]
    public void CanReQueueJobs()
    {
      Job.create("jobs", "resque.DummyJob", 20, "/tmp");
      Job job = Resque.Reserve("jobs");
      job.recreate();
      Assert.That(job, Is.EqualTo(Resque.Reserve("jobs")));
    }

    [Test]
    public void CanAskResqueForQueueSize()
    {
      Assert.That(0, Is.EqualTo(Resque.size("a_queue")));
      Job.create("a_queue", "resque.DummyJob", 1, "asdf");
      Assert.That(1, Is.EqualTo(Resque.size("a_queue")));
    }

    [Test]
    public void CanPutJobsOnTheQueueByAskingWhichQueueTheyAreInterestedIn()
    {
      Assert.That(0, Is.EqualTo(Resque.size("tester")));
      Assert.IsTrue(Resque.enqueue("resque.DummyJob", 20, "/tmp"));
      Assert.IsTrue(Resque.enqueue("resque.DummyJob", 20, "/tmp"));

      Job job = Resque.Reserve("tester");

      Assert.That(20, Is.EqualTo(job.args()[0]));
      Assert.That("/tmp", Is.EqualTo(job.args()[1]));
    }


    [Test]
    public void CanTestForEquality()
    {
      Assert.IsTrue(Job.create("jobs", "resque.DummyJob", 20, "/tmp"));
      Assert.IsTrue(Job.create("jobs", "resque.DummyJob", 20, "/tmp"));
      //Assert.IsTrue(Job.create("jobs", "dummy-job", 20, "/tmp"));  NEED TO  MAKE THIS WORK
      Assert.That(Resque.Reserve("jobs"), Is.EqualTo(Resque.Reserve("jobs")));

      Assert.IsTrue(Job.create("jobs", "resque.NotDummyJob", 20, "/tmp"));
      Assert.IsTrue(Job.create("jobs", "resque.DummyJob", 20, "/tmp"));
      Assert.That(Resque.Reserve("jobs"), Is.Not.EqualTo(Resque.Reserve("jobs")));

      Assert.IsTrue(Job.create("jobs", "resque.DummyJob", 20, "/tmp"));
      Assert.IsTrue(Job.create("jobs", "resque.DummyJob", 30, "/tmp"));
      Assert.That(Resque.Reserve("jobs"), Is.Not.EqualTo(Resque.Reserve("jobs")));

    }

    [Test]
    public void QueueMustBeInferrable()
    {
      Assert.That(
          new TestDelegate(EnqueueUninferrableJob),
          Throws.TypeOf<resque.NoQueueError>()
          );
    }

    [Test]
    public void CanPutItemsOnAQueue()
    {
      Dictionary<string, string> person = new Dictionary<string, string>();
      person.Add("name", "chris");
      Assert.That(Resque.Push("people", person), Is.True);
    }

    [Test]
    public void CanPullItemsOffAQueue()
    {
      Assert.That("chris", Is.EqualTo(Resque.Pop("people")["name"]));
      Assert.That("bob", Is.EqualTo(Resque.Pop("people")["name"]));
      Assert.That("mark", Is.EqualTo(Resque.Pop("people")["name"]));
      Assert.That(Resque.Pop("people"), Is.Null);
    }

    [Test]
    public void KnowsHowBigAQueueIs()
    {
      Assert.That(Resque.size("people"), Is.EqualTo(3));
      Assert.That("chris", Is.EqualTo(Resque.Pop("people")["name"]));
      Assert.That(Resque.size("people"), Is.EqualTo(2));
      Resque.Pop("people");
      Resque.Pop("people");
      Assert.That(Resque.size("people"), Is.EqualTo(0));
    }

    [Test]
    public void CanPeekAtAQueue()
    {
      Assert.That("chris", Is.EqualTo(Resque.Peek("people")["name"]));
      Assert.That(Resque.size("people"), Is.EqualTo(3));
    }

    [Test]
    public void CanPeekAtMultipleItemsOnQueue()
    {
      ArrayList result = Resque.Peek("people", 1, 1);
      Assert.That("bob", Is.EqualTo((((Dictionary<string, object>)result[0]))["name"]));

      result = Resque.Peek("people", 1, 2);
      Assert.That(((Dictionary<string, object>)result[0])["name"], Is.EqualTo("bob"));
      Assert.That(((Dictionary<string, object>)result[1])["name"], Is.EqualTo("mark"));

      result = Resque.Peek("people", 0, 2);
      Assert.That(((Dictionary<string, object>)result[0])["name"], Is.EqualTo("chris"));
      Assert.That(((Dictionary<string, object>)result[1])["name"], Is.EqualTo("bob"));

      result = Resque.Peek("people", 2, 1);
      Assert.That(((Dictionary<string, object>)result[0])["name"], Is.EqualTo("mark"));
      Assert.That(Resque.Peek("people", 3), Is.Null);
    }

    [Test]
    public void KnowsWhatQuestsItIsManaging()
    {
      Assert.That(Resque.queues(), Is.EqualTo(new[] { "people" }));
      Resque.Push("cars", new Dictionary<string, string> { { "make", "bmw" } });
      Assert.That(Resque.queues(), Is.EqualTo(new[] { "cars", "people" }));
    }


    [Test]
    public void QueuesAreAlwaysAList()
    {
      Resque.redis().FlushAll();
      Assert.That(Resque.queues(), Is.EqualTo(new string[0]));
    }

    [Test]
    public void CanDeleteAQueue()
    {
      Resque.Push("cars", new Dictionary<string, string> { { "make", "bmw" } });
      Assert.That(Resque.queues(), Is.EqualTo(new[] { "cars", "people" }));
      Resque.RemoveQueue("people");
      Assert.That(Resque.queues(), Is.EqualTo(new[] { "cars" }));
    }

    [Test]
    public void KeepsTrackOfResqueKeys()
    {
      Assert.That(Resque.keys(), Is.EqualTo(new[] { "queue:people", "queues" }));
    }

    [Test]
    public void BadlyWantsAClassName()
    {
      Assert.That(
         new TestDelegate(TryToCreateJobWithNoClassName),
         Throws.TypeOf<resque.NoClassError>()
        );
    }

    [Test]
    public void KeepsStats()
    {
      Job.create("jobs", "resque.DummyJob", 20, "/tmp");
    }

    [Test]
    public void AlwaysReturnsSomeKindOfFailureWhenAsked()
    {
      Assert.That(Resque.failure, Is.Not.Null);
    }

    internal void EnqueueUninferrableJob()
    {
      Resque.enqueue("resque.UninferrableInvalidJob", 123);
    }

    internal void TryToCreateJobWithNoClassName()
    {
      Job.create("jobs", null);
    }

  }

}
